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
            GameplayStart();
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
        public static void ShowInterstitial()
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
