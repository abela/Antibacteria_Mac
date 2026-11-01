using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>One menu page: its rows, which one is selected, and what Back does.</summary>
    public class MenuScreen : MonoBehaviour
    {
        public readonly List<MenuItem> Items = new List<MenuItem>();

        /// <summary>Back / Esc / B. Null = return to the previous screen.</summary>
        public Action OnBack;

        /// <summary>False for result screens that must be answered with a button.</summary>
        public bool CanGoBack = true;

        /// <summary>Called every time the screen opens (update stats here).</summary>
        public Action OnShown;

        /// <summary>Row selected when the screen first opens.</summary>
        public int DefaultIndex;

        /// <summary>Reselect DefaultIndex every time the screen opens (result screens), instead of remembering.</summary>
        public bool ResetSelectionOnShow;

        private int selected = -1;

        public MenuItem SelectedItem => selected >= 0 && selected < Items.Count ? Items[selected] : null;

        public static MenuScreen Create(Transform parent, string name)
        {
            RectTransform rt = UIFactory.Stretch(parent, name);
            var s = rt.gameObject.AddComponent<MenuScreen>();
            rt.gameObject.SetActive(false);
            return s;
        }

        public T Add<T>(T item) where T : MenuItem
        {
            Items.Add(item);
            return item;
        }

        public void Shown()
        {
            OnShown?.Invoke();
            if (ResetSelectionOnShow || SelectedItem == null || !SelectedItem.Interactable)
            {
                selected = -1;
                int start = Mathf.Clamp(DefaultIndex, 0, Mathf.Max(0, Items.Count - 1));
                for (int k = 0; k < Items.Count; k++)
                {
                    int i = (start + k) % Items.Count;
                    if (Items[i].gameObject.activeSelf && Items[i].Interactable) { selected = i; break; }
                }
            }
            for (int i = 0; i < Items.Count; i++) Items[i].SetSelected(i == selected);
            RefreshAll();
        }

        public void RefreshAll()
        {
            foreach (IRefreshable r in GetComponentsInChildren<IRefreshable>(true)) r.Refresh();
        }

        public void Select(MenuItem item)
        {
            int i = Items.IndexOf(item);
            if (i < 0 || i == selected || !item.Interactable) return;
            if (SelectedItem != null) SelectedItem.SetSelected(false);
            selected = i;
            item.SetSelected(true);
        }

        /// <summary>Move the selection up (-1) or down (+1), skipping disabled rows, wrapping around.</summary>
        public void Move(int direction)
        {
            if (Items.Count == 0) return;
            if (SelectedItem != null && SelectedItem.MoveWithin(direction)) return;
            int i = selected;
            for (int k = 0; k < Items.Count; k++)
            {
                i = ((i + direction) % Items.Count + Items.Count) % Items.Count;
                if (Items[i].gameObject.activeSelf && Items[i].Interactable)
                {
                    Select(Items[i]);
                    return;
                }
            }
        }

        public MenuItem ItemAt(Vector2 screenPosition)
        {
            foreach (MenuItem item in Items)
            {
                if (item.Hit == null || !item.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(item.Hit, screenPosition, null)) return item;
            }
            return null;
        }
    }
}
