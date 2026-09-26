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
        GameObject trailsPanel, eggsPanel;
        Text[] trailCoinBtn, trailYanBtn; Button[] trailYanButtons; Image[] trailCards;
        Text eggsBody; Text[] equipTexts; Button[] equipBtns; Button[] unequipBtns; Text[] unequipTexts;
        readonly Dictionary<string, Text> yanPriceById = new Dictionary<string, Text>();
        readonly Dictionary<string, Button> yanBtnById = new Dictionary<string, Button>();
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

            // ===== HUD в стиле роблокс-режимов =====
            var brown = new Color(0.32f, 0.23f, 0.18f, 0.95f);
            float cb = mobile ? 76 : 96;
            // круглые кнопки слева сверху: улучшения (сумка), перерождение (книга)
            UIKit.CircleButton(h, "BtnShop", Icons.Bag, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -12), cb, brown, () => OnMenuButton(0));
            var rb = UIKit.CircleButton(h, "BtnTrails", Icons.Shoe, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26 + cb, -12), cb, brown, () => OpenTrails());
            rebirthText = UIKit.Label(UIKit.Rect(rb.transform, "R", new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(cb + 40, 26)), Loc.Ru ? "Трейлы" : "Trails", 17, Color.white);
            // зелёная "Магазин NEW!" (донат за Яны)
            float shopW = mobile ? 170 : 216, shopH = mobile ? 70 : 92;
            Vector2 shopPos = mobile ? new Vector2(36 + cb * 2, -12) : new Vector2(12, -12 - cb - 44);
            var yb = UIKit.Button(h, "BtnYan", Loc.Ru ? "Магазин" : "Store", new Vector2(0, 1), new Vector2(0, 1), shopPos, new Vector2(shopW, shopH),
                new Color(0.2f, 0.85f, 0.25f), () => OnMenuButton(3), mobile ? 30 : 40);
            var tag = UIKit.Panel(yb.transform, "New", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(84, 26), new Color(1f, 0.85f, 0.2f), 2f);
            tag.raycastTarget = false;
            var nt = UIKit.Label(tag.transform, "NEW!", 17, Color.white);
            nt.rectTransform.offsetMin = Vector2.zero; nt.rectTransform.offsetMax = Vector2.zero;
            // шестерёнка справа сверху
            UIKit.CircleButton(h, "BtnSettings", Icons.Gear, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -12), cb, brown, () => OnMenuButton(4));
            // красные квадраты справа: ежедневная награда (яйцо), драконы (лапа)
            float sq = mobile ? 78 : 92;
            var red = new Color(0.9f, 0.2f, 0.2f);
            UIKit.Button(h, "BtnEggs", "", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, mobile ? 120 : 60), new Vector2(sq, sq), red, () => ShowOnly(eggsPanel), 14, Icons.Egg);
            var dailyBtn = UIKit.Button(h, "BtnDaily", Loc.Ru ? "Награда" : "Daily", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -24 - cb), new Vector2(cb, cb * 0.8f),
                new Color(0.95f, 0.3f, 0.45f), () => ShowOnly(dailyPanel), 13, Icons.Star);
            Destroy(dailyBtn.GetComponent<ButtonBounce>());
            dailyBtn.gameObject.AddComponent<DailyBadge>();
            UIKit.Button(h, "BtnDragons", "", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, (mobile ? 120 : 60) - sq - 10), new Vector2(sq, sq), red, () => OnMenuButton(2), 14, Icons.Paw);

            // скорость (кроссовок) и деньги: на ПК — слева снизу, на телефоне — под верхними кнопками (снизу джойстик)
            Vector2 statAnchor = mobile ? new Vector2(0, 1) : new Vector2(0, 0);
            Vector2 speedPos = mobile ? new Vector2(12, -100) : new Vector2(14, 118);
            Vector2 moneyPos = mobile ? new Vector2(12, -150) : new Vector2(90, 14);
            var shoe = UIKit.Icon(h, Icons.Shoe, statAnchor, speedPos + new Vector2(mobile ? 28 : 44, mobile ? -22 : 34), mobile ? 56 : 88);
            speedText = UIKit.Label(UIKit.Rect(h, "Speed", statAnchor, new Vector2(0, mobile ? 1 : 0), speedPos + new Vector2(mobile ? 60 : 100, 0), new Vector2(360, mobile ? 44 : 70)),
                "", mobile ? 30 : 48, Cyan, TextAnchor.MiddleLeft);
            coinsText = UIKit.Label(UIKit.Rect(h, "Money", statAnchor, new Vector2(0, mobile ? 1 : 0), moneyPos, new Vector2(520, mobile ? 56 : 96)),
                "", mobile ? 40 : 72, new Color(0.55f, 0.95f, 0.5f), TextAnchor.MiddleLeft);
            foreach (var o in coinsText.GetComponents<Outline>()) o.effectDistance *= 2f;
            foreach (var o in speedText.GetComponents<Outline>()) o.effectDistance *= 1.5f;
            // доход в секунду — справа снизу (на ПК)
            cpsText = UIKit.Label(UIKit.Rect(h, "Cps", mobile ? new Vector2(0, 1) : new Vector2(1, 0), mobile ? new Vector2(0, 1) : new Vector2(1, 0),
                mobile ? new Vector2(14, -202) : new Vector2(-16, 14), new Vector2(360, 48)), "", mobile ? 20 : 30, new Color(1f, 0.95f, 0.6f), mobile ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            invText = UIKit.Label(UIKit.Rect(h, "Inv", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 136 : 128), new Vector2(500, 26)), "", 17, new Color(1f, 0.85f, 0.4f));

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
            trailsPanel = BuildTrails();
            eggsPanel = BuildEggs();
            dailyPanel = BuildDaily();

            if (mobile) hud.AddComponent<MobileControls>().Build(h);

            // главное меню убрано — игрок сразу попадает в игру (см. StartGame)
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
                slotIcon[i] = UIKit.Icon(b.transform, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(0, 8), s * 0.95f);
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

        Image[] equipIcons, dragIcons;
        Text[] statChips;

        Text StatChip(Transform parent, Sprite icon, Color color, Vector2 pos)
        {
            var chip = UIKit.Panel(parent, "Chip", new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, new Vector2(205, 46), new Color(0.12f, 0.12f, 0.2f, 1f), 2f);
            UIKit.Icon(chip.transform, icon, new Vector2(0, 0.5f), new Vector2(26, 0), 40);
            return UIKit.Label(UIKit.Rect(chip.transform, "V", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(150, 40)), "", 21, color, TextAnchor.MiddleLeft);
        }

        /// <summary>
        /// Драконы: сверху бонусы и 5 НАДЕТЫХ (дают бонусы, летают рядом), ниже драконы на грядках (монеты):
        /// улучшить за монеты или надеть. Везде — 3D-миниатюры настоящих моделей.
        /// </summary>
        GameObject BuildDragons()
        {
            RectTransform body;
            var go = Modal(Loc.T("dragons"), new Color(1f, 0.55f, 0.15f), new Vector2(960, 680), out body);
            statChips = new[]
            {
                StatChip(body, Icons.Bolt, Cyan, new Vector2(-330, 0)),
                StatChip(body, Icons.Coin, Gold, new Vector2(-110, 0)),
                StatChip(body, Icons.Star, Pink, new Vector2(110, 0)),
                StatChip(body, Icons.Shoe, new Color(0.6f, 0.8f, 1f), new Vector2(330, 0)),
            };
            dragTotals = UIKit.Label(UIKit.Rect(body, "Hint", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(900, 26)), "", 18, new Color(0.75f, 0.8f, 0.95f));
            int ns = GameConfig.InventorySlots;
            equipTexts = new Text[ns]; unequipBtns = new Button[ns]; unequipTexts = new Text[ns]; equipIcons = new Image[ns];
            for (int i = 0; i < ns; i++)
            {
                int k = i;
                var c = UIKit.Panel(body, "Eq" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - 2) * 180, -84), new Vector2(170, 176), new Color(0.2f, 0.34f, 0.3f), 3f);
                equipIcons[i] = UIKit.Icon(c.transform, Icons.Dragon, new Vector2(0.5f, 1), new Vector2(0, -52), 104);
                equipTexts[i] = UIKit.Label(UIKit.Rect(c.transform, "T", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(164, 44)), "", 15, Color.white);
                unequipBtns[i] = UIKit.Button(c.transform, "Un", Loc.Ru ? "Снять" : "Unequip", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(146, 38),
                    new Color(0.95f, 0.45f, 0.25f), () => { GameManager.Instance.UnequipToPlot(k); RefreshPanels(); }, 17);
            }
            UIKit.Label(UIKit.Rect(body, "PlT", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -268), new Vector2(900, 28)),
                Loc.Ru ? "На грядках — приносят монеты" : "On plots — earn coins", 20, new Color(1f, 0.9f, 0.6f));
            int n = GameConfig.MaxPlots;
            dragCards = new Image[n]; dragTexts = new Text[n]; dragBtnTexts = new Text[n]; dragBtns = new Button[n]; equipBtns = new Button[n]; dragIcons = new Image[n];
            float cw = 296, ch = 150;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                int col = i % 3, row = i / 3;
                var card = UIKit.Panel(body, "D" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - 1) * (cw + 12), -302 - row * (ch + 12)), new Vector2(cw, ch), new Color(0.22f, 0.22f, 0.34f), 3f);
                dragCards[i] = card;
                dragIcons[i] = UIKit.Icon(card.transform, Icons.Dragon, new Vector2(0, 1), new Vector2(62, -54), 112);
                dragTexts[i] = UIKit.Label(UIKit.Rect(card.transform, "T", new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -6), new Vector2(cw - 124, 92)), "", 15, Color.white, TextAnchor.UpperLeft);
                dragBtns[i] = UIKit.Button(card.transform, "Up", "", new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(170, 42),
                    new Color(0.28f, 0.8f, 0.38f), () => { if (GameManager.Instance.TryUpgradeDragon(k)) RefreshPanels(); }, 16);
                dragBtnTexts[i] = dragBtns[i].GetComponentInChildren<Text>();
                equipBtns[i] = UIKit.Button(card.transform, "Eq", Loc.Ru ? "Надеть" : "Equip", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(104, 42),
                    new Color(0.3f, 0.55f, 1f), () => { GameManager.Instance.EquipFromPlot(k); RefreshPanels(); }, 16);
            }
            return go;
        }

        Button YanBuy(Transform parent, string id, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int font)
        {
            var b = UIKit.Button(parent, "Buy_" + id, "", anchor, new Vector2(0.5f, 0.5f), pos, size, color, () => YandexSDK.Purchase(id), font);
            yanPriceById[id] = b.GetComponentInChildren<Text>();
            yanBtnById[id] = b;
            return b;
        }

        /// <summary>Донат-магазин: Драконье яйцо (эксклюзивы с шансами), x2 доход, x2 рост яиц, монеты и скорость.</summary>
        GameObject BuildYanShop()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "МАГАЗИН" : "STORE", new Color(0.2f, 0.45f, 1f), new Vector2(900, 620), out body);
            var content = UIKit.Scroll(body, new Vector2(0.5f, 0.5f), Vector2.zero, body.sizeDelta, false);
            content.sizeDelta = new Vector2(body.sizeDelta.x, 1080);
            float w = body.sizeDelta.x - 20;

            // --- Драконье яйцо ---
            var egg = UIKit.Panel(content, "DragonEgg", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(w, 270), new Color(1f, 0.72f, 0.12f), 3f);
            var eggIcon = UIKit.Icon(egg.transform, IconArt.Egg(Tier.Legendary, true), new Vector2(0, 0.5f), new Vector2(110, 10), 210);
            eggIcon.gameObject.AddComponent<TitleWobble>();
            var nw = UIKit.Panel(egg.transform, "New", new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -8), new Vector2(110, 40), new Color(0.9f, 0.2f, 0.2f), 3f);
            UIKit.Label(nw.transform, "NEW!", 22, Color.white);
            UIKit.Label(UIKit.Rect(egg.transform, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(90, -6), new Vector2(600, 40)), Loc.Ru ? "ДРАКОНЬЕ ЯЙЦО" : "DRAGON EGG", 32, new Color(1f, 0.95f, 0.8f));
            int k = 0;
            foreach (var d in GameConfig.Dragons)
            {
                if (!d.exclusive) continue;
                var c = UIKit.Panel(egg.transform, "X" + k, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-120 + k * 96, -52), new Vector2(88, 96), new Color(d.body.r * 0.5f, d.body.g * 0.5f, d.body.b * 0.5f, 1f), 2f);
                UIKit.Icon(c.transform, IconArt.Dragon(d), new Vector2(0.5f, 0.5f), new Vector2(0, 8), 96);
                UIKit.Label(UIKit.Rect(c.transform, "P", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-2, 2), new Vector2(60, 26)), d.premiumChance + "%", 17, Color.white, TextAnchor.LowerRight);
                k++;
            }
            string[] ids = { "dragon_egg_1", "dragon_egg_3", "dragon_egg_10" };
            string[] labels = Loc.Ru ? new[] { "1 ЯЙЦО", "3 ЯЙЦА", "10 ЯИЦ" } : new[] { "1 EGG", "3 EGGS", "10 EGGS" };
            Color[] bc = { new Color(0.25f, 0.8f, 0.3f), new Color(1f, 0.6f, 0.1f), new Color(0.85f, 0.3f, 0.95f) };
            for (int i = 0; i < 3; i++)
            {
                float x = -110 + i * 200;
                UIKit.Label(UIKit.Rect(egg.transform, "L" + i, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x + 90, 72), new Vector2(190, 26)), labels[i], 17, Color.white);
                YanBuy(egg.transform, ids[i], new Vector2(0.5f, 0), new Vector2(x + 90, 38), new Vector2(180, 54), bc[i], 22);
            }

            // --- x2 Доход и x2 Рост ---
            float half = (w - 12) / 2f;
            var inc = UIKit.Panel(content, "X2Income", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-half / 2f - 6, -290), new Vector2(half, 200), new Color(1f, 0.85f, 0.2f), 3f);
            UIKit.Icon(inc.transform, Icons.Coin, new Vector2(0, 0.5f), new Vector2(80, 0), 130);
            UIKit.Label(UIKit.Rect(inc.transform, "T", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(250, 50)), Loc.Ru ? "x2 Доход" : "x2 Income", 32, Color.white, TextAnchor.MiddleRight);
            YanBuy(inc.transform, "x2_income", new Vector2(1, 0), new Vector2(-110, 50), new Vector2(190, 62), new Color(0.25f, 0.8f, 0.3f), 24);
            var gr = UIKit.Panel(content, "X2Grow", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(half / 2f + 6, -290), new Vector2(half, 200), new Color(0.4f, 0.85f, 0.95f), 3f);
            UIKit.Icon(gr.transform, IconArt.Egg(Tier.Rare), new Vector2(0, 0.5f), new Vector2(80, 0), 150);
            UIKit.Label(UIKit.Rect(gr.transform, "T", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -8), new Vector2(260, 70)), Loc.Ru ? "x2 Скорость\nроста яиц" : "x2 Egg\nGrowth", 26, Color.white, TextAnchor.MiddleRight);
            YanBuy(gr.transform, "x2_grow", new Vector2(1, 0), new Vector2(-110, 50), new Vector2(190, 62), new Color(0.25f, 0.8f, 0.3f), 24);

            // --- остальные товары: сетка ---
            int idx = 0;
            float cw = (w - 36) / 4f, ch = 250;
            foreach (var pd in GameConfig.Products)
            {
                if (pd.id.StartsWith("dragon_egg") || pd.id == "x2_income" || pd.id == "x2_grow") continue;
                int col = idx % 4, row = idx / 4;
                var card = UIKit.Panel(content, "P" + pd.id, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - 1.5f) * (cw + 12), -506 - row * (ch + 12)), new Vector2(cw, ch),
                    new Color(pd.color.r * 0.5f, pd.color.g * 0.5f, pd.color.b * 0.5f, 1f), 3f);
                var icon = pd.kind == ProductKind.Coins ? Icons.Coin : pd.kind == ProductKind.Speed ? Icons.Bolt : pd.kind == ProductKind.Egg ? Icons.Egg : Icons.Star;
                UIKit.Icon(card.transform, icon, new Vector2(0.5f, 1), new Vector2(0, -52), 78);
                UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -102), new Vector2(cw - 10, 30)), Loc.Ru ? pd.nameRu : pd.nameEn, 18, Color.white);
                UIKit.Label(UIKit.Rect(card.transform, "D", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -142), new Vector2(cw - 12, 44)), Loc.Ru ? pd.descRu : pd.descEn, 14, new Color(0.92f, 0.92f, 1f));
                YanBuy(card.transform, pd.id, new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(cw - 24, 50), new Color(0.3f, 0.8f, 0.35f), 19);
                idx++;
            }
            return go;
        }

        /// <summary>Магазин трейлов: горизонтальная лента карточек, покупка за монеты или Яны.</summary>
        GameObject BuildTrails()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "Магазин Трейлов" : "Trail Shop", new Color(0.85f, 0.3f, 0.95f), new Vector2(920, 520), out body);
            var content = UIKit.Scroll(body, new Vector2(0.5f, 0.5f), Vector2.zero, body.sizeDelta, true);
            int n = GameConfig.Trails.Length;
            float cw = 280, ch = body.sizeDelta.y - 20;
            content.sizeDelta = new Vector2(n * (cw + 14) + 14, body.sizeDelta.y);
            trailCoinBtn = new Text[n]; trailYanBtn = new Text[n]; trailYanButtons = new Button[n]; trailCards = new Image[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var t = GameConfig.Trails[i];
                var card = UIKit.Panel(content, "T" + i, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14 + i * (cw + 14), 0), new Vector2(cw, ch),
                    t.rainbow ? new Color(0.6f, 0.4f, 0.9f) : new Color(t.a.r * 0.75f, t.a.g * 0.75f, t.a.b * 0.75f, 1f), 3f);
                trailCards[i] = card;
                if (t.rainbow) card.gameObject.AddComponent<RainbowImage>();
                UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(cw - 10, 38)), Loc.Ru ? t.nameRu : t.nameEn, 26, Color.white);
                UIKit.Label(UIKit.Rect(card.transform, "R", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(cw - 10, 24)), Loc.Ru ? t.rarityRu : t.rarityEn, 17, Color.white);
                // превью: кроссовок и "шлейф"
                for (int j = 0; j < 3; j++)
                {
                    var streak = UIKit.Panel(card.transform, "S" + j, new Vector2(0.5f, 1), new Vector2(1, 0.5f), new Vector2(-10, -120 - j * 24), new Vector2(170 - j * 40, 16), Color.Lerp(t.b, t.a, j / 2f));
                    streak.raycastTarget = false;
                }
                var shoe = UIKit.Icon(card.transform, Icons.Shoe, new Vector2(0.5f, 1), new Vector2(40, -140), 110);
                shoe.color = Color.Lerp(t.a, Color.white, 0.4f);
                var band = UIKit.Panel(card.transform, "Band", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(cw - 20, 50), new Color(0, 0, 0, 0.25f));
                band.raycastTarget = false;
                UIKit.Label(band.transform, (Loc.Ru ? "Скорость x" : "Speed x") + t.mult.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), 24, Color.white);
                var cb = UIKit.Button(card.transform, "Coin", "", new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 12), new Vector2(160, 66), new Color(0.25f, 0.85f, 0.3f), () => { GameManager.Instance.BuyTrailForCoins(k); RefreshPanels(); }, 24);
                trailCoinBtn[i] = cb.GetComponentInChildren<Text>();
                var yb = UIKit.Button(card.transform, "Yan", "", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 12), new Vector2(92, 66), new Color(0.85f, 0.25f, 0.9f), () => YandexSDK.Purchase(GameConfig.Trails[k].productId), 17);
                trailYanBtn[i] = yb.GetComponentInChildren<Text>();
                trailYanButtons[i] = yb;
            }
            return go;
        }

        public void OpenTrails() { ShowOnly(trailsPanel); }

        /// <summary>Инвентарь яиц: сколько яиц ждёт свободной грядки.</summary>
        GameObject BuildEggs()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "Яйца" : "Eggs", new Color(0.9f, 0.2f, 0.2f), new Vector2(620, 460), out body);
            UIKit.Icon(body, IconArt.Egg(Tier.Epic), new Vector2(0.5f, 1), new Vector2(0, -60), 120);
            eggsBody = UIKit.Label(UIKit.Rect(body, "T", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(560, 230)), "", 21, Color.white);
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
            var subBg = UIKit.Panel(bg, "SubBg", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(760, 52), new Color(0.05f, 0.05f, 0.12f, 0.55f));
            subBg.raycastTarget = false;
            UIKit.Label(subBg.transform, Loc.T("subtitle"), 26, Color.white);
            var eggIcon = UIKit.Icon(bg, Icons.Egg, new Vector2(0.5f, 0.5f), new Vector2(-420, 170), 110);
            eggIcon.gameObject.AddComponent<TitleWobble>();
            var dragIcon = UIKit.Icon(bg, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(420, 170), 120);
            dragIcon.gameObject.AddComponent<TitleWobble>();
            // иконки встают по краям заголовка, шрифт уменьшается, если заголовок не влезает
            var fit = title.gameObject.AddComponent<TitleFit>();
            fit.title = title; fit.left = eggIcon.rectTransform; fit.right = dragIcon.rectTransform; fit.root = root;

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

        /// <summary>Старт сразу после загрузки: обучение управлению (один раз), ежедневная награда, геймплей.</summary>
        public void StartGame()
        {
            hud.SetActive(true);
            InputState.Blocked = false;
            YandexSDK.GameplayStart();
            AfterPlay();
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
        public void ShowRoulette(Tier eggTier, DragonDef result, bool premium, System.Action onDone)
        {
            var go = new GameObject("Roulette", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.AddComponent<Roulette>().Init((RectTransform)go.transform, eggTier, result, premium, onDone);
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

            coinsText.text = "$" + Loc.Num(d.coins);
            cpsText.text = "+$" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? " /сек" : " /s");
            speedText.text = Loc.Num(d.speedPoints);

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
                    slotIcon[i].sprite = IconArt.Dragon(def);
                    slotIcon[i].color = Color.white;
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
                string zoneReq = (gm.RebirthZoneOk ? "<color=#7CFF7C>" : "<color=#FF7C7C>") + (Loc.Ru ? "Нужна зона: " : "Zone required: ") + Loc.TierName((Tier)gm.RebirthReqTier) + "</color>";
                rebirthBody.text = Loc.F("rebirth_desc", next.ToString("0")) + "\n\n" + zoneReq + "\n<color=#FFD84A>" + Loc.F("rebirth_cost", Loc.Num(gm.RebirthCost)) + "</color>";
            }
            if (dragonsPanel.activeSelf)
            {
                statChips[0].text = "+" + gm.DragonSpeedPct.ToString("0") + "%";
                statChips[1].text = "$" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? "/с" : "/s");
                statChips[2].text = "+" + gm.DragonTrainPct.ToString("0") + "%";
                statChips[3].text = "+" + gm.DragonJump.ToString("0.#");
                dragTotals.text = Loc.Ru ? "Надетые драконы дают бонусы и летают рядом с тобой" : "Equipped dragons give bonuses and fly next to you";
                for (int i = 0; i < equipTexts.Length; i++)
                {
                    int id = d.dragonInv[i];
                    unequipBtns[i].gameObject.SetActive(id >= 0);
                    equipIcons[i].enabled = id >= 0;
                    if (id < 0) { equipTexts[i].text = Loc.Ru ? "<color=#8899AA>Пустой слот</color>" : "<color=#8899AA>Empty slot</color>"; continue; }
                    var def = GameConfig.GetDragon(id);
                    equipIcons[i].sprite = IconArt.Dragon(def);
                    float ls = GameConfig.DragonLevelStat(d.dragonInvLvl[i]);
                    equipTexts[i].text = "<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(GameConfig.GetTier(def.tier).color, Color.white, 0.35f)) + ">" + Loc.DragonName(def) + "</color>\n<size=13>+"
                        + (def.speedPct * ls).ToString("0") + (Loc.Ru ? "% скор.  +" : "% spd  +") + (def.trainPct * ls).ToString("0") + (Loc.Ru ? "% прок.</size>" : "% train</size>");
                }
                bool full = gm.FreeSlot() < 0;
                for (int i = 0; i < dragCards.Length; i++)
                {
                    bool owned = i < d.plotsOwned && d.plots[i].state == (int)PlotState.Dragon;
                    dragCards[i].gameObject.SetActive(owned);
                    if (!owned) continue;
                    var pl = d.plots[i];
                    var def = GameConfig.GetDragon(pl.dragonId);
                    var tc = GameConfig.GetTier(def.tier).color;
                    dragCards[i].color = Color.Lerp(new Color(0.2f, 0.2f, 0.32f), tc, 0.28f);
                    dragIcons[i].sprite = IconArt.Dragon(def);
                    float ls = GameConfig.DragonLevelStat(pl.level);
                    dragTexts[i].text = "<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(tc, Color.white, 0.35f)) + ">" + Loc.DragonName(def) + "</color>\n"
                        + "<color=#FFD84A>" + (Loc.Ru ? "Ур. " : "Lv. ") + pl.level + "</color>   " + Loc.TierName(def.tier) + "\n"
                        + "<color=#9CFF8A>$" + Loc.Num(def.coinsPerSec * GameConfig.DragonLevelIncome(pl.level)) + (Loc.Ru ? "/сек</color>" : "/s</color>")
                        + "\n<size=13><color=#AAB4D0>" + (Loc.Ru ? "надетый: +" : "equipped: +") + (def.speedPct * ls).ToString("0") + "% / +" + (def.trainPct * ls).ToString("0") + "%</color></size>";
                    bool max = pl.level >= GameConfig.DragonMaxLevel;
                    dragBtnTexts[i].text = max ? "MAX" : (Loc.Ru ? "Улучшить $" : "Upgrade $") + Loc.Num(gm.DragonUpgradeCost(i));
                    dragBtns[i].interactable = !max;
                    equipBtns[i].interactable = !full;
                }
            }
            if (yanPanel.activeSelf)
            {
                foreach (var pd in GameConfig.Products)
                {
                    Text t; Button bt;
                    if (!yanPriceById.TryGetValue(pd.id, out t)) continue;
                    bool owned = pd.kind == ProductKind.Permanent && gm.Owns(pd.id);
                    t.text = owned ? (Loc.Ru ? "Куплено" : "Owned") : YandexSDK.PriceText(pd);
                    if (yanBtnById.TryGetValue(pd.id, out bt)) bt.interactable = !owned;
                }
            }
            if (trailsPanel.activeSelf)
            {
                for (int i = 0; i < trailCoinBtn.Length; i++)
                {
                    var t = GameConfig.Trails[i];
                    bool owned = gm.OwnsTrail(i);
                    bool eq = d.equippedTrail == i;
                    trailCoinBtn[i].text = owned ? (eq ? (Loc.Ru ? "Надето" : "Equipped") : (Loc.Ru ? "Надеть" : "Equip")) : "$" + Loc.Num(t.coinPrice);
                    trailYanButtons[i].gameObject.SetActive(!owned);
                    trailYanBtn[i].text = YandexSDK.PriceText(t.productId, t.fallbackPrice).Replace(" ", "\n");
                    foreach (var o in trailCards[i].GetComponents<Outline>()) o.effectColor = eq ? Gold : UIKit.Stroke;
                }
            }
            if (eggsPanel.activeSelf)
            {
                var sb = new StringBuilder();
                if (d.inventory.Count == 0) sb.Append(Loc.Ru ? "Яиц в инвентаре нет.\nУкради яйцо у брейнротов или открой Драконье яйцо в магазине!" : "No eggs.\nSteal one from brainrots or get a Dragon Egg in the store!");
                else
                {
                    var counts = new Dictionary<string, int>();
                    foreach (var e in d.inventory)
                    {
                        string name = e.dragonId == GameConfig.PremiumEggMarker ? (Loc.Ru ? "Драконье яйцо" : "Dragon Egg") : Loc.TierName((Tier)e.tier);
                        int c; counts.TryGetValue(name, out c); counts[name] = c + 1;
                    }
                    foreach (var kv in counts) sb.AppendLine(kv.Key + "  x" + kv.Value);
                    sb.AppendLine();
                    sb.Append(Loc.Ru ? "Яйца сами посадятся на свободные грядки.\nОсвободи или купи грядку!" : "Eggs are planted automatically on free plots.\nFree up or buy a plot!");
                }
                eggsBody.text = sb.ToString();
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

    /// <summary>Подгоняет размер заголовка меню под ширину экрана и ставит иконки по его краям.</summary>
    public class TitleFit : MonoBehaviour
    {
        public Text title;
        public RectTransform left, right, root;
        float lastW = -1;

        void LateUpdate()
        {
            if (title == null || root == null) return;
            float screenW = root.rect.width;
            if (Mathf.Abs(screenW - lastW) < 1f) return;
            lastW = screenW;
            float iconSpace = 150f;
            title.fontSize = 96;
            float w = title.preferredWidth;
            float maxW = screenW - iconSpace * 2 - 40;
            if (w > maxW && w > 0)
            {
                title.fontSize = Mathf.Max(40, Mathf.FloorToInt(96 * maxW / w));
                w = title.preferredWidth;
            }
            var box = (RectTransform)title.rectTransform.parent;
            box.sizeDelta = new Vector2(w + 60, box.sizeDelta.y);
            float x = w / 2f + 85f;
            left.anchoredPosition = new Vector2(-x, left.anchoredPosition.y);
            right.anchoredPosition = new Vector2(x, right.anchoredPosition.y);
        }
    }

    public class PulseScale : MonoBehaviour
    {
        void Update() { transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f); }
    }
}
