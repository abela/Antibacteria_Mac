using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>Shows its GameObject for a while (real time), then hides it. Menu messages ("Not enough vitamins").</summary>
    public class HideAfter : MonoBehaviour
    {
        private float hideAt;

        public void Show(float seconds)
        {
            hideAt = Time.unscaledTime + seconds;
            gameObject.SetActive(true);
            foreach (IRefreshable r in GetComponentsInChildren<IRefreshable>(true)) r.Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime >= hideAt) gameObject.SetActive(false);
        }
    }
}
