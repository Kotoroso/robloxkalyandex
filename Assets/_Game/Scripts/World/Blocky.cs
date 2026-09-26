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

        public static Transform Part(Transform parent, Vector3 localPos, Vector3 size, Material mat,
            bool collider = false, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = new GameObject(type.ToString());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(type);
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
            // Торс
            Part(m, new Vector3(0, 3f, 0), new Vector3(2f, 2f, 1f), shirtM);
            // Голова + лицо
            a.head = Pivot(m, "Head", new Vector3(0, 4.6f, 0));
            Part(a.head, Vector3.zero, new Vector3(1.25f, 1.2f, 1.25f), skinM);
            var face = Part(a.head, new Vector3(0, 0, 0.63f), new Vector3(1.1f, 1.05f, 0.02f), Mats.Face(skin));
            face.localRotation = Quaternion.Euler(0, 180, 0);
            // Руки (пивот в плече)
            a.lArm = Pivot(m, "LArm", new Vector3(-1.5f, 3.9f, 0));
            Part(a.lArm, new Vector3(0, -0.9f, 0), new Vector3(1f, 2f, 1f), skinM);
            a.rArm = Pivot(m, "RArm", new Vector3(1.5f, 3.9f, 0));
            Part(a.rArm, new Vector3(0, -0.9f, 0), new Vector3(1f, 2f, 1f), skinM);
            // Ноги (пивот в бедре)
            a.lLeg = Pivot(m, "LLeg", new Vector3(-0.5f, 2f, 0));
            Part(a.lLeg, new Vector3(0, -1f, 0), new Vector3(1f, 2f, 1f), pantsM);
            a.rLeg = Pivot(m, "RLeg", new Vector3(0.5f, 2f, 0));
            Part(a.rLeg, new Vector3(0, -1f, 0), new Vector3(1f, 2f, 1f), pantsM);

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
            if (tier == Tier.Secret || tier == Tier.Mythic) root.AddComponent<RainbowTint>().target = body.GetComponent<Renderer>();
            return root;
        }

        // ================= ДРАКОН =================
        public static GameObject BuildDragon(Transform parent, DragonDef d)
        {
            float s = 0.8f + (int)d.tier * 0.18f;
            var root = new GameObject("Dragon_" + d.nameEn);
            root.transform.SetParent(parent, false);
            var m = Pivot(root.transform, "Model", Vector3.zero);
            m.localScale = Vector3.one * s;
            var body = Plastic(d.body); var belly = Plastic(d.belly); var wing = Plastic(d.wing);
            var dark = Plastic(new Color(0.08f, 0.08f, 0.08f)); var white = Plastic(Color.white);
            var horn = Plastic(Color.Lerp(d.belly, Color.white, 0.4f));

            Part(m, new Vector3(0, 1.3f, 0), new Vector3(1.6f, 1.3f, 2.4f), body);
            Part(m, new Vector3(0, 1.05f, 0.25f), new Vector3(1.3f, 0.9f, 1.9f), belly);
            // шея и голова
            var neck = Part(m, new Vector3(0, 2.1f, 1.1f), new Vector3(0.8f, 1.2f, 0.8f), body);
            neck.localRotation = Quaternion.Euler(25, 0, 0);
            var head = Pivot(m, "Head", new Vector3(0, 2.8f, 1.6f));
            Part(head, Vector3.zero, new Vector3(1.1f, 0.9f, 1.1f), body);
            Part(head, new Vector3(0, -0.15f, 0.75f), new Vector3(0.8f, 0.5f, 0.7f), body);
            Part(head, new Vector3(0, -0.35f, 0.7f), new Vector3(0.7f, 0.12f, 0.6f), belly);
            Part(head, new Vector3(-0.3f, 0.15f, 0.52f), new Vector3(0.25f, 0.3f, 0.1f), white);
            Part(head, new Vector3(0.3f, 0.15f, 0.52f), new Vector3(0.25f, 0.3f, 0.1f), white);
            Part(head, new Vector3(-0.3f, 0.12f, 0.58f), new Vector3(0.13f, 0.18f, 0.05f), dark);
            Part(head, new Vector3(0.3f, 0.12f, 0.58f), new Vector3(0.13f, 0.18f, 0.05f), dark);
            var h1 = Part(head, new Vector3(-0.3f, 0.65f, -0.25f), new Vector3(0.18f, 0.6f, 0.18f), horn);
            h1.localRotation = Quaternion.Euler(-30, 0, 0);
            var h2 = Part(head, new Vector3(0.3f, 0.65f, -0.25f), new Vector3(0.18f, 0.6f, 0.18f), horn);
            h2.localRotation = Quaternion.Euler(-30, 0, 0);
            // крылья
            var lw = Pivot(m, "LWing", new Vector3(-0.7f, 1.9f, 0.1f));
            Part(lw, new Vector3(-1.1f, 0, 0), new Vector3(2.2f, 0.1f, 1.5f), wing);
            Part(lw, new Vector3(-1.9f, 0, -0.6f), new Vector3(0.8f, 0.1f, 0.8f), wing);
            var rw = Pivot(m, "RWing", new Vector3(0.7f, 1.9f, 0.1f));
            Part(rw, new Vector3(1.1f, 0, 0), new Vector3(2.2f, 0.1f, 1.5f), wing);
            Part(rw, new Vector3(1.9f, 0, -0.6f), new Vector3(0.8f, 0.1f, 0.8f), wing);
            // хвост
            var t1 = Part(m, new Vector3(0, 1.15f, -1.6f), new Vector3(0.8f, 0.7f, 1.0f), body);
            var t2 = Part(m, new Vector3(0, 1.0f, -2.4f), new Vector3(0.55f, 0.5f, 0.9f), body);
            var t3 = Part(m, new Vector3(0, 0.9f, -3.1f), new Vector3(0.35f, 0.35f, 0.7f), body);
            var tip = Part(m, new Vector3(0, 1.0f, -3.55f), new Vector3(0.5f, 0.5f, 0.1f), wing);
            tip.localRotation = Quaternion.Euler(0, 0, 45);
            t1.name = "Tail1"; t2.name = "Tail2"; t3.name = "Tail3";
            // лапы
            Part(m, new Vector3(-0.55f, 0.35f, 0.8f), new Vector3(0.45f, 0.7f, 0.45f), body);
            Part(m, new Vector3(0.55f, 0.35f, 0.8f), new Vector3(0.45f, 0.7f, 0.45f), body);
            Part(m, new Vector3(-0.55f, 0.35f, -0.7f), new Vector3(0.45f, 0.7f, 0.45f), body);
            Part(m, new Vector3(0.55f, 0.35f, -0.7f), new Vector3(0.45f, 0.7f, 0.45f), body);
            // гребень на спине для высоких тиров
            for (int i = 0; i < (int)d.tier; i++)
                Part(m, new Vector3(0, 2.0f, 0.8f - i * 0.45f), new Vector3(0.12f, 0.4f, 0.3f), wing);

            var idle = root.AddComponent<DragonIdle>();
            idle.lWing = lw; idle.rWing = rw; idle.head = head; idle.model = m;
            return root;
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
            Part(root, new Vector3(0, h * 0.85f, 0), new Vector3(4.5f, h * 0.45f, 4.5f), Mats.Studs(new Color(0.2f, 0.62f, 0.25f), 4, 4), true);
            Part(root, new Vector3(0, h * 1.15f, 0), new Vector3(3f, h * 0.3f, 3f), Mats.Studs(new Color(0.25f, 0.7f, 0.3f), 3, 3), true);
        }

        /// <summary>3D-надпись, всегда повёрнутая к камере.</summary>
        public static TextMesh Label(Transform parent, string text, Vector3 localPos, float size, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Mats.UIFont;
            tm.fontSize = 64;
            tm.characterSize = size * 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            if (tm.font != null) mr.sharedMaterial = tm.font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<Billboard>();
            return tm;
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
