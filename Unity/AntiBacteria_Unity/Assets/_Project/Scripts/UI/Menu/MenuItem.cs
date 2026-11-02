using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>Anything on a menu screen whose text or colours change (language, palette, values).</summary>
    public interface IRefreshable
    {
        void Refresh();
    }

    /// <summary>A selectable menu row.</summary>
    public abstract class MenuItem : MonoBehaviour, IRefreshable
    {
        private bool interactable = true;

        public bool Interactable
        {
            get => interactable;
            set { interactable = value; Refresh(); }
        }

        public bool Selected { get; private set; }

        /// <summary>The row's clickable area.</summary>
        public RectTransform Hit { get; protected set; }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            Refresh();
        }

        /// <summary>Confirm (Enter / Space / A).</summary>
        public virtual void Submit() { }

        /// <summary>Left / right changes this item's value (options, difficulty): the prompt bar shows "Change".</summary>
        public virtual bool ChangesValue => false;

        /// <summary>Left / right. Returns true if the item used it.</summary>
        public virtual bool Step(int direction) => false;

        /// <summary>Mouse click at a screen position inside Hit.</summary>
        public virtual void Click(Vector2 screenPosition) => Submit();

        /// <summary>Mouse moved over this item (grids move their cursor here).</summary>
        public virtual void Hover(Vector2 screenPosition) { }

        /// <summary>
        /// Up (-1) / down (+1) inside the item, for items with several rows (the level grid).
        /// Return false at the edge so the selection moves on to the next item.
        /// </summary>
        public virtual bool MoveWithin(int direction) => false;

        public abstract void Refresh();
    }
}
