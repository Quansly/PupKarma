using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PupKarma;

public class PupData(PlayerNPCState pupState)
{
    public bool voidKarmaUp = true;

    public bool dontLoadData;

    public bool hadDataBefore;

    public bool karmaAlreadyAssigned;

    public bool needToSave = true;

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

    public void DataRealize()
    {
        realData = new(this, pup.realizedCreature as Player);
        if (!karmaAlreadyAssigned)
        {
            realData.AssignKarmaToPup();
        }
    }

    public void VisitIteratorAndIncreaseKarma(Oracle oracle)
    {
        if (!karmaState.gotIncreaseFromIterators.Contains(oracle.ID.value))
        {
            realData.IncreaseKarmaIterator();
            karmaState.gotIncreaseFromIterators.Add(oracle.ID.value);
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
                DeathPersistentSaveData savPlayerData = session.saveState.deathPersistentSaveData;
                if (OptionsMenu.AssignKarmaPlayer.Value)
                {
                    karmaCap = savPlayerData.karmaCap;
                    karma = savPlayerData.karma;
                }
                else if (!ModManager.Expedition || !Pup.world.game.rainWorld.ExpeditionMode)
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
                Logger.Debug($"Assigning karma to slugpup:\n\tClass: {realPup.slugcatStats.name}\n\t{owner}");
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

    public List<string> gotIncreaseFromIterators = [];

    public bool dead;

    public WorldCoordinate? karmaFlowerPos;

    public override string ToString()
    {
        return $"Karma: {karma} Karma cap: {karmaCap} Karma reinforce: {reinforcedKarma}";
    }

    public void LoadFromString(string s)
    {
        string[] array1 = Regex.Split(s, "<kpA>");
        for (int i = 0; i < array1.Length; i++)
        {
            string[] array2 = Regex.Split(array1[i], "<kpB>");
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
                case "VisitedIterators":
                    gotIncreaseFromIterators.AddRange(Regex.Split(array2[1], "<kpC>").Where(x => x != ""));
                    break;
            }
        }
    }

    public string SaveToString(bool pupIsDead)
    {
        string result;
        if (pupIsDead)
        {
            result = $"KarmaReinforce<kpB>{false}<kpA>Karma<kpB>{((reinforcedKarma || karma == 0) ? karma : (karma - 1))}<kpA>KarmaCap<kpB>{karmaCap}<kpA>";
        }
        else
        {
            result = $"KarmaReinforce<kpB>{reinforcedKarma}<kpA>Karma<kpB>{karma}<kpA>KarmaCap<kpB>{karmaCap}<kpA>";
        }
        if (gotIncreaseFromIterators.Count > 0)
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
