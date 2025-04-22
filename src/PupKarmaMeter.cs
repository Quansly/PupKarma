using HUD;
using RWCustom;
using System;
using UnityEngine;
using MoreSlugcats;

namespace PupKarma
{
    public class PupKarmaMeter : HudPart
    {
        public Vector2 pos;

        public Vector2 lastPos;

        public FSprite ringSprite;

        public FSprite vectorRingSprite;

        public FSprite darkFade;

        public FSprite karmaSprite;

        public FSprite glowSprite;

        public IntVector2 displayKarma;

        public Color karmaColor;

        public FoodMeter pupFoodMeter;

        public AbstractCreature abstractPup;

        public Player pup;

        public KarmaState karmaState;

        public bool showAsReinforced;

        public bool blinkRed;

        public float rad;

        public float lastRad;

        public float fade;

        public float uselessFade;

        public float lastFade;

        public float glowyFac;

        public float lastGlowyFac;

        public float foodMeterYPos;

        private float lastReinforcementCycle;

        private float reinforcementCycle;

        public int reinforceAnimation = -1;

        public int forceVisibleCounter;

        public int timer;

        public int color;

        public bool Show
        {
            get
            {
                return hud.owner.RevealMap || hud.showKarmaFoodRain || blinkRed || pupFoodMeter.notInShelter > 0f;
            }
        }

        public float Radius
        {
            get
            {
                return rad + (showAsReinforced ? 5.4f : 0);
            }
        }

        public FContainer fContainer
        {
            get
            {
                return hud.fContainers[1];
            }
        }

        public PupKarmaMeter(FoodMeter pupFoodMeter, KarmaState karmaState) : base(pupFoodMeter.hud)
        {
            this.karmaState = karmaState;
            pup = pupFoodMeter.pup;
            abstractPup = pup.abstractCreature;
            showAsReinforced = karmaState.reinforcedKarma;
            displayKarma = new IntVector2(karmaState.karma, karmaState.karmaCap);
            displayKarma.x = Custom.IntClamp(displayKarma.x, 0, displayKarma.y);
            this.pupFoodMeter = pupFoodMeter;
            darkFade = new("Futile_White")
            {
                shader = hud.rainWorld.Shaders["FlatLight"],
                color = new Color(0f, 0f, 0f)
            };
            fContainer.AddChild(darkFade);
            karmaSprite = new(KarmaMeter.KarmaSymbolSprite(true, displayKarma));
            fContainer.AddChild(karmaSprite);
            glowSprite = new("Futile_White")
            {
                shader = hud.rainWorld.Shaders["FlatLight"]
            };
            fContainer.AddChild(glowSprite);
        }

        public PupKarmaMeter(FoodMeter meter, PupData data) : this(meter, data.karmaState)
        {
            data.pupKarmaMeter = this;
        }

        public override void Update()
        {
            lastPos = pos;
            lastFade = fade;
            lastRad = rad;
            lastGlowyFac = glowyFac;
            float posY = foodMeterYPos + 20f + 20f * pupFoodMeter.pupNumber;

            if (hud.gourmandmeter != null)
            {
                posY -= 20f + 5f * hud.gourmandmeter.visibleRows;
            }
            pos = new(pupFoodMeter.pos.x - 30f, posY);
            
            if (abstractPup != null && abstractPup.realizedCreature != null && abstractPup.realizedCreature != pup)
            {
                pup = abstractPup.realizedCreature as Player;
            }
            if (fade > 0f)
            {
                glowyFac = Custom.LerpAndTick(glowyFac, fade * (showAsReinforced ? 1f : 0.9f), 0.1f, 0.033333335f);
                timer++;
            }
            else
            {
                glowyFac = 0f;
            }
            lastReinforcementCycle = reinforcementCycle;
            reinforcementCycle += 0.011111111f;
            if (OptionsMenu.SelectKarmagateMode.Value != 0 && abstractPup.state.alive && hud.owner != null && hud.owner is Player mainPlayerVis && mainPlayerVis.room != null &&
                mainPlayerVis.room.abstractRoom != null && mainPlayerVis.room.abstractRoom.gate && pup.room != null && pup.room == mainPlayerVis.room
                && mainPlayerVis.room.regionGate != null && mainPlayerVis.room.regionGate.mode == RegionGate.Mode.MiddleClosed)
            {
                forceVisibleCounter = Math.Max(forceVisibleCounter, 10);
            }
            if (OptionsMenu.LightUpPKM.Value && pup.HaveKarmaFlower())
            {
                forceVisibleCounter = Math.Max(forceVisibleCounter, 20);
                glowyFac = 1.25f;
            }
            if ((pupFoodMeter.PupInDanger && pupFoodMeter.deathFade == 0f) || (pupFoodMeter.deathFade > 0f && pupFoodMeter.deathFade < 1f))
            {
                if (fade < 1f)
                {
                    fade = Mathf.Min(1f, pupFoodMeter.fade + 0.1f);
                    uselessFade = Mathf.Min(1f, uselessFade + 0.1f);
                }
                else
                {
                    fade = Mathf.Max(1f, pupFoodMeter.fade - 0.1f);
                    uselessFade = Mathf.Max(1f, uselessFade - 0.1f);
                }
            }
            else if (Show)
            {
                float num = Mathf.Max((forceVisibleCounter > 0) ? 1f : 0f, 0.25f + 0.75f * ((hud.map != null) ? hud.map.fade : 0f));
                if (hud.showKarmaFoodRain)
                {
                    num = 1f;
                }
                if (fade < num)
                {
                    fade = Mathf.Min(num, fade + 0.1f);
                    uselessFade = Mathf.Min(num, uselessFade + 0.1f);
                }
                else
                {
                    fade = Mathf.Max(num, fade - 0.1f);
                    uselessFade = Mathf.Max(num, uselessFade - 0.1f);
                }
            }
            else
            {
                if (forceVisibleCounter > 0)
                {
                    forceVisibleCounter--;
                    fade = Mathf.Min(1f, fade + 0.1f);
                    uselessFade = Mathf.Min(1, uselessFade + 0.1f);
                }
                else
                {
                    fade = Mathf.Max(0f, fade - 0.0125f);
                    uselessFade = Mathf.Max(0, uselessFade - 0.012f);
                }
            }

            if (hud.HideGeneralHud)
            {
                fade = 0f;
            }

            color = 0;
            blinkRed = abstractPup.state.alive && hud.owner is Player mainPlayer && mainPlayer.room != null && mainPlayer.room.regionGate != null && pup.room != null && pup.room == mainPlayer.room && BlinkRedPupKarmaMeterGate(mainPlayer.room.regionGate);
            rad = Custom.LerpAndTick(rad, Custom.LerpMap(fade, 0f, 0.15f, 9f, Mathf.Lerp(15f, 13f, pupFoodMeter.deathFade), 1.3f), 0.12f, 0.1f);
            if (pupFoodMeter.notInShelter > 0f)
            {
                if (pupFoodMeter.timeCounter % 40 > 20)
                {
                    rad *= 0.98f;
                }
            }
            else if (pupFoodMeter.PupInDanger && pupFoodMeter.deathFade == 0f)
            {
                if (pupFoodMeter.timeCounter % 20 > 10)
                {
                    rad *= 0.98f;
                    color = 1;
                }
            }
            else if (blinkRed && hud.karmaMeter.timer % 30 > 15)
            {
                if (hud.karmaMeter.timer % 30 < 20)
                {
                    rad *= 0.98f;
                    color = 1;
                }
            }
            karmaSprite.color = karmaColor;
            if (ringSprite != null)
            {
                ringSprite.color = karmaColor;
            }
            glowSprite.color = karmaColor;

            if (reinforceAnimation > -1)
            {
                rad = Custom.LerpMap(fade, 0f, 0.15f, 9f, 15f, 1.3f);
                forceVisibleCounter = Math.Max(forceVisibleCounter, 200);
                reinforceAnimation++;
                if (reinforceAnimation == 20)
                {
                    hud.PlaySound(SoundID.HUD_Karma_Reinforce_Flicker);
                }
                if (reinforceAnimation > 20 && reinforceAnimation < 100)
                {
                    glowyFac = 1f + Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * 0.03f * Mathf.InverseLerp(20f, 100f, reinforceAnimation);
                }
                else if (reinforceAnimation == 104)
                {
                    FadeCircle fadeCircle = new(hud, rad, 9f, 0.68f, 50f, 4f, pos, hud.fContainers[1]);
                    fadeCircle.circle.sprite.color = karmaColor;
                    hud.fadeCircles.Add(fadeCircle);
                    hud.PlaySound(SoundID.HUD_Karma_Reinforce_Small_Circle);
                    hud.PlaySound(SoundID.HUD_Karma_Reinforce_Contract);
                }
                else if (reinforceAnimation > 104 && reinforceAnimation < 130)
                {
                    rad -= Mathf.Pow(Mathf.Sin(Mathf.InverseLerp(104f, 130f, reinforceAnimation) * Mathf.PI), 0.5f) * 2f;
                    fade = 1f - Mathf.Pow(Mathf.Sin(Mathf.InverseLerp(104f, 130f, reinforceAnimation) * Mathf.PI), 0.5f) * 0.5f;
                }
                else if (reinforceAnimation > 130)
                {
                    fade = 1f;
                    rad += Mathf.Sin(Mathf.Pow(Mathf.InverseLerp(130f, 140f, reinforceAnimation), 0.2f) * Mathf.PI) * 5f;
                    if (reinforceAnimation == 134)
                    {
                        glowyFac = 1.7f;
                    }
                    else if (reinforceAnimation == 135)
                    {
                        displayKarma = new IntVector2(karmaState.karma,
                            karmaState.karmaCap);
                        karmaSprite.element = Futile.atlasManager.GetElementWithName(KarmaMeter.KarmaSymbolSprite(true, displayKarma));
                        showAsReinforced = karmaState.reinforcedKarma;
                        FadeCircle fadeCircle = new(hud, rad, 13f, 0.78f, 80f, 8f, pos, hud.fContainers[1]);
                        fadeCircle.circle.sprite.color = karmaColor;
                        hud.fadeCircles.Add(fadeCircle);
                        hud.PlaySound(SoundID.HUD_Karma_Reinforce_Bump);
                        reinforceAnimation = -1;
                    }
                }
            }
        }

        public void UpdateGraphic()
        {
            displayKarma.x = karmaState.karma;
            displayKarma.y = karmaState.karmaCap;
            karmaSprite.element = Futile.atlasManager.GetElementWithName(KarmaMeter.KarmaSymbolSprite(true, displayKarma));
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public override void Draw(float timeStacker)
        {
            float num = Mathf.Lerp(lastFade, fade, timeStacker);
            if (pupFoodMeter.notInShelter > 0f)
            {
                num *= 0.65f + 0.35f * Mathf.Sin((pupFoodMeter.timeCounter + timeStacker) / 20f * Mathf.PI);
            }
            else if (pupFoodMeter.PupInDanger && pupFoodMeter.deathFade == 0f)
            {
                num *= 0.65f + 0.35f * Mathf.Sin((pupFoodMeter.timeCounter + timeStacker) / 20f * Mathf.PI * 2f);
            }
            else if (blinkRed)
            {
                num *= 0.65f + 0.35f * Mathf.Sin((hud.karmaMeter.timer + timeStacker) / 30f * Mathf.PI * 2f);
            }
            Vector2 vector = DrawPos(timeStacker);
            karmaSprite.x = vector.x;
            karmaSprite.y = vector.y;
            karmaSprite.scale = Mathf.Lerp(lastRad, rad, timeStacker) / 22.5f;
            karmaSprite.alpha = num;
            if (showAsReinforced)
            {
                if (ringSprite == null)
                {
                    ringSprite = new FSprite("smallKarmaRingReinforced", true);
                    fContainer.AddChild(ringSprite);
                }
                ringSprite.x = vector.x;
                ringSprite.y = vector.y;
                ringSprite.scale = Mathf.Lerp(lastRad, rad, timeStacker) / 22.5f;
                float num2 = Mathf.InverseLerp(0.1f, 0.85f, pupFoodMeter.forceSleep);
                ringSprite.alpha = num * Mathf.InverseLerp(0.2f, 0f, num2);
                if (num2 > 0f)
                {
                    if (vectorRingSprite == null)
                    {
                        vectorRingSprite = new("Futile_White", true)
                        {
                            shader = hud.rainWorld.Shaders["VectorCircleFadable"]
                        };
                        fContainer.AddChild(vectorRingSprite);
                    }
                    vectorRingSprite.isVisible = true;
                    vectorRingSprite.x = vector.x;
                    vectorRingSprite.y = vector.y;
                    float num3 = Mathf.Lerp(lastRad, rad, timer) + 8f + 30f * Custom.SCurve(Mathf.InverseLerp(0.2f, 1, num2), 0.75f);
                    vectorRingSprite.scale = num3 / 8;
                    float num4 = 2f * Mathf.Pow(Mathf.InverseLerp(0.4f, 0.2f, num2), 2f) + 2f * Mathf.Pow(Mathf.InverseLerp(1, 0.2f, num2), 0.5f);
                    vectorRingSprite.color = new Color(0, 0, num * Mathf.Pow(Mathf.InverseLerp(1, 0.2f, num2), 3), num4 / num3);
                }
                else if (vectorRingSprite != null)
                {
                    vectorRingSprite.RemoveFromContainer();
                    vectorRingSprite = null;
                }
            }
            else
            {
                if (ringSprite != null)
                {
                    ringSprite.RemoveFromContainer();
                    ringSprite = null;
                }
                if (vectorRingSprite != null)
                {
                    vectorRingSprite.RemoveFromContainer();
                    vectorRingSprite = null;
                }
            }
            darkFade.x = DrawPos(timeStacker).x;
            darkFade.y = DrawPos(timeStacker).y;
            darkFade.scaleX = 7.5f;
            darkFade.scaleY = 7.5f;
            darkFade.alpha = 0.2f * Mathf.Pow(num, 2f);
            float num5 = 0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(lastReinforcementCycle, reinforcementCycle, timeStacker));
            float num6 = Mathf.Lerp(lastGlowyFac, glowyFac, timeStacker);
            num5 *= Mathf.InverseLerp(0.9f, 1f, num6);
            glowSprite.x = DrawPos(timeStacker).x;
            glowSprite.y = DrawPos(timeStacker).y;
            glowSprite.scale = Mathf.Lerp((50f + 5f * num5) * num6 / 10f, (50f + 5f * num5) * num6 / 12f, pupFoodMeter.deathFade);
            glowSprite.alpha = (0.2f + 0.2f * num5) * num6 * Mathf.Pow(num, 2f);
            karmaColor = Color.Lerp(Color.Lerp(Custom.FadableVectorCircleColors[color], Custom.HSL2RGB(pup.npcStats.H, Mathf.Lerp(pup.npcStats.S, 1f, 0.8f), pup.npcStats.Dark ? 0.3f : 0.7f), 0.5f - color * 0.5f), new Color(0.6f, 0.6f, 0.6f), pupFoodMeter.deathFade);
        }

        public bool BlinkRedPupKarmaMeterGate(RegionGate gate)
        {
            if (gate.mode != RegionGate.Mode.MiddleClosed || !gate.EnergyEnoughToOpen || gate.unlocked || gate.karmaRequirements
                [gate.letThroughDir ? 0 : 1] == RegionGate.GateRequirement.DemoLock || gate.karmaRequirements
                [gate.letThroughDir ? 0 : 1] == MoreSlugcatsEnums.GateRequirement.RoboLock || gate.karmaRequirements
                [gate.letThroughDir ? 0 : 1] == MoreSlugcatsEnums.GateRequirement.OELock)
            {
                return false;
            }
            int num = gate.DetectZone(abstractPup);
            int var = OptionsMenu.SelectKarmagateMode.Value;
            if (var > 0 && num != 0 && num != 3)
            {
                return var == 1 ? !gate.MeetRequirement : !gate.KarmaGateRequirementPup(karmaState);
            }
            return false;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            darkFade.RemoveFromContainer();
            karmaSprite.RemoveFromContainer();
            ringSprite?.RemoveFromContainer();
            vectorRingSprite?.RemoveFromContainer();
            glowSprite.RemoveFromContainer();
        }

        public static PupKarmaMeter CreatePupKarmaMeter(FoodMeter meter)
        {
            if (!meter.IsPupFoodMeter || !meter.abstractPup.TryGetPupData(out PupData data))
            {
                return null;
            }
            PupKarmaMeter result = new(meter, data);
            meter.hud.AddPart(result);
            return result;
        }
    }
}
