﻿using BepInEx;
using BepInEx.Logging;
using PupKarma.Hooks;
using System;
namespace PupKarma
{
    [BepInPlugin(PLUGIN_GUID, PLUGN_NAME, PLUGIN_VERSION)]
    public class PupKarmaMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "quansly.pupkarma";

        public const string PLUGN_NAME = "Pup Karma";

        public const string PLUGIN_VERSION = "1.5.1.1";

        public static bool Pearlcat;

        internal static ManualLogSource LoggerPupKarma;

        internal static OptionsMenu options;

        public PupKarmaMain()
        {
            options = new(this);
        }

        public void OnEnable()
        {
            LoggerPupKarma = Logger;
            HUDHooks.Init();
            SaveHooks.Init();
            GameHooks.Init();
            On.RainWorld.OnModsInit += Hook_On_Mods_Init;
            On.RainWorld.PostModsInit += Hook_RainWorld_PostModsInit;
            Logger.LogInfo("Pup karma enable");
        }

        public void Hook_On_Mods_Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                MachineConnector.SetRegisteredOI("quansly.pupkarma", options);
                SSPupKarmaSubBehaviour.RegisterValues();
            }
            catch (Exception ex)
            {
                Logger.LogError("Pupkarma can't load OnModsInit.");
                Logger.LogError(ex);
                throw;
            }
        }

        public void Hook_RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    switch (mod.id)
                    {
                        case "slime-cubed.devconsole":
                            OtherPlugins.DevConsole.DevConsoleInit();
                            break;
                        case "Antoneeee.PupAi":
                            OtherPlugins.InitILPupAI();
                            break;
                        case "yeliah.slugpupFieldtrip":
                            OtherPlugins.InitILSlugpupSafari();
                            break;
                        case "regionkit":
                            RegionKitStuff.InitHookReginKit_BigKarmaShrine_Update();
                            break;
                        case "myr.chasing_wind":
                            OtherPlugins.CWHooks.Init();
                            break;
                        case "iwantbread.slugpupstuff":
                            OtherPlugins.RegisterPupsPlus();
                            break;
                        case "pearlcat":
                            Pearlcat = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Pupkarma can't load PostModsInit.");
                Logger.LogError(ex);
                throw;
            }
        }
    }
}
