using UnityEngine;

namespace DragonHeist
{
    /// <summary>Камера как в Роблоксе: орбита вокруг персонажа, ПКМ/свайп — поворот, колесо/щипок — зум.</summary>
    public class CameraRig : MonoBehaviour
    {
        public static Camera Cam;
        public Transform target;
        public float yaw = 0f, pitch = 20f, distance = 14f;
        const float MinDist = 4f, MaxDist = 32f;
        float shake;

        public static CameraRig Create(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            else if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 420f;
            cam.fieldOfView = 70f;
            Cam = cam;
            var rig = cam.gameObject.AddComponent<CameraRig>();
            rig.target = target;
            return rig;
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        /// <summary>В главном меню камера медленно облетает базу.</summary>
        public bool MenuOrbit;

        void LateUpdate()
        {
            if (target == null) return;
            if (MenuOrbit)
            {
                float a = Time.unscaledTime * 0.08f;
                Vector3 center = new Vector3(0, 0, -8f);
                transform.position = center + new Vector3(Mathf.Sin(a) * 38f, 22f, -Mathf.Cos(a) * 38f);
                transform.rotation = Quaternion.LookRotation(center + Vector3.up * 2f - transform.position);
                yaw = transform.eulerAngles.y;
                return;
            }
            var look = InputState.LookDelta;
            yaw += look.x * 0.25f;
            pitch = Mathf.Clamp(pitch - look.y * 0.2f, -10f, 75f);
            distance = Mathf.Clamp(distance + InputState.Zoom * 1.5f, MinDist, MaxDist);

            Vector3 focus = target.position + Vector3.up * 2.6f;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * Vector3.back;
            float d = distance;
            RaycastHit hit;
            if (Physics.SphereCast(focus, 0.3f, dir, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(1.5f, hit.distance - 0.2f);
            Vector3 pos = focus + dir * d;
            if (shake > 0)
            {
                pos += Random.insideUnitSphere * shake;
                shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2f);
            }
            transform.position = pos;
            transform.rotation = rot;
        }
    }
}
