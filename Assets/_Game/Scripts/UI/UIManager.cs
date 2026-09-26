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
        Image[] storeCards, storeIcons; Text[] storeTexts; Text storeTitle;
        Text eggsBody; RectTransform eggsGrid; Button eggsPlant, eggsPlantAll; string eggsSig = null; int eggSel = int.MinValue;
        readonly List<KeyValuePair<int, Image>> eggCards = new List<KeyValuePair<int, Image>>();
        Text[] equipTexts; Button[] equipBtns; Button[] unequipBtns; Text[] unequipTexts;
        readonly List<KeyValuePair<Image, DragonDef>> store3D = new List<KeyValuePair<Image, DragonDef>>();
        bool store3DDone;
        readonly Dictionary<string, Text> yanPriceById = new Dictionary<string, Text>();
        readonly Dictionary<string, Button> yanBtnById = new Dictionary<string, Button>();
        readonly List<GameObject> panels = new List<GameObject>();
        Text rebirthBody, dragonsBody;
        Text[] upgLevel, upgCost;
        Text soundText, musicText;
        Image[] switchBtns;
        Image[] sellCards, sellIcons; Text[] sellNames, sellPrices, sellChecks; Text sellAllText, sellSelText, sellEmpty, sellInfo; Button sellAllBtn, sellSelBtn;
        readonly HashSet<int> sellSel = new HashSet<int>();
        // слоты
        Image[] slotBg; Text[] slotName; Image[] slotIcon;
        // обучение
        GameObject tutPanel; Text tutText;
        float refreshTimer;
        bool mobile;
        // безопасная область холста в альбомной ориентации (в единицах интерфейса) — окна не бывают больше неё
        Vector2 avail = new Vector2(1150, 650);

        /// <summary>Открыто окно "Навыки" (для обучения).</summary>
        public bool SkillsOpen { get { return shopPanel != null && shopPanel.activeSelf; } }

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
            // на телефоне кнопки >= 80 единиц: при самом мелком экране (640x360) это ~44 CSS px — удобно попадать пальцем
            float cb = mobile ? 80 : 96;
            // круглые кнопки слева сверху: улучшения (сумка), перерождение (книга)
            var sb = UIKit.CircleButton(h, "BtnShop", Icons.Skills, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -12), cb, brown, () => OnMenuButton(0));
            HudCaption(sb.transform, Loc.Ru ? "Навыки" : "Skills", cb);
            var rb = UIKit.CircleButton(h, "BtnTrails", Icons.Shoe, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26 + cb, -12), cb, brown, () => OpenTrails());
            rebirthText = UIKit.Label(UIKit.Rect(rb.transform, "R", new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(cb + 20, 24)), Loc.Ru ? "Трейлы" : "Trails", 17, Color.white);
            UIKit.Fit(rebirthText, 12);
            // зелёная "Магазин NEW!" (донат за Яны)
            float shopW = mobile ? 170 : 216, shopH = mobile ? 70 : 92;
            Vector2 shopPos = mobile ? new Vector2(36 + cb * 2, -12) : new Vector2(12, -12 - cb - 44);
            var yb = UIKit.Button(h, "BtnYan", Loc.Ru ? "Магазин" : "Store", new Vector2(0, 1), new Vector2(0, 1), shopPos, new Vector2(shopW, shopH),
                new Color(0.2f, 0.85f, 0.25f), () => OnMenuButton(3), mobile ? 30 : 40);
            var tag = UIKit.Panel(yb.transform, "New", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(84, 26), new Color(1f, 0.85f, 0.2f), 2f);
            tag.raycastTarget = false;
            var nt = UIKit.Label(tag.transform, "NEW!", 17, Color.white);
            nt.rectTransform.offsetMin = Vector2.zero; nt.rectTransform.offsetMax = Vector2.zero;
            if (UIKit.ApplySkin(tag, "badge_new", false)) { tag.type = Image.Type.Simple; tag.preserveAspect = true; UIKit.HideLabels(tag.transform); }
            // шестерёнка справа сверху
            var gb = UIKit.CircleButton(h, "BtnSettings", Icons.Gear, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -12), cb, brown, () => OnMenuButton(4));
            HudCaption(gb.transform, Loc.Ru ? "Настройки" : "Settings", cb);
            // красные квадраты справа: ежедневная награда (яйцо), драконы (лапа)
            float sq = mobile ? 84 : 92;
            var red = new Color(0.9f, 0.2f, 0.2f);
            // подпись прямо на кнопке (иконка сверху, текст снизу), чтобы было понятно, что это
            UIKit.Button(h, "BtnEggs", Loc.Ru ? "Яйца" : "Eggs", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, mobile ? 120 : 60), new Vector2(sq, sq), red, () => ShowOnly(eggsPanel), 17, Icons.Egg);
            // ежедневная награда — без кнопки: окно само открывается при заходе, если награду можно забрать (AfterPlay)
            UIKit.Button(h, "BtnDragons", Loc.Ru ? "Драконы" : "Dragons", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, (mobile ? 120 : 60) - sq - 10), new Vector2(sq, sq), red, () => OnMenuButton(2), 16, Icons.Paw);

            // скорость (кроссовок) и деньги: на ПК — слева снизу, на телефоне — под верхними кнопками (снизу джойстик)
            // Телефон (от верха экрана): кнопки 12..92, подписи под ними 95..119, скорость 122..166, деньги 170..226, доход 226..274;
            //   ширина колонки <= 374, чтобы не заезжать на обучение/тост (они начинаются правее, x >= ~390).
            // ПК (от низа экрана): деньги 14..110 (x 90..440, левее слотов), скорость 134..204, кроссовок 124..212.
            Vector2 statAnchor = mobile ? new Vector2(0, 1) : new Vector2(0, 0);
            Vector2 speedPos = mobile ? new Vector2(12, -122) : new Vector2(14, 134);
            Vector2 moneyPos = mobile ? new Vector2(12, -170) : new Vector2(90, 14);
            var shoe = UIKit.Icon(h, Icons.Shoe, statAnchor, speedPos + new Vector2(mobile ? 28 : 44, mobile ? -22 : 34), mobile ? 56 : 88);
            speedText = UIKit.Label(UIKit.Rect(h, "Speed", statAnchor, new Vector2(0, mobile ? 1 : 0), speedPos + new Vector2(mobile ? 60 : 100, 0), new Vector2(300, mobile ? 44 : 70)),
                "", mobile ? 30 : 48, Cyan, TextAnchor.MiddleLeft);
            coinsText = UIKit.Label(UIKit.Rect(h, "Money", statAnchor, new Vector2(0, mobile ? 1 : 0), moneyPos, new Vector2(mobile ? 360 : 350, mobile ? 56 : 96)),
                "", mobile ? 40 : 72, new Color(0.55f, 0.95f, 0.5f), TextAnchor.MiddleLeft);
            UIKit.Fit(speedText, mobile ? 20 : 28);
            UIKit.Fit(coinsText, mobile ? 24 : 36);
            foreach (var o in coinsText.GetComponents<Outline>()) o.effectDistance *= 2f;
            foreach (var o in speedText.GetComponents<Outline>()) o.effectDistance *= 1.5f;
            // доход в секунду — справа снизу (на ПК)
            cpsText = UIKit.Label(UIKit.Rect(h, "Cps", mobile ? new Vector2(0, 1) : new Vector2(1, 0), mobile ? new Vector2(0, 1) : new Vector2(1, 0),
                mobile ? new Vector2(14, -226) : new Vector2(-16, 14), new Vector2(360, 48)), "", mobile ? 20 : 30, new Color(1f, 0.95f, 0.6f), mobile ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            UIKit.Fit(cpsText, 14);
            // над слотами (слоты 16..~103 с учётом увеличения выбранного): "выбран дракон" и под ним "яиц в инвентаре"
            invText = UIKit.Fit(UIKit.Label(UIKit.Rect(h, "Inv", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 134 : 130), new Vector2(380, 26)), "", 17, new Color(1f, 0.85f, 0.4f)), 12);

            // ===== Слоты драконов снизу =====
            // нижний инвентарь 1-5 убран: надетые драконы видны в окне "Драконы" и летают рядом с игроком
            slotBg = new Image[0]; slotName = new Text[0]; slotIcon = new Image[0];
            heldText = UIKit.Fit(UIKit.Label(UIKit.Rect(h, "Held", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 106 : 102), new Vector2(mobile ? 540 : 620, 26)), "", 18, new Color(0.7f, 1f, 0.85f)), 12);
            heldText.gameObject.SetActive(false); // дракона "в руке" больше нет
            UIKit.Inset(heldText, 6, 0);
            UIKit.Inset(invText, 6, 0);
            // телефон: подписи над слотами — по ширине полосы слотов (правее джойстика, левее прыжка)
            var hotbarStrip = mobile ? h.Find("Hotbar") : null;
            if (hotbarStrip != null)
                foreach (var ht in new[] { heldText, invText })
                {
                    var r = (RectTransform)ht.transform.parent;
                    float y = r.anchoredPosition.y;
                    r.SetParent(hotbarStrip, false);
                    r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0);
                    r.offsetMin = new Vector2(0, y); r.offsetMax = new Vector2(0, y + 26);
                }

            // ===== Обучение (сверху по центру) =====
            // На телефоне слева сверху кнопки до x=358 ("Магазин"), справа — шестерёнка/награда от W-102:
            // сдвигаем обучение/тост/баннер на +80 от центра, чтобы при самой узкой ширине холста (~1090) они шли по x 390..860.
            float tw = mobile ? 470 : 720, tx = mobile ? 80 : 0;
            var tp = UIKit.Panel(h, "Tutorial", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -12), new Vector2(tw, 104), new Color(0.12f, 0.1f, 0.25f, 0.88f), 3f);
            tutPanel = tp.gameObject;
            var tTitle = UIKit.Label(UIKit.Rect(tp.transform, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(tw - 20, 28)), Loc.T("tut_title"), 17, Gold);
            tTitle.alignment = TextAnchor.MiddleCenter;
            // текст x 14..tw-146, кнопка "Пропустить" 130x48 справа (x tw-142..tw-12), по высоте 36..84
            tutText = UIKit.Fit(UIKit.Label(UIKit.Rect(tp.transform, "B", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-66, 6), new Vector2(tw - 160, 72)), "", mobile ? 17 : 21, Color.white), 12);
            UIKit.Inset(tutText, 4, 2);
            UIKit.Button(tp.transform, "Skip", Loc.T("skip"), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, -8), new Vector2(130, 48),
                new Color(0.5f, 0.5f, 0.6f), () => { if (Tutorial.Instance != null) Tutorial.Instance.Skip(); }, 17);
            tutPanel.SetActive(false);

            // ===== Тост и баннер переноски =====
            toastBg = UIKit.Panel(h, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -126), new Vector2(mobile ? 470 : 680, 54), new Color(0.05f, 0.05f, 0.1f, 0.75f), 2f);
            toastText = UIKit.Fit(UIKit.Inset(UIKit.Label(toastBg.transform, "", mobile ? 19 : 23, Color.white), 12, 3), 13);
            toastBg.raycastTarget = false;
            UIKit.ApplySkin(toastBg, "toast", true);
            toastBg.gameObject.SetActive(false);

            carryBg = UIKit.Panel(h, "Carry", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(tx, -186), new Vector2(mobile ? 470 : 560, 48), new Color(0.9f, 0.2f, 0.2f, 0.85f), 3f);
            carryText = UIKit.Fit(UIKit.Inset(UIKit.Label(carryBg.transform, "", 21, Color.white), 12, 3), 13);
            carryBg.raycastTarget = false;
            carryBg.gameObject.SetActive(false);

            // ПК: над скоростью (она до 204 от низа); телефон: над джойстиком (до 270) и кнопкой действия (до 264)
            hintText = UIKit.Fit(UIKit.Label(UIKit.Rect(h, "Hint", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, mobile ? 276 : 212), new Vector2(mobile ? 520 : 760, 40)), "", 22, new Color(1f, 1f, 0.7f)), 14);

            BuildPrompt(h);

            adBg = UIKit.Panel(h, "AdCountdown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(420, 70), new Color(0.1f, 0.1f, 0.18f, 0.9f), 3f);
            adText = UIKit.Label(adBg.transform, "", 26, Gold);
            adBg.raycastTarget = false;
            adBg.gameObject.SetActive(false);

            // ===== Панели =====
            // Игра только в альбомной ориентации: окна считаются по альбомному холсту (даже если загрузка началась
            // в портрете — тогда поверх всего подсказка "Поверните устройство", а окна уже готовы под поворот).
            ResponsiveCanvas.Measure(mobile);
            var ls = ResponsiveCanvas.LandscapeSafeSize;
            if (ls.x > 100f && ls.y > 100f) avail = ls;
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

        /// <summary>Минимальный отступ окна от краёв безопасной области.</summary>
        const float ModalMargin = 12f;

        /// <summary>Подпись под круглой кнопкой HUD (белый текст с обводкой).</summary>
        static void HudCaption(Transform button, string text, float cb)
        {
            var t = UIKit.Label(UIKit.Rect(button, "Caption", new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(cb + 24, 24)), text, 17, Color.white);
            UIKit.Fit(t, 11);
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
            float s = mobile ? 80 : 74, gap = 10;
            float total = n * s + (n - 1) * gap;
            slotBg = new Image[n]; slotName = new Text[n]; slotIcon = new Image[n];
            // Телефон: слоты не лезут в зону джойстика (левые 42% экрана) и не заходят на прыжок (справа, x >= W-170):
            // полоса от 0.42W+8 до W-180, слоты по её центру. Полоса >= 453 при самом узком холсте (~1092) — 5 слотов (440) влезают.
            Transform bar = h;
            if (mobile)
            {
                var strip = UIKit.Rect(h, "Hotbar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
                strip.anchorMin = new Vector2(0.42f, 0); strip.anchorMax = new Vector2(1, 0);
                strip.offsetMin = new Vector2(8, 0); strip.offsetMax = new Vector2(-180, s + 30);
                bar = strip;
            }
            for (int i = 0; i < n; i++)
            {
                int k = i;
                float x = -total / 2f + s / 2f + i * (s + gap);
                var b = UIKit.Button(bar, "Slot" + i, "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 16), new Vector2(s, s),
                    new Color(0.15f, 0.15f, 0.22f, 0.85f), () => { if (GameManager.Instance != null) GameManager.Instance.SelectSlot(k); });
                Destroy(b.GetComponent<ButtonBounce>());
                slotBg[i] = b.GetComponent<Image>();
                if (UIKit.ApplySkin(slotBg[i], "slot", true))
                {
                    // обводка для подсветки выбранного слота золотом (у картинки своя тёмная обводка)
                    var so = slotBg[i].gameObject.AddComponent<Outline>();
                    so.effectColor = UIKit.Stroke; so.effectDistance = new Vector2(3f, -3f);
                    var so2 = slotBg[i].gameObject.AddComponent<Outline>();
                    so2.effectColor = UIKit.Stroke; so2.effectDistance = new Vector2(-3f, 3f);
                }
                slotIcon[i] = UIKit.Icon(b.transform, Icons.Dragon, new Vector2(0.5f, 0.5f), new Vector2(0, 8), s * 0.95f);
                // имя дракона — одной строкой в нижней полоске слота, не вылезает за его края
                slotName[i] = UIKit.Label(b.transform, "", 12, Color.white, TextAnchor.LowerCenter);
                slotName[i].rectTransform.anchorMin = Vector2.zero; slotName[i].rectTransform.anchorMax = new Vector2(1, 0);
                slotName[i].rectTransform.offsetMin = new Vector2(3, 2); slotName[i].rectTransform.offsetMax = new Vector2(-3, 22);
                UIKit.Fit(slotName[i], 8);
                var num = UIKit.Label(UIKit.Rect(b.transform, "N", new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -2), new Vector2(24, 24)), (i + 1).ToString(), 16, Gold, TextAnchor.UpperLeft);
                num.raycastTarget = false;
            }
        }

        void BuildPrompt(Transform h)
        {
            promptBg = UIKit.Panel(h, "Prompt", new Vector2(0, 0), new Vector2(0.5f, 0.5f), Vector2.zero, mobile ? new Vector2(300, 84) : new Vector2(330, 70),
                new Color(0.06f, 0.06f, 0.1f, 0.85f), 3f);
            promptRt = promptBg.rectTransform;
            UIKit.ApplySkin(promptBg, "prompt", true);
            var fill = UIKit.Panel(promptBg.transform, "Fill", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.3f, 0.9f, 0.4f, 0.55f));
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            fill.raycastTarget = false;
            promptFill = fill;
            // клавиша "E" как в ProximityPrompt
            var key = UIKit.Panel(promptBg.transform, "Key", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(50, 50), Color.white, 2f);
            key.raycastTarget = false;
            UIKit.ApplySkin(key, "key", false);
            promptKey = UIKit.Label(key.transform, mobile ? "!" : "E", 28, new Color(0.1f, 0.1f, 0.15f));
            var po = promptKey.GetComponents<Outline>();
            foreach (var o in po) o.enabled = false;
            promptText = UIKit.Label(UIKit.Rect(promptBg.transform, "T", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(mobile ? 228 : 258, 64)), "", mobile ? 21 : 20, Color.white, TextAnchor.MiddleLeft);
            UIKit.Fit(promptText, 13);
            if (mobile)
                UIKit.AddPointer(promptBg.gameObject, e => InputState.TouchAction = true, e => InputState.TouchAction = false);
            else promptBg.raycastTarget = false;
            promptBg.gameObject.SetActive(false);
        }

        // ============================ ПАНЕЛИ ============================
        /// <summary>
        /// Каркас окна: шапка цвета раздела на всю ширину с иконкой и крупным заголовком,
        /// тёмно-синее тело с лёгким узором, красная квадратная кнопка закрытия, "поп" при открытии.
        /// </summary>
        GameObject Modal(string title, Color color, Vector2 size, out RectTransform body)
        {
            // окно никогда не больше безопасной области экрана (с отступом 12 и сдвигом -6 вниз).
            // Альбом: холст телефона >= ~1090x650, ПК >= ~1336x790 — самые большие окна (960x610 / 960x740) влезают
            // с запасом, в т.ч. с вырезом сбоку (до ~80 единиц с каждой стороны); ограничение — страховка.
            float m = ModalMargin;
            Vector2 sz = new Vector2(Mathf.Min(size.x, 1180f, avail.x - 2f * m), Mathf.Min(size.y, avail.y - 2f * m - 12f));
            var bg = UIKit.Panel(root, "Modal_" + title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), sz, new Color(0.12f, 0.15f, 0.3f, 0.98f), 5f);
            var pat = UIKit.Rect(bg.transform, "Pattern", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIKit.Stretch(pat, 8);
            var pi = pat.gameObject.AddComponent<Image>();
            pi.sprite = UIKit.PatternSprite; pi.type = Image.Type.Tiled; pi.raycastTarget = false; pi.color = new Color(1, 1, 1, 0.5f);
            if (UIKit.ApplySkin(bg, "window", false)) pat.gameObject.SetActive(false);
            var header = UIKit.Panel(bg.transform, "Header", new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(sz.x, 86), color, 5f);
            var hrt = header.rectTransform;
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.offsetMin = new Vector2(0, -86); hrt.offsetMax = new Vector2(0, 0);
            var hg = UIKit.Rect(header.transform, "HeaderGloss", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(sz.x - 16, 34));
            var hgi = hg.gameObject.AddComponent<Image>(); hgi.sprite = UIKit.Rounded; hgi.type = Image.Type.Sliced; hgi.color = new Color(1, 1, 1, 0.18f); hgi.raycastTarget = false;
            if (UIKit.ApplySkin(header, "header", true)) hg.gameObject.SetActive(false);
            var tl = UIKit.Label(header.transform, title.ToUpper(), 42, Color.white);
            foreach (var o in tl.GetComponents<Outline>()) o.effectDistance *= 1.4f;
            // заголовок не заезжает под кнопку закрытия (ширина + отступ 14 справа), симметрично слева;
            // на телефоне крестик крупнее (84x74 ≈ 46x41 CSS px даже на 640x360)
            Vector2 closeSz = mobile ? new Vector2(84, 74) : new Vector2(66, 62);
            UIKit.Inset(tl, closeSz.x + 26, 8);
            UIKit.Fit(tl, 24);
            var go = bg.gameObject;
            var close = UIKit.Button(header.transform, "Close", "X", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), closeSz,
                new Color(0.93f, 0.22f, 0.28f), () => go.SetActive(false), 36);
            UIKit.ApplySkin(close.GetComponent<Image>(), "close", false);
            // на картинке ui_close крестика нет — белая "X" с обводкой остаётся видимой, чуть выше центра (у кнопки нижний бортик)
            var cx = close.GetComponentInChildren<Text>();
            if (cx != null)
            {
                cx.rectTransform.offsetMin = new Vector2(4, 6);
                cx.rectTransform.offsetMax = new Vector2(-4, 0);
            }
            body = UIKit.Rect(bg.transform, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -44), new Vector2(sz.x - 40, sz.y - 120));
            go.AddComponent<PopIn>();
            go.SetActive(false);
            panels.Add(go);
            return go;
        }

        GameObject BuildShop()
        {
            // Покупка грядок убрана — все грядки доступны сразу. Окно — только список улучшений:
            // строки 72 + зазор 8; тело окна = высота - 120. При 5 строках список 392 <= тела 410.
            int n = GameConfig.Upgrades.Length;
            // на телефоне строки и кнопки выше (кнопка 62 — удобнее пальцем): 5 строк = 422, окно 560 <= 614 (холст 650)
            float rowH = mobile ? 78 : 72, gap = 8;
            float listH = n * rowH + (n - 1) * gap;
            float winH = Mathf.Clamp(listH + 138, 300, 600);
            RectTransform body;
            var go = Modal(Loc.T("shop"), new Color(0.25f, 0.75f, 0.35f), new Vector2(760, winH), out body);
            Transform list = body;
            if (listH + 6 > body.sizeDelta.y)
            {
                // улучшений больше, чем влезает, — список прокручивается, а не вылезает за окно
                var content = UIKit.Scroll(body, new Vector2(0.5f, 0.5f), Vector2.zero, body.sizeDelta, false);
                content.sizeDelta = new Vector2(body.sizeDelta.x, listH + 8);
                list = content;
            }
            upgLevel = new Text[n]; upgCost = new Text[n];
            // ширина строки — по телу окна (в портрете окно 630, тело 590): имя/описание сужаются, кнопка и уровень справа
            float rowW = Mathf.Min(700f, body.sizeDelta.x), btnW = rowW < 640f ? 160f : 180f;
            float lvlX = rowW - 10 - btnW - 8 - 118, nameW = lvlX - 34;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var u = GameConfig.Upgrades[i];
                var row = UIKit.Panel(list, "Upg" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -3 - i * (rowH + gap)), new Vector2(rowW, rowH), new Color(0.2f, 0.19f, 0.3f, 1f), 2f);
                var dot = UIKit.Panel(row.transform, "C", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(12, rowH - 16), u.color);
                dot.raycastTarget = false;
                // (ширина 700) имя: 30..380 по X, верх строки; описание: низ строки; уровень: 384..502; кнопка: 510..690
                UIKit.Fit(UIKit.Label(UIKit.Rect(row.transform, "N", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -3), new Vector2(nameW, 36)), Loc.Ru ? u.nameRu : u.nameEn, 24, Color.white, TextAnchor.MiddleLeft), 16);
                UIKit.Fit(UIKit.Label(UIKit.Rect(row.transform, "D", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 3), new Vector2(nameW, 32)), Loc.Ru ? u.descRu : u.descEn, 16, new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleLeft), 11);
                upgLevel[i] = UIKit.Fit(UIKit.Label(UIKit.Rect(row.transform, "L", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(lvlX, 0), new Vector2(118, 44)), "", 20, Gold), 13);
                var b = UIKit.Button(row.transform, "Buy", "", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(btnW, mobile ? 62 : 54),
                    new Color(0.25f, 0.78f, 0.35f), () => { if (GameManager.Instance.TryBuyUpgrade(k)) RefreshPanels(); }, 20);
                upgCost[i] = b.GetComponentInChildren<Text>();
            }
            return go;
        }

        GameObject BuildRebirth()
        {
            RectTransform body;
            var go = Modal(Loc.T("rebirth"), new Color(0.78f, 0.32f, 0.95f), new Vector2(620, 480), out body);
            UIKit.Icon(body, Icons.Star, new Vector2(0.5f, 1), new Vector2(0, -44), 84);
            // между звездой (низ на 86 от верха тела) и кнопкой (верх на 66 от низа)
            rebirthBody = UIKit.Fit(UIKit.Label(UIKit.Rect(body, "T", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(560, 188)), "", 22, Color.white), 15);
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
            return UIKit.Fit(UIKit.Label(UIKit.Rect(chip.transform, "V", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(150, 40)), "", 21, color, TextAnchor.MiddleLeft), 13);
        }

        // ===== Окно "Драконы": слева прокручиваемый список (надетые / на грядках / в сумке), справа — выбранный дракон =====
        RectTransform dragList; Image dragBig; Text dragInfo, dragTitle; Button[] dragAct = new Button[3]; Text[] dragActText = new Text[3];
        string dragSig; int dragSelKind = -1, dragSelIdx = -1; // kind: 0 — надетый слот, 1 — грядка, 2 — сумка
        readonly List<KeyValuePair<int, Image>> dragItems = new List<KeyValuePair<int, Image>>();

        GameObject BuildDragons()
        {
            RectTransform body;
            var go = Modal(Loc.T("dragons"), new Color(1f, 0.55f, 0.15f), new Vector2(960, mobile ? 610 : 700), out body);
            float W = body.sizeDelta.x, H = body.sizeDelta.y;
            // бонусы сверху
            float chipW = (W - 30) / 4f;
            statChips = new Text[4];
            Sprite[] ci = { Icons.Bolt, Icons.Coin, Icons.Star, Icons.Shoe };
            Color[] cc = { Cyan, Gold, Pink, new Color(0.6f, 0.8f, 1f) };
            for (int i = 0; i < 4; i++)
            {
                var chip = UIKit.Panel(body, "Chip" + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(i * (chipW + 10), 0), new Vector2(chipW, 46), new Color(0.12f, 0.12f, 0.2f, 1f), 2f);
                UIKit.Icon(chip.transform, ci[i], new Vector2(0, 0.5f), new Vector2(26, 0), 38);
                statChips[i] = UIKit.Fit(UIKit.Label(UIKit.Rect(chip.transform, "V", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(chipW - 56, 40)), "", 20, cc[i], TextAnchor.MiddleLeft), 12);
            }
            // слева — список с полосой прокрутки
            float listW = W * 0.58f, listH = H - 58;
            var listBg = UIKit.Panel(body, "ListBg", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -56), new Vector2(listW, listH), new Color(0.08f, 0.09f, 0.18f, 0.9f), 2f);
            dragList = UIKit.Scroll(listBg.transform, new Vector2(0.5f, 0.5f), new Vector2(-9, 0), new Vector2(listW - 30, listH - 12), false);
            AddScrollbar(dragList.parent as RectTransform, listBg.rectTransform);
            // справа — выбранный дракон
            float detW = W - listW - 12;
            var det = UIKit.Panel(body, "Detail", new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -56), new Vector2(detW, listH), new Color(0.14f, 0.13f, 0.26f, 0.95f), 3f);
            dragTitle = UIKit.Fit(UIKit.Label(UIKit.Rect(det.transform, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(detW - 16, 40)), "", 24, Color.white), 14);
            float big = Mathf.Min(detW - 60, listH * 0.36f);
            dragBig = UIKit.Icon(det.transform, Icons.Dragon, new Vector2(0.5f, 1), new Vector2(0, -48 - big / 2f), big);
            float infoTop = 52 + big, btnArea = 3 * 54 + 8;
            dragInfo = UIKit.Fit(UIKit.Label(UIKit.Rect(det.transform, "I", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -infoTop), new Vector2(detW - 20, Mathf.Max(60, listH - infoTop - btnArea))), "", 17, new Color(0.9f, 0.92f, 1f), TextAnchor.UpperCenter), 11);
            Color[] bc = { new Color(0.3f, 0.55f, 1f), new Color(0.28f, 0.8f, 0.38f), new Color(0.95f, 0.45f, 0.25f) };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                dragAct[i] = UIKit.Button(det.transform, "A" + i, "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8 + (2 - i) * 54), new Vector2(detW - 30, 48), bc[i], () => { DragonAction(k); RefreshPanels(); }, 18);
                dragActText[i] = dragAct[i].GetComponentInChildren<Text>();
            }
            return go;
        }

        /// <summary>Видимая полоса прокрутки справа от списка.</summary>
        static void AddScrollbar(RectTransform viewport, RectTransform holder)
        {
            var sr = viewport.GetComponent<ScrollRect>();
            var track = UIKit.Rect(holder, "Scrollbar", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(14, holder.sizeDelta.y - 16));
            var ti = track.gameObject.AddComponent<Image>(); ti.sprite = UIKit.Rounded; ti.type = Image.Type.Sliced; ti.color = new Color(1, 1, 1, 0.1f);
            var area = UIKit.Rect(track, "Area", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); UIKit.Stretch(area, 1);
            var handle = UIKit.Rect(area, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); UIKit.Stretch(handle, 0);
            var hi = handle.gameObject.AddComponent<Image>(); hi.sprite = UIKit.Rounded; hi.type = Image.Type.Sliced; hi.color = new Color(1f, 0.75f, 0.3f, 0.9f);
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle; sb.targetGraphic = hi; sb.direction = Scrollbar.Direction.BottomToTop;
            if (sr != null) { sr.verticalScrollbar = sb; sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent; }
        }

        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

        void DragonAction(int btn)
        {
            var gm = GameManager.Instance;
            int k = dragSelKind, i = dragSelIdx;
            if (k == 0) { if (btn == 0) gm.UnequipToStore(i); else if (btn == 1) gm.UnequipToPlot(i); }
            else if (k == 1) { if (btn == 0) gm.EquipFromPlot(i); else if (btn == 1) gm.TryUpgradeDragon(i); else gm.PlotToStore(i); }
            else if (k == 2) { if (btn == 0) gm.EquipFromStore(i); else if (btn == 1) gm.StoreToPlot(i); }
            dragSig = null;
        }

        void RefreshDragons(SaveData d, GameManager gm)
        {
            statChips[0].text = "+" + gm.DragonSpeedPct.ToString("0") + "%";
            statChips[1].text = "$" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? "/с" : "/s");
            statChips[2].text = "+" + gm.DragonTrainPct.ToString("0") + "%";
            statChips[3].text = "+" + gm.DragonJump.ToString("0.#");

            // что где лежит
            var eq = new List<int>(); var pl = new List<int>();
            for (int i = 0; i < d.dragonInv.Count; i++) if (d.dragonInv[i] >= 0) eq.Add(i);
            for (int i = 0; i < d.plotsOwned && i < d.plots.Count; i++) if (d.plots[i].state == (int)PlotState.Dragon) pl.Add(i);
            var sig = new System.Text.StringBuilder();
            foreach (var i in eq) sig.Append('e').Append(i).Append(':').Append(d.dragonInv[i]).Append(d.dragonInvLvl[i]);
            foreach (var i in pl) sig.Append('p').Append(i).Append(':').Append(d.plots[i].dragonId).Append(d.plots[i].level);
            for (int i = 0; i < d.dragonStore.Count; i++) sig.Append('s').Append(d.dragonStore[i]).Append(d.dragonStoreLvl[i]);

            // выбор по умолчанию — первый дракон
            bool valid = (dragSelKind == 0 && eq.Contains(dragSelIdx)) || (dragSelKind == 1 && pl.Contains(dragSelIdx)) || (dragSelKind == 2 && dragSelIdx < d.dragonStore.Count);
            if (!valid) { dragSelKind = -1; if (eq.Count > 0) { dragSelKind = 0; dragSelIdx = eq[0]; } else if (pl.Count > 0) { dragSelKind = 1; dragSelIdx = pl[0]; } else if (d.dragonStore.Count > 0) { dragSelKind = 2; dragSelIdx = 0; } }

            if (sig.ToString() != dragSig)
            {
                dragSig = sig.ToString();
                foreach (Transform c in dragList) Destroy(c.gameObject);
                dragItems.Clear();
                float lw = dragList.sizeDelta.x, gap = 8;
                int cols = 3; float cw = (lw - (cols - 1) * gap) / cols, ch = cw * 0.9f;
                float y = 4;
                System.Action<string> header = t =>
                {
                    UIKit.Fit(UIKit.Label(UIKit.Rect(dragList, "H", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -y), new Vector2(lw, 30)), t, 19, new Color(1f, 0.88f, 0.55f), TextAnchor.MiddleLeft), 12);
                    y += 34;
                };
                System.Action<int, int, int, int> item = null;
                int n = 0;
                item = (kind, idx, id, lvl) =>
                {
                    int col = n % cols, row = n / cols;
                    var def = GameConfig.GetDragon(id);
                    var tc = GameConfig.GetTier(def.tier).color;
                    int kk = kind, ii = idx;
                    var b = UIKit.Button(dragList, "D", "", new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * (cw + gap), -(y + row * (ch + gap))), new Vector2(cw, ch),
                        Color.Lerp(new Color(0.2f, 0.2f, 0.32f), tc, 0.3f), () => { dragSelKind = kk; dragSelIdx = ii; RefreshPanels(); }, 14);
                    Destroy(b.GetComponent<ButtonBounce>());
                    UIKit.Icon(b.transform, IconArt.Dragon(def), new Vector2(0.5f, 1), new Vector2(0, -ch * 0.38f), ch * 0.62f);
                    UIKit.Fit(UIKit.Label(UIKit.Rect(b.transform, "N", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(cw - 8, ch * 0.28f)),
                        Loc.DragonName(def) + " <color=#FFD84A>" + (Loc.Ru ? "ур." : "lv.") + lvl + "</color>", 14, Color.white), 9);
                    dragItems.Add(new KeyValuePair<int, Image>(kind * 100 + idx, b.GetComponent<Image>()));
                    n++;
                };
                System.Action endSection = () => { if (n > 0) y += ((n + cols - 1) / cols) * (ch + gap) + 6; n = 0; };

                header((Loc.Ru ? "Надетые " : "Equipped ") + eq.Count + "/" + GameConfig.InventorySlots + (Loc.Ru ? "  — дают бонусы" : "  — give bonuses"));
                foreach (var i in eq) item(0, i, d.dragonInv[i], d.dragonInvLvl[i]);
                endSection();
                header((Loc.Ru ? "На грядках " : "On plots ") + pl.Count + "/" + GameConfig.MaxPlots + (Loc.Ru ? "  — приносят монеты" : "  — earn coins"));
                foreach (var i in pl) item(1, i, d.plots[i].dragonId, d.plots[i].level);
                endSection();
                header((Loc.Ru ? "В сумке " : "In bag ") + d.dragonStore.Count + "/" + GameConfig.StorageSlots + (Loc.Ru ? "  — просто хранятся" : "  — just stored"));
                for (int i = 0; i < d.dragonStore.Count; i++) item(2, i, d.dragonStore[i], d.dragonStoreLvl[i]);
                endSection();
                dragList.sizeDelta = new Vector2(lw, Mathf.Max(y + 8, (dragList.parent as RectTransform).sizeDelta.y));
            }
            foreach (var kv in dragItems)
                if (kv.Value != null) kv.Value.color = kv.Key == dragSelKind * 100 + dragSelIdx ? new Color(1f, 0.78f, 0.2f) : kv.Value.color.a > 0 ? RestColor(kv.Key, d) : kv.Value.color;

            // правая панель
            bool has = dragSelKind >= 0;
            dragBig.enabled = has;
            for (int i = 0; i < 3; i++) dragAct[i].gameObject.SetActive(false);
            if (!has)
            {
                dragTitle.text = Loc.Ru ? "Драконов пока нет" : "No dragons yet";
                dragInfo.text = Loc.Ru ? "Укради яйцо у брейнротов, вырасти его на грядке и открой — появится дракон!" : "Steal an egg from brainrots, grow it on a plot and open it to get a dragon!";
                return;
            }
            int sid, slvl;
            if (dragSelKind == 0) { sid = d.dragonInv[dragSelIdx]; slvl = d.dragonInvLvl[dragSelIdx]; }
            else if (dragSelKind == 1) { sid = d.plots[dragSelIdx].dragonId; slvl = d.plots[dragSelIdx].level; }
            else { sid = d.dragonStore[dragSelIdx]; slvl = d.dragonStoreLvl[dragSelIdx]; }
            var sd = GameConfig.GetDragon(sid);
            var stc = GameConfig.GetTier(sd.tier).color;
            dragBig.sprite = IconArt.Dragon(sd);
            dragTitle.text = "<color=#" + Hex(Color.Lerp(stc, Color.white, 0.3f)) + ">" + Loc.DragonName(sd) + "</color>";
            float ls = GameConfig.DragonLevelStat(slvl);
            string where = dragSelKind == 0 ? (Loc.Ru ? "Надет — даёт бонусы" : "Equipped — gives bonuses") : dragSelKind == 1 ? (Loc.Ru ? "На грядке — приносит монеты" : "On a plot — earns coins") : (Loc.Ru ? "В сумке — бонусов не даёт" : "In the bag — no bonuses");
            dragInfo.text = Loc.TierName(sd.tier) + "   <color=#FFD84A>" + (Loc.Ru ? "Ур. " : "Lv. ") + slvl + "</color>\n"
                + "<color=#9CFF8A>$" + Loc.Num(sd.coinsPerSec * GameConfig.DragonLevelIncome(slvl)) + (Loc.Ru ? "/сек на грядке</color>\n" : "/s on a plot</color>\n")
                + "<color=#8FE8FF>+" + (sd.speedPct * ls).ToString("0") + (Loc.Ru ? "% скорость   +" : "% speed   +") + (sd.trainPct * ls).ToString("0") + (Loc.Ru ? "% прокачка</color>\n" : "% training</color>\n")
                + "<size=14><color=#AAB4D0>" + where + "</color></size>";
            bool slotsFull = gm.FreeSlot() < 0, bagFull = gm.StoreFull, noPlot = gm.FreePlotCount == 0;
            if (dragSelKind == 0)
            {
                SetAct(0, Loc.Ru ? "Снять в сумку" : "Move to bag", !bagFull);
                SetAct(1, Loc.Ru ? "Поставить на грядку" : "Put on a plot", !noPlot);
            }
            else if (dragSelKind == 1)
            {
                SetAct(0, Loc.Ru ? "Надеть" : "Equip", !slotsFull || !bagFull);
                bool max = slvl >= GameConfig.DragonMaxLevel;
                SetAct(1, max ? "MAX" : (Loc.Ru ? "Улучшить $" : "Upgrade $") + Loc.Num(gm.DragonUpgradeCost(dragSelIdx)), !max);
                SetAct(2, Loc.Ru ? "Убрать в сумку" : "Move to bag", !bagFull);
            }
            else
            {
                SetAct(0, Loc.Ru ? "Надеть" : "Equip", !slotsFull);
                SetAct(1, Loc.Ru ? "Поставить на грядку" : "Put on a plot", !noPlot);
            }
        }

        Color RestColor(int key, SaveData d)
        {
            int kind = key / 100, idx = key % 100, id;
            if (kind == 0) id = idx < d.dragonInv.Count ? d.dragonInv[idx] : -1;
            else if (kind == 1) id = idx < d.plots.Count ? d.plots[idx].dragonId : -1;
            else id = idx < d.dragonStore.Count ? d.dragonStore[idx] : -1;
            if (id < 0) return new Color(0.2f, 0.2f, 0.32f);
            return Color.Lerp(new Color(0.2f, 0.2f, 0.32f), GameConfig.GetTier(GameConfig.GetDragon(id).tier).color, 0.3f);
        }

        void SetAct(int i, string text, bool enabled)
        {
            dragAct[i].gameObject.SetActive(true);
            dragActText[i].text = text;
            dragAct[i].interactable = enabled;
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
            var go = Modal(Loc.Ru ? "МАГАЗИН" : "STORE", new Color(0.2f, 0.45f, 1f), new Vector2(900, mobile ? 600 : 620), out body);
            var content = UIKit.Scroll(body, new Vector2(0.5f, 0.5f), Vector2.zero, body.sizeDelta, false);
            content.sizeDelta = new Vector2(body.sizeDelta.x, 1100);
            float w = body.sizeDelta.x - 20;

            // --- Драконье яйцо: слева яйцо, справа 6 эксклюзивов с шансами, снизу кнопки покупки ---
            const float eggH = 330f;
            var egg = UIKit.Panel(content, "DragonEgg", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(w, eggH), new Color(1f, 0.72f, 0.12f), 3f);
            egg.gameObject.AddComponent<ShineSweep>();
            var eggIcon = UIKit.Icon(egg.transform, IconArt.Egg(Tier.Legendary, true), new Vector2(0, 0.5f), new Vector2(118, -6), 220);
            eggIcon.gameObject.AddComponent<TitleWobble>();
            var nw = UIKit.Panel(egg.transform, "New", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(110, 40), new Color(0.9f, 0.2f, 0.2f), 3f);
            UIKit.Label(nw.transform, "NEW!", 22, Color.white);
            if (UIKit.ApplySkin(nw, "badge_new", false)) { nw.type = Image.Type.Simple; nw.preserveAspect = true; UIKit.HideLabels(nw.transform); }
            // правая часть карточки (справа от яйца)
            float left = -w / 2f + 240f, right = w / 2f - 16f, avail = right - left;
            UIKit.Fit(UIKit.Label(UIKit.Rect(egg.transform, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((left + right) / 2f, -8), new Vector2(avail, 44)), Loc.Ru ? "ДРАКОНЬЕ ЯЙЦО" : "DRAGON EGG", 34, new Color(1f, 0.97f, 0.85f)), 22);
            var ex = new List<DragonDef>();
            foreach (var d in GameConfig.Dragons) if (d.exclusive) ex.Add(d);
            float cwx = avail / ex.Count;
            for (int k = 0; k < ex.Count; k++)
            {
                var d = ex[k];
                var tc = GameConfig.GetTier(d.tier).color;
                var c = UIKit.Panel(egg.transform, "X" + k, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(left + cwx * (k + 0.5f), -58), new Vector2(cwx - 8, 124),
                    new Color(0.18f, 0.12f, 0.32f, 0.92f), 2f);
                var glow = UIKit.Icon(c.transform, UIKit.Circle, new Vector2(0.5f, 0.5f), new Vector2(0, 10), cwx - 20);
                glow.color = new Color(tc.r, tc.g, tc.b, 0.35f);
                var xi = UIKit.Icon(c.transform, IconArt.Dragon(d), new Vector2(0.5f, 0.5f), new Vector2(0, 12), Mathf.Min(cwx - 10, 110));
                store3D.Add(new KeyValuePair<Image, DragonDef>(xi, d));
                var chip = UIKit.Panel(c.transform, "P", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(cwx - 22, 26), k == ex.Count - 1 ? new Color(0.95f, 0.25f, 0.35f) : new Color(0.1f, 0.08f, 0.2f, 0.9f), 2f);
                chip.raycastTarget = false;
                UIKit.Fit(UIKit.Inset(UIKit.Label(chip.transform, d.premiumChance.ToString("0.#") + "%", 17, Color.white), 3, 2), 11);
            }
            string[] ids = { "dragon_egg_1", "dragon_egg_3", "dragon_egg_10" };
            string[] labels = Loc.Ru ? new[] { "1 ЯЙЦО", "3 ЯЙЦА", "10 ЯИЦ" } : new[] { "1 EGG", "3 EGGS", "10 EGGS" };
            Color[] bc = { new Color(0.25f, 0.8f, 0.3f), new Color(1f, 0.6f, 0.1f), new Color(0.85f, 0.3f, 0.95f) };
            float bw = avail / 3f;
            for (int i = 0; i < 3; i++)
            {
                float x = left + bw * (i + 0.5f);
                UIKit.Fit(UIKit.Label(UIKit.Rect(egg.transform, "L" + i, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 100), new Vector2(bw, 30)), labels[i], 20, Color.white), 13);
                YanBuy(egg.transform, ids[i], new Vector2(0.5f, 0), new Vector2(x, 54), new Vector2(bw - 18, 60), bc[i], 24);
            }

            int gridN = 0; // сколько карточек в сетке — чтобы выровнять ряд по центру
            foreach (var pd in GameConfig.Products)
                if (!pd.id.StartsWith("dragon_egg") && pd.id != "x2_income" && pd.id != "x2_grow" && pd.kind != ProductKind.Coins && pd.kind != ProductKind.Speed) gridN++;
            const float gridY = -350f - 200f - 12f; // сетка — под блоком x2 (x2 сразу под Драконьим яйцом)
            // --- товары (Золотое яйцо, x2 прокачка, монеты, энергетик): сетка сразу под Драконьим яйцом ---
            int idx = 0;
            float cw = (w - 36) / 4f, ch = 250;
            foreach (var pd in GameConfig.Products)
            {
                if (pd.id.StartsWith("dragon_egg") || pd.id == "x2_income" || pd.id == "x2_grow") continue;
                if (pd.kind == ProductKind.Coins || pd.kind == ProductKind.Speed) continue; // монеты и энергетик убраны из магазина
                int col = idx % 4, row = idx / 4;
                var card = UIKit.Panel(content, "P" + pd.id, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - (Mathf.Min(gridN, 4) - 1) / 2f) * (cw + 12), gridY - row * (ch + 12)), new Vector2(cw, ch),
                    new Color(pd.color.r * 0.5f, pd.color.g * 0.5f, pd.color.b * 0.5f, 1f), 3f);
                var icon = pd.kind == ProductKind.Coins ? Icons.Coin : pd.kind == ProductKind.Speed ? Icons.Bolt : pd.kind == ProductKind.Egg ? Icons.Egg
                    : (pd.kind == ProductKind.NoAdsTimed || pd.id == "noads_forever") ? (Icons.Art("icon_noads") ?? Icons.Star) : Icons.Star;
                UIKit.Icon(card.transform, icon, new Vector2(0.5f, 1), new Vector2(0, -52), 78);
                // иконка 13..91, имя 96..132, описание 134..186, кнопка 191..241 (от верха карточки 250)
                UIKit.Fit(UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(cw - 10, 36)), Loc.Ru ? pd.nameRu : pd.nameEn, 18, Color.white), 12);
                UIKit.Fit(UIKit.Label(UIKit.Rect(card.transform, "D", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -134), new Vector2(cw - 12, 52)), Loc.Ru ? pd.descRu : pd.descEn, 14, new Color(0.92f, 0.92f, 1f)), 10);
                YanBuy(card.transform, pd.id, new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(cw - 24, 50), new Color(0.3f, 0.8f, 0.35f), 19);
                idx++;
            }
            // --- x2 Доход и x2 Рост: сразу под Драконьим яйцом ---
            float half = (w - 12) / 2f;
            var inc = UIKit.Panel(content, "X2Income", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-half / 2f - 6, -350f), new Vector2(half, 200), new Color(1f, 0.85f, 0.2f), 3f);
            UIKit.Icon(inc.transform, Icons.Coin, new Vector2(0, 0.5f), new Vector2(64, 14), 110);
            UIKit.Icon(inc.transform, Icons.Coin, new Vector2(0, 0.5f), new Vector2(100, -10), 110);
            X2Badge(inc.transform, new Vector2(150, -40));
            ArtOverlay(inc.transform, "store_x2_income", new Vector2(100, 0), 170);
            UIKit.Fit(UIKit.Label(UIKit.Rect(inc.transform, "T", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(210, 56)), Loc.Ru ? "x2 Доход" : "x2 Income", 32, Color.white, TextAnchor.MiddleRight), 20);
            YanBuy(inc.transform, "x2_income", new Vector2(1, 0), new Vector2(-110, 50), new Vector2(190, 62), new Color(0.25f, 0.8f, 0.3f), 24);
            var gr = UIKit.Panel(content, "X2Grow", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(half / 2f + 6, -350f), new Vector2(half, 200), new Color(0.4f, 0.85f, 0.95f), 3f);
            UIKit.Icon(gr.transform, IconArt.Egg(Tier.Rare), new Vector2(0, 0.5f), new Vector2(84, 6), 150);
            X2Badge(gr.transform, new Vector2(150, -40));
            ArtOverlay(gr.transform, "store_x2_grow", new Vector2(100, 0), 170);
            UIKit.Fit(UIKit.Label(UIKit.Rect(gr.transform, "T", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -8), new Vector2(230, 78)), Loc.Ru ? "x2 Скорость\nроста яиц" : "x2 Egg\nGrowth", 26, Color.white, TextAnchor.MiddleRight), 16);
            YanBuy(gr.transform, "x2_grow", new Vector2(1, 0), new Vector2(-110, 50), new Vector2(190, 62), new Color(0.25f, 0.8f, 0.3f), 24);

            // высота прокрутки: Драконье яйцо + сетка + блок x2
            int gridRows = (idx + 3) / 4;
            content.sizeDelta = new Vector2(body.sizeDelta.x, Mathf.Max(body.sizeDelta.y, -gridY + gridRows * (ch + 12) + 8));
            return go;
        }

        /// <summary>Если есть своя картинка Resources/Art/&lt;name&gt;.png — кладём её поверх нарисованной иконки.</summary>
        static void ArtOverlay(Transform parent, string name, Vector2 pos, float size)
        {
            var sp = Icons.Art(name);
            if (sp == null) return;
            foreach (Transform c in parent) if (c.name == "Icon" || c.name == "X2") c.gameObject.SetActive(false);
            UIKit.Icon(parent, sp, new Vector2(0, 0.5f), pos, size);
        }

        static void X2Badge(Transform parent, Vector2 pos)
        {
            var b = UIKit.Panel(parent, "X2", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(78, 50), new Color(0.95f, 0.25f, 0.35f), 3f);
            b.raycastTarget = false;
            b.transform.localRotation = Quaternion.Euler(0, 0, -10f);
            UIKit.Label(b.transform, "x2", 32, Color.white);
            if (UIKit.ApplySkin(b, "badge_x2", false)) { b.type = Image.Type.Simple; b.preserveAspect = true; UIKit.HideLabels(b.transform); }
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
                // от верха карточки (380): имя 8..46, редкость 46..70, превью 72..232, полоса скорости 238..288, кнопки 302..368
                UIKit.Fit(UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(cw - 16, 38)), Loc.Ru ? t.nameRu : t.nameEn, 26, Color.white), 16);
                UIKit.Fit(UIKit.Label(UIKit.Rect(card.transform, "R", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(cw - 16, 26)), Loc.Ru ? t.rarityRu : t.rarityEn, 17, Color.white), 12);
                // превью: кроссовок и "шлейф"
                for (int j = 0; j < 3; j++)
                {
                    var streak = UIKit.Panel(card.transform, "S" + j, new Vector2(0.5f, 1), new Vector2(1, 0.5f), new Vector2(24, -120 - j * 24), new Vector2(150 - j * 36, 16), Color.Lerp(t.b, t.a, j / 2f));
                    streak.raycastTarget = false;
                }
                var trailArt = Icons.Art("trail_" + i);
                var shoe = UIKit.Icon(card.transform, trailArt != null ? trailArt : Icons.Shoe, new Vector2(0.5f, 1), trailArt != null ? new Vector2(0, -152) : new Vector2(40, -140), trailArt != null ? 160 : 110);
                if (trailArt == null) shoe.color = Color.Lerp(t.a, Color.white, 0.4f);
                var band = UIKit.Panel(card.transform, "Band", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(cw - 20, 50), new Color(0, 0, 0, 0.25f));
                band.raycastTarget = false;
                UIKit.Fit(UIKit.Label(band.transform, Loc.Ru ? "Красивый след" : "Cool trail", 24, Color.white), 15); // трейлы скорость не дают
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
        /// <summary>
        /// Инвентарь яиц: карточка на каждый вид яйца (иконка, название, количество) — выбираешь яйцо и сажаешь его
        /// кнопкой "Посадить" на свободную грядку, или "Посадить все".
        /// </summary>
        GameObject BuildEggs()
        {
            RectTransform body;
            var go = Modal(Loc.Ru ? "Яйца" : "Eggs", new Color(0.9f, 0.2f, 0.2f), new Vector2(780, 600), out body);
            // тело 740x480 (от верха): сетка 0..300, текст 306..404, кнопки 416..480
            eggsGrid = UIKit.Scroll(body, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(body.sizeDelta.x, 300), false);
            eggsBody = UIKit.Fit(UIKit.Label(UIKit.Rect(body, "T", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -306), new Vector2(body.sizeDelta.x - 20, 98)), "", 20, Color.white), 13);
            eggsPlant = UIKit.Button(body, "Plant", Loc.Ru ? "Посадить" : "Plant", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-150, 0), new Vector2(280, 62),
                new Color(0.3f, 0.8f, 0.35f), () => { if (eggSel != int.MinValue) GameManager.Instance.PlantFromInventory(eggSel); eggsSig = null; RefreshPanels(); }, 26);
            eggsPlantAll = UIKit.Button(body, "PlantAll", Loc.Ru ? "Посадить все" : "Plant all", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(150, 0), new Vector2(280, 62),
                new Color(1f, 0.6f, 0.15f), () => { GameManager.Instance.PlantAllFromInventory(); eggsSig = null; RefreshPanels(); }, 26);
            return go;
        }

        static string EggKindName(int key) { return key == -1 ? (Loc.Ru ? "Драконье яйцо" : "Dragon Egg") : Loc.TierName((Tier)key); }

        void RefreshEggs(SaveData d, GameManager gm)
        {
            // виды яиц и количество: сначала Драконьи, потом по убыванию тира
            var counts = new SortedDictionary<int, int>(Comparer<int>.Create((x, y) => (x == -1 ? 100 : x) == (y == -1 ? 100 : y) ? 0 : ((x == -1 ? 100 : x) > (y == -1 ? 100 : y) ? -1 : 1)));
            foreach (var e in d.inventory) { int k = GameManager.EggKey(e); int c; counts.TryGetValue(k, out c); counts[k] = c + 1; }
            if (!counts.ContainsKey(eggSel)) { eggSel = int.MinValue; foreach (var kv in counts) { eggSel = kv.Key; break; } }

            var sig = new StringBuilder();
            foreach (var kv in counts) sig.Append(kv.Key).Append(':').Append(kv.Value).Append(';');
            if (sig.ToString() != eggsSig)
            {
                eggsSig = sig.ToString();
                foreach (Transform c in eggsGrid) Destroy(c.gameObject);
                eggCards.Clear();
                float cw = 132, ch = 146, gap = 10;
                int cols = Mathf.Max(1, Mathf.FloorToInt((eggsGrid.sizeDelta.x + gap) / (cw + gap)));
                int i = 0;
                foreach (var kv in counts)
                {
                    int key = kv.Key, col = i % cols, row = i / cols;
                    var b = UIKit.Button(eggsGrid, "Egg" + key, "", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                        new Vector2((col - (cols - 1) / 2f) * (cw + gap), -6 - row * (ch + gap)), new Vector2(cw, ch), new Color(0.22f, 0.17f, 0.38f), () => { eggSel = key; RefreshPanels(); }, 16);
                    Destroy(b.GetComponent<ButtonBounce>());
                    UIKit.Icon(b.transform, IconArt.Egg(key == -1 ? Tier.Legendary : (Tier)key, key == -1), new Vector2(0.5f, 1), new Vector2(0, -50), 78);
                    UIKit.Fit(UIKit.Label(UIKit.Rect(b.transform, "N", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(cw - 12, 26)), EggKindName(key), 16, Color.white), 10);
                    UIKit.Fit(UIKit.Label(UIKit.Rect(b.transform, "C", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(cw - 12, 28)), "x" + kv.Value, 22, Gold), 14);
                    eggCards.Add(new KeyValuePair<int, Image>(key, b.GetComponent<Image>()));
                    i++;
                }
                int rows = (counts.Count + cols - 1) / cols;
                eggsGrid.sizeDelta = new Vector2(eggsGrid.sizeDelta.x, Mathf.Max(300f, 12 + rows * (ch + gap)));
            }
            foreach (var kv in eggCards)
                if (kv.Value != null) kv.Value.color = kv.Key == eggSel ? new Color(1f, 0.78f, 0.2f) : new Color(0.22f, 0.17f, 0.38f);

            int free = gm.FreePlotCount;
            string freeTxt = Loc.Ru ? "Свободных грядок: " + free : "Free plots: " + free;
            if (counts.Count == 0)
                eggsBody.text = Loc.Ru ? "Яиц в инвентаре нет.\nУкради яйцо у брейнротов или купи Драконье яйцо в магазине!" : "No eggs.\nSteal one from brainrots or get a Dragon Egg in the store!";
            else if (eggSel == -1)
                eggsBody.text = (Loc.Ru ? "<color=#FFD24A>Драконье яйцо</color> — эксклюзивный дракон, растёт 1 мин\n" : "<color=#FFD24A>Dragon Egg</color> — exclusive dragon, grows 1 min\n") + freeTxt;
            else
            {
                var ti = GameConfig.GetTier(eggSel);
                eggsBody.text = (Loc.Ru ? "Яйцо «" + EggKindName(eggSel) + "» — растёт " : EggKindName(eggSel) + " egg — grows ") + Loc.Time(ti.growSeconds * gm.GrowFactor) + "\n" + freeTxt;
            }
            eggsPlant.interactable = counts.Count > 0 && free > 0;
            eggsPlantAll.interactable = counts.Count > 0 && free > 0;
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
                var card = UIKit.Panel(body, "Day" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - (n - 1) / 2f) * (cw + 6), -6), new Vector2(cw, 220), new Color(0.2f, 0.19f, 0.3f), 2f);
                dayCards[i] = card;
                UIKit.Fit(UIKit.Label(UIKit.Rect(card.transform, "H", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(cw - 4, 32)), (Loc.Ru ? "День " : "Day ") + (i + 1), 17, Gold), 12);
                UIKit.Icon(card.transform, i == 3 || i == 6 ? Icons.Egg : (i == 1 || i == 5 ? Icons.Bolt : Icons.Coin), new Vector2(0.5f, 1), new Vector2(0, -68), 56);
                dayTexts[i] = UIKit.Fit(UIKit.Inset(UIKit.Label(UIKit.Rect(card.transform, "T", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(cw - 6, 110)), "", 13, Color.white), 4, 4), 10);
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
            UIKit.Fit(UIKit.Label(UIKit.Rect(body, "SwT", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -98), new Vector2(540, 40)), Loc.T("switch_sound"), 22, Gold), 14);
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

        /// <summary>
        /// Продавец: продаются только драконы из сумки (хранилища). Карточки 5×3 с ценой — нажми, чтобы выбрать
        /// (можно несколько), затем "Продать выбранных" или "Продать всех".
        /// </summary>
        GameObject BuildSell()
        {
            RectTransform body;
            var go = Modal(Loc.T("sell_title"), new Color(1f, 0.75f, 0.2f), new Vector2(900, mobile ? 600 : 620), out body);
            float bwid = body.sizeDelta.x, bh = body.sizeDelta.y;
            // тело (от верха): подсказка 0..30, сетка 36..(bh-76), кнопки bh-64..bh
            sellInfo = UIKit.Fit(UIKit.Label(UIKit.Rect(body, "Info", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(bwid, 30)), "", 19, new Color(0.8f, 0.85f, 1f)), 12);
            int n = GameConfig.StorageSlots;
            float gridH = bh - 36 - 76, gap = 8;
            float ch = Mathf.Min(128f, (gridH - 2 * gap) / 3f), cw = (bwid - 4 * gap) / 5f;
            sellCards = new Image[n]; sellIcons = new Image[n]; sellNames = new Text[n]; sellPrices = new Text[n]; sellChecks = new Text[n];
            for (int i = 0; i < n; i++)
            {
                int k = i, col = i % 5, row = i / 5;
                var b = UIKit.Button(body, "S" + i, "", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((col - 2) * (cw + gap), -36 - row * (ch + gap)), new Vector2(cw, ch),
                    new Color(0.22f, 0.2f, 0.34f), () => { if (!sellSel.Remove(k)) sellSel.Add(k); RefreshPanels(); }, 14);
                Destroy(b.GetComponent<ButtonBounce>());
                sellCards[i] = b.GetComponent<Image>();
                sellIcons[i] = UIKit.Icon(b.transform, Icons.Dragon, new Vector2(0, 0.5f), new Vector2(ch * 0.36f + 4, 4), ch * 0.7f);
                sellNames[i] = UIKit.Fit(UIKit.Label(UIKit.Rect(b.transform, "N", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -8), new Vector2(cw - ch * 0.72f - 10, ch * 0.45f)), "", 15, Color.white, TextAnchor.UpperLeft), 9);
                sellPrices[i] = UIKit.Fit(UIKit.Label(UIKit.Rect(b.transform, "P", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 8), new Vector2(cw - ch * 0.72f - 10, ch * 0.34f)), "", 18, new Color(0.6f, 1f, 0.5f), TextAnchor.LowerLeft), 11);
                sellChecks[i] = UIKit.Label(UIKit.Rect(b.transform, "V", new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -2), new Vector2(cw * 0.6f, 22)), Loc.Ru ? "ВЫБРАН" : "SELECTED", 13, Color.white, TextAnchor.UpperLeft);
            }
            var selBtn = UIKit.Button(body, "SellSel", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-bwid * 0.25f, 0), new Vector2(bwid * 0.46f, 60),
                new Color(1f, 0.7f, 0.15f), () => { GameManager.Instance.SellStored(new List<int>(sellSel)); sellSel.Clear(); RefreshPanels(); }, 22);
            sellSelText = selBtn.GetComponentInChildren<Text>(); sellSelBtn = selBtn;
            var all = UIKit.Button(body, "All", "", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(bwid * 0.25f, 0), new Vector2(bwid * 0.46f, 60),
                new Color(0.95f, 0.35f, 0.25f), () => { GameManager.Instance.SellStored(null); sellSel.Clear(); RefreshPanels(); }, 22);
            sellAllText = all.GetComponentInChildren<Text>(); sellAllBtn = all;
            sellEmpty = UIKit.Label(UIKit.Rect(body, "E", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 140)), Loc.T("sell_empty"), 22, new Color(0.85f, 0.85f, 0.95f));
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

        float hudTextTimer;
        /// <summary>Меняем текст, только если он действительно изменился (иначе uGUI перестраивает меш).</summary>
        static void SetText(Text t, string s) { if (t != null && t.text != s) t.text = s; }

        void Update()
        {
            var gm = GameManager.Instance;
            var d = SaveManager.Data;
            if (gm == null) return;
            InputState.Blocked = (menu != null && menu.activeSelf) || Roulette.Active || ResponsiveCanvas.RotateBlocking;

            // тексты HUD — 8 раз в секунду, а не каждый кадр (меньше мусора и перестроек канваса)
            hudTextTimer -= Time.unscaledDeltaTime;
            if (hudTextTimer <= 0)
            {
                hudTextTimer = 0.125f;
                SetText(coinsText, "$" + Loc.Num(d.coins));
                SetText(cpsText, "+$" + Loc.Num(gm.CoinsPerSec) + (Loc.Ru ? " /сек" : " /s"));
                SetText(speedText, Loc.Num(d.speedPoints));
                SetText(invText, d.inventory.Count > 0 ? Loc.F("inventory", d.inventory.Count) : "");
            }

            if (toastTimer > 0)
            {
                toastTimer -= Time.unscaledDeltaTime;
                toastBg.transform.localScale = Vector3.one * Mathf.MoveTowards(toastBg.transform.localScale.x, 1f, Time.unscaledDeltaTime);
                if (toastTimer <= 0) toastBg.gameObject.SetActive(false);
            }

            // обучение
            var tut = Tutorial.Instance;
            bool showTut = tut != null && tut.ShowPanel && hud.activeSelf; // не во время рулетки и обучения управлению
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
                // над кнопкой прыжка (она 40..170 от низа): кнопка действия 180..264
                promptRt.anchoredPosition = new Vector2(-190, 222);
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
            }
            if (rebirthPanel.activeSelf)
            {
                float next = GameConfig.RebirthMultiplier(d.rebirths + 1);
                string zoneReq = (gm.RebirthZoneOk ? "<color=#7CFF7C>" : "<color=#FF7C7C>") + (Loc.Ru ? "Нужна зона: " : "Zone required: ") + Loc.TierName((Tier)gm.RebirthReqTier) + "</color>";
                rebirthBody.text = Loc.F("rebirth_desc", next.ToString("0")) + "\n\n" + zoneReq + "\n<color=#FFD84A>" + Loc.F("rebirth_cost", Loc.Num(gm.RebirthCost)) + "</color>";
            }
            if (dragonsPanel.activeSelf) RefreshDragons(d, gm);
            if (yanPanel.activeSelf)
            {
                if (!store3DDone)
                {
                    store3DDone = true;
                    foreach (var kv in store3D) { var sp3 = ModelRenderer.Dragon(kv.Value); if (sp3 != null) kv.Key.sprite = sp3; }
                }
                foreach (var pd in GameConfig.Products)
                {
                    Text t; Button bt;
                    if (!yanPriceById.TryGetValue(pd.id, out t)) continue;
                    bool owned = pd.kind == ProductKind.Permanent && gm.Owns(pd.id);
                    t.text = owned ? (Loc.Ru ? "Куплено" : "Owned") : YandexSDK.PriceText(pd);
                    // "Без рекламы 2 часа": пока действует — показываем остаток (можно докупить, время складывается)
                    if (pd.kind == ProductKind.NoAdsTimed && gm.NoAdsSecondsLeft > 0 && !gm.Owns("noads_forever"))
                        t.text = YandexSDK.PriceText(pd) + "  (" + Loc.Time(gm.NoAdsSecondsLeft) + ")";
                    if (pd.kind == ProductKind.NoAdsTimed && gm.Owns("noads_forever")) { t.text = Loc.Ru ? "Не нужно" : "Not needed"; owned = true; }
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
            if (eggsPanel.activeSelf) RefreshEggs(d, gm);
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
                int count = d.dragonStore.Count; double total = 0, selTotal = 0;
                sellSel.RemoveWhere(x => x >= count);
                for (int i = 0; i < sellCards.Length; i++)
                {
                    bool has = i < count;
                    sellCards[i].gameObject.SetActive(has);
                    if (!has) continue;
                    var def = GameConfig.GetDragon(d.dragonStore[i]);
                    double price = gm.StoreSellPrice(i);
                    total += price;
                    bool sel = sellSel.Contains(i);
                    if (sel) selTotal += price;
                    var tc = GameConfig.GetTier(def.tier).color;
                    sellCards[i].color = sel ? new Color(1f, 0.78f, 0.2f) : Color.Lerp(new Color(0.2f, 0.19f, 0.32f), tc, 0.25f);
                    sellIcons[i].sprite = IconArt.Dragon(def);
                    sellNames[i].text = Loc.DragonName(def) + "\n<size=12><color=#FFD84A>" + (Loc.Ru ? "ур." : "lv.") + d.dragonStoreLvl[i] + "</color> " + Loc.TierName(def.tier) + "</size>";
                    sellPrices[i].text = "$" + Loc.Num(price);
                    sellChecks[i].enabled = sel;
                }
                sellInfo.text = (Loc.Ru ? "В сумке: " : "In bag: ") + count + "/" + GameConfig.StorageSlots
                    + (Loc.Ru ? "  —  нажми на драконов, чтобы выбрать" : "  —  tap dragons to select");
                sellSelText.text = (Loc.Ru ? "Продать выбранных " : "Sell selected ") + (sellSel.Count > 0 ? "($" + Loc.Num(selTotal) + ")" : "");
                sellSelBtn.interactable = sellSel.Count > 0;
                sellAllText.text = Loc.F("sell_all", "$" + Loc.Num(total));
                sellSelBtn.gameObject.SetActive(count > 0);
                sellAllBtn.gameObject.SetActive(count > 0);
                sellInfo.gameObject.SetActive(count > 0);
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

    /// <summary>Блик, пробегающий по витрине (как в роблокс-магазинах).</summary>
    public class ShineSweep : MonoBehaviour
    {
        RectTransform shine;
        void Start()
        {
            var rt = (RectTransform)transform;
            var s = UIKit.Rect(rt, "Shine", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, rt.rect.height * 1.4f));
            var img = s.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.12f);
            img.raycastTarget = false;
            s.localRotation = Quaternion.Euler(0, 0, -20f);
            s.SetAsFirstSibling();
            shine = s;
            gameObject.AddComponent<RectMask2D>();
        }
        void Update()
        {
            if (shine == null) return;
            float w = ((RectTransform)transform).rect.width;
            float t = Mathf.Repeat(Time.unscaledTime * 0.35f, 1.6f);
            shine.anchoredPosition = new Vector2(-100 + t * (w + 200) / 1.6f, 0);
        }
    }

    public class PulseScale : MonoBehaviour
    {
        void Update() { transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f); }
    }
}
