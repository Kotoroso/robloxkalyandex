using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DragonHeist.EditorTools
{
    /// <summary>
    /// Авто-настройка проекта под Яндекс Игры + сборка одной кнопкой.
    /// Меню: Dragon Heist → ...
    /// Командная строка: Unity -batchmode -projectPath . -executeMethod DragonHeist.EditorTools.YandexBuild.BuildBatch -quit
    /// </summary>
    [InitializeOnLoad]
    public static class YandexBuild
    {
        const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        const string OutDir = "Builds/YandexWebGL";

        static YandexBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("DH_Setup", false)) return;
                SessionState.SetBool("DH_Setup", true);
                EnsureScene();
                EnsureInputHandling();
                EnsureAlwaysIncludedShaders();
            };
        }

        [MenuItem("Dragon Heist/1. Настроить проект под Яндекс Игры")]
        public static void Setup()
        {
            EnsureAlwaysIncludedShaders();
            EnsureScene();
            EnsureInputHandling();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            PlayerSettings.companyName = "Kotoroso";
            PlayerSettings.productName = "Укради Дракона";
            PlayerSettings.colorSpace = ColorSpace.Gamma;             // быстрее на слабых телефонах
            PlayerSettings.runInBackground = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.WebGL.template = "PROJECT:Yandex";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;       // работает на любом хостинге
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
#else
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Medium);
#endif
            AssetDatabase.SaveAssets();
            Debug.Log("[Dragon Heist] Проект настроен под WebGL / Яндекс Игры.");
        }

        [MenuItem("Dragon Heist/2. Собрать для Яндекс Игр")]
        public static void BuildMenu()
        {
            if (Build())
            {
                EditorUtility.RevealInFinder(OutDir + "/index.html");
                EditorUtility.DisplayDialog("Dragon Heist",
                    "Готово! Заархивируй СОДЕРЖИМОЕ папки " + OutDir + " (index.html должен быть в корне zip) и загрузи в консоль Яндекс Игр.", "OK");
            }
        }

        [MenuItem("Dragon Heist/Открыть главную сцену")]
        public static void OpenScene()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildBatch()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build()
        {
            Setup();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("[Dragon Heist] Build " + report.summary.result + " size=" + report.summary.totalSize / (1024 * 1024) + "MB");
            return ok;
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                // туман включён в сцене, иначе Unity вырежет fog-варианты шейдеров из сборки
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.62f, 0.8f, 1f);
                RenderSettings.fogStartDistance = 120f;
                RenderSettings.fogEndDistance = 400f;
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        /// <summary>
        /// Шейдеры, которые игра ищет через Shader.Find (частицы, неон, прозрачность, небо) —
        /// без "Always Included" Unity вырежет их из WebGL-сборки.
        /// </summary>
        static void EnsureAlwaysIncludedShaders()
        {
            string[] names =
            {
                "Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Particles/Additive",
                "Legacy Shaders/Transparent/Diffuse", "Unlit/Color", "Skybox/Procedural"
            };
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            bool changed = false;
            foreach (var n in names)
            {
                var sh = Shader.Find(n);
                if (sh == null) continue;
                bool exists = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { exists = true; break; }
                if (exists) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Dragon Heist] Шейдеры добавлены в Always Included.");
            }
        }

        /// <summary>Игра использует старый Input Manager. Если в проекте включён только новый Input System — включаем "Both".</summary>
        static void EnsureInputHandling()
        {
            var ps = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (ps == null) return;
            var so = new SerializedObject(ps);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue == 1)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                if (EditorUtility.DisplayDialog("Dragon Heist",
                        "Включён режим ввода 'Both' (старый + новый Input System). Нужно перезапустить Unity.", "Перезапустить", "Позже"))
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
        }
    }
}
