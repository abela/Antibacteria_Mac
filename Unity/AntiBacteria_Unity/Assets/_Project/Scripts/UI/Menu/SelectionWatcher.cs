using System;
using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>Calls back when the selected row of a screen changes (text that depends on the highlighted row).</summary>
    public class SelectionWatcher : MonoBehaviour
    {
        private MenuScreen screen;
        private Action changed;
        private MenuItem last;

        public static SelectionWatcher Watch(MenuScreen screen, Action changed)
        {
            var w = screen.gameObject.AddComponent<SelectionWatcher>();
            w.screen = screen;
            w.changed = changed;
            return w;
        }

        private void Update()
        {
            MenuItem now = screen.SelectedItem;
            if (now == last) return;
            last = now;
            changed?.Invoke();
        }
    }
}
