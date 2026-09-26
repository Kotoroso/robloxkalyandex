using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Тач-управление как в мобильном Роблоксе: плавающий джойстик в левой части экрана,
    /// свайп по правой части — поворот камеры, щипок двумя пальцами — зум, кнопка прыжка справа внизу.
    /// </summary>
    public class MobileControls : MonoBehaviour
    {
        RectTransform joyBg, joyKnob, canvasRt;
        int joyFinger = -1, camFinger = -1, zoomFinger = -1;
        Vector2 joyCenter;
        float joyRadius = 90f;
        float lastPinch;

        public void Build(Transform root)
        {
            canvasRt = root as RectTransform;
            var bg = UIKit.Rect(root, "JoyBg", Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(170, 170), new Vector2(200, 200));
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = UIKit.Circle; bgImg.color = new Color(1, 1, 1, 0.18f); bgImg.raycastTarget = false;
            joyBg = bg;
            var knob = UIKit.Rect(bg, "Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            var kImg = knob.gameObject.AddComponent<Image>();
            kImg.sprite = UIKit.Circle; kImg.color = new Color(1, 1, 1, 0.55f); kImg.raycastTarget = false;
            joyKnob = knob;
            if (UIKit.ApplySkin(bgImg, "joystick_bg", false)) bgImg.type = Image.Type.Simple;
            if (UIKit.ApplySkin(kImg, "joystick_knob", false)) kImg.type = Image.Type.Simple;

            var jump = UIKit.Panel(root, "Jump", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(130, 130), new Color(1, 1, 1, 0.3f));
            jump.sprite = UIKit.Circle;
            jump.type = Image.Type.Simple;
            UIKit.Label(jump.transform, Loc.T("jump"), 24, Color.white);
            if (UIKit.ApplySkin(jump, "jump", false)) { jump.type = Image.Type.Simple; UIKit.HideLabels(jump.transform); }
            UIKit.AddPointer(jump.gameObject, e => InputState.TouchJump = true, null);
        }

        static bool OverUI(int fingerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
        }

        void Update()
        {
            float scale = canvasRt != null ? canvasRt.lossyScale.x : 1f;
            if (scale <= 0) scale = 1f;
            float radiusPx = joyRadius * scale;

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (OverUI(t.fingerId)) break;
                        if (joyFinger < 0 && t.position.x < Screen.width * 0.42f)
                        {
                            joyFinger = t.fingerId;
                            joyCenter = t.position;
                            joyBg.position = t.position;
                        }
                        else if (camFinger < 0) { camFinger = t.fingerId; }
                        else if (zoomFinger < 0) { zoomFinger = t.fingerId; lastPinch = -1; }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (t.fingerId == joyFinger)
                        {
                            Vector2 delta = Vector2.ClampMagnitude(t.position - joyCenter, radiusPx);
                            joyKnob.position = joyCenter + delta;
                            InputState.TouchMove = delta / radiusPx;
                        }
                        else if (t.fingerId == camFinger && zoomFinger < 0)
                        {
                            InputState.TouchLook += t.deltaPosition * 0.5f / scale;
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == joyFinger)
                        {
                            joyFinger = -1;
                            InputState.TouchMove = Vector2.zero;
                            joyKnob.localPosition = Vector3.zero;
                            joyBg.anchoredPosition = new Vector2(170, 170);
                        }
                        if (t.fingerId == camFinger) { camFinger = zoomFinger; zoomFinger = -1; }
                        else if (t.fingerId == zoomFinger) zoomFinger = -1;
                        break;
                }
            }

            // щипок для зума
            if (camFinger >= 0 && zoomFinger >= 0)
            {
                Vector2 a = Vector2.zero, b = Vector2.zero;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId == camFinger) a = t.position;
                    if (t.fingerId == zoomFinger) b = t.position;
                }
                float dist = Vector2.Distance(a, b);
                if (lastPinch > 0) InputState.TouchZoom += (lastPinch - dist) * 0.02f / scale;
                lastPinch = dist;
            }

            if (Input.touchCount == 0 && joyFinger >= 0)
            {
                joyFinger = camFinger = zoomFinger = -1;
                InputState.TouchMove = Vector2.zero;
            }
        }
    }
}
