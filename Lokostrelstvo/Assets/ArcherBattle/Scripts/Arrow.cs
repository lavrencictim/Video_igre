using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Ballistic arrow. Integrates gravity itself, points along its velocity and
    /// sweeps a segment each frame against the archers' head/body shapes so even fast arrows never tunnel.
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        public const float Gravity = 12f;
        public const float WorldLimitX = 16.2f;   // castle towers at the arena edges

        public static readonly List<Arrow> All = new List<Arrow>();

        public Archer Shooter { get; private set; }
        public Vector2 Velocity => velocity;
        public bool Flying => flying;
        public Vector2 Tip => transform.position;

        Vector2 velocity;
        bool flying;
        float stuckTime;
        SpriteRenderer sr;
        TrailRenderer trail;

        public static Arrow Spawn(Archer shooter, Vector2 tipPosition, Vector2 velocity)
        {
            var go = new GameObject("Arrow");
            var arrow = go.AddComponent<Arrow>();
            arrow.Shooter = shooter;
            arrow.velocity = velocity;
            arrow.flying = true;
            go.transform.position = tipPosition;
            arrow.sr = Art.MakeRenderer("sprite", go.transform, Art.Arrow, Color.white, 25);
            arrow.trail = go.AddComponent<TrailRenderer>();
            arrow.trail.time = 0.18f;
            arrow.trail.startWidth = 0.07f;
            arrow.trail.endWidth = 0f;
            arrow.trail.minVertexDistance = 0.05f;
            arrow.trail.startColor = new Color(1f, 1f, 1f, 0.55f);
            arrow.trail.endColor = new Color(1f, 1f, 1f, 0f);
            arrow.trail.sortingOrder = 24;
            arrow.trail.numCapVertices = 2;
            Art.ApplyMaterial(arrow.trail);
            arrow.Orient();
            All.Add(arrow);
            return arrow;
        }

        void OnDestroy() => All.Remove(this);

        public static void ClearAll()
        {
            for (int i = All.Count - 1; i >= 0; i--)
                if (All[i] != null) Destroy(All[i].gameObject);
            All.Clear();
        }

        void Orient()
        {
            float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, ang);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!flying)
            {
                stuckTime += dt;
                if (stuckTime > 6f)
                {
                    float a = Mathf.Clamp01(1f - (stuckTime - 6f));
                    sr.color = new Color(1, 1, 1, a);
                    if (a <= 0f) Destroy(gameObject);
                }
                return;
            }

            // Sub-step for accurate arcs and collision.
            int steps = Mathf.Max(1, Mathf.CeilToInt(velocity.magnitude * dt / 0.25f));
            float h = dt / steps;
            for (int s = 0; s < steps && flying; s++)
            {
                Vector2 from = transform.position;
                velocity.y -= Gravity * h;
                Vector2 to = from + velocity * h;
                Step(from, to);
            }
        }

        void Step(Vector2 from, Vector2 to)
        {
            // 1) archers
            Archer bestTarget = null;
            float bestT = 2f;
            bool bestHead = false;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                foreach (var target in gm.Archers)
                {
                    if (target == null || target == Shooter || !target.Alive) continue;
                    if (target.Raycast(from, to, out float t, out bool head) && t < bestT)
                    {
                        bestT = t;
                        bestTarget = target;
                        bestHead = head;
                    }
                }
            }

            // 2) ground (y = 0) and arena walls
            float groundT = 2f;
            if (to.y <= 0f && from.y > 0f) groundT = from.y / (from.y - to.y);
            float wallT = 2f;
            if (Mathf.Abs(to.x) >= WorldLimitX && Mathf.Abs(from.x) < WorldLimitX)
                wallT = (Mathf.Sign(to.x) * WorldLimitX - from.x) / (to.x - from.x);

            if (bestTarget != null && bestT <= groundT && bestT <= wallT)
            {
                Vector2 p = Vector2.Lerp(from, to, bestT);
                HitArcher(bestTarget, p, bestHead);
                return;
            }
            if (groundT <= 1f || wallT <= 1f)
            {
                Vector2 p = Vector2.Lerp(from, to, Mathf.Min(groundT, wallT));
                transform.position = p + velocity.normalized * 0.22f; // bury the tip a little
                Orient();
                Stick(null);
                Fx.GroundHit(p);
                Sfx.Play(Sfx.HitGround, 0.55f, 0.1f);
                return;
            }
            if (to.y < -10f || Mathf.Abs(to.x) > 60f) { Destroy(gameObject); return; }

            transform.position = to;
            Orient();
        }

        void HitArcher(Archer target, Vector2 point, bool head)
        {
            transform.position = point + velocity.normalized * 0.3f;
            Orient();
            Vector2 dir = velocity.normalized;
            Stick(target.Visual);
            sr.sortingOrder = head ? 19 : 17;
            target.TakeHit(head ? 40 : 20, head, point, dir, Shooter);
        }

        void Stick(Transform parent)
        {
            flying = false;
            velocity = Vector2.zero;
            if (trail != null) trail.emitting = false;
            if (parent != null) transform.SetParent(parent, true);
            else sr.sortingOrder = -11;   // behind the ground so the buried tip is hidden
        }
    }
}
