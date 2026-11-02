using AntiBacteria.Audio;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Player;
using UnityEngine;

namespace AntiBacteria.Gameplay.Pickups
{
    /// <summary>
    /// The currency drop. Waits until the player comes within 500 pt, then homes in at 1280 pt/s and is
    /// collected on contact (Docs/DesignReference/01, section 7).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Vitamin : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private float magnetDistancePoints = 500f;
        [SerializeField] private float homingSpeedPoints = 1280f;

        private Rigidbody2D rb;
        private bool homing;

        private void Awake() => rb = GetComponent<Rigidbody2D>();

        private void FixedUpdate()
        {
            PlayerController p = PlayerController.Current;
            if (p == null || !p.IsAlive) { rb.linearVelocity = Vector2.zero; return; }
            Vector2 d = (Vector2)p.transform.position - rb.position;
            if (!homing && d.magnitude <= Legacy.Units(magnetDistancePoints)) homing = true;
            rb.linearVelocity = homing ? d.normalized * Legacy.Units(homingSpeedPoints) : Vector2.zero;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != GameLayers.Get(GameLayers.Player)) return;
            GameSession.AddVitamins(value);
            Sound.Play(Sfx.Vitamin, 1f, 1f + Random.Range(0f, 0.15f));
            Destroy(gameObject);
        }
    }
}
