using System;
using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Runs a layout callback whenever its screen opens or the language / palette changes.
    /// For layout that needs measured text widths, which TextMeshPro only reports once the screen is active.
    /// </summary>
    public class RefreshHook : MonoBehaviour, IRefreshable
    {
        private Action action;

        public static RefreshHook Add(GameObject go, Action action)
        {
            var h = go.AddComponent<RefreshHook>();
            h.action = action;
            return h;
        }

        public void Refresh() => action?.Invoke();
    }
}
