using UnityEngine;

namespace DragonHeist
{
    /// <summary>Сборщик "роблокс-моделей" из кубиков: персонаж R6, драконы, яйца, брейнроты, деревья, надписи.</summary>
    public static class Blocky
    {
        static readonly System.Collections.Generic.Dictionary<PrimitiveType, Mesh> meshes =
            new System.Collections.Generic.Dictionary<PrimitiveType, Mesh>();

        /// <summary>Общий меш примитива (берём один раз), чтобы не создавать и не удалять тысячи лишних коллайдеров.</summary>
        static Mesh GetMesh(PrimitiveType type)
        {
            Mesh m;
            if (meshes.TryGetValue(type, out m) && m != null) return m;
            var tmp = GameObject.CreatePrimitive(type);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            meshes[type] = m;
            return m;
        }

        /// <summary>Когда true, кубы строятся со скруглёнными краями (персонажи, брейнроты, драконы).</summary>
        public static bool Round;
        public static float RoundFactor = 0.2f;
        public static int RoundSteps = 1; // 1 = фаска (дёшево для мобилок), 2+ = плавнее

        public static Transform Part(Transform parent, Vector3 localPos, Vector3 size, Material mat,
            bool collider = false, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = new GameObject(type.ToString());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            if (Round && type == PrimitiveType.Cube && !collider)
            {
                float minSide = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                t.localScale = Vector3.one;
                go.AddComponent<MeshFilter>().sharedMesh = RoundedMesh.Box(size, minSide * RoundFactor, RoundSteps);
            }
            else if (type == PrimitiveType.Cube)
            {
                // куб запекается под размер, чтобы UV были в юнитах и студы не растягивались
                t.localScale = Vector3.one;
                go.AddComponent<MeshFilter>().sharedMesh = RoundedMesh.FlatBox(size);
            }
            else
            {
                t.localScale = size;
                go.AddComponent<MeshFilter>().sharedMesh = GetMesh(type);
            }
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                if (type == PrimitiveType.Sphere) go.AddComponent<SphereCollider>();
                else if (type == PrimitiveType.Cube) go.AddComponent<BoxCollider>();
                else go.AddComponent<CapsuleCollider>();
            }
            return t;
        }

        public static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static void NoShadows(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ================= ПЕРСОНАЖ (R6) =================
        public class Avatar
        {
            public GameObject root;
            public Transform model, lArm, rArm, lLeg, rLeg, head, carryPoint;
        }

        /// <summary>Классический "нуб": жёлтая голова и руки, синий торс, зелёные ноги. Ноги стоят на y=0.</summary>
        public static Avatar BuildAvatar(Transform parent, Color skin, Color shirt, Color pants, float scale)
        {
            var a = new Avatar();
            a.root = new GameObject("Avatar");
            a.root.transform.SetParent(parent, false);
            a.model = Pivot(a.root.transform, "Model", Vector3.zero);
            a.model.localScale = Vector3.one * scale;
            var m = a.model;

            var skinM = Plastic(skin); var shirtM = Plastic(shirt); var pantsM = Plastic(pants);
            Round = true;
            RoundSteps = 2;
            // Торс 2x2x1 (пропорции R6)
            RoundFactor = 0.12f;
            Part(m, new Vector3(0, 3f, 0), new Vector3(2f, 2f, 1f), shirtM);
            // Руки и ноги 1x2x1 с маленькими зазорами, чтобы детали читались как отдельные "кубики"
            a.lArm = Pivot(m, "LArm", new Vector3(-1.52f, 3.9f, 0));
            Part(a.lArm, new Vector3(0, -0.9f, 0), new Vector3(0.98f, 2f, 0.98f), skinM);
            a.rArm = Pivot(m, "RArm", new Vector3(1.52f, 3.9f, 0));
            Part(a.rArm, new Vector3(0, -0.9f, 0), new Vector3(0.98f, 2f, 0.98f), skinM);
            a.lLeg = Pivot(m, "LLeg", new Vector3(-0.5f, 2f, 0));
            Part(a.lLeg, new Vector3(0, -1f, 0), new Vector3(0.96f, 2f, 0.98f), pantsM);
            a.rLeg = Pivot(m, "RLeg", new Vector3(0.5f, 2f, 0));
            Part(a.rLeg, new Vector3(0, -1f, 0), new Vector3(0.96f, 2f, 0.98f), pantsM);
            // Голова: круглая "роблокс-голова" с крупным смайликом
            a.head = Pivot(m, "Head", new Vector3(0, 4.62f, 0));
            RoundFactor = 0.3f;
            var headT = new GameObject("HeadMesh").transform;
            headT.SetParent(a.head, false);
            headT.gameObject.AddComponent<MeshFilter>().sharedMesh = RoundedMesh.Box(new Vector3(1.2f, 1.2f, 1.2f), 0.36f, 4);
            headT.gameObject.AddComponent<MeshRenderer>().sharedMaterial = skinM;
            var face = Part(a.head, new Vector3(0, 0.02f, 0.602f), new Vector3(0.62f, 0.62f, 1f), Mats.Face(skin), false, PrimitiveType.Quad);
            face.localRotation = Quaternion.Euler(0, 180, 0);
            Round = false;
            RoundFactor = 0.2f;
            RoundSteps = 1;

            a.carryPoint = Pivot(m, "Carry", new Vector3(0, 6.6f, 0));
            return a;
        }

        static Material Plastic(Color c) { return Mats.Plastic(c); }

        // ================= ЯЙЦО =================
        /// <summary>
        /// Донатное Драконье яйцо (по рисунку): яйцо из блоков — жёлтые верх и низ, оранжевое тело с красными полосками,
        /// белый пояс посередине, по бокам машущие крылья (оранжевая пластина + веер жёлтых перьев).
        /// </summary>
        public static GameObject BuildPremiumEgg(Transform parent, float size)
        {
            var root = new GameObject("Egg_Premium");
            root.transform.SetParent(parent, false);
            float u = size * 0.16f;
            var yellow = Mats.Plastic(new Color(1f, 0.88f, 0.05f));
            var orange = Mats.Plastic(new Color(1f, 0.47f, 0.07f));
            var red = Mats.Plastic(new Color(0.95f, 0.08f, 0.08f));
            var white = Mats.Glow(new Color(1f, 1f, 1f));
            // слои снизу вверх: ширина (в блоках) и цвет
            float[] w = { 3f, 5f, 6f, 7f, 7f, 6f, 5f, 3f };
            Material[] m = { yellow, yellow, orange, orange, orange, orange, yellow, yellow };
            float y = 0;
            for (int i = 0; i < w.Length; i++)
            {
                if (i == 4) // белый пояс между 4-м и 5-м слоем
                {
                    Part(root.transform, new Vector3(0, y + 0.4f * u, 0), new Vector3(7.5f, 0.8f, 7.5f) * u, white);
                    y += 0.8f * u;
                }
                Part(root.transform, new Vector3(0, y + 0.5f * u, 0), new Vector3(w[i], 1f, w[i]) * u, m[i]);
                // красные полоски слева и справа на каждой грани (над и под поясом)
                if (i == 3 || i == 4)
                    for (int f = 0; f < 4; f++)
                    {
                        var face = Pivot(root.transform, "Face", new Vector3(0, y + 0.5f * u, 0));
                        face.localRotation = Quaternion.Euler(0, f * 90f, 0);
                        for (int sx = -1; sx <= 1; sx += 2)
                            Part(face, new Vector3(sx * 2.1f * u, 0, w[i] * 0.5f * u + 0.03f * u), new Vector3(2.2f, 0.7f, 0.1f) * u, red);
                    }
                y += u;
            }
            // крылья
            var flap = root.AddComponent<WingFlap>();
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = Pivot(root.transform, side < 0 ? "WingL" : "WingR", new Vector3(side * 3.4f * u, y * 0.55f, 0));
                var panel = Part(wing, new Vector3(side * 1.8f * u, 1.5f * u, 0), new Vector3(3.6f, 2.6f, 0.35f) * u, orange);
                panel.localRotation = Quaternion.Euler(0, 0, side * 36f);
                var tip = Part(wing, new Vector3(side * 3.6f * u, 3.6f * u, 0), new Vector3(1.8f, 1.8f, 0.3f) * u, orange);
                tip.localRotation = Quaternion.Euler(0, 0, side * 50f);
                for (int k = 0; k < 6; k++)
                {
                    float t = k / 5f;
                    var fe = Part(wing, new Vector3(side * (1.0f + t * 3.8f) * u, (0.1f + t * 3.0f) * u - 0.9f * u, 0.05f * u), new Vector3(0.6f, 2.2f - t * 0.4f, 0.28f) * u, yellow);
                    fe.localRotation = Quaternion.Euler(0, 0, side * (8f + t * 40f));
                }
                if (side < 0) flap.left = wing; else flap.right = wing;
            }
            Fx.Sparkles(root.transform, new Vector3(0, y * 0.5f, 0), new Color(1f, 0.85f, 0.3f), 3.5f * u, 4f);
            return root;
        }

        public static GameObject BuildEgg(Transform parent, Tier tier, float size, bool premium = false)
        {
            if (premium) return BuildPremiumEgg(parent, size);
            var info = GameConfig.GetTier(tier);
            var root = new GameObject("Egg_" + tier);
            root.transform.SetParent(parent, false);
            var body = Part(root.transform, new Vector3(0, 0.65f * size, 0), new Vector3(1f, 1.3f, 1f) * size,
                Mats.EggMat(info.color), false, PrimitiveType.Sphere);
            body.name = "Body";
            if (tier >= Tier.Legendary)
            {
                // корона-шипы для крутых яиц
                var crown = Mats.Plastic(Color.Lerp(info.color, Color.white, 0.5f));
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2 / 5;
                    var s = Part(root.transform, new Vector3(Mathf.Cos(a) * 0.3f, 1.35f, Mathf.Sin(a) * 0.3f) * size,
                        new Vector3(0.15f, 0.3f, 0.15f) * size, crown);
                    s.localRotation = Quaternion.Euler(Mathf.Sin(a) * 25, 0, -Mathf.Cos(a) * 25);
                }
            }
            if (tier >= Tier.Secret || tier == Tier.Mythic) root.AddComponent<RainbowTint>().target = body.GetComponent<Renderer>();
            return root;
        }

        // ================= ДРАКОН =================
        // Все детали разнесены минимум на 0.03 по параллельным граням (иначе грани "съедают" друг друга — z-fighting).
        // Выступающие детали (живот, пластины, глаза, зрачки, блики, румянец, полосы) явно выдвинуты наружу.

        static Color Hsv(float h, float s, float v) { return Color.HSVToRGB(Mathf.Repeat(h, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v)); }

        /// <summary>Сочнее и ярче: поднимаем насыщенность и яркость, серые/белые/чёрные оставляем нейтральными.</summary>
        static Color Vivid(Color c, float valFloor)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (s < 0.12f) return Hsv(h, s, Mathf.Max(v, 0.16f));
            return Hsv(h, s * 1.2f + 0.08f, Mathf.Max(v, valFloor) * 1.08f + 0.04f);
        }

        static float Sat(Color c) { float h, s, v; Color.RGBToHSV(c, out h, out s, out v); return s * Mathf.Min(1f, v * 2f); }
        static float Hue(Color c) { float h, s, v; Color.RGBToHSV(c, out h, out s, out v); return h; }
        static float Val(Color c) { float h, s, v; Color.RGBToHSV(c, out h, out s, out v); return v; }
        static float HueDist(float a, float b) { float d = Mathf.Abs(a - b); return Mathf.Min(d, 1f - d); }

        /// <summary>Контрастный акцент (полоски, когти, плавник): от цвета тела, крыльев или живота.</summary>
        static Color AccentColor(Color body, Color belly, Color wing)
        {
            float h;
            if (Sat(body) > 0.15f)
            {
                // если крылья уже контрастные (как у Грозовика) — берём их тон, иначе комплементарный к телу
                if (Sat(wing) > 0.3f && HueDist(Hue(body), Hue(wing)) > 0.2f) h = Hue(wing);
                else h = Hue(body) + 0.5f;
            }
            else if (Sat(wing) > 0.15f) h = Hue(wing);
            else if (Sat(belly) > 0.15f) h = Hue(belly);
            else h = 0.07f; // полностью серый дракон — тёплые "угольки"
            return Hsv(h, 0.85f, 1f);
        }

        public static GameObject BuildDragon(Transform parent, DragonDef d)
        {
            float s = (0.8f + Mathf.Min((int)d.tier, 11) * 0.11f) * (d.exclusive ? 1.15f : 1f);
            int tier = (int)d.tier;
            var root = new GameObject("Dragon_" + d.nameEn);
            root.transform.SetParent(parent, false);
            var m = Pivot(root.transform, "Model", Vector3.zero);
            m.localScale = Vector3.one * s;

            // ---- палитра (небольшой набор цветов — материалы кэшируются в Mats) ----
            Color cBody = Vivid(d.body, 0.32f), cBelly = Vivid(d.belly, 0.55f), cWing = Vivid(d.wing, 0.3f);
            Color cAccent = AccentColor(cBody, cBelly, cWing);
            Color cMembrane;
            if (Sat(cWing) < 0.15f && Val(cWing) > 0.6f) cMembrane = Color.Lerp(cAccent, Color.white, 0.45f); // белые крылья → цветная перепонка
            else if (Val(cWing) < 0.35f) cMembrane = Color.Lerp(cWing, cAccent, 0.5f);                      // тёмные крылья → перепонка с акцентом
            else cMembrane = Color.Lerp(cWing, Color.white, 0.38f);
            float accH = Hue(cAccent);
            Color cGlow = Color.Lerp(cAccent, Color.white, 0.3f);

            var body = Plastic(cBody); var belly = Plastic(cBelly); var wing = Plastic(cWing);
            var membrane = Plastic(cMembrane); var accent = Plastic(cAccent);
            var plate = Plastic(Color.Lerp(cBelly, cAccent, 0.35f));
            var white = Plastic(Color.white);
            var blush = Plastic(new Color(1f, 0.52f, 0.64f));
            var gold = Plastic(new Color(1f, 0.8f, 0.18f));
            var glow = Mats.Glow(cGlow);
            bool fancyEyes = tier >= (int)Tier.Secret || d.exclusive;
            var iris = fancyEyes ? Mats.Glow(cAccent) : Plastic(Hsv(accH, 0.85f, 0.28f));
            var horn = tier >= (int)Tier.Legendary ? gold : Plastic(Color.Lerp(cBelly, Color.white, 0.35f));
            var claw = tier >= (int)Tier.Mythic ? glow : accent;
            var spikeMat = tier >= (int)Tier.Epic ? glow : wing;
            var finMat = tier >= (int)Tier.Legendary ? glow : accent;
            var stripeMat = d.exclusive ? Mats.Glow(cAccent) : accent;

            Round = true;
            RoundFactor = 0.2f;
            RoundSteps = 2;

            // ---- туловище: живот выступает спереди (+0.13) и снизу (+0.08), по бокам утоплен ----
            Part(m, new Vector3(0, 1.3f, 0), new Vector3(1.6f, 1.3f, 2.4f), body);             // x±0.8 y0.65..1.95 z±1.2
            Part(m, new Vector3(0, 1.02f, 0.38f), new Vector3(1.2f, 0.9f, 1.9f), belly);      // x±0.6 y0.57..1.47 z-0.57..1.33
            RoundSteps = 1;
            // чешуйчатые пластины на груди (выступают на 0.09 из живота)
            Part(m, new Vector3(0, 0.84f, 1.36f), new Vector3(0.86f, 0.13f, 0.12f), plate);
            Part(m, new Vector3(0, 1.14f, 1.36f), new Vector3(0.86f, 0.13f, 0.12f), plate);
            // шея (повёрнута — параллельных граней с туловищем и головой нет)
            var neck = Part(m, new Vector3(0, 2.1f, 1.1f), new Vector3(0.8f, 1.2f, 0.8f), body);
            neck.localRotation = Quaternion.Euler(25, 0, 0);

            // цветные полосы-обручи на спине: на 0.04 шире/выше туловища
            int stripes = (tier >= (int)Tier.Legendary && !d.exclusive) ? 3 : 2;
            for (int i = 0; i < stripes; i++)
            {
                float z = stripes == 3 ? 0.55f - i * 0.6f : 0.35f - i * 0.7f;
                Part(m, new Vector3(0, 1.595f, z), new Vector3(1.68f, 0.79f, 0.24f), stripeMat);  // x±0.84 y1.2..1.99
            }

            // ---- голова ----
            var head = Pivot(m, "Head", new Vector3(0, 2.8f, 1.6f));
            RoundFactor = 0.16f; RoundSteps = 2;
            Part(head, Vector3.zero, new Vector3(1.1f, 0.9f, 1.1f), body);                     // x±0.55 y±0.45 z±0.55
            Part(head, new Vector3(0, -0.2f, 0.72f), new Vector3(0.72f, 0.4f, 0.7f), body);    // морда: x±0.36 y-0.4..0 z0.37..1.07
            RoundFactor = 0.2f; RoundSteps = 1;
            Part(head, new Vector3(0, -0.43f, 0.7f), new Vector3(0.62f, 0.14f, 0.62f), belly); // челюсть: x±0.31 y-0.5..-0.36 z0.39..1.01
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side;
                // белок (z0.50..0.62, выступает на 0.07) → зрачок (+0.05) → блик (+0.04); блики с одной стороны — "глянец"
                Part(head, new Vector3(0.25f * sx, 0.21f, 0.56f), new Vector3(0.28f, 0.32f, 0.12f), white);
                Part(head, new Vector3(0.23f * sx, 0.19f, 0.64f), new Vector3(0.16f, 0.22f, 0.06f), iris);
                Part(head, new Vector3(0.23f * sx + 0.04f, 0.25f, 0.68f), new Vector3(0.07f, 0.07f, 0.06f), white);
                // румянец: под глазами, сбоку от морды (зазор 0.03), выступает на 0.05
                Part(head, new Vector3(0.44f * sx, -0.06f, 0.52f), new Vector3(0.1f, 0.1f, 0.16f), blush);
                // рога: наклонены назад и в стороны; с Легендарных — большие золотые
                bool big = tier >= (int)Tier.Legendary;
                var h = Part(head, new Vector3(0.3f * sx, big ? 0.7f : 0.62f, -0.25f),
                    big ? new Vector3(0.2f, 0.72f, 0.2f) : new Vector3(0.17f, 0.52f, 0.17f), horn);
                h.localRotation = Quaternion.Euler(-30, 0, -15 * sx);
            }
            if (tier >= (int)Tier.Divine && !d.exclusive)
            {
                // светящийся камень во лбу
                var gem = Part(head, new Vector3(0, 0.47f, 0.22f), new Vector3(0.16f, 0.1f, 0.16f), glow);
                gem.localRotation = Quaternion.Euler(0, 45, 0);
            }

            // ---- крылья: кость по переднему краю, светлая перепонка, ребро, ромб-кончик ----
            // толщины: перепонка 0.08 < ребро 0.14 < кончик 0.2 < кость 0.22 (зазоры ≥0.03)
            var lw = Pivot(m, "LWing", new Vector3(-0.72f, 1.8f, 0.05f));
            var rw = Pivot(m, "RWing", new Vector3(0.72f, 1.8f, 0.05f));
            for (int side = -1; side <= 1; side += 2)
            {
                var w = side < 0 ? lw : rw;
                float sx = side;
                Part(w, new Vector3(1.1f * sx, 0, 0.62f), new Vector3(2.2f, 0.22f, 0.24f), wing);       // кость z0.50..0.74
                Part(w, new Vector3(1.05f * sx, 0, -0.1f), new Vector3(2.0f, 0.08f, 1.35f), membrane);  // перепонка z-0.775..0.575
                Part(w, new Vector3(1.0f * sx, 0, -0.18f), new Vector3(0.12f, 0.14f, 1.3f), wing);      // ребро торчит за край на 0.06
                var tip = Part(w, new Vector3(1.95f * sx, 0, -0.35f), new Vector3(0.8f, 0.2f, 0.8f), wing);
                tip.localRotation = Quaternion.Euler(0, 45, 0);
                if (d.exclusive)
                {
                    // эксклюзив: вторая, светящаяся пара крыльев ниже и позади основной
                    var w2 = Pivot(w, side < 0 ? "LWing2" : "RWing2", new Vector3(-0.05f * sx, -0.28f, -0.55f));
                    w2.localRotation = Quaternion.Euler(0, 0, -12 * sx);
                    Part(w2, new Vector3(0.85f * sx, 0, 0), new Vector3(1.7f, 0.08f, 1.0f), glow);
                }
            }

            // ---- хвост: загибается вверх, у каждого сегмента свой наклон → нет параллельных граней ----
            var t1 = Part(m, new Vector3(0, 1.22f, -1.6f), new Vector3(0.78f, 0.66f, 1.0f), body);
            t1.localRotation = Quaternion.Euler(8, 0, 0);
            var t2 = Part(m, new Vector3(0, 1.38f, -2.42f), new Vector3(0.56f, 0.5f, 0.9f), body);
            t2.localRotation = Quaternion.Euler(18, 0, 0);
            var t3 = Part(m, new Vector3(0, 1.6f, -3.1f), new Vector3(0.38f, 0.36f, 0.8f), body);
            t3.localRotation = Quaternion.Euler(30, 0, 0);
            t1.name = "Tail1"; t2.name = "Tail2"; t3.name = "Tail3";
            // плавник-"пика" (плоский ромб, наклонён как последний сегмент, грань выше его на 0.14)
            var fin = Part(m, new Vector3(0, 1.92f, -3.66f), new Vector3(0.66f, 0.08f, 0.66f), finMat);
            fin.localRotation = Quaternion.Euler(30, 0, 0) * Quaternion.Euler(0, 45, 0);
            fin.name = "TailFin";

            // ---- лапы и цветные когти (когти шире лапы на 0.03 с каждой стороны и приподняты над землёй) ----
            for (int i = 0; i < 4; i++)
            {
                float lx = i % 2 == 0 ? -0.55f : 0.55f, lz = i < 2 ? 0.8f : -0.7f;
                Part(m, new Vector3(lx, 0.35f, lz), new Vector3(0.45f, 0.7f, 0.45f), body);
                if (i < 2) Part(m, new Vector3(lx, 0.11f, lz + 0.255f), new Vector3(0.51f, 0.14f, 0.16f), claw);
            }

            // ---- гребень: число шипов растёт с тиром; эксклюзивам — светящиеся кристаллы ----
            int spikes = d.exclusive ? 4 : Mathf.Min(2 + tier / 3, 5);
            float z0 = 0.7f, z1 = -1.0f;
            for (int i = 0; i < spikes; i++)
            {
                float t = spikes > 1 ? i / (float)(spikes - 1) : 0f;
                float z = Mathf.Lerp(z0, z1, t);
                float hgt = (d.exclusive ? 0.8f : 0.5f) - Mathf.Abs(t - 0.35f) * (d.exclusive ? 0.3f : 0.25f);
                Transform sp;
                if (d.exclusive)
                {
                    sp = Part(m, new Vector3(0, 1.95f + hgt * 0.3f, z), new Vector3(0.22f, hgt, 0.22f), glow);
                    sp.localRotation = Quaternion.Euler(-25, 0, 0) * Quaternion.Euler(0, 45, 0);
                }
                else
                {
                    sp = Part(m, new Vector3(0, 1.95f + hgt * 0.3f, z), new Vector3(0.14f, hgt, 0.24f), spikeMat);
                    sp.localRotation = Quaternion.Euler(-25, 0, 0);
                }
            }

            if (d.exclusive)
            {
                // корона: обод на 0.05 шире головы и выше глаз на 0.04, три зубца (центральный — светящийся)
                Part(head, new Vector3(0, 0.48f, 0), new Vector3(1.2f, 0.16f, 1.2f), gold);        // x,z±0.6 y0.40..0.56
                for (int i = -1; i <= 1; i++)
                {
                    var pt = Part(head, new Vector3(0.36f * i, 0.66f, 0.42f), new Vector3(0.15f, i == 0 ? 0.38f : 0.3f, 0.15f), i == 0 ? glow : gold);
                    pt.localRotation = Quaternion.Euler(0, 45, 0);
                }
                // полупрозрачная сфера-аура убрана: она мерцала при сортировке с частицами; свечение даёт Glow-детали
            }

            Round = false;
            RoundFactor = 0.2f;
            RoundSteps = 1;
            var idle = root.AddComponent<DragonIdle>();
            idle.lWing = lw; idle.rWing = rw; idle.head = head; idle.model = m;
            AttachDragonFx(root.transform, m, head, d, s, body);
            return root;
        }

        /// <summary>Эффекты редких драконов: огонь, иней, аура, нимб, радуга, звёзды, пустота.</summary>
        static void AttachDragonFx(Transform root, Transform model, Transform head, DragonDef d, float s, Material bodyMat)
        {
            Color sparkle = Color.Lerp(Vivid(d.wing, 0.3f), Color.white, 0.4f);
            switch (d.fx)
            {
                case DragonFx.Sparkle: Fx.Sparkles(root, new Vector3(0, 1.8f * s, 0), sparkle, 1.5f * s, 6f); break;
                case DragonFx.Frost: Fx.Sparkles(root, new Vector3(0, 2.2f * s, 0), new Color(0.7f, 0.95f, 1f), 1.8f * s, 8f); break;
                case DragonFx.Fire: Fx.Flames(root, new Vector3(0, 1.2f * s, 0), 1.2f * s); break;
                case DragonFx.Aura: Fx.Sparkles(root, new Vector3(0, 1.2f * s, 0), Vivid(d.belly, 0.55f), 2.4f * s, 14f); break;
                case DragonFx.Stars: Fx.Sparkles(root, new Vector3(0, 2.5f * s, 0), Color.white, 3f * s, 10f); break;
                case DragonFx.Void: Fx.Sparkles(root, new Vector3(0, 1.5f * s, 0), new Color(0.5f, 0.1f, 1f), 2f * s, 12f); break;
                case DragonFx.Rainbow:
                    foreach (var r in model.GetComponentsInChildren<Renderer>())
                        if (r.sharedMaterial == bodyMat) root.gameObject.AddComponent<RainbowTint>().target = r;
                    Fx.Sparkles(root, new Vector3(0, 2f * s, 0), Color.white, 2f * s, 8f);
                    break;
                case DragonFx.Halo:
                {
                    // выше рогов и короны (верх рогов ≈1.0, короны ≈0.85 в координатах головы)
                    var halo = Part(head, new Vector3(0, 1.25f, -0.1f), new Vector3(1.4f, 1.4f, 1f), Mats.UnlitAlpha(new Color(1f, 0.9f, 0.4f, 1f), Mats.RingTexture), false, PrimitiveType.Quad);
                    halo.localRotation = Quaternion.Euler(90, 0, 0);
                    Fx.Sparkles(root, new Vector3(0, 2.5f * s, 0), new Color(1f, 0.9f, 0.5f), 1.2f * s, 5f);
                    break;
                }
            }
        }

        // ================= БРЕЙНРОТЫ =================
        public class Guard
        {
            public GameObject root;
            public Transform model;
            public Transform[] legs;
            public Transform[] arms;
        }

        public static Guard BuildBrainrot(Transform parent, BrainrotKind kind, float scale)
        {
            var g = new Guard();
            g.root = new GameObject("Brainrot_" + kind);
            g.root.transform.SetParent(parent, false);
            g.model = Pivot(g.root.transform, "Model", Vector3.zero);
            g.model.localScale = Vector3.one * scale;
            var m = g.model;
            Round = true; RoundFactor = 0.22f; RoundSteps = 2;
            var white = Plastic(Color.white); var black = Plastic(new Color(0.07f, 0.07f, 0.07f));
            var red = Plastic(new Color(0.9f, 0.18f, 0.18f));
            var tooth = Plastic(new Color(1f, 0.98f, 0.9f));
            Transform l1, l2, a1 = null, a2 = null;

            switch (kind)
            {
                case BrainrotKind.TungSahur:
                {
                    // Тун Тун Тун Сахур: деревянное полено-человечек с круглыми глазами, жуткой улыбкой и битой
                    var wood = Plastic(new Color(0.8f, 0.58f, 0.36f)); var bark = Plastic(new Color(0.55f, 0.36f, 0.2f));
                    var ring = Plastic(new Color(0.93f, 0.76f, 0.52f));
                    Part(m, new Vector3(0, 3.5f, 0), new Vector3(1.7f, 1.55f, 1.7f), wood, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 5.06f, 0), new Vector3(1.5f, 0.02f, 1.5f), ring, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 5.08f, 0), new Vector3(0.9f, 0.02f, 0.9f), wood, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 5.1f, 0), new Vector3(0.35f, 0.02f, 0.35f), ring, false, PrimitiveType.Cylinder);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f + 30f; if (a > 60f && a < 120f) continue; // спереди лицо без коры
                        var st = Part(m, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.84f, 3.4f + (i % 2) * 0.3f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.84f),
                            new Vector3(0.12f, 1.6f + (i % 3) * 0.4f, 0.1f), bark);
                        st.localRotation = Quaternion.Euler(0, a, 0);
                    }
                    Eyes(m, new Vector3(0, 4.25f, 0.84f), 0.36f, white, black, 0f);
                    Brows(m, new Vector3(0, 4.78f, 0.86f), 0.4f, bark, 12f);
                    // широкая улыбка
                    Part(m, new Vector3(0, 3.45f, 0.84f), new Vector3(0.95f, 0.14f, 0.06f), black);
                    Part(m, new Vector3(-0.52f, 3.56f, 0.83f), new Vector3(0.14f, 0.26f, 0.06f), black);
                    Part(m, new Vector3(0.52f, 3.56f, 0.83f), new Vector3(0.14f, 0.26f, 0.06f), black);
                    Part(m, new Vector3(0, 3.8f, 0.9f), new Vector3(0.22f, 0.28f, 0.12f), bark); // нос-сучок
                    l1 = Leg(m, new Vector3(-0.42f, 1.95f, 0), 1.95f, 0.32f, wood);
                    l2 = Leg(m, new Vector3(0.42f, 1.95f, 0), 1.95f, 0.32f, wood);
                    Foot(l1, 1.95f, bark); Foot(l2, 1.95f, bark);
                    a1 = Leg(m, new Vector3(-0.98f, 4f, 0), 1.7f, 0.26f, wood);
                    a2 = Leg(m, new Vector3(0.98f, 4f, 0), 1.7f, 0.26f, wood);
                    // бейсбольная бита: ручка + толстая часть
                    var batRoot = Pivot(a2, "Bat", new Vector3(0, -1.7f, 0.1f));
                    batRoot.localRotation = Quaternion.Euler(-35, 0, 0);
                    var batWood = Plastic(new Color(0.62f, 0.4f, 0.22f));
                    Part(batRoot, new Vector3(0, 0, 0.5f), new Vector3(0.14f, 0.5f, 0.14f), batWood, false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
                    Part(batRoot, new Vector3(0, 0, 1.7f), new Vector3(0.3f, 0.75f, 0.3f), batWood, false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
                    Part(batRoot, new Vector3(0, 0, 0.02f), new Vector3(0.2f, 0.04f, 0.2f), black, false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                }
                case BrainrotKind.Lirili:
                {
                    // Лирили Ларила: голова слона, тело-кактус, сандалии
                    var cactus = Plastic(new Color(0.32f, 0.72f, 0.3f)); var dark = Plastic(new Color(0.2f, 0.52f, 0.2f));
                    var grey = Plastic(new Color(0.66f, 0.66f, 0.72f)); var ear = Plastic(new Color(0.9f, 0.62f, 0.68f));
                    var sandal = Plastic(new Color(0.58f, 0.36f, 0.2f));
                    Part(m, new Vector3(0, 2.8f, 0), new Vector3(1.5f, 1.05f, 1.5f), cactus, false, PrimitiveType.Cylinder);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f * Mathf.Deg2Rad;
                        Part(m, new Vector3(Mathf.Sin(a) * 0.74f, 2.8f, Mathf.Cos(a) * 0.74f), new Vector3(0.1f, 1.9f, 0.1f), dark).localRotation = Quaternion.Euler(0, i * 45f, 0);
                        for (int j = 0; j < 3; j++)
                            Part(m, new Vector3(Mathf.Sin(a + 0.4f) * 0.8f, 2.1f + j * 0.6f, Mathf.Cos(a + 0.4f) * 0.8f), new Vector3(0.04f, 0.04f, 0.2f), white).localRotation = Quaternion.Euler(0, i * 45f + 23f, 0);
                    }
                    // руки-отростки кактуса
                    Part(m, new Vector3(-1.05f, 2.9f, 0), new Vector3(0.7f, 0.36f, 0.36f), cactus);
                    Part(m, new Vector3(-1.3f, 3.35f, 0), new Vector3(0.36f, 0.9f, 0.36f), cactus);
                    Part(m, new Vector3(1.05f, 2.5f, 0), new Vector3(0.7f, 0.36f, 0.36f), cactus);
                    Part(m, new Vector3(1.3f, 2.9f, 0), new Vector3(0.36f, 0.8f, 0.36f), cactus);
                    // голова слона
                    Part(m, new Vector3(0, 4.45f, 0.1f), new Vector3(1.55f, 1.35f, 1.35f), grey);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        var e = Part(m, new Vector3(sx * 1.05f, 4.45f, -0.05f), new Vector3(0.14f, 1.35f, 1.2f), grey);
                        e.localRotation = Quaternion.Euler(0, sx * -25f, 0);
                        var ei = Part(e, new Vector3(sx * -0.04f, 0, 0.02f), new Vector3(0.1f, 1.0f, 0.85f), ear);
                        ei.localRotation = Quaternion.identity;
                        Part(m, new Vector3(sx * 0.42f, 3.95f, 0.85f), new Vector3(0.12f, 0.12f, 0.55f), tooth).localRotation = Quaternion.Euler(-35, 0, 0);
                    }
                    // хобот из 3 сегментов, загнут
                    var tr = Pivot(m, "Trunk", new Vector3(0, 4.2f, 0.8f));
                    var t1 = Part(tr, new Vector3(0, -0.3f, 0.05f), new Vector3(0.42f, 0.65f, 0.42f), grey); t1.localRotation = Quaternion.Euler(15, 0, 0);
                    var t2 = Part(tr, new Vector3(0, -0.85f, 0.25f), new Vector3(0.34f, 0.55f, 0.34f), grey); t2.localRotation = Quaternion.Euler(35, 0, 0);
                    var t3 = Part(tr, new Vector3(0, -1.2f, 0.55f), new Vector3(0.28f, 0.4f, 0.28f), grey); t3.localRotation = Quaternion.Euler(70, 0, 0);
                    Eyes(m, new Vector3(0, 4.7f, 0.79f), 0.24f, white, black, 0f);
                    // розовый цветок на макушке
                    Part(m, new Vector3(0.3f, 5.2f, 0), new Vector3(0.36f, 0.14f, 0.36f), Plastic(new Color(1f, 0.45f, 0.7f)));
                    Part(m, new Vector3(0.3f, 5.3f, 0), new Vector3(0.14f, 0.1f, 0.14f), Plastic(new Color(1f, 0.9f, 0.3f)));
                    l1 = Leg(m, new Vector3(-0.42f, 1.75f, 0), 1.6f, 0.5f, grey);
                    l2 = Leg(m, new Vector3(0.42f, 1.75f, 0), 1.6f, 0.5f, grey);
                    foreach (var l in new[] { l1, l2 })
                    {
                        Part(l, new Vector3(0, -1.7f, 0.12f), new Vector3(0.62f, 0.12f, 0.9f), sandal);
                        Part(l, new Vector3(0, -1.5f, 0.2f), new Vector3(0.56f, 0.1f, 0.12f), sandal);
                        for (int k = -1; k <= 1; k++) Part(l, new Vector3(k * 0.16f, -1.58f, 0.46f), new Vector3(0.12f, 0.12f, 0.1f), white);
                    }
                    break;
                }
                case BrainrotKind.Bombardiro:
                {
                    // Бомбардиро Крокодило: крокодил-бомбардировщик с пропеллерами и бомбами
                    var croc = Plastic(new Color(0.3f, 0.58f, 0.24f)); var crocDark = Plastic(new Color(0.2f, 0.42f, 0.16f));
                    var belly = Plastic(new Color(0.86f, 0.84f, 0.55f)); var metal = Plastic(new Color(0.46f, 0.52f, 0.44f));
                    var steel = Plastic(new Color(0.75f, 0.78f, 0.8f));
                    Part(m, new Vector3(0, 2.4f, 0), new Vector3(1.5f, 1.3f, 3.4f), croc);
                    Part(m, new Vector3(0, 2.05f, 0.1f), new Vector3(1.3f, 0.6f, 3.0f), belly);
                    for (int i = 0; i < 5; i++) Part(m, new Vector3(0, 3.1f, -1.2f + i * 0.55f), new Vector3(0.36f, 0.22f, 0.3f), crocDark); // гребень
                    // голова: верхняя и нижняя челюсти с зубами
                    Part(m, new Vector3(0, 2.75f, 2.2f), new Vector3(1.15f, 0.5f, 1.8f), croc);
                    Part(m, new Vector3(0, 2.2f, 2.1f), new Vector3(1.05f, 0.35f, 1.6f), belly);
                    for (int i = 0; i < 5; i++)
                    {
                        Part(m, new Vector3(-0.5f, 2.43f, 1.6f + i * 0.3f), new Vector3(0.08f, 0.2f, 0.08f), tooth);
                        Part(m, new Vector3(0.5f, 2.43f, 1.6f + i * 0.3f), new Vector3(0.08f, 0.2f, 0.08f), tooth);
                    }
                    Part(m, new Vector3(-0.25f, 3.05f, 3.0f), new Vector3(0.12f, 0.1f, 0.1f), black);
                    Part(m, new Vector3(0.25f, 3.05f, 3.0f), new Vector3(0.12f, 0.1f, 0.1f), black);
                    Eyes(m, new Vector3(0, 3.2f, 1.45f), 0.28f, white, black, 0f);
                    Brows(m, new Vector3(0, 3.55f, 1.46f), 0.3f, crocDark, -18f);
                    // крылья самолёта с красными звёздами и двигатели
                    Part(m, new Vector3(0, 2.7f, 0.2f), new Vector3(6.4f, 0.16f, 1.3f), metal);
                    Part(m, new Vector3(0, 2.62f, -2.1f), new Vector3(2.4f, 0.12f, 0.7f), metal);
                    Part(m, new Vector3(0, 3.2f, -2.1f), new Vector3(0.12f, 1.0f, 0.7f), metal);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Part(m, new Vector3(sx * 2.4f, 2.8f, 0.2f), new Vector3(0.5f, 0.02f, 0.5f), red, false, PrimitiveType.Cylinder);
                        Part(m, new Vector3(sx * 1.6f, 2.5f, 0.55f), new Vector3(0.5f, 0.55f, 0.5f), steel, false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
                        var prop = Pivot(m, "Prop", new Vector3(sx * 1.6f, 2.5f, 1.12f));
                        Part(prop, Vector3.zero, new Vector3(1.5f, 0.18f, 0.05f), black);
                        Part(prop, Vector3.zero, new Vector3(0.18f, 1.5f, 0.05f), black);
                        Part(prop, new Vector3(0, 0, 0.05f), new Vector3(0.2f, 0.2f, 0.2f), red);
                        prop.gameObject.AddComponent<Spinner>().speed = sx * 900f;
                    }
                    // бомбы под брюхом
                    foreach (float bz in new[] { -0.6f, 0.4f })
                    {
                        Part(m, new Vector3(0, 1.45f, bz), new Vector3(0.45f, 0.45f, 0.8f), Plastic(new Color(0.18f, 0.2f, 0.18f)), false, PrimitiveType.Capsule).localRotation = Quaternion.Euler(90, 0, 0);
                    }
                    var tail = Part(m, new Vector3(0, 2.3f, -2.3f), new Vector3(0.7f, 0.55f, 1.4f), croc); tail.localRotation = Quaternion.Euler(-10, 0, 0);
                    l1 = Leg(m, new Vector3(-0.55f, 1.8f, 0.6f), 1.7f, 0.38f, croc);
                    l2 = Leg(m, new Vector3(0.55f, 1.8f, 0.6f), 1.7f, 0.38f, croc);
                    Foot(l1, 1.7f, crocDark); Foot(l2, 1.7f, crocDark);
                    break;
                }
                case BrainrotKind.Tralalero:
                {
                    // Тралалеро Тралала: голубая акула на ногах в синих кроссовках
                    var shark = Plastic(new Color(0.36f, 0.6f, 0.9f)); var shoe = Plastic(new Color(0.12f, 0.35f, 0.95f));
                    Part(m, new Vector3(0, 2.6f, 0), new Vector3(1.4f, 1.4f, 3.6f), shark);
                    Part(m, new Vector3(0, 2.2f, 0.4f), new Vector3(1.2f, 0.75f, 2.9f), white);
                    Part(m, new Vector3(0, 2.75f, 2.1f), new Vector3(1.15f, 1.0f, 1.0f), shark); // морда
                    var dorsal = Part(m, new Vector3(0, 3.65f, -0.1f), new Vector3(0.18f, 1.2f, 1.0f), shark); dorsal.localRotation = Quaternion.Euler(-25, 0, 0);
                    var tailRoot = Pivot(m, "Tail", new Vector3(0, 2.7f, -1.9f));
                    Part(tailRoot, new Vector3(0, 0.45f, -0.35f), new Vector3(0.16f, 1.1f, 0.6f), shark).localRotation = Quaternion.Euler(-35, 0, 0);
                    Part(tailRoot, new Vector3(0, -0.3f, -0.3f), new Vector3(0.16f, 0.7f, 0.5f), shark).localRotation = Quaternion.Euler(35, 0, 0);
                    tailRoot.gameObject.AddComponent<Wiggle>().amount = 18f;
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        var fin = Part(m, new Vector3(sx * 0.85f, 2.2f, 0.7f), new Vector3(0.8f, 0.12f, 0.55f), shark);
                        fin.localRotation = Quaternion.Euler(0, sx * -20f, sx * -25f);
                        for (int i = 0; i < 3; i++) Part(m, new Vector3(sx * 0.72f, 2.75f, 0.6f + i * 0.25f), new Vector3(0.04f, 0.3f, 0.06f), Plastic(new Color(0.22f, 0.4f, 0.65f))); // жабры
                    }
                    // пасть с зубами
                    Part(m, new Vector3(0, 2.32f, 2.58f), new Vector3(0.9f, 0.2f, 0.08f), red);
                    for (int i = -3; i <= 3; i++) Part(m, new Vector3(i * 0.12f, 2.43f, 2.62f), new Vector3(0.08f, 0.12f, 0.05f), tooth);
                    Eyes(m, new Vector3(0, 3.02f, 2.6f), 0.24f, white, black, 0f);
                    l1 = Leg(m, new Vector3(-0.45f, 1.9f, 0.3f), 1.6f, 0.3f, shark);
                    l2 = Leg(m, new Vector3(0.45f, 1.9f, 0.3f), 1.6f, 0.3f, shark);
                    foreach (var l in new[] { l1, l2 })
                    {
                        Part(l, new Vector3(0, -1.7f, 0.18f), new Vector3(0.55f, 0.4f, 0.95f), shoe);
                        Part(l, new Vector3(0, -1.88f, 0.2f), new Vector3(0.6f, 0.12f, 1.0f), white); // подошва
                        Part(l, new Vector3(0.28f, -1.7f, 0.15f), new Vector3(0.03f, 0.1f, 0.45f), white).localRotation = Quaternion.Euler(-15, 0, 0); // "галочка"
                        Part(l, new Vector3(-0.28f, -1.7f, 0.15f), new Vector3(0.03f, 0.1f, 0.45f), white).localRotation = Quaternion.Euler(-15, 0, 0);
                    }
                    break;
                }
                case BrainrotKind.Patapim:
                {
                    // Брр Брр Патапим: лесное существо с огромным носом, листвой на голове и большими ступнями
                    var bark = Plastic(new Color(0.5f, 0.33f, 0.18f)); var leaf = Plastic(new Color(0.24f, 0.66f, 0.24f));
                    var leaf2 = Plastic(new Color(0.36f, 0.78f, 0.3f)); var skin = Plastic(new Color(0.92f, 0.7f, 0.55f));
                    var nose = Plastic(new Color(0.95f, 0.55f, 0.45f));
                    Part(m, new Vector3(0, 3.3f, 0), new Vector3(1.3f, 2.0f, 1.1f), bark);
                    Part(m, new Vector3(0, 3.0f, 0.5f), new Vector3(0.9f, 1.2f, 0.2f), Plastic(new Color(0.62f, 0.44f, 0.26f)));
                    Part(m, new Vector3(0, 4.9f, 0), new Vector3(1.6f, 1.3f, 1.4f), skin);          // голова
                    Part(m, new Vector3(0, 4.55f, 1.05f), new Vector3(0.7f, 0.9f, 0.9f), nose);     // огромный нос
                    Part(m, new Vector3(0, 4.2f, 1.35f), new Vector3(0.5f, 0.3f, 0.35f), nose);
                    Part(m, new Vector3(0, 4.1f, 0.72f), new Vector3(0.6f, 0.1f, 0.05f), black);    // рот под носом
                    Eyes(m, new Vector3(0, 5.2f, 0.72f), 0.25f, white, black, 0f);
                    Part(m, new Vector3(0, 5.8f, 0), new Vector3(2.4f, 0.9f, 2.2f), leaf);
                    Part(m, new Vector3(0.35f, 6.3f, -0.2f), new Vector3(1.6f, 0.7f, 1.5f), leaf2);
                    Part(m, new Vector3(-0.5f, 6.1f, 0.4f), new Vector3(1.0f, 0.5f, 1.0f), leaf2);
                    Part(m, new Vector3(0.6f, 5.6f, 1f), new Vector3(0.3f, 0.3f, 0.3f), red); // ягодка
                    l1 = Leg(m, new Vector3(-0.38f, 2.3f, 0), 2.2f, 0.26f, bark);
                    l2 = Leg(m, new Vector3(0.38f, 2.3f, 0), 2.2f, 0.26f, bark);
                    foreach (var l in new[] { l1, l2 })
                    {
                        Part(l, new Vector3(0, -2.25f, 0.35f), new Vector3(0.7f, 0.22f, 1.2f), skin); // большая ступня
                        for (int k = -1; k <= 1; k++) Part(l, new Vector3(k * 0.22f, -2.25f, 0.98f), new Vector3(0.18f, 0.18f, 0.18f), skin);
                    }
                    a1 = Leg(m, new Vector3(-0.85f, 4.1f, 0), 2.0f, 0.22f, bark);
                    a2 = Leg(m, new Vector3(0.85f, 4.1f, 0), 2.0f, 0.22f, bark);
                    foreach (var a in new[] { a1, a2 }) Part(a, new Vector3(0, -2.05f, 0), new Vector3(0.4f, 0.35f, 0.3f), skin);
                    break;
                }
                case BrainrotKind.Cappuccino:
                {
                    // Капучино Ассасино: чашка капучино в маске ниндзя с катанами
                    var cup = Plastic(new Color(0.97f, 0.95f, 0.92f)); var coffee = Plastic(new Color(0.55f, 0.35f, 0.2f));
                    var foam = Plastic(new Color(0.95f, 0.85f, 0.7f)); var mask = Plastic(new Color(0.12f, 0.12f, 0.16f));
                    var blade = Plastic(new Color(0.85f, 0.9f, 0.95f));
                    Part(m, new Vector3(0, 2.05f, 0), new Vector3(2.4f, 0.08f, 2.4f), cup, false, PrimitiveType.Cylinder); // блюдце
                    Part(m, new Vector3(0, 3.2f, 0), new Vector3(2f, 1.1f, 2f), cup, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 4.32f, 0), new Vector3(1.9f, 0.04f, 1.9f), coffee, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 4.36f, 0), new Vector3(1.2f, 0.03f, 1.2f), foam, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 4.39f, 0.1f), new Vector3(0.5f, 0.02f, 0.4f), coffee, false, PrimitiveType.Cylinder); // латте-арт сердечко
                    var handle = Part(m, new Vector3(-1.15f, 3.2f, 0), new Vector3(0.5f, 0.9f, 0.2f), cup);
                    handle.localRotation = Quaternion.identity;
                    // маска ниндзя с прорезью и красная повязка с хвостами
                    Part(m, new Vector3(0, 3.55f, 0), new Vector3(2.08f, 0.35f, 2.08f), mask, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 3.95f, 0), new Vector3(2.06f, 0.07f, 2.06f), red, false, PrimitiveType.Cylinder);
                    var knot1 = Part(m, new Vector3(0.25f, 3.8f, -1.3f), new Vector3(0.18f, 0.7f, 0.08f), red); knot1.localRotation = Quaternion.Euler(30, 0, 20);
                    var knot2 = Part(m, new Vector3(-0.1f, 3.75f, -1.3f), new Vector3(0.18f, 0.8f, 0.08f), red); knot2.localRotation = Quaternion.Euler(30, 0, -15);
                    Eyes(m, new Vector3(0, 3.58f, 1.02f), 0.2f, white, black, 0f);
                    Brows(m, new Vector3(0, 3.78f, 1.06f), 0.24f, black, -22f);
                    // катаны за спиной крест-накрест
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        var k = Pivot(m, "Katana", new Vector3(0, 3.4f, -1.15f));
                        k.localRotation = Quaternion.Euler(0, 0, sx * 35f);
                        Part(k, new Vector3(0, 0.9f, 0), new Vector3(0.1f, 1.8f, 0.05f), blade);
                        Part(k, new Vector3(0, -0.1f, 0), new Vector3(0.35f, 0.08f, 0.12f), Plastic(new Color(0.85f, 0.7f, 0.2f)));
                        Part(k, new Vector3(0, -0.45f, 0), new Vector3(0.12f, 0.6f, 0.12f), mask);
                    }
                    l1 = Leg(m, new Vector3(-0.45f, 2.0f, 0), 2.0f, 0.26f, mask);
                    l2 = Leg(m, new Vector3(0.45f, 2.0f, 0), 2.0f, 0.26f, mask);
                    Foot(l1, 2.0f, mask); Foot(l2, 2.0f, mask);
                    a1 = Leg(m, new Vector3(-1.2f, 3.4f, 0.2f), 1.4f, 0.22f, mask);
                    a2 = Leg(m, new Vector3(1.2f, 3.4f, 0.2f), 1.4f, 0.22f, mask);
                    foreach (var a in new[] { a1, a2 })
                    {
                        var kn = Part(a, new Vector3(0, -1.45f, 0.45f), new Vector3(0.06f, 0.2f, 0.9f), blade);
                        kn.localRotation = Quaternion.Euler(10, 0, 0);
                    }
                    break;
                }
                default:
                {
                    // Ла Вака Сатурно Сатурнита: голова коровы на планете Сатурн с кольцом, на человеческих ногах
                    var cow = Plastic(Color.white); var ringMat = Plastic(new Color(1f, 0.82f, 0.45f));
                    var planet = Plastic(new Color(0.95f, 0.72f, 0.4f)); var band = Plastic(new Color(0.85f, 0.55f, 0.3f));
                    var pink = Plastic(new Color(1f, 0.62f, 0.72f)); var horn = Plastic(new Color(0.96f, 0.9f, 0.7f));
                    Part(m, new Vector3(0, 3.1f, 0), new Vector3(2.6f, 2.6f, 2.6f), planet, false, PrimitiveType.Sphere);
                    Part(m, new Vector3(0, 3.5f, 0), new Vector3(2.5f, 0.12f, 2.5f), band, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 2.7f, 0), new Vector3(2.45f, 0.1f, 2.45f), band, false, PrimitiveType.Cylinder);
                    var rr = Part(m, new Vector3(0, 3.1f, 0), new Vector3(4.6f, 0.03f, 4.6f), ringMat, false, PrimitiveType.Cylinder);
                    rr.localRotation = Quaternion.Euler(14, 0, 10);
                    var rr2 = Part(m, new Vector3(0, 3.1f, 0), new Vector3(3.9f, 0.035f, 3.9f), band, false, PrimitiveType.Cylinder);
                    rr2.localRotation = Quaternion.Euler(14, 0, 10);
                    // голова коровы
                    Part(m, new Vector3(0, 4.85f, 0.3f), new Vector3(1.3f, 1.15f, 1.15f), cow);
                    Part(m, new Vector3(0.35f, 5.2f, 0.5f), new Vector3(0.55f, 0.45f, 0.8f), black);
                    Part(m, new Vector3(0, 4.5f, 0.95f), new Vector3(1.0f, 0.55f, 0.35f), pink);
                    Part(m, new Vector3(-0.2f, 4.5f, 1.13f), new Vector3(0.12f, 0.16f, 0.04f), black);
                    Part(m, new Vector3(0.2f, 4.5f, 1.13f), new Vector3(0.12f, 0.16f, 0.04f), black);
                    Eyes(m, new Vector3(0, 5.12f, 0.9f), 0.22f, white, black, 0f);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Part(m, new Vector3(sx * 0.5f, 5.6f, 0.2f), new Vector3(0.16f, 0.5f, 0.16f), horn).localRotation = Quaternion.Euler(0, 0, sx * -20f);
                        Part(m, new Vector3(sx * 0.82f, 5.05f, 0.2f), new Vector3(0.45f, 0.18f, 0.3f), cow).localRotation = Quaternion.Euler(0, 0, sx * -15f);
                    }
                    Fx.Sparkles(m, new Vector3(0, 3.1f, 0), new Color(1f, 0.9f, 0.6f), 2.2f, 3f);
                    l1 = Leg(m, new Vector3(-0.45f, 1.95f, 0), 1.95f, 0.36f, Plastic(new Color(0.95f, 0.8f, 0.65f)));
                    l2 = Leg(m, new Vector3(0.45f, 1.95f, 0), 1.95f, 0.36f, Plastic(new Color(0.95f, 0.8f, 0.65f)));
                    Foot(l1, 1.95f, black); Foot(l2, 1.95f, black);
                    break;
                }
            }
            Round = false; RoundFactor = 0.2f; RoundSteps = 1;
            g.legs = new[] { l1, l2 };
            g.arms = a1 != null ? new[] { a1, a2 } : new Transform[0];
            return g;
        }

        static Transform Leg(Transform m, Vector3 hip, float len, float w, Material mat)
        {
            var p = Pivot(m, "Limb", hip);
            Part(p, new Vector3(0, -len / 2f, 0), new Vector3(w, len, w), mat);
            return p;
        }

        static void Foot(Transform leg, float len, Material mat)
        {
            Part(leg, new Vector3(0, -len + 0.1f, 0.15f), new Vector3(0.5f, 0.22f, 0.7f), mat);
        }

        /// <summary>Большие мультяшные глаза: белок, зрачок и блик.</summary>
        static void Eyes(Transform m, Vector3 center, float size, Material white, Material black, float squint)
        {
            float dx = size * 1.15f;
            var shine = Plastic(Color.white);
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 c = center + new Vector3(sx * dx, 0, 0);
                Part(m, c, new Vector3(size * 1.6f, size * 1.6f * (1f - squint), 0.08f), white);
                Part(m, c + new Vector3(sx * -size * 0.12f, -size * 0.08f, 0.05f), new Vector3(size * 0.75f, size * 0.85f, 0.05f), black);
                Part(m, c + new Vector3(sx * -size * 0.12f + size * 0.18f, size * 0.18f, 0.08f), new Vector3(size * 0.25f, size * 0.25f, 0.03f), shine);
            }
        }

        /// <summary>Брови (угол &gt; 0 — удивлённые, &lt; 0 — злые).</summary>
        static void Brows(Transform m, Vector3 center, float size, Material mat, float angle)
        {
            float dx = size * 1.15f;
            foreach (float sx in new[] { -1f, 1f })
            {
                var b = Part(m, center + new Vector3(sx * dx, 0, 0), new Vector3(size * 1.5f, size * 0.3f, 0.08f), mat);
                b.localRotation = Quaternion.Euler(0, 0, sx * angle);
            }
        }

        // ================= ДЕКОР =================
        public static void Tree(Transform parent, Vector3 pos, float h)
        {
            var root = Pivot(parent, "Tree", pos);
            Part(root, new Vector3(0, h * 0.35f, 0), new Vector3(1.2f, h * 0.7f, 1.2f), Mats.Plastic(new Color(0.45f, 0.3f, 0.18f)), true);
            Round = true; RoundFactor = 0.25f;
            Part(root, new Vector3(0, h * 0.85f, 0), new Vector3(4.5f, h * 0.45f, 4.5f), Mats.Plastic(new Color(0.2f, 0.62f, 0.25f)));
            Part(root, new Vector3(0.3f, h * 1.15f, -0.2f), new Vector3(3f, h * 0.3f, 3f), Mats.Plastic(new Color(0.28f, 0.72f, 0.3f)));
            Round = false; RoundFactor = 0.2f;
        }

        /// <summary>3D-надпись с обводкой, всегда повёрнутая к камере.</summary>
        public static Label3D Label(Transform parent, string text, Vector3 localPos, float size, Color color)
        {
            return Label3D.Create(parent, text, localPos, size, color);
        }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = CameraRig.Cam;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    public class RainbowTint : MonoBehaviour
    {
        public Renderer target;
        MaterialPropertyBlock mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Update()
        {
            if (target == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f + transform.position.x * 0.01f, 1f), 0.6f, 1f);
            target.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            mpb.SetColor(BaseColorId, c);
            target.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Вращение вокруг локальной оси Z (пропеллеры).</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 600f;
        void Update() { transform.Rotate(0, 0, speed * Time.deltaTime, Space.Self); }
    }

    /// <summary>Покачивание влево-вправо (хвост акулы).</summary>
    public class Wiggle : MonoBehaviour
    {
        public float amount = 15f, freq = 6f;
        void Update() { transform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * freq) * amount, 0); }
    }

    /// <summary>Взмахи крыльев донатного яйца.</summary>
    public class WingFlap : MonoBehaviour
    {
        public Transform left, right;
        float seed;
        void Start() { seed = Random.value * 6f; }
        void Update()
        {
            float a = Mathf.Sin((Time.time + seed) * 6f) * 22f;
            if (left) left.localRotation = Quaternion.Euler(0, a * 0.6f, -a);
            if (right) right.localRotation = Quaternion.Euler(0, -a * 0.6f, a);
        }
    }

    public class DragonIdle : MonoBehaviour
    {
        public Transform lWing, rWing, head, model;
        float seed;
        void Start() { seed = Random.value * 10f; }
        void Update()
        {
            float t = Time.time + seed;
            float flap = Mathf.Sin(t * 4f) * 25f;
            if (lWing) lWing.localRotation = Quaternion.Euler(0, 0, flap + 10);
            if (rWing) rWing.localRotation = Quaternion.Euler(0, 0, -flap - 10);
            if (head) head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 8f, Mathf.Sin(t * 0.7f) * 20f, 0);
            if (model) model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 2f)) * 0.2f, 0);
        }
    }
}
