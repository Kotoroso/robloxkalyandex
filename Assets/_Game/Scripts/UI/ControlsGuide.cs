using UnityEngine;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Мини-обучение управлению (один раз при первом запуске). Отдельные шаги для ПК и телефона,
    /// с анимированными подсказками. Шаг засчитывается сам, когда игрок выполнил действие,
    /// или по кнопке "Дальше". Игрок может двигаться прямо во время обучения.
    /// </summary>
    public class ControlsGuide : MonoBehaviour
    {
        class Page { public string text; public System.Func<bool> done; public System.Action<RectTransform> art; }

        Page[] pages;
        int index;
        RectTransform panel, art;
        Text text, counter;
        Image check;
        float doneTimer = -1f;
        System.Action onFinish;
        bool mobile;
        RectTransform anim1, anim2;
        float t;

        public static void Show(RectTransform root, bool mobile, System.Action onFinish)
        {
            var go = new GameObject("ControlsGuide", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var g = go.AddComponent<ControlsGuide>();
            g.mobile = mobile;
            g.onFinish = onFinish;
            g.Build((RectTransform)go.transform);
        }

        void Build(RectTransform rt)
        {
            UIKit.Stretch(rt);
            bool ru = Loc.Ru;
            const string Y = "<color=#FFD84A>", E = "</color>";
            if (!mobile)
            {
                pages = new[]
                {
                    new Page { text = ru ? "Ходи клавишами " + Y + "W A S D" + E + " или " + Y + "стрелками" + E
                                         : "Walk with " + Y + "W A S D" + E + " or the " + Y + "arrow keys" + E,
                        done = () => InputState.Move.sqrMagnitude > 0.3f, art = ArtKeys },
                    new Page { text = ru ? "Зажми " + Y + "правую кнопку мыши" + E + " и двигай мышь, чтобы повернуть камеру"
                                         : "Hold the " + Y + "right mouse button" + E + " and move the mouse to turn the camera",
                        done = () => InputState.LookDelta.sqrMagnitude > 20f, art = ArtMouse },
                    new Page { text = ru ? "Крути " + Y + "колёсико мыши" + E + ", чтобы приблизить или отдалить камеру"
                                         : "Scroll the " + Y + "mouse wheel" + E + " to zoom the camera in and out",
                        done = () => Mathf.Abs(InputState.Zoom) > 0.01f, art = ArtWheel },
                    new Page { text = ru ? Y + "Пробел" + E + " — прыжок" : Y + "Space" + E + " — jump",
                        done = () => InputState.JumpPressed, art = ArtSpace },
                    new Page { text = ru ? Y + "E" + E + " — действие: украсть яйцо (удерживай), открыть яйцо, взять дракона"
                                         : Y + "E" + E + " — action: steal an egg (hold), open an egg, take a dragon",
                        done = null, art = ArtE },
                };
            }
            else
            {
                pages = new[]
                {
                    new Page { text = ru ? "Джойстик: веди пальцем по " + Y + "левой части экрана" + E + ", чтобы бегать"
                                         : "Joystick: drag on the " + Y + "left side of the screen" + E + " to run",
                        done = () => InputState.Move.sqrMagnitude > 0.3f, art = ArtJoystick },
                    new Page { text = ru ? "Проведи пальцем по " + Y + "правой части экрана" + E + ", чтобы повернуть камеру"
                                         : "Swipe on the " + Y + "right side of the screen" + E + " to turn the camera",
                        done = () => InputState.LookDelta.sqrMagnitude > 20f, art = ArtSwipe },
                    new Page { text = ru ? "Сведи или разведи " + Y + "два пальца" + E + ", чтобы приблизить или отдалить камеру"
                                         : "Pinch with " + Y + "two fingers" + E + " to zoom the camera in and out",
                        done = () => Mathf.Abs(InputState.Zoom) > 0.01f, art = ArtPinch },
                    new Page { text = ru ? "Кнопка " + Y + "«Прыжок»" + E + " справа внизу — прыжок"
                                         : "The " + Y + "Jump" + E + " button at the bottom right makes you jump",
                        done = () => InputState.JumpPressed, art = ArtJumpBtn },
                    new Page { text = ru ? "У яйца, грядки или дорожки появится " + Y + "кнопка действия" + E + " — нажми её"
                                         : "Near eggs, plots and treadmills an " + Y + "action button" + E + " appears — tap it",
                        done = null, art = ArtActionBtn },
                };
            }

            panel = UIKit.Panel(rt, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(620, 330), new Color(0.1f, 0.09f, 0.2f, 0.93f), 4f).rectTransform;
            panel.gameObject.AddComponent<PopIn>();
            var head = UIKit.Panel(panel, "Head", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(360, 54), new Color(0.3f, 0.6f, 1f), 3f);
            UIKit.Label(head.transform, Loc.Ru ? "УПРАВЛЕНИЕ" : "CONTROLS", 28, Color.white);
            // от верха панели (330): вкладка-заголовок до 29, рисунок 48..172, текст 172..256, снизу счётчик и "Дальше" (до 66 от низа)
            art = UIKit.Rect(panel, "Art", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(560, 124));
            text = UIKit.Fit(UIKit.Label(UIKit.Rect(panel, "Text", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(580, 84)), "", 22, Color.white), 14);
            counter = UIKit.Label(UIKit.Rect(panel, "Cnt", new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 18), new Vector2(120, 40)), "", 18, new Color(0.7f, 0.75f, 0.9f), TextAnchor.MiddleLeft);
            UIKit.Button(panel, "Next", Loc.Ru ? "Дальше" : "Next", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 14), new Vector2(170, 54),
                new Color(0.3f, 0.8f, 0.35f), Next, 22);
            check = UIKit.Icon(panel, Icons.Star, new Vector2(1, 1), new Vector2(-40, -40), 60);
            check.color = new Color(0.4f, 1f, 0.5f);
            ShowPage(0);
        }

        void ShowPage(int i)
        {
            index = i;
            doneTimer = -1f;
            check.enabled = false;
            text.text = pages[i].text;
            counter.text = (i + 1) + " / " + pages.Length;
            foreach (Transform c in art) Destroy(c.gameObject);
            anim1 = anim2 = null;
            pages[i].art(art);
        }

        void Next()
        {
            if (index + 1 < pages.Length) ShowPage(index + 1);
            else Finish();
        }

        void Finish()
        {
            GameAudio.Play(Sfx.Success);
            Destroy(gameObject);
            if (onFinish != null) onFinish();
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            var pg = pages[index];
            if (doneTimer < 0 && pg.done != null && pg.done())
            {
                doneTimer = 0.8f;
                check.enabled = true;
                GameAudio.Play(Sfx.Coin);
            }
            if (doneTimer >= 0)
            {
                doneTimer -= Time.unscaledDeltaTime;
                check.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 12f) * 0.1f);
                if (doneTimer < 0) Next();
            }
            // анимации подсказок
            if (anim1 != null) anim1.anchoredPosition = AnimPos(1);
            if (anim2 != null) anim2.anchoredPosition = AnimPos(2);
        }

        Vector2 AnimPos(int which)
        {
            float s = Mathf.Sin(t * 3f);
            switch (index)
            {
                case 0: return mobile ? new Vector2(Mathf.Cos(t * 2.5f) * 28f, Mathf.Sin(t * 2.5f) * 28f) : Vector2.zero;
                case 1: return new Vector2(s * 60f, 0);
                case 2: return mobile ? new Vector2((which == 1 ? -1 : 1) * (40 + s * 25f), 0) : new Vector2(0, s * 8f);
                default: return new Vector2(0, Mathf.Abs(s) * 6f);
            }
        }

        // ===================== Рисунки =====================
        static Image Key(RectTransform parent, string label, Vector2 pos, Vector2 size)
        {
            var k = UIKit.Panel(parent, "Key" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, new Color(0.95f, 0.95f, 1f), 3f);
            k.raycastTarget = false;
            var l = UIKit.Label(k.transform, label, 26, new Color(0.15f, 0.15f, 0.25f));
            foreach (var o in l.GetComponents<Outline>()) o.enabled = false;
            return k;
        }

        void ArtKeys(RectTransform a)
        {
            Key(a, "W", new Vector2(0, 32), new Vector2(58, 58));
            Key(a, "A", new Vector2(-64, -32), new Vector2(58, 58));
            Key(a, "S", new Vector2(0, -32), new Vector2(58, 58));
            Key(a, "D", new Vector2(64, -32), new Vector2(58, 58));
        }

        void ArtMouse(RectTransform a)
        {
            var holder = UIKit.Rect(a, "Mouse", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 120));
            var body = UIKit.Panel(holder, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 116), new Color(0.9f, 0.9f, 0.95f), 3f);
            body.raycastTarget = false;
            var rb = UIKit.Panel(holder, "RB", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(2, 6), new Vector2(34, 48), new Color(1f, 0.75f, 0.2f), 2f);
            rb.raycastTarget = false;
            anim1 = holder;
        }

        void ArtWheel(RectTransform a)
        {
            var body = UIKit.Panel(a, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 116), new Color(0.9f, 0.9f, 0.95f), 3f);
            body.raycastTarget = false;
            var wheel = UIKit.Panel(body.transform, "Wheel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(16, 34), new Color(1f, 0.75f, 0.2f), 2f);
            wheel.raycastTarget = false;
            anim1 = wheel.rectTransform;
        }

        void ArtSpace(RectTransform a)
        {
            anim1 = Key(a, "SPACE", Vector2.zero, new Vector2(300, 60)).rectTransform;
        }

        void ArtE(RectTransform a)
        {
            // только клавиша E (выбор слотов 1-5 убран вместе с нижним инвентарём)
            anim1 = Key(a, "E", Vector2.zero, new Vector2(84, 84)).rectTransform;
        }

        void ArtJoystick(RectTransform a)
        {
            var bg = UIKit.Rect(a, "Joy", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
            var bi = bg.gameObject.AddComponent<Image>(); bi.sprite = UIKit.Circle; bi.color = new Color(1, 1, 1, 0.25f); bi.raycastTarget = false;
            var knob = UIKit.Rect(bg, "Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
            var ki = knob.gameObject.AddComponent<Image>(); ki.sprite = UIKit.Circle; ki.color = new Color(1f, 0.85f, 0.3f); ki.raycastTarget = false;
            anim1 = knob;
        }

        RectTransform Finger(RectTransform a, Vector2 pos)
        {
            var f = UIKit.Rect(a, "Finger", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(46, 46));
            var fi = f.gameObject.AddComponent<Image>(); fi.sprite = UIKit.Circle; fi.color = new Color(1f, 0.85f, 0.3f, 0.95f); fi.raycastTarget = false;
            return f;
        }

        void ArtSwipe(RectTransform a) { anim1 = Finger(a, Vector2.zero); }
        void ArtPinch(RectTransform a) { anim1 = Finger(a, new Vector2(-40, 0)); anim2 = Finger(a, new Vector2(40, 0)); }

        void ArtJumpBtn(RectTransform a)
        {
            var j = UIKit.Panel(a, "Jump", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110), new Color(1, 1, 1, 0.35f));
            j.sprite = UIKit.Circle; j.type = Image.Type.Simple; j.raycastTarget = false;
            UIKit.Label(j.transform, Loc.T("jump"), 20, Color.white);
            anim1 = j.rectTransform;
        }

        void ArtActionBtn(RectTransform a)
        {
            var p = UIKit.Panel(a, "Act", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 74), new Color(0.06f, 0.06f, 0.1f, 0.9f), 3f);
            p.raycastTarget = false;
            UIKit.Label(p.transform, Loc.T("steal"), 22, Color.white);
            anim1 = p.rectTransform;
        }
    }
}
