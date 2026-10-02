using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Мост к Yandex Games SDK (Plugins/WebGL/YandexBridge.jslib + WebGLTemplates/Yandex/index.html).
    /// В редакторе и не-WebGL сборках всё эмулируется: реклама "просматривается" сразу.
    /// Объект обязан называться "YandexSDK" — на него шлёт SendMessage JS-код.
    /// </summary>
    public class YandexSDK : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void YG_GameReady();
        [DllImport("__Internal")] static extern void YG_GameplayStart();
        [DllImport("__Internal")] static extern void YG_GameplayStop();
        [DllImport("__Internal")] static extern void YG_ShowFullscreen();
        [DllImport("__Internal")] static extern void YG_ShowRewarded(int id);
        [DllImport("__Internal")] static extern void YG_SaveData(string json);
        [DllImport("__Internal")] static extern void YG_LoadData();
        [DllImport("__Internal")] static extern string YG_GetLang();
        [DllImport("__Internal")] static extern int YG_IsMobile();
        [DllImport("__Internal")] static extern int YG_IsReady();
        [DllImport("__Internal")] static extern void YG_ShowBanner();
        [DllImport("__Internal")] static extern void YG_HideBanner();
        [DllImport("__Internal")] static extern void YG_InitPayments();
        [DllImport("__Internal")] static extern void YG_Purchase(string id);
        [DllImport("__Internal")] static extern void YG_Consume(string token);
#endif

        public static YandexSDK Instance;
        public static bool AdOpen;
        public static bool CloudLoaded;
        public static string CloudJson = "";

        static Action<bool> rewardCallback;
        static bool rewardGranted;
        static float lastInterstitial = -999f;
        static bool hidden;

        // ===== Разметка геймплея (GameplayAPI) =====
        // Игра только сообщает, "хочет" ли она идти (gameplayWanted); фактический start/stop шлёт SyncGameplay():
        // геймплей идёт, только если LoadingAPI.ready() уже вызван и нет паузы (реклама / вкладка скрыта / game_api_pause / уведомление о рекламе).
        static bool gameplayWanted, gameplayRunning, readySent;
        static bool apiPaused, apiPauseWasRunning;   // событие SDK game_api_pause (реклама при старте, диалог покупки, смена вкладки)
        static bool adNotice;                         // 2-секундное предупреждение "Реклама через..." — игра на паузе

        /// <summary>Игра сейчас на паузе из-за рекламы, скрытой вкладки или события SDK.</summary>
        public static bool Paused { get { return AdOpen || hidden || apiPaused; } }

        /// <summary>Реклама (полноэкранная и баннер) отключена покупкой "Без рекламы".</summary>
        public static bool AdsDisabled { get { return GameManager.Instance != null && GameManager.Instance.AdsDisabled; } }

        // ===== Облачные сохранения =====
        public static bool CloudFailed;          // облако недоступно / не успело загрузиться — в облако не пишем, чтобы не затереть прогресс
        static bool cloudWriteBlocked;
        static string pendingCloudJson;
        static float lastCloudSave = -999f;
        const float CloudSaveInterval = 3f;       // не чаще раза в 3 с (лимиты player.setData)

        public static YandexSDK Create()
        {
            var go = new GameObject("YandexSDK");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<YandexSDK>();
            return Instance;
        }

        public static bool IsSdkReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return YG_IsReady() == 1; } catch { return false; }
#else
            return true;
#endif
        }

        public static string Lang()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { var l = YG_GetLang(); if (!string.IsNullOrEmpty(l)) return l; } catch { }
#endif
            var sys = Application.systemLanguage;
            return (sys == SystemLanguage.Russian || sys == SystemLanguage.Ukrainian || sys == SystemLanguage.Belarusian) ? "ru" : "en";
        }

        public static bool IsMobile()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { if (YG_IsMobile() == 1) return true; } catch { }
#endif
            return Application.isMobilePlatform;
        }

        /// <summary>
        /// LoadingAPI.ready(): вызывать один раз, когда мир построен и первый кадр отрисован — игрок уже может играть.
        /// Если SDK ещё не инициализировался, JS-мост вызовет ready() сразу после YaGames.init().
        /// </summary>
        public static void GameReady()
        {
            if (readySent) return;
            readySent = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_GameReady(); } catch { }
#endif
            SyncGameplay();
        }

        /// <summary>Игрок в игровом процессе (вызывает UI). Реальный GameplayAPI.start() — через SyncGameplay.</summary>
        public static void GameplayStart()
        {
            gameplayWanted = true;
            SyncGameplay();
        }

        /// <summary>Игрок вышел из игрового процесса (меню, пауза).</summary>
        public static void GameplayStop()
        {
            gameplayWanted = false;
            SyncGameplay();
        }

        static void SyncGameplay()
        {
            bool should = readySent && gameplayWanted && !Paused && !adNotice;
            if (should == gameplayRunning) return;
            gameplayRunning = should;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { if (should) YG_GameplayStart(); else YG_GameplayStop(); } catch { }
#endif
        }

        /// <summary>Предупреждение "Реклама через 2..." (требование 4.4: игра на паузе во время уведомления и показа).</summary>
        public static void SetAdNotice(bool on)
        {
            if (adNotice == on) return;
            adNotice = on;
            ApplyPause();
            SyncGameplay();
        }

        public static void LoadCloud()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_LoadData(); } catch { CloudFailed = true; CloudLoaded = true; }
#else
            CloudLoaded = true;
#endif
        }

        /// <summary>Облако не ответило до старта игры — в этой сессии пишем только локально, чтобы не затереть облачный прогресс.</summary>
        public static void BlockCloudWrites()
        {
            cloudWriteBlocked = true;
            Debug.LogWarning("Cloud save did not load in time: cloud writes disabled for this session");
        }

        /// <summary>Сохранение в облако (player.setData) с ограничением частоты; локальная копия пишется всегда (SaveManager).</summary>
        public static void SaveCloud(string json)
        {
            if (cloudWriteBlocked || CloudFailed) return;
            pendingCloudJson = json;
            if (Time.realtimeSinceStartup - lastCloudSave >= CloudSaveInterval) FlushCloud();
        }

        /// <summary>Немедленно отправить отложенное облачное сохранение (при скрытии вкладки).</summary>
        public static void FlushCloud()
        {
            if (pendingCloudJson == null) return;
            var json = pendingCloudJson;
            pendingCloudJson = null;
            lastCloudSave = Time.realtimeSinceStartup;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_SaveData(json); } catch { }
#endif
        }

        void Update()
        {
            if (pendingCloudJson != null && Time.realtimeSinceStartup - lastCloudSave >= CloudSaveInterval) FlushCloud();
        }

        /// <summary>
        /// Полноэкранная реклама — только через SDK, только в логической паузе (решает вызывающий код),
        /// не чаще кулдауна (SDK дополнительно ограничивает частоту сам). Не показывается при покупке "Без рекламы"
        /// и пока игра на паузе (вкладка скрыта, открыт диалог покупки и т.п.).
        /// </summary>
        public static void ShowInterstitial(bool fromTimer = false)
        {
            if (AdsDisabled || Paused) return;
            if (Time.realtimeSinceStartup - lastInterstitial < GameConfig.InterstitialCooldown) return;
            lastInterstitial = Time.realtimeSinceStartup;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_ShowFullscreen(); } catch { }
#endif
        }

        /// <summary>Реклама за вознаграждение. callback(true) если награда получена.</summary>
        public static void ShowRewarded(Action<bool> callback)
        {
            if (AdOpen) return;
            rewardCallback = callback;
            rewardGranted = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_ShowRewarded(1); }
            catch { FinishReward(false); }
#else
            rewardGranted = true;
            FinishReward(true);
#endif
        }

        static void FinishReward(bool ok)
        {
            var cb = rewardCallback;
            rewardCallback = null;
            if (cb != null) cb(ok);
        }

        /// <summary>Звук и игра на паузе во время рекламы / скрытой вкладки / game_api_pause; во время уведомления о рекламе — только игра.</summary>
        static void ApplyPause()
        {
            bool pause = Paused;
            AudioListener.pause = pause;
            Time.timeScale = (pause || adNotice) ? 0f : 1f;
        }

        // ===== Стики-баннер =====
        // В консоли разработчика: "Реклама" → "Sticky-баннеры" → включить позиции и опцию
        // "Использовать API для показа sticky-баннера", иначе showBannerAdv/hideBannerAdv не управляют баннером.
        public static void ShowBanner()
        {
            if (AdsDisabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_ShowBanner(); } catch { }
#endif
        }

        public static void HideBanner()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_HideBanner(); } catch { }
#endif
        }

        // ===== Покупки =====
        [Serializable] public class CatalogItem { public string id; public string price; public string priceValue; public string currency; }
        [Serializable] class CatalogList { public CatalogItem[] items; }
        [Serializable] class PurchaseItem { public string id; public string token; }
        [Serializable] class PurchaseList { public PurchaseItem[] items; }

        public static readonly System.Collections.Generic.Dictionary<string, CatalogItem> Catalog = new System.Collections.Generic.Dictionary<string, CatalogItem>();
        public static bool PaymentsReady;
        static readonly System.Collections.Generic.List<string> pendingOwned = new System.Collections.Generic.List<string>();
        static readonly System.Collections.Generic.HashSet<string> grantedTokens = new System.Collections.Generic.HashSet<string>();

        public static void InitPayments()
        {
            if (!GameConfig.PurchasesEnabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_InitPayments(); } catch { }
#else
            PaymentsReady = true;
#endif
        }

        public static string PriceText(ProductDef p) { return PriceText(p.id, p.fallbackPrice); }


        public static string PriceText(string id, string fallback)
        {
            CatalogItem c;
            // настоящая цена — из каталога Яндекса (требование 1.13.2: валюта портала определяется автоматически):
            // строка price из SDK уже содержит сумму и код валюты ("15 YAN").
            if (Catalog.TryGetValue(id, out c))
            {
                if (!string.IsNullOrEmpty(c.price)) return c.price;
                if (!string.IsNullOrEmpty(c.priceValue)) return c.priceValue + " " + (!string.IsNullOrEmpty(c.currency) ? c.currency : "YAN");
            }
            // каталог не загружен (редактор / покупки недоступны): цена по умолчанию в янах.
            // Никаких пересчётов в доллары — реальная цена всегда в валюте портала.
            return fallback + (Loc.Ru ? " ян" : " YAN");
        }

        /// <summary>Товар известен игре (в GameConfig.Products или трейлы).</summary>
        static bool KnownProduct(string id)
        {
            foreach (var p in GameConfig.Products) if (p.id == id) return true;
            foreach (var t in GameConfig.Trails) if (t.productId == id) return true;
            return false;
        }

        static bool IsPermanent(string id)
        {
            foreach (var p in GameConfig.Products) if (p.id == id) return p.kind == ProductKind.Permanent || p.kind == ProductKind.Trail;
            foreach (var t in GameConfig.Trails) if (t.productId == id) return true;
            return false;
        }

        public static void Purchase(string id)
        {
            if (!GameConfig.PurchasesEnabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!PaymentsReady)
            {
                if (UIManager.Instance != null) UIManager.Instance.Toast(Loc.Ru ? "Покупки сейчас недоступны" : "Purchases are unavailable right now", new Color(1f, 0.6f, 0.4f));
                InitPayments(); // повторная попытка инициализации
                return;
            }
            try { YG_Purchase(id); } catch { }
#else
            // в редакторе — сразу "покупаем"
            if (GameManager.Instance != null) GameManager.Instance.GrantProduct(id);
#endif
        }

        static void Consume(string token)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(token)) { try { YG_Consume(token); } catch { } }
#endif
        }

        static void Grant(string id, string token)
        {
            var gm = GameManager.Instance;
            if (gm == null) { pendingOwned.Add(id + "|" + token); return; }
            // один и тот же токен не выдаём дважды (повторный getPurchases, пока consume ещё в пути)
            if (!string.IsNullOrEmpty(token) && !grantedTokens.Add(token)) return;
            // все товары (включая новые noads_2h / noads_forever) выдаёт GameManager.GrantProduct
            bool consumable = gm.GrantProduct(id);
            // consume только для известных расходуемых товаров: постоянные (x2_*, trail_*, noads_forever) не расходуются
            // и восстанавливаются через getPurchases; неизвестный id не "сжигаем", чтобы не потерять покупку игрока.
            if (!KnownProduct(id)) { Debug.LogWarning("Unknown product id, not consumed: " + id); return; }
            if (consumable && !IsPermanent(id)) Consume(token);
        }

        /// <summary>Вызывается, когда игра загрузилась — выдаёт покупки, пришедшие раньше.</summary>
        public static void FlushPending()
        {
            var list = new System.Collections.Generic.List<string>(pendingOwned);
            pendingOwned.Clear();
            foreach (var s in list)
            {
                int k = s.IndexOf('|');
                Grant(s.Substring(0, k), s.Substring(k + 1));
            }
        }

        public void OnCatalog(string json)
        {
            try
            {
                var list = JsonUtility.FromJson<CatalogList>(json);
                if (list != null && list.items != null) foreach (var it in list.items) Catalog[it.id] = it;
                PaymentsReady = true;
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        /// <summary>Незавершённые покупки при старте: постоянные — восстанавливаем, расходуемые — выдаём и consume.</summary>
        public void OnPurchases(string json)
        {
            try
            {
                var list = JsonUtility.FromJson<PurchaseList>(json);
                if (list == null || list.items == null) return;
                foreach (var it in list.items)
                {
                    if (it == null || string.IsNullOrEmpty(it.id)) continue;
                    bool permanent = IsPermanent(it.id);
                    int trail = -1;
                    for (int ti = 0; ti < GameConfig.Trails.Length; ti++) if (GameConfig.Trails[ti].productId == it.id) trail = ti;
                    if (trail >= 0)
                    {
                        if (!SaveManager.Data.ownedTrails.Contains(trail)) SaveManager.Data.ownedTrails.Add(trail);
                    }
                    else if (permanent)
                    {
                        if (!SaveManager.Data.ownedProducts.Contains(it.id)) SaveManager.Data.ownedProducts.Add(it.id);
                    }
                    else Grant(it.id, it.token);
                }
                if (GameManager.Instance != null) { GameManager.Instance.RecalcStats(); GameManager.Instance.SaveNow(true); }
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        public void OnPurchaseSuccess(string json)
        {
            try
            {
                var it = JsonUtility.FromJson<PurchaseItem>(json);
                Grant(it.id, it.token);
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        public void OnPurchaseFailed(string id)
        {
            if (UIManager.Instance != null) UIManager.Instance.Toast(Loc.Ru ? "Покупка отменена" : "Purchase cancelled", new Color(1f, 0.6f, 0.4f));
        }

        // ===== Колбэки из JS (SendMessage) =====
        public void OnAdOpen(string _)
        {
            AdOpen = true;
            adNotice = false;
            ApplyPause();
            SyncGameplay();   // GameplayAPI.stop()
        }

        public void OnAdClose(string _)
        {
            AdOpen = false;
            if (GameManager.Instance != null) GameManager.Instance.ResetAdTimer();
            ApplyPause();
            SyncGameplay();   // GameplayAPI.start(), если игрок был в игре и пауз больше нет
        }

        /// <summary>
        /// ysdk.on('game_api_pause'): реклама при запуске игры (без колбэков), диалог покупки, смена вкладки,
        /// сворачивание окна. Платформа сама вызывает GameplayAPI.stop(), поэтому здесь только глушим звук и ставим паузу.
        /// </summary>
        public void OnGameApiPause(string _)
        {
            if (apiPaused) return;
            apiPaused = true;
            apiPauseWasRunning = gameplayRunning;
            gameplayRunning = false;   // stop() уже вызван платформой
            ApplyPause();
            if (GameManager.Instance != null) GameManager.Instance.SaveNow(true);
            FlushCloud();
        }

        /// <summary>ysdk.on('game_api_resume'): платформа сама вызывает start(), если геймплей шёл в момент паузы.</summary>
        public void OnGameApiResume(string _)
        {
            if (!apiPaused) return;
            apiPaused = false;
            gameplayRunning = apiPauseWasRunning;
            ApplyPause();
            SyncGameplay();
        }

        public void OnRewarded(string _) { rewardGranted = true; }

        public void OnRewardedClose(string _)
        {
            OnAdClose(_);
            FinishReward(rewardGranted);
        }

        public void OnRewardedError(string _)
        {
            OnAdClose(_);
            FinishReward(false);
        }

        public void OnCloudData(string json)
        {
            // пришло после старта игры (облако ответило слишком поздно) — прогресс уже выбран, в облако эту сессию не пишем
            if (cloudWriteBlocked) { Debug.LogWarning("Cloud save arrived late; ignored for this session"); return; }
            CloudJson = json ?? "";
            CloudLoaded = true;
        }

        /// <summary>player.getData не удался / игрок недоступен — работаем на локальном сохранении, облако не трогаем.</summary>
        public void OnCloudFailed(string _)
        {
            CloudFailed = true;
            CloudLoaded = true;
        }

        public void OnVisibility(string visible)
        {
            bool h = visible == "0";
            if (h == hidden) return;
            hidden = h;
            ApplyPause();
            SyncGameplay();
            if (hidden)
            {
                if (GameManager.Instance != null) GameManager.Instance.SaveNow(true);
                FlushCloud();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && GameManager.Instance != null) { GameManager.Instance.SaveNow(true); FlushCloud(); }
        }
    }
}
