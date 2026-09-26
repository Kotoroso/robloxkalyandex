using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Кэш материалов и процедурные текстуры в стиле Роблокса (студы, лицо).
    /// Всё строится из дефолтного материала примитива, поэтому работает и в Built-in, и в URP,
    /// и не требует шейдеров в "Always Included".
    /// </summary>
    public static class Mats
    {
        static Material baseMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Texture2D studTex, faceTex, eggSpotTex, smoothTex;
        static Font font;

        public static Material Base
        {
            get
            {
                if (baseMat == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    baseMat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                    Object.Destroy(tmp);
                    if (baseMat.HasProperty("_Glossiness")) baseMat.SetFloat("_Glossiness", 0.15f);
                    if (baseMat.HasProperty("_Smoothness")) baseMat.SetFloat("_Smoothness", 0.15f);
                    if (baseMat.HasProperty("_Metallic")) baseMat.SetFloat("_Metallic", 0f);
                }
                return baseMat;
            }
        }

        /// <summary>Гладкий пластик (персонажи, драконы).</summary>
        public static Material Plastic(Color c)
        {
            string key = "c" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c };
            m.mainTexture = Smooth;
            cache[key] = m;
            return m;
        }

        /// <summary>Пластик со студами. tiling = размер детали в студах.</summary>
        public static Material Studs(Color c, float tx, float ty)
        {
            tx = Mathf.Max(1, Mathf.Round(tx / 2f)); // 1 стад = 2 юнита
            ty = Mathf.Max(1, Mathf.Round(ty / 2f));
            string key = "s" + ColorUtility.ToHtmlStringRGB(c) + "_" + tx + "_" + ty;
            Material m;
            if (cache.TryGetValue(key, out m)) return m;
            m = new Material(Base) { color = c, mainTexture = StudTexture };
            m.mainTextureScale = new Vector2(tx, ty);
            cache[key] = m;
            return m;
        }

        /// <summary>Уникальный материал (для анимации оффсета текстуры, как у ленты дорожки).</summary>
        public static Material Unique(Color c, Texture tex)
        {
            return new Material(Base) { color = c, mainTexture = tex };
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
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.5f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.5f);
            cache[key] = m;
            return m;
        }

        static Texture2D Smooth
        {
            get
            {
                if (smoothTex == null)
                {
                    smoothTex = new Texture2D(4, 4, TextureFormat.RGB24, false);
                    var px = new UnityEngine.Color[16];
                    for (int i = 0; i < 16; i++) px[i] = UnityEngine.Color.white;
                    smoothTex.SetPixels(px);
                    smoothTex.Apply();
                }
                return smoothTex;
            }
        }

        public static Texture2D StudTexture
        {
            get
            {
                if (studTex != null) return studTex;
                const int N = 64;
                studTex = new Texture2D(N, N, TextureFormat.RGB24, true);
                studTex.wrapMode = TextureWrapMode.Repeat;
                studTex.filterMode = FilterMode.Bilinear;
                studTex.anisoLevel = 2;
                var px = new UnityEngine.Color[N * N];
                Vector2 c = new Vector2(N / 2f, N / 2f);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float v = 1f;
                        // лёгкий шов по краю плитки
                        if (x == 0 || y == 0) v = 0.86f;
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                        float d = Vector2.Distance(p, c);
                        if (d < 17f)
                        {
                            // стад: верх светлее, низ темнее
                            float light = Mathf.Clamp01((p.y - c.y) / 17f * 0.5f + 0.5f);
                            v = Mathf.Lerp(0.88f, 1.08f, light);
                            if (d > 15f) v = 0.8f;
                        }
                        else if (d < 19f && p.y < c.y) v = 0.82f; // тень от стада
                        px[y * N + x] = new UnityEngine.Color(v, v, v);
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
                faceTex = new Texture2D(N, N, TextureFormat.RGB24, false);
                faceTex.filterMode = FilterMode.Bilinear;
                faceTex.wrapMode = TextureWrapMode.Clamp;
                var px = new UnityEngine.Color[N * N];
                for (int i = 0; i < px.Length; i++) px[i] = UnityEngine.Color.white;
                UnityEngine.Color black = new UnityEngine.Color(0.08f, 0.08f, 0.08f);
                // глаза (овалы)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float ex1 = (x - 22) / 3.2f, ey = (y - 38) / 5.5f, ex2 = (x - 42) / 3.2f;
                        if (ex1 * ex1 + ey * ey < 1 || ex2 * ex2 + ey * ey < 1) px[y * N + x] = black;
                        // улыбка — дуга
                        float dx = x - 32, dy = y - 34;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r > 13 && r < 16.5f && dy < -6) px[y * N + x] = black;
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
                eggSpotTex = new Texture2D(N, N, TextureFormat.RGB24, false);
                var px = new UnityEngine.Color[N * N];
                var rnd = new System.Random(7);
                for (int i = 0; i < px.Length; i++) px[i] = UnityEngine.Color.white;
                for (int s = 0; s < 14; s++)
                {
                    int cx = rnd.Next(N), cy = rnd.Next(N), r = rnd.Next(3, 7);
                    for (int y = -r; y <= r; y++)
                        for (int x = -r; x <= r; x++)
                            if (x * x + y * y <= r * r)
                            {
                                int xx = (cx + x + N) % N, yy = (cy + y + N) % N;
                                px[yy * N + xx] = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
                            }
                }
                eggSpotTex.SetPixels(px);
                eggSpotTex.Apply();
                return eggSpotTex;
            }
        }

        public static Font UIFont
        {
            get
            {
                if (font != null) return font;
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return font;
            }
        }
    }
}
