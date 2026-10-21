using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Generates every sprite the game uses at runtime, so the project needs no imported art.
    /// </summary>
    public static class Art
    {
        static Material spriteMaterial;
        static bool materialResolved;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>Unlit sprite material (so nothing depends on 2D lights). Null if no shader was found.</summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (!materialResolved)
                {
                    materialResolved = true;
                    Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (s == null) s = Shader.Find("Sprites/Default");
                    if (s != null) spriteMaterial = new Material(s);
                }
                return spriteMaterial;
            }
        }

        public static void ApplyMaterial(Renderer r)
        {
            if (SpriteMaterial != null) r.sharedMaterial = SpriteMaterial;
        }

        // ------------------------------------------------------------------ helpers

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Sprite Finish(string key, Texture2D t, Color[] px, Vector2 pivot, float ppu, Vector4 border = default)
        {
            t.SetPixels(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu, 0, SpriteMeshType.FullRect, border);
            s.name = key;
            cache[key] = s;
            return s;
        }

        static bool TryGet(string key, out Sprite s) => cache.TryGetValue(key, out s) && s != null;

        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>Scale a sprite renderer so it covers exactly w x h world units.</summary>
        public static void SetSize(SpriteRenderer sr, float w, float h)
        {
            Vector2 b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
        }

        public static SpriteRenderer MakeRenderer(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            ApplyMaterial(sr);
            return sr;
        }

        public static SpriteRenderer Box(string name, Transform parent, Sprite sprite, Color color, int order,
                                         Vector2 localPos, Vector2 size, float rotation = 0f)
        {
            var sr = MakeRenderer(name, parent, sprite, color, order);
            sr.transform.localPosition = localPos;
            sr.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            SetSize(sr, size.x, size.y);
            return sr;
        }

        // ------------------------------------------------------------------ basic shapes

        /// <summary>White square, 1x1 unit, with the given pivot.</summary>
        public static Sprite Square(Vector2 pivot)
        {
            string key = "sq" + pivot;
            if (TryGet(key, out var s)) return s;
            var t = NewTex(8, 8);
            var px = new Color[64];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            return Finish(key, t, px, pivot, 8);
        }

        public static Sprite Square() => Square(new Vector2(0.5f, 0.5f));

        /// <summary>Anti-aliased white circle, 1 unit diameter.</summary>
        public static Sprite Circle
        {
            get
            {
                if (TryGet("circle", out var s)) return s;
                const int n = 128;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                float r = n * 0.5f - 1f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                        px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f));
                    }
                return Finish("circle", t, px, new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Soft radial blob for glows, smoke and particles. 1 unit diameter.</summary>
        public static Sprite SoftCircle
        {
            get
            {
                if (TryGet("soft", out var s)) return s;
                const int n = 64;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                        float a = Mathf.Clamp01(1f - d);
                        px[y * n + x] = new Color(1, 1, 1, a * a);
                    }
                return Finish("soft", t, px, new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Thin ring used for impact shockwaves. 1 unit diameter.</summary>
        public static Sprite Ring
        {
            get
            {
                if (TryGet("ring", out var s)) return s;
                const int n = 128;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                float r = n * 0.5f - 2f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                        float a = Mathf.Clamp01(1f - Mathf.Abs(d - (r - 4f)) / 4f);
                        px[y * n + x] = new Color(1, 1, 1, a);
                    }
                return Finish("ring", t, px, new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Upward pointing triangle, 1x1 unit, pivot at bottom centre.</summary>
        public static Sprite Triangle
        {
            get
            {
                if (TryGet("tri", out var s)) return s;
                const int n = 64;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float cov = 0f;
                        for (int sy = 0; sy < 3; sy++)
                            for (int sx = 0; sx < 3; sx++)
                            {
                                float fx = (x + (sx + 0.5f) / 3f) / n, fy = (y + (sy + 0.5f) / 3f) / n;
                                float half = 0.5f * (1f - fy);
                                if (Mathf.Abs(fx - 0.5f) <= half) cov += 1f / 9f;
                            }
                        px[y * n + x] = new Color(1, 1, 1, cov);
                    }
                return Finish("tri", t, px, new Vector2(0.5f, 0f), n);
            }
        }

        /// <summary>Five pointed star, 1 unit across.</summary>
        public static Sprite Star
        {
            get
            {
                if (TryGet("star", out var s)) return s;
                const int n = 64;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f) / (n * 0.5f);
                        float ang = Mathf.Atan2(p.y, p.x) + Mathf.PI / 2f;
                        float k = Mathf.Cos(5f * ang) * 0.5f + 0.5f;           // 1 at the points
                        float radius = Mathf.Lerp(0.42f, 0.98f, Mathf.Pow(k, 3f));
                        float a = Mathf.Clamp01((radius - p.magnitude) * n * 0.5f);
                        px[y * n + x] = new Color(1, 1, 1, a);
                    }
                return Finish("star", t, px, new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>White rounded rectangle. With sliced=true it has 9-slice borders for UI use.</summary>
        public static Sprite RoundedRect(bool sliced)
        {
            string key = sliced ? "rr_ui" : "rr";
            if (TryGet(key, out var s)) return s;
            const int n = 64;
            const float rad = 18f;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, rad, n - rad), cy = Mathf.Clamp(y + 0.5f, rad, n - rad);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(rad - d + 0.5f));
                }
            Vector4 border = sliced ? new Vector4(22, 22, 22, 22) : Vector4.zero;
            return Finish(key, t, px, new Vector2(0.5f, 0.5f), sliced ? 100 : n, border);
        }

        /// <summary>Capsule limb, pivot at one end so it can rotate around a joint.</summary>
        public static Sprite Limb(Vector2 pivot)
        {
            string key = "limb" + pivot;
            if (TryGet(key, out var s)) return s;
            const int w = 32, h = 32;
            var t = NewTex(w, h);
            var px = new Color[w * h];
            float rad = 10f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, rad + 1, w - rad - 1), cy = Mathf.Clamp(y + 0.5f, rad + 1, h - rad - 1);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(rad - d + 0.5f));
                }
            return Finish(key, t, px, pivot, w);
        }

        // ------------------------------------------------------------------ weapon sprites

        public const float BowHalfHeight = 0.62f;   // world units
        public const float BowBulge = 0.24f;        // how far the grip sits in front of the tips

        /// <summary>Wooden recurve-ish bow facing +X, pivot at the grip.</summary>
        public static Sprite Bow
        {
            get
            {
                if (TryGet("bow", out var s)) return s;
                const float ppu = 128f;
                int margin = 6;
                int bulgePx = Mathf.RoundToInt(BowBulge * ppu);
                int halfPx = Mathf.RoundToInt(BowHalfHeight * ppu);
                int w = bulgePx + margin * 2 + 4, h = halfPx * 2 + margin * 2;
                var t = NewTex(w, h);
                var outline = new float[w * h];
                var fill = new float[w * h];
                for (int i = 0; i <= 600; i++)
                {
                    float tt = i / 300f - 1f;                         // -1..1 along the limbs
                    float cx = margin + 2 + bulgePx * (1f - tt * tt);
                    float cy = h * 0.5f + tt * halfPx;
                    float thick = Mathf.Lerp(2.0f, 5.0f, 1f - Mathf.Abs(tt));
                    Stamp(outline, w, h, cx, cy, thick + 1.6f);
                    Stamp(fill, w, h, cx, cy, thick);
                }
                var px = new Color[w * h];
                Color dark = new Color(0.22f, 0.12f, 0.05f), wood = new Color(0.66f, 0.42f, 0.2f);
                for (int i = 0; i < px.Length; i++)
                {
                    int y = i / w;
                    float grain = 0.9f + 0.1f * Mathf.Sin(y * 0.6f);
                    Color c = Color.Lerp(dark, wood * grain, fill[i]);
                    // leather grip in the middle
                    if (Mathf.Abs(y - h * 0.5f) < 9f && fill[i] > 0.5f) c = Color.Lerp(c, new Color(0.35f, 0.18f, 0.1f), 0.8f);
                    c.a = Mathf.Max(outline[i], fill[i]);
                    px[i] = c;
                }
                float pivotX = (margin + 2 + bulgePx) / (float)w;
                return Finish("bow", t, px, new Vector2(pivotX, 0.5f), ppu);
            }
        }

        static void Stamp(float[] buf, int w, int h, float cx, float cy, float r)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r + 1));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    int i = y * w + x;
                    if (a > buf[i]) buf[i] = a;
                }
        }

        public const float ArrowLength = 0.95f;

        /// <summary>Arrow pointing +X, pivot at the tip.</summary>
        public static Sprite Arrow
        {
            get
            {
                if (TryGet("arrow", out var s)) return s;
                const int w = 152, h = 20;
                var t = NewTex(w, h);
                var px = new Color[w * h];
                Color shaft = new Color(0.78f, 0.6f, 0.38f), steel = new Color(0.78f, 0.8f, 0.86f), steelDark = new Color(0.35f, 0.37f, 0.42f);
                Color feather = new Color(0.92f, 0.22f, 0.18f), featherLight = new Color(1f, 0.95f, 0.9f);
                float mid = h * 0.5f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fy = y + 0.5f, fx = x + 0.5f;
                        Color c = Color.clear;
                        // shaft
                        float sa = Mathf.Clamp01(1.6f - Mathf.Abs(fy - mid));
                        if (fx > 6 && fx < 130 && sa > 0) c = new Color(shaft.r, shaft.g, shaft.b, sa);
                        // fletching (two feathers)
                        if (fx > 4 && fx < 34)
                        {
                            float k = (fx - 4) / 30f;                       // 0 at back, 1 at front
                            float half = Mathf.Lerp(8.5f, 1.5f, k);
                            float d = Mathf.Abs(fy - mid);
                            if (d < half && d > 1.2f)
                            {
                                Color f = (x / 4) % 2 == 0 ? feather : feather * 0.85f;
                                if (fx < 9) f = featherLight;
                                f.a = Mathf.Clamp01(half - d + 0.5f);
                                c = f;
                            }
                        }
                        // nock
                        if (fx <= 6 && Mathf.Abs(fy - mid) < 2.4f) c = new Color(0.2f, 0.15f, 0.1f, 1);
                        // head
                        if (fx >= 126)
                        {
                            float k = (fx - 126) / (w - 126f);
                            float half = Mathf.Lerp(6.5f, 0.3f, k);
                            float d = Mathf.Abs(fy - mid);
                            float a = Mathf.Clamp01(half - d + 0.5f);
                            if (a > 0)
                            {
                                Color st = d < half * 0.4f ? steel : steelDark;
                                st.a = a;
                                c = st;
                            }
                        }
                        px[y * w + x] = c;
                    }
                return Finish("arrow", t, px, new Vector2(1f, 0.5f), w / ArrowLength);
            }
        }

        // ------------------------------------------------------------------ environment

        public static Sprite SkyGradient
        {
            get
            {
                if (TryGet("sky", out var s)) return s;
                const int h = 256;
                var t = NewTex(4, h);
                var px = new Color[4 * h];
                Color top = new Color(0.16f, 0.24f, 0.48f), mid = new Color(0.45f, 0.55f, 0.78f), low = new Color(0.98f, 0.76f, 0.52f);
                for (int y = 0; y < h; y++)
                {
                    float k = y / (h - 1f);
                    Color c = k < 0.45f ? Color.Lerp(low, mid, k / 0.45f) : Color.Lerp(mid, top, (k - 0.45f) / 0.55f);
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
                }
                return Finish("sky", t, px, new Vector2(0.5f, 0.5f), 4);
            }
        }

        /// <summary>Mountain silhouette strip (white, tint it). Pivot bottom centre, 1 unit tall.</summary>
        public static Sprite Hills(int seed)
        {
            string key = "hills" + seed;
            if (TryGet(key, out var s)) return s;
            const int w = 512, h = 128;
            var t = NewTex(w, h);
            var px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float fx = x / (float)w;
                float height = 0.45f
                               + 0.22f * Mathf.Sin(fx * Mathf.PI * 2f * 2f + seed)
                               + 0.12f * Mathf.Sin(fx * Mathf.PI * 2f * 5f + seed * 2.3f)
                               + 0.12f * (Noise(fx * 18f, seed * 7f) - 0.5f);
                height = Mathf.Clamp(height, 0.1f, 0.95f) * h;
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Clamp01(height - y);
                    float shade = 0.85f + 0.15f * (y / (float)h);
                    px[y * w + x] = new Color(shade, shade, shade, a);
                }
            }
            return Finish(key, t, px, new Vector2(0.5f, 0f), h);
        }

        /// <summary>Tileable stone brick wall (2x2 world units per tile).</summary>
        public static Sprite Bricks
        {
            get
            {
                if (TryGet("bricks", out var s)) return s;
                const int n = 128;
                var t = NewTex(n, n);
                t.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[n * n];
                const int rowH = 32, brickW = 64;
                for (int y = 0; y < n; y++)
                {
                    int row = y / rowH;
                    int offset = (row % 2) * (brickW / 2);
                    for (int x = 0; x < n; x++)
                    {
                        int bx = ((x + offset) % n) / brickW;
                        int lx = (x + offset) % brickW, ly = y % rowH;
                        bool mortar = lx < 3 || ly < 3;
                        float tone = 0.78f + 0.2f * Hash(bx + row * 7, row * 13);
                        float n2 = 0.92f + 0.08f * Noise(x * 0.25f, y * 0.25f);
                        // bevel: lighter top edge, darker bottom edge
                        float bevel = ly > rowH - 6 ? 1.08f : (ly < 7 ? 0.9f : 1f);
                        Color c = mortar ? new Color(0.36f, 0.33f, 0.3f) : new Color(0.66f, 0.62f, 0.56f) * tone * n2 * bevel;
                        c.a = 1;
                        px[y * n + x] = c;
                    }
                }
                return Finish("bricks", t, px, new Vector2(0.5f, 0.5f), 64);
            }
        }

        /// <summary>Tileable packed sand/dirt for the arena floor (2x2 units per tile).</summary>
        public static Sprite Dirt
        {
            get
            {
                if (TryGet("dirt", out var s)) return s;
                const int n = 128;
                var t = NewTex(n, n);
                t.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float v = 0.5f * Noise(x * 0.08f, y * 0.08f) + 0.3f * Noise(x * 0.3f + 40, y * 0.3f) + 0.2f * Hash(x, y);
                        // make it tile by blending with mirrored coordinates
                        Color c = Color.Lerp(new Color(0.55f, 0.4f, 0.26f), new Color(0.72f, 0.56f, 0.36f), v);
                        if (Hash(x * 3, y * 5) > 0.985f) c = new Color(0.45f, 0.42f, 0.4f); // pebbles
                        c.a = 1;
                        px[y * n + x] = c;
                    }
                return Finish("dirt", t, px, new Vector2(0.5f, 0.5f), 64);
            }
        }

        /// <summary>Strip of grass blades, pivot at the bottom, tileable horizontally.</summary>
        public static Sprite GrassStrip
        {
            get
            {
                if (TryGet("grass", out var s)) return s;
                const int w = 128, h = 32;
                var t = NewTex(w, h);
                t.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[w * h];
                for (int x = 0; x < w; x++)
                {
                    float blade = 10f + 14f * Hash(x, 99) * (0.6f + 0.4f * Mathf.Sin(x * 0.7f));
                    for (int y = 0; y < h; y++)
                    {
                        float a = y < 8 ? 1f : Mathf.Clamp01(blade - y + 0.5f);
                        float g = 0.75f + 0.25f * (y / (float)h) + 0.1f * Hash(x, y);
                        px[y * w + x] = new Color(0.32f * g, 0.56f * g, 0.22f * g, a);
                    }
                }
                return Finish("grass", t, px, new Vector2(0.5f, 0f), 64);
            }
        }

        public static Sprite Cloud
        {
            get
            {
                if (TryGet("cloud", out var s)) return s;
                const int w = 256, h = 128;
                var t = NewTex(w, h);
                var px = new Color[w * h];
                Vector3[] blobs =
                {
                    new Vector3(70, 50, 42), new Vector3(120, 68, 54), new Vector3(175, 52, 44), new Vector3(205, 42, 30),
                    new Vector3(40, 40, 28), new Vector3(125, 40, 50)
                };
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float a = 0f;
                        foreach (var b in blobs)
                        {
                            float d = Vector2.Distance(new Vector2(x, y), new Vector2(b.x, b.y)) / b.z;
                            a = Mathf.Max(a, Mathf.Clamp01((1f - d) * 4f));
                        }
                        float shade = 0.85f + 0.15f * (y / (float)h);
                        px[y * w + x] = new Color(shade, shade, shade, a * Mathf.Clamp01(y / 30f + 0.4f));
                    }
                return Finish("cloud", t, px, new Vector2(0.5f, 0.5f), 64);
            }
        }

        /// <summary>Hanging banner with a swallow tail, pivot at the top. White, tint it.</summary>
        public static Sprite Banner
        {
            get
            {
                if (TryGet("banner", out var s)) return s;
                const int w = 32, h = 96;
                var t = NewTex(w, h);
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float notch = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f) * 14f; // V cut at the bottom
                        float a = Mathf.Clamp01(y + 0.5f - (14f - notch));
                        bool trim = x < 3 || x >= w - 3 || y > h - 5;
                        float shade = trim ? 0.75f : 1f;
                        px[y * w + x] = new Color(shade, shade, shade, a);
                    }
                return Finish("banner", t, px, new Vector2(0.5f, 1f), 32);
            }
        }

        /// <summary>Archery target face (red/white rings), 1 unit across.</summary>
        public static Sprite TargetFace
        {
            get
            {
                if (TryGet("target", out var s)) return s;
                const int n = 96;
                var t = NewTex(n, n);
                var px = new Color[n * n];
                Color[] rings = { new Color(1f, 0.85f, 0.2f), new Color(0.85f, 0.15f, 0.15f), new Color(0.2f, 0.45f, 0.85f), new Color(0.1f, 0.1f, 0.1f), Color.white };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f - 1);
                        int ring = Mathf.Clamp(Mathf.FloorToInt(d * 5f), 0, 4);
                        Color c = rings[ring];
                        c.a = Mathf.Clamp01((1f - d) * n * 0.5f);
                        px[y * n + x] = c;
                    }
                return Finish("target", t, px, new Vector2(0.5f, 0.5f), n);
            }
        }
    }
}
