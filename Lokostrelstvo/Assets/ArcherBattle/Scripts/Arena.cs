using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Builds the medieval arena out of generated sprites: sunset sky, mountains, castle wall with towers,
    /// banners and torches, a cheering crowd, and a sand floor. Also animates the ambient details.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        public const float PlayerMinX = -13.5f, PlayerMaxX = -3.5f;
        public const float AIMinX = 3.5f, AIMaxX = 13.5f;

        class Spectator { public Transform t; public float baseY, phase, speed; }
        class Flicker { public Transform t; public SpriteRenderer sr; public float baseScale, seed; }
        class Cloud { public Transform t; public float speed; }
        class Wave { public Transform t; public Vector3 baseScale; public float seed; }

        readonly List<Spectator> crowd = new List<Spectator>();
        readonly List<Flicker> flames = new List<Flicker>();
        readonly List<Cloud> clouds = new List<Cloud>();
        readonly List<Wave> banners = new List<Wave>();
        float cheer;

        public static Arena Build()
        {
            var a = new GameObject("Arena").AddComponent<Arena>();
            a.Construct();
            return a;
        }

        public void Cheer(float amount) => cheer = Mathf.Max(cheer, amount);

        SpriteRenderer Box(string n, Sprite s, Color c, int order, Vector2 pos, Vector2 size, float rot = 0f)
            => Art.Box(n, transform, s, c, order, pos, size, rot);

        SpriteRenderer Tiled(string n, Sprite s, Color c, int order, Vector2 pos, Vector2 size)
        {
            var sr = Art.MakeRenderer(n, transform, s, c, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.transform.localPosition = pos;
            return sr;
        }

        void Construct()
        {
            var rnd = new System.Random(7);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var sq = Art.Square();
            var sqBottom = Art.Square(new Vector2(0.5f, 0f));

            // --- sky, sun, mountains, clouds
            Box("Sky", Art.SkyGradient, Color.white, -100, new Vector2(0, 12), new Vector2(140, 34));
            Box("SunGlow", Art.SoftCircle, new Color(1f, 0.85f, 0.55f, 0.55f), -99, new Vector2(9, 7.5f), new Vector2(11, 11));
            Box("Sun", Art.Circle, new Color(1f, 0.93f, 0.75f), -99, new Vector2(9, 7.5f), new Vector2(2.6f, 2.6f));
            Box("MountainsFar", Art.Hills(3), new Color(0.55f, 0.58f, 0.75f), -96, new Vector2(0, 1.5f), new Vector2(120, 9));
            Box("MountainsNear", Art.Hills(11), new Color(0.38f, 0.45f, 0.55f), -95, new Vector2(4, 1.5f), new Vector2(110, 6.5f));
            for (int i = 0; i < 9; i++)
            {
                var c = Box("Cloud" + i, Art.Cloud, new Color(1f, 0.96f, 0.92f, R(0.6f, 0.9f)), -94,
                            new Vector2(R(-40, 40), R(9, 17)), Vector2.one);
                float sc = R(1.0f, 2.2f);
                c.transform.localScale = new Vector3(sc, sc * R(0.8f, 1f), 1);
                clouds.Add(new Cloud { t = c.transform, speed = R(0.15f, 0.5f) });
            }

            // --- castle wall behind the arena
            Color wallTint = new Color(0.72f, 0.7f, 0.74f);
            Tiled("Wall", Art.Bricks, wallTint, -60, new Vector2(0, 3.2f), new Vector2(80, 6.4f));
            for (float x = -40f; x < 40f; x += 1.2f)
                Box("Merlon", sq, wallTint * 0.95f, -60, new Vector2(x, 6.75f), new Vector2(0.7f, 0.7f));
            Box("WallTopShade", sq, new Color(0, 0, 0, 0.2f), -59, new Vector2(0, 6.3f), new Vector2(80, 0.2f));

            // gate in the middle
            Box("GateArch", Art.Circle, new Color(0.14f, 0.1f, 0.1f), -58, new Vector2(0, 3.0f), new Vector2(3.2f, 3.2f));
            Box("GateBody", sqBottom, new Color(0.14f, 0.1f, 0.1f), -58, new Vector2(0, 0f), new Vector2(3.2f, 3.0f));
            for (int i = -3; i <= 3; i++)
                Box("Bar", sqBottom, new Color(0.35f, 0.33f, 0.32f), -57, new Vector2(i * 0.42f, 0f), new Vector2(0.08f, 4.2f + (3 - Mathf.Abs(i)) * 0.1f));
            for (int i = 0; i < 5; i++)
                Box("BarH", sq, new Color(0.35f, 0.33f, 0.32f), -57, new Vector2(0, 0.6f + i * 0.85f), new Vector2(3f, 0.07f));

            // towers at both edges (arrows stick into them)
            BuildTower(-17.8f, new Color(0.2f, 0.35f, 0.75f));
            BuildTower(17.8f, new Color(0.78f, 0.18f, 0.16f));
            BuildTower(-30f, new Color(0.2f, 0.35f, 0.75f));
            BuildTower(30f, new Color(0.78f, 0.18f, 0.16f));

            // banners on the wall: blue on the player's side, red on the AI side
            for (int i = 0; i < 8; i++)
            {
                float x = -14f + i * 4f;
                if (Mathf.Abs(x) < 2.5f) continue;
                Color c = x < 0 ? new Color(0.2f, 0.38f, 0.82f) : new Color(0.82f, 0.18f, 0.16f);
                var b = Box("Banner", Art.Banner, c, -55, new Vector2(x, 6.1f), new Vector2(1.0f, 2.8f));
                Art.Box("Emblem", b.transform, Art.Star, new Color(1f, 0.85f, 0.3f), -54, new Vector2(0, -1.0f), new Vector2(0.5f, 0.5f));
                Box("BannerRod", sq, new Color(0.3f, 0.2f, 0.1f), -54, new Vector2(x, 6.12f), new Vector2(1.25f, 0.1f));
                banners.Add(new Wave { t = b.transform, baseScale = b.transform.localScale, seed = R(0, 10) });
            }

            // torches between banners
            for (int i = 0; i < 7; i++)
            {
                float x = -12f + i * 4f;
                if (Mathf.Abs(x) < 2.5f) continue;
                Box("TorchStick", sq, new Color(0.3f, 0.2f, 0.12f), -54, new Vector2(x, 4.1f), new Vector2(0.14f, 0.7f), 0f);
                Box("TorchCup", Art.Triangle, new Color(0.25f, 0.25f, 0.27f), -54, new Vector2(x, 4.75f), new Vector2(0.4f, 0.3f), 180f);
                var glow = Box("TorchGlow", Art.SoftCircle, new Color(1f, 0.6f, 0.2f, 0.35f), -55, new Vector2(x, 4.95f), new Vector2(2.4f, 2.4f));
                var flame = Box("Flame", Art.SoftCircle, new Color(1f, 0.75f, 0.25f), -53, new Vector2(x, 4.95f), new Vector2(0.5f, 0.75f));
                var core = Box("FlameCore", Art.SoftCircle, new Color(1f, 0.97f, 0.7f), -52, new Vector2(x, 4.9f), new Vector2(0.25f, 0.4f));
                flames.Add(new Flicker { t = glow.transform, sr = glow, baseScale = glow.transform.localScale.x, seed = R(0, 100) });
                flames.Add(new Flicker { t = flame.transform, sr = flame, baseScale = flame.transform.localScale.x, seed = R(0, 100) });
                flames.Add(new Flicker { t = core.transform, sr = core, baseScale = core.transform.localScale.x, seed = R(0, 100) });
            }

            // --- crowd behind a wooden barrier
            Color[] shirts =
            {
                new Color(0.7f, 0.2f, 0.2f), new Color(0.25f, 0.4f, 0.75f), new Color(0.3f, 0.6f, 0.3f), new Color(0.8f, 0.65f, 0.25f),
                new Color(0.55f, 0.3f, 0.6f), new Color(0.6f, 0.45f, 0.3f), new Color(0.85f, 0.85f, 0.8f)
            };
            Color[] skins = { new Color(0.96f, 0.8f, 0.65f), new Color(0.85f, 0.65f, 0.48f), new Color(0.6f, 0.42f, 0.3f), new Color(0.4f, 0.28f, 0.2f) };
            for (int row = 0; row < 2; row++)
            {
                for (float x = -16f; x <= 16f; x += R(0.55f, 0.85f))
                {
                    if (Mathf.Abs(x) < 2f) continue;
                    var root = new GameObject("Spectator").transform;
                    root.SetParent(transform, false);
                    float baseY = 1.25f + row * 0.45f;
                    root.localPosition = new Vector3(x + row * 0.3f, baseY, 0);
                    int order = -45 + (1 - row) * 2;
                    float shade = row == 1 ? 0.8f : 1f;
                    Art.Box("body", root, Art.RoundedRect(false), shirts[rnd.Next(shirts.Length)] * shade, order, new Vector2(0, 0f), new Vector2(0.48f, 0.7f));
                    Art.Box("head", root, Art.Circle, skins[rnd.Next(skins.Length)] * shade, order + 1, new Vector2(0, 0.52f), new Vector2(0.36f, 0.36f));
                    if (rnd.NextDouble() < 0.3)
                        Art.Box("hat", root, Art.Triangle, shirts[rnd.Next(shirts.Length)] * 0.8f * shade, order + 1, new Vector2(0, 0.62f), new Vector2(0.4f, 0.3f));
                    crowd.Add(new Spectator { t = root, baseY = baseY, phase = R(0, 10), speed = R(2f, 4f) });
                }
            }
            Box("Barrier", sqBottom, new Color(0.45f, 0.29f, 0.16f), -40, new Vector2(0, 0f), new Vector2(80, 1.25f));
            Box("BarrierTop", sq, new Color(0.35f, 0.22f, 0.12f), -39, new Vector2(0, 1.22f), new Vector2(80, 0.14f));
            for (float x = -40f; x <= 40f; x += 2f)
                Box("Post", sqBottom, new Color(0.33f, 0.2f, 0.1f), -39, new Vector2(x, 0f), new Vector2(0.18f, 1.3f));
            for (float x = -39f; x <= 40f; x += 0.9f)
                Box("Plank", sq, new Color(0, 0, 0, 0.12f), -39, new Vector2(x, 0.6f), new Vector2(0.04f, 1.2f));

            // decorations: archery targets and hay bales near the walls
            BuildTarget(-15.3f);
            BuildTarget(15.3f);
            Box("Hay1", Art.RoundedRect(false), new Color(0.88f, 0.75f, 0.35f), -8, new Vector2(-14.6f, 0.35f), new Vector2(1.3f, 0.75f));
            Box("Hay2", Art.RoundedRect(false), new Color(0.88f, 0.75f, 0.35f), -8, new Vector2(14.6f, 0.35f), new Vector2(1.3f, 0.75f));

            // --- arena floor
            Tiled("Floor", Art.Dirt, Color.white, -10, new Vector2(0, -4f), new Vector2(100, 8f));
            Box("FloorEdge", sq, new Color(0.45f, 0.32f, 0.2f), -9, new Vector2(0, -0.04f), new Vector2(100, 0.12f));
            Box("FloorLight", sq, new Color(1f, 0.9f, 0.7f, 0.15f), -9, new Vector2(0, -0.3f), new Vector2(100, 0.4f));
            var grassL = Art.MakeRenderer("Grass", transform, Art.GrassStrip, Color.white, -9);
            grassL.drawMode = SpriteDrawMode.Tiled;
            grassL.size = new Vector2(100, 0.4f);
            grassL.transform.localPosition = new Vector3(0, -0.05f, 0);

            // centre line + side zone markers
            Box("CenterStake", sqBottom, new Color(0.9f, 0.85f, 0.75f), -8, new Vector2(0, -0.2f), new Vector2(0.12f, 0.9f));
            Box("CenterFlag", Art.Triangle, new Color(1f, 0.85f, 0.3f), -8, new Vector2(0.28f, 0.65f), new Vector2(0.3f, 0.45f), -90f);
            for (int i = 0; i < 2; i++)
            {
                float minX = i == 0 ? PlayerMinX : AIMinX, maxX = i == 0 ? PlayerMaxX : AIMaxX;
                Color c = i == 0 ? new Color(0.3f, 0.5f, 1f, 0.25f) : new Color(1f, 0.3f, 0.25f, 0.25f);
                Box("Zone", sq, c, -9, new Vector2((minX + maxX) / 2, -0.12f), new Vector2(maxX - minX + 0.6f, 0.08f));
            }
            for (int i = 0; i < 40; i++)
                Box("Pebble", Art.Circle, new Color(0.4f, 0.33f, 0.27f, 0.7f), -9, new Vector2(R(-30, 30), R(-1.6f, -0.3f)), new Vector2(R(0.1f, 0.25f), R(0.07f, 0.14f)));

            // soft vignette at the bottom
            Box("BottomShade", Art.Square(new Vector2(0.5f, 1f)), new Color(0, 0, 0, 0.25f), -8, new Vector2(0, -2.5f), new Vector2(100, 6f));
        }

        void BuildTower(float x, Color roof)
        {
            Color stone = new Color(0.66f, 0.64f, 0.68f);
            Tiled("Tower", Art.Bricks, stone, -50, new Vector2(x, 4.5f), new Vector2(3.6f, 9f));
            for (int i = 0; i < 4; i++)
                Box("TowerMerlon", Art.Square(), stone * 0.95f, -50, new Vector2(x - 1.35f + i * 0.9f, 9.25f), new Vector2(0.55f, 0.55f));
            Box("Roof", Art.Triangle, roof, -49, new Vector2(x, 9.5f), new Vector2(4.2f, 3.2f));
            Box("RoofShade", Art.Triangle, new Color(0, 0, 0, 0.18f), -48, new Vector2(x + 0.5f, 9.5f), new Vector2(2.1f, 2.6f));
            Box("Window", Art.RoundedRect(false), new Color(0.12f, 0.09f, 0.08f), -49, new Vector2(x, 6.4f), new Vector2(0.55f, 1.1f));
            Box("WindowGlow", Art.SoftCircle, new Color(1f, 0.7f, 0.3f, 0.4f), -48, new Vector2(x, 6.3f), new Vector2(0.6f, 0.9f));
            Box("Flagpole", Art.Square(new Vector2(0.5f, 0f)), new Color(0.3f, 0.2f, 0.1f), -49, new Vector2(x, 12.6f), new Vector2(0.08f, 1.6f));
            var flag = Box("Flag", Art.Triangle, roof, -49, new Vector2(x + 0.02f, 13.85f), new Vector2(0.6f, 1.0f), -90f);
            banners.Add(new Wave { t = flag.transform, baseScale = flag.transform.localScale, seed = x });
        }

        void BuildTarget(float x)
        {
            Box("TargetLegA", Art.Square(new Vector2(0.5f, 0f)), new Color(0.4f, 0.26f, 0.14f), -8, new Vector2(x - 0.35f, 0f), new Vector2(0.12f, 1.6f), 12f);
            Box("TargetLegB", Art.Square(new Vector2(0.5f, 0f)), new Color(0.4f, 0.26f, 0.14f), -8, new Vector2(x + 0.35f, 0f), new Vector2(0.12f, 1.6f), -12f);
            Box("TargetFace", Art.TargetFace, Color.white, -7, new Vector2(x, 1.35f), new Vector2(1.2f, 1.2f));
        }

        void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            cheer = Mathf.MoveTowards(cheer, 0f, dt * 0.6f);

            foreach (var c in clouds)
            {
                var p = c.t.localPosition;
                p.x += c.speed * dt;
                if (p.x > 45f) p.x = -45f;
                c.t.localPosition = p;
            }
            foreach (var f in flames)
            {
                float n = Mathf.PerlinNoise(f.seed, t * 6f);
                float s = f.baseScale * (0.8f + 0.4f * n);
                f.t.localScale = new Vector3(s, f.t.localScale.y / Mathf.Max(0.0001f, f.t.localScale.x) * s, 1);
            }
            foreach (var b in banners)
            {
                float w = Mathf.Sin(t * 2.3f + b.seed) * 0.08f + Mathf.Sin(t * 5.1f + b.seed * 2f) * 0.03f;
                b.t.localScale = new Vector3(b.baseScale.x * (1f + w), b.baseScale.y, 1);
            }
            foreach (var s in crowd)
            {
                float idle = Mathf.Abs(Mathf.Sin(t * s.speed + s.phase)) * 0.06f;
                float jump = Mathf.Abs(Mathf.Sin(t * (s.speed * 3f) + s.phase)) * 0.45f * cheer;
                var p = s.t.localPosition;
                p.y = s.baseY + idle + jump;
                s.t.localPosition = p;
            }
        }
    }
}
