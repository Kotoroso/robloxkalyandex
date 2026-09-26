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
            PlayerSettings.WebGL.nameFilesAsHashes = true;           // новое имя файла при изменении — кэш браузера/CDN не отдаст старую сборку
            EditorUserBuildSettings.development = false;
            // только WebGL 2.0 (OpenGLES3): без WebGL1-фолбэка и лишних вариантов шейдеров
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            // High: движок и BCL режутся сильнее (меньше wasm → быстрее загрузка); код игры защищён Assets/_Game/link.xml
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
#else
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
#endif
#if UNITY_2022_1_OR_NEWER
            // IL2CPP "Faster (smaller) builds": заметно меньше wasm, на скорость этой игры почти не влияет
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            // память: сразу 128 МБ (мир строится кодом — без серии дорогих ростов кучи при загрузке), рост геометрический, потолок 2 ГБ
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
#endif
            AssetDatabase.SaveAssets();
            Debug.Log("[Dragon Heist] Проект настроен под WebGL / Яндекс Игры.");
        }

        [MenuItem("Dragon Heist/2. Собрать для Яндекс Игр")]
        public static void BuildMenu()
        {
            if (Build())
            {
                EditorUtility.RevealInFinder(File.Exists(OutDir + ".zip") ? OutDir + ".zip" : OutDir + "/index.html");
                EditorUtility.DisplayDialog("Dragon Heist",
                    "Готово! Архив для загрузки: " + OutDir + ".zip — загрузи его в консоль Яндекс Игр (Черновик → Архив с игрой).", "OK");
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
            if (ok) MakeZip();
            return ok;
        }

        /// <summary>Готовый архив для загрузки в консоль Яндекс Игр: Builds/YandexWebGL.zip, index.html — в корне архива.</summary>
        static void MakeZip()
        {
            string zipPath = OutDir + ".zip";
            try
            {
                if (File.Exists(zipPath)) File.Delete(zipPath);
                string root = Path.GetFullPath(OutDir);
                using (var fs = new FileStream(zipPath, FileMode.Create))
                using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string rel = file.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/');
                        var entry = zip.CreateEntry(rel, System.IO.Compression.CompressionLevel.Optimal);
                        using (var es = entry.Open())
                        using (var src = File.OpenRead(file)) src.CopyTo(es);
                    }
                }
                Debug.Log("[Dragon Heist] Архив для Яндекс Игр: " + Path.GetFullPath(zipPath));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Dragon Heist] Не удалось создать zip: " + e.Message + ". Заархивируй содержимое папки " + OutDir + " вручную.");
            }
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

        /// <summary>Все шейдеры, которые игра берёт через Shader.Find (мир, неон, частицы, прозрачность, небо).</summary>
        static readonly string[] RuntimeShaders =
        {
            "Standard",                                   // весь мир: пластик, клавиши, персонажи, драконы
            "Legacy Shaders/Diffuse",                     // дешёвое освещение на слабых устройствах
            "Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Particles/Additive",
            "Legacy Shaders/Transparent/Diffuse", "Unlit/Color", "Unlit/Transparent", "Sprites/Default",
            "Skybox/Procedural"
        };
        const string ShaderRefDir = "Assets/_Game/Resources/ShaderRefs";

        /// <summary>
        /// Материалы создаются кодом во время игры, поэтому Unity при сборке "не видит" их шейдеры и вырезает —
        /// в WebGL всё становится розовым. Лечим двумя способами: (1) материал-заглушка на каждый шейдер в Resources
        /// (всё из Resources обязательно попадает в сборку), (2) шейдеры в списке Always Included.
        /// </summary>
        static void EnsureAlwaysIncludedShaders()
        {
            // (1) материалы-заглушки в Resources/ShaderRefs
            if (!AssetDatabase.IsValidFolder(ShaderRefDir))
            {
                Directory.CreateDirectory(ShaderRefDir);
                AssetDatabase.Refresh();
            }
            foreach (var n in RuntimeShaders)
            {
                var sh = Shader.Find(n);
                if (sh == null) continue;
                string path = ShaderRefDir + "/" + n.Replace("/", "_").Replace(" ", "") + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
                else if (m.shader != sh) { m.shader = sh; EditorUtility.SetDirty(m); }
            }
            AssetDatabase.SaveAssets();

            // (2) список Always Included Shaders в Project Settings → Graphics
            Object gs = UnityEngine.Rendering.GraphicsSettings.GetGraphicsSettings();
            if (gs == null) gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) { Debug.LogWarning("[Dragon Heist] Не нашёл GraphicsSettings — шейдеры включены только через Resources/ShaderRefs."); return; }
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            bool changed = false;
            foreach (var n in RuntimeShaders)
            {
                if (n == "Standard") continue; // Standard целиком в Always Included — очень долгая сборка; хватает заглушки в Resources
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
