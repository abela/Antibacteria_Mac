using System.Collections.Generic;
using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Level;
using AntiBacteria.Gameplay.Player;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using Pole = Rewired.Pole;
using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// First-play controls tutorial. After GET READY, one step at a time in a box at the bottom of the screen:
    /// MOVE (W A S D / left stick), AIM AND SHOOT (mouse + left button / right stick + trigger), SLOW MOTION
    /// (right button or Space / left trigger), then GOOD LUCK! Each step waits until the player has done it.
    /// Keys come from the player's current bindings and glyphs follow the device in use (keyboard and mouse,
    /// Xbox, PlayStation, Switch). The clock and the waves wait (CampaignLevelRunner.HoldForTutorial) until the
    /// last step, then the tutorial is saved as done (Progress.TutorialDone) and never shows again.
    /// Steps the level can't do are skipped: no shooting in Peace Walker, no slow motion on boss levels.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        private enum Step { Move, Shoot, Slow, GoodLuck }

        private const float BoxBottom = 174f;      // canvas pixels (320 x 180): low, clear of the player who starts mid-screen
        private const float KeyHeight = 13f, Gap = 4f, RowGap = 3f, Pad = 8f;
        private const float MoveSeconds = 0.8f;    // of real movement
        private const int ShotsNeeded = 6;
        private const float AutoAimSeconds = 2f;   // auto-shoot on: aim for a moment
        private const float DoneFlashSeconds = 0.55f, GoodLuckSeconds = 1.4f;

        private UISkin skin;
        private RectTransform root, box;
        private CampaignLevelRunner runner;
        private MenuNavigator navigator;
        private SlowMotionAbility slow;
        private readonly List<Step> steps = new List<Step>();
        private int index;
        private float progress, doneTimer = -1f, goodLuckTimer;
        private int shots;
        private string shownKey;
        private TextMeshProUGUI label;

        /// <summary>Adds the tutorial to a level if it hasn't been finished yet. Holds the level until it is.</summary>
        public static void CreateIfNeeded(UISkin skin, RectTransform parent, CampaignLevelRunner runner, MenuNavigator navigator)
        {
            if (runner == null || Progress.TutorialDone) return;
            var t = UIFactory.Rect(parent, "Tutorial", 0f, 0f, UIFactory.ScreenW, UIFactory.ScreenH, new Vector2(0f, 1f)).gameObject.AddComponent<TutorialOverlay>();
            t.skin = skin;
            t.root = (RectTransform)t.transform;
            t.runner = runner;
            t.navigator = navigator;
            t.slow = runner.GetComponent<SlowMotionAbility>();
            runner.HoldForTutorial = true;
        }

        private void Start()
        {
            PlayerController.Fired += OnFired;
            Loc.Changed += ForceRedraw;
            PromptSettings.Changed += ForceRedraw;
        }

        private void OnDestroy()
        {
            PlayerController.Fired -= OnFired;
            Loc.Changed -= ForceRedraw;
            PromptSettings.Changed -= ForceRedraw;
            if (runner != null) runner.HoldForTutorial = false;
        }

        private void ForceRedraw() => shownKey = null;

        /// <summary>What this level allows, decided once play starts (the level has set the player up by then).</summary>
        private void PlanSteps()
        {
            PlayerController p = PlayerController.Current;
            steps.Add(Step.Move);
            if (p == null || p.CanShoot) steps.Add(Step.Shoot);
            if (slow != null && slow.Enabled) steps.Add(Step.Slow);
            steps.Add(Step.GoodLuck);
        }

        private void OnFired()
        {
            if (Current == Step.Shoot && doneTimer < 0f) shots++;
        }

        private Step Current => steps.Count == 0 ? Step.Move : steps[Mathf.Min(index, steps.Count - 1)];

        private void Update()
        {
            if (runner == null) { Destroy(gameObject); return; }
            if (runner.Current == CampaignLevelRunner.Phase.Won || runner.Current == CampaignLevelRunner.Phase.Lost) { Destroy(gameObject); return; }
            bool visible = runner.Current == CampaignLevelRunner.Phase.Playing && !navigator.IsOpen && !PauseController.IsPaused;
            if (box != null && box.gameObject.activeSelf != visible) box.gameObject.SetActive(visible);
            if (runner.Current != CampaignLevelRunner.Phase.Playing) return;
            if (PauseController.IsPaused) return;
            if (steps.Count == 0) PlanSteps();

            float dt = Time.unscaledDeltaTime;
            Redraw();
            if (!visible) return;

            if (Current == Step.GoodLuck)
            {
                goodLuckTimer += dt;
                if (goodLuckTimer >= GoodLuckSeconds) Destroy(gameObject);
                return;
            }

            if (doneTimer >= 0f)
            {
                // The step is done: its box flashes for a moment, then the next step.
                doneTimer += dt;
                if (label != null) label.enabled = Mathf.FloorToInt(doneTimer * 10f) % 2 == 0;
                if (doneTimer >= DoneFlashSeconds) Next();
                return;
            }

            switch (Current)
            {
                case Step.Move:
                    if (GameInput.Move.magnitude > 0.3f) progress += dt;
                    if (progress >= MoveSeconds) Complete();
                    break;
                case Step.Shoot:
                    if (GameSettings.AutoShoot) { progress += dt; if (progress >= AutoAimSeconds) Complete(); }
                    else if (shots >= ShotsNeeded) Complete();
                    break;
                case Step.Slow:
                    if (slow == null || slow.IsActive) Complete();
                    break;
            }
        }

        private void Complete()
        {
            doneTimer = 0f;
            Sound.Play(Sfx.UiConfirm);
        }

        private void Next()
        {
            index++;
            progress = 0f;
            doneTimer = -1f;
            shots = 0;
            shownKey = null;
            if (Current == Step.GoodLuck)
            {
                // Last step: the level starts for real now.
                runner.HoldForTutorial = false;
                Progress.MarkTutorialDone();
            }
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>One thing in a row of glyphs: a key cap, a mouse with a button lit, or plain text ("/", "+").</summary>
        private struct Glyph
        {
            public string key;      // key cap text
            public int mouse;       // 0 none, 1 left button lit, 2 right button lit, 3 mouse only (aim)
            public string text;     // plain text
            public static Glyph Key(string s) => new Glyph { key = s };
            public static Glyph Mouse(int button) => new Glyph { mouse = button };
            public static Glyph Text(string s) => new Glyph { text = s };
        }

        private void Redraw()
        {
            PromptFamily family = ControllerPrompts.Shown;
            string key = index + "|" + steps.Count + "|" + family + "|" + GameSettings.AutoShoot + "|" + Loc.Current;
            if (key == shownKey) return;
            shownKey = key;
            if (box != null) Destroy(box.gameObject);

            List<List<Glyph>> rows = Rows(Current, family);
            string title = Title(Current).ToUpperInvariant();

            box = UIFactory.Rect(root, "Box", 0f, 0f, UIFactory.ScreenW, UIFactory.ScreenH, new Vector2(0f, 1f));
            // Measure first: the box fits its widest line.
            label = UIFactory.Text(skin, box, "Label", 160f, 0f, HorizontalAlignmentOptions.Center, 1, PaletteRole.Accent);
            label.text = title;
            float labelW = UIFactory.Width(label, title);
            var widths = new List<float>();
            float contentW = labelW;
            foreach (List<Glyph> r in rows) { float w = RowWidth(r); widths.Add(w); contentW = Mathf.Max(contentW, w); }
            float rowsH = rows.Count * KeyHeight + Mathf.Max(0, rows.Count - 1) * RowGap;
            float h = Pad + rowsH + (rows.Count > 0 ? 5f : 0f) + 11f + Pad;
            // Room on both sides for the step counter in the top-right corner, so the box stays centred.
            string count = Current == Step.GoodLuck ? null : (index + 1) + "/" + (steps.Count - 1);
            float countW = count != null ? UIFactory.Width(label, count) + 6f : 0f;
            float w0 = Mathf.Max(96f, contentW + 2f * Pad + 2f * countW);
            float x0 = Mathf.Floor(160f - w0 / 2f), y0 = BoxBottom - h, x1 = x0 + w0 - 1f, y1 = BoxBottom - 1f;

            UIFactory.Panel(skin, box, x0, y0, x1, y1);
            label.transform.SetAsLastSibling();
            float y = y0 + Pad;
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(rows[i], 160f - Mathf.Floor(widths[i] / 2f), y);
                y += KeyHeight + RowGap;
            }
            if (rows.Count > 0) y += 5f - RowGap;
            ((RectTransform)label.transform).anchoredPosition = new Vector2(160f, -(y - 3f));

            // Step counter in the box's top-right corner (not on GOOD LUCK).
            if (count != null)
            {
                var c = UIFactory.Text(skin, box, "Count", x1 - 5f, y0 + 2f, HorizontalAlignmentOptions.Right, 1, PaletteRole.Foreground, true);
                c.text = count;
            }
        }

        private static string Title(Step s)
        {
            switch (s)
            {
                case Step.Move: return Loc.Get(WordIDs.TutorialMove);
                case Step.Shoot: return Loc.Get(GameSettings.AutoShoot ? WordIDs.TutorialAim : WordIDs.TutorialAimShoot);
                case Step.Slow: return Loc.Get(WordIDs.SlowMotion);
                default: return Loc.Get(WordIDs.TutorialGoodLuck);
            }
        }

        private static List<List<Glyph>> Rows(Step s, PromptFamily family)
        {
            var rows = new List<List<Glyph>>();
            bool pad = family != PromptFamily.Keyboard;
            switch (s)
            {
                case Step.Move:
                    if (pad) { rows.Add(new List<Glyph> { Glyph.Key(StickName(family, true)) }); break; }
                    // W over A S D, from the player's own bindings.
                    rows.Add(new List<Glyph> { Glyph.Key(KeyName(InputActions.MoveVertical, Pole.Positive, "W")) });
                    rows.Add(new List<Glyph>
                    {
                        Glyph.Key(KeyName(InputActions.MoveHorizontal, Pole.Negative, "A")),
                        Glyph.Key(KeyName(InputActions.MoveVertical, Pole.Negative, "S")),
                        Glyph.Key(KeyName(InputActions.MoveHorizontal, Pole.Positive, "D")),
                    });
                    break;
                case Step.Shoot:
                    if (pad)
                    {
                        var r = new List<Glyph> { Glyph.Key(StickName(family, false)) };
                        if (!GameSettings.AutoShoot) { r.Add(Glyph.Text("+")); r.Add(Glyph.Key(PadName(family, InputActions.Fire, "RT"))); }
                        rows.Add(r);
                        break;
                    }
                    var m = new List<Glyph> { Glyph.Mouse(GameSettings.AutoShoot ? 3 : 1) };
                    string fireKey = GameSettings.AutoShoot ? null : Remapping.BindingName(BindDevice.Keyboard, InputActions.Fire);
                    if (!string.IsNullOrEmpty(fireKey)) { m.Add(Glyph.Text("/")); m.Add(Glyph.Key(Upper(fireKey))); }
                    rows.Add(m);
                    break;
                case Step.Slow:
                    if (pad) { rows.Add(new List<Glyph> { Glyph.Key(PadName(family, InputActions.SlowMotion, "LT")) }); break; }
                    var sl = new List<Glyph> { Glyph.Mouse(2) };
                    string slowKey = Remapping.BindingName(BindDevice.Keyboard, InputActions.SlowMotion);
                    if (!string.IsNullOrEmpty(slowKey)) { sl.Add(Glyph.Text("/")); sl.Add(Glyph.Key(Upper(slowKey))); }
                    rows.Add(sl);
                    break;
            }
            return rows;
        }

        private static string Upper(string s) => s.ToUpperInvariant();

        private static string KeyName(string action, Pole pole, string fallback)
        {
            string n = Remapping.BindingName(BindDevice.Keyboard, action, pole, true);
            return Upper(string.IsNullOrEmpty(n) ? fallback : Short(n));
        }

        /// <summary>"Up Arrow" -> "UP", "Left Shift" -> "L SHIFT": key caps stay small.</summary>
        private static string Short(string n)
        {
            n = n.Replace(" Arrow", "");
            if (n.StartsWith("Left ")) n = "L " + n.Substring(5);
            if (n.StartsWith("Right ")) n = "R " + n.Substring(6);
            return n;
        }

        private static string StickName(PromptFamily f, bool left) => left ? "LS" : "RS";

        /// <summary>The pad button bound to an action, in the family's own short names (RT / R2 / ZR...).</summary>
        private static string PadName(PromptFamily f, string action, string fallback)
        {
            string n = (Remapping.BindingName(BindDevice.Gamepad, action) ?? "").ToLowerInvariant();
            int i = n.Contains("trigger") ? 0 : n.Contains("shoulder") || n.Contains("bumper") ? 1 : -1;
            bool right = n.Contains("right");
            if (i < 0)
            {
                if (n.Length == 0) n = fallback.ToLowerInvariant();
                else return Upper(n);
                i = n.EndsWith("t") ? 0 : 1;
                right = n.StartsWith("r");
            }
            string[,] names =
            {
                // trigger, shoulder - right then left
                { "RT", "RB", "LT", "LB" },   // Xbox
                { "R2", "R1", "L2", "L1" },   // PlayStation
                { "ZR", "R", "ZL", "L" },     // Switch
            };
            int fam = f == PromptFamily.PlayStation ? 1 : f == PromptFamily.Switch ? 2 : 0;
            return names[fam, (right ? 0 : 2) + i];
        }

        private float RowWidth(List<Glyph> row)
        {
            float w = 0f;
            for (int i = 0; i < row.Count; i++)
            {
                if (i > 0) w += Gap;
                w += GlyphWidth(row[i]);
            }
            return w;
        }

        private float GlyphWidth(Glyph g)
        {
            if (g.mouse != 0) return 9f;
            if (g.text != null) return UIFactory.Width(label, g.text);
            return Mathf.Max(KeyHeight, UIFactory.Width(label, g.key) + 6f);
        }

        private void DrawRow(List<Glyph> row, float x, float y)
        {
            foreach (Glyph g in row)
            {
                float w = GlyphWidth(g);
                if (g.mouse != 0)
                {
                    UIFactory.Sprite(skin, box, "Mouse", MouseSprite(g.mouse), x + 4.5f, y + KeyHeight / 2f);
                }
                else if (g.text != null)
                {
                    var t = UIFactory.Text(skin, box, "Sep", x, y - 2f, HorizontalAlignmentOptions.Left, 1, PaletteRole.Foreground, true);
                    t.text = g.text;
                }
                else
                {
                    UIFactory.Frame(skin, box, x, y, x + w - 1f, y + KeyHeight - 1f, PaletteRole.Foreground, false);
                    var t = UIFactory.Text(skin, box, "Key", x + Mathf.Floor(w / 2f), y - 2f, HorizontalAlignmentOptions.Center, 1, PaletteRole.Foreground);
                    t.text = g.key;
                }
                x += w + Gap;
            }
        }

        /// <summary>9 x 13 mouse; button 1 (left) or 2 (right) lit in the accent colour, 3 = no button lit.</summary>
        private static Sprite MouseSprite(int button)
        {
            string l = button == 1 ? "RRR" : "...", r = button == 2 ? "RRR" : "...";
            return PixelShapes.FromRows("tut_mouse_" + button, new[]
            {
                "..#####..",
                ".#" + l.Substring(0, 2) + "#" + r.Substring(0, 2) + "#.",
                "#" + l + "#" + r + "#",
                "#" + l + "#" + r + "#",
                "#########",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                ".#.....#.",
                "..#####..",
            });
        }
    }
}
