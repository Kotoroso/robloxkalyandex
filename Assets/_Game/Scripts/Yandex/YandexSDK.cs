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

        public static void GameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_GameReady(); } catch { }
#endif
            // GameplayAPI.start() вызывается, когда игрок нажимает "Играть" в главном меню
        }

        public static void GameplayStart()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_GameplayStart(); } catch { }
#endif
        }

        public static void GameplayStop()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_GameplayStop(); } catch { }
#endif
        }

        public static void LoadCloud()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_LoadData(); } catch { CloudLoaded = true; }
#else
            CloudLoaded = true;
#endif
        }

        public static void SaveCloud(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { YG_SaveData(json); } catch { }
#endif
        }

        /// <summary>Полноэкранная реклама (с кулдауном, как требует модерация).</summary>
        public static void ShowInterstitial(bool fromTimer = false)
        {
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

        static void ApplyPause()
        {
            bool pause = AdOpen || hidden;
            AudioListener.pause = pause;
            Time.timeScale = pause ? 0f : 1f;
        }

        // ===== Стики-баннер =====
        public static void ShowBanner()
        {
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

        public static void InitPayments()
        {
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
            if (Catalog.TryGetValue(id, out c) && !string.IsNullOrEmpty(c.priceValue)) return c.priceValue + (Loc.Ru ? " ян" : " YAN");
            return fallback + (Loc.Ru ? " ян" : " YAN");
        }

        public static void Purchase(string id)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
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
            bool consumable = gm.GrantProduct(id);
            if (consumable) Consume(token);
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
                    bool permanent = false;
                    foreach (var p in GameConfig.Products) if (p.id == it.id && p.kind == ProductKind.Permanent) permanent = true;
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
                if (GameManager.Instance != null) GameManager.Instance.RecalcStats();
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
            GameplayStop();
            ApplyPause();
        }

        public void OnAdClose(string _)
        {
            AdOpen = false;
            if (GameManager.Instance != null) GameManager.Instance.ResetAdTimer();
            ApplyPause();
            GameplayStart();
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
            CloudJson = json ?? "";
            CloudLoaded = true;
        }

        public void OnVisibility(string visible)
        {
            hidden = visible == "0";
            ApplyPause();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && GameManager.Instance != null) GameManager.Instance.SaveNow(true);
        }
    }
}
