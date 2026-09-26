using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// 2D-иконки драконов и яиц для интерфейса (рисуются кодом в цветах конкретного дракона):
    /// тело, живот, крылья, рога, хвост, глаз с бликом, толстая обводка, свечение для редких тиров.
    /// Надёжно работает везде (без рендер-текстур), кэшируется.
    /// </summary>
    public static class IconArt
    {
        const int N = 128;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Color OutlineC = new Color(0.1f, 0.07f, 0.12f, 1f);

        // ===================== примитивы =====================
        static float Ell(float x, float y, float cx, float cy, float rx, float ry, float rot = 0f)
        {
            float dx = x - cx, dy = y - cy;
            if (rot != 0f) { float c = Mathf.Cos(rot), s = Mathf.Sin(rot); float t = dx * c + dy * s; dy = -dx * s + dy * c; dx = t; }
            dx /= rx; dy /= ry;
            return Mathf.Sqrt(dx * dx + dy * dy) - 1f; // <0 внутри (нормированно)
        }

        static bool InTri(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (x - b.x) * (a.y - b.y) - (a.x - b.x) * (y - b.y);
            float d2 = (x - c.x) * (b.y - c.y) - (b.x - c.x) * (y - c.y);
            float d3 = (x - a.x) * (c.y - a.y) - (c.x - a.x) * (y - a.y);
            bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0), pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(neg && pos);
        }

        static Color Shade(Color c, float y) { float k = Mathf.Lerp(0.78f, 1.12f, y); return new Color(Mathf.Min(1, c.r * k), Mathf.Min(1, c.g * k), Mathf.Min(1, c.b * k), 1f); }

        static Color Vivid(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.2f + 0.05f), Mathf.Clamp01(v * 1.05f + 0.05f));
        }

        delegate Color Layer(float x, float y); // x,y в 0..1, (0,0) — низ-лево; вернуть a=0 если пусто

        static Sprite Make(string key, Layer shape, Color glow)
        {
            Sprite sp;
            if (cache.TryGetValue(key, out sp)) return sp;
            var mask = new Color[N * N];
            // 1) рисуем фигуру
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    mask[y * N + x] = shape((x + 0.5f) / N, (y + 0.5f) / N);
            // 2) обводка: расширяем альфу на 3 пикселя
            var px = new Color[N * N];
            const int R = 3;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var c = mask[y * N + x];
                    if (c.a > 0.5f) { px[y * N + x] = c; continue; }
                    bool near = false;
                    for (int oy = -R; oy <= R && !near; oy++)
                        for (int ox = -R; ox <= R; ox++)
                        {
                            if (ox * ox + oy * oy > R * R) continue;
                            int xx = x + ox, yy = y + oy;
                            if (xx < 0 || yy < 0 || xx >= N || yy >= N) continue;
                            if (mask[yy * N + xx].a > 0.5f) { near = true; break; }
                        }
                    if (near) px[y * N + x] = OutlineC;
                    else if (glow.a > 0)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                        float a = Mathf.Clamp01(1f - d) * glow.a;
                        px[y * N + x] = new Color(glow.r, glow.g, glow.b, a * a);
                    }
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            cache[key] = sp;
            return sp;
        }

        /// <summary>
        /// Своя картинка из проекта (если есть): Assets/_Game/Resources/Art/&lt;name&gt;.png — PNG с прозрачным фоном.
        /// Так можно подменить любую иконку картинкой из нейросети.
        /// </summary>
        static Sprite Custom(string name)
        {
            string key = "art_" + name;
            Sprite sp;
            if (cache.TryGetValue(key, out sp)) return sp;
            var tex = Resources.Load<Texture2D>("Art/" + name);
            sp = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
            cache[key] = sp;
            return sp;
        }

        // ===================== дракон =====================
        public static Sprite Dragon(DragonDef d)
        {
            var custom = Custom("dragon_" + d.id);
            if (custom != null) return custom;
            Color body = Vivid(d.body), belly = Vivid(d.belly), wing = Vivid(d.wing);
            Color horn = Color.Lerp(belly, Color.white, 0.5f);
            bool rainbow = d.fx == DragonFx.Rainbow;
            Color glow = d.tier >= Tier.Legendary || d.exclusive ? new Color(Color.Lerp(wing, Color.white, 0.4f).r, Color.Lerp(wing, Color.white, 0.4f).g, Color.Lerp(wing, Color.white, 0.4f).b, 0.55f) : new Color(0, 0, 0, 0);
            bool crown = d.exclusive;
            return Make("d" + d.id, (x, y) =>
            {
                Color bodyC = rainbow ? Color.HSVToRGB(Mathf.Repeat(x * 1.3f + y * 0.4f, 1f), 0.55f, 1f) : body;
                // крыло (за телом): большой треугольник-перепонка
                bool wingIn = InTri(x, y, new Vector2(0.42f, 0.52f), new Vector2(0.18f, 0.93f), new Vector2(0.68f, 0.82f))
                           || InTri(x, y, new Vector2(0.42f, 0.52f), new Vector2(0.68f, 0.82f), new Vector2(0.6f, 0.55f));
                // хвост
                float tail = Ell(x, y, 0.2f, 0.32f, 0.2f, 0.07f, -0.5f);
                bool tailTip = InTri(x, y, new Vector2(0.02f, 0.44f), new Vector2(0.1f, 0.36f), new Vector2(0.03f, 0.3f));
                // тело, живот, шея, голова, морда
                float bodyE = Ell(x, y, 0.45f, 0.36f, 0.24f, 0.17f);
                float bellyE = Ell(x, y, 0.5f, 0.3f, 0.17f, 0.09f);
                float neck = Ell(x, y, 0.64f, 0.52f, 0.08f, 0.15f, 0.5f);
                float head = Ell(x, y, 0.74f, 0.66f, 0.13f, 0.11f);
                float snout = Ell(x, y, 0.86f, 0.62f, 0.09f, 0.065f);
                // рога
                bool horns = InTri(x, y, new Vector2(0.68f, 0.74f), new Vector2(0.62f, 0.9f), new Vector2(0.73f, 0.76f))
                          || InTri(x, y, new Vector2(0.76f, 0.76f), new Vector2(0.74f, 0.93f), new Vector2(0.81f, 0.75f));
                // лапы
                float leg1 = Ell(x, y, 0.36f, 0.19f, 0.05f, 0.1f), leg2 = Ell(x, y, 0.56f, 0.19f, 0.05f, 0.1f);
                // глаз
                float eye = Ell(x, y, 0.77f, 0.69f, 0.035f, 0.045f);
                float pupil = Ell(x, y, 0.785f, 0.685f, 0.018f, 0.03f);
                float shine = Ell(x, y, 0.77f, 0.705f, 0.009f, 0.009f);
                // гребень на спине
                bool spikes = false;
                for (int i = 0; i < 4; i++)
                {
                    float sx = 0.3f + i * 0.08f;
                    if (InTri(x, y, new Vector2(sx, 0.5f), new Vector2(sx + 0.035f, 0.58f), new Vector2(sx + 0.07f, 0.5f))) spikes = true;
                }
                // корона эксклюзивов
                bool crownIn = crown && (InTri(x, y, new Vector2(0.66f, 0.76f), new Vector2(0.68f, 0.86f), new Vector2(0.71f, 0.77f))
                                      || InTri(x, y, new Vector2(0.71f, 0.77f), new Vector2(0.745f, 0.9f), new Vector2(0.78f, 0.77f))
                                      || InTri(x, y, new Vector2(0.78f, 0.77f), new Vector2(0.81f, 0.86f), new Vector2(0.83f, 0.76f)));

                if (shine < 0) return Color.white;
                if (pupil < 0) return new Color(0.08f, 0.06f, 0.1f);
                if (eye < 0) return Color.white;
                if (crownIn) return new Color(1f, 0.82f, 0.2f);
                if (snout < 0 || head < 0) return Shade(bodyC, y);
                if (horns) return horn;
                if (bellyE < 0) return Shade(belly, y);
                if (bodyE < 0 || neck < 0) return Shade(bodyC, y);
                if (leg1 < 0 || leg2 < 0) return Shade(bodyC * 0.85f, y);
                if (spikes) return wing;
                if (tailTip) return wing;
                if (tail < 0) return Shade(bodyC, y);
                if (wingIn)
                {
                    // перепонки крыла полосами
                    bool rib = Mathf.Repeat((x - 0.42f) * 9f + (y - 0.52f) * 4f, 1f) < 0.12f;
                    return rib ? Shade(wing * 0.75f, y) : Shade(wing, y);
                }
                return new Color(0, 0, 0, 0);
            }, glow);
        }

        // ===================== яйцо =====================
        public static Sprite Egg(Tier t, bool premium = false)
        {
            var custom = Custom(premium ? "egg_premium" : "egg_" + (int)t);
            if (custom != null) return custom;
            Color c = premium ? new Color(1f, 0.78f, 0.15f) : Vivid(GameConfig.GetTier(t).color);
            Color spot = Color.Lerp(c, premium ? new Color(1f, 0.45f, 0.05f) : Color.white, 0.45f);
            Color glow = t >= Tier.Legendary || premium ? new Color(c.r, c.g, c.b, 0.5f) : new Color(0, 0, 0, 0);
            return Make("e" + (int)t + (premium ? "p" : ""), (x, y) =>
            {
                float ry = y > 0.45f ? 0.42f : 0.36f;
                float e = Ell(x, y, 0.5f, 0.45f, 0.3f, ry);
                if (e >= 0) return new Color(0, 0, 0, 0);
                float sh = Ell(x, y, 0.4f, 0.62f, 0.07f, 0.1f, 0.4f);
                if (sh < 0) return new Color(1, 1, 1, 1);
                bool spots = Ell(x, y, 0.62f, 0.55f, 0.06f, 0.05f) < 0 || Ell(x, y, 0.42f, 0.35f, 0.07f, 0.055f) < 0
                          || Ell(x, y, 0.6f, 0.28f, 0.05f, 0.04f) < 0 || Ell(x, y, 0.35f, 0.52f, 0.04f, 0.035f) < 0;
                var col = spots ? spot : c;
                if (premium && Mathf.Abs(y - 0.48f) < 0.03f) col = new Color(1f, 0.95f, 0.6f); // золотой поясок
                return Shade(col, y);
            }, glow);
        }
    }
}
