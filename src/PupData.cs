using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PupKarma;

public class PupData(PlayerNPCState pupState)
{
    public static bool needToSave = true;

    public bool dontLoadData;

    public bool hadDataBefore;

    public bool voidKarmaUp = true;

    public bool karmaAlreadyAssigned;

    public PupKarmaMeter pupKarmaMeter;

    public KarmaState karmaState = new();

    public AbstractCreature pup = pupState.player;

    public RealData realData;

    public Player firstBackPlayer;

    public int karma
    {
        get
        {
            return karmaState.karma;
        }
        set
        {
            karmaState.karma = value;
        }
    }

    public int karmaCap
    {
        get
        {
            return karmaState.karmaCap;
        }
        set
        {
            karmaState.karmaCap = value;
        }
    }

    public bool reinforcedKarma
    {
        get
        {
            return karmaState.reinforcedKarma;
        }
        set
        {
            karmaState.reinforcedKarma = value;
        }
    }

    public void TryToSave()
    {
        if (needToSave)
        {
            var storySession = pup.world.game.GetStorySession;
            if (!storySession.GetStorySessionExt().allDatas.Contains(this))
            {
                storySession.GetStorySessionExt().allDatas.Add(this);
            }
            if (hadDataBefore && !storySession.saveState.GetSVExt().stateHaveDataBefore.Contains(karmaState))
            {
                storySession.saveState.GetSVExt().stateHaveDataBefore.Add(karmaState);
            }
        }
    }

    public void DataRealize()
    {
        realData = new(this, pup.realizedCreature as Player);
        if (!karmaAlreadyAssigned)
        {
            realData.AssignKarmaToPup();
        }
        if (!hadDataBefore)
        {
            TryToSave();
        }
    }

    public void VisitIteratorAndIncreaseKarma(Oracle.OracleID oracle)
    {
        if (!karmaState.VisitOracle(oracle))
        {
            realData.IncreaseKarmaIterator();
            karmaState.AddOracle(oracle);
        }
    }

    public override string ToString()
    {
        return $"Pup{pup.ID}\n\tKarma info: ({karmaState})";
    }

    public class RealData
    {
        public PupData owner;

        public Player realPup;

        public PupClassKarmaInfo classKarmaInfo;

        public RealData(PupData data, Player slugpup)
        {
            owner = data;
            realPup = slugpup;
            classKarmaInfo = PupClassKarmaInfo.GetPupKarmaInfo(realPup.slugcatStats.name);
        }

        public AbstractCreature Pup
        {
            get
            {
                return owner.pup;
            }
        }

        public int karma
        {
            get
            {
                return owner.karma;
            }
            set
            {
                owner.karma = value;
            }
        }

        public int karmaCap
        {
            get
            {
                return owner.karmaCap;
            }
            set
            {
                owner.karmaCap = value;
            }
        }

        public void AssignKarmaToPup()
        {
            if (Pup.world.game.session is StoryGameSession session)
            {
                if (OptionsMenu.AssignKarmaPlayer.Value)
                {
                    DeathPersistentSaveData savPlayerData = session.saveState.deathPersistentSaveData;
                    karmaCap = savPlayerData.karmaCap;
                    karma = savPlayerData.karma;
                }
                else if (!(ModManager.Expedition && Pup.world.game.rainWorld.ExpeditionMode))
                {
                    int ghosts = session.GetStorySessionExt().ghosts;
                    int ghostKarmaCap = Mathf.Clamp(9 - (ghosts + ((ghosts > 3) ? 1 : 0)), classKarmaInfo.minKarmaCap, 9);
                    if (OptionsMenu.PersonalityKarma.Value)
                    {
                        
                        karmaCap = Mathf.Clamp(1 + Mathf.FloorToInt(9 * (Pup.personality.sympathy - (Pup.personality.aggression * 1.2f) - (Pup.personality.dominance / 2f))), ghostKarmaCap, 9);
                        karma = Mathf.Clamp(Mathf.FloorToInt(karmaCap * ((Pup.personality.bravery + Pup.personality.energy) * 0.8f + (Pup.personality.nervous / 1.25f))), classKarmaInfo.minKarma, karmaCap);
                    }
                    else
                    {
                        karmaCap = ghostKarmaCap;

                        Random.State state = Random.state;
                        Random.InitState(Pup.ID.RandomSeed);
                        karma = Mathf.RoundToInt((karmaCap - classKarmaInfo.minKarma) * Mathf.Pow(Random.value, 2.15f) + classKarmaInfo.minKarma);
                        Random.state = state;
                    }
                }
                owner.karmaAlreadyAssigned = true;
                Logger.DTDebug($"Assigning karma to slugpup:\n\tClass: {realPup.slugcatStats.name}\n\t{owner}");
            }
        }

        public void IncreaseKarmaIterator()
        {
            if (karmaCap < 9)
            {
                if (classKarmaInfo.iteratorKarmaAsHunter)
                {
                    karmaCap += karmaCap == 4 ? 2 : 1;
                }
                else
                {
                    karmaCap = 9;
                }
            }
            karma = karmaCap;
            owner.pupKarmaMeter?.UpdateGraphic();
        }
    }
}

public class KarmaState
{
    public int karma;

    public int karmaCap = 4;

    public bool reinforcedKarma;

    public string oldPupString = "";

    public string firstRespawnShelter = "";

    public bool dead;

    public WorldCoordinate? karmaFlowerPos;

    public List<Oracle.OracleID> gotIncreaseFromIterators;

    public override string ToString()
    {
        return $"Karma: {karma} Karma cap: {karmaCap} Karma reinforce: {reinforcedKarma}";
    }

    public void LoadFromString(string s)
    {
        string[] array1 = Regex.Split(s, "<kpA>");
        for (int num1 = 0; num1 < array1.Length; num1++)
        {
            string[] array2 = Regex.Split(array1[num1], "<kpB>");
            switch (array2[0])
            {
                case "KarmaReinforce":
                    reinforcedKarma = bool.Parse(array2[1]);
                    break;
                case "Karma":
                    karma = int.Parse(array2[1]);
                    break;
                case "KarmaCap":
                    karmaCap = int.Parse(array2[1]);
                    break;
                case "RespawnShelter":
                    firstRespawnShelter = array2[1];
                    Logger.Debug("Load shelter name: " + firstRespawnShelter);
                    break;
                case "VisitedIterators":
                    string[] oracles = Regex.Split(array2[1], "<kpC>");
                    for (int num2 = 0; num2 < oracles.Length; num2++)
                    {
                        if (oracles[num2] != "")
                        {
                            AddOracle(new(oracles[num2]));
                        }
                    }
                    break;
            }
        }
    }

    public void AddOracle(Oracle.OracleID oracle)
    {
        gotIncreaseFromIterators ??= [];
        gotIncreaseFromIterators.Add(oracle);
    }

    public bool VisitOracle(Oracle.OracleID oracle)
    {
        return gotIncreaseFromIterators != null && gotIncreaseFromIterators.Contains(oracle);
    }

    public string SaveToString(bool pupIsDead)
    {
        string result;
        if (pupIsDead)
        {
            result = $"KarmaReinforce<kpB>{false}<kpA>Karma<kpB>{((reinforcedKarma || karma == 0) ? karma : (karma - 1))}<kpA>KarmaCap<kpB>{karmaCap}<kpA>";
            if (OptionsMenu.CanRespawnPupInPrevShelter && firstRespawnShelter != "")
            {
                result += $"RespawnShelter<kpB>{firstRespawnShelter}<kpA>";
            }
        }
        else
        {
            result = $"KarmaReinforce<kpB>{reinforcedKarma}<kpA>Karma<kpB>{karma}<kpA>KarmaCap<kpB>{karmaCap}<kpA>";
        }
        if (gotIncreaseFromIterators != null)
        {
            result += $"VisitedIterators<kpB>";
            for (int i = 0; i < gotIncreaseFromIterators.Count; i++)
            {
                result += $"{gotIncreaseFromIterators[i]}<kpC>";
            }
            result += "<kpA>";
        }
        return result;
    }

    public string PupToStringWithOldState(bool pupIsDead)
    {
        return oldPupString + $"PupData<cC>{SaveToString(pupIsDead)}<cB>";
    }
}
