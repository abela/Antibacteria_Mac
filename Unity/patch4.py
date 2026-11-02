import os,sys,json
A=sys.argv[1]
S=os.path.join(A,'_Project/Scripts')
def patch(rel, pairs, base=S):
    p=os.path.join(base,rel); s=open(p,encoding='utf-8').read()
    for a,b in pairs:
        n=s.count(a); assert n==1,(rel,n,a[:80]); s=s.replace(a,b)
    open(p,'w',encoding='utf-8').write(s); print('ok',rel)

# ---------------- core
patch('Core/GameSession.cs',[
("    public enum GameMode { Campaign = 0, CaptureFlag = 1, Rush = 2, PeaceWalker = 3 }\n",
 "    public enum GameMode { Campaign = 0, CaptureFlag = 1, Rush = 2, PeaceWalker = 3, Roguelike = 4 }\n"),
])
patch('Core/SceneFlow.cs',[
("            GameSession.Mode = mode;\n            GameSession.Difficulty = GameSettings.Difficulty;\n            Load(GameplayScene);\n        }\n",
 "            GameSession.Mode = mode;\n            GameSession.Difficulty = GameSettings.Difficulty;\n"
 "            if (mode == GameMode.Roguelike) RoguelikeRun.Begin(); // from the menu or Retry: a fresh run, room 1\n"
 "            Load(GameplayScene);\n        }\n\n"
 "        /// <summary>Roguelike: load the next room of the run in progress (RoguelikeRun.NextRoom was called).</summary>\n"
 "        public static void ContinueRun() => Load(GameplayScene);\n"),
])
patch('Core/Progress.cs',[
("        private const int EndlessModeCount = 3;\n", "        private const int EndlessModeCount = 4; // Capture the Flag, Rush, Peace Walker, Roguelike\n"),
("            public int[] best = new int[EndlessModeCount * DifficultyCount];\n        }\n",
 "            public int[] best = new int[EndlessModeCount * DifficultyCount];\n"
 "            public int[] roguelikeRoom = new int[DifficultyCount]; // furthest room reached per difficulty\n        }\n"),
("            Array.Resize(ref data.modes.best, EndlessModeCount * DifficultyCount);\n",
 "            Array.Resize(ref data.modes.best, EndlessModeCount * DifficultyCount);\n"
 "            if (data.modes.roguelikeRoom == null) data.modes.roguelikeRoom = new int[DifficultyCount];\n"
 "            Array.Resize(ref data.modes.roguelikeRoom, DifficultyCount);\n"),
("        private static int ModeIndex(GameMode mode, Difficulty d)",
 "        public static int RoguelikeBestRoom(Difficulty d) => Data.modes.roguelikeRoom[(int)d];\n\n"
 "        /// <summary>A roguelike run ended in this room. True (and saved) when it is the furthest yet.</summary>\n"
 "        public static bool RecordRoguelikeRoom(Difficulty d, int room)\n        {\n"
 "            if (room <= Data.modes.roguelikeRoom[(int)d]) return false;\n"
 "            Data.modes.roguelikeRoom[(int)d] = room;\n            Save();\n            return true;\n        }\n\n"
 "        private static int ModeIndex(GameMode mode, Difficulty d)"),
])
patch('Rendering/PixelShapes.cs',[
("        // ------------------------------------------------------------------ helpers\n",
 """        /// <summary>A sprite from text rows, top row first: '#' foreground, 'R' accent, anything else clear.</summary>
        public static Sprite FromRows(string key, string[] rows) =>
            Get("rows_" + key, () =>
            {
                int h = rows.Length, w = 0;
                foreach (string r in rows) w = Mathf.Max(w, r.Length);
                var px = new Color32[w, h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < rows[y].Length; x++)
                    {
                        char c = rows[y][x];
                        if (c == '#') px[x, h - 1 - y] = White;
                        else if (c == 'R') px[x, h - 1 - y] = Red;
                    }
                return Draw(px);
            });

        /// <summary>A diamond outline (2r+1 wide), with an accent centre if asked (roguelike XP orbs).</summary>
        public static Sprite Diamond(int r, bool accentCore) =>
            Get($"diamond{r}_{accentCore}", () =>
            {
                int n = 2 * r + 1;
                var px = new Color32[n, n];
                for (int x = 0; x < n; x++)
                    for (int y = 0; y < n; y++)
                    {
                        int d = Mathf.Abs(x - r) + Mathf.Abs(y - r);
                        if (d == r) px[x, y] = White;
                        else if (d < r && accentCore) px[x, y] = Red;
                    }
                return Draw(px);
            });

        // ------------------------------------------------------------------ helpers
"""),
])

# ---------------- modes / runner
patch('Gameplay/Modes/EndlessModes.cs',[
("        public virtual bool PlayerCanShoot => true;\n",
 "        public virtual bool PlayerCanShoot => true;\n        /// <summary>Played on a campaign level's data (the roguelike's rooms), not on the mode's own spawns.</summary>\n        public virtual bool UsesLevelData => false;\n"),
("                case GameMode.PeaceWalker: return new PeaceWalkerMode();\n",
 "                case GameMode.PeaceWalker: return new PeaceWalkerMode();\n                case GameMode.Roguelike: return new Roguelike.RoguelikeRoom();\n"),
])
R='Gameplay/Level/CampaignLevelRunner.cs'
patch(R,[
("        public bool IsBossLevel => !IsEndless && level != null && level.IsBoss;\n",
 "        public bool IsBossLevel => (Mode == null || Mode.UsesLevelData) && level != null && level.IsBoss;\n"),
("            if (IsEndless) return; // no level data: the mode spawns everything\n",
 "            if (IsEndless && !Mode.UsesLevelData) return; // no level data: the mode spawns everything\n"),
("            GameSession.ResetRun();\n            Progress.CountPlay(); // first-time hints fade out after a few plays\n",
 "            // Roguelike rooms after the first keep the run's score, kills and vitamins.\n"
 "            bool nextRoom = RoguelikeRun.Active && RoguelikeRun.ContinuingRoom;\n"
 "            RoguelikeRun.ContinuingRoom = false;\n"
 "            if (!nextRoom)\n            {\n                GameSession.ResetRun();\n"
 "                Progress.CountPlay(); // first-time hints fade out after a few plays\n            }\n"),
("                PlayerController.Current.VolleySize = IsEndless ? 1 : WeaponSettings.Current.VolleyForLevel(GameSession.CurrentLevel);\n",
 "                PlayerController.Current.VolleySize = IsEndless && !Mode.UsesLevelData ? 1\n"
 "                    : WeaponSettings.Current.VolleyForLevel(GameSession.CurrentLevel) + RunModifiers.ExtraVolley;\n"
 "                if (RoguelikeRun.Active && RoguelikeRun.CarriedLives > 0) PlayerController.Current.SetLives(RoguelikeRun.CarriedLives);\n"),
("            if (!IsBossLevel && !IsEndless && GetComponent<BonusSpawner>() == null)",
 "            if (!IsBossLevel && (!IsEndless || Mode.UsesLevelData) && GetComponent<BonusSpawner>() == null)"),
("        private void SpawnBoss()\n        {\n",
 "        /// <summary>Roguelike boss rooms: the room decides when.</summary>\n        public void SpawnBossNow() { if (!bossSpawned) SpawnBoss(); }\n\n"
 "        /// <summary>Roguelike: through the gate to the next room (the scene reloads, the run carries on).</summary>\n"
 "        public void NextRoom()\n        {\n            if (Current != Phase.Playing) return;\n"
 "            PlayerController p = PlayerController.Current;\n"
 "            RoguelikeRun.NextRoom(p != null ? p.Lives : -1, ElapsedSeconds);\n"
 "            Progress.SaveIfDirty();\n            SceneFlow.ContinueRun();\n        }\n\n"
 "        /// <summary>Roguelike: the last room's boss is down. The run is won (no campaign progress is touched).</summary>\n"
 "        public void CompleteRun()\n        {\n            if (Current != Phase.Playing) return;\n"
 "            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.Despawn();\n"
 "            EnemyBullet.ClearAll();\n            SetPhase(Phase.Won, 0f);\n        }\n\n"
 "        private void SpawnBoss()\n        {\n"),
("            if (Boss != null) Boss.Defeated += Win;\n",
 "            if (Boss != null && !IsEndless) Boss.Defeated += Win; // roguelike rooms watch their boss themselves\n"),
("                    AchievementTracker.RunEnded(GameSession.Mode, GameSession.Score, Mode.Completed);\n",
 "                    AchievementTracker.RunEnded(GameSession.Mode, GameSession.Score, Mode.Completed);\n"
 "                    if (RoguelikeRun.Active) NewRoomBest = Progress.RecordRoguelikeRoom(GameSession.Difficulty, RoguelikeRun.Room);\n"),
("        public bool NewModeBest { get; private set; }\n",
 "        public bool NewModeBest { get; private set; }\n        /// <summary>A roguelike run ended further than ever on this difficulty.</summary>\n        public bool NewRoomBest { get; private set; }\n"),
])

# ---------------- player & co
P='Gameplay/Player/PlayerController.cs'
patch(P,[
("        public int MaxLives => maxLives;\n", "        public int MaxLives => maxLives + RunModifiers.ExtraMaxLives; // roguelike Vitality perk\n"),
("            Lives = maxLives;\n", "            Lives = MaxLives;\n"),
("            float speed = Legacy.Units(maxSpeedPoints) * Mathf.Max(upgrade, Buffs != null ? Buffs.SpeedMultiplier : 1f);\n",
 "            float speed = Legacy.Units(maxSpeedPoints) * Mathf.Max(upgrade, Buffs != null ? Buffs.SpeedMultiplier : 1f) * RunModifiers.SpeedMultiplier;\n"),
("                fireCooldown += pattern.intervalSeconds;\n", "                fireCooldown += pattern.intervalSeconds * RunModifiers.FireIntervalMultiplier; // Rapid Fire perk\n"),
("            if (Progress.ImmortalityOn) return; // bought Immortality: never lose a life (2013 upgrade)\n            Lives--;\n            LivesChanged?.Invoke();\n            if (Lives <= 0) { Sound.Play(Sfx.PlayerDie); Die(); return; }\n",
 "            if (Progress.ImmortalityOn) return; // bought Immortality: never lose a life (2013 upgrade)\n"
 "            if (RoguelikeRun.Active && RoguelikeRun.ShieldsLeft > 0)\n            {\n"
 "                // Shield perk: this hit is blocked (once per room per rank).\n"
 "                RoguelikeRun.ShieldsLeft--;\n                Sound.Play(Sfx.ShieldHit);\n"
 "                Fx.Sparks(transform.position, Vector2.zero, 0f, 16, 0.2f);\n"
 "                invulnerableUntil = Time.time + invulnerableSeconds;\n                GetComponent<SpriteFlash>()?.Flash();\n                return;\n            }\n"
 "            Lives--;\n"
 "            if (Lives <= 0 && RunModifiers.CanRevive) { RoguelikeRun.ReviveUsed = true; Lives = 1; } // Second Wind perk\n"
 "            LivesChanged?.Invoke();\n"
 "            if (RunModifiers.NovaOnHit && Buffs != null) Buffs.Apply(BonusType.Boom); // Nova perk\n"
 "            if (Lives <= 0) { Sound.Play(Sfx.PlayerDie); Die(); return; }\n"),
("            Lives = Mathf.Min(maxLives, Lives + lives);\n", "            Lives = Mathf.Min(MaxLives, Lives + lives);\n"),
("        private void UpdateBlink()\n",
 "        /// <summary>Roguelike: start a room with the lives carried from the last one.</summary>\n"
 "        public void SetLives(int lives)\n        {\n            Lives = Mathf.Clamp(lives, 1, MaxLives);\n            LivesChanged?.Invoke();\n        }\n\n"
 "        private void UpdateBlink()\n"),
])
patch('Gameplay/Player/PlayerShot.cs',[
("            live = true;\n", "            live = true;\n            piercesLeft = RunModifiers.Pierce; // roguelike Piercing perk\n"),
("                bool hurts = enemy.CanBeHit;\n                enemy.TakeHit(); // does nothing to shields, chain bodies and Tentacles, but the shot is spent\n",
 "                bool hurts = enemy.CanBeHit;\n"
 "                int damage = RunModifiers.ShotDamage; // roguelike Heavy Shots perk\n"
 "                if (hurts && damage > 1) enemy.TakeHits(damage);\n"
 "                else enemy.TakeHit(); // does nothing to shields, chain bodies and Tentacles, but the shot is spent\n"),
("                Fx.Sparks(at, -dir, 70f, f.enemyHitSparks, hurts ? 0.5f : 0f);\n                Release();\n",
 "                Fx.Sparks(at, -dir, 70f, f.enemyHitSparks, hurts ? 0.5f : 0f);\n"
 "                if (hurts && piercesLeft > 0) { piercesLeft--; return; } // flies on through\n                Release();\n"),
])
patch('Gameplay/Player/SlowMotionAbility.cs',[
("                Meter01 = Mathf.Min(1f, Meter01 + dt / rechargeSeconds);\n",
 "                Meter01 = Mathf.Min(1f, Meter01 + dt / rechargeSeconds * RunModifiers.SlowmoRechargeMultiplier); // Time Bender perk\n"),
])
patch('Gameplay/Bonuses/PlayerBuffs.cs',[
("                left[(int)Slot.Weapon] = s.durationSeconds; // a new weapon replaces the old one, full time\n",
 "                left[(int)Slot.Weapon] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; // a new weapon replaces the old one, full time\n"),
("                    case BonusType.Immortality: left[(int)Slot.Immortality] = s.durationSeconds; break;\n                    case BonusType.DoubleSpeed: left[(int)Slot.Speed] = s.durationSeconds; break;\n",
 "                    case BonusType.Immortality: left[(int)Slot.Immortality] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; break;\n"
 "                    case BonusType.DoubleSpeed: left[(int)Slot.Speed] = s.durationSeconds * RunModifiers.BonusDurationMultiplier; break;\n"),
])
patch('UI/HudController.cs',[
("                    float w = Mathf.Max(1f, Mathf.Ceil(BuffIconPixels * left / s.durationSeconds));\n",
 "                    float w = Mathf.Max(1f, Mathf.Ceil(BuffIconPixels * Mathf.Clamp01(left / (s.durationSeconds * RunModifiers.BonusDurationMultiplier))));\n"),
("            if (levelText != null && runner != null && runner.IsEndless) levelText.SetText(",
 "            if (levelText != null && RoguelikeRun.Active) levelText.SetText(Loc.Get(WordIDs.Room) + \" \" + RoguelikeRun.Room);\n"
 "            else if (levelText != null && runner != null && runner.IsEndless) levelText.SetText("),
("            if (player == null) return;\n            for (int i = 0; i < hearts.Length; i++)\n",
 "            if (player == null) return;\n            GrowHearts(player.MaxLives);\n            for (int i = 0; i < hearts.Length; i++)\n"),
("        private void BuildBuffSlots()\n",
 "        /// <summary>More hearts when the maximum grows (roguelike Vitality perk), same spacing as the scene's.</summary>\n"
 "        private void GrowHearts(int count)\n        {\n"
 "            if (hearts.Length >= count || hearts.Length < 2 || hearts[0] == null || hearts[1] == null) return;\n"
 "            float step = hearts[1].rectTransform.anchoredPosition.x - hearts[0].rectTransform.anchoredPosition.x;\n"
 "            var list = new System.Collections.Generic.List<Image>(hearts);\n"
 "            while (list.Count < count)\n            {\n"
 "                Image last = list[list.Count - 1];\n"
 "                Image h = Instantiate(last, last.transform.parent);\n"
 "                h.rectTransform.anchoredPosition = last.rectTransform.anchoredPosition + new Vector2(step, 0f);\n"
 "                list.Add(h);\n            }\n            hearts = list.ToArray();\n        }\n\n"
 "        private void BuildBuffSlots()\n"),
])
patch('Gameplay/RewardSpawner.cs',[
("            float want = count * DifficultySettings.Current.Vitamins(GameSession.Difficulty);\n",
 "            float want = count * DifficultySettings.Current.Vitamins(GameSession.Difficulty) * RunModifiers.VitaminMultiplier;\n"),
])
patch('Gameplay/Pickups/Vitamin.cs',[
("            if (!homing && d.magnitude <= Legacy.Units(magnetDistancePoints)) homing = true;\n",
 "            if (!homing && d.magnitude <= Legacy.Units(magnetDistancePoints) * RunModifiers.MagnetMultiplier) homing = true;\n"),
])

# ---------------- menus
patch('UI/Menus/MainMenuController.cs',[
("            GameMode[] endless = { GameMode.CaptureFlag, GameMode.Rush, GameMode.PeaceWalker };\n",
 "            GameMode[] endless = { GameMode.Roguelike, GameMode.CaptureFlag, GameMode.Rush, GameMode.PeaceWalker };\n"),
("            s.Add(MenuButton.Create(skin, t, \"Back\", 160f, 132f, () => Loc.Get(WordIDs.Back), navigator.Back));\n            return s;\n        }\n\n        /// <summary>After an endless mode",
 "            s.Add(MenuButton.Create(skin, t, \"Back\", 160f, 140f, () => Loc.Get(WordIDs.Back), navigator.Back));\n            return s;\n        }\n\n        /// <summary>After an endless mode"),
])
G='UI/Menus/GameplayMenuController.cs'
patch(G,[
("            PromptBar.Create(skin, root, navigator); // [A] Select  [B] Back on pause, options and result screens\n",
 "            if (RoguelikeRun.Active)\n            {\n                RoguelikeHud.Create(skin, root);\n                levelUp = BuildLevelUp();\n            }\n"
 "            PromptBar.Create(skin, root, navigator); // [A] Select  [B] Back on pause, options and result screens\n"),
("        private const float ToastSeconds = 1.6f;\n",
 "        private const float ToastSeconds = 1.6f;\n        private MenuScreen levelUp;\n        private PerkCards perkCards;\n"),
("            UpdateBonusLabels();\n", "            UpdateBonusLabels();\n            if (levelUp != null) CheckLevelUp();\n"),
("                () => Loc.Get(runner != null && runner.IsBossLevel ? WordIDs.GetReadyForBoss : WordIDs.GetReady).ToUpperInvariant(),\n",
 "                () => RoguelikeRun.Active\n"
 "                    ? (Loc.Get(WordIDs.Room) + \" \" + RoguelikeRun.Room + (runner != null && runner.IsBossLevel ? \"  \" + Loc.Get(WordIDs.Boss) : \"\")).ToUpperInvariant()\n"
 "                    : Loc.Get(runner != null && runner.IsBossLevel ? WordIDs.GetReadyForBoss : WordIDs.GetReady).ToUpperInvariant(),\n"),
("        private string LevelName() => runner != null && runner.IsEndless\n",
 "        private string LevelName() => RoguelikeRun.Active\n"
 "            ? Loc.Get(WordIDs.Room) + \" \" + RoguelikeRun.Room + \"  \" + Loc.Get(WordIDs.Best) + \" \" + Progress.RoguelikeBestRoom(GameSession.Difficulty)\n"
 "            : runner != null && runner.IsEndless\n"),
("() => FormatTime(runner != null ? runner.ElapsedSeconds : 0f));\n",
 "() => FormatTime((runner != null ? runner.ElapsedSeconds : 0f) + (RoguelikeRun.Active ? RoguelikeRun.SecondsBeforeThisRoom : 0f)));\n"),
("        private void BuildToast()\n",
 """        // ------------------------------------------------------------------ roguelike level up

        /// <summary>"LEVEL UP!", the run's new level and three perk cards; the game is paused until one is taken.</summary>
        private MenuScreen BuildLevelUp()
        {
            MenuScreen s = MenuScreen.Create(root, "LevelUp");
            Transform t = s.transform;
            s.CanGoBack = false;
            s.ResetSelectionOnShow = true;
            UIFactory.Panel(skin, t, 4f, 6f, 315f, 160f);
            LiveText.Create(skin, t, "Title", 160f, 10f, () => Loc.Get(WordIDs.LevelUp).ToUpperInvariant(), scale: 2,
                role: PaletteRole.Accent, maxWidth: 300f);
            LiveText.Create(skin, t, "Subtitle", 160f, 38f, () => Loc.Get(WordIDs.Lv) + " " + RoguelikeRun.Level + "   " + Loc.Get(WordIDs.ChoosePerk), dim: true);
            perkCards = s.Add(PerkCards.Create(skin, t, 56f, RoguelikeSettings.Current.perkChoices, TakePerk));
            return s;
        }

        /// <summary>A level up is waiting and nothing else is on screen: pause and offer perks.</summary>
        private void CheckLevelUp()
        {
            if (RoguelikeRun.PendingLevelUps <= 0 || navigator.IsOpen || PauseController.IsPaused) return;
            if (runner == null || runner.Current != CampaignLevelRunner.Phase.Playing) return;
            var offer = RoguelikeRun.Offer(RoguelikeSettings.Current.perkChoices);
            if (offer.Count == 0) { RoguelikeRun.ConsumeLevelUp(); return; } // every perk maxed
            PauseController.Pause();
            Sound.Play(Sfx.BonusAppear);
            perkCards.Show(offer);
            navigator.Show(levelUp);
        }

        private void TakePerk(PerkId perk)
        {
            RoguelikeRun.Take(perk);
            Sound.Play(Sfx.BonusPickup);
            var offer = RoguelikeRun.PendingLevelUps > 0 ? RoguelikeRun.Offer(RoguelikeSettings.Current.perkChoices) : null;
            if (offer != null && offer.Count > 0)
            {
                perkCards.Show(offer); // another level up is waiting
                levelUp.RefreshAll();
                return;
            }
            navigator.Clear();
            PauseController.Resume();
        }

        private void BuildToast()
"""),
])

# ---------------- words
words=[("Roguelike",["Roguelike","Roguelike","Roguelike","Рогалик","Roguelike"]),
 ("Room",["Room","Salle","Raum","Комната","Sala"]),
 ("LevelUp",["Level up!","Niveau sup !","Level up!","Новый уровень!","¡Nivel arriba!"]),
 ("ChoosePerk",["Choose a perk","Choisissez un atout","Wähle einen Vorteil","Выберите улучшение","Elige una ventaja"]),
 ("RoomClear",["Room clear!","Salle nettoyée !","Raum gesäubert!","Комната зачищена!","¡Sala despejada!"]),
 ("GoThroughGate",["Go through the gate","Passez la porte","Geh durch das Tor","Пройдите в ворота","Cruza la puerta"]),
 ("Rank",["Rank","Rang","Rang","Ранг","Rango"]),
 ("New",["New","Nouveau","Neu","Новое","Nuevo"]),
 ("Lv",["Lv","Niv","Lv","Ур","Nv"]),
 ("PerkRapidFire",["Rapid Fire","Tir rapide","Schnellfeuer","Скорострельность","Fuego rápido"]),
 ("PerkRapidFireDesc",["Shoot {0}% faster","Tirez {0}% plus vite","{0}% schneller schießen","Стрельба быстрее на {0}%","Disparas un {0}% más rápido"]),
 ("PerkExtraBarrel",["Extra Barrel","Canon en plus","Extralauf","Доп. ствол","Cañón extra"]),
 ("PerkExtraBarrelDesc",["+{0} bullet per volley","+{0} balle par salve","+{0} Kugel pro Salve","+{0} пуля в залпе","+{0} bala por ráfaga"]),
 ("PerkHeavyShots",["Heavy Shots","Tirs lourds","Schwere Schüsse","Тяжёлые пули","Disparos pesados"]),
 ("PerkHeavyShotsDesc",["+{0} damage per bullet","+{0} dégât par balle","+{0} Schaden pro Kugel","+{0} урон за пулю","+{0} daño por bala"]),
 ("PerkPiercing",["Piercing","Perforant","Durchschlag","Пробивание","Perforante"]),
 ("PerkPiercingDesc",["Bullets go through {0} more enemy","Les balles traversent {0} ennemi de plus","Kugeln durchdringen {0} Gegner mehr","Пули пробивают ещё {0} врага","Las balas atraviesan {0} enemigo más"]),
 ("PerkQuickFeet",["Quick Feet","Pieds légers","Flinke Füße","Быстрые ноги","Pies ligeros"]),
 ("PerkQuickFeetDesc",["Move {0}% faster","Bougez {0}% plus vite","{0}% schneller bewegen","Движение быстрее на {0}%","Te mueves un {0}% más rápido"]),
 ("PerkVitality",["Vitality","Vitalité","Vitalität","Живучесть","Vitalidad"]),
 ("PerkVitalityDesc",["+{0} max life, heals {0}","+{0} vie max, soigne {0}","+{0} max. Leben, heilt {0}","+{0} к макс. жизням, лечит {0}","+{0} vida máx., cura {0}"]),
 ("PerkFirstAid",["First Aid","Premiers soins","Erste Hilfe","Аптечка","Primeros auxilios"]),
 ("PerkFirstAidDesc",["Heal {0} life after every room","Soigne {0} vie après chaque salle","Heilt {0} Leben nach jedem Raum","Лечит {0} жизнь после комнаты","Cura {0} vida tras cada sala"]),
 ("PerkShield",["Shield","Bouclier","Schild","Щит","Escudo"]),
 ("PerkShieldDesc",["Blocks {0} hit in every room","Bloque {0} coup par salle","Blockt {0} Treffer pro Raum","Блокирует {0} удар в комнате","Bloquea {0} golpe por sala"]),
 ("PerkSecondWind",["Second Wind","Second souffle","Zweiter Atem","Второе дыхание","Segundo aliento"]),
 ("PerkSecondWindDesc",["Survive one deadly hit","Survivez à un coup fatal","Überlebe einen tödlichen Treffer","Переживите один смертельный удар","Sobrevive a un golpe mortal"]),
 ("PerkNova",["Nova","Nova","Nova","Нова","Nova"]),
 ("PerkNovaDesc",["Losing a life blasts the enemies around you","Perdre une vie souffle les ennemis proches","Ein verlorenes Leben sprengt die Gegner um dich","Потеря жизни взрывает врагов рядом","Perder una vida hace estallar a los enemigos cercanos"]),
 ("PerkMagnet",["Magnet","Aimant","Magnet","Магнит","Imán"]),
 ("PerkMagnetDesc",["Pull orbs and vitamins from {0}% farther","Attire orbes et vitamines {0}% plus loin","Zieht Kugeln und Vitamine {0}% weiter an","Притягивает сферы и витамины на {0}% дальше","Atrae orbes y vitaminas un {0}% más lejos"]),
 ("PerkWisdom",["Wisdom","Sagesse","Weisheit","Мудрость","Sabiduría"]),
 ("PerkWisdomDesc",["+{0}% XP from orbs","+{0}% d'XP des orbes","+{0}% EP aus Kugeln","+{0}% опыта со сфер","+{0}% de XP de los orbes"]),
 ("PerkGoldenTouch",["Golden Touch","Toucher d'or","Goldenes Händchen","Золотое касание","Toque dorado"]),
 ("PerkGoldenTouchDesc",["+{0}% vitamins","+{0}% de vitamines","+{0}% Vitamine","+{0}% витаминов","+{0}% de vitaminas"]),
 ("PerkBonusLover",["Bonus Lover","Fan de bonus","Bonusfan","Любитель бонусов","Amante de bonus"]),
 ("PerkBonusLoverDesc",["Bonuses last {0}% longer","Les bonus durent {0}% de plus","Boni halten {0}% länger","Бонусы длятся на {0}% дольше","Los bonus duran un {0}% más"]),
 ("PerkTimeBender",["Time Bender","Maître du temps","Zeitbieger","Повелитель времени","Doblatiempo"]),
 ("PerkTimeBenderDesc",["Slow motion recharges {0}% faster","Le ralenti se recharge {0}% plus vite","Zeitlupe lädt {0}% schneller","Замедление заряжается на {0}% быстрее","La cámara lenta recarga un {0}% más rápido"]),
]
p=os.path.join(A,'GleyPlugins/Localization/Resources/LocalizationFile.json')
d=json.load(open(p,encoding='utf-8'))
ids={w['ID'] for w in d['allText']}
nxt=max(w['enumID'] for w in d['allText'])+1
enum_lines=""
for name,tr in words:
    if name in ids: continue
    d['allText'].append({"ID":name,"enumID":nxt,"translations":[{"language":l,"word":w} for l,w in zip([10,14,15,30,34],tr)]})
    enum_lines+=f"{name} = {nxt},\n"; nxt+=1
open(p,'w',encoding='utf-8').write(json.dumps(d,separators=(',',':'),ensure_ascii=False)); print('loc ok',nxt)
patch('GleyPlugins/Localization/Scripts/WordIDs.cs',[("DiffImpossibleDesc = 103,\n","DiffImpossibleDesc = 103,\n"+enum_lines)], base=A)
