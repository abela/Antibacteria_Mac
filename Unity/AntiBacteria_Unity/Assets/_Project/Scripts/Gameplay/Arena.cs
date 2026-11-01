using AntiBacteria.Core;
using UnityEngine;

namespace AntiBacteria.Gameplay
{
    /// <summary>
    /// The playable field: a rectangle centred on this transform, closed by four solid walls.
    /// Original: bg.jpg 2176x1536 pt inset by 20 pt (Docs 01, section 10); here 432 x 304 px.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        [Tooltip("Playable size in units (16 px each). 27 x 19 = 432 x 304 px.")]
        [SerializeField] private Vector2 size = new Vector2(27f, 19f);

        [SerializeField] private float wallThickness = 4f;

        public static Arena Current { get; private set; }

        public Rect Bounds => new Rect((Vector2)transform.position - size * 0.5f, size);

        private void Awake()
        {
            Current = this;
            BuildWalls();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void BuildWalls()
        {
            if (transform.Find("Walls") != null) return;
            var walls = new GameObject("Walls").transform;
            walls.SetParent(transform, false);
            float t = wallThickness, w = size.x, h = size.y;
            AddWall(walls, "Top", new Vector2(0f, (h + t) * 0.5f), new Vector2(w + 2f * t, t));
            AddWall(walls, "Bottom", new Vector2(0f, -(h + t) * 0.5f), new Vector2(w + 2f * t, t));
            AddWall(walls, "Left", new Vector2(-(w + t) * 0.5f, 0f), new Vector2(t, h));
            AddWall(walls, "Right", new Vector2((w + t) * 0.5f, 0f), new Vector2(t, h));
        }

        private static void AddWall(Transform parent, string name, Vector2 localPos, Vector2 boxSize)
        {
            var go = new GameObject(name);
            go.layer = GameLayers.Get(GameLayers.Wall);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = boxSize;
        }

        /// <summary>Uniform random point inside the field, kept <paramref name="margin"/> units from the walls.</summary>
        public Vector2 RandomPointInside(float margin)
        {
            Rect r = Bounds;
            return new Vector2(Random.Range(r.xMin + margin, r.xMax - margin), Random.Range(r.yMin + margin, r.yMax - margin));
        }

        /// <summary>Random point on the field border (original SpawnManager.getValidOuterSpawn: random side,
        /// random point along it), <paramref name="inset"/> units inside the walls.</summary>
        public Vector2 RandomPointOnBorder(float inset)
        {
            Rect r = Bounds;
            float xMin = r.xMin + inset, xMax = r.xMax - inset, yMin = r.yMin + inset, yMax = r.yMax - inset;
            switch (Random.Range(0, 4))
            {
                case 0: return new Vector2(Random.Range(xMin, xMax), yMax);
                case 1: return new Vector2(Random.Range(xMin, xMax), yMin);
                case 2: return new Vector2(xMin, Random.Range(yMin, yMax));
                default: return new Vector2(xMax, Random.Range(yMin, yMax));
            }
        }

        public Vector2 Clamp(Vector2 p, float margin)
        {
            Rect r = Bounds;
            return new Vector2(Mathf.Clamp(p.x, r.xMin + margin, r.xMax - margin), Mathf.Clamp(p.y, r.yMin + margin, r.yMax - margin));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
