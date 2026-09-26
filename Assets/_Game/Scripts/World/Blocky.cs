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
        public static GameObject BuildEgg(Transform parent, Tier tier, float size)
        {
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
            Round = true;
            var white = Plastic(Color.white); var black = Plastic(new Color(0.07f, 0.07f, 0.07f));
            var red = Plastic(new Color(0.85f, 0.15f, 0.15f));
            Transform l1, l2, a1 = null, a2 = null;

            switch (kind)
            {
                case BrainrotKind.TungSahur:
                {
                    // деревянное бревно с битой
                    var wood = Plastic(new Color(0.72f, 0.5f, 0.3f));
                    Part(m, new Vector3(0, 3.2f, 0), new Vector3(1.6f, 3.4f, 1.6f), wood, false, PrimitiveType.Cylinder).localScale = new Vector3(1.6f, 1.7f, 1.6f);
                    Eyes(m, new Vector3(0, 4.1f, 0.78f), 0.35f, white, black);
                    Part(m, new Vector3(0, 3.4f, 0.8f), new Vector3(0.7f, 0.15f, 0.05f), black);
                    l1 = Leg(m, new Vector3(-0.4f, 1.5f, 0), 1.5f, 0.35f, wood);
                    l2 = Leg(m, new Vector3(0.4f, 1.5f, 0), 1.5f, 0.35f, wood);
                    a1 = Leg(m, new Vector3(-0.95f, 3.6f, 0), 1.4f, 0.3f, wood);
                    a2 = Leg(m, new Vector3(0.95f, 3.6f, 0), 1.4f, 0.3f, wood);
                    var bat = Part(a2, new Vector3(0, -1.4f, 0.8f), new Vector3(0.3f, 0.3f, 2.2f), Plastic(new Color(0.45f, 0.28f, 0.15f)));
                    bat.localRotation = Quaternion.Euler(-20, 0, 0);
                    break;
                }
                case BrainrotKind.Lirili:
                {
                    // слон-кактус в сандалиях
                    var cactus = Plastic(new Color(0.3f, 0.65f, 0.25f)); var grey = Plastic(new Color(0.6f, 0.6f, 0.65f));
                    Part(m, new Vector3(0, 2.6f, 0), new Vector3(1.6f, 2.4f, 1.4f), cactus);
                    for (int i = 0; i < 6; i++)
                        Part(m, new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 1.8f + i * 0.3f, 0), new Vector3(0.3f, 0.06f, 0.06f), white);
                    Part(m, new Vector3(0, 4.3f, 0.1f), new Vector3(1.5f, 1.2f, 1.3f), grey);
                    Part(m, new Vector3(-0.95f, 4.3f, 0), new Vector3(0.3f, 1.1f, 1f), grey);
                    Part(m, new Vector3(0.95f, 4.3f, 0), new Vector3(0.3f, 1.1f, 1f), grey);
                    var trunk = Part(m, new Vector3(0, 3.7f, 0.95f), new Vector3(0.35f, 1.2f, 0.35f), grey);
                    trunk.localRotation = Quaternion.Euler(20, 0, 0);
                    Eyes(m, new Vector3(0, 4.55f, 0.66f), 0.25f, white, black);
                    l1 = Leg(m, new Vector3(-0.45f, 1.4f, 0), 1.3f, 0.45f, grey);
                    l2 = Leg(m, new Vector3(0.45f, 1.4f, 0), 1.3f, 0.45f, grey);
                    Part(l1, new Vector3(0, -1.35f, 0.1f), new Vector3(0.6f, 0.12f, 0.8f), Plastic(new Color(0.55f, 0.35f, 0.2f)));
                    Part(l2, new Vector3(0, -1.35f, 0.1f), new Vector3(0.6f, 0.12f, 0.8f), Plastic(new Color(0.55f, 0.35f, 0.2f)));
                    break;
                }
                case BrainrotKind.Bombardiro:
                {
                    // крокодил-бомбардировщик
                    var croc = Plastic(new Color(0.25f, 0.5f, 0.2f)); var metal = Plastic(new Color(0.55f, 0.6f, 0.62f));
                    Part(m, new Vector3(0, 2.2f, 0), new Vector3(1.4f, 1.2f, 3.2f), croc);
                    Part(m, new Vector3(0, 2.2f, 2.2f), new Vector3(1.1f, 0.6f, 1.6f), croc);
                    for (int i = 0; i < 4; i++) Part(m, new Vector3(0, 1.85f, 1.6f + i * 0.4f), new Vector3(1.12f, 0.1f, 0.12f), white);
                    Eyes(m, new Vector3(0, 2.75f, 1.3f), 0.3f, white, black);
                    Part(m, new Vector3(0, 2.6f, 0), new Vector3(5.5f, 0.15f, 1.2f), metal);
                    Part(m, new Vector3(0, 2.4f, -1.9f), new Vector3(2f, 0.12f, 0.7f), metal);
                    Part(m, new Vector3(0, 1.4f, 0.3f), new Vector3(0.4f, 0.4f, 1.2f), Plastic(new Color(0.2f, 0.2f, 0.2f)), false, PrimitiveType.Capsule);
                    l1 = Leg(m, new Vector3(-0.5f, 1.6f, 0.6f), 1.5f, 0.35f, croc);
                    l2 = Leg(m, new Vector3(0.5f, 1.6f, 0.6f), 1.5f, 0.35f, croc);
                    break;
                }
                case BrainrotKind.Tralalero:
                {
                    // акула в кроссовках
                    var shark = Plastic(new Color(0.35f, 0.55f, 0.85f)); var shoe = Plastic(new Color(0.15f, 0.35f, 0.95f));
                    Part(m, new Vector3(0, 2.4f, 0), new Vector3(1.3f, 1.3f, 3.4f), shark);
                    Part(m, new Vector3(0, 2.1f, 0.4f), new Vector3(1.1f, 0.8f, 2.6f), white);
                    Part(m, new Vector3(0, 3.4f, -0.2f), new Vector3(0.2f, 1.1f, 0.9f), shark);
                    var tail = Part(m, new Vector3(0, 2.6f, -2f), new Vector3(0.2f, 1.4f, 0.7f), shark);
                    tail.localRotation = Quaternion.Euler(-25, 0, 0);
                    Eyes(m, new Vector3(0, 2.75f, 1.5f), 0.28f, white, black);
                    Part(m, new Vector3(0, 2.05f, 1.72f), new Vector3(0.8f, 0.12f, 0.05f), red);
                    l1 = Leg(m, new Vector3(-0.45f, 1.6f, 0.3f), 1.4f, 0.28f, shark);
                    l2 = Leg(m, new Vector3(0.45f, 1.6f, 0.3f), 1.4f, 0.28f, shark);
                    Part(l1, new Vector3(0, -1.4f, 0.2f), new Vector3(0.5f, 0.35f, 0.8f), shoe);
                    Part(l2, new Vector3(0, -1.4f, 0.2f), new Vector3(0.5f, 0.35f, 0.8f), shoe);
                    break;
                }
                case BrainrotKind.Patapim:
                {
                    // лесной носатый патапим
                    var bark = Plastic(new Color(0.5f, 0.33f, 0.18f)); var leaf = Plastic(new Color(0.2f, 0.6f, 0.2f));
                    var skin = Plastic(new Color(0.85f, 0.65f, 0.5f));
                    Part(m, new Vector3(0, 3.0f, 0), new Vector3(1.4f, 2.2f, 1.2f), bark);
                    Part(m, new Vector3(0, 4.7f, 0), new Vector3(2.4f, 1.6f, 2.2f), leaf);
                    Part(m, new Vector3(0, 4.3f, 0.7f), new Vector3(1.1f, 1.0f, 0.4f), skin);
                    Part(m, new Vector3(0, 4.1f, 1.1f), new Vector3(0.45f, 0.7f, 0.7f), skin);
                    Eyes(m, new Vector3(0, 4.6f, 0.92f), 0.26f, white, black);
                    l1 = Leg(m, new Vector3(-0.4f, 2f, 0), 2f, 0.3f, bark);
                    l2 = Leg(m, new Vector3(0.4f, 2f, 0), 2f, 0.3f, bark);
                    a1 = Leg(m, new Vector3(-0.9f, 3.8f, 0), 1.8f, 0.25f, bark);
                    a2 = Leg(m, new Vector3(0.9f, 3.8f, 0), 1.8f, 0.25f, bark);
                    break;
                }
                case BrainrotKind.Cappuccino:
                {
                    // чашка-ассасин
                    var cup = Plastic(new Color(0.95f, 0.93f, 0.9f)); var coffee = Plastic(new Color(0.45f, 0.28f, 0.15f));
                    var mask = Plastic(new Color(0.12f, 0.12f, 0.15f));
                    Part(m, new Vector3(0, 3f, 0), new Vector3(2f, 1.3f, 2f), cup, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(0, 4.32f, 0), new Vector3(1.85f, 0.05f, 1.85f), coffee, false, PrimitiveType.Cylinder);
                    Part(m, new Vector3(1.15f, 3f, 0), new Vector3(0.3f, 1.1f, 0.8f), cup);
                    Part(m, new Vector3(0, 3.6f, 0.9f), new Vector3(1.6f, 0.5f, 0.3f), mask);
                    Eyes(m, new Vector3(0, 3.62f, 1.06f), 0.22f, white, black);
                    l1 = Leg(m, new Vector3(-0.45f, 1.7f, 0), 1.7f, 0.25f, mask);
                    l2 = Leg(m, new Vector3(0.45f, 1.7f, 0), 1.7f, 0.25f, mask);
                    a1 = Leg(m, new Vector3(-1.15f, 3.4f, 0), 1.3f, 0.22f, mask);
                    a2 = Leg(m, new Vector3(-1.15f, 3.4f, 0), 1.3f, 0.22f, mask);
                    a2.localPosition = new Vector3(1.4f, 3.4f, 0.4f);
                    var knife = Part(a2, new Vector3(0, -1.5f, 0.5f), new Vector3(0.08f, 0.25f, 1.1f), Plastic(new Color(0.8f, 0.85f, 0.9f)));
                    knife.localRotation = Quaternion.Euler(10, 0, 0);
                    break;
                }
                default:
                {
                    // Ла Вака Сатурно — корова с кольцом Сатурна
                    var cow = Plastic(Color.white); var ring = Plastic(new Color(1f, 0.8f, 0.3f)); var pink = Plastic(new Color(1f, 0.6f, 0.7f));
                    Part(m, new Vector3(0, 2.6f, 0), new Vector3(1.8f, 1.5f, 2.8f), cow);
                    Part(m, new Vector3(0.5f, 3.1f, 0.4f), new Vector3(0.9f, 0.6f, 0.9f), black);
                    Part(m, new Vector3(-0.6f, 2.4f, -0.6f), new Vector3(0.7f, 0.8f, 0.9f), black);
                    Part(m, new Vector3(0, 3.3f, 1.8f), new Vector3(1.2f, 1.1f, 1.1f), cow);
                    Part(m, new Vector3(0, 3.0f, 2.35f), new Vector3(0.9f, 0.5f, 0.1f), pink);
                    Eyes(m, new Vector3(0, 3.55f, 2.36f), 0.25f, white, black);
                    Part(m, new Vector3(-0.5f, 4f, 1.7f), new Vector3(0.15f, 0.5f, 0.15f), ring);
                    Part(m, new Vector3(0.5f, 4f, 1.7f), new Vector3(0.15f, 0.5f, 0.15f), ring);
                    var rr = Part(m, new Vector3(0, 2.6f, 0), new Vector3(5f, 0.04f, 5f), ring, false, PrimitiveType.Cylinder);
                    rr.localRotation = Quaternion.Euler(12, 0, 8);
                    l1 = Leg(m, new Vector3(-0.5f, 1.9f, 0.6f), 1.9f, 0.4f, cow);
                    l2 = Leg(m, new Vector3(0.5f, 1.9f, 0.6f), 1.9f, 0.4f, cow);
                    a1 = Leg(m, new Vector3(-0.5f, 1.9f, -0.8f), 1.9f, 0.4f, cow);
                    a2 = Leg(m, new Vector3(0.5f, 1.9f, -0.8f), 1.9f, 0.4f, cow);
                    break;
                }
            }
            Round = false;
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

        static void Eyes(Transform m, Vector3 center, float size, Material white, Material black)
        {
            float dx = size * 1.1f;
            Part(m, center + new Vector3(-dx, 0, 0), new Vector3(size * 1.4f, size * 1.4f, 0.06f), white);
            Part(m, center + new Vector3(dx, 0, 0), new Vector3(size * 1.4f, size * 1.4f, 0.06f), white);
            Part(m, center + new Vector3(-dx, 0, 0.04f), new Vector3(size * 0.6f, size * 0.7f, 0.04f), black);
            Part(m, center + new Vector3(dx, 0, 0.04f), new Vector3(size * 0.6f, size * 0.7f, 0.04f), black);
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
