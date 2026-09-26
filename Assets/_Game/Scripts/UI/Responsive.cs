using UnityEngine;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Отступы под вырезы/чёлки телефонов (Screen.safeArea). Вешается на корневой контейнер HUD.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastRes;

        void Update()
        {
            var sa = Screen.safeArea;
            var res = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && res == lastRes) return;
            last = sa; lastRes = res;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            Vector2 min = sa.position, max = sa.position + sa.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Адаптация под популярные разрешения игроков Яндекс Игр:
    /// 16:9 (1920x1080, 1366x768, 1536x864, 1280x720), узкие окна со стики-баннером (~4:3..16:10),
    /// вытянутые телефоны (19.5:9, 20:9), планшеты 4:3, ПОРТРЕТ (телефон вертикально — в Яндекс Играх так играют часто).
    /// Меняет, по какой стороне масштабируется интерфейс, и вертикальный FOV камеры, чтобы горизонтальный обзор не "сжимался".
    ///
    /// Размер холста (в единицах интерфейса) для популярных экранов:
    ///   телефон, альбом (холст 1150x650, по высоте): 640x360 → 1156x650, 740x360 → 1336x650, 844x390 → 1406x650,
    ///     915x412 → 1443x650, 932x430 → 1409x650; планшет 1024x768 → 1105x829, 1180x820 → 1092x759;
    ///   ПК (1400x790): 1366x768 → 1405x790, 1920x1080 / 2560x1440 → 1404x790, 21:9 2560x1080 → 1872x790;
    ///   портрет (650x1150, "Expand": ширина >= 650 и высота >= 1150): 360x640 → 650x1156, 390x844 → 650x1407,
    ///     412x915 → 650x1444, планшет 768x1024 → 863x1150.
    /// Когда размер экрана устоялся (поворот телефона, изменение окна), вызывается Changed — UIManager
    /// переставляет HUD (альбом/портрет) и перестраивает окна под новый размер.
    /// </summary>
    public class ResponsiveCanvas : MonoBehaviour
    {
        CanvasScaler scaler;
        Vector2Int lastRes;
        Rect lastSafe;
        bool mobile, pending, first = true;
        float pendingTimer;

        /// <summary>Размер холста в единицах интерфейса (для текущего экрана).</summary>
        public static Vector2 CanvasSize { get; private set; }
        /// <summary>Безопасная область (без чёлки/выреза) в единицах холста, начало — левый нижний угол.</summary>
        public static Rect SafeRect { get; private set; }
        /// <summary>Экран выше, чем шире (телефон вертикально, узкое окно).</summary>
        public static bool Portrait { get; private set; }
        /// <summary>Размер экрана изменился и устоялся (с задержкой ~0.25 c, чтобы не перестраивать окна на каждом кадре ресайза).</summary>
        public System.Action Changed;

        public void Init(CanvasScaler s, bool isMobile, Transform root)
        {
            scaler = s;
            mobile = isMobile;
            // Раньше в портрете весь экран закрывала подсказка "Поверни телефон" — теперь интерфейс сам
            // перестраивается под портрет (см. UIManager.ApplyLayout), играть можно в любой ориентации.
            Measure(mobile);
        }

        /// <summary>Опорное разрешение и match CanvasScaler для экрана res.</summary>
        public static void Params(Vector2 res, bool isMobile, out Vector2 refRes, out float match)
        {
            float aspect = res.x / Mathf.Max(1f, res.y);
            if (aspect < 1f)
            {
                // портрет: ширина холста >= 650 и высота >= 1150 (как ScreenMatchMode.Expand):
                // телефоны (<= 9:16) — по ширине, планшеты/почти квадратные окна — по высоте
                refRes = new Vector2(650, 1150);
                match = aspect <= 650f / 1150f ? 0f : 1f;
            }
            else
            {
                refRes = isMobile ? new Vector2(1150, 650) : new Vector2(1400, 790);
                // широкие экраны — масштаб по высоте (места по бокам больше),
                // узкие (баннер сбоку, 4:3) — по ширине, чтобы ничего не вылезло за край
                float refAspect = refRes.x / refRes.y;
                match = aspect >= refAspect ? 1f : Mathf.Clamp01((aspect - 1.2f) / (refAspect - 1.2f)) * 0.6f;
            }
        }

        /// <summary>Считает размер холста и безопасную область так же, как CanvasScaler (ScaleWithScreenSize, MatchWidthOrHeight).</summary>
        public static void Measure(bool isMobile)
        {
            int w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;
            Vector2 refRes; float match;
            Params(new Vector2(w, h), isMobile, out refRes, out match);
            float lw = Mathf.Log(w / refRes.x, 2f), lh = Mathf.Log(h / refRes.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(lw, lh, match));
            if (scale <= 0f) scale = 1f;
            CanvasSize = new Vector2(w / scale, h / scale);
            var sa = Screen.safeArea;
            if (sa.width <= 1f || sa.height <= 1f) sa = new Rect(0, 0, w, h);
            SafeRect = new Rect(sa.x / scale, sa.y / scale, sa.width / scale, sa.height / scale);
            Portrait = w < h;
        }

        void Update()
        {
            if (scaler == null) return;
            if (pending)
            {
                pendingTimer -= Time.unscaledDeltaTime;
                if (pendingTimer <= 0f) { pending = false; if (Changed != null) Changed(); }
            }
            var res = new Vector2Int(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            if (res == lastRes && safe == lastSafe) return;
            lastRes = res; lastSafe = safe;
            if (res.x <= 0 || res.y <= 0) return;
            float aspect = res.x / (float)res.y;

            Vector2 refRes; float match;
            Params(new Vector2(res.x, res.y), mobile, out refRes, out match);
            scaler.referenceResolution = refRes;
            scaler.matchWidthOrHeight = match;
            Measure(mobile);
            // первый раз — сразу (на старте), дальше — когда размер перестанет меняться
            pending = true;
            pendingTimer = first ? 0f : 0.25f;
            first = false;

            // FOV: сохраняем горизонтальный обзор ~100° на узких экранах, на широких — вертикальный 70°
            var cam = CameraRig.Cam;
            if (cam != null)
            {
                float hfov = 100f * Mathf.Deg2Rad;
                float vfov = 2f * Mathf.Atan(Mathf.Tan(hfov / 2f) / Mathf.Max(0.5f, aspect)) * Mathf.Rad2Deg;
                cam.fieldOfView = Mathf.Clamp(vfov, 62f, 85f);
            }
        }
    }

    /// <summary>Пульсирующая кнопка награды, пока награда доступна.</summary>
    public class DailyBadge : MonoBehaviour
    {
        void Update()
        {
            var gm = GameManager.Instance;
            bool avail = gm != null && gm.DailyAvailable;
            float s = avail ? 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 0.1f : 1f;
            transform.localScale = Vector3.one * s;
            transform.localRotation = Quaternion.Euler(0, 0, avail ? Mathf.Sin(Time.unscaledTime * 6f) * 6f : 0f);
        }
    }
}

namespace DragonHeist
{
    /// <summary>Переливающийся радужный цвет картинки (радужный трейл в магазине).</summary>
    public class RainbowImage : MonoBehaviour
    {
        UnityEngine.UI.Image img;
        void Awake() { img = GetComponent<UnityEngine.UI.Image>(); }
        void Update()
        {
            if (img == null) return;
            var c = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.2f, 1f), 0.55f, 0.85f);
            img.color = c;
        }
    }
}
