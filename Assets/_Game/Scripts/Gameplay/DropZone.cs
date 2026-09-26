using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Площадка сдачи яиц у ворот: светящаяся зелёная плита с золотой рамкой, крутящееся яйцо,
    /// стрелка вниз и столб света. Когда игрок несёт яйцо — всё ярко пульсирует и видно издалека.
    /// </summary>
    public class DropZone : MonoBehaviour
    {
        Transform arrow, egg, beam;
        Transform[] frame;
        float t;

        public static DropZone Build(Transform parent, Vector3 center, Vector2 size, float top)
        {
            var root = new GameObject("DropZone").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(center.x, top, center.z);
            var dz = root.gameObject.AddComponent<DropZone>();

            // плита: тёмно-зелёный бортик + светлая середина, чуть выше клавиш (как коврик)
            Blocky.Round = true; Blocky.RoundFactor = 0.25f;
            Blocky.Part(root, new Vector3(0, 0.06f, 0), new Vector3(size.x, 0.3f, size.y), Mats.Plastic(new Color(0.16f, 0.62f, 0.32f)));
            Blocky.Part(root, new Vector3(0, 0.12f, 0), new Vector3(size.x - 1.2f, 0.3f, size.y - 1.2f), Mats.Plastic(new Color(0.42f, 0.92f, 0.52f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            // шашечки-полоски "сюда", как на посадочной площадке
            var stripe = Mats.Plastic(new Color(0.3f, 0.82f, 0.42f));
            for (int i = 0; i < 4; i++)
                Blocky.Part(root, new Vector3(0, 0.28f, -size.y / 2f + 1.6f + i * (size.y - 3.2f) / 3f), new Vector3(size.x - 2.4f, 0.02f, 0.5f), stripe);
            // светящаяся золотая рамка (пульсирует)
            var gold = Mats.Glow(new Color(1f, 0.85f, 0.25f));
            dz.frame = new[]
            {
                Blocky.Part(root, new Vector3(0, 0.3f, -size.y / 2f + 0.3f), new Vector3(size.x - 0.2f, 0.08f, 0.35f), gold),
                Blocky.Part(root, new Vector3(0, 0.3f, size.y / 2f - 0.3f), new Vector3(size.x - 0.2f, 0.08f, 0.35f), gold),
                Blocky.Part(root, new Vector3(-size.x / 2f + 0.3f, 0.3f, 0), new Vector3(0.35f, 0.08f, size.y - 0.2f), gold),
                Blocky.Part(root, new Vector3(size.x / 2f - 0.3f, 0.3f, 0), new Vector3(0.35f, 0.08f, size.y - 0.2f), gold),
            };
            // крутящееся яйцо-подсказка на подставке
            Blocky.Part(root, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.15f, 1.6f), Mats.Plastic(new Color(1f, 0.85f, 0.3f)), false, PrimitiveType.Cylinder);
            dz.egg = Blocky.Pivot(root, "Egg", new Vector3(0, 0.7f, 0));
            Blocky.BuildEgg(dz.egg, Tier.Legendary, 1.1f);
            // стрелка вниз из кубиков
            dz.arrow = Blocky.Pivot(root, "Arrow", new Vector3(0, 5f, 0));
            var arrowMat = Mats.Glow(new Color(0.45f, 1f, 0.5f));
            Blocky.Part(dz.arrow, new Vector3(0, 0.7f, 0), new Vector3(0.55f, 1.4f, 0.55f), arrowMat);
            var h1 = Blocky.Part(dz.arrow, new Vector3(-0.38f, -0.2f, 0), new Vector3(0.5f, 1.3f, 0.5f), arrowMat); h1.localRotation = Quaternion.Euler(0, 0, 45);
            var h2 = Blocky.Part(dz.arrow, new Vector3(0.38f, -0.2f, 0), new Vector3(0.5f, 1.3f, 0.5f), arrowMat); h2.localRotation = Quaternion.Euler(0, 0, -45);
            // столб света (две скрещённые полупрозрачные плоскости)
            dz.beam = Blocky.Pivot(root, "Beam", Vector3.zero);
            var beamMat = Mats.UnlitAlpha(new Color(0.55f, 1f, 0.6f, 0.55f), Mats.BeamTexture);
            for (int i = 0; i < 2; i++)
            {
                var q = Blocky.Part(dz.beam, new Vector3(0, 7f, 0), new Vector3(Mathf.Min(size.x, size.y) * 0.8f, 14f, 1f), beamMat, false, PrimitiveType.Quad);
                q.localRotation = Quaternion.Euler(0, i * 90f, 0);
            }
            Blocky.NoShadows(dz.beam.gameObject);
            Blocky.NoShadows(dz.arrow.gameObject);

            // коллайдер, чтобы стоять на плите
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.05f, 0);
            col.size = new Vector3(size.x, 0.5f, size.y);
            return dz;
        }

        void Update()
        {
            t += Time.deltaTime;
            var p = PlayerController.Instance;
            bool carrying = p != null && p.Carrying != null;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * (carrying ? 6f : 2.5f));
            egg.localRotation = Quaternion.Euler(0, t * 60f, 0);
            egg.localPosition = new Vector3(0, 0.7f + Mathf.Sin(t * 2f) * 0.12f, 0);
            arrow.gameObject.SetActive(carrying);
            if (carrying)
            {
                arrow.localPosition = new Vector3(0, 4.2f + Mathf.Abs(Mathf.Sin(t * 3f)) * 1.2f, 0);
                arrow.localRotation = Quaternion.Euler(0, t * 90f, 0);
            }
            beam.localScale = new Vector3(1f, carrying ? 1f + pulse * 0.15f : 0.45f, 1f);
            float fy = 1f + pulse * (carrying ? 1.2f : 0.4f);
            foreach (var f in frame) f.localScale = new Vector3(1f, fy, 1f);
        }
    }
}
