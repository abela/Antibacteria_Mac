using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Gameplay.Enemies
{
    /// <summary>
    /// Chorux (ID 11): a homing cell guarded by two ChoruxDefenders (ID 12). The defenders block shots and can't be
    /// hurt until the Chorux core is hit; then the core vanishes (keeps drifting, harmless) and the defenders open up.
    /// When the last defender dies the core is gone for good. Docs/DesignReference/02, sections 3.9-3.10.
    /// Fix: the original never paid Chorux's 200 points; they are paid when the core breaks.
    /// </summary>
    public class Chorux : Enemy
    {
        public const int DefenderId = 12;
        public const int DefenderCount = 2;

        private readonly List<Enemy> defenders = new List<Enemy>();

        public bool CoreBroken { get; private set; }

        public override void Init(EnemyDefinition definition)
        {
            base.Init(definition);
            for (int i = 0; i < DefenderCount; i++)
            {
                GameObject go = EnemyCatalog.SpawnById(DefenderId, transform.position);
                Enemy d = go != null ? go.GetComponent<Enemy>() : null;
                if (d == null) continue;
                d.Invulnerable = true;
                defenders.Add(d);
            }
        }

        protected override void OnLethalHit()
        {
            if (CoreBroken) return;
            CoreBroken = true;
            PayOut();
            Hide();
            foreach (Enemy d in defenders)
            {
                if (d == null) continue;
                d.Invulnerable = false;
                d.UseAltSprite();
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!CoreBroken) return;
            defenders.RemoveAll(d => d == null);
            if (defenders.Count == 0) Despawn(); // already paid out
        }

        protected override void Die(bool killedByPlayer)
        {
            // Level end / restart: take the bodyguards along.
            if (!killedByPlayer) foreach (Enemy d in defenders) if (d != null) d.Despawn();
            base.Die(killedByPlayer);
        }
    }
}
