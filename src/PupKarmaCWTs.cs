using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using UnityEngine.Experimental.GlobalIllumination;

namespace PupKarma
{
    public static class PupKarmaCWTs
    {
        private static ConditionalWeakTable<PlayerNPCState, PupData> pupsData = new();

        private static ConditionalWeakTable<FoodMeter, PupKarmaMeter> foodKarmaCWT = new();

        private static ConditionalWeakTable<SaveState, SaveStateExt> saveStateExCWT = new();

        private static ConditionalWeakTable<RegionGate, GateExt> gateReq = new();

        private static ConditionalWeakTable<StoryGameSession, StorySessionExt> sessionExt = new();

        private static ConditionalWeakTable<PlayerProgression.MiscProgressionData, MiscProgDataExt> miscDataExt = new();

        private static PupData GetPupData(this PlayerNPCState state)
        {
            return pupsData.GetValue(state, _ => new PupData(state));
        }

        public static bool TryGetPupData(this PlayerNPCState pupState, out PupData data)
        {
            data = null;
            return pupState != null && pupState.player.world.game.IsStorySession && (data = pupState.GetPupData()) != null;
        }

        public static bool TryGetPupData(this AbstractCreature crit, out PupData data)
        {
            data = null;
            return crit != null && crit.world.game.IsStorySession && crit.state is PlayerNPCState pupState && (data = pupState.GetPupData()) != null;
        }

        public static PupKarmaMeter GetPKM(this FoodMeter meter)
        {
            return foodKarmaCWT.GetValue(meter, _ => PupKarmaMeter.CreatePupKarmaMeter(meter));
        }

        public static bool TryGetPupKarmaMeter(this FoodMeter foodMeter, out PupKarmaMeter pupKarmaMeter)
        {
            pupKarmaMeter = null;
            return foodMeter != null && foodMeter.hud.owner is Player && foodMeter.IsPupFoodMeter && (pupKarmaMeter = foodMeter.GetPKM()) != null;
        }

        public static SaveStateExt GetSVEX(this SaveState saveState)
        {
            return saveStateExCWT.GetOrCreateValue(saveState);
        }

        public static GateExt GetGateExt(this RegionGate gate)
        {
            return gateReq.GetOrCreateValue(gate);
        }

        public static StorySessionExt GetStorySessionExt(this StoryGameSession session)
        {
            return sessionExt.GetValue(session, _ => new StorySessionExt(session));
        }

        public static MiscProgDataExt GetMPDExt(this PlayerProgression.MiscProgressionData miscData)
        {
            return miscDataExt.GetOrCreateValue(miscData);
        }

        public class SaveStateExt
        {
            public bool passage;

            public int ascendedSlugpups;

            public FlowerController flowerController = new();

            public List<KarmaState> stateHaveDataBefore = [];

            public string[] SavePupsAndFlowersToUnsaveable(bool saveAsDead)
            {
                string resultPups = "";

                foreach (KarmaState karmaState in stateHaveDataBefore)
                {
                    resultPups += karmaState.PupToStringWithOldState(saveAsDead || karmaState.dead) + "<svC>";
                    if (!saveAsDead && karmaState.dead && karmaState.karmaFlowerPos != null)
                    {
                        flowerController.flowersPositions.Add(karmaState.karmaFlowerPos.Value);
                    }
                }
                stateHaveDataBefore.Clear();

                return
                [
                    resultPups,
                    flowerController.SaveFlowers()[1]
                ];
            }

            public class FlowerController
            {
                public List<WorldCoordinate> flowersPositions = [];

                public Dictionary<AbstractConsumable, WorldCoordinate> associatedFlowersToPos = [];

                public void ClearFlowers()
                {
                    flowersPositions.Clear();
                    associatedFlowersToPos.Clear();
                }

                public string[] SaveFlowers()
                {
                    string result = "";

                    Logger.DTDebug($"Flowers count: {flowersPositions.Count}\n\tassociated: {associatedFlowersToPos.Count}");

                    foreach (WorldCoordinate flowerPos in flowersPositions.Concat(associatedFlowersToPos.Values))
                    {
                        result += flowerPos.SaveToString() + "<svC>";
                    }
                    if (result != "")
                    {
                        Logger.DTDebug("Saving flowers: " + result);
                    }
                    return
                    [
                        "FlowerPupsPos",
                        result
                    ];
                }
            }
        }

        public class GateExt
        {
            public bool interstitialPlayerReq;
        }

        public class StorySessionExt
        {
            public StoryGameSession session;

            public List<PupData> allDatas = [];

            public int ghosts;

            public int intermediateAscendedPups;

            public StorySessionExt(StoryGameSession session)
            {
                this.session = session;
                List<string> regions = SlugcatStats.SlugcatStoryRegions(session.saveStateNumber);
                for (int i = 0; i < regions.Count; i++)
                {
                    if (World.CheckForRegionGhost(session.saveStateNumber, regions[i]))
                    {
                        GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(regions[i]);
                        if (!(session.saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID) && (session.saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] == 2)))
                        {
                            ghosts++;
                        }
                    }
                }
            }

            public void AddAllFlowersFromDatas()
            {
                int i = 0;
                foreach (PupData data in allDatas)
                {
                    if (data.karmaState.karmaFlowerPos != null && data.karmaState.karmaFlowerPos.Value.Valid)
                    {
                        session.saveState.GetSVEX().flowerController.flowersPositions.Add(data.karmaState.karmaFlowerPos.Value);
                        data.karmaState.karmaFlowerPos = null;
                        i++;
                    }
                }
                Logger.DTDebug("Flowers saved: " + i);
            }
        }

        public class MiscProgDataExt
        {
            public Dictionary<SlugcatStats.Name, int> slugcatAscendedPups = [];
        }
    }
}
