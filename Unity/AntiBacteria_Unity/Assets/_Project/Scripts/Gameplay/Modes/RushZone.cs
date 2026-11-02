using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Modes
{
    /// <summary>
    /// Rush objective: a ring the player must stand in until it fills (RushStation.mm). Fills while inside and drains
    /// outside, in game time. Drawn as a ring of dots that turn red as it fills (2013: a faded square scaled 5x
    /// and a radial progress timer).
    /// </summary>
    public class RushZone : MonoBehaviour
    {
        private const int Dots = 40;

        public event Action Completed;
        public float Progress01 { get; private set; }

        private SpriteRenderer[] dots;
        private float radius;
        private bool done;

        public static RushZone Spawn(Vector2 at)
        {
            ModeSettings s = ModeSettings.Current;
            var go = new GameObject("RushZone");
            go.transform.position = at;
            var z = go.AddComponent<RushZone>();
            z.radius = Legacy.Units(s.zoneRadiusPoints);
            z.dots = new SpriteRenderer[Dots];
            float rPixels = z.radius * Legacy.PixelsPerUnit;
            for (int i = 0; i < Dots; i++)
            {
                float a = Mathf.PI / 2f - i * 2f * Mathf.PI / Dots; // clockwise from the top, like a clock filling
                var d = new GameObject("Dot");
                d.transform.SetParent(go.transform, false);
                d.transform.localPosition = new Vector3(Mathf.Round(Mathf.Cos(a) * rPixels), Mathf.Round(Mathf.Sin(a) * rPixels), 0f) / Legacy.PixelsPerUnit;
                var sr = d.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = s.material;
                sr.sortingOrder = 5;
                z.dots[i] = sr;
            }
            var centre = new GameObject("Flag");
            centre.transform.SetParent(go.transform, false);
            var c = centre.AddComponent<SpriteRenderer>();
            c.sprite = s.flag;
            c.sharedMaterial = s.material;
            c.sortingOrder = 5;
            z.Refresh();
            return z;
        }

        public bool PlayerInside
        {
            get
            {
                PlayerController p = PlayerController.Current;
                return p != null && p.IsAlive && Vector2.Distance(p.transform.position, transform.position) <= radius;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (done || dt <= 0f) return;
            ModeSettings s = ModeSettings.Current;
            float step = dt / Mathf.Max(0.1f, s.zoneFillSeconds);
            Progress01 = Mathf.Clamp01(Progress01 + (PlayerInside ? step : -step));
            Refresh();
            if (Progress01 >= 1f)
            {
                done = true;
                Completed?.Invoke();
                Destroy(gameObject);
            }
        }

        private void Refresh()
        {
            ModeSettings s = ModeSettings.Current;
            int lit = Mathf.FloorToInt(Progress01 * Dots);
            bool blink = PlayerInside && Mathf.FloorToInt(Time.time / 0.15f) % 2 == 0; // the next dot blinks while filling
            for (int i = 0; i < Dots; i++)
            {
                bool on = i < lit || (i == lit && blink);
                dots[i].sprite = on ? s.dotFilled : s.dot;
                Color c = dots[i].color;
                c.a = on ? 1f : 0.45f;
                dots[i].color = c;
            }
        }
    }
}
