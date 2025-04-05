using HUD;
using UnityEngine;

namespace PupKarma.Hooks
{
    internal class HUDHooks
    {
        public static void Init()
        {
            On.HUD.FoodMeter.GameUpdate += Hook_On_FoodMeter_GameUpdate;
        }

        private static void Hook_On_FoodMeter_GameUpdate(On.HUD.FoodMeter.orig_GameUpdate orig, FoodMeter self)
        {
            orig(self);
            if (!OptionsMenu.HideKarmaMeter.Value && self.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && self.TryGetPupKarmaMeter(out PupKarmaMeter pupKarmaMeter))
            {
                pupKarmaMeter.foodMeterYPos = self.pos.y;
                self.pos.y = Mathf.Lerp(pupKarmaMeter.foodMeterYPos, pupKarmaMeter.pos.y, pupKarmaMeter.uselessFade);
            }
        }
    }
}
