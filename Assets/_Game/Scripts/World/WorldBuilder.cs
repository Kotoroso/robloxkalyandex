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
        public static Vector3 SpawnPoint = new Vector3(0f, 1.2f, -24f);
        public const float BaseHalfWidth = 46f;
        public static readonly List<Vector3> BotHomes = new List<Vector3>();
        public static readonly List<Vector3> BotEntrances = new List<Vector3>();
        public static readonly List<List<Vector3>> BotPlots = new List<List<Vector3>>();

        static Transform staticRoot, dynamicRoot, signs;
        static System.Random rnd;

        public static void Build(GameManager gm)
        {
            SetupLighting();
            rnd = new System.Random(42);
            BotHomes.Clear();
            BotEntrances.Clear();
            BotPlots.Clear();
            SkipRects.Clear();

            staticRoot = new GameObject("World_Static").transform;
            dynamicRoot = new GameObject("World_Dynamic").transform;
            signs = new GameObject("Signs").transform;

            float half = GameConfig.RunwayWidth / 2f;
            float endZ = GameConfig.ZoneCenterZ(GameConfig.Tiers.Length - 1) + GameConfig.ZoneLength / 2f + 6f;

            // ===== ЗЕМЛЯ =====
            float groundLen = endZ + 200f;
            var grass = new Color(0.27f, 0.8f, 0.2f);
            Blocky.Part(staticRoot, new Vector3(0, -0.5f, groundLen / 2f - 100f), new Vector3(320f, 1f, groundLen), Mats.Keycaps(GrassKeys), true);

            BuildBase(gm, half);
            BuildRunway(gm, half, endZ);
            BuildScenery(half, endZ);

            BuildKeyboards(endZ);

            foreach (var r in staticRoot.GetComponentsInChildren<Renderer>()) r.receiveShadows = true;
            StaticBatchingUtility.Combine(staticRoot.gameObject);
        }

        // ============================ БАЗА ============================
        // Палитра спавна: мягкие, приятные цвета (без "вырвиглаза")
        static readonly Color BaseFloor = new Color(0.86f, 0.9f, 0.97f);
        static readonly Color BaseTrim = new Color(0.45f, 0.62f, 0.95f);
        static readonly Color GrassKeys = new Color(0.46f, 0.82f, 0.42f);

        /// <summary>
        /// Спавн по образцу роблокс-режимов: просторная площадка с клавиатурным полом, высокие стены-каньон,
        /// в стенах вырезаны 3 "стойла" фейк-игроков, у тебя 6 грядок, 7 беговых дорожек, продавец,
        /// магазин трейлов, таблички управления и площадка сдачи яиц у ворот.
        /// </summary>
        static readonly string[] BotNicks = { "Kirill2012", "Sasha_PRO", "xX_Tima_Xx", "Egorka777", "LizaPlays", "Dima_Roblox", "KOTIK_228", "Nastya2011", "Maks_YT", "MegaVanya" };

        /// <summary>
        /// Спавн по образцу "Укради Яйцо": большая площадка, в скалах вырезаны базы —
        /// твоя ("ВАША БАЗА", 6 грядок) и 3 базы фейк-игроков с никами над входом.
        /// Слева спереди — зона тренажёров (беговые дорожки), продавец, магазин трейлов,
        /// таблички управления, площадка сдачи яиц у ворот.
        /// </summary>
        static void BuildBase(GameManager gm, float half)
        {
            float bx = BaseHalfWidth, minZ = GameConfig.BaseMinZ, maxZ = GameConfig.BaseMaxZ, midZ = (minZ + maxZ) / 2f, depth = maxZ - minZ;
            Blocky.Part(staticRoot, new Vector3(0, 0.05f, midZ), new Vector3(bx * 2 + 2f, 0.1f, depth + 2f), Mats.Keycaps(BaseTrim), true);
            Blocky.Part(staticRoot, new Vector3(0, 0.08f, midZ), new Vector3(bx * 2 - 2f, 0.1f, depth - 2f), Mats.Keycaps(BaseFloor), true);

            // ниши: справа — твоя база, слева и сзади (2) — базы ботов
            float pZ0 = -46f, pZ1 = -14f, pDepth = 26f;       // твоя (справа)
            float lZ0 = -42f, lZ1 = -22f, lDepth = 18f;       // бот слева
            float b1X0 = -30f, b1X1 = -12f, b2X0 = 12f, b2X1 = 30f, bDepth = 16f; // боты сзади
            WallZ(-bx - 1f, minZ, lZ0); WallZ(-bx - 1f, lZ1, maxZ);
            WallZ(bx + 1f, minZ, pZ0); WallZ(bx + 1f, pZ1, maxZ);
            WallX(minZ - 1f, -bx - 1f, b1X0); WallX(minZ - 1f, b1X1, b2X0); WallX(minZ - 1f, b2X1, bx + 1f);
            float gap = half;
            WallX(maxZ, -bx - 1f, -gap); WallX(maxZ, gap, bx + 1f);

            var nickRnd = new System.Random(System.Environment.TickCount);
            var nicks = new List<string>(BotNicks);
            System.Func<string> Nick = () => { int i = nickRnd.Next(nicks.Count); var n = nicks[i]; nicks.RemoveAt(i); return n; };

            // твоя база
            var pc = new Vector3(bx + pDepth / 2f, 0, (pZ0 + pZ1) / 2f);
            Alcove(pc, new Vector2(pDepth, pZ1 - pZ0), 1, new Color(0.55f, 0.8f, 1f), Loc.Ru ? "ВАША БАЗА" : "YOUR BASE", new Color(0.5f, 1f, 0.6f), false);
            int idx = 0;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 2; col++)
                    gm.Plots.Add(BasePlot.Build(dynamicRoot, idx++, new Vector3(bx + 8.5f + col * 9.5f, DeckTop - 0.1f, pZ0 + 7f + row * 9f)));
            // базы ботов
            Alcove(new Vector3(-bx - lDepth / 2f, 0, (lZ0 + lZ1) / 2f), new Vector2(lDepth, lZ1 - lZ0), 0, new Color(1f, 0.62f, 0.6f), (Loc.Ru ? "База " : "Base of ") + Nick(), Color.white, true);
            Alcove(new Vector3((b1X0 + b1X1) / 2f, 0, minZ - bDepth / 2f), new Vector2(b1X1 - b1X0, bDepth), 2, new Color(1f, 0.85f, 0.5f), (Loc.Ru ? "База " : "Base of ") + Nick(), Color.white, true);
            Alcove(new Vector3((b2X0 + b2X1) / 2f, 0, minZ - bDepth / 2f), new Vector2(b2X1 - b2X0, bDepth), 2, new Color(0.75f, 0.65f, 1f), (Loc.Ru ? "База " : "Base of ") + Nick(), Color.white, true);

            Blocky.Label(signs, Loc.T("base"), new Vector3(0, 15f, maxZ), 2.6f, new Color(1f, 0.93f, 0.55f)).text = Loc.Ru ? "К ЯЙЦАМ" : "TO THE EGGS";
            Arch(new Vector3(0, 0, maxZ), gap - 0.5f, BaseTrim);
            // площадка сдачи яиц у ворот: светящаяся зелёная плита, яйцо, стрелка и столб света
            Vector3 dropC = new Vector3(2f, 0, maxZ - 5f);
            DropZone.Build(dynamicRoot, dropC, new Vector2(8f, 7f), DeckTop - 0.1f);
            Blocky.Label(signs, Loc.Ru ? "СДАЙ ЯЙЦО СЮДА" : "BRING EGGS HERE", new Vector3(dropC.x, 3.4f, dropC.z), 1.4f, new Color(0.7f, 1f, 0.6f));
            SkipRects.Add(new Rect(dropC.x - 4.5f, dropC.z - 4f, 9f, 8f));

            // зона тренажёров слева спереди
            WoodDeck(new Rect(-46.5f, -12.5f, 41.5f, 13f)); // до x=-5: не заходит на площадку сдачи
            for (int i = 0; i < GameConfig.Treadmills.Length; i++)
                Treadmill.Build(dynamicRoot, i, new Vector3(-43f + i * 5.8f, DeckTop, -6f), 0f);
            Blocky.Label(signs, Loc.Ru ? "ТРЕНАЖЁРЫ" : "TREADMILLS", new Vector3(-25.6f, 8f, -1f), 1.6f, new Color(0.6f, 0.95f, 1f));

            // продавец и магазин трейлов
            WoodDeck(new Rect(-45.5f, -21.5f, 9f, 9f));
            WoodDeck(new Rect(36.5f, -9.5f, 9f, 9f));
            SellerNPC.Build(dynamicRoot, new Vector3(-41f, DeckTop, -17f), 90f);
            SellerNPC.Build(dynamicRoot, new Vector3(41f, DeckTop, -5f), -90f, true);

            // спавн по центру
            Blocky.Round = true; Blocky.RoundFactor = 0.4f;
            Blocky.Part(staticRoot, new Vector3(SpawnPoint.x, 0.3f, SpawnPoint.z), new Vector3(7f, 0.4f, 7f), Mats.Plastic(new Color(0.96f, 0.96f, 1f)));
            Blocky.Part(staticRoot, new Vector3(SpawnPoint.x, 0.45f, SpawnPoint.z), new Vector3(5f, 0.25f, 5f), Mats.Plastic(BaseTrim));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            SkipRects.Add(new Rect(SpawnPoint.x - 4.5f, SpawnPoint.z - 4.5f, 9f, 9f));

            SpawnBoards();
            BotPlayer.SpawnAll(dynamicRoot, BotHomes, BotEntrances, BotPlots);

            for (int i = 0; i < 3; i++) Lamp(new Vector3(-bx + 2f, 0, -44f + i * 8f));
            FlowerBed(new Vector3(-12f, 0.6f, 5f), 8f, 1.6f);
            FlowerBed(new Vector3(12f, 0.6f, 5f), 8f, 1.6f);
        }

        /// <summary>Верх клавиш пола — на этой высоте стоят настилы и объекты.</summary>
        public const float DeckTop = 0.58f;

        /// <summary>Деревянный настил из досок вровень с клавишами (под дорожками, продавцами, площадкой сдачи).</summary>
        static void WoodDeck(Rect r)
        {
            Color[] tones = { new Color(0.66f, 0.45f, 0.26f), new Color(0.6f, 0.4f, 0.22f), new Color(0.7f, 0.5f, 0.3f) };
            float h = 0.35f;
            int n = Mathf.Max(1, Mathf.RoundToInt(r.width / 1.2f));
            float pw = r.width / n;
            for (int i = 0; i < n; i++)
                Blocky.Part(staticRoot, new Vector3(r.x + pw * (i + 0.5f), DeckTop - h / 2f, r.y + r.height / 2f), new Vector3(pw - 0.08f, h, r.height), Mats.Plastic(tones[i % 3]));
            // тёмная рамка по краю
            var frame = Mats.Plastic(new Color(0.42f, 0.27f, 0.14f));
            Blocky.Part(staticRoot, new Vector3(r.center.x, DeckTop - h / 2f + 0.02f, r.y), new Vector3(r.width + 0.3f, h + 0.04f, 0.3f), frame);
            Blocky.Part(staticRoot, new Vector3(r.center.x, DeckTop - h / 2f + 0.02f, r.yMax), new Vector3(r.width + 0.3f, h + 0.04f, 0.3f), frame);
            Blocky.Part(staticRoot, new Vector3(r.x, DeckTop - h / 2f + 0.02f, r.center.y), new Vector3(0.3f, h + 0.04f, r.height), frame);
            Blocky.Part(staticRoot, new Vector3(r.xMax, DeckTop - h / 2f + 0.02f, r.center.y), new Vector3(0.3f, h + 0.04f, r.height), frame);
            var col = new GameObject("DeckCollider");
            col.transform.SetParent(dynamicRoot, false);
            col.transform.position = new Vector3(r.center.x, (KeyFloorY + DeckTop) / 2f, r.center.y);
            col.AddComponent<BoxCollider>().size = new Vector3(r.width, DeckTop - KeyFloorY, r.height);
            SkipRects.Add(new Rect(r.x - 0.5f, r.y - 0.5f, r.width + 1f, r.height + 1f));
        }

        /// <summary>Деревянная вывеска на двух столбах: доска из досок, рамка, крупный текст на лицевой стороне.</summary>
        static void WoodSign(Vector3 basePos, Vector3 facing, string text, Color textColor, float width)
        {
            var root = new GameObject("WoodSign").transform;
            root.SetParent(signs, false);
            root.position = basePos;
            root.rotation = Quaternion.LookRotation(facing);
            var wood = Mats.Plastic(new Color(0.62f, 0.4f, 0.22f));
            var woodDark = Mats.Plastic(new Color(0.45f, 0.28f, 0.15f));
            float h = 3.6f;
            // столбы
            Blocky.Part(root, new Vector3(-width * 0.38f, 1.2f, -0.1f), new Vector3(0.45f, 2.4f, 0.45f), woodDark);
            Blocky.Part(root, new Vector3(width * 0.38f, 1.2f, -0.1f), new Vector3(0.45f, 2.4f, 0.45f), woodDark);
            // доски (три горизонтальные планки) и рамка
            for (int i = 0; i < 3; i++)
                Blocky.Part(root, new Vector3(0, 2.2f + h * (i + 0.5f) / 3f, 0), new Vector3(width, h / 3f - 0.06f, 0.35f), i % 2 == 0 ? wood : Mats.Plastic(new Color(0.66f, 0.43f, 0.24f)));
            Blocky.Part(root, new Vector3(0, 2.2f + h + 0.12f, 0), new Vector3(width + 0.5f, 0.3f, 0.5f), woodDark);
            Blocky.Part(root, new Vector3(0, 2.2f - 0.12f, 0), new Vector3(width + 0.5f, 0.3f, 0.5f), woodDark);
            Blocky.Part(root, new Vector3(-width / 2f - 0.1f, 2.2f + h / 2f, 0), new Vector3(0.3f, h + 0.5f, 0.5f), woodDark);
            Blocky.Part(root, new Vector3(width / 2f + 0.1f, 2.2f + h / 2f, 0), new Vector3(0.3f, h + 0.5f, 0.5f), woodDark);
            // текст на лицевой стороне (не поворачивается к камере)
            var l = Blocky.Label(root, text, new Vector3(0, 2.2f + h / 2f, 0.22f), Mathf.Clamp(width / Mathf.Max(6, text.Length) * 3.1f, 2.2f, 4.2f), textColor);
            l.billboard = false;
            l.maxDistance = 160f;
            l.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        }

        /// <summary>Области, где не нужны клавиши пола (спавн, площадка сдачи и т.п.).</summary>
        public static readonly List<Rect> SkipRects = new List<Rect>();

        /// <summary>Ниша в скале (база): пол, внешние стены, ник над входом; для ботов — грядки с драконами.</summary>
        static void Alcove(Vector3 center, Vector2 size, int side, Color accent, string title, Color titleColor, bool bot)
        {
            float hx = size.x / 2f, hz = size.y / 2f;
            Blocky.Part(staticRoot, center + new Vector3(0, 0.08f, 0), new Vector3(size.x, 0.1f, size.y), Mats.Keycaps(Color.Lerp(BaseFloor, accent, 0.4f)), true);
            Vector3 entrance;
            if (side < 2)
            {
                float outer = center.x + (side == 0 ? -hx - 1f : hx + 1f);
                WallZ(outer, center.z - hz - 1f, center.z + hz + 1f);
                WallX(center.z - hz - 1f, center.x - hx - 1f, center.x + hx + 1f);
                WallX(center.z + hz + 1f, center.x - hx - 1f, center.x + hx + 1f);
                entrance = new Vector3(center.x + (side == 0 ? hx : -hx), 0, center.z);
            }
            else
            {
                WallX(center.z - hz - 1f, center.x - hx - 1f, center.x + hx + 1f);
                WallZ(center.x - hx - 1f, center.z - hz - 1f, center.z + hz + 1f);
                WallZ(center.x + hx + 1f, center.z - hz - 1f, center.z + hz + 1f);
                entrance = new Vector3(center.x, 0, center.z + hz);
            }
            // деревянная вывеска с ником на скале над входом (смотрит внутрь спавна)
            Vector3 dirIn = side == 0 ? Vector3.right : side == 1 ? Vector3.left : Vector3.forward;
            // каменная перемычка над входом — вывеска стоит на ней, а не висит в воздухе
            float open = side < 2 ? size.y : size.x;
            Vector3 lintelSize = side < 2 ? new Vector3(2f, 3f, open + 2f) : new Vector3(open + 2f, 3f, 2f);
            Blocky.Part(staticRoot, entrance + new Vector3(0, WallHeight - 1.5f, 0) - dirIn * 1f, lintelSize, Mats.Studs(WallBrown), false);
            Vector3 lintelTop = side < 2 ? new Vector3(2.4f, 1.4f, open + 2.2f) : new Vector3(open + 2.2f, 1.4f, 2.4f);
            Blocky.Part(staticRoot, entrance + new Vector3(0, WallHeight + 0.7f, 0) - dirIn * 1f, lintelTop, Mats.Studs(WallGreen), false);
            WoodSign(entrance + new Vector3(0, WallHeight + 1.4f, 0) - dirIn * 1f, dirIn, title, titleColor, Mathf.Min(16f, (side < 2 ? size.y : size.x) * 0.85f));
            if (!bot) return;
            var rnd2 = new System.Random((int)(center.x * 13 + center.z * 7));
            var plotList = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                float a = (i % 3 - 1), b = (i / 3 - 0.5f);
                Vector3 p = side < 2 ? center + new Vector3(b * hx * 0.9f, 0, a * hz * 0.62f) : center + new Vector3(a * hx * 0.62f, 0, b * hz * 0.9f);
                plotList.Add(p);
                Blocky.Part(staticRoot, p + new Vector3(0, 0.45f, 0), new Vector3(4.6f, 0.7f, 4.6f), Mats.Studs(new Color(0.35f, 0.72f, 0.32f)), true);
                Blocky.Part(staticRoot, p + new Vector3(0, 0.84f, 0), new Vector3(3.6f, 0.1f, 3.6f), Mats.Studs(new Color(0.48f, 0.33f, 0.2f)), false);
                if (rnd2.Next(3) == 0)
                {
                    var egg = Blocky.BuildEgg(dynamicRoot, (Tier)rnd2.Next(0, 5), 1.3f);
                    egg.transform.position = p + new Vector3(0, 0.9f, 0);
                }
                else
                {
                    var list = new List<DragonDef>();
                    foreach (var dd in GameConfig.Dragons) if (!dd.exclusive && (int)dd.tier <= 6) list.Add(dd);
                    var dr = Blocky.BuildDragon(dynamicRoot, list[rnd2.Next(list.Count)]);
                    dr.transform.position = p + new Vector3(0, 0.9f, 0);
                    dr.transform.localScale = Vector3.one * 0.7f;
                    dr.transform.rotation = Quaternion.Euler(0, rnd2.Next(360), 0);
                }
            }
            BotHomes.Add(center + new Vector3(0, 0.6f, 0));
            BotEntrances.Add(entrance + dirIn * 3f + new Vector3(0, 0.6f, 0));
            BotPlots.Add(plotList);
        }

        // ===================== Пол из механических клавиш =====================
        const float KeyFloorY = 0.13f;

        static Color[] Tints(Color c)
        {
            return new[] { Color.Lerp(c, Color.white, 0.55f), Color.Lerp(c, Color.white, 0.3f), Color.Lerp(c, Color.white, 0.8f), c * 0.9f + new Color(0, 0, 0, 0.1f) };
        }

        /// <summary>Есть ли в клетке объект (грядка, дорожка, стена, постамент...) — там клавиш не ставим.</summary>
        static bool Occupied(Vector3 c)
        {
            foreach (var r in SkipRects) if (r.Contains(new Vector2(c.x, c.z))) return true;
            var hits = Physics.OverlapBox(new Vector3(c.x, KeyFloorY + 0.6f, c.z), new Vector3(0.85f, 0.35f, 0.85f));
            foreach (var h in hits)
            {
                string n = h.gameObject.name;
                if (n.StartsWith("Gate_") || n.StartsWith("InvisibleWall")) continue;
                return true;
            }
            return false;
        }

        /// <summary>ASMR-клавиатура по всей карте: база, ниши-базы, дорога, зоны тиров (у каждой своя палитра).</summary>
        static void BuildKeyboards(float endZ)
        {
            Physics.SyncTransforms();
            var root = new GameObject("KeyboardFloor").transform; // не в staticRoot: меши клавиш уже объединены
            float bx = BaseHalfWidth, minZ = GameConfig.BaseMinZ, maxZ = GameConfig.BaseMaxZ;
            System.Func<Vector3, bool> skip = Occupied;
            KeycapFloor.Build(root, new Rect(-bx, minZ, bx * 2f, maxZ - minZ), KeyFloorY, KeycapFloor.PastelPalette, 1, skip);
            KeycapFloor.Build(root, new Rect(bx, -46f, 26f, 32f), KeyFloorY, Tints(new Color(0.45f, 0.7f, 1f)), 2, skip);
            KeycapFloor.Build(root, new Rect(-bx - 18f, -42f, 18f, 20f), KeyFloorY, Tints(new Color(1f, 0.5f, 0.5f)), 3, skip);
            KeycapFloor.Build(root, new Rect(-30f, minZ - 16f, 18f, 16f), KeyFloorY, Tints(new Color(1f, 0.75f, 0.3f)), 4, skip);
            KeycapFloor.Build(root, new Rect(12f, minZ - 16f, 18f, 16f), KeyFloorY, Tints(new Color(0.65f, 0.5f, 1f)), 5, skip);
            float half = GameConfig.RunwayWidth / 2f;
            // дорога между зонами — кофейная палитра, зоны — в цветах своего тира
            System.Func<Vector3, bool> runwaySkip = c =>
            {
                for (int t = 0; t < GameConfig.Tiers.Length; t++)
                {
                    float cz = GameConfig.ZoneCenterZ(t);
                    if (c.z >= cz - GameConfig.ZoneLength / 2f && c.z < cz + GameConfig.ZoneLength / 2f) return true;
                }
                return Occupied(c);
            };
            KeycapFloor.Build(root, new Rect(-half, maxZ, half * 2f, endZ - maxZ), KeyFloorY, KeycapFloor.CoffeePalette, 6, runwaySkip);
            for (int t = 0; t < GameConfig.Tiers.Length; t++)
            {
                float cz = GameConfig.ZoneCenterZ(t);
                KeycapFloor.Build(root, new Rect(-half, cz - GameConfig.ZoneLength / 2f, half * 2f, GameConfig.ZoneLength), KeyFloorY,
                    Tints(GameConfig.Tiers[t].color), 10 + t, skip);
            }
        }

        static void WallZ(float x, float z0, float z1) { CastleWall(new Vector3(x, 0, (z0 + z1) / 2f), Mathf.Abs(z1 - z0), false, null); }
        static void WallX(float z, float x0, float x1) { CastleWall(new Vector3((x0 + x1) / 2f, 0, z), Mathf.Abs(x1 - x0), true, null); }

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
            var b1 = Board(new Vector3(-9f, 0.1f, -15f), mobile ? (ru ? "Джойстик слева" : "Left joystick") : (ru ? "Передвижение" : "Movement"));
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
            var b2 = Board(new Vector3(9f, 0.1f, -15f), mobile ? (ru ? "Свайп справа — камера" : "Swipe right — camera") : (ru ? "Поворот камеры" : "Camera"));
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
                Mats.Keycaps(new Color(0.93f, 0.88f, 0.78f)), true);
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
                Color floor = Color.Lerp(info.color, Color.white, 0.45f);
                Blocky.Part(staticRoot, new Vector3(0, 0.07f, cz), new Vector3(GameConfig.RunwayWidth, 0.1f, GameConfig.ZoneLength), Mats.Keycaps(floor), true);
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
            // не удаляем свет из сцены (на него ссылаются настройки освещения) — берём существующий направленный
            Light sun = null;
            foreach (var l in lights)
            {
                if (l == null) continue;
                if (sun == null && l.type == LightType.Directional) sun = l;
                else l.enabled = false;
            }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
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
