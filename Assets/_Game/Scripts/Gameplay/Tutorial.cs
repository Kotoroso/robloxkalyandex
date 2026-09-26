using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Пошаговое обучение: тропа из светящихся стрелок от игрока к цели, кольцо и прыгающая стрелка над целью,
    /// панель с текстом сверху. Шаги: дорожка → украсть яйцо → отнести на базу → вырастить → готово.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public static Tutorial Instance;
        public const int Done = 5;

        readonly List<Transform> chevrons = new List<Transform>();
        Transform ring, beam, arrow;
        float lastStepChange;
        int stolenAtStep;
        const int MaxChevrons = 36;
        const float Spacing = 1.8f;

        public int Step { get { return SaveManager.Data.tutorialStep; } }
        public bool Active { get { return Step < Done; } }

        public static Tutorial Create()
        {
            var go = new GameObject("Tutorial");
            Instance = go.AddComponent<Tutorial>();
            Instance.Build();
            return Instance;
        }

        void Build()
        {
            var chevMat = Mats.UnlitAlpha(new Color(1f, 0.92f, 0.2f, 1f), Mats.ChevronTexture);
            for (int i = 0; i < MaxChevrons; i++)
            {
                var q = Blocky.Part(transform, Vector3.zero, new Vector3(1.3f, 1.3f, 1f), chevMat, false, PrimitiveType.Quad);
                Blocky.NoShadows(q.gameObject);
                q.gameObject.SetActive(false);
                chevrons.Add(q);
            }
            ring = Blocky.Part(transform, Vector3.zero, new Vector3(6f, 6f, 1f), Mats.UnlitAlpha(new Color(1f, 0.9f, 0.2f, 1f), Mats.RingTexture), false, PrimitiveType.Quad);
            ring.localRotation = Quaternion.Euler(90, 0, 0);
            beam = Blocky.Part(transform, Vector3.zero, new Vector3(2.2f, 30f, 1f), Mats.UnlitAlpha(new Color(1f, 0.95f, 0.4f, 0.7f), Mats.BeamTexture), false, PrimitiveType.Quad);
            Blocky.NoShadows(ring.gameObject); Blocky.NoShadows(beam.gameObject);
            // прыгающая 3D-стрелка вниз
            arrow = new GameObject("TutArrow").transform;
            arrow.SetParent(transform, false);
            var am = Mats.Glow(new Color(1f, 0.85f, 0.1f));
            Blocky.Round = true;
            Blocky.Part(arrow, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 1.6f, 0.6f), am);
            var h1 = Blocky.Part(arrow, new Vector3(-0.35f, 0.35f, 0), new Vector3(0.5f, 1.2f, 0.5f), am);
            h1.localRotation = Quaternion.Euler(0, 0, 40);
            var h2 = Blocky.Part(arrow, new Vector3(0.35f, 0.35f, 0), new Vector3(0.5f, 1.2f, 0.5f), am);
            h2.localRotation = Quaternion.Euler(0, 0, -40);
            Blocky.Round = false;
            Blocky.NoShadows(arrow.gameObject);
            stolenAtStep = SaveManager.Data.totalStolen;
            if (Step == 2) SaveManager.Data.tutorialStep = 1; // яйцо в руках не сохраняется
        }

        public void Skip()
        {
            SaveManager.Data.tutorialStep = Done;
            HideAll();
            GameManager.Instance.SaveNow(true);
        }

        void Advance()
        {
            SaveManager.Data.tutorialStep++;
            lastStepChange = Time.time;
            stolenAtStep = SaveManager.Data.totalStolen;
            GameAudio.Play(Sfx.Success);
            if (PlayerController.Instance != null) Fx.Burst(PlayerController.Instance.transform.position + Vector3.up * 2f, new Color(1f, 0.9f, 0.3f), 25, 5f, 0.5f, 0.8f);
            GameManager.Instance.SaveNow(true);
        }

        void HideAll()
        {
            foreach (var c in chevrons) c.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            beam.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
        }

        public string Text
        {
            get
            {
                switch (Step)
                {
                    case 0: return Loc.F("tut_1", Loc.Num(GameConfig.Tiers[1].reqPoints));
                    case 1: return Loc.F("tut_2", Loc.T(InputState.Mobile ? "tut_2_mob" : "tut_2_pc"));
                    case 2: return Loc.T("tut_3");
                    case 3: return Loc.T("tut_4");
                    case 4: return Loc.T("tut_5");
                    default: return "";
                }
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var p = PlayerController.Instance;
            if (gm == null || p == null) return;
            if (!Active) { if (ring.gameObject.activeSelf) HideAll(); return; }

            var d = SaveManager.Data;
            bool hasTarget = true;
            Vector3 target = Vector3.zero;
            switch (Step)
            {
                case 0:
                    target = Treadmill.All.Count > 0 ? Treadmill.All[0].transform.position : Vector3.zero;
                    if (d.speedPoints >= GameConfig.Tiers[1].reqPoints) Advance();
                    break;
                case 1:
                    target = gm.Pedestals.Count > 0 ? gm.Pedestals[0].transform.position : Vector3.zero;
                    if (p.Carrying != null) Advance();
                    break;
                case 2:
                    target = new Vector3(0, 0, GameConfig.BaseMaxZ - 6f);
                    if (d.totalStolen > stolenAtStep) Advance();
                    else if (p.Carrying == null) { SaveManager.Data.tutorialStep = 1; }
                    break;
                case 3:
                {
                    int egg = -1;
                    bool anyDragon = false;
                    for (int i = 0; i < d.plotsOwned; i++)
                    {
                        if (d.plots[i].state == (int)PlotState.Egg && egg < 0) egg = i;
                        if (d.plots[i].state == (int)PlotState.Dragon) anyDragon = true;
                    }
                    if (anyDragon) { Advance(); break; }
                    if (egg >= 0) target = gm.Plots[egg].transform.position;
                    else { SaveManager.Data.tutorialStep = 1; hasTarget = false; }
                    break;
                }
                case 4:
                    hasTarget = false;
                    if (Time.time - lastStepChange > 8f) Advance();
                    break;
            }

            if (!Active || !hasTarget) { HideAll(); return; }
            UpdatePath(p.transform.position, target);
            UpdateHighlight(target);
        }

        /// <summary>Строит ломаную: если игрок и цель по разные стороны ворот базы — через ворота.</summary>
        void UpdatePath(Vector3 from, Vector3 to)
        {
            var pts = new List<Vector3> { new Vector3(from.x, 0, from.z) };
            bool fromBase = from.z < GameConfig.BaseMaxZ, toBase = to.z < GameConfig.BaseMaxZ;
            if (fromBase != toBase)
            {
                pts.Add(new Vector3(0, 0, GameConfig.BaseMaxZ - 3f));
                pts.Add(new Vector3(0, 0, GameConfig.BaseMaxZ + 3f));
            }
            pts.Add(new Vector3(to.x, 0, to.z));

            float phase = Mathf.Repeat(Time.time * 3f, Spacing);
            float dist = 1.5f + phase; // старт чуть впереди игрока
            int used = 0;
            float acc = 0;
            for (int s = 0; s < pts.Count - 1 && used < MaxChevrons; s++)
            {
                Vector3 a = pts[s], b = pts[s + 1];
                float len = Vector3.Distance(a, b);
                if (len < 0.01f) continue;
                Vector3 dir = (b - a) / len;
                while (dist - acc < len && used < MaxChevrons)
                {
                    float along = dist - acc;
                    // не рисуем последние 3 юнита — там кольцо
                    if (s == pts.Count - 2 && len - along < 3f) break;
                    var c = chevrons[used++];
                    c.gameObject.SetActive(true);
                    c.position = a + dir * along + Vector3.up * 0.25f;
                    c.rotation = Quaternion.LookRotation(Vector3.down, dir);
                    dist += Spacing;
                }
                acc += len;
            }
            for (int i = used; i < chevrons.Count; i++)
                if (chevrons[i].gameObject.activeSelf) chevrons[i].gameObject.SetActive(false);
        }

        void UpdateHighlight(Vector3 target)
        {
            float t = Time.time;
            ring.gameObject.SetActive(true);
            beam.gameObject.SetActive(true);
            arrow.gameObject.SetActive(true);
            float pulse = 1f + Mathf.Sin(t * 4f) * 0.08f;
            ring.position = new Vector3(target.x, 0.3f, target.z);
            ring.localScale = new Vector3(6f * pulse, 6f * pulse, 1f);
            ring.rotation = Quaternion.Euler(90, t * 40f, 0);
            beam.position = new Vector3(target.x, 15f, target.z);
            var cam = CameraRig.Cam;
            if (cam != null)
            {
                Vector3 look = beam.position - cam.transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) beam.rotation = Quaternion.LookRotation(look);
            }
            arrow.position = new Vector3(target.x, 5.5f + Mathf.Abs(Mathf.Sin(t * 3f)) * 1.2f, target.z);
            arrow.rotation = Quaternion.Euler(0, t * 90f, 0);
        }
    }
}
