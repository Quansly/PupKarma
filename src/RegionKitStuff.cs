using MonoMod.RuntimeDetour;
using RegionKit.Modules.Objects;
using System;

namespace PupKarma
{
    internal static class RegionKitStuff
    {
        public static void InitHookReginKit_BigKarmaShrine_Update()
        {
            new Hook(typeof(BigKarmaShrine).GetMethod(nameof(BigKarmaShrine.Update)), new Action<Action<BigKarmaShrine, bool>, BigKarmaShrine, bool>((Action<BigKarmaShrine, bool> orig, BigKarmaShrine self, bool eu) =>
            {
                orig(self, eu);
                try
                {
                    if (self.room.game.session is StoryGameSession)
                    {
                        AbstractRoom room = self.room.abstractRoom;
                        for (int i = 0; i < room.creatures.Count; i++)
                        {
                            if (room.creatures[i].IsSlugpup() && room.creatures[i].TryGetPupData(out PupData data) && self.PupActivate(data))
                            {
                                if (self.SetKarma != -1)
                                {
                                    data.karma = self.SetKarma;
                                }
                                if (self.SetKarmaCap != -1)
                                {
                                    data.karmaCap = self.SetKarmaCap;
                                }
                                if (data.pupKarmaMeter != null)
                                {
                                    data.pupKarmaMeter.reinforceAnimation = 1;
                                }
                                data.voidKarmaUp = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }));
        }

        public static bool KarmaPupsRequirement(this BigKarmaShrine self, PupData data)
        {
            return data.karmaCap >= self.ReqKarmaCap && data.karma >= self.ReqKarma;
        }

        public static bool PupActivate(this BigKarmaShrine self, PupData data)
        {
            return data.pup.realizedCreature != null && data.pup.realizedCreature.room == self.room && self.KarmaPupsRequirement(data) && self.PosRequirement(data.pup.realizedCreature.firstChunk.pos - self.pObj.pos) && data.voidKarmaUp;
        }
    }
}
