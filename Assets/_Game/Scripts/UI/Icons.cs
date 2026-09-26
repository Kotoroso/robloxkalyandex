using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Процедурные иконки для интерфейса (монета, молния, звезда, шестерёнка, сумка, яйцо, дракон).</summary>
    public static class Icons
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int N = 64;

        public static Sprite Coin { get { return Get("coin", DrawCoin); } }
        public static Sprite Bolt { get { return Get("bolt", DrawBolt); } }
        public static Sprite Star { get { return Get("star", DrawStar); } }
        public static Sprite Gear { get { return Get("gear", DrawGear); } }
        public static Sprite Bag { get { return Get("bag", DrawBag); } }
        public static Sprite Egg { get { return Get("egg", DrawEgg); } }
        public static Sprite Dragon { get { return Get("dragon", DrawDragon); } }
        public static Sprite Paw { get { return Get("paw", DrawPaw); } }
        public static Sprite Shoe { get { return Get("shoe", DrawShoe); } }
        public static Sprite Book { get { return Get("book", DrawBook); } }

        /// <summary>Картинка из Resources/Art/&lt;name&gt;.png или null.</summary>
        public static Sprite Art(string name)
        {
            string key = "art_" + name;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var t = Resources.Load<Texture2D>("Art/" + name);
            s = t != null ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)) : null;
            cache[key] = s;
            return s;
        }

        delegate Color PixelFn(float x, float y); // x,y в -1..1

        static Sprite Get(string name, PixelFn fn)
        {
            Sprite s;
            if (cache.TryGetValue(name, out s)) return s;
            // своя картинка (например из нейросети): Resources/Art/icon_<name>.png
            var custom = Resources.Load<Texture2D>("Art/icon_" + name);
            if (custom != null)
            {
                s = Sprite.Create(custom, new Rect(0, 0, custom.width, custom.height), new Vector2(0.5f, 0.5f));
                cache[name] = s;
                return s;
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // 2x2 суперсэмплинг — сглаженные края
                    Color acc = new Color(0, 0, 0, 0);
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float fx = ((x + 0.25f + sx * 0.5f) / N) * 2f - 1f;
                            float fy = ((y + 0.25f + sy * 0.5f) / N) * 2f - 1f;
                            Color c = fn(fx, fy);
                            acc += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    acc /= 4f;
                    px[y * N + x] = acc.a > 0.001f ? new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a) : new Color(0, 0, 0, 0);
                }
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            cache[name] = s;
            return s;
        }

        static readonly Color Outline = new Color(0.1f, 0.08f, 0.06f, 1f);
        static readonly Color Clear = new Color(0, 0, 0, 0);

        static bool InPoly(float x, float y, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > y) != (poly[j].y > y)) &&
                    (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        static Vector2[] Scale(Vector2[] p, float s)
        {
            var c = Vector2.zero;
            foreach (var v in p) c += v;
            c /= p.Length;
            var r = new Vector2[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = c + (p[i] - c) * s;
            return r;
        }

        static Color PolyWithOutline(float x, float y, Vector2[] poly, Color fill)
        {
            if (InPoly(x, y, Scale(poly, 0.84f))) return fill;
            if (InPoly(x, y, poly)) return Outline;
            return Clear;
        }

        static Color DrawCoin(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d > 0.92f) return Clear;
            if (d > 0.8f) return Outline;
            if (d > 0.62f) return new Color(1f, 0.72f, 0.1f);
            if (d > 0.54f) return new Color(0.85f, 0.55f, 0.05f);
            // блик
            if (x < -0.1f && y > 0.1f && d < 0.4f) return new Color(1f, 0.95f, 0.6f);
            return new Color(1f, 0.82f, 0.2f);
        }

        static readonly Vector2[] BoltPoly = {
            new Vector2(0.15f, 0.95f), new Vector2(-0.55f, -0.05f), new Vector2(-0.05f, -0.05f),
            new Vector2(-0.2f, -0.95f), new Vector2(0.55f, 0.15f), new Vector2(0.05f, 0.15f) };

        static Color DrawBolt(float x, float y) { return PolyWithOutline(x, y, BoltPoly, new Color(0.35f, 0.95f, 1f)); }

        static Vector2[] starPoly;
        static Color DrawStar(float x, float y)
        {
            if (starPoly == null)
            {
                starPoly = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                    float r = i % 2 == 0 ? 0.95f : 0.42f;
                    starPoly[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.05f);
                }
            }
            return PolyWithOutline(x, y, starPoly, new Color(1f, 0.55f, 1f));
        }

        static Color DrawGear(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Atan2(y, x);
            float teeth = Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 1f) < 0.5f ? 0.92f : 0.72f;
            if (d > teeth) return Clear;
            if (d > teeth - 0.12f) return Outline;
            if (d < 0.28f) return d > 0.18f ? Outline : Clear;
            return new Color(0.85f, 0.88f, 0.92f);
        }

        static Color DrawBag(float x, float y)
        {
            // ручка
            float hx = x, hy = y - 0.45f;
            float hd = Mathf.Sqrt(hx * hx + hy * hy);
            if (y > 0.3f && hd > 0.3f && hd < 0.45f) return Outline;
            var body = new[] { new Vector2(-0.75f, 0.35f), new Vector2(0.75f, 0.35f), new Vector2(0.85f, -0.9f), new Vector2(-0.85f, -0.9f) };
            return PolyWithOutline(x, y, body, new Color(0.35f, 0.85f, 0.4f));
        }

        static Color DrawEgg(float x, float y)
        {
            float ex = x / 0.7f, ey = (y + 0.05f) / (y > 0 ? 0.95f : 0.8f);
            float d = ex * ex + ey * ey;
            if (d > 1f) return Clear;
            if (d > 0.72f) return Outline;
            if ((x + 0.3f) * (x + 0.3f) + (y - 0.3f) * (y - 0.3f) < 0.03f) return new Color(1f, 1f, 1f);
            float spot = Mathf.Sin(x * 9f) * Mathf.Sin(y * 7f);
            return spot > 0.6f ? new Color(0.95f, 0.6f, 0.2f) : new Color(1f, 0.85f, 0.45f);
        }

        static float Ell(float x, float y, float cx, float cy, float rx, float ry) { float dx = (x - cx) / rx, dy = (y - cy) / ry; return dx * dx + dy * dy; }

        static Color DrawPaw(float x, float y)
        {
            Color fill = new Color(1f, 0.62f, 0.25f);
            float pad = Ell(x, y, 0, -0.35f, 0.5f, 0.42f);
            float[] tx = { -0.62f, -0.22f, 0.22f, 0.62f }, ty = { 0.05f, 0.45f, 0.45f, 0.05f };
            float best = pad;
            for (int i = 0; i < 4; i++) best = Mathf.Min(best, Ell(x, y, tx[i], ty[i], 0.22f, 0.27f));
            if (best < 0.62f) return fill;
            if (best < 1f) return Outline;
            return Clear;
        }

        static readonly Vector2[] ShoePoly = {
            new Vector2(-0.9f, -0.45f), new Vector2(-0.85f, 0.1f), new Vector2(-0.45f, 0.35f), new Vector2(-0.2f, 0.1f),
            new Vector2(0.25f, 0.0f), new Vector2(0.75f, -0.15f), new Vector2(0.95f, -0.4f), new Vector2(0.9f, -0.55f), new Vector2(-0.9f, -0.55f) };

        static Color DrawShoe(float x, float y)
        {
            if (y < -0.42f && y > -0.62f && x > -0.95f && x < 0.97f) return Color.white; // подошва
            return PolyWithOutline(x, y, ShoePoly, new Color(0.25f, 0.55f, 1f));
        }

        static Color DrawBook(float x, float y)
        {
            var cover = new[] { new Vector2(-0.6f, -0.85f), new Vector2(0.75f, -0.85f), new Vector2(0.85f, 0.85f), new Vector2(-0.5f, 0.85f) };
            if (InPoly(x, y, Scale(cover, 0.84f)))
                return (x < -0.3f) ? new Color(0.15f, 0.4f, 0.9f) : new Color(0.3f, 0.6f, 1f);
            if (InPoly(x, y, cover)) return Outline;
            return Clear;
        }

        static readonly Vector2[] DragonPoly = {
            new Vector2(-0.9f, -0.2f), new Vector2(-0.55f, 0.1f), new Vector2(-0.3f, 0.75f), new Vector2(-0.05f, 0.2f),
            new Vector2(0.3f, 0.2f), new Vector2(0.45f, 0.55f), new Vector2(0.9f, 0.45f), new Vector2(0.7f, 0.2f),
            new Vector2(0.55f, -0.3f), new Vector2(0.35f, -0.8f), new Vector2(0.15f, -0.35f), new Vector2(-0.3f, -0.35f),
            new Vector2(-0.45f, -0.8f), new Vector2(-0.6f, -0.35f) };

        static Color DrawDragon(float x, float y) { return PolyWithOutline(x, y, DragonPoly, new Color(1f, 0.45f, 0.2f)); }
    }
}
