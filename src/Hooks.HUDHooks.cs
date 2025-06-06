using HUD;
using System.Collections.Generic;
using UnityEngine;

namespace PupKarma.Hooks
{
    internal class HUDHooks
    {
        public static void Init()
        {
            On.HUD.FoodMeter.GameUpdate += Hook_On_FoodMeter_GameUpdate;
            On.HUD.FoodMeter.TrySpawnPupBars += FoodMeter_TrySpawnPupBars;
        }

        private static void FoodMeter_TrySpawnPupBars(On.HUD.FoodMeter.orig_TrySpawnPupBars orig, FoodMeter self)
        {
            orig(self);
            Player hudPlayer = self.hud.owner as Player;
            var storySessionExt = hudPlayer.abstractCreature.world.game.GetStorySession.GetStorySessionExt();
            if (!self.IsPupFoodMeter && self.pupBars != null && storySessionExt.pupsWantsFoodBars != null)
            {
                List<AbstractCreature> clone = [.. storySessionExt.pupsWantsFoodBars];
                foreach (var crit in clone)
                {
                    if (crit.world != hudPlayer.abstractCreature.world)
                    {
                        storySessionExt.pupsWantsFoodBars.Remove(crit);
                        continue;
                    }
                    if (crit.realizedCreature != null)
                    {
                        int num = self.pupBars.Count + 1;
                        FoodMeter pupFoodMeter = new(self.hud, 0, 0, crit.realizedCreature as Player, num);
                        self.hud.parts.Add(pupFoodMeter);
                        self.pupBars.Add(pupFoodMeter);
                        storySessionExt.pupsWantsFoodBars.Remove(crit);
                    }
                }
                if (storySessionExt.pupsWantsFoodBars.Count == 0)
                {
                    storySessionExt.pupsWantsFoodBars = null;
                }
            }
        }

        private static void Hook_On_FoodMeter_GameUpdate(On.HUD.FoodMeter.orig_GameUpdate orig, FoodMeter self)
        {
            orig(self);
            if (!OptionsMenu.HideKarmaMeter.Value && self.hud.owner is Player && self.TryGetPupKarmaMeter(out PupKarmaMeter pupKarmaMeter))
            {
                pupKarmaMeter.foodMeterYPos = self.pos.y;
                self.pos.y = Mathf.Lerp(pupKarmaMeter.foodMeterYPos, pupKarmaMeter.pos.y, pupKarmaMeter.uselessFade);
            }
        }
    }
}
