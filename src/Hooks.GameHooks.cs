using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidSea;

namespace PupKarma.Hooks
{
    internal class GameHooks
    {
        public static void Init()
        {
            On.KarmaFlower.BitByPlayer += Hook_KarmaFlower_BitByPlayer;
            IL.KarmaFlower.BitByPlayer += IL_KarmaFlower_BitByPlayer;
            On.KarmaFlower.Consume += Hook_KarmaFlower_Consume;
            On.MoreSlugcats.SlugNPCAI.WantsToEatThis += Hook_SlugNPCAI_WantsToEatThis;
            IL.MoreSlugcats.SlugNPCAI.Move += IL_SlugNPCAI_Move;
            On.Player.ctor += Player_ctor;
            On.Player.Update += Hook_Player_Update;
            IL.Player.UpdateMSC += Player_UpdateMSC;
            On.Player.SlugOnBack.SlugToBack += Hook_SlugOnBack_SlugToBack;
            On.AbstractCreature.Update += Hook_AbstractCreature_Update;
            InitHookMeetRequirment();
            IL.RegionGate.KarmaBlinkRed += IL_RegionGate_KarmaBlinkRed;
            On.SSOracleBehavior.Update += Hook_SSOracleBehavior_Update;
            On.SSOracleBehavior.NewAction += Hook_SSOracleBehavior_NewAction;
            IL.SSOracleBehavior.SeePlayer += IL_SSOracleBehavior_SeePlayer;
            IL.SSOracleBehavior.SSSleepoverBehavior.ctor += IL_SSSleepoverBehavior_ctor;
            IL.SSOracleBehavior.Update += IL_SSOracleBehavior_Update;
            On.SSOracleBehavior.ReactToHitWeapon += Hook_SSOracleBehavior_ReactToHitWeapon;
            On.RoomSpecificScript.SB_A14KarmaIncrease.Update += Hook_SB_A14KarmaIncrease_Update;
            On.TempleGuardAI.ThrowOutScore += Hook_TempleGuardAI_ThrowOutScore;
            On.VoidSea.VoidSeaScene.Update += Hook_VoidSeaScene_Update;
            IL.World.SpawnGhost += IL_World_SpawnGhost;
            On.RainWorldGame.ExitToVoidSeaSlideShow += Hook_RainWorldGame_ExitToVoidSeaSlideShow;
        }

        private static void Hook_RainWorldGame_ExitToVoidSeaSlideShow(On.RainWorldGame.orig_ExitToVoidSeaSlideShow orig, RainWorldGame self)
        {
            if (self.session is StoryGameSession session)
            {
                var countPupDic = session.saveState.progression.miscProgressionData.GetMiscProgDataExt().slugcatAscendedPups;
                if (countPupDic.ContainsKey(session.saveStateNumber))
                {
                    countPupDic[session.saveStateNumber] = session.GetStorySessionExt().intermediateAscendedPups;
                }
                else
                {
                    countPupDic.Add(session.saveStateNumber, session.GetStorySessionExt().intermediateAscendedPups);
                }
            }
            orig(self);
        }

        private static void Player_UpdateMSC(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchLdfld<Creature.Grasp>("grabbed"));
                c.EmitDelegate((PhysicalObject obj) =>
                {
                    return obj != null && !(obj is Player player && player.isNPC && !(PupKarmaMain.Pearlcat && player.abstractCreature.IsPearlPup()));
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void IL_World_SpawnGhost(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchCall(typeof(GhostWorldPresence).GetMethod(nameof(GhostWorldPresence.SpawnGhost))));

                List<PupData> pupDataGhosts = [];
                bool aritPupReject = false;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.EmitDelegate((bool ob, World world, int num) =>
                {
                    int opVal = OptionsMenu.SelectReqToSpawnGhost.Value;
                    if (opVal == 0) return ob;
                    foreach (string str in world.game.GetStorySession.saveState.pendingFriendCreatures)
                    {
                        AbstractCreature crit = SaveState.AbstractCreatureFromString(world, str, false);
                        if (crit != null && crit.IsSlugpup() && crit.TryGetPupData(out PupData data))
                        {
                            pupDataGhosts.Add(data);
                            crit.state.CycleTick();
                            data.needToSave = false;
                            crit.Realize();
                        }
                    }
                    bool isOne = opVal == 1;
                    foreach (PupData data in pupDataGhosts.Where(data => data.pup.state.alive))
                    {
                        if (GhostWorldPresence.SpawnGhost(GhostWorldPresence.GetGhostID(world.region.name), data.karma, data.karmaCap, num, world.game.StoryCharacter == SlugcatStats.Name.Red) == isOne)
                        {
                            Logger.DTDebug(isOne ? "Pup has suitable karma level! Spawn echo!" : "Pup doesn't has suitable karma level! Echo spawn canceled!");
                            aritPupReject = true;
                            return isOne;
                        }
                    }
                    return ob;
                });

                c.GotoNext(MoveType.After, x => x.MatchLdfld<DeathPersistentSaveData>("reinforcedKarma"));

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.EmitDelegate((bool ob, World world, int num) =>
                {
                    int opVal = OptionsMenu.SelectReqToSpawnGhost.Value;
                    if (opVal == 1)
                    {
                        foreach (PupData data in pupDataGhosts.Where(data => data.pup.state.alive))
                        {

                            if (data.karmaCap < 4 && data.karmaCap == data.karma && data.reinforcedKarma)
                            {
                                Logger.DTDebug("Arti: Pup has suitable karma level! Spawn echo!");
                                return true;
                            }
                        }
                    }
                    else if (opVal == 2 && aritPupReject)
                    {
                        return false;
                    }

                    return ob;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void Hook_VoidSeaScene_Update(On.VoidSea.VoidSeaScene.orig_Update orig, VoidSeaScene self, bool eu)
        {
            int voidPups = 0;
            for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
            {
                AbstractCreature crit = self.room.abstractRoom.creatures[i];
                if (crit.IsSlugpup() && !(PupKarmaMain.Pearlcat && crit.IsPearlPup()))
                {
                    Player pup = crit.realizedCreature as Player;
                    if (pup.inVoidSea = pup.mainBodyChunk.pos.y < self.sceneOrigo.y && !self.Inverted)
                    {
                        voidPups++;
                    }
                    self.UpdatePlayerInVoidSea(pup);
                }
            }
            if (self.room.game.session is StoryGameSession session)
            {
                session.GetStorySessionExt().intermediateAscendedPups = voidPups;
            }
            orig(self, eu);
        }

        private static float Hook_TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (self.creature.world.game.IsStorySession && crit.representedCreature != null && crit.representedCreature.IsSlugpup())
            {
                if (OptionsMenu.IgnoreGuardianKarma.Value || (crit.representedCreature.TryGetPupData(out PupData data) && data.karmaCap == 9))
                {
                    return 0;
                }
                else
                {
                    if (crit.representedCreature.realizedCreature == self.pickUpObject)
                    {
                        (crit.representedCreature.realizedCreature as Player).onBack?.slugOnBack.DropSlug();
                        if (crit.representedCreature.realizedCreature.grabbedBy.Count > 0 && crit.representedCreature.realizedCreature.grabbedBy[0].grabber is Player)
                        {
                            crit.representedCreature.realizedCreature.grabbedBy[0].Release();
                        }
                    }
                    return 500f / (self.ProtectExitDistance(crit.BestGuessForPosition().Tile) + crit.TicksSinceSeen / 2f);
                }
            }
            return orig(self, crit);
        }

        private static void Hook_SB_A14KarmaIncrease_Update(On.RoomSpecificScript.SB_A14KarmaIncrease.orig_Update orig, RoomSpecificScript.SB_A14KarmaIncrease self, bool eu)
        {
            if (self.room.game.IsStorySession)
            {
                AbstractRoom room = self.room.abstractRoom;
                for (int i = 0; i < room.creatures.Count; i++)
                {
                    if (room.creatures[i].TryGetPupData(out PupData data) && data.voidKarmaUp && room.creatures[i].realizedCreature.room == self.room && room.creatures[i].realizedCreature.firstChunk.pos.x < 550f && data.karmaCap == 9)
                    {
                        data.karma = data.karmaCap;
                        if (data.pupKarmaMeter != null)
                        {
                            data.pupKarmaMeter.reinforceAnimation = 1;
                        }
                        data.voidKarmaUp = false;
                    }
                }
            }
            orig(self, eu);
        }

        private static void IL_SSOracleBehavior_Update(ILContext il)
        {
            ILCursor c1 = new(il);
            c1.GotoNext(x => x.MatchLdstr("Yes, help yourself. They are not edible."));
            c1.GotoNext(MoveType.After, x => x.MatchStfld<SSOracleBehavior>("lastPearlPickedUp"));
            ILLabel label = c1.MarkLabel();

            ILCursor c2 = new(il);
            c2.GotoNext(MoveType.After, x => x.MatchLdfld<SSOracleBehavior>("pearlPickupReaction"));
            c2.GotoPrev(MoveType.Before, x => x.MatchLdarg(0));

            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate((SSOracleBehavior self) =>
            {
                return self.currSubBehavior.ID == SSPupKarmaSubBehaviour.SSBehaviourIDs.IncreasePupKarmaCap;
            });
            c2.Emit(OpCodes.Brtrue, label);
        }

        private static void IL_SSSleepoverBehavior_ctor(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(x => x.MatchCallvirt<SSOracleBehavior>("TurnOffSSMusic"));
            c.GotoNext(MoveType.After, x => x.MatchStfld<SSOracleBehavior.SSSleepoverBehavior>("gravOn"));

            ILCursor c2 = new(c);
            c2.GotoNext(x => x.MatchLdarg(0));
            ILLabel label = c2.MarkLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((SSOracleBehavior.SSSleepoverBehavior self) =>
            {
                return self.owner.currSubBehavior.ID == SSPupKarmaSubBehaviour.SSBehaviourIDs.IncreasePupKarmaCap;
            });
            c.Emit(OpCodes.Brfalse_S, label);
            c.Emit(OpCodes.Ret);
        }

        private static void IL_SSOracleBehavior_SeePlayer(ILContext il)
        {
            ILCursor c1 = new(il);
            c1.GotoNext(x => x.MatchLdstr("Artificer visit"));
            c1.GotoNext(x => x.MatchCall<int>("ToString"));
            c1.GotoNext(MoveType.Before, x => x.MatchLdarg(0));

            ILCursor c2 = new(c1);
            c2.GotoNext(x => x.MatchBr(out _));
            ILLabel label = c2.MarkLabel();

            c1.Emit(OpCodes.Ldarg_0);
            c1.EmitDelegate((SSOracleBehavior owner) =>
            {
                if ((!ModManager.Expedition || !owner.oracle.room.game.rainWorld.ExpeditionMode) && owner.oracle.HaveOneLowKarmaCapInIteratorRoom())
                {
                    owner.NewAction(SSPupKarmaSubBehaviour.SSActions.TalkingAboutSlugpups);
                    return true;
                }
                return false;
            });
            c1.Emit(OpCodes.Brtrue_S, label);
        }

        private static void Hook_SSOracleBehavior_ReactToHitWeapon(On.SSOracleBehavior.orig_ReactToHitWeapon orig, SSOracleBehavior self)
        {
            if (self.currSubBehavior.ID == SSPupKarmaSubBehaviour.SSBehaviourIDs.IncreasePupKarmaCap)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    self.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, self.oracle.firstChunk).requireActiveUpkeep = false;
                }
                else
                {
                    self.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, self.oracle.firstChunk).requireActiveUpkeep = false;
                }
                self.conversation = null;
                self.dialogBox.Interrupt("GET OUT! YOU DON'T NEED THIS HELP!", 10);
                self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                return;
            }
            orig(self);
        }

        private static void Hook_SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
        {
            if (nextAction == SSPupKarmaSubBehaviour.SSActions.TalkingAboutSlugpups)
            {
                self.currSubBehavior.NewAction(self.action, nextAction);
                SSOracleBehavior.SubBehavior subBehavior = new SSPupKarmaSubBehaviour(self);
                self.allSubBehaviors.Add(subBehavior);
                subBehavior.Activate(self.action, nextAction);
                self.action = nextAction;
                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;
                self.inActionCounter = 0;
                return;
            }
            else if (nextAction == SSPupKarmaSubBehaviour.SSActions.IncraseKarmaToSlugpups)
            {
                self.inActionCounter = 0;
                self.action = nextAction;
                return;
            }
            orig(self, nextAction);
        }

        private static void Hook_SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (self.oracle.room.game.IsStorySession)
            {
                if (self.action == SSOracleBehavior.Action.General_GiveMark && self.oracle.ID == Oracle.OracleID.SS && self.inActionCounter == 300 && self.oracle.room.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear)
                {
                    AbstractRoom room = self.oracle.room.abstractRoom;
                    for (int i = 0; i < room.creatures.Count; i++)
                    {
                        if (room.creatures[i].TryGetPupData(out PupData data))
                        {
                            data.VisitIteratorAndIncreaseKarma(self.oracle);
                        }
                    }
                }
            }
        }

        private static void IL_RegionGate_KarmaBlinkRed(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchCallvirt<RegionGate>("get_MeetRequirement"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((bool origBool, RegionGate gate) =>
                {
                    return origBool || (OptionsMenu.SelectKarmagateMode.Value == 2 && gate.GetGateExt().interstitialPlayerReq);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void InitHookMeetRequirment()
        {
            try
            {
                new ILHook(typeof(RegionGate).GetMethod("get_MeetRequirement"), il =>
                {
                    ILCursor c = new(il);
                    c.GotoNext(MoveType.After, x => x.MatchLdloc(5));
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((bool origBool, RegionGate gate) =>
                    {
                        gate.GetGateExt().interstitialPlayerReq = origBool;
                        int var = OptionsMenu.SelectKarmagateMode.Value;
                        if (var > 0)
                        {
                            bool isOne = var == 1;
                            foreach (AbstractCreature pup in gate.room.abstractRoom.creatures.Where(crits => crits.IsSlugpup() && crits.state.alive))
                            {
                                int zone = gate.DetectZone(pup);
                                if (pup.TryGetPupData(out PupData data) && zone != 0 && zone != 3)
                                {
                                    if (gate.karmaRequirements[gate.letThroughDir ? 0 : 1].value == "10" && isOne && !gate.KarmaGateRequirementPup(data.karmaState))
                                    {
                                        return false;
                                    }
                                    else if (gate.KarmaGateRequirementPup(data.karmaState) == isOne)
                                    {
                                        return isOne;
                                    }
                                }
                            }
                        }
                        return origBool;
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void Hook_AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig(self, time);
            if (self.state is PlayerNPCState pupState && pupState.TryGetPupData(out PupData data))
            {
                data.karmaState.dead = pupState.dead;
            }
        }

        private static void Hook_SlugOnBack_SlugToBack(On.Player.SlugOnBack.orig_SlugToBack orig, Player.SlugOnBack self, Player playerToBack)
        {
            orig(self, playerToBack);
            if (self.slugcat != null && self.slugcat.isNPC && self.slugcat.abstractCreature.TryGetPupData(out PupData data))
            {
                data.firstBackPlayer = playerToBack.GetFirstPlayerOnBack();
            }
        }

        private static void Hook_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.isNPC && self.abstractCreature.TryGetPupData(out PupData data))
            {
                if (data.realData.realPup != self)
                {
                    data.realData.realPup = self;
                }
                if (self.onBack == null && data.firstBackPlayer != null)
                {
                    data.firstBackPlayer = null;
                }
                if (OptionsMenu.SpawnKarmaFlowers.Value && data.reinforcedKarma && !self.dead)
                {
                    if (data.firstBackPlayer != null && data.firstBackPlayer.PosReadyToSpawnKarmaFlower())
                    {
                        data.karmaState.karmaFlowerPos = data.firstBackPlayer.room.GetWorldCoordinate(data.firstBackPlayer.bodyChunks[1].pos);
                    }
                    else if ((self.grabbedBy.Count == 0 || self.grabbedBy[0].grabber is Player) && self.PosReadyToSpawnKarmaFlower())
                    {
                        data.karmaState.karmaFlowerPos = self.room.GetWorldCoordinate(self.bodyChunks[1].pos);
                    }
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (world.game.session is StoryGameSession storySession && abstractCreature.TryGetPupData(out PupData data))
            {
                data.DataRealize();
                if (data.needToSave)
                {
                    Logger.DTDebug("saving");
                    if (!storySession.GetStorySessionExt().allDatas.Contains(data))
                    {
                        storySession.GetStorySessionExt().allDatas.Add(data);
                    }
                    if (data.hadDataBefore && !storySession.saveState.GetSVEX().stateHaveDataBefore.Contains(data.karmaState))
                    {
                        storySession.saveState.GetSVEX().stateHaveDataBefore.Add(data.karmaState);
                    }
                }
            }
        }

        private static void IL_SlugNPCAI_Move(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After,
                    x => x.MatchCeq(),
                    x => x.MatchAnd(),
                    x => x.MatchBrfalse(out _));
                ILLabel label = c.MarkLabel();
                c.GotoPrev(MoveType.After, x => x.MatchStfld<Player>("standing"));
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((SlugNPCAI self) =>
                {
                    return self.cat.HaveKarmaFlower();
                });
                c.Emit(OpCodes.Brtrue_S, label);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static bool Hook_SlugNPCAI_WantsToEatThis(On.MoreSlugcats.SlugNPCAI.orig_WantsToEatThis orig, SlugNPCAI self, PhysicalObject obj)
        {
            if (obj is KarmaFlower flower && self.creature.world.game.IsStorySession && self.creature.TryGetPupData(out PupData data))
            {
                if (flower.AbstrConsumable.originRoom == -2 && OptionsMenu.DontPickUpPlayerFlower.Value && !self.HoldingThis(flower))
                {
                    return false;
                }
                return !data.reinforcedKarma;
            }
            return orig(self, obj);
        }

        private static void Hook_KarmaFlower_Consume(On.KarmaFlower.orig_Consume orig, KarmaFlower self)
        {
            orig(self);
            if (self.room.game.session is StoryGameSession storySession && storySession.saveState.GetSVEX().flowerController.associatedFlowersToPos.Remove(self.AbstrConsumable))
            {
                Logger.Debug("Flower be consumed! Remove from associated flowers dictionary.");
            }
        }

        private static void IL_KarmaFlower_BitByPlayer(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchCallvirt<Creature.Grasp>("Release"));
                c.GotoPrev(x => x.MatchLdarg(1));
                ILLabel label = c.MarkLabel();

                c.GotoPrev(MoveType.After, x => x.MatchCallvirt<Player>("ObjectEaten"));

                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((Creature.Grasp grasp) =>
                {
                    Player player = grasp.grabber as Player;
                    if (OptionsMenu.SelectDistributionKarmaReinforce.Value == 2 && player.room.game.IsStorySession && player.isNPC && player.abstractCreature.TryGetPupData(out PupData data) && !data.reinforcedKarma)
                    {
                        data.reinforcedKarma = true;
                        if (data.pupKarmaMeter != null && !data.pupKarmaMeter.showAsReinforced)
                        {
                            data.pupKarmaMeter.reinforceAnimation = 0;
                        }
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, label);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void Hook_KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            int opNum = OptionsMenu.SelectDistributionKarmaReinforce.Value;
            if (self.room.game.IsStorySession && self.bites < 1 && opNum != 2)
            {
                Player player = grasp.grabber as Player;
                if (player.isNPC)
                {
                    if (opNum == 0)
                    {
                        if (player.abstractCreature.TryGetPupData(out PupData data) && !data.reinforcedKarma)
                        {
                            data.reinforcedKarma = true;
                            if (data.pupKarmaMeter != null && !data.pupKarmaMeter.showAsReinforced)
                            {
                                data.pupKarmaMeter.reinforceAnimation = 0;
                            }
                        }
                    }
                    foreach (RoomCamera cam in player.room.game.cameras)
                    {
                        if (!cam.hud.karmaMeter.showAsReinforced)
                        {
                            cam.hud.karmaMeter.reinforceAnimation = 0;
                        }
                    }
                }
                if (opNum == 1)
                {
                    foreach (PupData data in player.room.game.GetStorySession.GetStorySessionExt().allDatas.Where(data => !data.reinforcedKarma && data.pup.state.alive))
                    {
                        data.reinforcedKarma = true;
                        if (data.pupKarmaMeter != null && !data.pupKarmaMeter.showAsReinforced)
                        {
                            data.pupKarmaMeter.reinforceAnimation = 0;
                        }
                    }
                }
            }
        }
    }
}
