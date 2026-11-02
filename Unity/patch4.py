import os,sys
S=sys.argv[1]
def patch(rel, pairs):
    p=os.path.join(S,rel); s=open(p,encoding='utf-8').read()
    for a,b in pairs:
        n=s.count(a); assert n==1,(rel,n,a[:70]); s=s.replace(a,b)
    open(p,'w',encoding='utf-8').write(s); print('ok',rel)
patch('Core/Worlds.cs',[
("        /// <summary>Levels of the world cleared on the given difficulty.</summary>\n",
 "        /// <summary>Levels of the world cleared on any difficulty.</summary>\n"
 "        public static int ClearedOnAny(int world)\n        {\n            int n = 0;\n"
 "            for (int i = 0; i < LevelsPerWorld; i++) if (Progress.IsClearedOnAny(FirstLevel(world) + i)) n++;\n"
 "            return n;\n        }\n\n"
 "        /// <summary>Levels of the world cleared on the given difficulty.</summary>\n"),
])
patch('UI/Menu/WorldMap.cs',[
("    /// States: locked (dim), open (outline), cleared on the chosen difficulty (filled).\n",
 "    /// States: locked (dim), open (outline), cleared on any difficulty (filled).\n"),
("            Difficulty d = GameSettings.Difficulty;\n            int first = Worlds.FirstLevel(world);\n",
 "            int first = Worlds.FirstLevel(world);\n"),
("                bool cleared = open && Progress.IsCleared(level, d);\n",
 "                bool cleared = open && Progress.IsClearedOnAny(level); // difficulty is picked after the level\n"),
])
patch('UI/Menus/MainMenuController.cs',[
("        private GameMode pendingMode = GameMode.CaptureFlag; // the endless mode whose difficulty is being chosen\n",
 "        // What the difficulty screen will start: an endless mode, or (Campaign) the level picked on the map.\n"
 "        private GameMode pendingMode = GameMode.CaptureFlag;\n        private int pendingLevel = 1;\n"),
("            levelSelect = LevelSelectMenu.Build(skin, root, navigator);\n",
 "            levelSelect = LevelSelectMenu.Build(skin, root, navigator, level =>\n"
 "            {\n                pendingMode = GameMode.Campaign;\n                pendingLevel = level;\n                navigator.Push(difficulty);\n            });\n"),
("            LiveText.Create(skin, t, \"Title\", 160f, 22f, () => GameplayMenuController.ModeName(pendingMode).ToUpperInvariant(), role: PaletteRole.Accent);\n",
 "            bool campaign() => pendingMode == GameMode.Campaign;\n"
 "            LiveText.Create(skin, t, \"Title\", 160f, 22f, () => campaign()\n"
 "                ? LevelSelectMenu.Title(pendingLevel, true)\n"
 "                : GameplayMenuController.ModeName(pendingMode).ToUpperInvariant(), role: PaletteRole.Accent);\n"),
("            LiveText.Create(skin, t, \"Subtitle\", 160f, 44f, () => Loc.Get(WordIDs.Difficulty), dim: true);\n",
 "            // Campaign: the world's name and \"Difficulty\"; endless modes: \"Difficulty\".\n"
 "            LiveText.Create(skin, t, \"Subtitle\", 160f, 44f, () => campaign()\n"
 "                ? Worlds.Name(Worlds.Of(pendingLevel)) + \"  \" + Loc.Get(WordIDs.Difficulty)\n"
 "                : Loc.Get(WordIDs.Difficulty), dim: true);\n"),
("                    GameSettings.SetDifficulty(d);\n                    SceneFlow.LoadMode(pendingMode);\n",
 "                    GameSettings.SetDifficulty(d);\n"
 "                    if (campaign()) SceneFlow.LoadLevel(pendingLevel);\n                    else SceneFlow.LoadMode(pendingMode);\n"),
("                    int best = Progress.ModeBest(pendingMode, d);\n",
 "                    int best = campaign() ? Progress.Best(pendingLevel, d) : Progress.ModeBest(pendingMode, d);\n"),
])
