using MoreSlugcats;

namespace PupKarma
{
    internal class SSPupKarmaSubBehaviour : SSOracleBehavior.ConversationBehavior
    {
        public bool finishedFlag;
        public SSPupKarmaSubBehaviour(SSOracleBehavior owner) : base(owner, SSBehaviourIDs.IncreasePupKarmaCap, Conversation.ID.None)
        {
            owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            owner.getToWorking = 0;
            owner.LockShortcuts();
            owner.TurnOffSSMusic(true);
        }

        public override void Update()
        {
            base.Update();
            if (finishedFlag)
            {
                return;
            }
            if (action == SSActions.TalkingAboutSlugpups)
            {
                if (owner.inActionCounter == 120)
                {
                    dialogBox.NewMessage("Greetings, citizen.", 0);
                }
                if (owner.inActionCounter == 250)
                {
                    dialogBox.NewMessage("I noticed that your friends have some problems.", 0);
                }
                if (owner.inActionCounter == 380)
                {
                    dialogBox.NewMessage("I can fix them, so let's get started.", 0);
                    owner.NewAction(SSActions.IncraseKarmaToSlugpups);
                }
            }
            else if (action == SSActions.IncraseKarmaToSlugpups)
            {
                if (owner.inActionCounter == 160)
                {
                    for (int i = 0; i < owner.oracle.room.abstractRoom.creatures.Count; i++)
                    {
                        AbstractCreature crit = owner.oracle.room.abstractRoom.creatures[i];
                        if (crit.TryGetPupData(out PupData data))
                        {
                            data.VisitIteratorAndIncreaseKarma(oracle);
                        }
                    }
                    owner.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);                
                }
                if (owner.inActionCounter == 200)
                {
                    dialogBox.NewMessage("Done", 0);
                }
                if (owner.inActionCounter == 300)
                {
                    owner.getToWorking = 1;
                    dialogBox.NewMessage("Well, if you don't have anything else interesting, then you can go.", 0);
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
                    owner.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            owner.UnlockShortcuts();
            finishedFlag = true;
        }

        public static void RegisterValues()
        {
            SSBehaviourIDs.RegisterValues();
            SSActions.RegisterValues();
        }

        public static class SSBehaviourIDs
        {
            public static SubBehavID IncreasePupKarmaCap;

            public static void RegisterValues()
            {
                IncreasePupKarmaCap = new SubBehavID("IncreasePupKarma", true);
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
                TalkingAboutSlugpups = new SSOracleBehavior.Action("TalkingAboutSlugpups", true);
                IncraseKarmaToSlugpups = new SSOracleBehavior.Action("IncraseKarmaToSlugpups", true);
            }

            public static void UnregiterValues()
            {
                TalkingAboutSlugpups?.Unregister();
                TalkingAboutSlugpups = null;

                IncraseKarmaToSlugpups?.Unregister();
                IncraseKarmaToSlugpups = null;
            }
        }
    }
}
