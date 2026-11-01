using System;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// The campaign map: 5 worlds (rows) x 10 levels (columns), the last column being the bosses.
    /// Cell states: locked (dim), open (outline), cleared on the chosen difficulty (filled).
    /// Arrows / stick / d-pad move the cursor; leaving the bottom row moves on to the difficulty row.
    /// </summary>
    public class LevelGrid : MenuItem
    {
        public const int Columns = 10;
        public const int Rows = 5;
        public const float CellW = 20f;
        public const float CellH = 16f;
        public const float Gap = 3f;
        private const float Margin = 3f; // room for the cursor inside the clickable area
        private const float NumberOffsetY = 0f;

        private struct Cell
        {
            public Image fill;
            public PaletteGraphic[] frame;
            public TextMeshProUGUI number;
        }

        private Cell[] cells;
        private Image[] cursor;
        private int index;
        private Action<int> onPick;

        /// <summary>Raised when the cursor moves (the info line listens).</summary>
        public Action CursorMoved;

        public int SelectedLevel => index + 1;

        public static LevelGrid Create(UISkin skin, Transform parent, float x, float y, Action<int> onPick)
        {
            float w = Columns * CellW + (Columns - 1) * Gap, h = Rows * CellH + (Rows - 1) * Gap;
            RectTransform area = UIFactory.Rect(parent, "LevelGrid", x - Margin, y - Margin, w + 2f * Margin, h + 2f * Margin,
                new Vector2(0f, 1f));
            var g = area.gameObject.AddComponent<LevelGrid>();
            g.Hit = area;
            g.onPick = onPick;
            g.cells = new Cell[Columns * Rows];
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
            {
                float cx = Margin + c * (CellW + Gap), cy = Margin + r * (CellH + Gap);
                var holder = UIFactory.Rect(area, $"Level{r * Columns + c + 1}", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
                Image fill = UIFactory.Image(holder, "Fill", skin.pixel, cx, cy, CellW, CellH);
                UIFactory.Frame(skin, holder, cx, cy, cx + CellW - 1f, cy + CellH - 1f, PaletteRole.Foreground, false);
                TextMeshProUGUI num = UIFactory.Text(skin, holder, "Number", cx + CellW / 2f, cy + NumberOffsetY,
                    HorizontalAlignmentOptions.Center);
                num.text = (c + 1).ToString();
                var frame = new PaletteGraphic[4];
                int k = 0;
                foreach (Transform child in holder)
                    if (child.name.StartsWith("Frame")) frame[k++] = child.GetComponent<PaletteGraphic>();
                g.cells[r * Columns + c] = new Cell { fill = fill, frame = frame, number = num };
            }

            var cursorHolder = UIFactory.Rect(area, "Cursor", 0f, 0f, 0f, 0f, new Vector2(0f, 1f));
            UIFactory.Frame(skin, cursorHolder, -2f, -2f, CellW + 1f, CellH + 1f, PaletteRole.Accent, false);
            g.cursor = cursorHolder.GetComponentsInChildren<Image>();
            g.Refresh();
            return g;
        }

        public void SetCursor(int level)
        {
            index = Mathf.Clamp(level - 1, 0, Columns * Rows - 1);
            Refresh();
            CursorMoved?.Invoke();
        }

        public override bool Step(int direction)
        {
            int c = index % Columns + direction;
            if (c < 0 || c >= Columns) return true; // stay at the edge
            SetCursor(index + direction + 1);
            return true;
        }

        public override bool MoveWithin(int direction)
        {
            int r = index / Columns + direction;
            if (r < 0 || r >= Rows) return false; // leave the grid (up: wraps to Back, down: difficulty row)
            SetCursor(index + direction * Columns + 1);
            return true;
        }

        public override void Submit()
        {
            if (Progress.IsUnlocked(SelectedLevel)) onPick?.Invoke(SelectedLevel);
        }

        public override void Hover(Vector2 screenPosition)
        {
            int i = CellAt(screenPosition);
            if (i >= 0 && i != index) SetCursor(i + 1);
        }

        public override void Click(Vector2 screenPosition)
        {
            int i = CellAt(screenPosition);
            if (i < 0) return;
            SetCursor(i + 1);
            Submit();
        }

        private int CellAt(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            float x = local.x - Margin, y = -local.y - Margin;
            int c = Mathf.FloorToInt(x / (CellW + Gap)), r = Mathf.FloorToInt(y / (CellH + Gap));
            if (c < 0 || c >= Columns || r < 0 || r >= Rows) return -1;
            if (x - c * (CellW + Gap) >= CellW || y - r * (CellH + Gap) >= CellH) return -1; // in a gap
            return r * Columns + c;
        }

        public override void Refresh()
        {
            if (cells == null) return;
            Difficulty d = GameSettings.Difficulty;
            for (int i = 0; i < cells.Length; i++)
            {
                int level = i + 1;
                bool boss = i % Columns == Columns - 1;
                bool open = Progress.IsUnlocked(level);
                bool cleared = open && Progress.IsCleared(level, d);
                PaletteRole role = boss ? PaletteRole.Accent : PaletteRole.Foreground;

                Cell cell = cells[i];
                foreach (PaletteGraphic f in cell.frame) f.Set(role, !open);
                cell.fill.enabled = cleared;
                cell.fill.GetComponent<PaletteGraphic>().Set(role, false);
                cell.number.GetComponent<PaletteGraphic>().Set(cleared ? PaletteRole.Background : role, !open);
            }

            bool show = Selected;
            foreach (Image c in cursor) c.enabled = show;
            if (cursor.Length > 0)
            {
                Transform holder = cursor[0].transform.parent;
                ((RectTransform)holder).anchoredPosition = new Vector2(
                    Margin + index % Columns * (CellW + Gap), -(Margin + index / Columns * (CellH + Gap)));
            }
        }
    }
}
