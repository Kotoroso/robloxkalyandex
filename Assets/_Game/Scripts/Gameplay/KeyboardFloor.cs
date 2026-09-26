using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// ASMR-пол из механических клавиш: наступаешь — клавиша проваливается и щёлкает
    /// (звук выбирается в настройках: синий / коричневый / красный свитч). RGB-подсветка волной.
    /// </summary>
    public class KeyboardFloor : MonoBehaviour
    {
        int cols, rows;
        float pitch;
        Transform[] caps;
        Renderer[] glows;
        float[] depth;
        int pressed = -1;
        MaterialPropertyBlock mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const float CapTop = 0.75f;   // высота верха клавиши (по нему ходит игрок)
        const float Travel = 0.28f;

        static readonly Color[] RowColors =
        {
            new Color(1f, 0.55f, 0.6f), new Color(1f, 0.78f, 0.45f), new Color(1f, 0.95f, 0.55f),
            new Color(0.6f, 0.92f, 0.6f), new Color(0.55f, 0.8f, 1f), new Color(0.78f, 0.65f, 1f)
        };

        public static KeyboardFloor Build(Transform parent, Vector3 origin, int cols, int rows, float pitch)
        {
            var go = new GameObject("KeyboardFloor");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var k = go.AddComponent<KeyboardFloor>();
            k.cols = cols; k.rows = rows; k.pitch = pitch;
            float w = cols * pitch, h = rows * pitch;
            var tr = go.transform;

            // корпус клавиатуры + невидимый коллайдер на уровне верха клавиш
            Blocky.Round = true; Blocky.RoundFactor = 0.3f;
            Blocky.Part(tr, new Vector3(w / 2f, 0.2f, h / 2f), new Vector3(w + 1.2f, 0.4f, h + 1.2f), Mats.Plastic(new Color(0.16f, 0.16f, 0.2f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            var col = new GameObject("WalkCollider");
            col.transform.SetParent(tr, false);
            col.transform.localPosition = new Vector3(w / 2f, CapTop / 2f, h / 2f);
            var bc = col.AddComponent<BoxCollider>();
            bc.size = new Vector3(w + 1.2f, CapTop, h + 1.2f);

            int n = cols * rows;
            k.caps = new Transform[n];
            k.glows = new Renderer[n];
            k.depth = new float[n];
            var glowMat = Mats.Glow(Color.white);
            float cap = pitch * 0.86f;
            Blocky.Round = true; Blocky.RoundFactor = 0.22f; Blocky.RoundSteps = 1;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int i = r * cols + c;
                    var center = new Vector3((c + 0.5f) * pitch, 0, (r + 0.5f) * pitch);
                    // подсветка под клавишей
                    var g = Blocky.Part(tr, center + new Vector3(0, 0.42f, 0), new Vector3(pitch * 0.95f, 0.05f, pitch * 0.95f), glowMat);
                    k.glows[i] = g.GetComponent<Renderer>();
                    // клавиша: нижняя часть шире, верх уже — форма кейкапа
                    var capRoot = Blocky.Pivot(tr, "Key", center + new Vector3(0, 0.45f, 0));
                    var m = Mats.KeycapMat(RowColors[r % RowColors.Length]);
                    Blocky.Part(capRoot, new Vector3(0, 0.12f, 0), new Vector3(cap, 0.24f, cap), m);
                    Blocky.Part(capRoot, new Vector3(0, 0.27f, 0), new Vector3(cap * 0.8f, 0.08f, cap * 0.8f), Mats.KeycapMat(Color.Lerp(RowColors[r % RowColors.Length], Color.white, 0.35f)));
                    k.caps[i] = capRoot;
                }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;

            Blocky.Label(tr, Loc.T("asmr"), new Vector3(w / 2f, 3.2f, h + 0.8f), 1.3f, new Color(0.9f, 0.7f, 1f));
            return k;
        }

        void Update()
        {
            var p = PlayerController.Instance;
            int now = -1;
            if (p != null)
            {
                Vector3 lp = transform.InverseTransformPoint(p.transform.position);
                if (lp.y > 0.2f && lp.y < CapTop + 0.6f)
                {
                    int c = Mathf.FloorToInt(lp.x / pitch), r = Mathf.FloorToInt(lp.z / pitch);
                    if (c >= 0 && c < cols && r >= 0 && r < rows) now = r * cols + c;
                }
            }
            if (now != pressed)
            {
                int type = SaveManager.Data.switchType;
                if (pressed >= 0) GameAudio.PlaySwitch(type, false);
                if (now >= 0) GameAudio.PlaySwitch(type, true);
                pressed = now;
            }

            float dt = Time.deltaTime;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            float t = Time.time;
            for (int i = 0; i < caps.Length; i++)
            {
                float target = i == pressed ? Travel : 0f;
                depth[i] = Mathf.MoveTowards(depth[i], target, dt * (target > depth[i] ? 6f : 3f));
                var lp = caps[i].localPosition;
                lp.y = 0.45f - depth[i];
                caps[i].localPosition = lp;

                // RGB-волна, нажатая клавиша вспыхивает белым
                int c = i % cols, r = i / cols;
                var col = Color.HSVToRGB(Mathf.Repeat(t * 0.12f + c * 0.05f + r * 0.03f, 1f), 0.75f, 1f);
                if (depth[i] > 0.01f) col = Color.Lerp(col, Color.white, depth[i] / Travel);
                mpb.SetColor(ColorId, col);
                mpb.SetColor(BaseColorId, col);
                glows[i].SetPropertyBlock(mpb);
            }
        }
    }
}
