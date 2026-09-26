using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Строит карту в коде: база с грядками и беговыми дорожками, длинная дорога с зонами тиров,
    /// в каждой зоне яйцо дракона и брейнроты-охранники. Дальше зона — круче яйцо и быстрее охрана.
    /// </summary>
    public static class WorldBuilder
    {
        public static Vector3 SpawnPoint = new Vector3(-9f, 1f, -8f);

        public static void Build(GameManager gm)
        {
            SetupLighting();

            var staticRoot = new GameObject("World_Static").transform;
            var dynamicRoot = new GameObject("World_Dynamic").transform;

            float half = GameConfig.RunwayWidth / 2f;
            float endZ = GameConfig.ZoneCenterZ(GameConfig.Tiers.Length - 1) + GameConfig.ZoneLength / 2f + 6f;

            // Бейсплейт (серый со студами — как в Роблокс Студио)
            float groundLen = endZ + 120f;
            Blocky.Part(staticRoot, new Vector3(0, -0.5f, groundLen / 2f - 70f), new Vector3(240f, 1f, groundLen),
                Mats.Studs(new Color(0.39f, 0.4f, 0.42f), 240f, groundLen), true);

            // ===== БАЗА =====
            float bx = 32f;
            Blocky.Part(staticRoot, new Vector3(0, 0.02f, (GameConfig.BaseMinZ + GameConfig.BaseMaxZ) / 2f),
                new Vector3(bx * 2, 0.1f, GameConfig.BaseMaxZ - GameConfig.BaseMinZ), Mats.Studs(new Color(0.35f, 0.6f, 0.95f), bx * 2, 42), true);
            var wallMat = Mats.Studs(new Color(0.95f, 0.95f, 0.95f), 4, 2);
            Blocky.Part(staticRoot, new Vector3(0, 1.5f, GameConfig.BaseMinZ - 0.5f), new Vector3(bx * 2 + 1, 3f, 1f), wallMat, true);
            Blocky.Part(staticRoot, new Vector3(-bx - 0.5f, 1.5f, (GameConfig.BaseMinZ + GameConfig.BaseMaxZ) / 2f), new Vector3(1f, 3f, 43f), wallMat, true);
            Blocky.Part(staticRoot, new Vector3(bx + 0.5f, 1.5f, (GameConfig.BaseMinZ + GameConfig.BaseMaxZ) / 2f), new Vector3(1f, 3f, 43f), wallMat, true);
            // передняя стена с проходом на дорогу
            float gap = half;
            float seg = bx - gap;
            Blocky.Part(staticRoot, new Vector3(-(gap + seg / 2f), 1.5f, GameConfig.BaseMaxZ), new Vector3(seg, 3f, 1f), wallMat, true);
            Blocky.Part(staticRoot, new Vector3(gap + seg / 2f, 1.5f, GameConfig.BaseMaxZ), new Vector3(seg, 3f, 1f), wallMat, true);

            var signs = new GameObject("Signs").transform;
            Blocky.Label(signs, Loc.T("base"), new Vector3(0, 9f, GameConfig.BaseMaxZ), 3f, new Color(1f, 0.95f, 0.3f));
            BuildArch(staticRoot, new Vector3(0, 0, GameConfig.BaseMaxZ), half, new Color(0.2f, 0.5f, 1f));

            // Грядки: 3 колонки x 4 ряда справа
            int idx = 0;
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 3; col++)
                {
                    var pos = new Vector3(6f + col * 8.6f, 0, -29f + row * 8.6f);
                    gm.Plots.Add(BasePlot.Build(dynamicRoot, idx++, pos));
                }

            // Беговые дорожки слева
            for (int i = 0; i < GameConfig.Treadmills.Length; i++)
                Treadmill.Build(dynamicRoot, i, new Vector3(-27f + i * 6f, 0, -25f), 0f);

            // спавн-площадка
            Blocky.Part(staticRoot, new Vector3(SpawnPoint.x, 0.15f, SpawnPoint.z), new Vector3(5f, 0.3f, 5f),
                Mats.Studs(new Color(0.95f, 0.95f, 0.95f), 5, 5), true);

            // ===== ДОРОГА И ЗОНЫ =====
            float runLen = endZ - GameConfig.BaseMaxZ;
            Blocky.Part(staticRoot, new Vector3(0, 0.02f, GameConfig.BaseMaxZ + runLen / 2f), new Vector3(GameConfig.RunwayWidth, 0.1f, runLen),
                Mats.Studs(new Color(0.5f, 0.5f, 0.52f), GameConfig.RunwayWidth, runLen), true);
            var sideMat = Mats.Studs(new Color(0.63f, 0.37f, 0.2f), 2, 4);
            Blocky.Part(staticRoot, new Vector3(-half - 0.5f, 2f, GameConfig.BaseMaxZ + runLen / 2f), new Vector3(1f, 4f, runLen), sideMat, true);
            Blocky.Part(staticRoot, new Vector3(half + 0.5f, 2f, GameConfig.BaseMaxZ + runLen / 2f), new Vector3(1f, 4f, runLen), sideMat, true);
            Blocky.Part(staticRoot, new Vector3(0, 2f, endZ + 0.5f), new Vector3(GameConfig.RunwayWidth + 2f, 4f, 1f), sideMat, true);

            for (int t = 0; t < GameConfig.Tiers.Length; t++)
            {
                var info = GameConfig.Tiers[t];
                float cz = GameConfig.ZoneCenterZ(t);
                Color floor = Color.Lerp(info.color, new Color(0.5f, 0.5f, 0.5f), 0.45f);
                Blocky.Part(staticRoot, new Vector3(0, 0.05f, cz), new Vector3(GameConfig.RunwayWidth, 0.1f, GameConfig.ZoneLength),
                    Mats.Studs(floor, GameConfig.RunwayWidth, GameConfig.ZoneLength), true);
                float archZ = cz - GameConfig.ZoneLength / 2f;
                BuildArch(staticRoot, new Vector3(0, 0, archZ), half, info.color);
                Blocky.Label(signs, Loc.F("zone", Loc.TierName(info.tier).ToUpper()), new Vector3(0, 9.2f, archZ), 2.2f, info.color);
                Blocky.Label(signs, Loc.F("min_speed", Mathf.RoundToInt(info.guardSpeed)), new Vector3(0, 7.6f, archZ), 1.3f, Color.white);

                var ped = EggPedestal.Build(dynamicRoot, info.tier, new Vector3(0, 0, cz + 6f));
                for (int g = 0; g < info.guardCount; g++)
                {
                    float a = g * Mathf.PI * 2f / info.guardCount;
                    BrainrotGuard.Build(dynamicRoot, ped, info, ped.transform.position + new Vector3(Mathf.Cos(a) * 7f, 0, Mathf.Sin(a) * 7f));
                }
                // декор по бокам зоны
                var deco = Mats.Studs(Color.Lerp(info.color, Color.white, 0.2f), 2, 2);
                Blocky.Part(staticRoot, new Vector3(-half + 3f, 1f, cz + 12f), new Vector3(2f, 2f, 2f), deco, true);
                Blocky.Part(staticRoot, new Vector3(half - 3f, 1f, cz - 8f), new Vector3(2f, 2f, 2f), deco, true);
                Blocky.Part(staticRoot, new Vector3(half - 3f, 3f, cz - 8f), new Vector3(1.4f, 2f, 1.4f), deco, true);
            }

            // Деревья вокруг
            var rnd = new System.Random(42);
            for (int i = 0; i < 46; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (half + 8f + (float)rnd.NextDouble() * 50f);
                float z = -50f + (float)rnd.NextDouble() * (endZ + 60f);
                if (z < GameConfig.BaseMaxZ + 4 && Mathf.Abs(x) < bx + 6) continue;
                Blocky.Tree(staticRoot, new Vector3(x, 0, z), 4f + (float)rnd.NextDouble() * 4f);
            }

            // Объединяем всю статику в батчи — сильно режет draw calls на слабых телефонах
            foreach (var r in staticRoot.GetComponentsInChildren<Renderer>()) r.receiveShadows = true;
            StaticBatchingUtility.Combine(staticRoot.gameObject);
        }

        static void BuildArch(Transform root, Vector3 pos, float half, Color c)
        {
            var m = Mats.Studs(c, 2, 8);
            Blocky.Part(root, pos + new Vector3(-half + 1f, 3.5f, 0), new Vector3(2f, 7f, 2f), m, true);
            Blocky.Part(root, pos + new Vector3(half - 1f, 3.5f, 0), new Vector3(2f, 7f, 2f), m, true);
            Blocky.Part(root, pos + new Vector3(0, 7.5f, 0), new Vector3(half * 2f, 1.2f, 2f), Mats.Studs(c, half * 2f, 2), true);
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
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0);
            sun.intensity = 0.95f;
            sun.color = new Color(1f, 0.97f, 0.9f);
            bool mobile = InputState.Mobile;
            sun.shadows = mobile ? LightShadows.None : LightShadows.Hard;
            sun.shadowStrength = 0.45f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.48f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.32f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.8f, 1f);
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 400f;

            QualitySettings.shadowDistance = 45f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.antiAliasing = mobile ? 0 : 2;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
