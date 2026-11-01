using AntiBacteria.Core;
using AntiBacteria.Gameplay;
using AntiBacteria.Gameplay.Effects;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Pickups;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Rendering;
using AntiBacteria.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Builds the vertical slice from scratch: physics layers + collision matrix, prefabs (player, shot,
    /// BlueCorosia, vitamin, kill pop), data (enemy definition, catalog, 50 levels) and the Gameplay scene
    /// (arena, player, systems, HUD). Re-run after changing the builder; it replaces what it built before.
    /// </summary>
    public static class SliceBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Gameplay.unity";
        private const string PrefabDir = Root + "/Prefabs";
        private const string EnemyDataDir = Root + "/Data/Enemies";

        [MenuItem("AntiBacteria/Build/Vertical Slice")]
        public static void BuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            var log = new System.Text.StringBuilder("SliceBuilder:\n");
            EnsureLayers(log);
            ConfigurePhysics();

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/PaletteSprite.mat");
            EnsureFolder(PrefabDir); EnsureFolder(EnemyDataDir);

            // ---------------- prefabs
            GameObject shot = SavePrefab(BuildShot(mat), "PlayerShot");
            GameObject vitamin = SavePrefab(BuildVitamin(mat), "Vitamin");
            GameObject pop = SavePrefab(BuildPop(mat), "FxPop");
            GameObject blue = SavePrefab(BuildEnemy(mat, "Enemy_BlueCorosia", "enemy_spiky", 6.4f), "Enemy_BlueCorosia");
            GameObject player = SavePrefab(BuildPlayer(mat, shot.GetComponent<PlayerShot>()), "Player");

            // ---------------- data
            var blueDef = LoadOrCreate<EnemyDefinition>(EnemyDataDir + "/00_BlueCorosia.asset");
            blueDef.legacyId = 0; blueDef.displayName = "BlueCorosia";
            blueDef.prefab = blue.GetComponent<Enemy>();
            blueDef.movement = EnemyMovement.Wander;
            blueDef.speedPoints = 8f * Legacy.PointsPerMetre; // 8 m/s (BlueCorosia.mm:28)
            blueDef.arriveDistancePoints = 10f;
            blueDef.spinDegreesPerSecond = 300f;               // 90 deg / 0.3 s (BlueCorosia.mm:86)
            blueDef.hits = 1; blueDef.fadeInSeconds = 0.1f; blueDef.killScore = 50; blueDef.vitamins = 2;
            EditorUtility.SetDirty(blueDef);

            var catalog = LoadOrCreate<EnemyCatalog>(Root + "/Data/EnemyCatalog.asset");
            var cso = new SerializedObject(catalog);
            var list = cso.FindProperty("enemies");
            list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = blueDef;
            cso.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine(LevelImporter.ImportAll());
            var level1 = AssetDatabase.LoadAssetAtPath<LevelData>(LevelImporter.AssetPath(1));
            AssetDatabase.SaveAssets();

            // ---------------- scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (string n in new[] { "Arena", "Player", "Systems", "HUD" })
            {
                GameObject old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }

            var arena = new GameObject("Arena");
            arena.AddComponent<Arena>();
            var floor = MakeSprite(arena.transform, "Floor", S("arena_floor"), mat, -100);
            floor.transform.localPosition = Vector3.zero;

            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(player);
            playerInstance.transform.position = Vector3.zero;

            var systems = new GameObject("Systems");
            var runner = systems.AddComponent<CampaignLevelRunner>();
            // OpenScene(Single) unloads unused assets, so reload the data objects before wiring them.
            level1 = AssetDatabase.LoadAssetAtPath<LevelData>(LevelImporter.AssetPath(1));
            catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(Root + "/Data/EnemyCatalog.asset");
            vitamin = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Vitamin.prefab");
            pop = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/FxPop.prefab");
            Set(runner, "level", level1); Set(runner, "catalog", catalog);
            var levelDb = AssetDatabase.LoadAssetAtPath<LevelDatabase>(MenuBuilder.LevelDatabasePath);
            if (levelDb != null) Set(runner, "database", levelDb);
            if (level1 == null || catalog == null) Debug.LogError("SliceBuilder: level or catalog missing");
            var spawner = systems.AddComponent<RewardSpawner>();
            Set(spawner, "vitaminPrefab", vitamin); Set(spawner, "popPrefab", pop);
            var slow = systems.AddComponent<SlowMotionAbility>();
            systems.AddComponent<PauseController>();

            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();

            BuildHud(mat, slow, runner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            MenuBuilder.SetBuildScenes(); // keeps MainMenu first when it exists
            AssetDatabase.SaveAssets();
            log.AppendLine("Gameplay scene rebuilt and set as the first build scene.");
            return log.ToString();
        }

        // ---------------------------------------------------------------- layers & physics
        private static void EnsureLayers(System.Text.StringBuilder log)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            foreach (string name in GameLayers.All)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).stringValue == name) exists = true;
                if (exists) continue;
                for (int i = 6; i < layers.arraySize; i++)
                {
                    if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                    layers.GetArrayElementAtIndex(i).stringValue = name;
                    log.AppendLine($"  added layer {name} ({i})");
                    break;
                }
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePhysics()
        {
            Physics2D.gravity = Vector2.zero;
            int[] ids = new int[GameLayers.All.Length + 1];
            ids[0] = 0; // Default
            for (int i = 0; i < GameLayers.All.Length; i++) ids[i + 1] = GameLayers.Get(GameLayers.All[i]);
            foreach (int a in ids) foreach (int b in ids) Physics2D.IgnoreLayerCollision(a, b, true);
            for (int i = 0; i < GameLayers.Pairs.GetLength(0); i++)
                Physics2D.IgnoreLayerCollision(GameLayers.Get(GameLayers.Pairs[i, 0]), GameLayers.Get(GameLayers.Pairs[i, 1]), false);

            Time.fixedDeltaTime = 1f / 60f; // saved to the project's Time settings; the original stepped physics every frame at 60 fps
        }

        // ---------------------------------------------------------------- prefab builders
        private static GameObject BuildPlayer(Material mat, PlayerShot shot)
        {
            var go = new GameObject("Player") { layer = GameLayers.Get(GameLayers.Player) };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>(); col.radius = Px(5f);
            var body = MakeSprite(go.transform, "Body", S("player_cell"), mat, 10);
            var nucleus = MakeSprite(go.transform, "Nucleus", S("player_nucleus"), mat, 11);
            var pc = go.AddComponent<PlayerController>();
            Set(pc, "shotPrefab", shot); Set(pc, "bodyRenderer", body); Set(pc, "nucleusRenderer", nucleus);
            var flash = go.AddComponent<SpriteFlash>(); Set(flash, "renderers", new Object[] { body, nucleus });
            return go;
        }

        private static GameObject BuildShot(Material mat)
        {
            var go = new GameObject("PlayerShot") { layer = GameLayers.Get(GameLayers.PlayerShot) };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = Px(1.5f);
            MakeSprite(go.transform, "Sprite", S("player_shot"), mat, 9);
            go.AddComponent<PlayerShot>();
            return go;
        }

        private static GameObject BuildEnemy(Material mat, string name, string sprite, float radiusPx)
        {
            var go = new GameObject(name) { layer = GameLayers.Get(GameLayers.Enemy) };
            var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = Px(radiusPx);
            var body = MakeSprite(go.transform, "Body", S(sprite), mat, 5);
            var enemy = go.AddComponent<Enemy>();
            Set(enemy, "bodyRenderer", body); Set(enemy, "hitbox", col);
            var flash = go.AddComponent<SpriteFlash>(); Set(flash, "renderers", new Object[] { body });
            return go;
        }

        private static GameObject BuildVitamin(Material mat)
        {
            var go = new GameObject("Vitamin") { layer = GameLayers.Get(GameLayers.Pickup) };
            var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = Px(3f);
            MakeSprite(go.transform, "Sprite", S("vitamin"), mat, 6);
            go.AddComponent<Vitamin>();
            return go;
        }

        private static GameObject BuildPop(Material mat)
        {
            var go = new GameObject("FxPop");
            var sr = go.AddComponent<SpriteRenderer>(); sr.sharedMaterial = mat; sr.sortingOrder = 20; sr.sprite = S("fx_pop_0");
            var anim = go.AddComponent<FrameAnimation>();
            Set(anim, "frames", new Object[] { S("fx_pop_0"), S("fx_pop_1"), S("fx_pop_2") });
            return go;
        }

        // ---------------------------------------------------------------- HUD
        private static void BuildHud(Material mat, SlowMotionAbility slow, CampaignLevelRunner runner)
        {
            var hud = new GameObject("HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(320f, 180f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = Legacy.PixelsPerUnit;

            var hearts = new Image[3];
            for (int i = 0; i < 3; i++) hearts[i] = Img(hud.transform, "Heart" + i, S("hud_heart"), mat, new Vector2(0f, 1f), new Vector2(8 + i * 9, -7));

            Img(hud.transform, "MeterFrame", U("hud_meter_frame"), mat, new Vector2(0.5f, 1f), new Vector2(-30, -7));
            var fill = Img(hud.transform, "MeterFill", S("pixel_white"), mat, new Vector2(0.5f, 1f), new Vector2(-28, -9));
            fill.rectTransform.sizeDelta = new Vector2(57f, 2f);

            Img(hud.transform, "VitaminIcon", S("vitamin"), mat, new Vector2(1f, 1f), new Vector2(-46, -8));
            PixelText vit = MakeText(hud.transform, "Vitamins", mat, new Vector2(1f, 1f), new Vector2(-39, -7), PixelText.Align.Left);
            PixelText timer = MakeText(hud.transform, "Timer", mat, new Vector2(0.5f, 1f), new Vector2(0, -16), PixelText.Align.Center);
            PixelText level = MakeText(hud.transform, "Level", mat, new Vector2(0f, 0f), new Vector2(8, 12), PixelText.Align.Left);

            var hc = hud.AddComponent<HudController>();
            Set(hc, "hearts", hearts);
            Set(hc, "heartFull", S("hud_heart")); Set(hc, "heartEmpty", S("hud_heart_empty"));
            Set(hc, "meterFill", fill.rectTransform);
            Set(hc, "vitaminsText", vit); Set(hc, "timerText", timer); Set(hc, "levelText", level);
            Set(hc, "slowMotion", slow); Set(hc, "runner", runner);
        }

        private static Image Img(Transform parent, string name, Sprite sprite, Material mat, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.material = mat; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : Vector2.one;
            return img;
        }

        private static PixelText MakeText(Transform parent, string name, Material mat, Vector2 anchor, Vector2 pos, PixelText.Align align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1f, 5f);
            var pt = go.AddComponent<PixelText>();
            var glyphs = new Object[13];
            string[] names = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "minus", "x", "colon" };
            for (int i = 0; i < names.Length; i++) glyphs[i] = U("glyph_" + names[i]);
            Set(pt, "glyphs", glyphs); Set(pt, "material", mat); Set(pt, "align", (int)align);
            return pt;
        }

        // ---------------------------------------------------------------- helpers
        private static float Px(float pixels) => pixels / Legacy.PixelsPerUnit;

        private static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Sprites/{name}.png");
        private static Sprite U(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/UI/{name}.png");

        private static SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, Material mat, int order)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sharedMaterial = mat; sr.sortingOrder = order;
            return sr;
        }

        private static GameObject SavePrefab(GameObject temp, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"SliceBuilder: {target.GetType().Name}.{field} not found"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                    break;
                default: Debug.LogError($"SliceBuilder: unsupported value for {field}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
