using System;
using AntiBacteria.Core;
using AntiBacteria.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Normal / Hard / Impossible side by side (the 2013 level select's three buttons). The chosen one is bright,
    /// the others dim; when the row is selected the chosen one turns accent with arrows. Left/right or a click picks.
    /// </summary>
    public class DifficultyRow : MenuItem
    {
        private static readonly float[] Centres = { 96f, 160f, 224f };
        private const float ArrowGap = 4f;

        private TextMeshProUGUI[] labels;
        private Image leftArrow, rightArrow;
        private Action onChanged, onSubmit;

        public static DifficultyRow Create(UISkin skin, Transform parent, float y, Action onChanged, Action onSubmit)
        {
            RectTransform row = UIFactory.Rect(parent, "Difficulty", 56f, y, 208f, skin.rowHeight, new Vector2(0f, 1f));
            var d = row.gameObject.AddComponent<DifficultyRow>();
            d.Hit = row;
            d.onChanged = onChanged;
            d.onSubmit = onSubmit;
            d.labels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
                d.labels[i] = UIFactory.Text(skin, row, "Label" + i, Centres[i] - 56f, 0f, HorizontalAlignmentOptions.Center);
            d.leftArrow = UIFactory.Image(row, "ArrowL", skin.arrow, 0f, 5f, 3f, 5f, PaletteRole.Accent);
            d.rightArrow = UIFactory.Image(row, "ArrowR", skin.arrow, 0f, 5f, 3f, 5f, PaletteRole.Accent);
            d.rightArrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            d.Refresh();
            return d;
        }

        private void Set(int value)
        {
            value = Mathf.Clamp(value, 0, 2);
            if (value == (int)GameSettings.Difficulty) return;
            GameSettings.SetDifficulty((Difficulty)value);
            Refresh();
            onChanged?.Invoke();
        }

        public override bool Step(int direction)
        {
            Set((int)GameSettings.Difficulty + direction);
            return true;
        }

        public override void Submit() => onSubmit?.Invoke();

        public override void Click(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Hit, screenPosition, null, out Vector2 local);
            float x = local.x + 56f;
            int nearest = 0;
            for (int i = 1; i < 3; i++)
                if (Mathf.Abs(x - Centres[i]) < Mathf.Abs(x - Centres[nearest])) nearest = i;
            Set(nearest);
        }

        public override void Refresh()
        {
            if (labels == null) return;
            int current = (int)GameSettings.Difficulty;
            WordIDs[] words = { WordIDs.Normal, WordIDs.Hard, WordIDs.Impossible };
            for (int i = 0; i < 3; i++)
            {
                labels[i].text = Loc.Get(words[i]);
                bool chosen = i == current;
                labels[i].GetComponent<PaletteGraphic>().Set(chosen && Selected ? PaletteRole.Accent : PaletteRole.Foreground, !chosen);
            }

            leftArrow.enabled = rightArrow.enabled = Selected;
            float w = UIFactory.Width(labels[current], labels[current].text);
            float centre = Centres[current] - 56f;
            leftArrow.rectTransform.anchoredPosition = new Vector2(Mathf.Floor(centre - w / 2f) - ArrowGap - 3f, -5f);
            rightArrow.rectTransform.anchoredPosition = new Vector2(Mathf.Ceil(centre + w / 2f) + ArrowGap + 3f, -5f);
        }
    }
}
