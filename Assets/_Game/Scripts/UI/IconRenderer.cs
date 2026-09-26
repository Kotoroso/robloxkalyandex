using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// 3D-миниатюры для интерфейса: настоящие модели драконов и яиц рендерятся отдельной камерой
    /// в текстуру (один раз, с кэшем) — в UI видны те же объёмные модели, что и в мире.
    /// </summary>
    public static class IconRenderer
    {
        static Camera cam;
        static RenderTexture rt;
        static Light keyLight, rimLight;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Vector3 Studio = new Vector3(0, -500f, 0);
        const int Size = 192;

        static void Ensure()
        {
            if (cam != null) return;
            var go = new GameObject("IconCamera");
            Object.DontDestroyOnLoad(go);
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 60f;
            rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            // свои лампы, чтобы миниатюры были яркими и объёмными
            keyLight = new GameObject("IconKeyLight").AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 40f;
            keyLight.intensity = 1.6f;
            keyLight.transform.position = Studio + new Vector3(-6f, 8f, -10f);
            Object.DontDestroyOnLoad(keyLight.gameObject);
            rimLight = new GameObject("IconRimLight").AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = 40f;
            rimLight.intensity = 1.1f;
            rimLight.color = new Color(0.7f, 0.85f, 1f);
            rimLight.transform.position = Studio + new Vector3(8f, 5f, 8f);
            Object.DontDestroyOnLoad(rimLight.gameObject);
            keyLight.enabled = false; rimLight.enabled = false;
        }

        public static Sprite Dragon(DragonDef d)
        {
            string key = "d" + d.id;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var model = Blocky.BuildDragon(null, d);
            s = Render(model, 3.2f + (int)d.tier * 0.12f, new Vector3(0, 1.6f, 0), 35f);
            cache[key] = s;
            return s;
        }

        public static Sprite Egg(Tier t, bool premium = false)
        {
            string key = "e" + (int)t + (premium ? "p" : "");
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var model = Blocky.BuildEgg(null, t, 1.6f);
            if (premium) foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Mats.EggMat(new Color(1f, 0.78f, 0.15f));
            s = Render(model, 1.9f, new Vector3(0, 1.05f, 0), 15f);
            cache[key] = s;
            return s;
        }

        /// <summary>Ставит модель в "студию", рендерит, читает пиксели в Sprite и удаляет модель.</summary>
        static Sprite Render(GameObject model, float distance, Vector3 focus, float yaw)
        {
            Ensure();
            // эффекты частиц и анимации в миниатюре не нужны
            foreach (var ps in model.GetComponentsInChildren<ParticleSystem>()) Object.DestroyImmediate(ps.gameObject);
            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            model.transform.position = Studio;
            model.transform.rotation = Quaternion.Euler(0, 180f + yaw, 0);

            Vector3 f = Studio + focus;
            cam.transform.position = f + new Vector3(0, distance * 0.35f, -distance * 3.2f);
            cam.transform.LookAt(f);

            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            keyLight.enabled = true; rimLight.enabled = true;
            cam.Render();
            keyLight.enabled = false; rimLight.enabled = false;
            RenderSettings.fog = fog;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Object.Destroy(model);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
