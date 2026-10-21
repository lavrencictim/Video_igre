using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Lightweight pooled sprite particles: sparks, dust, impact rings, headshot stars.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        static Fx instance;

        class Particle
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float life, maxLife, gravity, drag, startSize, endSize, spin;
            public Color color;
            public bool active;
        }

        readonly List<Particle> particles = new List<Particle>();

        public static void Init(Transform parent)
        {
            if (instance != null) return;
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<Fx>();
        }

        Particle Get(Sprite sprite)
        {
            foreach (var p in particles)
                if (!p.active)
                {
                    p.sr.sprite = sprite;
                    p.t.gameObject.SetActive(true);
                    p.active = true;
                    return p;
                }
            var sr = Art.MakeRenderer("p", transform, sprite, Color.white, 40);
            var np = new Particle { t = sr.transform, sr = sr, active = true };
            particles.Add(np);
            return np;
        }

        static void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, Color c, float size, float endSize, float life,
                          float gravity, float drag, float spin, int order)
        {
            if (instance == null) return;
            var p = instance.Get(sprite);
            p.t.position = new Vector3(pos.x, pos.y, 0f);
            p.t.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            p.vel = vel;
            p.color = c;
            p.startSize = size;
            p.endSize = endSize;
            p.life = 0f;
            p.maxLife = life;
            p.gravity = gravity;
            p.drag = drag;
            p.spin = spin;
            p.sr.sortingOrder = order;
            p.sr.color = c;
            p.t.localScale = Vector3.one * size;
        }

        /// <summary>Spray of particles. direction/spread in degrees (spread 360 = all around).</summary>
        public static void Burst(Vector2 pos, Color color, int count, float minSpeed, float maxSpeed, float size,
                                 float life, float gravity = 0f, float direction = 90f, float spread = 360f,
                                 Sprite sprite = null, float drag = 2f)
        {
            if (sprite == null) sprite = Art.SoftCircle;
            for (int i = 0; i < count; i++)
            {
                float a = (direction + Random.Range(-spread * 0.5f, spread * 0.5f)) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(minSpeed, maxSpeed);
                float s = size * Random.Range(0.6f, 1.3f);
                Color c = color;
                c.r *= Random.Range(0.85f, 1.1f);
                c.g *= Random.Range(0.85f, 1.1f);
                Spawn(sprite, pos, v, c, s, s * 0.2f, life * Random.Range(0.7f, 1.2f), gravity, drag, Random.Range(-400f, 400f), 40);
            }
        }

        public static void Ring(Vector2 pos, Color color, float endSize, float life)
        {
            Spawn(Art.Ring, pos, Vector2.zero, color, 0.1f, endSize, life, 0f, 0f, 0f, 41);
        }

        public static void Flash(Vector2 pos, Color color, float size, float life)
        {
            Spawn(Art.SoftCircle, pos, Vector2.zero, color, size, size * 1.6f, life, 0f, 0f, 0f, 39);
        }

        public static void Dust(Vector2 pos, float amount = 1f)
        {
            Burst(pos + Vector2.up * 0.05f, new Color(0.78f, 0.66f, 0.5f, 0.55f), Mathf.CeilToInt(4 * amount), 0.4f, 1.6f * amount,
                  0.45f * amount, 0.55f, -0.6f, 90f, 150f);
        }

        public static void BodyHit(Vector2 pos, Vector2 arrowDir)
        {
            float dir = Mathf.Atan2(arrowDir.y, arrowDir.x) * Mathf.Rad2Deg;
            Burst(pos, new Color(1f, 0.92f, 0.6f), 10, 3f, 7f, 0.13f, 0.25f, 0f, dir, 70f, Art.Square(), 6f);   // sparks
            Burst(pos, new Color(0.85f, 0.12f, 0.12f), 12, 1.5f, 5f, 0.18f, 0.5f, 12f, dir + 180f, 160f);     // red splash
            Ring(pos, new Color(1f, 1f, 1f, 0.8f), 1.2f, 0.25f);
            Flash(pos, new Color(1f, 0.85f, 0.6f, 0.8f), 0.8f, 0.12f);
            CameraRig.Shake(0.18f);
        }

        public static void HeadHit(Vector2 pos, Vector2 arrowDir)
        {
            float dir = Mathf.Atan2(arrowDir.y, arrowDir.x) * Mathf.Rad2Deg;
            Burst(pos, new Color(1f, 0.85f, 0.2f), 14, 2f, 7f, 0.38f, 0.8f, 6f, 90f, 360f, Art.Star, 3f);       // golden stars
            Burst(pos, new Color(1f, 1f, 0.8f), 16, 5f, 11f, 0.12f, 0.3f, 0f, dir, 90f, Art.Square(), 7f);      // sparks
            Burst(pos, new Color(0.85f, 0.1f, 0.1f), 16, 2f, 6f, 0.2f, 0.6f, 12f, dir + 180f, 200f);
            Ring(pos, new Color(1f, 0.9f, 0.3f, 0.95f), 2.6f, 0.35f);
            Ring(pos, new Color(1f, 1f, 1f, 0.8f), 1.4f, 0.2f);
            Flash(pos, new Color(1f, 0.95f, 0.6f, 0.9f), 1.8f, 0.18f);
            CameraRig.Shake(0.45f);
        }

        public static void GroundHit(Vector2 pos)
        {
            Burst(pos, new Color(0.7f, 0.56f, 0.4f, 0.8f), 7, 0.6f, 2.4f, 0.28f, 0.5f, 3f, 90f, 120f);
            Burst(pos, new Color(0.45f, 0.35f, 0.25f), 5, 2f, 4f, 0.07f, 0.4f, 14f, 90f, 90f, Art.Square(), 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in particles)
            {
                if (!p.active) continue;
                p.life += dt;
                float k = p.life / p.maxLife;
                if (k >= 1f)
                {
                    p.active = false;
                    p.t.gameObject.SetActive(false);
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                p.vel *= 1f / (1f + p.drag * dt);
                p.t.position += (Vector3)(p.vel * dt);
                p.t.Rotate(0, 0, p.spin * dt);
                p.t.localScale = Vector3.one * Mathf.Lerp(p.startSize, p.endSize, k);
                Color c = p.color;
                c.a *= 1f - k * k;
                p.sr.color = c;
            }
        }
    }
}
