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
            // (пока идёт ожидание, поверх висит экран загрузки Яндекса — LoadingAPI.ready() ещё не вызван)
            while (!YandexSDK.IsSdkReady() && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }

            string lang = YandexSDK.Lang();
            Loc.SetLanguage(lang); // ru/be/kk/uk/uz -> русский, остальное -> английский
            InputState.Mobile = YandexSDK.IsMobile();
            Perf.Init(InputState.Mobile); // качество под платформу + губернатор FPS (до постройки мира: от него зависят материалы и тени)

            // облачное сохранение
            YandexSDK.LoadCloud();
            t = 0;
            while (!YandexSDK.CloudLoaded && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            // облако не ответило вовремя — играем с локальным сохранением и не пишем в облако, чтобы не затереть прогресс
            if (!YandexSDK.CloudLoaded) YandexSDK.BlockCloudWrites();
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

            if (GameConfig.PurchasesEnabled) YandexSDK.InitPayments(); // покупки выключены — платежи не подключаем
            YandexSDK.FlushPending();
            YandexSDK.ShowBanner();          // не показывается, если куплено "Без рекламы"
            UIManager.Instance.StartGame();  // GameplayAPI.start() уйдёт только после LoadingAPI.ready()

            // LoadingAPI.ready() — строго когда игрок уже может играть: мир построен и первый кадр отрисован
            yield return null;
            yield return new WaitForEndOfFrame();
            YandexSDK.GameReady();
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
