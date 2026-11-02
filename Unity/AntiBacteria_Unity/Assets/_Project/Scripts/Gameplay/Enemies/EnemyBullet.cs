using AntiBacteria.Audio;
using System.Collections.Generic;
using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>
    /// Enemy bullet (red hollow orb). Flies straight at the original 30 m/s until it hits the player, a wall or a
    /// player shot (shots cancel each other, as in 2013). It never hurts enemies.
    /// The player's own trigger handles the damage; the bullet only removes itself.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBullet : MonoBehaviour
    {
        private const float MaxLifetime = 10f; // safety net; the original relied on the walls

        private static readonly List<EnemyBullet> Live = new List<EnemyBullet>();

        public Enemy Owner { get; private set; }

        private Rigidbody2D rb;
        private float age;
        private bool dead;

        public static int LiveCount => Live.Count;

        public static EnemyBullet Spawn(EnemyBullet prefab, Vector2 position, Vector2 direction, float speed, Enemy owner)
        {
            EnemyBullet b = Instantiate(prefab, position, Quaternion.identity);
            b.Owner = owner;
            b.rb.position = position;
            b.rb.linearVelocity = direction.normalized * speed;
            Live.Add(b);
            Sound.Play(Sfx.EnemyShoot); // rate-limited, so a ring of 20 is one sound
            return b;
        }

        private void Awake() => rb = GetComponent<Rigidbody2D>();

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= MaxLifetime) Kill();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            int layer = other.gameObject.layer;
            if (layer == GameLayers.Get(GameLayers.Player) || layer == GameLayers.Get(GameLayers.Wall)) Kill();
        }

        public void Kill()
        {
            if (dead) return;
            dead = true;
            Live.Remove(this);
            Destroy(gameObject);
        }

        private void OnDestroy() => Live.Remove(this);

        /// <summary>Remove every bullet fired by this enemy (it was shot down).</summary>
        public static void ClearOwner(Enemy owner)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].Owner == owner) Live[i].Kill();
        }

        /// <summary>Remove every enemy bullet (level over).</summary>
        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Live[i].Kill();
            Live.Clear();
        }
    }
}
