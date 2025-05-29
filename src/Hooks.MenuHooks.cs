using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PupKarma.Hooks
{
    internal class MenuHooks
    {
        public static void Init()
        {
            PupScenesID.RegisterValues();
            IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        }

        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);
            string scenePath = $"Scenes{Path.DirectorySeparatorChar}";
            if (self.sceneID.value.Contains("PupKarma"))
            {
                string folderScenePath = $"{scenePath}{self.sceneID}";
                int pups = Mathf.Clamp(self.menu.manager.rainWorld.progression.miscProgressionData.GetMiscProgDataExt().slugcatAscendedPups[(self.owner as SlugcatSelectMenu.SlugcatPage).slugcatNumber], 1, 2);
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, folderScenePath, "flat_" + pups, new(0, 0), false, false));
                    return;
                }
                SceneInfo sceneInfo = new(File.ReadAllLines(AssetManager.ResolveFilePath($"{folderScenePath}{Path.DirectorySeparatorChar}sceneinfo.txt")));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, scenePath + sceneInfo.bkgPath, sceneInfo.bkgName, sceneInfo.bkgPos, 4.5f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderScenePath, "ghost_a_" + pups, sceneInfo.aPos, 2.85f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderScenePath, "ghost_b_" + pups, sceneInfo.bPos, 2.7f, MenuDepthIllustration.MenuShader.Overlay));
                (self as InteractiveMenuScene).idleDepths.Add(3.1f);
                (self as InteractiveMenuScene).idleDepths.Add(2.8f);
            }
        }

        private static void SlugcatPage_AddImage(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MenuObject>("menu"));
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate((SlugcatSelectMenu.SlugcatPage page, MenuScene.SceneID sceneID) =>
                {
                    if (page.menu.manager.rainWorld.progression.miscProgressionData.GetMiscProgDataExt().slugcatAscendedPups.ContainsKey(page.slugcatNumber) && page.menu.manager.rainWorld.progression.miscProgressionData.GetMiscProgDataExt().slugcatAscendedPups[page.slugcatNumber] > 0)
                    {
                        if (sceneID == MenuScene.SceneID.Ghost_White)
                        {
                            sceneID = PupScenesID.PupKarma_White_Ghost_Pup;
                        }
                        else if (sceneID == MenuScene.SceneID.Ghost_Red)
                        {
                            sceneID = PupScenesID.PupKarma_Red_Ghost_Pup;
                        }
                        else if (sceneID == MoreSlugcatsEnums.MenuSceneID.End_Gourmand)
                        {
                            sceneID = PupScenesID.PupKarma_Gourmand_Ghost_Pup;
                        }
                    }
                    return sceneID;
                });
                c.Emit(OpCodes.Stloc_0);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private class SceneInfo
        {
            public Vector2 bkgPos;

            public Vector2 aPos;

            public Vector2 bPos;

            public string bkgPath;

            public string bkgName;

            public SceneInfo(string[] lines)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] splt = Regex.Split(lines[i], ": ");
                    switch (splt[0])
                    {
                        case "bkg":
                            bkgPos = VecFormString(splt[1]);
                            break;
                        case "a":
                            aPos = VecFormString(splt[1]);
                            break;
                        case "b":
                            bPos = VecFormString(splt[1]);
                            break;
                        case "bkg_path":
                            bkgPath = splt[1];
                            break;
                        case "bkg_name":
                            bkgName = splt[1];
                            break;
                    }
                }
            }

            Vector2 VecFormString(string str)
            {
                string[] pos = Regex.Split(str, ", ");
                return new Vector2(float.Parse(pos[0]), float.Parse(pos[1]));
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
