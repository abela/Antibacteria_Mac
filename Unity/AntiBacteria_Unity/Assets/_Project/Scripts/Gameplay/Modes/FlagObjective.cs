using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Modes
{
    /// <summary>
    /// Capture the Flag / Peace Walker objective: touch it to score (Flag.mm, PeaceFlag.mm). A red flag for
    /// Capture the Flag, a white one for Peace Walker (2013 tinted it green, outside the 3-colour palette).
    /// </summary>
    public class FlagObjective : MonoBehaviour
    {
        public event Action Taken;
        private bool taken;
        private SpriteRenderer sr;

        public static FlagObjective Spawn(Sprite sprite, Vector2 at)
        {
            ModeSettings s = ModeSettings.Current;
            var go = new GameObject("Flag") { layer = GameLayers.Get(GameLayers.Pickup) };
            go.transform.position = at;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(13f, 13f) / Legacy.PixelsPerUnit;
            var f = go.AddComponent<FlagObjective>();
            f.sr = go.AddComponent<SpriteRenderer>();
            f.sr.sprite = sprite;
            f.sr.sharedMaterial = s.material;
            f.sr.sortingOrder = 7;
            return f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            PlayerController p = other.GetComponentInParent<PlayerController>();
            if (p == null || !p.IsAlive) return;
            taken = true;
            Taken?.Invoke();
            Destroy(gameObject);
        }
    }
}
