using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// 3D-картинки драконов для витрины магазина: настоящая модель рендерится отдельной камерой в текстуру.
    /// Вызывается только когда мир уже построен (при открытии магазина). Лампы студии создаются лениво
    /// и пересоздаются, если их кто-то удалил — никаких MissingReferenceException.
    /// </summary>
    public static class ModelRenderer
    {
        static Camera cam;
        static RenderTexture rt;
        static Light keyLight, fillLight;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Vector3 Studio = new Vector3(0, -600f, 0);
        const int Size = 256;

        static void Ensure()
        {
            if (cam == null)
            {
                var go = new GameObject("ModelRenderCamera");
                Object.DontDestroyOnLoad(go);
                cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
                cam.fieldOfView = 24f;
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 80f;
            }
            if (rt == null)
            {
                rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 4;
            }
            cam.targetTexture = rt;
            keyLight = MakeLight(keyLight, "ModelKeyLight", new Vector3(-8f, 10f, -12f), 1.7f, Color.white);
            fillLight = MakeLight(fillLight, "ModelFillLight", new Vector3(10f, 4f, 6f), 1.1f, new Color(0.7f, 0.85f, 1f));
        }

        static Light MakeLight(Light l, string name, Vector3 offset, float intensity, Color color)
        {
            if (l == null)
            {
                var go = new GameObject(name);
                Object.DontDestroyOnLoad(go);
                l = go.AddComponent<Light>();
            }
            l.type = LightType.Point;
            l.range = 50f;
            l.intensity = intensity;
            l.color = color;
            l.transform.position = Studio + offset;
            l.enabled = false;
            return l;
        }

        /// <summary>3D-картинка дракона (кэш). null — если что-то пошло не так (тогда используется 2D-иконка).</summary>
        public static Sprite Dragon(DragonDef d)
        {
            string key = "d" + d.id;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            // своя картинка (например из нейросети) важнее рендера
            var tex = Resources.Load<Texture2D>("Art/dragon_" + d.id);
            if (tex != null) { s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)); cache[key] = s; return s; }
            try
            {
                var model = Blocky.BuildDragon(null, d);
                s = Render(model, 35f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("ModelRenderer: " + e.Message);
                s = null;
            }
            cache[key] = s;
            return s;
        }

        /// <summary>Рендер модели целиком: камера сама подбирает расстояние по габаритам (крылья, хвост, корона — всё в кадре).</summary>
        static Sprite Render(GameObject model, float yaw)
        {
            Ensure();
            foreach (var ps in model.GetComponentsInChildren<ParticleSystem>()) ps.gameObject.SetActive(false);
            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            model.transform.position = Studio;
            model.transform.rotation = Quaternion.Euler(0, 180f + yaw, 0);

            // габариты всех видимых частей (без частиц и прозрачных квадов-нимбов)
            bool has = false;
            var b = new Bounds(Studio, Vector3.zero);
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.gameObject.activeInHierarchy) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) b = new Bounds(Studio + Vector3.up * 2f, Vector3.one * 4f);
            float radius = b.extents.magnitude;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            Vector3 dir = new Vector3(0.25f, 0.3f, -1f).normalized;
            cam.transform.position = b.center + dir * dist;
            cam.transform.LookAt(b.center);
            cam.nearClipPlane = Mathf.Max(0.1f, dist - radius * 2f);
            cam.farClipPlane = dist + radius * 2f;

            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            keyLight.enabled = true; fillLight.enabled = true;
            cam.Render();
            keyLight.enabled = false; fillLight.enabled = false;
            RenderSettings.fog = fog;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            model.SetActive(false);
            Object.Destroy(model);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
