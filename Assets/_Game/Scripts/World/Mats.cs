using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Кэш материалов и процедурные текстуры в стиле Роблокса.
    /// Непрозрачные материалы строятся из дефолтного материала примитива (работает везде).
    /// Светящиеся/прозрачные/частицы используют маленькие встроенные шейдеры, которые
    /// editor-скрипт добавляет в "Always Included Shaders" (есть фолбэк, если их нет).
    /// </summary>
    public static class Mats
    {
        static Material baseMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Texture2D studTex, faceTex, eggSpotTex, smoothTex, softDot, chevronTex, ringTex, beamTex, squareTex, keycapTex;
        static Font font;

        /// <summary>Сколько студов на 1 юнит мира (персонаж ≈ 3 юнита ≈ 5 студов, как в Роблоксе).</summary>
        public const float StudsPerUnit = 1.2f;

        public static Material Base
        {
            get
            {
                if (baseMat == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    baseMat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                    Object.Destroy(tmp);
                    SetGloss(baseMat, 0.25f);
                    if (baseMat.HasProperty("_Metallic")) baseMat.SetFloat("_Metallic", 0f);
                }
                return baseMat;
            }
        }

        static void SetGloss(Material m, float g)
        {
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", g);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", g);
        }

        /// <summary>Гладкий пластик (персонажи, драконы, декор).</summary>
        public static Material Plastic(Color c)
        {
            string key = "c" + ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = Smooth };
            cache[key] = m;
            return m;
        }

        /// <summary>Пластик со студами. UV деталей уже в мировых единицах, поэтому ничего не растягивается.</summary>
        public static Material Studs(Color c)
        {
            string key = "s" + ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = StudTexture };
            m.mainTextureScale = new Vector2(StudsPerUnit, StudsPerUnit);
            SetGloss(m, 0.2f);
            cache[key] = m;
            return m;
        }

        /// <summary>Пол из механических клавиш (ASMR-клавиатура по всей карте). Одна клавиша = 2x2 юнита.</summary>
        public static Material Keycaps(Color c)
        {
            string key = "kc" + ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = KeycapTexture };
            m.mainTextureScale = new Vector2(0.5f, 0.5f);
            SetGloss(m, 0.35f);
            cache[key] = m;
            return m;
        }

        public static Texture2D KeycapTexture
        {
            get
            {
                if (keycapTex != null) return keycapTex;
                const int N = 64;
                keycapTex = NewTex(N, N, false, true, "keycap");
                keycapTex.anisoLevel = 4;
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f;
                        // расстояние до края скруглённого квадрата клавиши
                        float m1 = 5f, r = 7f;
                        float cx = Mathf.Clamp(fx, m1 + r, N - m1 - r), cy = Mathf.Clamp(fy, m1 + r, N - m1 - r);
                        float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy)) - r; // <0 внутри
                        float v;
                        if (d > 0.5f) v = 0.42f;                        // щель между клавишами
                        else
                        {
                            // верх клавиши: чуть вогнутый, светлее к центру; скос по краю
                            float inner = Mathf.Clamp01(-d / 6f);
                            float dx = (fx - N / 2f) / (N / 2f), dy = (fy - N / 2f) / (N / 2f);
                            float dish = 1f - (dx * dx + dy * dy) * 0.06f;
                            v = Mathf.Lerp(0.78f, 1f, inner) * dish;
                            if (fy > N * 0.62f && inner > 0.5f) v = Mathf.Min(1f, v + 0.03f); // блик сверху
                        }
                        px[y * N + x] = new Color(v, v, v);
                    }
                keycapTex.SetPixels(px);
                keycapTex.Apply(true);
                return keycapTex;
            }
        }

        /// <summary>Совместимость со старыми вызовами (тайлинг теперь берётся из UV меша).</summary>
        public static Material Studs(Color c, float tx, float ty) { return Studs(c); }

        public static Material Unique(Color c, Texture tex)
        {
            return new Material(Base) { color = c, mainTexture = tex };
        }

        static Shader Find(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>Светящийся (неосвещаемый) цвет — неон, полоски, лучи.</summary>
        public static Material Glow(Color c)
        {
            string key = "g" + ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var sh = Find("Unlit/Color", "Universal Render Pipeline/Unlit");
            if (sh != null) { m = new Material(sh); m.color = c; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); }
            else m = Plastic(c);
            cache[key] = m;
            return m;
        }

        /// <summary>Полупрозрачный материал (барьеры зон, подсветка).</summary>
        public static Material Transparent(Color c, Texture tex = null)
        {
            string key = "t" + ColorUtility.ToHtmlStringRGBA(c) + (tex != null ? tex.name : "");
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var sh = Find("Legacy Shaders/Transparent/Diffuse", "Particles/Standard Unlit", "Unlit/Transparent");
            if (sh != null) { m = new Material(sh) { color = c }; if (tex != null) m.mainTexture = tex; }
            else m = Plastic(new Color(c.r, c.g, c.b, 1f));
            cache[key] = m;
            return m;
        }

        /// <summary>Материал частиц (мягкая точка).</summary>
        public static Material Particle(Texture tex = null, bool additive = false)
        {
            string key = "p" + (tex != null ? tex.name : "dot") + additive;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var sh = additive
                ? Find("Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit")
                : Find("Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Unlit/Transparent");
            if (sh != null) { m = new Material(sh); m.mainTexture = tex != null ? tex : SoftDot; }
            else m = Plastic(Color.white);
            cache[key] = m;
            return m;
        }

        /// <summary>Неосвещаемый полупрозрачный цвет с текстурой (стрелки обучения, кольца подсветки, лучи).</summary>
        public static Material UnlitAlpha(Color c, Texture tex)
        {
            string key = "ua" + ColorUtility.ToHtmlStringRGBA(c) + (tex != null ? tex.name : "");
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var sh = Find("Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Transparent/Diffuse", "Unlit/Transparent");
            if (sh != null)
            {
                m = new Material(sh);
                m.mainTexture = tex;
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a * 0.5f));
                else m.color = c;
            }
            else m = Glow(c);
            cache[key] = m;
            return m;
        }

        static Texture2D trailSoft, trailCore;

        /// <summary>Поперечный профиль следа (v — поперёк ленты): мягкое свечение или яркая сердцевина.</summary>
        public static Texture2D TrailTexture(bool core)
        {
            var t = core ? trailCore : trailSoft;
            if (t != null) return t;
            const int W = 4, H = 64;
            t = NewTex(W, H, true, false, core ? "trailCore" : "trailSoft");
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = Mathf.Abs((y + 0.5f) / H * 2f - 1f); // 0 в центре, 1 по краям
                float a = core ? Mathf.Clamp01(1f - v * v * 1.6f) : Mathf.Exp(-v * v * 4.5f) * (1f - v);
                for (int x = 0; x < W; x++) px[y * W + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            t.Apply();
            if (core) trailCore = t; else trailSoft = t;
            return t;
        }

        /// <summary>Материал светящегося следа (альфа-смешивание: хорошо виден и на светлом полу).</summary>
        public static Material TrailMat(bool core)
        {
            string key = core ? "trailCoreMat" : "trailSoftMat";
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            var sh = Find("Legacy Shaders/Particles/Alpha Blended", "Sprites/Default", "Particles/Standard Unlit", "Unlit/Transparent");
            if (sh != null) { m = new Material(sh); m.mainTexture = TrailTexture(core); }
            else m = Glow(Color.white);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            cache[key] = m;
            return m;
        }

        public static Texture2D SquareTexture
        {
            get
            {
                if (squareTex != null) return squareTex;
                squareTex = NewTex(8, 8, true, false, "square");
                var px = new Color[64];
                for (int i = 0; i < 64; i++) px[i] = Color.white;
                squareTex.SetPixels(px);
                squareTex.Apply();
                return squareTex;
            }
        }

        public static Texture2D RingTexture
        {
            get
            {
                if (ringTex != null) return ringTex;
                const int N = 64;
                ringTex = NewTex(N, N, true, true, "ring");
                ringTex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                        px[y * N + x] = new Color(1, 1, 1, a);
                    }
                ringTex.SetPixels(px);
                ringTex.Apply(true);
                return ringTex;
            }
        }

        /// <summary>Вертикальный градиент (луч света над целью).</summary>
        public static Texture2D BeamTexture
        {
            get
            {
                if (beamTex != null) return beamTex;
                const int W = 16, H = 64;
                beamTex = NewTex(W, H, true, false, "beam");
                beamTex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float fx = 1f - Mathf.Abs((x + 0.5f) / W * 2f - 1f);
                        float fy = 1f - y / (float)H;
                        px[y * W + x] = new Color(1, 1, 1, fx * fx * fy);
                    }
                beamTex.SetPixels(px);
                beamTex.Apply();
                return beamTex;
            }
        }

        public static Material Face(Color skin)
        {
            string key = "face" + ColorUtility.ToHtmlStringRGB(skin);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = skin, mainTexture = FaceTexture };
            cache[key] = m;
            return m;
        }

        public static Material EggMat(Color c)
        {
            string key = "egg" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = EggSpots };
            SetGloss(m, 0.6f);
            cache[key] = m;
            return m;
        }

        public static Material KeycapMat(Color c)
        {
            string key = "key" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = Smooth };
            SetGloss(m, 0.45f);
            cache[key] = m;
            return m;
        }

        // ====================== Текстуры ======================
        static Texture2D NewTex(int w, int h, bool alpha, bool mip, string name)
        {
            var t = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, mip);
            t.name = name;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        static Texture2D Smooth
        {
            get
            {
                if (smoothTex != null) return smoothTex;
                // лёгкий шум пластика, чтобы поверхности не были "мёртвыми"
                const int N = 32;
                smoothTex = NewTex(N, N, false, true, "smooth");
                var px = new Color[N * N];
                var rnd = new System.Random(3);
                for (int i = 0; i < px.Length; i++) { float v = 0.97f + (float)rnd.NextDouble() * 0.03f; px[i] = new Color(v, v, v); }
                smoothTex.SetPixels(px);
                smoothTex.Apply(true);
                return smoothTex;
            }
        }

        public static Texture2D StudTexture
        {
            get
            {
                if (studTex != null) return studTex;
                const int N = 64;
                studTex = NewTex(N, N, false, true, "studs");
                studTex.anisoLevel = 4;
                var px = new Color[N * N];
                Vector2 c = new Vector2(N / 2f, N / 2f);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                        float v = 0.96f;
                        // мягкий шов плитки
                        float edge = Mathf.Min(Mathf.Min(p.x, N - p.x), Mathf.Min(p.y, N - p.y));
                        if (edge < 1.5f) v = 0.9f;
                        float d = Vector2.Distance(p, c);
                        if (d < 17f)
                        {
                            // верх стада: лёгкий градиент (свет сверху-слева)
                            float light = Mathf.Clamp01(((p.y - c.y) - (p.x - c.x)) / 34f + 0.5f);
                            v = Mathf.Lerp(0.95f, 1f, light);
                            if (d > 15f) v = Mathf.Lerp(1f, 0.86f, (d - 15f) / 2f);
                        }
                        else if (d < 20f)
                        {
                            // тень от стада снизу-справа
                            float s = Vector2.Dot((p - c).normalized, new Vector2(0.7f, -0.7f));
                            if (s > 0) v = Mathf.Lerp(0.96f, 0.88f, s * (1f - (d - 17f) / 3f));
                        }
                        px[y * N + x] = new Color(v, v, v);
                    }
                studTex.SetPixels(px);
                studTex.Apply(true);
                return studTex;
            }
        }

        static Texture2D FaceTexture
        {
            get
            {
                if (faceTex != null) return faceTex;
                const int N = 64;
                faceTex = NewTex(N, N, false, false, "face");
                faceTex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[N * N];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                Color black = new Color(0.08f, 0.08f, 0.08f);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float ex1 = (x - 22) / 4.2f, ey = (y - 40) / 7.5f, ex2 = (x - 42) / 4.2f;
                        if (ex1 * ex1 + ey * ey < 1 || ex2 * ex2 + ey * ey < 1) px[y * N + x] = black;
                        float dx = x - 32, dy = y - 36;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r > 17f && r < 21.5f && dy < -9f) px[y * N + x] = black;
                    }
                faceTex.SetPixels(px);
                faceTex.Apply();
                return faceTex;
            }
        }

        static Texture2D EggSpots
        {
            get
            {
                if (eggSpotTex != null) return eggSpotTex;
                const int N = 64;
                eggSpotTex = NewTex(N, N, false, true, "eggspots");
                var px = new Color[N * N];
                var rnd = new System.Random(7);
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                for (int s = 0; s < 14; s++)
                {
                    int cx = rnd.Next(N), cy = rnd.Next(N), r = rnd.Next(3, 7);
                    for (int y = -r; y <= r; y++)
                        for (int x = -r; x <= r; x++)
                            if (x * x + y * y <= r * r)
                                px[((cy + y + N) % N) * N + (cx + x + N) % N] = new Color(0.72f, 0.72f, 0.72f);
                }
                eggSpotTex.SetPixels(px);
                eggSpotTex.Apply(true);
                return eggSpotTex;
            }
        }

        /// <summary>Мягкая круглая точка для частиц.</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot != null) return softDot;
                const int N = 32;
                softDot = NewTex(N, N, true, false, "softdot");
                softDot.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        px[y * N + x] = new Color(1, 1, 1, a * a);
                    }
                softDot.SetPixels(px);
                softDot.Apply();
                return softDot;
            }
        }

        /// <summary>Стрелки-шевроны для ленты беговой дорожки и тропы обучения.</summary>
        public static Texture2D ChevronTexture
        {
            get
            {
                if (chevronTex != null) return chevronTex;
                const int N = 64;
                chevronTex = NewTex(N, N, true, true, "chevron");
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        // V-образная стрелка вверх (по +V)
                        float cx = Mathf.Abs(x + 0.5f - N / 2f);
                        float yy = y + 0.5f - cx * 0.9f;
                        float band = Mathf.Repeat(yy, N);
                        bool on = band > 18 && band < 34 && cx < 26;
                        px[y * N + x] = on ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
                    }
                chevronTex.SetPixels(px);
                chevronTex.Apply(true);
                return chevronTex;
            }
        }

        public static Font UIFont
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/Rubik-ExtraBold");
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return font;
            }
        }
    }
}
