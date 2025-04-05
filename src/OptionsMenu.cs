using Menu.Remix.MixedUI;
using UnityEngine;

namespace PupKarma
{
    internal class OptionsMenu : OptionInterface
    {
        public Color cheatColor = new(0.85f, 0.35f, 0.4f);

        public static Configurable<int> SelectKarmagateMode;

        public static Configurable<int> SelectDistributionKarmaReinforce;

        public static Configurable<int> SelectReqToSpawnGhost;

        public static Configurable<bool> SpawnKarmaFlowers;

        public static Configurable<bool> AssignKarmaPlayer;

        public static Configurable<bool> HideKarmaMeter;

        public static Configurable<bool> RespawnPup;

        public static Configurable<bool> ReturnPupInShelterAfterSave;

        public static Configurable<bool> PersonalityKarma;

        public static Configurable<bool> IgnoreGuardianKarma;

        public static Configurable<bool> SpawnFlowersWithASimpleDistribution;

        public static Configurable<bool> LightUpPKM;

        public static Configurable<bool> DontPickUpPlayerFlower;
        
        public void CreateCheckBoxNLabel(OpTab tabAdder, Configurable<bool> configCheckBox, float posX, float posY, string labelText, string buttonDesc, bool isCheats = false)
        {
            if(isCheats)
            {
                tabAdder.AddItems(new UIelement[]
                {
                    new OpCheckBox(configCheckBox, posX, posY)
                    {
                        description = buttonDesc,
                        colorEdge = cheatColor
                    },
                    new OpLabel(posX + 31, posY + 3, labelText)
                    {
                        color = cheatColor
                    }
                });
                return;
            }
            tabAdder.AddItems(new UIelement[]
            {
                new OpCheckBox(configCheckBox, posX, posY)
                {
                    description = buttonDesc
                },
                new OpLabel(posX + 31, posY + 3, labelText)
            });
        }

        public void CreateCheckBoxNLabel(OpTab tabAdder, OpCheckBox checkbox, string labelText, bool isCheats = false)
        {
            if (isCheats)
            {
                tabAdder.AddItems(new UIelement[]
                {
                    checkbox,
                    new OpLabel(checkbox.pos.x + 31, checkbox.pos.y + 3, labelText)
                    {
                        color = cheatColor
                    }
                });
                return;
            }
            tabAdder.AddItems(new UIelement[]
            {
                checkbox,
                new OpLabel(checkbox.pos.x + 31, checkbox.pos.y + 3, labelText)
            });
        }

        public OpLabel CreateLabelForRadioButtons(OpRadioButtonGroup radioGroup, int numButton, string text)
        {
            return new OpLabel(radioGroup.buttons[numButton].pos.x + radioGroup.buttons[numButton].size.x + 7, radioGroup.buttons[numButton].pos.y + 3, text);
        }

        public OptionsMenu(PupKarmaMain plugin) 
        {
            SelectKarmagateMode = config.Bind("pupkarma_select_karmagate_mode", 3);
            SelectDistributionKarmaReinforce = config.Bind("pupkarma_select_reinforce_influence", 3);
            SelectReqToSpawnGhost = config.Bind("pupkarma_select_req_to_spawn_ghosts", 0);
            SpawnKarmaFlowers = config.Bind("pupkarma_can_pups_spawn_flowers", true);
            AssignKarmaPlayer = config.Bind("give_pups_player_karma", false);
            HideKarmaMeter = config.Bind("hide_pup_karma_meters", false);
            RespawnPup = config.Bind("respawn_pup", false);
            ReturnPupInShelterAfterSave = config.Bind("return_pup_in_shelter", false);
            PersonalityKarma = config.Bind("use_personality_karma", false);
            IgnoreGuardianKarma = config.Bind("ignore_guardian_karma", false);
            SpawnFlowersWithASimpleDistribution = config.Bind("cheat_spawn_flowers", false);
            LightUpPKM = config.Bind("light_up_pup_karma_meter", true);
            DontPickUpPlayerFlower = config.Bind("do_not_pick_up_player_flower", true);
        }

        public override void Initialize()
        {
            OpTab options = new OpTab(this, "Options");
            OpTab cheats = new OpTab(this, "Cheats")
            {
                colorButton = cheatColor
            };

            Tabs = new OpTab[]
            {
                options,
                cheats
            };
            OpRadioButtonGroup gateButtons = new OpRadioButtonGroup(SelectKarmagateMode);
            OpRadioButtonGroup reinforceButtons = new OpRadioButtonGroup(SelectDistributionKarmaReinforce);
            OpRadioButtonGroup ghostSpawnReq = new OpRadioButtonGroup(SelectReqToSpawnGhost);
            options.AddItems(new UIelement[]
            {
                gateButtons,
                reinforceButtons,
                ghostSpawnReq
            });
            gateButtons.SetButtons(new OpRadioButton[]
            {
                new OpRadioButton(30, 510)
                {
                    description = "When switching between regions, slugpups karma will not be taken into account."
                },
                new OpRadioButton(230, 510)
                {
                    description = "When switching between regions, only the appropriate karma of the player or slugpup will be taken into account."
                },
                new OpRadioButton(430, 510)
                {
                    description = "When switching between regions, the karma of each slugpups will be taken into account."
                }
            });
            reinforceButtons.SetButtons(new OpRadioButton[]
            {
                new OpRadioButton(30, 400)
                {
                    description = "When a karma flower is eaten by a slugpup, karma reinforce is given to that slugpup and the player."
                },
                new OpRadioButton(230, 400)
                {
                    description = "When a karma flower is eaten by a slugpup or a player, karma reinforce is given to the player and all slugpups."
                },
                new OpRadioButton(430, 400)
                {
                    description = "When eating a karma flower by a slugpup, karma reinforce is given only to that slugpup."
                }
            });
            ghostSpawnReq.SetButtons(new OpRadioButton[]
            {
                new OpRadioButton(30, 290)
                {
                    description = "The karma of slugpups does not affect the spawn of echoes in any way."
                },
                new OpRadioButton(230, 290)
                {
                    description = "One slugpup with a suitable karma level is enough to spawn an echo."
                },
                new OpRadioButton(430, 290)
                {
                    description = "To spawn an echo, all slugpups and the player must have a suitable karma level."
                }
            });
            float opY = 220f;

            options.AddItems(
            [
                new OpLabel(70, 550, "The mode of passing through the karma gate", true),
                CreateLabelForRadioButtons(gateButtons, 0, "No influence"),
                CreateLabelForRadioButtons(gateButtons, 1, "Simplified passage"),
                CreateLabelForRadioButtons(gateButtons, 2, "Complicated passage"),

                new OpLabel(167, 440, "Distribution karma reinforce", true),
                CreateLabelForRadioButtons(reinforceButtons, 0, "Standart distribution"),
                CreateLabelForRadioButtons(reinforceButtons, 1, "Simplified distribution"),
                CreateLabelForRadioButtons(reinforceButtons, 2, "Complicated distribution"),

                new OpLabel (115, 330, "Requirements for the spawn of echoes", true),
                CreateLabelForRadioButtons(ghostSpawnReq, 0, "No influence"),
                CreateLabelForRadioButtons(ghostSpawnReq, 1, "Simplified echo spawn"),
                CreateLabelForRadioButtons(ghostSpawnReq, 2, "Complicated echo spawn"),

                new OpLabel(240, opY, "Other Options", true)
            ]);
            OpCheckBox hidePKMButton = new OpCheckBox(HideKarmaMeter, 330f, opY - 30f);
            OverrideOpCheckBox lightUpPKMButton = new OverrideOpCheckBox(LightUpPKM, 330f, opY - 65f, hidePKMButton)
            {
                description = "Lights up pup karma meter when pup holds karma flower"
            };
            CreateCheckBoxNLabel(options, SpawnKarmaFlowers, 30f, opY - 30f, "Pups spawn karma flowers", "Slugpups leave karma flowers after death if their karma is rainforced, also karma flowers appear if the player died, but slugpups had reinforced karma.\n(works only with a standard and complicated reinforce distribution)");
            CreateCheckBoxNLabel(options, AssignKarmaPlayer, 30f, opY - 65f, "Pups have player's karma", "Slugpups have the karma and karma cap of the player. Unlike the \"Pups assgning karma cap\" feature, it does not matter that the karma cap level may be lower than 5.\nAlso, if these two features are enabled, the game will give preference to this feature.");
            CreateCheckBoxNLabel(options, PersonalityKarma, 30f, opY - 100f, "Personality pup karma", "Pups will use their personal qualities to assign karma when they appear.");
            CreateCheckBoxNLabel(options, hidePKMButton, "Hide pup karma meters");
            CreateCheckBoxNLabel(options, lightUpPKMButton, "Light up pup karma meter");
            CreateCheckBoxNLabel(options, DontPickUpPlayerFlower, 330f, opY - 100f, "Don't pick up player spawned karma flower", "Slugpups will not pick up the karma flower that appeared at the player's death site (slugpups will want to eat it if you give it to them)");

            cheats.AddItems(
            [
                new OpLabel(267, 450, "Cheats", true)
                {
                    color = cheatColor
                }
            ]);

            OpCheckBox respawnButton = new OpCheckBox(RespawnPup, 30, 400)
            {
                colorEdge = cheatColor,
                description = "Saved dead pups will be revived for the next cycle at the cost of lowering karma."
            };
            OverrideOpCheckBox returnButton = new OverrideOpCheckBox(ReturnPupInShelterAfterSave, 30, 365, respawnButton, true)
            {
                colorEdge = cheatColor,
                description = "Returns saved slugpups to the shelter after the cycle ends."
            };

            CreateCheckBoxNLabel(cheats, respawnButton, "Revive dead pups", true);
            CreateCheckBoxNLabel(cheats, returnButton, "Returning pups", true);
            CreateCheckBoxNLabel(cheats, IgnoreGuardianKarma, 30, 330, "Guardians ignore slugpups", "Guardians ignore slugpups with inappropriate karma.", true);
            CreateCheckBoxNLabel(cheats, SpawnFlowersWithASimpleDistribution, 30, 295, "Pup flowers in simplified distribution", "Karma flowers of slugpups can appear with a simplified distribution of reinforce.", true);
        }
    }
}
