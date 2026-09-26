using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Строит карту в коде: база-замок (грядки, беговые дорожки, ASMR-клавиатура, продавец),
    /// длинная дорога через 7 тематических зон с воротами по скорости, трава, деревья, холмы, облака и небо.
    /// Вся статика объединяется в батчи, чтобы тянули средние телефоны.
    /// </summary>
    public static class WorldBuilder
    {
        public static Vector3 SpawnPoint = new Vector3(-3f, 1f, 2f);
        public const float BaseHalfWidth = 40f;

        static Transform staticRoot, dynamicRoot, signs;
        static System.Random rnd;

        public static void Build(GameManager gm)
        {
            SetupLighting();
            rnd = new System.Random(42);

            staticRoot = new GameObject("World_Static").transform;
            dynamicRoot = new GameObject("World_Dynamic").transform;
            signs = new GameObject("Signs").transform;

            float half = GameConfig.RunwayWidth / 2f;
            float endZ = GameConfig.ZoneCenterZ(GameConfig.Tiers.Length - 1) + GameConfig.ZoneLength / 2f + 6f;

            // ===== ЗЕМЛЯ =====
            float groundLen = endZ + 200f;
            var grass = new Color(0.27f, 0.8f, 0.2f);
            Blocky.Part(staticRoot, new Vector3(0, -0.5f, groundLen / 2f - 100f), new Vector3(320f, 1f, groundLen), Mats.Studs(grass), true);

            BuildBase(gm, half);
            BuildRunway(gm, half, endZ);
            BuildScenery(half, endZ);

            foreach (var r in staticRoot.GetComponentsInChildren<Renderer>()) r.receiveShadows = true;
            StaticBatchingUtility.Combine(staticRoot.gameObject);
        }

        // ============================ БАЗА ============================
        static void BuildBase(GameManager gm, float half)
        {
            float bx = BaseHalfWidth, minZ = GameConfig.BaseMinZ, maxZ = GameConfig.BaseMaxZ, midZ = (minZ + maxZ) / 2f, depth = maxZ - minZ;
            // пол базы со светлой плиткой и синей каймой
            Blocky.Part(staticRoot, new Vector3(0, 0.05f, midZ), new Vector3(bx * 2 + 2f, 0.1f, depth + 2f), Mats.Studs(new Color(0.3f, 0.55f, 0.95f)), true);
            Blocky.Part(staticRoot, new Vector3(0, 0.08f, midZ), new Vector3(bx * 2 - 1f, 0.1f, depth - 1f), Mats.Studs(new Color(0.88f, 0.9f, 0.94f)), true);

            // стены-замок с зубцами
            var wall = Mats.Studs(new Color(0.82f, 0.8f, 0.76f));
            CastleWall(new Vector3(0, 0, minZ - 0.5f), bx * 2 + 1f, true, wall);
            CastleWall(new Vector3(-bx - 0.5f, 0, midZ), depth + 1f, false, wall);
            CastleWall(new Vector3(bx + 0.5f, 0, midZ), depth + 1f, false, wall);
            float gap = half, seg = bx - gap;
            CastleWall(new Vector3(-(gap + seg / 2f), 0, maxZ), seg, true, wall);
            CastleWall(new Vector3(gap + seg / 2f, 0, maxZ), seg, true, wall);

            // башни по углам и у ворот
            Tower(new Vector3(-bx - 0.5f, 0, minZ - 0.5f), new Color(0.3f, 0.55f, 1f));
            Tower(new Vector3(bx + 0.5f, 0, minZ - 0.5f), new Color(0.3f, 0.55f, 1f));
            Tower(new Vector3(-bx - 0.5f, 0, maxZ), new Color(1f, 0.45f, 0.3f));
            Tower(new Vector3(bx + 0.5f, 0, maxZ), new Color(1f, 0.45f, 0.3f));
            Tower(new Vector3(-gap - 1.5f, 0, maxZ), new Color(1f, 0.85f, 0.2f));
            Tower(new Vector3(gap + 1.5f, 0, maxZ), new Color(1f, 0.85f, 0.2f));

            Blocky.Label(signs, Loc.T("base"), new Vector3(0, 11.5f, maxZ), 3f, new Color(1f, 0.92f, 0.3f));
            // площадка сдачи яиц у ворот
            Blocky.Part(staticRoot, new Vector3(0, 0.12f, maxZ - 4f), new Vector3(12f, 0.06f, 6f), Mats.Glow(new Color(0.3f, 1f, 0.45f)));
            Blocky.Part(staticRoot, new Vector3(0, 0.13f, maxZ - 4f), new Vector3(10.8f, 0.06f, 4.8f), Mats.Studs(new Color(0.25f, 0.7f, 0.3f)));
            Blocky.Label(signs, Loc.Ru ? "СДАЙ ЯЙЦО СЮДА" : "BRING EGGS HERE", new Vector3(0, 3.2f, maxZ - 4f), 1.4f, new Color(0.5f, 1f, 0.6f));
            Arch(new Vector3(0, 0, maxZ), gap - 0.5f, new Color(0.3f, 0.55f, 1f));

            // Грядки: 3 колонки x 4 ряда справа
            int idx = 0;
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 3; col++)
                    gm.Plots.Add(BasePlot.Build(dynamicRoot, idx++, new Vector3(14f + col * 8.6f, 0, -29f + row * 8.6f)));

            // Беговые дорожки слева у задней стены
            for (int i = 0; i < GameConfig.Treadmills.Length; i++)
                Treadmill.Build(dynamicRoot, i, new Vector3(-36f + i * 6f, 0.1f, -26f), 0f);

            // ASMR-клавиатура и продавец
            KeyboardFloor.Build(dynamicRoot, new Vector3(-37f, 0.1f, -16.5f), 10, 5, 2.2f);
            SellerNPC.Build(dynamicRoot, new Vector3(-31f, 0.1f, 4.2f), 180f);
            SellerNPC.Build(dynamicRoot, new Vector3(5f, 0.1f, -9f), -50f, true);

            // спавн-площадка
            Blocky.Round = true; Blocky.RoundFactor = 0.4f;
            Blocky.Part(staticRoot, new Vector3(SpawnPoint.x, 0.2f, SpawnPoint.z), new Vector3(5f, 0.25f, 5f), Mats.Plastic(new Color(0.95f, 0.95f, 0.98f)));
            Blocky.Part(staticRoot, new Vector3(SpawnPoint.x, 0.28f, SpawnPoint.z), new Vector3(3.6f, 0.2f, 3.6f), Mats.Plastic(new Color(0.3f, 0.55f, 1f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;

            SpawnBoards();
            BotPlayer.SpawnAll(dynamicRoot, 7);

            // фонари и клумбы
            for (int i = 0; i < 4; i++)
            {
                Lamp(new Vector3(-bx + 2f, 0, minZ + 6f + i * 10f));
                Lamp(new Vector3(bx - 2f, 0, minZ + 6f + i * 10f));
            }
            FlowerBed(new Vector3(-8f, 0.1f, 5.5f), 6f, 1.6f);
            FlowerBed(new Vector3(-16f, 0.1f, 5.5f), 6f, 1.6f);
            FlowerBed(new Vector3(12f, 0.1f, 5.5f), 10f, 1.6f);
        }

        static readonly Color WallBrown = new Color(0.74f, 0.52f, 0.33f);
        static readonly Color WallGreen = new Color(0.25f, 0.78f, 0.2f);
        public const float WallHeight = 12f;

        /// <summary>Высокая коричневая стена со студами и зелёным верхом — "каньон" как в роблокс-режимах.</summary>
        static void CastleWall(Vector3 center, float length, bool alongX, Material m)
        {
            float h = WallHeight;
            Vector3 size = alongX ? new Vector3(length, h, 2f) : new Vector3(2f, h, length);
            Blocky.Part(staticRoot, center + new Vector3(0, h / 2f, 0), size, Mats.Studs(WallBrown), true);
            Vector3 top = alongX ? new Vector3(length + 0.2f, 1.4f, 2.4f) : new Vector3(2.4f, 1.4f, length + 0.2f);
            Blocky.Part(staticRoot, center + new Vector3(0, h + 0.7f, 0), top, Mats.Studs(WallGreen), false);
        }

        static void Tower(Vector3 pos, Color flag) { }

        // ===== Таблички с управлением у спавна (как в роблокс-режимах) =====
        static void SpawnBoards()
        {
            bool mobile = InputState.Mobile;
            bool ru = Loc.Ru;
            // "Передвижение": клавиши WASD или джойстик
            var b1 = Board(new Vector3(-13f, 0.1f, 6.5f), mobile ? (ru ? "Джойстик слева" : "Left joystick") : (ru ? "Передвижение" : "Movement"));
            if (!mobile)
            {
                Key3D(b1, "W", new Vector3(0, 3.1f, -0.6f));
                Key3D(b1, "A", new Vector3(-1.5f, 1.6f, -0.6f));
                Key3D(b1, "S", new Vector3(0, 1.6f, -0.6f));
                Key3D(b1, "D", new Vector3(1.5f, 1.6f, -0.6f));
            }
            else
            {
                Blocky.Part(b1, new Vector3(0, 2.3f, -0.4f), new Vector3(3f, 3f, 0.2f), Mats.Plastic(new Color(0.8f, 0.85f, 0.95f)), false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
                Blocky.Part(b1, new Vector3(0.6f, 2.5f, -0.6f), new Vector3(1.3f, 1.3f, 0.2f), Mats.Plastic(new Color(1f, 0.8f, 0.2f)), false, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90, 0, 0);
            }
            // "Поворот камеры": мышь с зажатой правой кнопкой или свайп
            var b2 = Board(new Vector3(7f, 0.1f, 6.5f), mobile ? (ru ? "Свайп справа — камера" : "Swipe right — camera") : (ru ? "Поворот камеры" : "Camera"));
            Blocky.Round = true; Blocky.RoundFactor = 0.45f; Blocky.RoundSteps = 2;
            Blocky.Part(b2, new Vector3(0, 2.2f, -0.6f), new Vector3(1.8f, 2.8f, 0.9f), Mats.Plastic(Color.white));
            Blocky.RoundFactor = 0.3f;
            if (!mobile)
            {
                Blocky.Part(b2, new Vector3(0.45f, 3.05f, -1.08f), new Vector3(0.8f, 1.0f, 0.1f), Mats.Plastic(new Color(0.95f, 0.2f, 0.2f)));
                Blocky.Part(b2, new Vector3(0, 3.05f, -1.1f), new Vector3(0.18f, 0.6f, 0.12f), Mats.Plastic(new Color(0.2f, 0.2f, 0.25f)));
            }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f; Blocky.RoundSteps = 1;
            var hint = Blocky.Label(b2, mobile ? "" : (ru ? "зажми ПКМ" : "hold RMB"), new Vector3(0, 0.6f, -0.3f), 0.55f, new Color(1f, 0.85f, 0.3f));
            hint.billboard = false;
        }

        static Transform Board(Vector3 pos, string title)
        {
            var root = Blocky.Pivot(dynamicRoot, "Board", pos);
            var post = Mats.Plastic(new Color(0.3f, 0.22f, 0.15f));
            Blocky.Part(root, new Vector3(-2.6f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.3f), post, true);
            Blocky.Part(root, new Vector3(2.6f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.3f), post, true);
            var l = Blocky.Label(root, title, new Vector3(0, 5.2f, -0.2f), 1.5f, Color.white);
            l.billboard = false;
            return root;
        }

        static void Key3D(Transform parent, string letter, Vector3 pos)
        {
            Blocky.Round = true; Blocky.RoundFactor = 0.25f; Blocky.RoundSteps = 2;
            Blocky.Part(parent, pos, new Vector3(1.3f, 1.3f, 0.6f), Mats.Plastic(Color.white));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f; Blocky.RoundSteps = 1;
            var l = Blocky.Label(parent, letter, pos + new Vector3(0, 0, -0.32f), 1.3f, new Color(0.1f, 0.1f, 0.15f));
            l.billboard = false;
        }

        static void Lamp(Vector3 pos)
        {
            Blocky.Part(staticRoot, pos + new Vector3(0, 2f, 0), new Vector3(0.3f, 4f, 0.3f), Mats.Plastic(new Color(0.2f, 0.2f, 0.25f)));
            Blocky.Round = true; Blocky.RoundFactor = 0.3f;
            Blocky.Part(staticRoot, pos + new Vector3(0, 4.3f, 0), new Vector3(0.8f, 0.8f, 0.8f), Mats.Glow(new Color(1f, 0.93f, 0.6f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
        }

        static void FlowerBed(Vector3 pos, float w, float d)
        {
            Blocky.Part(staticRoot, pos + new Vector3(0, 0.2f, 0), new Vector3(w, 0.4f, d), Mats.Studs(new Color(0.45f, 0.3f, 0.18f)));
            Color[] petals = { new Color(1f, 0.4f, 0.5f), new Color(1f, 0.9f, 0.3f), new Color(0.7f, 0.5f, 1f), Color.white };
            int n = Mathf.RoundToInt(w * 1.3f);
            for (int i = 0; i < n; i++)
            {
                float x = -w / 2f + 0.4f + (float)rnd.NextDouble() * (w - 0.8f);
                float z = -d / 2f + 0.3f + (float)rnd.NextDouble() * (d - 0.6f);
                Blocky.Part(staticRoot, pos + new Vector3(x, 0.6f, z), new Vector3(0.08f, 0.5f, 0.08f), Mats.Plastic(new Color(0.2f, 0.6f, 0.2f)));
                Blocky.Part(staticRoot, pos + new Vector3(x, 0.9f, z), new Vector3(0.3f, 0.2f, 0.3f), Mats.Plastic(petals[i % petals.Length]));
            }
        }

        static void Arch(Vector3 pos, float half, Color c)
        {
            var m = Mats.Plastic(c);
            var glow = Mats.Glow(Color.Lerp(c, Color.white, 0.35f));
            Blocky.Round = true; Blocky.RoundFactor = 0.25f;
            Blocky.Part(staticRoot, pos + new Vector3(-half, 4f, 0), new Vector3(2.2f, 8f, 2.2f), m);
            Blocky.Part(staticRoot, pos + new Vector3(half, 4f, 0), new Vector3(2.2f, 8f, 2.2f), m);
            Blocky.Part(staticRoot, pos + new Vector3(0, 8.6f, 0), new Vector3(half * 2f + 2.8f, 1.6f, 2.4f), m);
            Blocky.Part(staticRoot, pos + new Vector3(-half, 8.4f, 0), new Vector3(2.8f, 0.8f, 2.8f), m);
            Blocky.Part(staticRoot, pos + new Vector3(half, 8.4f, 0), new Vector3(2.8f, 0.8f, 2.8f), m);
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            Blocky.Part(staticRoot, pos + new Vector3(0, 7.75f, -1.21f), new Vector3(half * 2f + 1f, 0.15f, 0.05f), glow);
            Blocky.Part(staticRoot, pos + new Vector3(0, 7.75f, 1.21f), new Vector3(half * 2f + 1f, 0.15f, 0.05f), glow);
        }

        // ============================ ДОРОГА И ЗОНЫ ============================
        static void BuildRunway(GameManager gm, float half, float endZ)
        {
            float startZ = GameConfig.BaseMaxZ, runLen = endZ - startZ;
            Blocky.Part(staticRoot, new Vector3(0, 0.04f, startZ + runLen / 2f), new Vector3(GameConfig.RunwayWidth, 0.1f, runLen),
                Mats.Studs(new Color(0.72f, 0.7f, 0.66f)), true);
            // невидимые стены + деревянный забор
            AddWall(new Vector3(-half - 0.5f, 3f, startZ + runLen / 2f), new Vector3(1f, 6f, runLen));
            AddWall(new Vector3(half + 0.5f, 3f, startZ + runLen / 2f), new Vector3(1f, 6f, runLen));
            AddWall(new Vector3(0, 3f, endZ + 0.5f), new Vector3(GameConfig.RunwayWidth + 2f, 6f, 1f));
            CastleWall(new Vector3(-half - 1f, 0, startZ + runLen / 2f), runLen, false, null);
            CastleWall(new Vector3(half + 1f, 0, startZ + runLen / 2f), runLen, false, null);
            Blocky.Part(staticRoot, new Vector3(0, 1.5f, endZ + 0.5f), new Vector3(GameConfig.RunwayWidth + 2f, 3f, 1f), Mats.Studs(new Color(0.63f, 0.42f, 0.25f)), false);

            for (int t = 0; t < GameConfig.Tiers.Length; t++)
            {
                var info = GameConfig.Tiers[t];
                float cz = GameConfig.ZoneCenterZ(t);
                Color floor = Color.Lerp(info.color, new Color(0.55f, 0.55f, 0.55f), 0.5f);
                Blocky.Part(staticRoot, new Vector3(0, 0.07f, cz), new Vector3(GameConfig.RunwayWidth, 0.1f, GameConfig.ZoneLength), Mats.Studs(floor), true);
                Blocky.Part(staticRoot, new Vector3(0, 0.09f, cz - GameConfig.ZoneLength / 2f + 0.5f), new Vector3(GameConfig.RunwayWidth, 0.1f, 1f), Mats.Glow(info.color), false);

                float archZ = cz - GameConfig.ZoneLength / 2f;
                Arch(new Vector3(0, 0, archZ), half - 1.2f, info.color);
                Blocky.Label(signs, Loc.F("zone", Loc.TierName(info.tier).ToUpper()), new Vector3(0, 11f, archZ), 2.2f, Color.Lerp(info.color, Color.white, 0.3f));
                Blocky.Label(signs, Loc.F("min_speed", Mathf.RoundToInt(info.guardSpeed)), new Vector3(0, 9.9f, archZ - 1.4f), 1.1f, Color.white);
                if (t > 0) ZoneGate.Build(dynamicRoot, info, archZ, GameConfig.RunwayWidth);

                var ped = EggPedestal.Build(dynamicRoot, info.tier, new Vector3(0, 0.1f, cz + 6f));
                if (!gm.Pedestals.Contains(ped)) gm.Pedestals.Add(ped);
                for (int g = 0; g < info.guardCount; g++)
                {
                    float a = g * Mathf.PI * 2f / info.guardCount;
                    BrainrotGuard.Build(dynamicRoot, ped, info, ped.transform.position + new Vector3(Mathf.Cos(a) * 7f, 0, Mathf.Sin(a) * 7f));
                }
                ZoneDecor(t, cz, half, info.color);
            }
        }

        static void AddWall(Vector3 center, Vector3 size)
        {
            var go = new GameObject("InvisibleWall");
            go.transform.SetParent(dynamicRoot, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        static void Fence(float x, float z0, float z1)
        {
            var wood = Mats.Plastic(new Color(0.66f, 0.45f, 0.26f));
            var woodDark = Mats.Plastic(new Color(0.55f, 0.36f, 0.2f));
            for (float z = z0 + 1f; z < z1; z += 4f)
                Blocky.Part(staticRoot, new Vector3(x, 1.1f, z), new Vector3(0.45f, 2.2f, 0.45f), woodDark);
            float len = z1 - z0;
            Blocky.Part(staticRoot, new Vector3(x, 0.8f, z0 + len / 2f), new Vector3(0.2f, 0.3f, len), wood);
            Blocky.Part(staticRoot, new Vector3(x, 1.6f, z0 + len / 2f), new Vector3(0.2f, 0.3f, len), wood);
        }

        static float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }

        /// <summary>Тематический декор зоны по краям дорожки (не мешает бегать по центру).</summary>
        static void ZoneDecor(int tier, float cz, float half, Color c)
        {
            float zl = GameConfig.ZoneLength;
            var spots = new List<Vector3>();
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                spots.Add(new Vector3(side * R(half - 4.5f, half - 1.8f), 0.1f, cz + R(-zl / 2f + 3f, zl / 2f - 2f)));
            }
            Blocky.Round = true;
            int k2 = 0;
            foreach (var p in spots)
            {
                k2++;
                switch (tier)
                {
                    case 0: // камни и кусты
                        Blocky.RoundFactor = 0.35f;
                        Blocky.Part(staticRoot, p + new Vector3(0, 0.6f, 0), new Vector3(R(1.2f, 2.2f), R(0.9f, 1.6f), R(1.2f, 2f)), Mats.Plastic(new Color(0.55f, 0.55f, 0.58f)));
                        Blocky.Part(staticRoot, p + new Vector3(0, 0.6f, 1.6f), new Vector3(1.4f, 1.2f, 1.4f), Mats.Plastic(new Color(0.25f, 0.6f, 0.25f)));
                        break;
                    case 1: // грибы
                    {
                        Blocky.RoundFactor = 0.3f;
                        float h = R(1.2f, 2.4f);
                        Blocky.Part(staticRoot, p + new Vector3(0, h / 2f, 0), new Vector3(0.6f, h, 0.6f), Mats.Plastic(new Color(0.96f, 0.93f, 0.85f)));
                        Blocky.RoundFactor = 0.45f;
                        Blocky.Part(staticRoot, p + new Vector3(0, h + 0.3f, 0), new Vector3(2f, 0.8f, 2f), Mats.Plastic(k2 % 2 == 0 ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.75f, 0.35f)));
                        break;
                    }
                    case 2: // ледяные кристаллы
                    case 3: // фиолетовые кристаллы
                    {
                        Blocky.RoundFactor = 0.15f;
                        var cm = Mats.Plastic(Color.Lerp(c, Color.white, tier == 2 ? 0.45f : 0.2f));
                        for (int k = 0; k < 3; k++)
                        {
                            var cr = Blocky.Part(staticRoot, p + new Vector3(R(-0.6f, 0.6f), 1.2f, R(-0.6f, 0.6f)), new Vector3(0.7f, R(1.8f, 3.4f), 0.7f), cm);
                            cr.localRotation = Quaternion.Euler(R(-20, 20), R(0, 90), R(-20, 20));
                        }
                        Blocky.Part(staticRoot, p + new Vector3(0, 0.15f, 0), new Vector3(2f, 0.1f, 2f), Mats.Glow(Color.Lerp(c, Color.white, 0.3f)));
                        break;
                    }
                    case 4: // золотые колонны и монеты
                        Blocky.RoundFactor = 0.2f;
                        Blocky.Part(staticRoot, p + new Vector3(0, 2f, 0), new Vector3(1.2f, 4f, 1.2f), Mats.Plastic(new Color(1f, 0.82f, 0.25f)));
                        Blocky.Part(staticRoot, p + new Vector3(0, 4.2f, 0), new Vector3(1.8f, 0.4f, 1.8f), Mats.Plastic(new Color(1f, 0.9f, 0.5f)));
                        for (int k = 0; k < 4; k++)
                            Blocky.Part(staticRoot, p + new Vector3(0, 0.2f + k * 0.18f, 1.4f), new Vector3(0.9f, 0.08f, 0.9f), Mats.Plastic(new Color(1f, 0.78f, 0.15f)), false, PrimitiveType.Cylinder);
                        break;
                    case 5: // лава и тёмные камни
                        Blocky.RoundFactor = 0.3f;
                        Blocky.Part(staticRoot, p + new Vector3(0, 0.12f, 0), new Vector3(3f, 0.06f, 2.4f), Mats.Glow(new Color(1f, 0.45f, 0.1f)));
                        Blocky.Part(staticRoot, p + new Vector3(0, 0.8f, 1.8f), new Vector3(1.4f, 1.6f, 1.4f), Mats.Plastic(new Color(0.18f, 0.12f, 0.12f)));
                        break;
                    default: // парящие светящиеся кубы
                    {
                        Blocky.RoundFactor = 0.2f;
                        Blocky.Part(staticRoot, p + new Vector3(0, 1.5f, 0), new Vector3(0.8f, 3f, 0.8f), Mats.Plastic(new Color(0.15f, 0.2f, 0.3f)));
                        var cube = Blocky.Part(staticRoot, p + new Vector3(0, 4f, 0), new Vector3(1.2f, 1.2f, 1.2f), Mats.Glow(c));
                        cube.localRotation = Quaternion.Euler(45, 45, 0);
                        break;
                    }
                }
            }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
        }

        // ============================ ПЕЙЗАЖ ============================
        static void BuildScenery(float half, float endZ)
        {
            for (int i = 0; i < 60; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (half + 6f + (float)rnd.NextDouble() * 60f);
                float z = -60f + (float)rnd.NextDouble() * (endZ + 90f);
                if (z < GameConfig.BaseMaxZ + 6 && Mathf.Abs(x) < BaseHalfWidth + 8f) continue;
                Blocky.Tree(staticRoot, new Vector3(x, 0, z), 4f + (float)rnd.NextDouble() * 4f);
            }
            Blocky.Round = true; Blocky.RoundFactor = 0.4f;
            for (int i = 0; i < 50; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (half + 3f + (float)rnd.NextDouble() * 70f);
                float z = -60f + (float)rnd.NextDouble() * (endZ + 90f);
                if (z < GameConfig.BaseMaxZ + 6 && Mathf.Abs(x) < BaseHalfWidth + 8f) continue;
                bool rock = i % 3 == 0;
                float s = R(1f, 2.4f);
                Blocky.Part(staticRoot, new Vector3(x, s * 0.35f, z), new Vector3(s * 1.3f, s * 0.8f, s),
                    Mats.Plastic(rock ? new Color(0.6f, 0.6f, 0.63f) : new Color(0.25f, 0.58f, 0.25f)));
            }
            // холмы на горизонте
            Blocky.RoundFactor = 0.45f;
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * R(90f, 115f);
                float z = -80f + i * (endZ + 160f) / 16f;
                float h = R(14f, 30f);
                Blocky.Part(staticRoot, new Vector3(x, h * 0.4f, z), new Vector3(R(30f, 50f), h, R(30f, 50f)), Mats.Plastic(new Color(0.3f, 0.62f, 0.3f)));
            }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;

            BuildMountains(endZ);

            // облака (отдельно от статики — медленно плывут)
            var clouds = new GameObject("Clouds").AddComponent<CloudDrift>();
            var white = Mats.Plastic(Color.white);
            Blocky.Round = true; Blocky.RoundFactor = 0.45f;
            for (int i = 0; i < 14; i++)
            {
                var c = Blocky.Pivot(clouds.transform, "Cloud", new Vector3(R(-150f, 150f), R(45f, 70f), R(-80f, endZ + 60f)));
                int parts = rnd.Next(3, 6);
                for (int k = 0; k < parts; k++)
                    Blocky.Part(c, new Vector3(k * 4f - parts * 2f, R(-1f, 1.5f), R(-2f, 2f)), new Vector3(R(6f, 9f), R(3f, 5f), R(5f, 8f)), white);
            }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            Blocky.NoShadows(clouds.gameObject);
        }

        /// <summary>
        /// Кубические горы по краям карты (как в Майнкрафте): камень, земля, трава, снежные шапки.
        /// Закрывают пустоту на горизонте. Высота — из шума Перлина, ступеньками по 4 юнита.
        /// </summary>
        static void BuildMountains(float endZ)
        {
            const float B = 8f;                // размер блока-колонны
            float minX = -160f, maxX = 160f, minZ = -100f, maxZ = endZ + 100f;
            var stone = Mats.Studs(new Color(0.52f, 0.52f, 0.55f));
            var dirt = Mats.Studs(new Color(0.5f, 0.35f, 0.22f));
            var grassTop = Mats.Studs(new Color(0.35f, 0.68f, 0.3f));
            var snow = Mats.Studs(new Color(0.95f, 0.97f, 1f));
            int rings = 4;
            for (float x = minX; x <= maxX; x += B)
                for (float z = minZ; z <= maxZ; z += B)
                {
                    // расстояние до края в блоках
                    int d = Mathf.RoundToInt(Mathf.Min(Mathf.Min(x - minX, maxX - x), Mathf.Min(z - minZ, maxZ - z)) / B);
                    if (d >= rings) continue;
                    float n = Mathf.PerlinNoise(x * 0.021f + 13.7f, z * 0.021f + 5.3f);
                    float h = (rings - d) * 7f + n * 26f + 6f;
                    h = Mathf.Max(4f, Mathf.Round(h / 4f) * 4f);
                    float cx = x, cz = z;
                    // камень снизу
                    Blocky.Part(staticRoot, new Vector3(cx, (h - 4f) / 2f, cz), new Vector3(B, h - 4f, B), h > 34f ? stone : dirt);
                    // верхний блок: снег на высоких, трава на остальных
                    var top = h > 38f ? snow : grassTop;
                    Blocky.Part(staticRoot, new Vector3(cx, h - 2f, cz), new Vector3(B, 4f, B), top);
                    // выступы-ступеньки для "майнкрафтовости"
                    if (n > 0.55f && d > 0)
                        Blocky.Part(staticRoot, new Vector3(cx, h + 2f, cz), new Vector3(B * 0.5f, 4f, B * 0.5f), h + 4f > 38f ? snow : grassTop);
                }
        }

        static void SetupLighting()
        {
#if UNITY_2022_2_OR_NEWER
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            var lights = Object.FindObjectsOfType<Light>();
#endif
            foreach (var l in lights) Object.Destroy(l.gameObject);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0);
            sun.intensity = 1.0f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            bool mobile = InputState.Mobile;
            sun.shadows = mobile ? LightShadows.None : LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            RenderSettings.sun = sun;

            // процедурное небо (если шейдер есть в сборке), иначе — сплошной цвет
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.45f, 0.62f, 1f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.45f, 0.6f, 0.45f));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.8f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.25f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.05f);
                RenderSettings.skybox = sky;
            }
            else RenderSettings.skybox = null;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.36f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.97f);
            RenderSettings.fogStartDistance = 110f;
            RenderSettings.fogEndDistance = 380f;

            QualitySettings.shadowDistance = 50f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.antiAliasing = mobile ? 0 : 4;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }

    /// <summary>Облака медленно плывут и возвращаются.</summary>
    public class CloudDrift : MonoBehaviour
    {
        void Update()
        {
            foreach (Transform c in transform)
            {
                var p = c.position;
                p.x += Time.deltaTime * 1.5f;
                if (p.x > 170f) p.x = -170f;
                c.position = p;
            }
        }
    }
}
