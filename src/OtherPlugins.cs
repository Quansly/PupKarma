using DevConsole;
using DevConsole.Commands;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using PupAi.Hooks;
using CWStuff;
using HUD;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PupKarma
{
    internal static class OtherPlugins
    {
        public static void InitILPupAI()
        {
            new ILHook(typeof(SlugNPCAIHooks).GetMethod(nameof(SlugNPCAIHooks.SlugNPCAI_Move)), il =>
            {
                ILCursor c1 = new(il);
                c1.GotoNext(MoveType.After,
                    x => x.MatchCeq(),
                    x => x.MatchAnd(),
                    x => x.MatchBrfalse(out _));
                ILLabel label = c1.MarkLabel();
                c1.GotoPrev(MoveType.After,
                    x => x.MatchStfld<Player>("standing"),
                    x => x.MatchNop(),
                    x => x.MatchNop());
                c1.Emit(OpCodes.Ldarg_1);
                c1.EmitDelegate((SlugNPCAI ai) =>
                {
                    return ai.cat.HaveKarmaFlower();
                });
                c1.Emit(OpCodes.Brtrue_S, label);

                ILCursor c2 = new(il);
                c2.GotoNext(MoveType.After,
                    x => x.MatchLdloc(6),
                    x => x.MatchBrfalse(out _));
                label = c2.MarkLabel();
                c2.GotoPrev(MoveType.After,
                    x => x.MatchLdloca(0),
                    x => x.MatchLdcI4(0),
                    x => x.MatchStfld<Player.InputPackage>("pckp"));
                c2.MoveAfterLabels();
                c2.Emit(OpCodes.Ldarg_1);
                c2.EmitDelegate((SlugNPCAI ai) =>
                {
                    return ai.cat.HaveKarmaFlower();
                });
                c2.Emit(OpCodes.Brtrue_S, label);
            });
        }

        public static void InitILSlugpupSafari()
        {
            IL.MoreSlugcats.SlugNPCAI.Move += (ILContext il) =>
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchLdsfld<SlugNPCAI.BehaviorType>("OnHead"));
                ILCursor cursor = c;
                cursor.GotoNext(x => x.MatchRet());
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((SlugNPCAI ai) =>
                {
                    if (ai.cat.HaveKarmaFlower())
                    {
                        ai.cat.input[0].pckp = true;
                    }
                });
            };
        }

        public static void RegisterPupsPlus()
        {
            PupClassKarmaInfo.RegisterSlugpup(SlugpupStuff.SlugpupStuff.VariantName.Rotundpup, true);
            PupClassKarmaInfo.RegisterSlugpup(SlugpupStuff.SlugpupStuff.VariantName.Hunterpup, true, 2);
            PupClassKarmaInfo.RegisterSlugpup(SlugpupStuff.SlugpupStuff.VariantName.Tundrapup, false, 0, 2);
        }

        public static bool IsPearlPup(this AbstractCreature crit)
        {
            return Pearlcat.Hooks.IsPearlpup(crit);
        }

        public class DevConsole
        {
            public static Dictionary<int, PupData> autoCompDatas = [];

            public static void DevConsoleInit()
            {
                On.RainWorldGame.ctor += DC_Hook_RainWorldGame_ctor;
                On.HUD.FoodMeter.ctor += DC_Hook_FoodMeter_ctor;
                DevConcoleCommandsBuilder();
            }

            private static void DC_Hook_FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player associatedPup, int pupNumber)
            {
                orig(self, hud, maxFood, survivalLimit, associatedPup, pupNumber);
                if (hud.owner is Player && associatedPup != null && associatedPup.abstractCreature.TryGetPupData(out PupData data))
                {
                    autoCompDatas.Add(pupNumber, data);
                }
            }

            private static void DC_Hook_RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
            {
                autoCompDatas.Clear();
                orig(self, manager);
            }

            public static void DevConcoleCommandsBuilder()
            {

                new CommandBuilder("karma_pups").RunGame((RainWorldGame game, string[] args) =>
                {
                    if (!game.IsStorySession)
                    {
                        GameConsole.WriteLine("You can change the karma of pups only in story mode");
                        return;
                    }
                    if (args.Length == 0)
                    {
                        GameConsole.WriteLine("Select pup");
                    }
                    else
                    {
                        if (args[0].Equals("allPups"))
                        {
                            if (args.Length == 1)
                            {
                                GameConsole.WriteLine("Select value");
                            }
                            else
                            {
                                if (args[1].Equals("reinforce"))
                                {
                                    foreach (PupData data in game.GetStorySession.GetStorySessionExt().allDatas)
                                    {
                                        data.reinforcedKarma = !data.reinforcedKarma;
                                        if (data.pupKarmaMeter != null)
                                        {
                                            data.pupKarmaMeter.reinforceAnimation = 0;
                                        }
                                    }
                                    GameConsole.WriteLine("Command completed successfully");
                                    return;
                                }
                                if (int.TryParse(args[1], out int newKarma))
                                {
                                    if (newKarma >= 0 && newKarma <= 9)
                                    {
                                        foreach (PupData data in game.GetStorySession.GetStorySessionExt().allDatas)
                                        {   
                                            data.karma = newKarma;
                                            data.pupKarmaMeter?.UpdateGraphic();
                                        }
                                        GameConsole.WriteLine($"Set karma for pups: {newKarma}");
                                        return;
                                    }
                                    GameConsole.WriteLine("Pups karma must be in the range from 0 to 9");
                                    return;
                                }
                                GameConsole.WriteLine("Pups karma must be an integer");
                            }
                            return;
                        }
                        else if (int.TryParse(args[0], out int dataNumber))
                        {
                            if (autoCompDatas.TryGetValue(dataNumber, out PupData data))
                            {
                                if (args.Length == 1)
                                {
                                    GameConsole.WriteLine($"Slugpup: {dataNumber} | Karma: {data.karma}");
                                }
                                else
                                {
                                    if (args[1].Equals("reinforce"))
                                    {
                                        data.reinforcedKarma = !data.reinforcedKarma;
                                        if (data.reinforcedKarma)
                                        {
                                            GameConsole.WriteLine("Slugpup karma reinforce set");
                                        }
                                        else
                                        {
                                            GameConsole.WriteLine("Slugpup karma reinforce removed");
                                        }
                                        if (data.pupKarmaMeter != null)
                                        {
                                            data.pupKarmaMeter.reinforceAnimation = 0;
                                        }
                                        return;

                                    }
                                    if (int.TryParse(args[1], out int newKarma))
                                    {
                                        if (newKarma >= 0 && newKarma <= 9)
                                        {
                                            data.karma = newKarma;
                                            data.pupKarmaMeter?.UpdateGraphic();
                                            GameConsole.WriteLine($"Set karma cap for pup number {dataNumber}: {newKarma}");
                                            return;
                                        }
                                        GameConsole.WriteLine("Pup karma must be in the range from 0 to 9");
                                        return;
                                    }
                                    GameConsole.WriteLine("Pup karma must be an integer");
                                }
                                return;
                            }
                            GameConsole.WriteLine("Slugpup data not found");
                            return;
                        }
                        GameConsole.WriteLine("Pup data number must be an integer");
                    }
                }).Help("karma_pups [pupFoodBarNumber?] [value?]").AutoComplete(agrs =>
                {
                    if (agrs.Length == 0) return autoCompDatas.Keys.Select(k => k.ToString()).Concat(["allPups"]); ;

                    if (agrs.Length == 1) return Enumerable.Select(Enumerable.Range(0, 10), i => i.ToString()).Concat(["reinforce"]);

                    return null;
                }).Register();

                new CommandBuilder("karma_cap_pups").RunGame((RainWorldGame game, string[] args) =>
                {
                    if (!game.IsStorySession)
                    {
                        GameConsole.WriteLine("You can change the karma cap of pups only in story mode");
                        return;
                    }
                    if (args.Length == 0)
                    {
                        GameConsole.WriteLine("Select pup");
                    }
                    else
                    {
                        if (args[0].Equals("allPups"))
                        {
                            if (args.Length == 1)
                            {
                                GameConsole.WriteLine("Select value");
                            }
                            else
                            {
                                if (int.TryParse(args[1], out int newKarmaCap))
                                {
                                    if (newKarmaCap >= 0 && newKarmaCap <= 9)
                                    {
                                        foreach (PupData data in game.GetStorySession.GetStorySessionExt().allDatas)
                                        {
                                            data.karmaCap = newKarmaCap;
                                        }
                                        GameConsole.WriteLine($"Set karma cap for pups: {newKarmaCap}");
                                        return;
                                    }
                                    GameConsole.WriteLine("Pups karma cap must be in the range from 0 to 9");
                                    return;
                                }
                                GameConsole.WriteLine("Pups karma cap must be an integer");
                            }
                            return;
                        }
                        else if (int.TryParse(args[0], out int dataNumber))
                        {
                            if (autoCompDatas.TryGetValue(dataNumber, out PupData data))
                            {
                                if (args.Length == 1)
                                {
                                    GameConsole.WriteLine($"Slugpup: {dataNumber} | Karma cap: {data.karmaCap}");
                                }
                                else
                                {
                                    if (int.TryParse(args[1], out int newKarmaCap))
                                    {
                                        if (newKarmaCap >= 0 && newKarmaCap <= 9)
                                        {
                                            data.karmaCap = newKarmaCap;
                                            GameConsole.WriteLine($"Set karma cap for pup number {dataNumber}: {newKarmaCap}");
                                            return;
                                        }
                                        GameConsole.WriteLine("Pup karma cap must be in the range from 0 to 9");
                                        return;
                                    }
                                    GameConsole.WriteLine("Pup karma cap must be an integer");
                                }
                                return;
                            }
                            GameConsole.WriteLine("Pup not found");
                            return;
                        }
                        GameConsole.WriteLine("Pup number must be an integer");
                    }
                }).Help("karma_cap_pups [pupFoodBarNumber?] [value?]").AutoComplete(agrs =>
                {
                    if (agrs.Length == 0)
                    {
                        return autoCompDatas.Keys.Select(k => k.ToString()).Concat(["allPups"]);
                    }

                    if (agrs.Length == 1) return new string[]
                    {
                    "4",
                    "6",
                    "7",
                    "8",
                    "9"
                    };

                    return null;
                }).Register();

                new CommandBuilder("assign_karma_to_slugpup").RunGame((RainWorldGame game, string[] args) =>
                {
                    if (!game.IsStorySession)
                    {
                        GameConsole.WriteLine("You can assign karma to pups only in story mode");
                        return;
                    }
                    if (args.Length == 0)
                    {
                        GameConsole.WriteLine("Select pup");
                    }
                    else
                    {
                        if (args[0].Equals("allPups"))
                        {
                            foreach (PupData data in game.GetStorySession.GetStorySessionExt().allDatas)
                            {
                                data.realData.AssignKarmaToPup();
                                data.pupKarmaMeter?.UpdateGraphic();
                            }
                            GameConsole.WriteLine("Command completed successfully");
                            return;
                        }
                        else if (int.TryParse(args[0], out int dataNumber))
                        {
                            if (autoCompDatas.TryGetValue(dataNumber, out PupData data))
                            {
                                data.realData.AssignKarmaToPup();
                                data.pupKarmaMeter?.UpdateGraphic();
                                GameConsole.WriteLine($"Assign karma for pup number {dataNumber}, new data:\nKarma: {data.karma}\nKarma cap: {data.karmaCap}");
                                return;
                            }
                            GameConsole.WriteLine("Pup not found");
                            return;
                        }
                        GameConsole.WriteLine("Pup number must be an integer");
                    }
                }).Help("assign_karma_to_slugpup [pupFoodBarNumber?]").AutoComplete(agrs =>
                {
                    if (agrs.Length == 0)
                    {
                        return autoCompDatas.Keys.Select(k => k.ToString()).Concat([ "allPups" ]);
                    }

                    return null;
                }).Register();
                Logger.DTDebug("KarmaPups commands created");
            }
        }

        public class CWHooks
        {
            private static bool karmaPups = false;

            public static SSOracleBehavior.Action IncraseKarmaToSlugpupsCW;

            internal static void Init()
            {
                RegisterValues();
                On.SSOracleBehavior.Update += Hook_CWBehaviour_Update;
                On.SSOracleBehavior.NewAction += Hook_CWBehaviour_NewAction;
                On.SSOracleBehavior.SlugcatEnterRoomReaction += Hook_CWBehaviour_ReactToSlug;
                On.SSOracleBehavior.SpecialEvent += Hook_CWBehaviour_SpecialEvent;
                InitHookCWConvAddEvents();
            }

            public static void RegisterValues()
            {
                IncraseKarmaToSlugpupsCW = new SSOracleBehavior.Action("IncraseKarmaToSlugpupsCW", true);
            }

            public static void UnregisterValues()
            {
                IncraseKarmaToSlugpupsCW?.Unregister();
                IncraseKarmaToSlugpupsCW = null;
            }

            private static void Hook_CWBehaviour_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
            {
                if (self.oracle.IsCW() && eventName.Equals("KARMAPUPS"))
                {
                    karmaPups = true;
                    return;
                }
                orig(self, eventName);
            }

            public static void Hook_CWBehaviour_ReactToSlug(On.SSOracleBehavior.orig_SlugcatEnterRoomReaction orig, SSOracleBehavior self)
            {
                if (self.oracle.IsCW() && self.oracle.HaveOneLowKarmaCapInIteratorRoom())
                {
                    if (self.currSubBehavior is CWGeneralConversation genB)
                    {
                        genB.SeenPlayer = true;
                    }
                    else if (self.currSubBehavior is CWNoSubBehavior noB)
                    {
                        noB.SeenPlayer = true;
                    }
                    self.NewAction(IncraseKarmaToSlugpupsCW);
                    return;
                }
                orig(self);
            }

            public static void InitHookCWConvAddEvents()
            {
                new Hook(typeof(CWConversation).GetMethod(nameof(CWConversation.AddEvents)), new Action<Action<CWConversation>, CWConversation>((Action<CWConversation> orig, CWConversation self) =>
                {
                    if (self is CWConversation convCW)
                    {
                        if (convCW.Convo.Contains("IncreasePupKarmaCW"))
                        {
                            CWConversation.CWEventsFromFile(convCW, "IncreaseKarmaCap_ToPups");
                            return;
                        }
                    }
                    orig(self);
                }));
            }

            public static void Hook_CWBehaviour_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
            {
                if (self.oracle.IsCW() && nextAction == IncraseKarmaToSlugpupsCW)
                {
                    self.currSubBehavior.NewAction(self.action, nextAction);
                    Conversation.ID id = new("IncreasePupKarmaCW");
                    CWGeneralConversation subB = new(self, id);
                    self.InitateConversation(id, subB);
                    self.allSubBehaviors.Add(subB);
                    subB.Activate(self.action, nextAction);
                    if (self.currSubBehavior is CWNoSubBehavior a)
                    {
                        subB.SeenPlayer = a.SeenPlayer;
                    }
                    else if (self.currSubBehavior is CWGeneralConversation b)
                    {
                        subB.SeenPlayer = b.SeenPlayer;
                    }
                    self.currSubBehavior.Deactivate();
                    self.currSubBehavior = subB;
                    self.inActionCounter = 0;
                    self.action = nextAction;
                    return;
                }
                orig(self, nextAction);
            }

            public static void Hook_CWBehaviour_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
            {
                if (self.oracle.IsCW() && self.action == IncraseKarmaToSlugpupsCW && karmaPups)
                {
                    AbstractRoom room = self.oracle.room.abstractRoom;
                    for (int i = 0; i < room.creatures.Count; i++)
                    {
                        if (room.creatures[i].TryGetPupData(out PupData data))
                        {
                            data.VisitIteratorAndIncreaseKarma(self.oracle);
                        }
                    }
                    self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);
                    karmaPups = false;
                }

                orig(self, eu);

                if (self.oracle.IsCW() && self.action == SSOracleBehavior.Action.General_GiveMark && self.player != null && self.inActionCounter == 300 && self.currSubBehavior is CWGeneralConversation CWconv && ((CWconv.Gifts & CWGeneralConversation.GiftStates.Karma10) == CWGeneralConversation.GiftStates.Karma10))
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
    }
}
