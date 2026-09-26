using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>Весь интерфейс строится кодом. Адаптирован под ПК и телефоны (крупнее кнопки на мобилках).</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        Canvas canvas;
        Text coinsText, speedText, rebirthText, cpsText, toastText, carryText, invText, hintText;
        Image toastBg, carryBg;
        Image promptBg, promptFill;
        Text promptText;
        float toastTimer;
        GameObject rebirthPanel, dragonsPanel, shopPanel;
        Text rebirthBody, dragonsBody, shopPlotBtnText;
        Text soundBtnText;
        float refreshTimer;

        public static UIManager Create()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }
            var go = new GameObject("UI");
            var ui = go.AddComponent<UIManager>();
            Instance = ui;
            ui.Build();
            return ui;
        }

        void Build()
        {
            bool mobile = InputState.Mobile;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = mobile ? new Vector2(1100, 620) : new Vector2(1366, 768);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = transform;

            // ===== Статы слева сверху =====
            var stats = UIKit.Panel(root, "Stats", new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(300, 150), new Color(0, 0, 0, 0.35f));
            coinsText = StatLine(stats.transform, 0, new Color(1f, 0.85f, 0.2f));
            cpsText = StatLine(stats.transform, 1, new Color(1f, 0.95f, 0.6f));
            speedText = StatLine(stats.transform, 2, new Color(0.4f, 0.95f, 1f));
            rebirthText = StatLine(stats.transform, 3, new Color(1f, 0.55f, 1f));

            // ===== Кнопки справа =====
            float bw = mobile ? 170 : 190, bh = mobile ? 58 : 56;
            UIKit.Button(root, "BtnRebirth", Loc.T("rebirth"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(bw, bh),
                new Color(0.85f, 0.3f, 0.9f), () => Toggle(rebirthPanel));
            UIKit.Button(root, "BtnDragons", Loc.T("dragons"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14 - (bh + 10)), new Vector2(bw, bh),
                new Color(1f, 0.55f, 0.15f), () => Toggle(dragonsPanel));
            UIKit.Button(root, "BtnShop", Loc.T("shop"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14 - 2 * (bh + 10)), new Vector2(bw, bh),
                new Color(0.25f, 0.75f, 0.3f), () => Toggle(shopPanel));
            var sb = UIKit.Button(root, "BtnSound", "", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14 - 3 * (bh + 10)), new Vector2(bw, bh),
                new Color(0.4f, 0.45f, 0.55f), ToggleSound, 22);
            soundBtnText = sb.GetComponentInChildren<Text>();
            UpdateSoundText();

            // ===== Тост и баннер переноски =====
            toastBg = UIKit.Panel(root, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(640, 64), new Color(0, 0, 0, 0.55f));
            toastText = UIKit.Label(toastBg.transform, "", 26, Color.white);
            toastBg.raycastTarget = false;
            toastBg.gameObject.SetActive(false);

            carryBg = UIKit.Panel(root, "Carry", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(560, 52), new Color(0.9f, 0.2f, 0.2f, 0.8f));
            carryText = UIKit.Label(carryBg.transform, "", 24, Color.white);
            carryBg.raycastTarget = false;
            carryBg.gameObject.SetActive(false);

            var hintRt = UIKit.Rect(root, "Hint", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 230 : 150), new Vector2(700, 40));
            hintText = UIKit.Label(hintRt, "", 22, new Color(1f, 1f, 0.7f));

            var invRt = UIKit.Rect(root, "Inv", new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -170), new Vector2(360, 34));
            invText = UIKit.Label(invRt, "", 20, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft);

            // ===== Подсказка взаимодействия / кнопка действия =====
            Vector2 promptAnchor = mobile ? new Vector2(1, 0) : new Vector2(0.5f, 0);
            Vector2 promptPivot = mobile ? new Vector2(1, 0) : new Vector2(0.5f, 0);
            Vector2 promptPos = mobile ? new Vector2(-30, 190) : new Vector2(0, 60);
            Vector2 promptSize = mobile ? new Vector2(330, 90) : new Vector2(460, 76);
            promptBg = UIKit.Panel(root, "Prompt", promptAnchor, promptPivot, promptPos, promptSize, new Color(0.1f, 0.1f, 0.1f, 0.75f));
            var fillImg = UIKit.Panel(promptBg.transform, "Fill", new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero, new Color(0.3f, 0.85f, 0.35f, 0.9f));
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            fillImg.raycastTarget = false;
            promptFill = fillImg;
            promptText = UIKit.Label(promptBg.transform, "", mobile ? 26 : 24, Color.white);
            if (mobile)
                UIKit.AddPointer(promptBg.gameObject, e => InputState.TouchAction = true, e => InputState.TouchAction = false);
            else promptBg.raycastTarget = false;
            promptBg.gameObject.SetActive(false);

            // ===== Панели =====
            rebirthPanel = Modal(root, Loc.T("rebirth"), new Color(0.55f, 0.2f, 0.65f), out rebirthBody);
            UIKit.Button(rebirthPanel.transform, "Do", Loc.T("rebirth"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(260, 64),
                new Color(0.85f, 0.3f, 0.9f), () => { if (GameManager.Instance.TryRebirth()) rebirthPanel.SetActive(false); }, 28);

            dragonsPanel = Modal(root, Loc.T("dragons"), new Color(0.75f, 0.4f, 0.1f), out dragonsBody);
            dragonsBody.alignment = TextAnchor.UpperLeft;
            dragonsBody.fontSize = 16;

            shopPanel = Modal(root, Loc.T("shop"), new Color(0.15f, 0.55f, 0.2f), out Text shopBody);
            shopBody.text = Loc.Ru
                ? "Больше грядок — больше яиц одновременно!\nДорожки покупаются прямо на базе."
                : "More plots — more eggs at once!\nTreadmills are bought at your base.";
            var pb = UIKit.Button(shopPanel.transform, "BuyPlot", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(380, 64),
                new Color(0.25f, 0.75f, 0.3f), () => GameManager.Instance.TryBuyPlot(), 24);
            shopPlotBtnText = pb.GetComponentInChildren<Text>();

            if (mobile) gameObject.AddComponent<MobileControls>().Build(root);

            Toast(Loc.T("hint_start"), Color.white, 6f);
        }

        Text StatLine(Transform parent, int line, Color c)
        {
            var rt = UIKit.Rect(parent, "Line" + line, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -6 - line * 35), new Vector2(290, 36));
            return UIKit.Label(rt, "", 24, c, TextAnchor.MiddleLeft);
        }

        GameObject Modal(Transform root, string title, Color color, out Text body)
        {
            var bg = UIKit.Panel(root, "Modal_" + title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 480), color);
            var inner = UIKit.Panel(bg.transform, "Inner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(600, 300), new Color(0, 0, 0, 0.3f));
            var titleRt = UIKit.Rect(bg.transform, "Title", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(560, 60));
            UIKit.Label(titleRt, title, 38, Color.white);
            body = UIKit.Label(inner.transform, "", 24, Color.white);
            var go = bg.gameObject;
            UIKit.Button(bg.transform, "Close", "X", new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, -10), new Vector2(56, 56),
                new Color(0.9f, 0.25f, 0.25f), () => go.SetActive(false), 28);
            go.SetActive(false);
            return go;
        }

        void Toggle(GameObject panel)
        {
            bool show = !panel.activeSelf;
            rebirthPanel.SetActive(false); dragonsPanel.SetActive(false); shopPanel.SetActive(false);
            panel.SetActive(show);
            RefreshPanels();
        }

        void ToggleSound()
        {
            SaveManager.Data.soundOn = !SaveManager.Data.soundOn;
            GameAudio.SetEnabled(SaveManager.Data.soundOn);
            UpdateSoundText();
        }

        void UpdateSoundText()
        {
            soundBtnText.text = Loc.T("sound") + ": " + (SaveManager.Data.soundOn ? (Loc.Ru ? "ВКЛ" : "ON") : (Loc.Ru ? "ВЫКЛ" : "OFF"));
        }

        public void Toast(string msg, Color color, float time = 2.6f)
        {
            if (toastText == null) return;
            toastText.text = msg;
            toastText.color = color;
            toastTimer = time;
            toastBg.gameObject.SetActive(true);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var d = SaveManager.Data;
            if (gm == null) return;

            coinsText.text = Loc.T("coins") + ": " + Loc.Num(d.coins);
            cpsText.text = "+" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? " /сек" : " /s");
            speedText.text = Loc.T("speed") + ": " + Mathf.RoundToInt(gm.WalkSpeed);
            rebirthText.text = Loc.T("rebirths") + ": " + d.rebirths;
            invText.text = d.inventory.Count > 0 ? Loc.F("inventory", d.inventory.Count) : "";

            if (toastTimer > 0)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0) toastBg.gameObject.SetActive(false);
            }

            var p = PlayerController.Instance;
            if (p != null)
            {
                bool carrying = p.Carrying != null;
                if (carryBg.gameObject.activeSelf != carrying) carryBg.gameObject.SetActive(carrying);
                if (carrying)
                {
                    carryText.text = Loc.F("carrying", Loc.TierName(p.Carrying.tier));
                    float pulse = 0.65f + Mathf.Sin(Time.time * 6f) * 0.2f;
                    var c = GameConfig.GetTier(p.Carrying.tier).color * 0.7f; c.a = pulse;
                    carryBg.color = c;
                }

                hintText.text = p.OnTreadmill ? Loc.T("treadmill_hint") : "";

                var it = p.Current;
                bool show = it != null;
                if (promptBg.gameObject.activeSelf != show) promptBg.gameObject.SetActive(show);
                if (!show) InputState.TouchAction = false;
                if (show)
                {
                    promptText.text = (InputState.Mobile ? "" : "[E]  ") + it.Prompt;
                    var rt = promptBg.rectTransform;
                    promptFill.rectTransform.offsetMax = new Vector2(rt.rect.width * Mathf.Clamp01(p.HoldProgress), 0);
                }
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0) { refreshTimer = 0.5f; RefreshPanels(); }
        }

        void RefreshPanels()
        {
            var gm = GameManager.Instance;
            var d = SaveManager.Data;
            if (gm == null) return;

            if (rebirthPanel.activeSelf)
            {
                float nextMult = GameConfig.RebirthMultiplier(d.rebirths + 1);
                rebirthBody.text = Loc.F("rebirth_desc", nextMult.ToString("0.#")) + "\n\n" + Loc.F("rebirth_cost", Loc.Num(gm.RebirthCost));
            }
            if (dragonsPanel.activeSelf)
            {
                var sb = new StringBuilder();
                sb.AppendLine(Loc.F("total_bonus", gm.DragonSpeedPct.ToString("0"), Loc.Num(gm.CoinsPerSec),
                    gm.DragonTrainPct.ToString("0"), gm.DragonJump.ToString("0.#")));
                sb.AppendLine();
                if (gm.DragonCount == 0) sb.Append(Loc.T("no_dragons"));
                for (int i = 0; i < d.plotsOwned; i++)
                {
                    var pl = d.plots[i];
                    if (pl.state != (int)PlotState.Dragon) continue;
                    var def = GameConfig.GetDragon(pl.dragonId);
                    sb.AppendLine(Loc.F("dragon_line", Loc.DragonName(def), Loc.TierName(def.tier), def.speedPct, Loc.Num(def.coinsPerSec), def.trainPct));
                }
                dragonsBody.text = sb.ToString();
            }
            if (shopPanel.activeSelf)
            {
                shopPlotBtnText.text = d.plotsOwned >= GameConfig.MaxPlots ? Loc.T("plots_max")
                    : Loc.F("buy_plot", Loc.Num(GameConfig.PlotCost(d.plotsOwned)));
            }
        }
    }
}
