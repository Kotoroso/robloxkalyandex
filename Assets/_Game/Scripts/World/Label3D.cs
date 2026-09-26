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
            rt.sizeDelta = new Vector2(1000, 300);
            rt.localScale = Vector3.one * 0.0065f * size;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            var trt = UIKit.Rect(go.transform, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 300));
            var t = trt.gameObject.AddComponent<Text>();
            t.font = Mats.UIFont;
            t.fontSize = 64;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.color = color;
            t.text = text;
            t.lineSpacing = 0.9f;
            var o = trt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 1f);
            o.effectDistance = new Vector2(4, -4);
            var o2 = trt.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0, 0, 0, 0.9f);
            o2.effectDistance = new Vector2(-3, 3);

            var l = go.AddComponent<Label3D>();
            l.txt = t;
            l.canvas = canvas;
            return l;
        }

        void LateUpdate()
        {
            var cam = CameraRig.Cam;
            if (cam == null) return;
            Vector3 d = transform.position - cam.transform.position;
            bool visible = d.sqrMagnitude < maxDistance * maxDistance;
            if (canvas.enabled != visible) canvas.enabled = visible;
            if (visible && billboard) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
