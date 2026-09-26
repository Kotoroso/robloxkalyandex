using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Настоящий 3D-пол из механических клавиш (как в роблокс-картах "ASMR keyboard").
    /// Клавиши НЕ создаются отдельными объектами: на каждый чанк 32x32 клавиши строится
    /// по одному объединённому мешу на цвет палитры (≤ 60000 вершин, 16-битные индексы).
    /// Буквы A–Z печатаются из одного атласа, сгенерированного в рантайме из встроенного
    /// пиксельного шрифта 5x7. Ходьба — по невидимым BoxCollider'ам на высоте верха клавиш.
    /// </summary>
    public static class KeycapFloor
    {
        /// <summary>Шаг сетки клавиш в юнитах мира (клавиша i занимает x ∈ [2i, 2i+2)).</summary>
        public const float Pitch = 2f;
        /// <summary>Высота клавиши: верх на y + Height.</summary>
        public const float Height = 0.45f;

        const float BottomHalf = 0.92f;  // низ 1.84 x 1.84
        const float TopHalf = 0.75f;     // верх 1.5 x 1.5
        const float DishDepth = 0.03f;   // лёгкая вогнутость верха
        const int ChunkKeys = 32;        // чанк 32x32 клавиши = 64x64 юнита
        const int VertsPerKey = 21;      // 5 (верх) + 4x4 (скосы)
        const int MaxVerts = 60000;      // запас до лимита 16-битных индексов

        // атлас букв
        const int AtlasSize = 512, CellPx = 64, AtlasCols = 8, WhiteCell = 63;
        const float CellInsetPx = 3f;
        const int FontPx = 6;            // размер "пикселя" шрифта в текселях

        static Texture2D atlas;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static readonly Color[] DefaultPalette =
        {
            new Color(1f, 0.78f, 0.86f),   // розовый
            new Color(0.82f, 0.76f, 0.98f), // лавандовый
            new Color(0.98f, 0.97f, 1f),   // белый
            new Color(0.66f, 0.55f, 0.92f), // фиолетовый
        };

        /// <summary>Пастельная палитра: розовый, лавандовый, белый, фиолетовый.</summary>
        public static Color[] PastelPalette { get { return (Color[])DefaultPalette.Clone(); } }

        /// <summary>Вариант палитры: коричневый / кремовый ("кофейная" клавиатура).</summary>
        public static Color[] CoffeePalette
        {
            get
            {
                return new[]
                {
                    new Color(0.55f, 0.38f, 0.27f), // коричневый
                    new Color(0.96f, 0.91f, 0.8f),  // кремовый
                    new Color(0.78f, 0.64f, 0.48f), // карамельный
                    new Color(0.99f, 0.97f, 0.92f), // молочный
                };
            }
        }

        /// <summary>
        /// Строит пол из клавиш в прямоугольнике мира.
        /// </summary>
        /// <param name="parent">родитель (ожидается без поворота и масштаба, например staticRoot)</param>
        /// <param name="area">область в мировых XZ: x = area.x..area.xMax, z = area.y..area.yMax.
        /// Берутся клавиши, центр которых (2i+1, 2j+1) попадает в область.</param>
        /// <param name="y">мировая высота поверхности, на которой стоят клавиши (верх клавиш = y + 0.45)</param>
        /// <param name="palette">цвета клавиш (null/пусто — пастельная палитра)</param>
        /// <param name="seed">сид для случайных цветов и букв (детерминированно по клетке)</param>
        /// <param name="skip">skip(центр клетки в мире) == true — клавишу не ставить (клетка занята объектом)</param>
        public static void Build(Transform parent, Rect area, float y, Color[] palette, int seed, System.Func<Vector3, bool> skip = null)
        {
            if (palette == null || palette.Length == 0) palette = DefaultPalette;

            int i0 = Mathf.CeilToInt((area.x - 1f) / Pitch);
            int i1 = Mathf.CeilToInt((area.xMax - 1f) / Pitch) - 1;
            int j0 = Mathf.CeilToInt((area.y - 1f) / Pitch);
            int j1 = Mathf.CeilToInt((area.yMax - 1f) / Pitch) - 1;
            if (i1 < i0 || j1 < j0) return;

            var mats = new Material[palette.Length];
            for (int k = 0; k < palette.Length; k++) mats[k] = Mat(palette[k]);

            var root = new GameObject("KeycapFloor");
            root.transform.SetParent(parent, false);

            int cx0 = FloorDiv(i0, ChunkKeys), cx1 = FloorDiv(i1, ChunkKeys);
            int cz0 = FloorDiv(j0, ChunkKeys), cz1 = FloorDiv(j1, ChunkKeys);
            var batches = new Batch[palette.Length];
            var occ = new bool[ChunkKeys, ChunkKeys];

            for (int cz = cz0; cz <= cz1; cz++)
                for (int cx = cx0; cx <= cx1; cx++)
                {
                    int bi = cx * ChunkKeys, bj = cz * ChunkKeys; // первая клавиша чанка
                    Vector3 origin = new Vector3(bi * Pitch, y, bj * Pitch);
                    int ia = Mathf.Max(i0, bi), ib = Mathf.Min(i1, bi + ChunkKeys - 1);
                    int ja = Mathf.Max(j0, bj), jb = Mathf.Min(j1, bj + ChunkKeys - 1);

                    GameObject chunk = null;
                    int part = 0, keys = 0;
                    System.Array.Clear(occ, 0, occ.Length);

                    for (int j = ja; j <= jb; j++)
                        for (int i = ia; i <= ib; i++)
                        {
                            var world = new Vector3(i * Pitch + 1f, y, j * Pitch + 1f);
                            if (skip != null && skip(world)) continue;

                            uint h = Hash(i, j, seed);
                            int ci = (int)(h % (uint)palette.Length);
                            int letter = (int)((h >> 11) % 26u);

                            if (chunk == null)
                            {
                                chunk = new GameObject("Keys_" + cx + "_" + cz);
                                chunk.transform.SetParent(root.transform, false);
                                chunk.transform.position = origin;
                            }

                            var b = batches[ci];
                            if (b == null) b = batches[ci] = new Batch();
                            if (b.verts.Count + VertsPerKey > MaxVerts) Flush(b, chunk.transform, mats[ci], ref part);

                            // локальные координаты центра клавиши внутри чанка
                            AddKey(b, (i - bi) * Pitch + 1f, (j - bj) * Pitch + 1f, letter);
                            occ[i - bi, j - bj] = true;
                            keys++;
                        }

                    if (chunk == null) continue;
                    for (int k = 0; k < batches.Length; k++)
                        if (batches[k] != null && batches[k].verts.Count > 0) Flush(batches[k], chunk.transform, mats[k], ref part);
                    BuildColliders(chunk.transform, occ);
                }
        }

        // ====================== Геометрия ======================
        class Batch
        {
            public readonly List<Vector3> verts = new List<Vector3>(4096);
            public readonly List<Vector3> norms = new List<Vector3>(4096);
            public readonly List<Vector2> uvs = new List<Vector2>(4096);
            public readonly List<int> tris = new List<int>(8192);

            public void Clear() { verts.Clear(); norms.Clear(); uvs.Clear(); tris.Clear(); }
        }

        /// <summary>
        /// Одна клавиша: вогнутый верх (центр + 4 угла, 4 треугольника) и 4 скошенные грани
        /// (по 4 вершины, 2 треугольника). Итого 21 вершина и 12 треугольников, без дна.
        /// </summary>
        static void AddKey(Batch b, float cx, float cz, int letter)
        {
            const float B = BottomHalf, T = TopHalf, H = Height;

            // --- верх ---
            int col = letter % AtlasCols, row = letter / AtlasCols;
            float u0 = (col * CellPx + CellInsetPx) / AtlasSize, u1 = ((col + 1) * CellPx - CellInsetPx) / AtlasSize;
            float v0 = (row * CellPx + CellInsetPx) / AtlasSize, v1 = ((row + 1) * CellPx - CellInsetPx) / AtlasSize;

            int s = b.verts.Count;
            b.verts.Add(new Vector3(cx, H - DishDepth, cz)); b.uvs.Add(new Vector2((u0 + u1) * 0.5f, (v0 + v1) * 0.5f));
            b.verts.Add(new Vector3(cx - T, H, cz + T)); b.uvs.Add(new Vector2(u0, v1)); // лево-верх (+z)
            b.verts.Add(new Vector3(cx + T, H, cz + T)); b.uvs.Add(new Vector2(u1, v1)); // право-верх
            b.verts.Add(new Vector3(cx + T, H, cz - T)); b.uvs.Add(new Vector2(u1, v0)); // право-низ
            b.verts.Add(new Vector3(cx - T, H, cz - T)); b.uvs.Add(new Vector2(u0, v0)); // лево-низ
            for (int k = 0; k < 5; k++) b.norms.Add(Vector3.up);
            // по часовой стрелке при взгляде сверху (лицевая сторона в Unity)
            b.tris.Add(s); b.tris.Add(s + 1); b.tris.Add(s + 2);
            b.tris.Add(s); b.tris.Add(s + 2); b.tris.Add(s + 3);
            b.tris.Add(s); b.tris.Add(s + 3); b.tris.Add(s + 4);
            b.tris.Add(s); b.tris.Add(s + 4); b.tris.Add(s + 1);

            // --- скосы ---
            float wc = (WhiteCell % AtlasCols + 0.5f) * CellPx / AtlasSize;
            float wr = (WhiteCell / AtlasCols + 0.5f) * CellPx / AtlasSize;
            var white = new Vector2(wc, wr);
            // нормаль скоса: наружу и вверх; (d*H + up*(B-T)), нормализованная
            float len = Mathf.Sqrt(H * H + (B - T) * (B - T));
            float nh = H / len, ny = (B - T) / len;

            Side(b, cx, cz, 0f, 1f, nh, ny, white);   // +z
            Side(b, cx, cz, 1f, 0f, nh, ny, white);   // +x
            Side(b, cx, cz, 0f, -1f, nh, ny, white);  // -z
            Side(b, cx, cz, -1f, 0f, nh, ny, white);  // -x
        }

        /// <summary>Скошенная грань с наружным направлением (dx, dz).</summary>
        static void Side(Batch b, float cx, float cz, float dx, float dz, float nh, float ny, Vector2 uv)
        {
            const float B = BottomHalf, T = TopHalf, H = Height;
            // касательная "вправо" для наблюдателя снаружи грани: (-dz, dx) → нормаль треугольников наружу
            float tx = -dz, tz = dx;
            int s = b.verts.Count;
            // смотрим на грань снаружи: левый-низ, левый-верх, правый-верх, правый-низ
            b.verts.Add(new Vector3(cx + dx * B - tx * B, 0f, cz + dz * B - tz * B));
            b.verts.Add(new Vector3(cx + dx * T - tx * T, H, cz + dz * T - tz * T));
            b.verts.Add(new Vector3(cx + dx * T + tx * T, H, cz + dz * T + tz * T));
            b.verts.Add(new Vector3(cx + dx * B + tx * B, 0f, cz + dz * B + tz * B));
            var n = new Vector3(dx * nh, ny, dz * nh);
            for (int k = 0; k < 4; k++) { b.norms.Add(n); b.uvs.Add(uv); }
            b.tris.Add(s); b.tris.Add(s + 1); b.tris.Add(s + 2);
            b.tris.Add(s); b.tris.Add(s + 2); b.tris.Add(s + 3);
        }

        static void Flush(Batch b, Transform chunk, Material mat, ref int part)
        {
            var mesh = new Mesh { name = "KeycapFloor_" + chunk.name + "_" + part };
            mesh.SetVertices(b.verts);
            mesh.SetNormals(b.norms);
            mesh.SetUVs(0, b.uvs);
            mesh.SetTriangles(b.tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // CPU-копия не нужна: коллайдеры отдельные

            var go = new GameObject("Caps" + part);
            go.layer = MeshMerge.FloorLayer; // дальние чанки отсекаются (Perf → Camera.layerCullDistances), под ними плоский пол
            go.transform.SetParent(chunk, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // дёшево для мобилок
            r.receiveShadows = true;
            part++;
            b.Clear();
        }

        /// <summary>
        /// Невидимые "крышки" для ходьбы: клетки с клавишами жадно объединяются в прямоугольники,
        /// на каждый — BoxCollider от пола до верха клавиш. Занятые (skip) клетки остаются пустыми.
        /// </summary>
        static void BuildColliders(Transform chunk, bool[,] occ)
        {
            var go = new GameObject("KeysTop");
            go.transform.SetParent(chunk, false);
            var used = new bool[ChunkKeys, ChunkKeys];
            for (int j = 0; j < ChunkKeys; j++)
                for (int i = 0; i < ChunkKeys; i++)
                {
                    if (!occ[i, j] || used[i, j]) continue;
                    int w = 1;
                    while (i + w < ChunkKeys && occ[i + w, j] && !used[i + w, j]) w++;
                    int d = 1;
                    while (j + d < ChunkKeys)
                    {
                        bool full = true;
                        for (int k = 0; k < w; k++)
                            if (!occ[i + k, j + d] || used[i + k, j + d]) { full = false; break; }
                        if (!full) break;
                        d++;
                    }
                    for (int dd = 0; dd < d; dd++)
                        for (int k = 0; k < w; k++) used[i + k, j + dd] = true;

                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = new Vector3(w * Pitch, Height, d * Pitch);
                    bc.center = new Vector3((i + w * 0.5f) * Pitch, Height * 0.5f, (j + d * 0.5f) * Pitch);
                }
        }

        static int FloorDiv(int a, int b) { return a >= 0 ? a / b : -((-a + b - 1) / b); }

        static uint Hash(int i, int j, int seed)
        {
            unchecked
            {
                uint h = (uint)i * 73856093u ^ (uint)j * 19349663u ^ (uint)seed * 83492791u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15; h *= 0x27d4eb2du; h ^= h >> 16;
                return h;
            }
        }

        // ====================== Материалы и атлас ======================
        static Material Mat(Color c)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Mats.Base) { color = c, mainTexture = Atlas };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.4f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.4f);
            Mats.RegisterLit(m);
            cache[key] = m;
            return m;
        }

        /// <summary>Атлас 512x512: 8x8 клеток по 64 px, в клетках 0..25 — буквы A–Z, клетка 63 — чисто белая (скосы).</summary>
        public static Texture2D Atlas
        {
            get
            {
                if (atlas != null) return atlas;
                atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGB24, true);
                atlas.name = "keycap_letters";
                atlas.filterMode = FilterMode.Bilinear;
                atlas.wrapMode = TextureWrapMode.Clamp;
                atlas.anisoLevel = 2;

                var px = new Color32[AtlasSize * AtlasSize];
                var bg = new Color32(255, 255, 255, 255);
                var ink = new Color32(46, 46, 46, 255); // 0.18
                for (int p = 0; p < px.Length; p++) px[p] = bg;

                const int glyphW = 5 * FontPx, glyphH = 7 * FontPx;
                const int offX = (CellPx - glyphW) / 2, offY = (CellPx - glyphH) / 2;
                for (int l = 0; l < 26; l++)
                {
                    int cellX = (l % AtlasCols) * CellPx, cellY = (l / AtlasCols) * CellPx;
                    for (int r = 0; r < 7; r++)
                    {
                        int bits = Font5x7[l * 7 + r];
                        int py0 = cellY + offY + (6 - r) * FontPx; // строка 0 шрифта — верхняя
                        for (int c = 0; c < 5; c++)
                        {
                            if ((bits & (0x10 >> c)) == 0) continue;
                            int px0 = cellX + offX + c * FontPx;
                            for (int yy = 0; yy < FontPx; yy++)
                                for (int xx = 0; xx < FontPx; xx++)
                                    px[(py0 + yy) * AtlasSize + px0 + xx] = ink;
                        }
                    }
                }
                atlas.SetPixels32(px);
                atlas.Apply(true);
                return atlas;
            }
        }

        /// <summary>Пиксельный шрифт 5x7, A–Z: по 7 строк сверху вниз, старший из 5 бит — левый столбец.</summary>
        static readonly byte[] Font5x7 =
        {
            0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11, // A
            0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E, // B
            0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E, // C
            0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C, // D
            0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F, // E
            0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10, // F
            0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F, // G
            0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11, // H
            0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E, // I
            0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C, // J
            0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11, // K
            0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F, // L
            0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11, // M
            0x11, 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, // N
            0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E, // O
            0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10, // P
            0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D, // Q
            0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11, // R
            0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E, // S
            0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, // T
            0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E, // U
            0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04, // V
            0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A, // W
            0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11, // X
            0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04, // Y
            0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F, // Z
        };
    }
}
