using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>Хелперы uGUI в стиле роблокс-симуляторов: скруглённые панели с толстой обводкой, глянцевые кнопки, текст с контуром.</summary>
    public static class UIKit
    {
        static Sprite rounded, circle, gloss;
        public static readonly Color Stroke = new Color(0.08f, 0.07f, 0.12f, 1f);

        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int N = 64; const float R = 20f;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        px[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(R - d + 0.5f));
                    }
                tex.SetPixels(px);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
                return rounded;
            }
        }

        /// <summary>Блик на верхней половине кнопки (глянец).</summary>
        static Sprite Gloss
        {
            get
            {
                if (gloss != null) return gloss;
                const int N = 64; const float R = 20f;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float a = Mathf.Clamp01(R - d + 0.5f) * (y > N / 2 ? 0.28f : 0f);
                        px[y * N + x] = new Color(1, 1, 1, a);
                    }
                tex.SetPixels(px);
                tex.Apply();
                gloss = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
                return gloss;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int N = 64;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                var px = new Color[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f));
                        px[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(N / 2f - d));
                    }
                tex.SetPixels(px);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
                return circle;
            }
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Скруглённая панель. stroke > 0 — толстая тёмная обводка как в роблокс-интерфейсах.</summary>
        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, float stroke = 0f)
        {
            var rt = Rect(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            if (stroke > 0)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Stroke;
                o.effectDistance = new Vector2(stroke, -stroke);
                var o2 = rt.gameObject.AddComponent<Outline>();
                o2.effectColor = Stroke;
                o2.effectDistance = new Vector2(-stroke, stroke);
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(rt, 6);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Mats.UIFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 0.95f;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.05f, 0.04f, 0.08f, 1f);
            o.effectDistance = new Vector2(2.2f, -2.2f);
            var o2 = rt.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0.05f, 0.04f, 0.08f, 1f);
            o2.effectDistance = new Vector2(-1.6f, 1.6f);
            return t;
        }

        public static Image Icon(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, float size)
        {
            var rt = Rect(parent, "Icon", anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Глянцевая кнопка с обводкой, тенью и (необязательно) иконкой сверху.</summary>
        public static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            Color color, UnityAction onClick, int fontSize = 26, Sprite icon = null)
        {
            var img = Panel(parent, name, anchor, pivot, pos, size, color, 3f);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.fadeDuration = 0.05f;
            b.colors = colors;
            b.targetGraphic = img;

            var g = Rect(img.transform, "Gloss", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(g, 3);
            var gi = g.gameObject.AddComponent<Image>();
            gi.sprite = Gloss; gi.type = Image.Type.Sliced; gi.raycastTarget = false;

            if (icon != null)
            {
                float s = Mathf.Min(size.x, size.y) * 0.58f;
                Icon(img.transform, icon, new Vector2(0.5f, 0.5f), new Vector2(0, text.Length > 0 ? size.y * 0.1f : 0), s);
                if (text.Length > 0)
                {
                    var t = Label(img.transform, text, fontSize, Color.white, TextAnchor.LowerCenter);
                    t.rectTransform.offsetMin = new Vector2(0, 2);
                }
            }
            else Label(img.transform, text, fontSize, Color.white);

            b.onClick.AddListener(() => { GameAudio.Play(Sfx.Click); b.transform.localScale = Vector3.one * 0.92f; });
            img.gameObject.AddComponent<ButtonBounce>();
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static void AddPointer(GameObject go, UnityAction<BaseEventData> down, UnityAction<BaseEventData> up)
        {
            var et = go.GetComponent<EventTrigger>();
            if (et == null) et = go.AddComponent<EventTrigger>();
            if (down != null)
            {
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                e.callback.AddListener(down);
                et.triggers.Add(e);
            }
            if (up != null)
            {
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                e.callback.AddListener(up);
                et.triggers.Add(e);
                var ex = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                ex.callback.AddListener(up);
                et.triggers.Add(ex);
            }
        }
    }

    /// <summary>Кнопка "пружинит" обратно после нажатия.</summary>
    public class ButtonBounce : MonoBehaviour
    {
        void Update()
        {
            var s = transform.localScale.x;
            if (Mathf.Abs(s - 1f) > 0.001f)
                transform.localScale = Vector3.one * Mathf.MoveTowards(s, 1f, Time.unscaledDeltaTime * 1.5f);
        }
    }

    /// <summary>Плавное появление панели (масштаб "поп").</summary>
    public class PopIn : MonoBehaviour
    {
        float t;
        void OnEnable() { t = 0; transform.localScale = Vector3.one * 0.7f; }
        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime * 6f);
            float k = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, 1f - (1f - t) * (1f - t)) * k;
        }
    }
}
