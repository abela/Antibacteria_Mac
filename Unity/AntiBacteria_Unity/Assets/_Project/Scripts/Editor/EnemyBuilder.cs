using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using AntiBacteria.Gameplay.Enemies.Bosses;
using AntiBacteria.Rendering;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Builds every regular enemy: prefabs, definitions and the catalog, with the original numbers.
    /// Sources: Docs/DesignReference/02 (group A) and 03 (group B). Speeds are m/s x 32 = points/s.
    /// Fixes from Docs/DesignReference/BugFixes.md are noted where a number differs from 2013.
    /// Hitboxes match the pixel art (the original sensors were up to 40 % larger than the sprites).
    /// Re-run any time: AntiBacteria > Build > Enemies.
    /// </summary>
    public static class EnemyBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs/Enemies";
        private const string DataDir = Root + "/Data/Enemies";
        public const string CatalogPath = Root + "/Data/EnemyCatalog.asset";

        private static Material mat;

        private static float M(float metresPerSecond) => metresPerSecond * Legacy.PointsPerMetre;

        [MenuItem("AntiBacteria/Build/Enemies")]
        public static void BuildMenu() => Debug.Log(BuildAll());

        public static string BuildAll()
        {
            var log = new System.Text.StringBuilder("EnemyBuilder:\n");
            SliceBuilder.ConfigurePhysicsPublic(); // collision matrix incl. player shots vs enemy bullets
            mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/PaletteSprite.mat");
            EnsureFolder(Root + "/Prefabs"); EnsureFolder(PrefabDir); EnsureFolder(DataDir);

            EnemyBullet bullet = BuildBullet();
            var catalog = new List<EnemyDefinition>();

            // ---------------------------------------------------------------- group A
            // 0 BlueCorosia: fodder, wanders, spins (BlueCorosia.mm)
            var d = Def(0, "BlueCorosia", Single("Enemy_BlueCorosia", "enemy_spiky", 6.4f));
            d.movement = EnemyMovement.Wander; d.speedPoints = M(8); d.spinDegreesPerSecond = 300f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 50; d.vitamins = 2;
            catalog.Add(Save(d));

            // 1 Loctopus: blue chaser (Loctopus.mm). Fix: drops 3 vitamins as its data says (code dropped 2).
            d = Def(1, "Loctopus", Single("Enemy_Loctopus", "enemy_loctopus", 5.5f, "eye_small"));
            d.movement = EnemyMovement.Homing; d.speedPoints = M(9); d.eyeOffsetPixels = 2;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 80; d.vitamins = 3;
            catalog.Add(Save(d));

            // 2 RedNoise: red chaser, appears instantly (RedNoise.mm)
            GameObject redNoisePrefab = Single("Enemy_RedNoise", "enemy_rednoise", 5.5f, "eye_small");
            d = Def(2, "RedNoise", redNoisePrefab);
            d.movement = EnemyMovement.Homing; d.speedPoints = M(10); d.eyeOffsetPixels = 2;
            d.hits = 1; d.fadeInSeconds = 0f; d.killScore = 80; d.vitamins = 3;
            catalog.Add(Save(d));

            // 3 SpiralCorosia: homes in big loops (SpiralCorosia.mm): 6 m/s + 10-20 m/s orbit, 1-3 deg/update
            d = Def(3, "SpiralCorosia", Single("Enemy_SpiralCorosia", "enemy_spiral", 6f));
            d.movement = EnemyMovement.Spiral; d.speedPoints = M(6);
            d.orbitSpeedPointsMin = M(10); d.orbitSpeedPointsMax = M(20); d.orbitDegreesPerSecondMin = 120f; d.orbitDegreesPerSecondMax = 360f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 90; d.vitamins = 3;
            catalog.Add(Save(d));

            // 5 BigHunterus: big slow chaser, 5 hits (BigHunterus.mm)
            d = Def(5, "BigHunterus", Single("Enemy_BigHunterus", "enemy_bighunter", 15f, "eye_big"));
            d.movement = EnemyMovement.Homing; d.speedPoints = M(7); d.eyeOffsetPixels = 4;
            d.hits = 5; d.fadeInSeconds = 0.1f; d.killScore = 120; d.vitamins = 5;
            catalog.Add(Save(d));

            // 6 BigBlueCorosia: big wanderer, 7 hits (BigBlueCorosia.mm)
            d = Def(6, "BigBlueCorosia", Single("Enemy_BigBlueCorosia", "enemy_big", 12.5f));
            d.movement = EnemyMovement.Wander; d.speedPoints = M(7); d.spinDegreesPerSecond = 300f;
            d.hits = 7; d.fadeInSeconds = 0.3f; d.killScore = 100; d.vitamins = 5;
            catalog.Add(Save(d));

            // 7 ShootingCorosia: big wanderer, 4-way burst every 2 s (ShootingCorosia.mm). Bursts follow its nozzles.
            d = Def(7, "ShootingCorosia", Single("Enemy_ShootingCorosia", "enemy_shooting", 11.5f));
            d.movement = EnemyMovement.Wander; d.speedPoints = M(7); d.spinDegreesPerSecond = 300f;
            d.hits = 10; d.fadeInSeconds = 0.3f; d.killScore = 130; d.vitamins = 7;
            d.shotPattern = ShotPattern.Radial; d.bulletPrefab = bullet; d.shotIntervalSeconds = 2f; d.shotCount = 4;
            d.shotOffsetPoints = 80f; d.shotSpeedPoints = M(30);
            catalog.Add(Save(d));

            // 9 Adrenalium: dashes at 18-25 m/s, half the time to where the player stands (Adrenalium.mm)
            d = Def(9, "Adrenalium", Single("Enemy_Adrenalium", "enemy_adrenalium", 5.5f));
            d.movement = EnemyMovement.Dash; d.speedPoints = M(20); d.dashSpeedPointsMin = M(18); d.dashSpeedPointsMax = M(25);
            d.dashPlayerChance = 0.5f; d.spinDegreesPerSecond = 300f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 150; d.vitamins = 5;
            catalog.Add(Save(d));

            // 12 ChoruxDefender: spiral homer, invulnerable while its Chorux lives (ChoruxDefender.mm)
            d = Def(12, "ChoruxDefender", Single("Enemy_ChoruxDefender", "enemy_defender", 4.5f, null, "enemy_defender_open"));
            d.movement = EnemyMovement.Spiral; d.speedPoints = M(7);
            d.orbitSpeedPointsMin = M(10); d.orbitSpeedPointsMax = M(20); d.orbitDegreesPerSecondMin = 120f; d.orbitDegreesPerSecondMax = 360f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 90; d.vitamins = 2;
            catalog.Add(Save(d));

            // 11 Chorux: homing core with 2 defenders (Chorux.mm). Fix: its 200 points are actually paid.
            d = Def(11, "Chorux", Single("Enemy_Chorux", "enemy_chorux", 5.5f, null, null, typeof(Chorux)));
            d.movement = EnemyMovement.Homing; d.speedPoints = M(6); d.spinDegreesPerSecond = 300f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 200; d.vitamins = 3;
            catalog.Add(Save(d));

            // 13 Spawner: big fast wanderer that hatches Loctopus/Adrenalium/RedNoise/SpiralCorosia every 1 s (Spawner.mm).
            // Fixes: interval no longer halves after slow motion; at most 8 children alive; vitamins only when killed.
            d = Def(13, "Spawner", Single("Enemy_Spawner", "enemy_spawner", 12.5f));
            d.movement = EnemyMovement.Wander; d.speedPoints = M(15); d.spinDegreesPerSecond = 300f;
            d.hits = 10; d.fadeInSeconds = 0.3f; d.killScore = 160; d.vitamins = 10;
            d.spawnIntervalSeconds = 1f; d.spawnIds = new[] { 1, 9, 2, 3 }; d.maxChildren = 8;
            catalog.Add(Save(d));

            // 17 Tentacle: slow, indestructible (Peace Walker hazard; Tentacle.mm)
            d = Def(17, "Tentacle", Single("Enemy_Tentacle", "enemy_tentacle", 5f));
            d.movement = EnemyMovement.Wander; d.speedPoints = M(3); d.spinDegreesPerSecond = 300f;
            d.hits = 1; d.invulnerable = true; d.fadeInSeconds = 0.1f; d.killScore = 0; d.vitamins = 0;
            catalog.Add(Save(d));

            // 18 ZigZagus: homing with a +-20 m/s wobble at ~600 deg/s (ZigZagus.mm). Fix: wobble is across the heading.
            d = Def(18, "ZigZagus", Single("Enemy_ZigZagus", "enemy_zigzag", 5.5f, "eye_small"));
            d.movement = EnemyMovement.ZigZag; d.speedPoints = M(7); d.wobbleSpeedPoints = M(20); d.wobbleDegreesPerSecond = 600f;
            d.eyeOffsetPixels = 2; d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 160; d.vitamins = 4;
            catalog.Add(Save(d));

            // 20 Gnida: homing, sidesteps when aimed at while you fire (Gnida.mm).
            // Fix: the original sidestep was 50 m/s by accident (un-normalised); 16 m/s is a readable dodge. Tune here.
            d = Def(20, "Gnida", Single("Enemy_Gnida", "enemy_gnida", 5.5f, "eye_small"));
            d.movement = EnemyMovement.Homing; d.speedPoints = M(10); d.eyeOffsetPixels = 2;
            d.dodgeWhenAimedAt = true; d.dodgeSpeedPoints = M(16); d.dodgeConeDegrees = 30f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 200; d.vitamins = 5;
            catalog.Add(Save(d));

            // 21 ZamriGnida: spiral homing + dodge (ZamriGnida.mm): 6 m/s + 7-16 m/s orbit. Dodge 14 m/s (was 36 by accident).
            d = Def(21, "ZamriGnida", Single("Enemy_ZamriGnida", "enemy_zamri", 6f, "eye_small"));
            d.movement = EnemyMovement.Spiral; d.speedPoints = M(6); d.eyeOffsetPixels = 2;
            d.orbitSpeedPointsMin = M(7); d.orbitSpeedPointsMax = M(16); d.orbitDegreesPerSecondMin = 120f; d.orbitDegreesPerSecondMax = 360f;
            d.dodgeWhenAimedAt = true; d.dodgeSpeedPoints = M(14); d.dodgeConeDegrees = 30f;
            d.hits = 1; d.fadeInSeconds = 0.1f; d.killScore = 250; d.vitamins = 6;
            catalog.Add(Save(d));

            // ---------------------------------------------------------------- group B
            // 8 Sunux: stationary sun, 8-way burst every 2 s (Sunux.mm). Fix: vitamins only when killed.
            d = Def(8, "Sunux", Single("Enemy_Sunux", "enemy_sunux", 9.5f));
            d.movement = EnemyMovement.Stationary; d.spinDegreesPerSecond = 300f;
            d.hits = 20; d.fadeInSeconds = 0.3f; d.killScore = 150; d.vitamins = 7;
            d.shotPattern = ShotPattern.Radial; d.bulletPrefab = bullet; d.shotIntervalSeconds = 2f; d.shotCount = 8;
            d.shotOffsetPoints = 80f; d.shotSpeedPoints = M(30);
            catalog.Add(Save(d));

            GameObject segmentPrefab = Single("Enemy_Segment", "snake_segment_r4", 5f);

            // 14 Serpentus: 6 segments, 7 hits per head, next segment takes over (Serpentus.mm, SerpentusBody.mm)
            var seg = Def(-14, "SerpentusSegment", segmentPrefab);
            seg.movement = EnemyMovement.Controlled; seg.hits = 7; seg.fadeInSeconds = 0.1f; seg.killScore = 80; seg.vitamins = 3;
            Save(seg);
            d = Def(14, "Serpentus", ChainRoot("Enemy_Serpentus"));
            Chain(d, seg, 6, ChainDeath.PromoteNext, "snake_head", 180f, "snake_segment_r4", "snake_segment_r4", "snake_segment_r3", "snake_segment_r3", "snake_segment_r2");
            catalog.Add(Save(d));

            // 19 ShootingSerpentus: 5 segments, 4 hits each, head fires an aimed bullet every 1.5 s (ShootingSerpentus.mm)
            seg = Def(-19, "ShootingSerpentusSegment", segmentPrefab);
            seg.movement = EnemyMovement.Controlled; seg.hits = 4; seg.fadeInSeconds = 0.1f; seg.killScore = 80; seg.vitamins = 3;
            Save(seg);
            d = Def(19, "ShootingSerpentus", ChainRoot("Enemy_ShootingSerpentus"));
            Chain(d, seg, 5, ChainDeath.PromoteNext, "snake_head_shooter", 180f, "snake_segment_r4", "snake_segment_r3", "snake_segment_r2");
            d.shotPattern = ShotPattern.Aimed; d.bulletPrefab = bullet; d.shotIntervalSeconds = 1.5f; d.shotOffsetPoints = 30f; d.shotSpeedPoints = M(30);
            catalog.Add(Save(d));

            // 10 CaterPillar: 20 small segments, head has 10 hits, killing it pops the whole worm (CaterPillar.mm).
            // Fixes: paid 0 points and 0 vitamins and the head looked like the body; now 300 points, 5 vitamins, own head.
            seg = Def(-10, "CaterPillarSegment", segmentPrefab);
            seg.movement = EnemyMovement.Controlled; seg.hits = 10; seg.fadeInSeconds = 0.1f; seg.killScore = 300; seg.vitamins = 5;
            Save(seg);
            d = Def(10, "CaterPillar", ChainRoot("Enemy_CaterPillar"));
            Chain(d, seg, 20, ChainDeath.KillAll, "cat_head", 90f, "cat_segment");
            d.segmentSpacingPoints = 30f;
            catalog.Add(Save(d));

            // 15 LotusBacterius: 7 segments; the 10-hit head dies and the 6 petals spiral off as 1-hit homers
            // (LotusBacterius.mm, LotusBacteriaBody.mm). Fix: petals drop 3 vitamins (the double drop gave 6).
            var petal = Def(16, "LotusPetal", segmentPrefab);
            petal.movement = EnemyMovement.Spiral; petal.speedPoints = M(7);
            petal.orbitSpeedPointsMin = M(10); petal.orbitSpeedPointsMax = M(20); petal.orbitDegreesPerSecondMin = 120f; petal.orbitDegreesPerSecondMax = 360f;
            petal.hits = 1; petal.fadeInSeconds = 0f; petal.killScore = 90; petal.vitamins = 3;
            Save(petal);
            seg = Def(-15, "LotusSegment", segmentPrefab);
            seg.movement = EnemyMovement.Controlled; seg.hits = 1; seg.fadeInSeconds = 0.1f; seg.killScore = 90; seg.vitamins = 3;
            Save(seg);
            d = Def(15, "LotusBacterius", ChainRoot("Enemy_LotusBacterius"));
            Chain(d, seg, 7, ChainDeath.Bloom, "lotus_head", -1f, "lotus_petal");
            d.headHits = 10; d.bloomDefinition = petal;
            catalog.Add(Save(d));

            // ---------------------------------------------------------------- bosses (Docs/DesignReference/03 sections 5-6)
            // Boss kill scores: 2013 paid 0 (and hid the score on boss levels); these give the result screen meaning.

            // 22 BigBoss (level 10): huge spinning blob, dashes (1/3 of targets = player), 17/20/23 m/s, 255 hits
            d = Def(22, "BigBoss", Single("Boss_BigBoss", "Bosses/boss_bigboss", 38f, null, null, typeof(BossEnemy)));
            d.movement = EnemyMovement.Dash; d.dashPlayerChance = 1f / 3f; d.speedPoints = M(17);
            d.speedPointsByDifficulty = new[] { M(17), M(20), M(23) }; d.spinDegreesPerSecond = 300f;
            d.hits = 255; d.fadeInSeconds = 0.1f; d.killScore = 1000; d.vitamins = 10;
            Boss(d, "boss_bigboss");
            catalog.Add(Save(d));

            // 23 Inferno (level 20): burning mass, dashes (1/2 = player), 7 -> 10 -> 16 m/s as it is hurt, 300 hits,
            // 10/20/25 indestructible Tentacles
            d = Def(23, "Inferno", Single("Boss_Inferno", "Bosses/boss_inferno", 25f, null, null, typeof(Inferno)));
            d.movement = EnemyMovement.Dash; d.dashPlayerChance = 0.5f; d.speedPoints = M(7);
            d.dashSpeedPointsMin = M(7); d.dashSpeedPointsMax = M(7); d.spinDegreesPerSecond = 300f;
            d.countByDifficulty = new[] { 10, 20, 25 };
            d.hits = 300; d.fadeInSeconds = 0.5f; d.killScore = 1500; d.vitamins = 10;
            Boss(d, "boss_inferno");
            catalog.Add(Save(d));

            // 24 BigRedNoise (level 30): giant chaser 12/15/18 m/s, 400 hits, minion waves at 300/200/100.
            // Minions are RedNoise without vitamins (bornFromBoss).
            var minion = Def(-2, "RedNoiseMinion", redNoisePrefab);
            minion.movement = EnemyMovement.Homing; minion.speedPoints = M(10); minion.eyeOffsetPixels = 2;
            minion.hits = 1; minion.fadeInSeconds = 0f; minion.killScore = 80; minion.vitamins = 0;
            catalog.Add(Save(minion));
            d = Def(24, "BigRedNoise", Single("Boss_BigRedNoise", "Bosses/boss_bigrednoise", 30f, "Bosses/eye_boss", null, typeof(BigRedNoise)));
            d.movement = EnemyMovement.Homing; d.speedPoints = M(12); d.speedPointsByDifficulty = new[] { M(12), M(15), M(18) };
            d.eyeOffsetPixels = 6; d.hits = 400; d.fadeInSeconds = 1f; d.killScore = 2000; d.vitamins = 10;
            Boss(d, "boss_bigrednoise");
            catalog.Add(Save(d));

            // 25 Anakonda (level 40): 24-segment snake at 17 m/s, 14 hits per head, aimed shots 1.5/1.0/0.5 s,
            // grows a 5-hit segment every 3.0/2.0/1.8 s
            seg = Def(-25, "AnakondaSegment", segmentPrefab);
            seg.movement = EnemyMovement.Controlled; seg.hits = 14; seg.fadeInSeconds = 0.1f; seg.killScore = 80; seg.vitamins = 3;
            Save(seg);
            d = Def(25, "Anakonda", ChainRoot("Boss_Anakonda", typeof(Anakonda)));
            Chain(d, seg, 24, ChainDeath.PromoteNext, "Bosses/ana_head", 180f,
                "Bosses/ana_segment_r7", "Bosses/ana_segment_r7", "Bosses/ana_segment_r6", "Bosses/ana_segment_r6", "Bosses/ana_segment_r5");
            d.speedPoints = M(17); d.segmentSpacingPoints = 61f; // the original's gap at 17 m/s
            d.shotPattern = ShotPattern.Aimed; d.bulletPrefab = bullet; d.shotIntervalSeconds = 1.5f;
            d.shotIntervalByDifficulty = new[] { 1.5f, 1.0f, 0.5f }; d.shotOffsetPoints = 40f; d.shotSpeedPoints = M(30);
            d.growIntervalByDifficulty = new[] { 3f, 2f, 1.8f };
            d.isBoss = true; d.killScore = 2500;
            catalog.Add(Save(d));

            // 26 Frunctus (level 50, final): spiral homing 6 m/s + 10-20 m/s orbit, dodges in a 45 degree cone,
            // dashes away from walls at 80 m/s, aimed shots 1.5/0.7/0.3 s. Fix: 150 hits (2013 shipped with 1).
            d = Def(26, "Frunctus", Single("Boss_Frunctus", "Bosses/boss_frunctus", 9f, "Bosses/eye_frunctus", null, typeof(Frunctus)));
            d.movement = EnemyMovement.Spiral; d.speedPoints = M(6);
            d.orbitSpeedPointsMin = M(10); d.orbitSpeedPointsMax = M(20); d.orbitDegreesPerSecondMin = 200f; d.orbitDegreesPerSecondMax = 500f;
            d.dodgeWhenAimedAt = true; d.dodgeConeDegrees = 45f; d.dodgeSpeedPoints = M(6);
            d.eyeOffsetPixels = 3;
            d.shotPattern = ShotPattern.Aimed; d.bulletPrefab = bullet; d.shotIntervalSeconds = 1.5f;
            d.shotIntervalByDifficulty = new[] { 1.5f, 0.7f, 0.3f }; d.shotOffsetPoints = 50f; d.shotSpeedPoints = M(30);
            d.hits = 150; d.fadeInSeconds = 0.1f; d.killScore = 5000; d.vitamins = 10;
            Boss(d, "boss_frunctus");
            catalog.Add(Save(d));

            // ---------------------------------------------------------------- catalog
            var cat = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(CatalogPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<EnemyCatalog>(); AssetDatabase.CreateAsset(cat, CatalogPath); }
            var so = new SerializedObject(cat);
            SerializedProperty list = so.FindProperty("enemies");
            list.arraySize = catalog.Count;
            for (int i = 0; i < catalog.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();

            log.AppendLine($"{catalog.Count} enemy types in the catalog (IDs: {string.Join(", ", catalog.ConvertAll(c => c.legacyId))}).");
            log.AppendLine("Gridium (4) and ArmagedonBacteria (27) were dead in 2013 and are not ported.");
            return log.ToString();
        }

        // ---------------------------------------------------------------- definitions

        private static EnemyDefinition Def(int id, string name, GameObject prefab)
        {
            string path = $"{DataDir}/{(id < 0 ? "seg" + (-id).ToString("00") : id.ToString("00"))}_{name}.asset";
            var d = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<EnemyDefinition>(); AssetDatabase.CreateAsset(d, path); }
            var fresh = ScriptableObject.CreateInstance<EnemyDefinition>(); // reset to defaults so re-runs are exact
            EditorUtility.CopySerialized(fresh, d);
            Object.DestroyImmediate(fresh);
            d.name = System.IO.Path.GetFileNameWithoutExtension(path);
            d.legacyId = id;
            d.displayName = name;
            d.prefab = prefab;
            return d;
        }

        private static EnemyDefinition Save(EnemyDefinition d)
        {
            EditorUtility.SetDirty(d);
            return d;
        }

        private static void Boss(EnemyDefinition d, string sprite)
        {
            d.isBoss = true;
            d.damageStages = new[] { S("Bosses/" + sprite), S("Bosses/" + sprite + "_1"), S("Bosses/" + sprite + "_2"), S("Bosses/" + sprite + "_3") };
        }

        private static void Chain(EnemyDefinition d, EnemyDefinition segment, int count, ChainDeath death, string head, float facing,
            params string[] body)
        {
            d.movement = EnemyMovement.Chain;
            d.speedPoints = M(10);
            d.arriveDistancePoints = 10f;
            d.chainSegments = count;
            d.segmentDefinition = segment;
            d.segmentSpacingPoints = 36f; // the original's steady-state gap at 10 m/s, 60 fps
            d.chainDeath = death;
            d.headSprite = S(head);
            d.headFacingDegrees = facing;
            d.bodySprites = System.Array.ConvertAll(body, S);
            d.hits = segment.hits;
            d.killScore = 0; // paid per segment
            d.vitamins = 0;
        }

        // ---------------------------------------------------------------- prefabs

        private static GameObject Single(string name, string sprite, float radiusPx, string eye = null, string alt = null,
            System.Type type = null)
        {
            var go = new GameObject(name) { layer = GameLayers.Get(GameLayers.Enemy) };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = radiusPx / Legacy.PixelsPerUnit;
            SpriteRenderer body = MakeSprite(go.transform, "Body", S(sprite), 5);
            SpriteRenderer eyeR = eye != null ? MakeSprite(go.transform, "Eye", S(eye), 6) : null;
            var enemy = (Enemy)go.AddComponent(type ?? typeof(Enemy));
            Set(enemy, "bodyRenderer", body);
            Set(enemy, "hitbox", col);
            if (eyeR != null) Set(enemy, "eyeRenderer", eyeR);
            if (alt != null) Set(enemy, "altSprite", S(alt));
            var flash = go.AddComponent<SpriteFlash>();
            Set(flash, "renderers", eyeR != null ? new Object[] { body, eyeR } : new Object[] { body });
            return SavePrefab(go, name);
        }

        private static GameObject ChainRoot(string name, System.Type type = null)
        {
            var go = new GameObject(name);
            go.AddComponent(type ?? typeof(ChainEnemy));
            return SavePrefab(go, name);
        }

        private static EnemyBullet BuildBullet()
        {
            var go = new GameObject("EnemyBullet") { layer = GameLayers.Get(GameLayers.EnemyShot) };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 2f / Legacy.PixelsPerUnit;
            MakeSprite(go.transform, "Sprite", S("enemy_bullet"), 8);
            go.AddComponent<EnemyBullet>();
            return SavePrefab(go, "EnemyBullet").GetComponent<EnemyBullet>();
        }

        // ---------------------------------------------------------------- helpers

        private static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Sprites/{name}.png");

        private static SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name) { layer = parent.gameObject.layer };
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

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"EnemyBuilder: {target.GetType().Name}.{field} not found"); return; }
            if (value is Object o) p.objectReferenceValue = o;
            else if (value is Object[] arr)
            {
                p.arraySize = arr.Length;
                for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
