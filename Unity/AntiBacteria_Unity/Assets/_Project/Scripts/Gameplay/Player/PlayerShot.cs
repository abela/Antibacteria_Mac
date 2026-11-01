using System;
using AntiBacteria.Core;
using AntiBacteria.Gameplay.Enemies;
using UnityEngine;

namespace AntiBacteria.Gameplay.Player
{
    /// <summary>
    /// One player bullet. Flies straight; one hit on an enemy, gone on a wall. Pooled by PlayerController.
    /// Original: 40 m/s, no lifetime (relied on the walls), every bullet could hurt the player (Docs 01, section 6).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerShot : MonoBehaviour
    {
        private Rigidbody2D rb;
        private float lifeLeft;
        private Action<PlayerShot> release;
        private bool live;

        private void Awake() => rb = GetComponent<Rigidbody2D>();

        public void Launch(Vector2 position, Vector2 direction, float speed, float lifetime, Action<PlayerShot> releaser)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            rb.position = position;
            rb.linearVelocity = direction * speed;
            lifeLeft = lifetime;
            release = releaser;
            live = true;
        }

        private void Update()
        {
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f) Release();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!live) return;
            int layer = other.gameObject.layer;
            if (layer == GameLayers.Get(GameLayers.Enemy))
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.CanBeHit) return; // still fading in: pass through
                enemy.TakeHit();
                Release();
            }
            else if (layer == GameLayers.Get(GameLayers.Wall))
            {
                Release();
            }
        }

        private void Release()
        {
            if (!live) return;
            live = false;
            rb.linearVelocity = Vector2.zero;
            release?.Invoke(this);
        }
    }
}
