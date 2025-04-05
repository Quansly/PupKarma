using System.Collections.Generic;
using UnityEngine;

namespace PupKarma
{
    public struct PupClassKarmaInfo
    {
        private static Dictionary<SlugcatStats.Name, PupClassKarmaInfo> registeredPups = [];

        public int minKarma;

        public int minKarmaCap;

        public bool iteratorKarmaAsHunter;

        public PupClassKarmaInfo(bool iteratorKarmaAsHunter, int minKarma, int minKarmaCap)
        {
            this.iteratorKarmaAsHunter = iteratorKarmaAsHunter;
            this.minKarmaCap = Mathf.Clamp(minKarmaCap, 0, 9);
            this.minKarma = Mathf.Clamp(minKarma, 0, this.minKarmaCap);
        }

        public static void RegisterSlugpup(SlugcatStats.Name pupClass, bool iteratorKarmaAsHunter = false, int minKarma = 0, int minKarmaCap = 4)
        {
            if (!registeredPups.ContainsKey(pupClass))
            {
                registeredPups.Add(pupClass, new(iteratorKarmaAsHunter, minKarma, minKarmaCap));
                Logger.Debug($"Slugpup {pupClass} has been registered");
            }
        }

        public static void UnregisterSlugpup(SlugcatStats.Name pupClass)
        {
            if (registeredPups.Remove(pupClass)) Logger.Debug($"Slugpup {pupClass} has been unregistred");
        }

        public static PupClassKarmaInfo GetPupKarmaInfo(SlugcatStats.Name pupClass)
        {
            if (registeredPups.TryGetValue(pupClass, out PupClassKarmaInfo result))
            {
                return result;
            }
            return new(false, 0, 4);
        }
    }
}
