using Menu;

namespace PupKarma
{
    public class PupKarmaEnums
    {
        public static void RegisterValues()
        {
            PupScenesID.RegisterValues();
            SSBehaviourID.RegisterValues();
            SSActions.RegisterValues();
        }

        public static class SSBehaviourID
        {
            public static SSOracleBehavior.SubBehavior.SubBehavID IncreasePupKarmaCap;

            public static void RegisterValues()
            {
                IncreasePupKarmaCap = new("IncreasePupKarma", true);
            }

            public static void UnregisterValues()
            {
                IncreasePupKarmaCap?.Unregister();
                IncreasePupKarmaCap = null;
            }
        }

        public static class SSActions
        {
            public static SSOracleBehavior.Action TalkingAboutSlugpups;

            public static SSOracleBehavior.Action IncraseKarmaToSlugpups;

            public static void RegisterValues()
            {
                TalkingAboutSlugpups = new("TalkingAboutSlugpups", true);
                IncraseKarmaToSlugpups = new("IncraseKarmaToSlugpups", true);
            }

            public static void UnregiterValues()
            {
                TalkingAboutSlugpups?.Unregister();
                TalkingAboutSlugpups = null;

                IncraseKarmaToSlugpups?.Unregister();
                IncraseKarmaToSlugpups = null;
            }
        }

        public static class PupScenesID
        {
            public static MenuScene.SceneID PupKarma_White_Ghost_Pup;

            public static MenuScene.SceneID PupKarma_Red_Ghost_Pup;

            public static MenuScene.SceneID PupKarma_Gourmand_Ghost_Pup;

            public static void RegisterValues()
            {
                PupKarma_White_Ghost_Pup = new("PupKarma_White_Ghost_Pup", true);
                PupKarma_Red_Ghost_Pup = new("PupKarma_Red_Ghost_Pup", true);
                PupKarma_Gourmand_Ghost_Pup = new("PupKarma_Gourmand_Ghost_Pup", true);
            }
        }
    }
}
