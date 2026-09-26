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
    /// вытянутые телефоны (19.5:9, 20:9), планшеты 4:3. Меняет, по какой стороне масштабируется интерфейс,
    /// и вертикальный FOV камеры, чтобы горизонтальный обзор не "сжимался".
    /// Игра только альбомная: в портрете весь экран закрывает подсказка "Поверните устройство" (блокирует ввод, игра идёт дальше).
    ///
    /// Размер холста (в единицах интерфейса) для популярных экранов:
    ///   телефон (опорный 1150x650): 640x360 → 1156x650, 740x360 → 1336x650, 844x390 → 1406x650,
    ///     915x412 → 1443x650, 932x430 → 1409x650; планшет 1024x768 → 1105x829, 1180x820 (iPad) → 1092x759;
    ///   ПК (опорный 1400x790): 1366x768 → 1405x790, 1920x1080 / 2560x1440 → 1404x790, 21:9 2560x1080 → 1872x790,
    ///     узкое окно 4:3 1024x768 → 1346x1009.
    /// Итого в альбоме холст всегда >= ~1090 x 650 (телефон/планшет) и >= ~1336 x 790 (ПК) — под это рассчитан HUD и окна.
    /// </summary>
    public class ResponsiveCanvas : MonoBehaviour
    {
        CanvasScaler scaler;
        Vector2Int lastRes;
        GameObject rotateHint;
        bool mobile;

        /// <summary>Безопасная область (без выреза/чёлки) альбомного холста, в единицах интерфейса. В портрете — как если бы экран повернули.</summary>
        public static Vector2 LandscapeSafeSize { get; private set; }
        /// <summary>Сейчас портрет: показана подсказка повернуть устройство, ввод в игре заблокирован.</summary>
        public static bool RotateBlocking { get; private set; }

        public void Init(CanvasScaler s, bool isMobile, Transform root)
        {
            scaler = s;
            mobile = isMobile;
            Measure(mobile);
            // оверлей "Поверните устройство": на весь экран, перехватывает все нажатия
            var rt = UIKit.Rect(root, "RotateHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.2f, 0.98f);
            img.raycastTarget = true;
            // "телефон", который поворачивается из вертикального положения в горизонтальное
            var phone = UIKit.Panel(rt, "Phone", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(120, 200), new Color(0.35f, 0.75f, 1f), 5f);
            phone.raycastTarget = false;
            var screen = UIKit.Panel(phone.transform, "Screen", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 164), new Color(0.1f, 0.12f, 0.25f));
            screen.raycastTarget = false;
            var icon = UIKit.Icon(screen.transform, Icons.Dragon, new Vector2(0.5f, 0.5f), Vector2.zero, 80);
            icon.raycastTarget = false;
            phone.gameObject.AddComponent<RotateHintAnim>();
            var t = UIKit.Label(UIKit.Rect(rt, "T", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(580, 200)),
                Loc.Ru ? "Поверните устройство\nгоризонтально" : "Rotate your device\nto landscape", 44, Color.white);
            UIKit.Fit(t, 24);
            rotateHint = rt.gameObject;
            rotateHint.SetActive(false);
        }

        /// <summary>Опорное разрешение и match CanvasScaler для экрана res.</summary>
        public static void Params(Vector2 res, bool isMobile, out Vector2 refRes, out float match)
        {
            float aspect = res.x / Mathf.Max(1f, res.y);
            if (aspect < 1f)
            {
                // портрет (только подсказка повернуть): ширина холста >= 650 и высота >= 1150 — надпись всегда влезает
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

        /// <summary>
        /// Безопасная область альбомного холста так же, как её посчитает CanvasScaler (ScaleWithScreenSize, MatchWidthOrHeight).
        /// Если сейчас портрет — считаем для повёрнутого экрана (окна строятся один раз, сразу под альбом).
        /// </summary>
        public static void Measure(bool isMobile)
        {
            int w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;
            var sa = Screen.safeArea;
            if (h > w) { int tmp = w; w = h; h = tmp; sa = new Rect(0, 0, w, h); }
            if (sa.width <= 1f || sa.height <= 1f) sa = new Rect(0, 0, w, h);
            Vector2 refRes; float match;
            Params(new Vector2(w, h), isMobile, out refRes, out match);
            float lw = Mathf.Log(w / refRes.x, 2f), lh = Mathf.Log(h / refRes.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(lw, lh, match));
            if (scale <= 0f) scale = 1f;
            LandscapeSafeSize = new Vector2(sa.width / scale, sa.height / scale);
        }

        void Update()
        {
            // подсказка всегда поверх всего (рулетка, обучение управлению создаются позже)
            if (rotateHint != null && rotateHint.activeSelf) rotateHint.transform.SetAsLastSibling();
            var res = new Vector2Int(Screen.width, Screen.height);
            if (res == lastRes || scaler == null) return;
            lastRes = res;
            if (res.x <= 0 || res.y <= 0) return;
            float aspect = res.x / (float)res.y;

            Vector2 refRes; float match;
            Params(new Vector2(res.x, res.y), mobile, out refRes, out match);
            scaler.referenceResolution = refRes;
            scaler.matchWidthOrHeight = match;
            RotateBlocking = aspect < 1f;
            if (rotateHint != null) rotateHint.SetActive(RotateBlocking);

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

    /// <summary>Анимация подсказки: телефон поворачивается из вертикального положения в горизонтальное и обратно.</summary>
    public class RotateHintAnim : MonoBehaviour
    {
        void Update()
        {
            // 0..1.2 c — стоит вертикально, 1.2..2 c — поворот, 2..3.2 c — горизонтально, потом рывком назад
            float t = Mathf.Repeat(Time.unscaledTime, 3.2f);
            float k = Mathf.Clamp01((t - 1.2f) / 0.8f);
            k = k * k * (3f - 2f * k);
            transform.localRotation = Quaternion.Euler(0, 0, -90f * k);
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
