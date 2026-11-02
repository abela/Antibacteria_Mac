import os,sys
root=sys.argv[1]
def patch(rel, pairs):
    p=os.path.join(root,rel); s=open(p,encoding='utf-8').read()
    for a,b in pairs:
        n=s.count(a); assert n==1,(rel,n,a[:70]); s=s.replace(a,b)
    open(p,'w',encoding='utf-8').write(s); print('ok',rel)

patch('UI/Menu/UISkin.cs',[
("        public Sprite[] backdropSprites = new Sprite[0];\n",
 "        public Sprite[] backdropSprites = new Sprite[0];\n        [Tooltip(\"The worm that now and then crawls across the main menu.\")]\n        public Sprite crawlerHead;\n        public Sprite crawlerSegment;\n"),
])
patch('Editor/MenuBuilder.cs',[
("""            skin.backdropSprites = new[] { S("Sprites/enemy_spiky"), S("Sprites/enemy_big"), S("Sprites/snake_head"), S("Sprites/vitamin") }
                .Where(s => s != null).ToArray();
""","""            skin.backdropSprites = new[]
                {
                    S("Sprites/enemy_spiky"), S("Sprites/enemy_big"), S("Sprites/enemy_zigzag"), S("Sprites/vitamin"),
                    S("Sprites/enemy_spiral"), S("Sprites/enemy_gnida"), S("Sprites/enemy_loctopus"), S("Sprites/enemy_zamri"),
                    S("Sprites/enemy_adrenalium"), S("Sprites/enemy_chorux"), S("Sprites/enemy_defender"), S("Sprites/enemy_tentacle"),
                    S("Sprites/enemy_sunux"), S("Sprites/vitamin"),
                }
                .Where(s => s != null).ToArray();
            skin.crawlerHead = S("Sprites/snake_head");
            skin.crawlerSegment = S("Sprites/snake_segment_r2");
"""),
])
patch('Gameplay/Arena.cs',[
("            Current = this;\n            BuildWalls();\n",
 "            Current = this;\n            BuildWalls();\n            if (GetComponent<Ambience.ArenaAmbience>() == null) gameObject.AddComponent<Ambience.ArenaAmbience>(); // background life\n"),
])
patch('Editor/GameDataSettings.cs',[
("using AntiBacteria.Gameplay.Modes;\n", "using AntiBacteria.Gameplay.Modes;\nusing AntiBacteria.Gameplay.Ambience;\n"),
("        public static ModeSettings Modes() =>",
 """        public static AmbienceSettings AmbienceCfg() =>
            GetOrCreate<AmbienceSettings>(Resources + "/AmbienceSettings.asset", AmbienceSettings.Use, a => a.material == null, FillAmbienceArt);

        public static void FillAmbienceArt(AmbienceSettings s)
        {
            Undo.RecordObject(s, "Link Ambience Art");
            s.material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/PaletteSprite.mat");
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssetIfDirty(s);
            // The cilia read the floor's pixels to find the inner edge of the walls.
            var floor = AssetImporter.GetAtPath(Art + "/Sprites/arena_floor.png") as TextureImporter;
            if (floor != null && !floor.isReadable) { floor.isReadable = true; floor.SaveAndReimport(); }
        }

        [SettingsProvider]
        public static SettingsProvider AmbiencePage() => Page(
            "Project/AntiBacteria/Ambience", "Ambience",
            "Background life: drifting specks, microbes, pulse rings and wall cilia in the arena, and the main menu backdrop. " +
            "Pure decoration. Arena changes apply when a level starts.",
            AmbienceCfg, new HashSet<string> { "ambience", "background", "specks", "dust", "microbes", "cilia", "menu", "backdrop", "life" },
            s => { if (GUILayout.Button("Re-link material / make the floor readable")) FillAmbienceArt(s); });

        [MenuItem("AntiBacteria/Settings/Ambience...")]
        private static void OpenAmbience() => SettingsService.OpenProjectSettings("Project/AntiBacteria/Ambience");

        public static ModeSettings Modes() =>"""),
])
