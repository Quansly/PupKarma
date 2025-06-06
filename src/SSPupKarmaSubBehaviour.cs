using MoreSlugcats;

namespace PupKarma
{
    internal class SSPupKarmaSubBehaviour : SSOracleBehavior.ConversationBehavior
    {
        public bool finishedFlag;
        public SSPupKarmaSubBehaviour(SSOracleBehavior owner) : base(owner, PupKarmaEnums.SSBehaviourID.IncreasePupKarmaCap, Conversation.ID.None)
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
            if (action == PupKarmaEnums.SSActions.TalkingAboutSlugpups)
            {
                if (owner.inActionCounter == 120)
                {
                    dialogBox.NewMessage("Greetings, citizen.", 0);
                }
                if (owner.inActionCounter == 250)
                {
                    dialogBox.NewMessage("I see you've found new friends. They seem to have some problems.", 0);
                }
                if (owner.inActionCounter == 380)
                {
                    dialogBox.NewMessage("I can help them, so let's get started.", 0);
                    owner.NewAction(PupKarmaEnums.SSActions.IncraseKarmaToSlugpups);
                }
            }
            else if (action == PupKarmaEnums.SSActions.IncraseKarmaToSlugpups)
            {
                if (owner.inActionCounter == 160)
                {
                    for (int i = 0; i < oracle.room.abstractRoom.creatures.Count; i++)
                    {
                        AbstractCreature crit = oracle.room.abstractRoom.creatures[i];
                        if (crit.TryGetPupData(out PupData data))
                        {
                            data.VisitIteratorAndIncreaseKarma(oracle.ID);
                        }
                    }
                    oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);                
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
    }
}
