using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Настройки качества под платформу + динамический "губернатор" FPS.
    /// Меряет средний FPS окнами по 2 с; если долго ниже порога — снижает качество ступенями
    /// (тени → разрешение (devicePixelRatio) → частицы/дальность → на мобилке лимит 30 FPS),
    /// если долго с запасом — возвращает ступень (не больше пары раз, чтобы не "качаться").
    /// </summary>
    public class Perf : MonoBehaviour
    {
        struct Profile
        {
            public LightShadows shadows;
            public float shadowDistance;
            public int aa;
            public float dprScale;      // множитель к стартовому devicePixelRatio
            public float particles;     // множитель частоты/количества частиц
            public float cull;          // множитель дальностей отсечения (декор, персонажи, надписи, частицы)
            public bool cheapShader;    // Lambert вместо Standard
            public int fps;             // целевой FPS
        }

        static readonly Profile[] Desktop =
        {
            new Profile { shadows = LightShadows.Soft, shadowDistance = 45f, aa = 4, dprScale = 1f,    particles = 1f,   cull = 1f,    fps = 60 },
            new Profile { shadows = LightShadows.Hard, shadowDistance = 35f, aa = 2, dprScale = 1f,    particles = 1f,   cull = 1f,    fps = 60 },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f,  aa = 0, dprScale = 1f,    particles = 1f,   cull = 0.9f,  fps = 60 },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f,  aa = 0, dprScale = 0.75f, particles = 0.6f, cull = 0.8f,  fps = 60 },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f,  aa = 0, dprScale = 0.6f,  particles = 0.4f, cull = 0.7f,  fps = 60, cheapShader = true },
        };

        static readonly Profile[] Mobile =
        {
            new Profile { shadows = LightShadows.None, shadowDistance = 0f, aa = 0, dprScale = 1f,   particles = 1f,    cull = 1f,   fps = 60, cheapShader = true },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f, aa = 0, dprScale = 0.8f, particles = 0.6f,  cull = 0.85f, fps = 60, cheapShader = true },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f, aa = 0, dprScale = 0.8f, particles = 0.4f,  cull = 0.7f, fps = 30, cheapShader = true },
            new Profile { shadows = LightShadows.None, shadowDistance = 0f, aa = 0, dprScale = 0.68f, particles = 0.3f, cull = 0.6f, fps = 30, cheapShader = true },
        };

        public static Perf Instance;
        public static bool IsMobile;
        public static int Level { get; private set; }
        /// <summary>Множитель частиц для новых систем (Fx).</summary>
        public static float ParticleScale = 1f;
        /// <summary>Множитель дальности показа надписей Label3D.</summary>
        public static float LabelDistanceScale = 1f;
        /// <summary>Дальность, дальше которой зацикленные частицы выключаются.</summary>
        public static float ParticleCullDistance = 70f;

        static Profile[] table;
        static float baseDpr = 1f;

        // базовые дальности (до множителя cull)
        const float DecorCullDesktop = 170f, DecorCullMobile = 120f;
        const float ActorCullDesktop = 190f, ActorCullMobile = 130f;
        const float FloorCullDesktop = 260f, FloorCullMobile = 185f;
        const float FarClipDesktop = 420f, FarClipMobile = 320f;

        // губернатор
        float winTime, lastChange;
        int winFrames, lowCount, highCount;
        readonly int[] downs = new int[8];
        Camera appliedCam;
        float particleTick;

        /// <summary>Вызывается из GameBootstrap, когда уже известно, мобилка ли это.</summary>
        public static void Init(bool mobile)
        {
            if (Instance != null) return;
            IsMobile = mobile;
            table = mobile ? Mobile : Desktop;
            baseDpr = Dpr.Get();
            if (baseDpr <= 0f) baseDpr = 1f;

            // общие настройки (не меняются губернатором)
            QualitySettings.vSyncCount = 0;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowResolution = mobile ? ShadowResolution.Low : ShadowResolution.Medium;
            QualitySettings.anisotropicFiltering = mobile ? AnisotropicFiltering.Disable : AnisotropicFiltering.Enable;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            QualitySettings.lodBias = mobile ? 0.7f : 1f;
            QualitySettings.particleRaycastBudget = 16;
            Time.maximumDeltaTime = 0.1f; // после фризов не "догоняем" физику десятком шагов

            var go = new GameObject("Perf");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<Perf>();
            Apply(0, true);
        }

        /// <summary>Тени солнца по текущему профилю (вызывается при настройке света).</summary>
        public static void ApplySun(Light sun)
        {
            if (sun == null || table == null) return;
            var p = table[Level];
            sun.shadows = p.shadows;
        }

        static void Apply(int level, bool initial)
        {
            Level = Mathf.Clamp(level, 0, table.Length - 1);
            var p = table[Level];
            QualitySettings.antiAliasing = p.aa;
            QualitySettings.shadows = p.shadows == LightShadows.None ? ShadowQuality.Disable : (p.shadows == LightShadows.Hard ? ShadowQuality.HardOnly : ShadowQuality.All);
            QualitySettings.shadowDistance = p.shadowDistance;
            if (RenderSettings.sun != null) RenderSettings.sun.shadows = p.shadows;
            Application.targetFrameRate = p.fps;
            ParticleScale = p.particles;
            LabelDistanceScale = Mathf.Lerp(1f, p.cull, 0.6f);
            ParticleCullDistance = (IsMobile ? 50f : 70f) * p.cull;
            Mats.SetCheapLighting(p.cheapShader);
            float minDpr = IsMobile ? 0.75f : 1f;
            float dpr = Mathf.Max(Mathf.Min(minDpr, baseDpr), baseDpr * p.dprScale);
            if (!initial || p.dprScale < 1f) Dpr.Set(dpr);
            if (Instance != null) Instance.appliedCam = null; // пересчитать камеру
            if (!initial) Debug.Log("[Perf] quality level " + Level + " (dpr " + dpr.ToString("0.00") + ")");
        }

        void ApplyCamera(Camera cam)
        {
            var p = table[Level];
            float cull = p.cull;
            float far = (IsMobile ? FarClipMobile : FarClipDesktop) * Mathf.Lerp(1f, cull, 0.5f);
            cam.farClipPlane = far;
            // туман заканчивается до дальней плоскости — обрез геометрии не виден
            if (RenderSettings.fog)
            {
                RenderSettings.fogEndDistance = far * 0.92f;
                RenderSettings.fogStartDistance = far * (IsMobile ? 0.28f : 0.26f);
            }
            var d = new float[32];
            d[MeshMerge.DecorLayer] = (IsMobile ? DecorCullMobile : DecorCullDesktop) * cull;
            d[MeshMerge.ActorLayer] = (IsMobile ? ActorCullMobile : ActorCullDesktop) * cull;
            d[MeshMerge.FloorLayer] = (IsMobile ? FloorCullMobile : FloorCullDesktop) * cull;
            cam.layerCullDistances = d;
            cam.layerCullSpherical = true;
            appliedCam = cam;
            lastChange = Time.realtimeSinceStartup; // прогрев после постройки мира / смены профиля — не меряем
        }

        void Update()
        {
            var cam = CameraRig.Cam;
            if (cam != null && cam != appliedCam) ApplyCamera(cam);

            float dt = Time.unscaledDeltaTime;
            // частицы вдали — выключаем (по кусочку списка за раз)
            particleTick -= dt;
            if (particleTick <= 0f && cam != null)
            {
                particleTick = 0.2f;
                Fx.CullTick(cam.transform.position, ParticleCullDistance);
            }

            // не меряем: пауза/реклама/вкладка скрыта, фризы (загрузка, GC), первые секунды после смены
            bool paused = YandexSDK.Paused || !Application.isFocused;
            if (paused || dt > 0.25f || Time.realtimeSinceStartup - lastChange < 4f || Time.realtimeSinceStartup < 8f)
            {
                winTime = 0; winFrames = 0;
                return;
            }
            winTime += dt;
            winFrames++;
            if (winTime < 2f) return;
            float fps = winFrames / winTime;
            winTime = 0; winFrames = 0;
            int target = table[Level].fps;

            if (fps < target * 0.8f) { lowCount++; highCount = 0; }
            else if (fps > target * 0.95f) { highCount++; lowCount = 0; }
            else { lowCount = 0; highCount = 0; }

            if (lowCount >= 2 && Level < table.Length - 1)
            {
                downs[Level]++;
                lowCount = 0; highCount = 0;
                lastChange = Time.realtimeSinceStartup;
                Apply(Level + 1, false);
            }
            else if (highCount >= 10 && Level > 0 && table[Level].fps == table[Level - 1].fps && downs[Level - 1] < 2)
            {
                // 20 с с запасом — пробуем вернуть ступень (с лимитом 30 FPS запас не измерить — там не поднимаемся)
                lowCount = 0; highCount = 0;
                lastChange = Time.realtimeSinceStartup;
                Apply(Level - 1, false);
            }
        }
    }

    /// <summary>
    /// Разрешение рендера в WebGL: Module.devicePixelRatio (стартовое значение задаёт WebGL-шаблон).
    /// Unity берёт его каждый кадр при подгонке размера канваса, поэтому его можно менять на лету.
    /// </summary>
    public static class Dpr
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern float PerfGetDpr();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void PerfSetDpr(float v);
        public static float Get() { try { return PerfGetDpr(); } catch { return 1f; } }
        public static void Set(float v) { try { PerfSetDpr(v); } catch { } }
#else
        public static float Get() { return 1f; }
        public static void Set(float v) { }
#endif
    }
}
