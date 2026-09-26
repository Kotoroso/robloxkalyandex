using System.Collections;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Точка входа. Запускается автоматически в ЛЮБОЙ сцене (RuntimeInitializeOnLoadMethod),
    /// поэтому сцену руками собирать не нужно: всё — мир, персонаж, UI, звук — создаётся кодом.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        static bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (started) return;
            started = true;
            var go = new GameObject("GameBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
        }

        IEnumerator Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            YandexSDK.Create();

            // ждём инициализацию Yandex SDK (в редакторе — мгновенно)
            float t = 0;
            while (!YandexSDK.IsSdkReady() && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }

            string lang = YandexSDK.Lang();
            Loc.Ru = lang == "ru" || lang == "be" || lang == "kk" || lang == "uk" || lang == "uz";
            InputState.Mobile = YandexSDK.IsMobile();

            // облачное сохранение
            YandexSDK.LoadCloud();
            t = 0;
            while (!YandexSDK.CloudLoaded && t < 4f) { t += Time.unscaledDeltaTime; yield return null; }
            SaveManager.Resolve(YandexSDK.CloudJson);

            GameAudio.Create();
            UIManager.Create();
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            WorldBuilder.Build(gm);
            var player = PlayerController.Create(WorldBuilder.SpawnPoint);
            var rig = CameraRig.Create(player.transform);
            rig.yaw = 0f;
            UIManager.Instance.SetMenuCamera();
            gm.Init();
            Tutorial.Create();

            YandexSDK.GameReady();
            YandexSDK.InitPayments();
            YandexSDK.FlushPending();
            YandexSDK.ShowBanner();
            UIManager.Instance.StartGame();
        }

        void Update()
        {
            InputState.Poll();
        }

        void OnApplicationQuit()
        {
            if (GameManager.Instance != null) GameManager.Instance.SaveNow(false);
        }
    }
}
