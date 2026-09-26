using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Весь интерфейс строится кодом в стиле роблокс-симуляторов: главное меню, HUD с иконками,
    /// кнопки меню слева, слоты драконов снизу, подсказка действия над объектом, обучение,
    /// магазин улучшений, перерождение, драконы, настройки, продавец и рулетка открытия яиц.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        RectTransform root;
        Text coinsText, cpsText, speedText, rebirthText, toastText, carryText, invText, hintText, heldText;
        Image toastBg, carryBg;
        RectTransform promptRt;
        Image promptBg, promptFill;
        Text promptText, promptKey;
        float toastTimer;
        GameObject hud, menu;
        GameObject shopPanel, rebirthPanel, dragonsPanel, settingsPanel, sellPanel, yanPanel, dailyPanel;
        Text[] yanPrices; Text[] yanNames;
        Text[] dayTexts; Image[] dayCards; Button dailyClaim, dailyDouble;
        Image adBg; Text adText;
        Image[] dragCards; Text[] dragTexts; Text[] dragBtnTexts; Button[] dragBtns;
        Text dragTotals;
        readonly List<GameObject> panels = new List<GameObject>();
        Text rebirthBody, dragonsBody, plotBtnText;
        Text[] upgLevel, upgCost;
        Text soundText, musicText;
        Image[] switchBtns;
        Text[] sellRows; Button[] sellBtns; Text sellAllText; Text sellEmpty;
        // слоты
        Image[] slotBg; Text[] slotName; Image[] slotIcon;
        // обучение
        GameObject tutPanel; Text tutText;
        float refreshTimer;
        bool mobile;

        public bool AnyPanelOpen
        {
            get { foreach (var p in panels) if (p.activeSelf) return true; return menu != null && menu.activeSelf; }
        }

        public static UIManager Create()
        {
#if UNITY_2022_2_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() == null)
#else
            if (FindObjectOfType<EventSystem>() == null)
#endif
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

        static readonly Color Gold = new Color(1f, 0.84f, 0.2f);
        static readonly Color Cyan = new Color(0.35f, 0.95f, 1f);
        static readonly Color Pink = new Color(1f, 0.55f, 1f);

        void Build()
        {
            mobile = InputState.Mobile;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = mobile ? new Vector2(1150, 650) : new Vector2(1400, 790);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            root = (RectTransform)transform;

            hud = UIKit.Rect(root, "HUD", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UIKit.Stretch((RectTransform)hud.transform);
            hud.AddComponent<SafeArea>();
            var h = hud.transform;

            // ===== Статы слева сверху (пилюли с иконками) =====
            coinsText = StatPill(h, 0, Icons.Coin, Gold);
            cpsText = UIKit.Label(UIKit.Rect(h, "Cps", new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -66), new Vector2(260, 26)), "", 18, new Color(1f, 0.95f, 0.65f), TextAnchor.MiddleLeft);
            speedText = StatPill(h, 1, Icons.Bolt, Cyan);
            rebirthText = StatPill(h, 2, Icons.Star, Pink);
            invText = UIKit.Label(UIKit.Rect(h, "Inv", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 136 : 128), new Vector2(500, 26)), "", 17, new Color(1f, 0.85f, 0.4f));

            // ===== Кнопки меню: ПК — колонка слева, телефон — ряд справа сверху =====
            string[] names = { Loc.T("shop"), Loc.T("rebirth"), Loc.T("dragons"), Loc.Ru ? "Донат" : "Store", Loc.T("settings") };
            Sprite[] icons = { Icons.Bag, Icons.Star, Icons.Dragon, Icons.Coin, Icons.Gear };
            Color[] cols = { new Color(0.25f, 0.78f, 0.35f), new Color(0.78f, 0.32f, 0.95f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.78f, 0.1f), new Color(0.45f, 0.5f, 0.62f) };
            for (int i = 0; i < names.Length; i++)
            {
                int k = i;
                if (mobile)
                {
                    float bs = 72;
                    UIKit.Button(h, "Menu" + i, names[i], new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12 - (names.Length - 1 - i) * (bs + 8), -12), new Vector2(bs, bs),
                        cols[i], () => OnMenuButton(k), 13, icons[i]);
                }
                else
                {
                    float bs = 74;
                    float y = (2 - i) * (bs + 10) - 60;
                    UIKit.Button(h, "Menu" + i, names[i], new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, y), new Vector2(bs, bs),
                        cols[i], () => OnMenuButton(k), 14, icons[i]);
                }
            }
            // ежедневная награда — кнопка-подарок
            var dailyBtn = UIKit.Button(h, "DailyBtn", Loc.Ru ? "Награда" : "Daily", mobile ? new Vector2(1, 1) : new Vector2(1, 1), new Vector2(1, 1),
                mobile ? new Vector2(-12, -94) : new Vector2(-16, -16), new Vector2(mobile ? 72 : 80, mobile ? 72 : 80),
                new Color(0.95f, 0.3f, 0.45f), () => ShowOnly(dailyPanel), 13, Icons.Egg);
            Destroy(dailyBtn.GetComponent<ButtonBounce>());
            dailyBtn.gameObject.AddComponent<DailyBadge>();

            // ===== Слоты драконов снизу =====
            BuildHotbar(h);
            heldText = UIKit.Label(UIKit.Rect(h, "Held", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 112 : 104), new Vector2(700, 30)), "", 18, new Color(0.7f, 1f, 0.85f));

            // ===== Обучение (сверху по центру) =====
            float tw = mobile ? 470 : 720, tx = mobile ? -70 : 0;
            var tp = UIKit.Panel(h, "Tutorial", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -12), new Vector2(tw, 104), new Color(0.12f, 0.1f, 0.25f, 0.88f), 3f);
            tutPanel = tp.gameObject;
            var tTitle = UIKit.Label(UIKit.Rect(tp.transform, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(700, 26)), Loc.T("tut_title"), 17, Gold);
            tTitle.alignment = TextAnchor.MiddleCenter;
            tutText = UIKit.Label(UIKit.Rect(tp.transform, "B", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-50, 6), new Vector2(tw - 130, 72)), "", mobile ? 17 : 21, Color.white);
            UIKit.Button(tp.transform, "Skip", Loc.T("skip"), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, -8), new Vector2(100, 38),
                new Color(0.5f, 0.5f, 0.6f), () => { if (Tutorial.Instance != null) Tutorial.Instance.Skip(); }, 16);
            tutPanel.SetActive(false);

            // ===== Тост и баннер переноски =====
            toastBg = UIKit.Panel(h, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -126), new Vector2(mobile ? 470 : 680, 54), new Color(0.05f, 0.05f, 0.1f, 0.75f), 2f);
            toastText = UIKit.Label(toastBg.transform, "", mobile ? 19 : 23, Color.white);
            toastBg.raycastTarget = false;
            toastBg.gameObject.SetActive(false);

            carryBg = UIKit.Panel(h, "Carry", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -186), new Vector2(mobile ? 470 : 560, 48), new Color(0.9f, 0.2f, 0.2f, 0.85f), 3f);
            carryText = UIKit.Label(carryBg.transform, "", 21, Color.white);
            carryBg.raycastTarget = false;
            carryBg.gameObject.SetActive(false);

            hintText = UIKit.Label(UIKit.Rect(h, "Hint", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 250 : 190), new Vector2(760, 40)), "", 22, new Color(1f, 1f, 0.7f));

            BuildPrompt(h);

            adBg = UIKit.Panel(h, "AdCountdown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(420, 70), new Color(0.1f, 0.1f, 0.18f, 0.9f), 3f);
            adText = UIKit.Label(adBg.transform, "", 26, Gold);
            adBg.raycastTarget = false;
            adBg.gameObject.SetActive(false);

            // ===== Панели =====
            shopPanel = BuildShop();
            rebirthPanel = BuildRebirth();
            dragonsPanel = BuildDragons();
            settingsPanel = BuildSettings();
            sellPanel = BuildSell();
            yanPanel = BuildYanShop();
            dailyPanel = BuildDaily();

            if (mobile) hud.AddComponent<MobileControls>().Build(h);

            BuildMenu();
            gameObject.AddComponent<ResponsiveCanvas>().Init(scaler, mobile, root);
        }

        Text StatPill(Transform parent, int line, Sprite icon, Color color)
        {
            float y = -14 - line * 66 - (line > 0 ? 22 : 0);
            var bg = UIKit.Panel(parent, "Stat" + line, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(270, 52), new Color(0.08f, 0.08f, 0.14f, 0.78f), 3f);
            var ic = UIKit.Icon(bg.transform, icon, new Vector2(0, 0.5f), new Vector2(26, 0), 58);
            ic.rectTransform.anchoredPosition = new Vector2(24, 2);
            var t = UIKit.Label(UIKit.Rect(bg.transform, "V", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(206, 48)), "", 28, color, TextAnchor.MiddleLeft);
            return t;
        }

        void BuildHotbar(Transform h)
        {
            int n = GameConfig.InventorySlots;
            float s = mobile ? 78 : 74, gap = 10;
            float total = n * s + (n - 1) * gap;
            slotBg = new Image[n]; slotName = new Text[n]; slotIcon = new Image[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                float x = -total / 2f + s / 2f + i * (s + gap);
                var b = UIKit.Button(h, "Slot" + i, "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 16), new Vector2(s, s),
                    new Color(0.15f, 0.15f, 0.22f, 0.85f), () => { if (GameManager.Instance != null) GameManager.Instance.SelectSlot(k); });
                Destroy(b.GetComponent<ButtonBounce>());
                slotBg[i] = b.GetComponent<Image>();
                slotIcon[i] = UIKit.Icon(b.transform, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(0, 8), s * 0.55f);
                slotName[i] = UIKit.Label(b.transform, "", 12, Color.white, TextAnchor.LowerCenter);
                var num = UIKit.Label(UIKit.Rect(b.transform, "N", new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -2), new Vector2(24, 24)), (i + 1).ToString(), 16, Gold, TextAnchor.UpperLeft);
                num.raycastTarget = false;
            }
        }

        void BuildPrompt(Transform h)
        {
            promptBg = UIKit.Panel(h, "Prompt", new Vector2(0, 0), new Vector2(0.5f, 0.5f), Vector2.zero, mobile ? new Vector2(300, 84) : new Vector2(330, 70),
                new Color(0.06f, 0.06f, 0.1f, 0.85f), 3f);
            promptRt = promptBg.rectTransform;
            var fill = UIKit.Panel(promptBg.transform, "Fill", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.3f, 0.9f, 0.4f, 0.55f));
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            fill.raycastTarget = false;
            promptFill = fill;
            // клавиша "E" как в ProximityPrompt
            var key = UIKit.Panel(promptBg.transform, "Key", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(50, 50), Color.white, 2f);
            key.raycastTarget = false;
            promptKey = UIKit.Label(key.transform, mobile ? "!" : "E", 28, new Color(0.1f, 0.1f, 0.15f));
            var po = promptKey.GetComponents<Outline>();
            foreach (var o in po) o.enabled = false;
            promptText = UIKit.Label(UIKit.Rect(promptBg.transform, "T", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(mobile ? 228 : 258, 64)), "", mobile ? 21 : 20, Color.white, TextAnchor.MiddleLeft);
            if (mobile)
                UIKit.AddPointer(promptBg.gameObject, e => InputState.TouchAction = true, e => InputState.TouchAction = false);
            else promptBg.raycastTarget = false;
            promptBg.gameObject.SetActive(false);
        }

        // ============================ ПАНЕЛИ ============================
        GameObject Modal(string title, Color color, Vector2 size, out RectTransform body)
        {
            var bg = UIKit.Panel(root, "Modal_" + title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), size, new Color(0.13f, 0.12f, 0.2f, 0.97f), 4f);
            var header = UIKit.Panel(bg.transform, "Header", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(size.x * 0.62f, 64), color, 4f);
            UIKit.Label(header.transform, title, 34, Color.white);
            var go = bg.gameObject;
            UIKit.Button(bg.transform, "Close", "X", new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(58, 58),
                new Color(0.92f, 0.25f, 0.28f), () => go.SetActive(false), 30);
            body = UIKit.Rect(bg.transform, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(size.x - 40, size.y - 90));
            go.AddComponent<PopIn>();
            go.SetActive(false);
            panels.Add(go);
            return go;
        }

        GameObject BuildShop()
        {
            RectTransform body;
            var go = Modal(Loc.T("shop"), new Color(0.25f, 0.75f, 0.35f), new Vector2(760, 560), out body);
            int n = GameConfig.Upgrades.Length;
            upgLevel = new Text[n]; upgCost = new Text[n];
            float rowH = 72;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var u = GameConfig.Upgrades[i];
                var row = UIKit.Panel(body, "Upg" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -i * (rowH + 8)), new Vector2(700, rowH), new Color(0.2f, 0.19f, 0.3f, 1f), 2f);
                var dot = UIKit.Panel(row.transform, "C", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(12, rowH - 16), u.color);
                dot.raycastTarget = false;
                UIKit.Label(UIKit.Rect(row.transform, "N", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -4), new Vector2(360, 34)), Loc.Ru ? u.nameRu : u.nameEn, 24, Color.white, TextAnchor.MiddleLeft);
                UIKit.Label(UIKit.Rect(row.transform, "D", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 4), new Vector2(360, 30)), Loc.Ru ? u.descRu : u.descEn, 16, new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleLeft);
                upgLevel[i] = UIKit.Label(UIKit.Rect(row.transform, "L", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(390, 0), new Vector2(110, 40)), "", 20, Gold);
                var b = UIKit.Button(row.transform, "Buy", "", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(180, 54),
                    new Color(0.25f, 0.78f, 0.35f), () => { if (GameManager.Instance.TryBuyUpgrade(k)) RefreshPanels(); }, 20);
                upgCost[i] = b.GetComponentInChildren<Text>();
            }
            var pb = UIKit.Button(body, "BuyPlot", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(420, 60),
                new Color(1f, 0.6f, 0.15f), () => { GameManager.Instance.TryBuyPlot(); RefreshPanels(); }, 22);
            plotBtnText = pb.GetComponentInChildren<Text>();
            return go;
        }

        GameObject BuildRebirth()
        {
            RectTransform body;
            var go = Modal(Loc.T("rebirth"), new Color(0.78f, 0.32f, 0.95f), new Vector2(620, 480), out body);
            UIKit.Icon(body, Icons.Star, new Vector2(0.5f, 1), new Vector2(0, -44), 84);
            rebirthBody = UIKit.Label(UIKit.Rect(body, "T", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(560, 220)), "", 22, Color.white);
            UIKit.Button(body, "Do", Loc.T("rebirth"), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(300, 66),
                new Color(0.78f, 0.32f, 0.95f), () => { if (GameManager.Instance.TryRebirth()) rebirthPanel.SetActive(false); }, 28);
            return go;
        }

        GameObject BuildDragons()
        {
            RectTransform body;
            var go = Modal(Loc.T("dragons"), new Color(1f, 0.55f, 0.15f), new Vector2(900, 620), out body);
            dragTotals = UIKit.Label(UIKit.Rect(body, "Tot", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(860, 34)), "", 17, Gold);
            int n = GameConfig.MaxPlots;
            dragCards = new Image[n]; dragTexts = new Text[n]; dragBtnTexts = new Text[n]; dragBtns = new Button[n];
            float cw = 280, ch = 118;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                int col = i % 3, row = i / 3;
                var card = UIKit.Panel(body, "D" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - 1) * (cw + 10), -40 - row * (ch + 8)), new Vector2(cw, ch), new Color(0.2f, 0.19f, 0.3f), 2f);
                dragCards[i] = card;
                dragTexts[i] = UIKit.Label(UIKit.Rect(card.transform, "T", new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -4), new Vector2(cw - 16, 70)), "", 15, Color.white, TextAnchor.UpperLeft);
                dragBtns[i] = UIKit.Button(card.transform, "Up", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(cw - 20, 38),
                    new Color(0.25f, 0.78f, 0.35f), () => { if (GameManager.Instance.TryUpgradeDragon(k)) RefreshPanels(); }, 16);
                dragBtnTexts[i] = dragBtns[i].GetComponentInChildren<Text>();
            }
            return go;
        }

        GameObject BuildYanShop()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "Магазин" : "Store", new Color(1f, 0.72f, 0.1f), new Vector2(900, 600), out body);
            int n = GameConfig.Products.Length;
            yanPrices = new Text[n]; yanNames = new Text[n];
            float cw = 205, ch = 230;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var pd = GameConfig.Products[i];
                int col = i % 4, row = i / 4;
                var card = UIKit.Panel(body, "P" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - 1.5f) * (cw + 12), -row * (ch + 12)), new Vector2(cw, ch),
                    new Color(pd.color.r * 0.45f, pd.color.g * 0.45f, pd.color.b * 0.45f, 1f), 3f);
                var icon = pd.kind == ProductKind.Coins ? Icons.Coin : pd.kind == ProductKind.Speed ? Icons.Bolt : pd.kind == ProductKind.Egg ? Icons.Egg : Icons.Star;
                UIKit.Icon(card.transform, icon, new Vector2(0.5f, 1), new Vector2(0, -50), 76);
                yanNames[i] = UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -98), new Vector2(cw - 10, 30)), Loc.Ru ? pd.nameRu : pd.nameEn, 18, Color.white);
                UIKit.Label(UIKit.Rect(card.transform, "D", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(cw - 12, 44)), Loc.Ru ? pd.descRu : pd.descEn, 14, new Color(0.9f, 0.9f, 1f));
                var b = UIKit.Button(card.transform, "Buy", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(cw - 24, 46),
                    new Color(0.3f, 0.8f, 0.35f), () => YandexSDK.Purchase(GameConfig.Products[k].id), 19);
                yanPrices[i] = b.GetComponentInChildren<Text>();
            }
            return go;
        }

        GameObject BuildDaily()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "Ежедневная награда" : "Daily reward", new Color(0.95f, 0.3f, 0.45f), new Vector2(900, 470), out body);
            int n = GameConfig.DailyDays;
            dayTexts = new Text[n]; dayCards = new Image[n];
            float cw = 116;
            for (int i = 0; i < n; i++)
            {
                var card = UIKit.Panel(body, "Day" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - 3) * (cw + 6), -6), new Vector2(cw, 220), new Color(0.2f, 0.19f, 0.3f), 2f);
                dayCards[i] = card;
                UIKit.Label(UIKit.Rect(card.transform, "H", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(cw, 30)), (Loc.Ru ? "День " : "Day ") + (i + 1), 17, Gold);
                UIKit.Icon(card.transform, i == 3 || i == 6 ? Icons.Egg : (i == 1 || i == 5 ? Icons.Bolt : Icons.Coin), new Vector2(0.5f, 1), new Vector2(0, -68), 56);
                dayTexts[i] = UIKit.Label(UIKit.Rect(card.transform, "T", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(cw - 6, 110)), "", 13, Color.white);
            }
            dailyClaim = UIKit.Button(body, "Claim", Loc.Ru ? "Забрать" : "Claim", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-150, 0), new Vector2(260, 64),
                new Color(0.3f, 0.8f, 0.35f), () => { GameManager.Instance.ClaimDaily(false); RefreshPanels(); }, 26);
            dailyDouble = UIKit.Button(body, "Double", Loc.Ru ? "x2 за рекламу" : "x2 for ad", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(150, 0), new Vector2(260, 64),
                new Color(1f, 0.6f, 0.15f), () => YandexSDK.ShowRewarded(ok =>
                {
                    if (ok) GameManager.Instance.ClaimDaily(true);
                    else Toast(Loc.T("ad_fail"), new Color(1f, 0.6f, 0.4f));
                    RefreshPanels();
                }), 24);
            return go;
        }

        public void ShowAdCountdown(int n)
        {
            if (adBg == null) return;
            bool show = n > 0;
            if (adBg.gameObject.activeSelf != show) adBg.gameObject.SetActive(show);
            if (show) adText.text = (Loc.Ru ? "Реклама через " : "Ad in ") + n + "...";
        }

        /// <summary>После "Играть": мини-обучение управлению (один раз), потом ежедневная награда.</summary>
        void AfterPlay()
        {
            var gm = GameManager.Instance;
            if (!SaveManager.Data.controlsSeen)
            {
                ControlsGuide.Show(root, mobile, () =>
                {
                    SaveManager.Data.controlsSeen = true;
                    if (gm != null) gm.SaveNow(false);
                    if (gm != null && gm.DailyAvailable) ShowOnly(dailyPanel);
                });
            }
            else if (gm != null && gm.DailyAvailable) ShowOnly(dailyPanel);
        }

        GameObject BuildSettings()
        {
            RectTransform body;
            var go = Modal(Loc.T("settings"), new Color(0.45f, 0.5f, 0.62f), new Vector2(600, 470), out body);
            var sb = UIKit.Button(body, "Sound", "", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-135, -10), new Vector2(250, 60), new Color(0.3f, 0.55f, 0.95f), ToggleSound, 22);
            soundText = sb.GetComponentInChildren<Text>();
            var mb = UIKit.Button(body, "Music", "", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(135, -10), new Vector2(250, 60), new Color(0.3f, 0.55f, 0.95f), ToggleMusic, 22);
            musicText = mb.GetComponentInChildren<Text>();
            UIKit.Label(UIKit.Rect(body, "SwT", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(540, 36)), Loc.T("switch_sound"), 22, Gold);
            switchBtns = new Image[3];
            Color[] swc = { new Color(0.25f, 0.45f, 1f), new Color(0.6f, 0.4f, 0.22f), new Color(0.9f, 0.25f, 0.25f) };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var b = UIKit.Button(body, "Sw" + i, Loc.T("sw_" + i), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150 - i * 68), new Vector2(420, 58), swc[i], () =>
                {
                    SaveManager.Data.switchType = k;
                    GameAudio.PlaySwitch(k, true);
                    RefreshPanels();
                }, 21);
                switchBtns[i] = b.GetComponent<Image>();
            }
            return go;
        }

        GameObject BuildSell()
        {
            RectTransform body;
            var go = Modal(Loc.T("sell_title"), new Color(1f, 0.75f, 0.2f), new Vector2(680, 540), out body);
            int n = GameConfig.InventorySlots;
            sellRows = new Text[n]; sellBtns = new Button[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var row = UIKit.Panel(body, "Row" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -i * 70), new Vector2(620, 62), new Color(0.2f, 0.19f, 0.3f), 2f);
                sellRows[i] = UIKit.Label(UIKit.Rect(row.transform, "T", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(390, 56)), "", 20, Color.white, TextAnchor.MiddleLeft);
                sellBtns[i] = UIKit.Button(row.transform, "S", "", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(200, 50),
                    new Color(1f, 0.7f, 0.15f), () => { GameManager.Instance.SellSlot(k); RefreshPanels(); }, 19);
            }
            var all = UIKit.Button(body, "All", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(380, 60),
                new Color(0.95f, 0.35f, 0.25f), () => { GameManager.Instance.SellAll(); RefreshPanels(); }, 22);
            sellAllText = all.GetComponentInChildren<Text>();
            sellEmpty = UIKit.Label(UIKit.Rect(body, "E", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 120)), Loc.T("sell_empty"), 22, new Color(0.85f, 0.85f, 0.95f));
            return go;
        }

        public void OpenSell() { ShowOnly(sellPanel); }

        void OnMenuButton(int i)
        {
            var p = i == 0 ? shopPanel : i == 1 ? rebirthPanel : i == 2 ? dragonsPanel : i == 3 ? yanPanel : settingsPanel;
            if (p.activeSelf) p.SetActive(false); else ShowOnly(p);
        }

        void ShowOnly(GameObject p)
        {
            foreach (var x in panels) x.SetActive(false);
            p.SetActive(true);
            RefreshPanels();
        }

        void ToggleSound()
        {
            SaveManager.Data.soundOn = !SaveManager.Data.soundOn;
            GameAudio.SetEnabled(SaveManager.Data.soundOn);
            RefreshPanels();
        }

        void ToggleMusic()
        {
            SaveManager.Data.musicOn = !SaveManager.Data.musicOn;
            GameAudio.SetMusic(SaveManager.Data.musicOn);
            RefreshPanels();
        }

        // ============================ ГЛАВНОЕ МЕНЮ ============================
        void BuildMenu()
        {
            var bg = UIKit.Rect(root, "Menu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(bg);
            var dim = bg.gameObject.AddComponent<Image>();
            dim.color = new Color(0.05f, 0.1f, 0.25f, 0.35f);
            menu = bg.gameObject;

            var title = UIKit.Label(UIKit.Rect(bg, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1100, 140)), Loc.T("title"), 96, Gold);
            foreach (var o in title.GetComponents<Outline>()) o.effectDistance *= 2.2f;
            title.gameObject.AddComponent<TitleWobble>();
            UIKit.Label(UIKit.Rect(bg, "Sub", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 50)), Loc.T("subtitle"), 28, Color.white);
            var eggIcon = UIKit.Icon(bg, Icons.Egg, new Vector2(0.5f, 0.5f), new Vector2(-420, 170), 110);
            eggIcon.gameObject.AddComponent<TitleWobble>();
            var dragIcon = UIKit.Icon(bg, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(420, 170), 120);
            dragIcon.gameObject.AddComponent<TitleWobble>();

            var play = UIKit.Button(bg, "Play", Loc.T("play"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(380, 110),
                new Color(0.3f, 0.85f, 0.35f), OnPlay, 52);
            Destroy(play.GetComponent<ButtonBounce>());
            play.gameObject.AddComponent<PulseScale>();
            UIKit.Button(bg, "Set", Loc.T("settings"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(260, 64),
                new Color(0.45f, 0.5f, 0.62f), () => ShowOnly(settingsPanel), 24, null);
            menu.transform.SetAsLastSibling();
            foreach (var p in panels) p.transform.SetAsLastSibling();

            hud.SetActive(false);
            InputState.Blocked = true;
            var rig = CameraRig.Cam != null ? CameraRig.Cam.GetComponent<CameraRig>() : null;
            if (rig != null) rig.MenuOrbit = true;
        }

        public void SetMenuCamera()
        {
            var rig = CameraRig.Cam != null ? CameraRig.Cam.GetComponent<CameraRig>() : null;
            if (rig != null) rig.MenuOrbit = menu != null && menu.activeSelf;
        }

        void OnPlay()
        {
            menu.SetActive(false);
            settingsPanel.SetActive(false);
            hud.SetActive(true);
            InputState.Blocked = false;
            var rig = CameraRig.Cam != null ? CameraRig.Cam.GetComponent<CameraRig>() : null;
            if (rig != null) { rig.MenuOrbit = false; rig.yaw = 0; rig.pitch = 20; }
            GameAudio.Play(Sfx.Success);
            YandexSDK.GameplayStart();
            Toast(Loc.T("hint_start"), Color.white, 5f);
            AfterPlay();
        }

        // ============================ РУЛЕТКА ============================
        public void ShowRoulette(Tier eggTier, DragonDef result, System.Action onDone)
        {
            var go = new GameObject("Roulette", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.AddComponent<Roulette>().Init((RectTransform)go.transform, eggTier, result, onDone);
        }

        // ============================ ОБНОВЛЕНИЕ ============================
        public void Toast(string msg, Color color, float time = 2.6f)
        {
            if (toastText == null) return;
            toastText.text = msg;
            toastText.color = color;
            toastTimer = time;
            toastBg.gameObject.SetActive(true);
            toastBg.transform.localScale = Vector3.one * 1.1f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var d = SaveManager.Data;
            if (gm == null) return;
            InputState.Blocked = (menu != null && menu.activeSelf) || Roulette.Active;

            coinsText.text = Loc.Num(d.coins);
            cpsText.text = "+" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? " /сек" : " /s");
            speedText.text = Loc.Num(d.speedPoints);
            rebirthText.text = d.rebirths.ToString() + "  <size=18>x" + Loc.Num(gm.CoinMultiplier) + "</size>";
            invText.text = d.inventory.Count > 0 ? Loc.F("inventory", d.inventory.Count) : "";

            if (toastTimer > 0)
            {
                toastTimer -= Time.unscaledDeltaTime;
                toastBg.transform.localScale = Vector3.one * Mathf.MoveTowards(toastBg.transform.localScale.x, 1f, Time.unscaledDeltaTime);
                if (toastTimer <= 0) toastBg.gameObject.SetActive(false);
            }

            // обучение
            var tut = Tutorial.Instance;
            bool showTut = tut != null && tut.Active && hud.activeSelf;
            if (tutPanel.activeSelf != showTut) tutPanel.SetActive(showTut);
            if (showTut) tutText.text = tut.Text;

            UpdateHotbar(gm, d);

            var p = PlayerController.Instance;
            if (p != null)
            {
                bool carrying = p.Carrying != null;
                if (carryBg.gameObject.activeSelf != carrying) carryBg.gameObject.SetActive(carrying);
                if (carrying)
                {
                    carryText.text = Loc.F("carrying", Loc.TierName(p.Carrying.tier));
                    float pulse = 0.7f + Mathf.Sin(Time.time * 6f) * 0.2f;
                    var c = GameConfig.GetTier(p.Carrying.tier).color * 0.75f; c.a = pulse;
                    carryBg.color = c;
                }
                hintText.text = p.OnTreadmill && !(p.CurrentTreadmill != null && p.CurrentTreadmill.Training) ? Loc.T("treadmill_hint") : "";
                UpdatePrompt(p);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0) { refreshTimer = 0.4f; RefreshPanels(); }
        }

        void UpdateHotbar(GameManager gm, SaveData d)
        {
            for (int i = 0; i < slotBg.Length; i++)
            {
                int id = d.dragonInv[i];
                bool sel = d.selectedSlot == i;
                if (id < 0)
                {
                    slotName[i].text = "";
                    slotIcon[i].enabled = false;
                    slotBg[i].color = new Color(0.15f, 0.15f, 0.22f, 0.8f);
                }
                else
                {
                    var def = GameConfig.GetDragon(id);
                    var tc = GameConfig.GetTier(def.tier).color;
                    slotName[i].text = Loc.DragonName(def);
                    slotIcon[i].enabled = true;
                    slotIcon[i].color = Color.Lerp(def.body, Color.white, 0.2f);
                    slotBg[i].color = new Color(tc.r * 0.55f, tc.g * 0.55f, tc.b * 0.55f, 0.95f);
                }
                float target = sel ? 1.12f : 1f;
                var t = slotBg[i].transform;
                if (Mathf.Abs(t.localScale.x - target) > 0.001f)
                    t.localScale = Vector3.one * Mathf.MoveTowards(t.localScale.x, target, Time.unscaledDeltaTime * 2f);
                var outl = slotBg[i].GetComponents<Outline>();
                foreach (var o in outl) o.effectColor = sel ? Gold : UIKit.Stroke;
            }
            int held = gm.HeldDragonId;
            if (held >= 0)
            {
                var hd = GameConfig.GetDragon(held);
                heldText.text = Loc.F("held", Loc.DragonName(hd), hd.speedPct.ToString("0"));
            }
            else heldText.text = "";
        }

        void UpdatePrompt(PlayerController p)
        {
            var it = p.Current;
            bool show = it != null && hud.activeSelf && !AnyPanelOpen && !Roulette.Active;
            if (promptBg.gameObject.activeSelf != show) promptBg.gameObject.SetActive(show);
            if (!show) { InputState.TouchAction = false; return; }
            promptText.text = it.Prompt;
            promptFill.rectTransform.offsetMax = new Vector2(promptRt.rect.width * Mathf.Clamp01(p.HoldProgress), 0);

            // позиция: над объектом (как ProximityPrompt), на мобилке — внизу справа над прыжком
            if (mobile)
            {
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(1, 0);
                promptRt.anchoredPosition = new Vector2(-190, 210);
            }
            else
            {
                var cam = CameraRig.Cam;
                Vector3 sp = cam != null ? cam.WorldToScreenPoint(it.InteractPos + Vector3.up * 3.2f) : new Vector3(Screen.width / 2f, 120f, 1f);
                if (sp.z < 0) sp = new Vector3(Screen.width / 2f, Screen.height * 0.25f, 1f);
                float scale = root.lossyScale.x > 0 ? root.lossyScale.x : 1f;
                promptRt.anchorMin = promptRt.anchorMax = Vector2.zero;
                Vector2 pos = new Vector2(sp.x / scale, sp.y / scale);
                Vector2 size = root.rect.size;
                pos.x = Mathf.Clamp(pos.x, 180, size.x - 180);
                pos.y = Mathf.Clamp(pos.y, 130, size.y - 140);
                promptRt.anchoredPosition = pos;
            }
        }

        void RefreshPanels()
        {
            var gm = GameManager.Instance;
            var d = SaveManager.Data;
            if (gm == null) return;

            if (shopPanel.activeSelf)
            {
                for (int i = 0; i < upgLevel.Length; i++)
                {
                    var u = GameConfig.Upgrades[i];
                    int lvl = gm.UpgLevel(i);
                    upgLevel[i].text = (Loc.Ru ? "Ур. " : "Lv. ") + lvl + "/" + u.maxLevel;
                    upgCost[i].text = lvl >= u.maxLevel ? "MAX" : Loc.Num(GameConfig.UpgradeCost(i, lvl));
                }
                plotBtnText.text = d.plotsOwned >= GameConfig.MaxPlots ? Loc.T("plots_max") : Loc.F("buy_plot", Loc.Num(GameConfig.PlotCost(d.plotsOwned)));
            }
            if (rebirthPanel.activeSelf)
            {
                float next = GameConfig.RebirthMultiplier(d.rebirths + 1);
                rebirthBody.text = Loc.F("rebirth_desc", next.ToString("0")) + "\n\n<color=#FFD84A>" + Loc.F("rebirth_cost", Loc.Num(gm.RebirthCost)) + "</color>";
            }
            if (dragonsPanel.activeSelf)
            {
                dragTotals.text = Loc.F("total_bonus", gm.DragonSpeedPct.ToString("0"), Loc.Num(gm.CoinsPerSec),
                    gm.DragonTrainPct.ToString("0"), gm.DragonJump.ToString("0.#")).Replace("\n", "   ");
                for (int i = 0; i < dragCards.Length; i++)
                {
                    bool owned = i < d.plotsOwned && d.plots[i].state == (int)PlotState.Dragon;
                    dragCards[i].gameObject.SetActive(owned);
                    if (!owned) continue;
                    var pl = d.plots[i];
                    var def = GameConfig.GetDragon(pl.dragonId);
                    var tc = GameConfig.GetTier(def.tier).color;
                    dragCards[i].color = new Color(tc.r * 0.35f, tc.g * 0.35f, tc.b * 0.35f, 1f);
                    float ls = GameConfig.DragonLevelStat(pl.level);
                    dragTexts[i].text = "<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(tc, Color.white, 0.3f)) + ">" + Loc.DragonName(def) + "</color>  <color=#FFD84A>"
                        + (Loc.Ru ? "Ур." : "Lv.") + pl.level + "</color>\n"
                        + Loc.Num(def.coinsPerSec * GameConfig.DragonLevelIncome(pl.level)) + (Loc.Ru ? "/сек  +" : "/s  +") + (def.speedPct * ls).ToString("0") + (Loc.Ru ? "% скор.\n+" : "% spd\n+")
                        + (def.trainPct * ls).ToString("0") + (Loc.Ru ? "% прокачки" : "% training");
                    bool max = pl.level >= GameConfig.DragonMaxLevel;
                    dragBtnTexts[i].text = max ? "MAX" : (Loc.Ru ? "Улучшить " : "Upgrade ") + Loc.Num(gm.DragonUpgradeCost(i));
                    dragBtns[i].interactable = !max;
                }
                if (gm.DragonCount == 0) dragTotals.text = Loc.T("no_dragons").Replace("\n", " ");
            }
            if (yanPanel.activeSelf)
            {
                for (int i = 0; i < yanPrices.Length; i++)
                {
                    var pd = GameConfig.Products[i];
                    bool owned = pd.kind == ProductKind.Permanent && gm.Owns(pd.id);
                    yanPrices[i].text = owned ? (Loc.Ru ? "Куплено" : "Owned") : YandexSDK.PriceText(pd);
                    yanPrices[i].transform.parent.GetComponent<Button>().interactable = !owned;
                }
            }
            if (dailyPanel.activeSelf)
            {
                int next = gm.DailyNextDay;
                bool avail = gm.DailyAvailable;
                int claimedUpTo = avail ? next - 1 : d.dailyStreak;
                for (int i = 0; i < dayTexts.Length; i++)
                {
                    int day = i + 1;
                    dayTexts[i].text = gm.DailyText(day);
                    bool isNext = avail && day == next;
                    bool done = day <= claimedUpTo;
                    dayCards[i].color = isNext ? new Color(0.35f, 0.7f, 0.35f) : done ? new Color(0.25f, 0.25f, 0.3f) : new Color(0.2f, 0.19f, 0.3f);
                    foreach (var o in dayCards[i].GetComponents<Outline>()) o.effectColor = isNext ? Gold : UIKit.Stroke;
                }
                dailyClaim.interactable = avail;
                dailyDouble.interactable = avail;
                dailyClaim.GetComponentInChildren<Text>().text = avail ? (Loc.Ru ? "Забрать" : "Claim") : (Loc.Ru ? "Завтра!" : "Tomorrow!");
            }
            if (settingsPanel.activeSelf)
            {
                soundText.text = Loc.T("sound") + ": " + (d.soundOn ? Loc.T("on") : Loc.T("off"));
                musicText.text = Loc.T("music") + ": " + (d.musicOn ? Loc.T("on") : Loc.T("off"));
                for (int i = 0; i < 3; i++)
                    foreach (var o in switchBtns[i].GetComponents<Outline>()) o.effectColor = d.switchType == i ? Gold : UIKit.Stroke;
            }
            if (sellPanel.activeSelf)
            {
                int count = 0; double total = 0;
                for (int i = 0; i < sellRows.Length; i++)
                {
                    int id = d.dragonInv[i];
                    bool has = id >= 0;
                    sellRows[i].transform.parent.gameObject.SetActive(has);
                    if (!has) continue;
                    count++;
                    var def = GameConfig.GetDragon(id);
                    double price = gm.SlotSellPrice(i);
                    total += price;
                    string hex = ColorUtility.ToHtmlStringRGB(GameConfig.GetTier(def.tier).color);
                    sellRows[i].text = "<color=#" + hex + ">" + Loc.DragonName(def) + "</color>\n<size=15>" + Loc.TierName(def.tier) + "</size>";
                    sellBtns[i].GetComponentInChildren<Text>().text = Loc.F("sell", Loc.Num(price));
                }
                sellAllText.text = Loc.F("sell_all", Loc.Num(total));
                sellAllText.transform.parent.gameObject.SetActive(count > 0);
                sellEmpty.gameObject.SetActive(count == 0);
            }
        }
    }

    /// <summary>Заголовок меню покачивается.</summary>
    public class TitleWobble : MonoBehaviour
    {
        float seed;
        void Start() { seed = Random.value * 5f; }
        void Update()
        {
            float t = Time.unscaledTime + seed;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f) * 3f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.2f) * 0.03f);
        }
    }

    public class PulseScale : MonoBehaviour
    {
        void Update() { transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f); }
    }
}
