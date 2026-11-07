using System;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// One world of the campaign as a little map: levels 1-5 left to right, 6-9 back right to left, and the boss
    /// (big accent node with the boss mark) at the end of the path. Dotted links light up as levels are cleared.
    /// States: locked (dim), open (outline), cleared on any difficulty (filled, with 1-3 medal stars under it:
    /// the best medal on any difficulty, bronze 1, silver 2, gold 3).
    /// Arrows / stick / d-pad move along the map; up from the top row goes to the world picker, down from the
    /// bottom row to the difficulty row.
    /// </summary>
    public class WorldMap : MenuItem
    {
        private const float Node = 16f, BossNode = 24f, CursorPad = 2f;

        // Node centres, relative to the map's top-left (the map is placed so these line up with the screen centre).
        private static readonly Vector2[] Centres =
        {
            new Vector2(24, 8), new Vector2(64, 8), new Vector2(104, 8), new Vector2(144, 8), new Vector2(184, 8),
            new Vector2(184, 42), new Vector2(144, 42), new Vector2(104, 42), new Vector2(64, 42), new Vector2(22, 42),
        };
        public const float Width = 208f, Height = 58f;

        private class Cell
        {
            public Image fill, mark;
            public PaletteGraphic[] frame;
            public TextMeshProUGUI number;
            public PaletteGraphic[][] stars; // 3 stars, each made of row runs
            public Rect starArea;            // map space, y down
        }

        private const float StarSize = 7f, StarPitch = 8f;
        private static readonly string[] Star =
        {
            "...#...",
            "..###..",
            "#######",
            ".#####.",
            "..###..",
            ".##.##.",
            ".#...#.",
        };

        private readonly Cell[] cells = new Cell[Worlds.LevelsPerWorld];
        private readonly List<Image>[] links = new List<Image>[Worlds.LevelsPerWorld - 1];
        private Image[] cursor;
        private int world = 1, index;
        private Action<int> onPick;

        /// <summary>Raised when the cursor or the world changes (the info line and the world name listen).</summary>
        public Action Changed;

        public int World => world;
        public int SelectedLevel => Worlds.FirstLevel(world) + index;

        private static float Size(int i) => i == Worlds.LevelsPerWorld - 1 ? BossNode : Node;

        public static WorldMap Create(UISkin skin, Transform parent, float x, float y, Sprite bossMark, Action<int> onPick)
        {
            RectTransform area = UIFactory.Rect(parent, "WorldMap", x, y, Width, Height, new Vector2(0f, 1f));
            var m = area.gameObject.AddComponent<WorldMap>();
            m.Hit = area;
            m.onPick = onPick;

            // Dotted links first, so the nodes sit on top of them.
            for (int i = 0; i < m.links.Length; i++)
            {
                m.links[i] = new List<Image>();
                Vector2 a = Centres[i], b = Centres[i + 1];
                Vector2 dir = (b - a).normalized;
                float from = Size(i) / 2f + 2f, to = (b - a).magnitude - Size(i + 1) / 2f - 2f;
                for (float d = from; d <= to; d += 3f)
                {
                    Vector2 p = a + dir * d;
                    m.links[i].Add(UIFactory.Image(area, "Link" + i, skin.pixel, Mathf.Round(p.x), Mathf.Round(p.y), 1f, 1f));
                }
            }

            for (int i = 0; i < Worlds.LevelsPerWorld; i++)
            {
                float s = Size(i);
                float x0 = Mathf.Round(Centres[i].x - s / 2f), y0 = Mathf.Round(Centres[i].y - s / 2f);
                var holder = UIFactory.Rect(area, "Node" + (i + 1), 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
                var c = new Cell { fill = UIFactory.Image(holder, "Fill", skin.pixel, x0, y0, s, s) };
                UIFactory.Frame(skin, holder, x0, y0, x0 + s - 1f, y0 + s - 1f, PaletteRole.Foreground, false);
                var frame = new List<PaletteGraphic>();
                foreach (Transform child in holder)
                    if (child.name.StartsWith("Frame")) frame.Add(child.GetComponent<PaletteGraphic>());
                c.frame = frame.ToArray();
                if (i == Worlds.LevelsPerWorld - 1 && bossMark != null)
                    c.mark = UIFactory.Sprite(skin, holder, "BossMark", bossMark, x0 + s / 2f, y0 + s / 2f);
                else
                {
                    c.number = UIFactory.Text(skin, holder, "Number", x0 + s / 2f, y0, HorizontalAlignmentOptions.Center);
                    c.number.text = (i + 1).ToString();
                }
                // Medal stars, centred under the node.
                float sx = Mathf.Round(Centres[i].x - (2f * StarPitch + StarSize) / 2f), sy = y0 + s + 2f;
                c.starArea = new Rect(sx - 1f, sy - 1f, 2f * StarPitch + StarSize + 2f, StarSize + 2f);
                c.stars = new PaletteGraphic[3][];
                for (int k = 0; k < 3; k++)
                {
                    var runs = new List<PaletteGraphic>();
                    for (int row = 0; row < Star.Length; row++)
                        for (int col = 0; col < Star[row].Length; col++)
                        {
                            if (Star[row][col] != '#') continue;
                            int end = col;
                            while (end + 1 < Star[row].Length && Star[row][end + 1] == '#') end++;
                            Image run = UIFactory.Image(holder, "Star" + k, skin.pixel, sx + k * StarPitch + col, sy + row, end - col + 1, 1f);
                            runs.Add(run.GetComponent<PaletteGraphic>());
                            col = end;
                        }
                    c.stars[k] = runs.ToArray();
                }
                m.cells[i] = c;
            }

            var cursorHolder = UIFactory.Rect(area, "Cursor", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
            UIFactory.Frame(skin, cursorHolder, 0f, 0f, 1f, 1f, PaletteRole.Accent, false);
            m.cursor = cursorHolder.GetComponentsInChildren<Image>();
            m.Refresh();
            return m;
        }

        /// <summary>Show a world (1..5) with the cursor on one of its levels (index 0..9).</summary>
        public void SetWorld(int newWorld, int newIndex)
        {
            world = Mathf.Clamp(newWorld, 1, Worlds.Count);
            index = Mathf.Clamp(newIndex, 0, Worlds.LevelsPerWorld - 1);
            Refresh();
            Changed?.Invoke();
        }

        /// <summary>Put the cursor on a level (any world).</summary>
        public void ShowLevel(int level) => SetWorld(Worlds.Of(level), Worlds.IndexInWorld(level) - 1);

        private void SetIndex(int i)
        {
            if (i == index) return;
            index = i;
            Refresh();
            Changed?.Invoke();
        }

        /// <summary>Nearest node in a direction: dx for left / right on the same row, dy for the other row.</summary>
        private int Neighbour(int dx, int dy)
        {
            Vector2 c = Centres[index];
            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < Centres.Length; i++)
            {
                if (i == index) continue;
                Vector2 d = Centres[i] - c;
                bool sameRow = Mathf.Abs(d.y) < 1f;
                if (dx != 0 && (!sameRow || Mathf.Sign(d.x) != dx)) continue;
                if (dy != 0 && (sameRow || Mathf.Sign(d.y) != dy)) continue;
                float score = dx != 0 ? Mathf.Abs(d.x) : Mathf.Abs(d.x) + 0.1f * Mathf.Abs(d.y);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        public override bool Step(int direction)
        {
            int n = Neighbour(direction, 0);
            if (n >= 0) SetIndex(n);
            return true; // stay at the row's end
        }

        public override bool MoveWithin(int direction)
        {
            int n = Neighbour(0, direction);
            if (n < 0) return false; // off the map: the world picker above, the difficulty row below
            SetIndex(n);
            return true;
        }

        public override void Submit()
        {
            if (Progress.IsUnlocked(SelectedLevel)) onPick?.Invoke(SelectedLevel);
        }

        public override void Hover(Vector2 screenPosition)
        {
            int i = NodeAt(screenPosition);
            if (i >= 0) SetIndex(i);
        }

        public override void Click(Vector2 screenPosition)
        {
            int i = NodeAt(screenPosition);
            if (i < 0) return;
            SetIndex(i);
            Submit();
        }

        private int NodeAt(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            var p = new Vector2(local.x + Hit.rect.width * Hit.pivot.x, -(local.y - Hit.rect.height * (1f - Hit.pivot.y)));
            for (int i = 0; i < Centres.Length; i++)
            {
                float h = Size(i) / 2f + 2f;
                if (Mathf.Abs(p.x - Centres[i].x) <= h && Mathf.Abs(p.y - Centres[i].y) <= h) return i;
            }
            return -1;
        }

        public override void Refresh()
        {
            if (cells[0] == null) return;
            int first = Worlds.FirstLevel(world);
            for (int i = 0; i < cells.Length; i++)
            {
                int level = first + i;
                bool boss = i == cells.Length - 1;
                bool open = Progress.IsUnlocked(level);
                bool cleared = open && Progress.IsClearedOnAny(level); // difficulty is picked after the level
                PaletteRole role = boss ? PaletteRole.Accent : PaletteRole.Foreground;
                Cell c = cells[i];
                foreach (PaletteGraphic f in c.frame) f.Set(role, !open);
                c.fill.enabled = cleared;
                c.fill.GetComponent<PaletteGraphic>().Set(role, false);
                if (c.number != null) c.number.GetComponent<PaletteGraphic>().Set(cleared ? PaletteRole.Background : role, !open);
                if (c.mark != null) c.mark.color = new Color(1f, 1f, 1f, cleared ? 0f : open ? 1f : 0.35f);
                int stars = cleared ? Medals.StarCount(Progress.BestMedal(level)) : 0;
                for (int k = 0; k < c.stars.Length; k++)
                    foreach (PaletteGraphic g in c.stars[k])
                    {
                        g.gameObject.SetActive(cleared);
                        g.Set(k < stars ? PaletteRole.Accent : PaletteRole.Foreground, k >= stars);
                    }
            }
            // Links: lit once cleared; dots under a node's stars are hidden so the stars read cleanly.
            for (int i = 0; i < links.Length; i++)
            {
                bool cleared = Progress.IsUnlocked(first + i) && Progress.IsClearedOnAny(first + i);
                foreach (Image dot in links[i])
                {
                    dot.GetComponent<PaletteGraphic>().Set(PaletteRole.Foreground, !cleared);
                    Vector2 p = dot.rectTransform.anchoredPosition;
                    var at = new Vector2(p.x + 0.5f, -p.y + 0.5f);
                    bool covered = false;
                    for (int n = 0; n < cells.Length && !covered; n++)
                        covered = cells[n].stars[0][0].gameObject.activeSelf && cells[n].starArea.Contains(at);
                    dot.enabled = !covered;
                }
            }

            bool show = Selected;
            float s = Size(index) + 2f * CursorPad;
            Vector2 tl = new Vector2(Mathf.Round(Centres[index].x - s / 2f), Mathf.Round(Centres[index].y - s / 2f));
            foreach (Image img in cursor)
            {
                img.enabled = show;
                var rt = img.rectTransform;
                switch (img.name)
                {
                    case "FrameTop": rt.anchoredPosition = new Vector2(tl.x, -tl.y); rt.sizeDelta = new Vector2(s, 1f); break;
                    case "FrameBottom": rt.anchoredPosition = new Vector2(tl.x, -(tl.y + s - 1f)); rt.sizeDelta = new Vector2(s, 1f); break;
                    case "FrameLeft": rt.anchoredPosition = new Vector2(tl.x, -tl.y); rt.sizeDelta = new Vector2(1f, s); break;
                    case "FrameRight": rt.anchoredPosition = new Vector2(tl.x + s - 1f, -tl.y); rt.sizeDelta = new Vector2(1f, s); break;
                }
            }
        }
    }
}
