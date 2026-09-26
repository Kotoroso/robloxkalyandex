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
    /// вытянутые телефоны (19.5:9, 20:9). Меняет, по какой стороне масштабируется интерфейс,
    /// и вертикальный FOV камеры, чтобы горизонтальный обзор не "сжимался". В портрете — подсказка повернуть экран.
    /// </summary>
    public class ResponsiveCanvas : MonoBehaviour
    {
        CanvasScaler scaler;
        Vector2Int lastRes;
        GameObject rotateHint;
        bool mobile;

        public void Init(CanvasScaler s, bool isMobile, Transform root)
        {
            scaler = s;
            mobile = isMobile;
            // оверлей "Поверни телефон"
            var rt = UIKit.Rect(root, "RotateHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.2f, 0.97f);
            var icon = UIKit.Icon(rt, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(0, 110), 160);
            icon.gameObject.AddComponent<TitleWobble>();
            UIKit.Label(UIKit.Rect(rt, "T", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(560, 200)),
                Loc.Ru ? "Поверни телефон горизонтально,\nтак играть удобнее!" : "Rotate your phone to landscape\nfor the best experience!", 34, Color.white);
            rotateHint = rt.gameObject;
            rotateHint.SetActive(false);
        }

        void Update()
        {
            var res = new Vector2Int(Screen.width, Screen.height);
            if (res == lastRes || scaler == null) return;
            lastRes = res;
            if (res.x <= 0 || res.y <= 0) return;
            float aspect = res.x / (float)res.y;

            if (aspect < 1f)
            {
                // портрет: интерфейс по ширине, подсказка повернуть
                scaler.referenceResolution = new Vector2(650, 1150);
                scaler.matchWidthOrHeight = 0f;
                if (rotateHint != null) { rotateHint.SetActive(mobile); rotateHint.transform.SetAsLastSibling(); }
            }
            else
            {
                if (rotateHint != null) rotateHint.SetActive(false);
                scaler.referenceResolution = mobile ? new Vector2(1150, 650) : new Vector2(1400, 790);
                // широкие экраны — масштаб по высоте (места по бокам больше),
                // узкие (баннер сбоку, 4:3) — по ширине, чтобы ничего не вылезло за край
                float refAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
                scaler.matchWidthOrHeight = aspect >= refAspect ? 1f : Mathf.Clamp01((aspect - 1.2f) / (refAspect - 1.2f)) * 0.6f;
            }

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
