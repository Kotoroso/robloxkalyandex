using UnityEngine;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Надпись в мире (как BillboardGui в Роблоксе): жирный шрифт, чёрная обводка, всегда смотрит в камеру,
    /// скрывается на большой дистанции ради производительности.
    /// </summary>
    public class Label3D : MonoBehaviour
    {
        Text txt;
        Canvas canvas;
        public float maxDistance = 110f;
        public bool billboard = true;   // false — надпись на табличке, не поворачивается

        public string text
        {
            get { return txt.text; }
            set { if (txt.text != value) txt.text = value; }
        }

        public Color color
        {
            get { return txt.color; }
            set { txt.color = value; }
        }

        public static Label3D Create(Transform parent, string text, Vector3 localPos, float size, Color color)
        {
            var go = new GameObject("Label3D", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localPosition = localPos;
            rt.sizeDelta = new Vector2(1500, 450);
            rt.localScale = Vector3.one * 0.0043f * size; // крупный шрифт + мелкий масштаб = чёткие буквы
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            var trt = UIKit.Rect(go.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 450));
            var t = trt.gameObject.AddComponent<Text>();
            t.font = Mats.UIFont;
            t.fontSize = 96;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.color = color;
            t.text = text;
            t.lineSpacing = 0.9f;
            var o = trt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 1f);
            o.effectDistance = new Vector2(5, -5);
            // каждая Outline умножает геометрию текста на 5 (две подряд — на 25: лишний overdraw и пересборка).
            // На телефоне — одна обводка, на ПК — вторая для более плотного контура.
            if (!InputState.Mobile)
            {
                var o2 = trt.gameObject.AddComponent<Outline>();
                o2.effectColor = new Color(0, 0, 0, 0.9f);
                o2.effectDistance = new Vector2(-4, 4);
            }

            var l = go.AddComponent<Label3D>();
            l.txt = t;
            l.canvas = canvas;
            l.slot = nextSlot++ & 3;
            return l;
        }

        static int nextSlot;
        int slot;
        bool visible = true;

        void LateUpdate()
        {
            var cam = CameraRig.Cam;
            if (cam == null) return;
            var ct = cam.transform;
            Vector3 d = transform.position - ct.position;
            // видимость проверяем раз в 4 кадра (у разных надписей — в разные кадры), с гистерезисом,
            // чтобы канвас не включался/выключался (и не пересобирался) на границе
            if (((Time.frameCount + slot) & 3) == 0 || visible)
            {
                float max = maxDistance * Perf.LabelDistanceScale;
                float d2 = d.sqrMagnitude;
                bool want = visible ? d2 < max * max * 1.1f : d2 < max * max;
                // позади камеры (с запасом на размер надписи) — не рисуем
                if (want && Vector3.Dot(d, ct.forward) < -4f) want = false;
                if (want != visible)
                {
                    visible = want;
                    canvas.enabled = want;
                }
            }
            if (visible && billboard && d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
