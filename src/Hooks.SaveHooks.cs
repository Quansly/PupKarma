using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PupKarma.Hooks
{
    internal class SaveHooks
    {
        public static void Init()
        {
            On.SaveState.SessionEnded += Hook_SaveState_SessionEnded;
            On.SaveState.GhostEncounter += Hook_SaveState_GhostEncounter;
            On.SaveState.LoadGame += Hook_SaveState_LoadGame;
            On.SaveState.ApplyCustomEndGame += Hook_SaveState_ApplyCustomEndGame;
            On.PlayerProgression.SaveToDisk += Hook_PlayerProgression_SaveToDisk;
            IL.PlayerProgression.SaveDeathPersistentDataOfCurrentState += IL_PlayerProgression_SaveDeathPersistentDataOfCurrentState;
            On.MoreSlugcats.PlayerNPCState.LoadFromString += Hook_PlayerNPCState_LoadFromString;
            On.MoreSlugcats.PlayerNPCState.ToString += Hook_PlayerNPCState_ToString;
            On.MoreSlugcats.PlayerNPCState.CycleTick += Hook_PlayerNPCState_CycleTick;
            On.RegionState.AdaptWorldToRegionState += Hook_RegionState_AdaptWorldToRegionState;
            IL.RegionState.AdaptRegionStateToWorld += IL_RegionState_AdaptRegionStateToWorld;
            On.PlayerProgression.MiscProgressionData.ToString += Hook_MiscProgressionData_ToString;
            On.PlayerProgression.MiscProgressionData.FromString += Hook_MiscProgressionData_FromString;
            On.PlayerProgression.WipeAll += Hook_PlayerProgression_WipeAll;
        }

        private static void Hook_PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
        {
            self.miscProgressionData.GetMPDExt().slugcatAscendedPups.Clear();
            orig(self);
        }

        private static void Hook_MiscProgressionData_FromString(On.PlayerProgression.MiscProgressionData.orig_FromString orig, PlayerProgression.MiscProgressionData self, string s)
        {
            orig(self, s);
            string[] array1 = Regex.Split(s, "<mpdA>");
            for (int i = 0; i < array1.Length; i++)
            {
                string[] array2 = Regex.Split(array1[i], "<mpdB>");
                if (array2[0] == "SLUGCATASCENDEDPUPS")
                {
                    self.GetMPDExt().slugcatAscendedPups[new(array2[1])] = int.Parse(array2[2]);
                    self.unrecognizedSaveStrings.Remove(array1[i]);
                }
            }
        }

        private static string Hook_MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
        {
            string result = orig(self);
            if (self.GetMPDExt().slugcatAscendedPups.Count > 0)
            {
                foreach (var kvp in self.GetMPDExt().slugcatAscendedPups)
                {
                    if (kvp.Value > 0)
                    {
                        result += $"SLUGCATASCENDEDPUPS<mpdB>{kvp.Key}<mpdB>{kvp.Value}<mpdA>";
                    }
                }
            }
            return result;
        }

        private static void IL_RegionState_AdaptRegionStateToWorld(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(x => x.MatchLdstr("Add pup to pendingFriendSpawns {0}"));
            c.GotoNext(MoveType.Before, x => x.MatchLdarg(0));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 6);
            c.EmitDelegate((RegionState regionState, AbstractCreature slugpup) =>
            {
                if (slugpup.TryGetPupData(out PupData data))
                {
                    regionState.world.game.GetStorySession.GetStorySessionExt().allDatas.Remove(data);
                    regionState.saveState.GetSVExt().stateHaveDataBefore.Remove(data.karmaState);
                }
            });
        }

        private static void Hook_RegionState_AdaptWorldToRegionState(On.RegionState.orig_AdaptWorldToRegionState orig, RegionState self)
        {
            orig(self);
            PupKarmaCWTs.SaveStateExt svEx = self.saveState.GetSVExt();
            Logger.Debug("Adapting flowers: " + svEx.flowerController.flowersPositions.Count);
            int i = 0;
            svEx.flowerController.flowersPositions.RemoveAll(coord =>
            {
                if (coord.Valid && self.world.IsRoomInRegion(coord.room))
                {
                    AbstractConsumable karmaFlower = new(self.world, AbstractPhysicalObject.AbstractObjectType.KarmaFlower, null,
                        coord, self.world.game.GetNewID(), -1, -1, null);
                    self.world.GetAbstractRoom(coord).AddEntity(karmaFlower);
                    Logger.DTDebug($"Flower loaded.\n\t{karmaFlower.ID}\n\tPosition: {karmaFlower.pos}\n\tRoom name: {karmaFlower.Room.name}");
                    if (self.saveState.saveStateNumber == SlugcatStats.Name.Yellow)
                    {
                        Logger.DTDebug("Slugcat is Monk! Add coord to associated flower dictionary.");
                        svEx.flowerController.associatedFlowersToPos.Add(karmaFlower, coord);
                    }
                    i++;
                    return true;
                }
                return false;
            });
            Logger.Debug("Flowers adapted: " + i);
        }

        private static void IL_PlayerProgression_SaveDeathPersistentDataOfCurrentState(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchStloc(1));
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((string[] origStrs, PlayerProgression progression, bool saveAsIfPlayerDied) =>
                {
                    try
                    {
                        for (int num1 = 0; num1 < origStrs.Length; num1++)
                        {
                            string[] array2 = Regex.Split(origStrs[num1], "<progDivB>");

                            if (array2[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(array2[1]) == progression.currentSaveState.saveStateNumber)
                            {
                                Logger.DTDebug($"Save state found. Player is dead: {saveAsIfPlayerDied}");
                                bool pupFlowersFound = false;
                                string[] pupsAndFlowers = progression.currentSaveState.GetSVExt().SavePupsAndFlowersToUnsaveable(saveAsIfPlayerDied);
                                string[] arr1 = Regex.Split(array2[1], "<svA>");
                                for (int num2 = 0; num2 < arr1.Length; num2++)
                                {
                                    string[] arr2 = Regex.Split(arr1[num2], "<svB>");

                                    if (arr2[0] == "FRIENDS" && pupsAndFlowers[0] != "")
                                    {
                                        Logger.DTDebug($"Friends found. Replace pups\nFriends bebore replaces: {arr2[1]}");
                                        string[] arr3 = Regex.Split(arr2[1], "<svC>");
                                        for (int num3 = 0; num3 < arr3.Length; num3++)
                                        {
                                            Logger.DTDebug("Friend: " + arr3[num3]);
                                            AbstractCreature abstractCreature = SaveState.AbstractCreatureFromString(null, arr3[num3], false);
                                            if (abstractCreature != null && abstractCreature.IsSlugpup())
                                            {
                                                Logger.DTDebug("Friend is slugpup");
                                                arr2[1] = Regex.Replace(arr2[1], arr3[num3] + "<svC>", "");
                                                Logger.DTDebug("After pup's null: " + arr2[1]);
                                            }
                                        }
                                        Logger.DTDebug("Pup to ready to save: " + pupsAndFlowers[0]);
                                        arr2[1] += pupsAndFlowers[0];
                                        string text = string.Join("<svB>", arr2);
                                        origStrs[num1] = Regex.Replace(origStrs[num1], arr1[num2], text);
                                        Logger.DTDebug("Friends after replaces: " + arr2[1]);
                                    }
                                    if (arr2[0] == "FlowerPupsPos" && !pupFlowersFound)
                                    {
                                        string text = $"FlowerPupsPos<svB>{pupsAndFlowers[1]}";
                                        if (pupsAndFlowers[1] != "") Logger.DTDebug("Save flowers: " + pupsAndFlowers[1]);
                                        origStrs[num1] = Regex.Replace(origStrs[num1], arr1[num2], text);
                                        pupFlowersFound = true;
                                    }
                                }
                                if (!pupFlowersFound && pupsAndFlowers[1] != "")
                                {
                                    Logger.DTDebug("Flowers not found. Saving separately");
                                    origStrs[num1] += $"FlowerPupsPos<svB>{pupsAndFlowers[1]}<svA>";
                                }
                                Logger.DTDebug("Save state result: " + origStrs[num1]);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    return origStrs;
                });
                c.Emit(OpCodes.Stloc_1);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void Hook_SaveState_ApplyCustomEndGame(On.SaveState.orig_ApplyCustomEndGame orig, SaveState self, RainWorldGame game, bool addFiveCycles)
        {
            self.GetSVExt().passage = true;
            orig(self, game, addFiveCycles);
        }

        private static void Hook_PlayerNPCState_CycleTick(On.MoreSlugcats.PlayerNPCState.orig_CycleTick orig, PlayerNPCState self)
        {
            orig(self);
            if (OptionsMenu.RevivePup.Value && self.player.world.game.IsStorySession && self.dead && self.TryGetPupData(out PupData data) && data.hadDataBefore && data.karma > 0)
            {
                Logger.DTDebug("Pup dead! Reviving...");
                self.alive = true;
                data.karma -= data.reinforcedKarma ? 0 : 1;
                data.reinforcedKarma = false;
            }
        }

        private static string Hook_PlayerNPCState_ToString(On.MoreSlugcats.PlayerNPCState.orig_ToString orig, PlayerNPCState self)
        {
            string text = orig(self);
            if (self.TryGetPupData(out PupData data) && !data.dontLoadData)
            {
                bool shouldRespawn = OptionsMenu.RevivePup.Value;
                if (shouldRespawn && self.dead && data.karma > 0)
                {
                    Logger.DTDebug("Pup dead! Replace dead strings...");
                    text = Regex.Replace(text, "Dead", (self.socialMemory != null && self.socialMemory.relationShips.Count > 0) ? self.socialMemory.ToString() : "");
                }
                text += $"PupData<cC>{data.karmaState.SaveToString(shouldRespawn && data.karmaState.dead)}<cB>";
            }
            return text;
        }

        private static void Hook_PlayerNPCState_LoadFromString(On.MoreSlugcats.PlayerNPCState.orig_LoadFromString orig, PlayerNPCState self, string[] s)
        {
            orig(self, s);
            if (self.TryGetPupData(out PupData data) && !data.hadDataBefore)
            {
                data.hadDataBefore = true;
                for (int i = 0; i < s.Length; i++)
                {
                    string[] array = Regex.Split(s[i], "<cC>");
                    if (array[0] == "PupData")
                    {
                        data.karmaState.LoadFromString(array[1]);
                        data.karmaAlreadyAssigned = true;
                        break;
                    }
                }
                self.unrecognizedSaveStrings.Remove("PupData");
                data.TryToSave();

                data.dontLoadData = true;
                data.karmaState.oldPupString = SaveState.AbstractCreatureToStringStoryWorld(self.player);
                data.dontLoadData = false;
            }
        }

        private static bool Hook_PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            try
            {
                if (self.currentSaveState != null)
                {
                    PupKarmaCWTs.SaveStateExt svEx = self.currentSaveState.GetSVExt();

                    if (svEx.passage)
                    {
                        svEx.flowerController.ClearFlowers();
                        for (int i = 0; i < self.currentSaveState.pendingFriendCreatures.Count; i++)
                        {
                            AbstractCreature abstractCreature = SaveState.AbstractCreatureFromString(null, self.currentSaveState.pendingFriendCreatures[i], false);
                            if (abstractCreature != null && abstractCreature.IsSlugpup())
                            {
                                string[] a1 = Regex.Split(Regex.Split(self.currentSaveState.pendingFriendCreatures[i], "<cA>")[3], "<cB>");
                                for (int num4 = 0; num4 < a1.Length; num4++)
                                {
                                    string[] a2 = Regex.Split(a1[num4], "<cC>");
                                    if (a2[0] == "PupData")
                                    {
                                        KarmaState karmaState = new();
                                        karmaState.LoadFromString(a2[1]);
                                        karmaState.karma = karmaState.karmaCap;
                                        self.currentSaveState.pendingFriendCreatures[i] = Regex.Replace(self.currentSaveState.pendingFriendCreatures[i], a2[1], karmaState.SaveToString(false));
                                        break;
                                    }
                                }
                            }
                        }
                        svEx.passage = false;
                    }
                    else if (saveCurrentState)
                    {
                        self.currentSaveState.AddUnrecognized(svEx.flowerController.SaveFlowers());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private static void Hook_SaveState_GhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
        {
            foreach (KarmaState karmaState in self.GetSVExt().stateHaveDataBefore)
            {
                if (karmaState.karmaCap < 9)
                {
                    karmaState.karmaCap += (karmaState.karmaCap == 4 ? 2 : 1);
                }
                karmaState.karma = karmaState.karmaCap;
            }
            orig(self, ghost, rainWorld);
        }

        private static void Hook_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
        {
            orig(self, str, game);
            PupKarmaCWTs.SaveStateExt svEx = self.GetSVExt();
            string[] array1 = Regex.Split(str, "<svA>");
            for (int i = 0; i < array1.Length; i++)
            {
                string[] array2 = Regex.Split(array1[i], "<svB>");
                if (array2[0] == "FlowerPupsPos")
                {
                    Logger.DTDebug("Loading flowers from string " + array2[1]);
                    svEx.flowerController.ClearFlowers();
                    string[] array3 = Regex.Split(array2[1], "<svC>");
                    for (int j = 0; j < array3.Length; j++)
                    {
                        if (array3[j] != string.Empty)
                        {
                            svEx.flowerController.flowersPositions.Add(WorldCoordinate.FromString(array3[j]));
                        }
                    }
                    Logger.DTDebug($"Total loaded flowers: {svEx.flowerController.flowersPositions.Count}");
                    break;
                }
            }
            string[] a = [.. self.unrecognizedSaveStrings];
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Contains("FlowerPupsPos") && a[i] != "")
                {
                    Logger.DTDebug("Remove flowers from unrecognized list string");
                    self.unrecognizedSaveStrings.Remove(a[i]);
                    break;
                }
            }
        }

        private static void Hook_SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            try
            {
                PupKarmaCWTs.SaveStateExt svEx = self.GetSVExt();
                PupKarmaCWTs.StorySessionExt sessionExt = game.GetStorySession.GetStorySessionExt();
                Logger.DTDebug("Session ended. Passing through an pups and datas. Datas found: " + sessionExt.allDatas.Count);
                if (survived)
                {
                    Logger.Debug("Player survive!");
                    svEx.stateHaveDataBefore.Clear();
                    AbstractRoom playerShelter = (game.FirstAlivePlayer ?? game.FirstAnyPlayer).Room;
                    if (playerShelter == null)
                    {
                        for (int i = 0; i < game.Players.Count; i++)
                        {
                            if (game.Players[i] != null && (playerShelter = game.Players[i].Room) != null)
                            {
                                break;
                            }
                        }
                    }
                    foreach (PupData data in sessionExt.allDatas)
                    {
                        bool notInShelter = playerShelter == null || playerShelter != data.pup.Room;

                        if ((notInShelter || data.pup.state.dead) && data.karmaState.karmaFlowerPos != null)
                        {
                            Logger.DTDebug("Slugpup is dead or not in shelter. Save pup flower!");
                            svEx.flowerController.flowersPositions.Add(data.karmaState.karmaFlowerPos.Value);
                        }

                        if (OptionsMenu.ReturnPupInShelterAfterSave.Value && notInShelter && data.hadDataBefore && data.karma > 0)
                        {
                            Logger.DTDebug($"Pup's not in the player's shelter! Return pup back!\nPup info: {data.pup}, pup class: {data.realData.realPup.slugcatStats.name}");

                            data.karmaState.dead = true;
                            string pupString = SaveState.AbstractCreatureToStringStoryWorld(data.pup);

                            pupString = Regex.Replace(pupString, $"Food<cC>{data.realData.realPup.FoodInStomach}", $"Food<cC>{SlugcatStats.SlugcatFoodMeter(data.realData.realPup.slugcatStats.name).y}");
                            Logger.DTDebug("Saving slugpup: " + pupString);
                            self.pendingFriendCreatures.Add(pupString);
                        }
                        else if (data.pup.state.alive)
                        {
                            if (data.karma < data.karmaCap)
                            {
                                data.karma++;
                            }
                            if (newMalnourished || data.realData.realPup.FoodInStomach < data.realData.realPup.slugcatStats.foodToHibernate)
                            {
                                data.reinforcedKarma = false;
                            }
                        }
                    }
                }
                else if (!self.lastMalnourished && (OptionsMenu.SelectDistributionKarmaReinforce.Value != 1 || OptionsMenu.SpawnFlowersWithASimpleDistribution.Value))
                {
                    Logger.Debug("Player not survive. Saving slugpups karma flowers");
                    sessionExt.AddAllFlowersFromDatas();
                }
                sessionExt.allDatas.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            orig(self, game, survived, newMalnourished);
        }
    }
}
