using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Пошаговое обучение: тропа из светящихся стрелок от игрока к цели, кольцо, столб света и прыгающая стрелка
    /// над целью, короткая инструкция в панели сверху (UIManager). Для кнопок HUD — прыгающая стрелка у кнопки.
    /// Шаги засчитываются по реальным событиям; если игрок уже что-то сделал — обучение само перескакивает вперёд,
    /// а если что-то пошло не так (поймали, яйцо пропало) — возвращается на нужный шаг. Застрять нельзя:
    /// последние шаги закрываются по таймеру, есть кнопка "Пропустить".
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public static Tutorial Instance;

        // Номера шагов начинаются с 10: старые сохранения хранили 0..5 (5 = пройдено) — см. Build().
        const int SBase = 10;
        const int STrain = 10;    // (a) беговая дорожка -> скорость
        const int SGoEgg = 11;    // (b) через ворота к первому яйцу
        const int SSteal = 12;    // (c) украсть (E / кнопка действия), брейнроты погонятся
        const int SReturn = 13;   // (d) донести до зелёной зоны сдачи
        const int SGrow = 14;     // (e) вырастить (реклама ускоряет) и открыть яйцо
        const int STake = 15;     // (f) забрать дракона в слот
        const int SSkills = 16;   // (g) навыки за монеты
        const int SSummary = 17;  // (h) итог: цели игры
        public const int Done = 18;

        readonly List<Transform> chevrons = new List<Transform>();
        Transform ring, beam, arrow;
        float stepStart;
        int stolenAtStep, ownedAtStep;
        float caughtUntil = -1f;
        const int MaxChevrons = 36;
        const float Spacing = 1.8f;

        // состояние шага "вырастить": 0 — растёт, 1 — готово, 2 — яйцо в инвентаре
        int growMode;
        double growLeft;

        // стрелка у кнопки HUD
        RectTransform uiArrow;
        Vector2 uiArrowBase, uiArrowDir;
        string uiArrowFor;
        float uiSearchTimer;

        public int Step { get { return SaveManager.Data.tutorialStep; } }
        public bool Active { get { return Step < Done; } }

        /// <summary>Показывать ли панель и указатели сейчас: не во время обучения управлению и не во время рулетки.</summary>
        public bool ShowPanel
        {
            get
            {
                if (!Active || !SaveManager.Data.controlsSeen || Roulette.Active) return false;
                var gm = GameManager.Instance;
                return gm == null || !gm.Opening;
            }
        }

        /// <summary>Скорость, до которой качаемся в первом шаге: столько же, сколько "Рекомендуется" на первом яйце.</summary>
        static double TrainGoal
        {
            get
            {
                var t = GameConfig.Tiers;
                return System.Math.Max(10, (t.Length > 1 ? t[1].reqPoints * 0.5 : 0) + t[0].reqPoints);
            }
        }

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
            HideAll();

            // миграция старой нумерации (0..4 — шаги, 5 — пройдено)
            var d = SaveManager.Data;
            if (d.tutorialStep < SBase)
                d.tutorialStep = d.tutorialStep >= 5 ? Done : (d.tutorialStep <= 0 ? STrain : SGoEgg);
            // яйцо в руках не сохраняется — начинаем с похода к яйцу (дальше Resolve сам перескочит вперёд, если надо)
            if (d.tutorialStep == SSteal || d.tutorialStep == SReturn) d.tutorialStep = SGoEgg;
            if (d.tutorialStep > Done) d.tutorialStep = Done;
            OnStepEntered();
        }

        public void Skip()
        {
            SaveManager.Data.tutorialStep = Done;
            HideAll();
            DestroyUiArrow();
            GameManager.Instance.SaveNow(true);
        }

        void OnStepEntered()
        {
            stepStart = Time.time;
            stolenAtStep = SaveManager.Data.totalStolen;
            ownedAtStep = OwnedCount();
        }

        /// <summary>Переход на шаг: вперёд — с "дзынь" и искрами, назад — тихо.</summary>
        void GoTo(int step)
        {
            if (step == Step) return;
            bool forward = step > Step;
            SaveManager.Data.tutorialStep = step;
            OnStepEntered();
            if (forward)
            {
                GameAudio.Play(Sfx.Success);
                if (PlayerController.Instance != null) Fx.Burst(PlayerController.Instance.transform.position + Vector3.up * 2f, new Color(1f, 0.9f, 0.3f), 25, 5f, 0.5f, 0.8f);
            }
            if (step >= Done) { HideAll(); DestroyUiArrow(); }
            GameManager.Instance.SaveNow(forward);
        }

        static int OwnedCount()
        {
            var d = SaveManager.Data;
            int c = d.dragonStore != null ? d.dragonStore.Count : 0;
            if (d.dragonInv != null) foreach (var id in d.dragonInv) if (id >= 0) c++;
            return c;
        }

        static string Act { get { return Loc.T(InputState.Mobile ? "tut_act_mob" : "tut_act_pc"); } }

        public string Text
        {
            get
            {
                switch (Step)
                {
                    case STrain: return Loc.F("tut_train", Loc.Num(SaveManager.Data.speedPoints), Loc.Num(TrainGoal));
                    case SGoEgg: return Time.time < caughtUntil ? Loc.T("tut_caught") : Loc.T("tut_go_egg");
                    case SSteal: return Loc.F("tut_steal", Loc.T(InputState.Mobile ? "tut_steal_mob" : "tut_steal_pc"));
                    case SReturn: return Loc.T("tut_return");
                    case SGrow:
                        if (growMode == 2) return Loc.T("tut_plant");
                        if (growMode == 1) return Loc.F("tut_open", Act);
                        return Loc.F("tut_grow", Loc.Time(growLeft));
                    case STake: return Loc.F("tut_take", Loc.Cap(Act));
                    case SSkills: return Loc.T("tut_skills");
                    case SSummary: return Loc.T("tut_done");
                    default: return "";
                }
            }
        }

        EggPedestal FirstPedestal(GameManager gm)
        {
            foreach (var ped in gm.Pedestals) if (ped != null && ped.tier == Tier.Common) return ped;
            return gm.Pedestals.Count > 0 ? gm.Pedestals[0] : null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var p = PlayerController.Instance;
            if (gm == null || p == null) return;
            if (!Active) { if (ring.gameObject.activeSelf) HideAll(); if (uiArrow != null) DestroyUiArrow(); return; }

            var d = SaveManager.Data;
            bool carrying = p.Carrying != null;
            int eggPlot = -1, dragonPlot = -1;
            for (int i = 0; i < d.plotsOwned && i < d.plots.Count && i < gm.Plots.Count; i++)
            {
                if (d.plots[i].state == (int)PlotState.Egg)
                {
                    // сначала — созревшее яйцо, иначе то, что созреет раньше
                    if (eggPlot < 0 || d.plots[i].readyAt < d.plots[eggPlot].readyAt) eggPlot = i;
                }
                if (d.plots[i].state == (int)PlotState.Dragon && dragonPlot < 0) dragonPlot = i;
            }
            bool invEgg = d.inventory.Count > 0;
            var ped = FirstPedestal(gm);
            float pedDist = ped != null ? Vector3.Distance(Flat(p.transform.position), Flat(ped.transform.position)) : 999f;

            // ===== переходы между шагами (вперёд — если уже сделано, назад — если что-то потеряно) =====
            int s = Step;
            if (s == STrain && (d.speedPoints >= TrainGoal || d.totalStolen > 0)) s = SGoEgg;
            if (s == SGoEgg && pedDist < 9f) s = SSteal;
            if (s == SSteal && !carrying && pedDist > 18f) s = SGoEgg;
            if (s < SReturn && carrying) s = SReturn;
            if (s == SReturn && !carrying)
            {
                if (d.totalStolen > stolenAtStep) s = SGrow;
                else { s = SGoEgg; caughtUntil = Time.time + 5f; }
            }
            if (s < SGrow && !carrying && (eggPlot >= 0 || invEgg)) s = SGrow;
            if (s <= SGrow && dragonPlot >= 0 && !gm.Opening && (s != SGrow || eggPlot < 0 || d.totalHatched > 0)) s = STake;
            if (s < SSkills && s != STake && dragonPlot < 0 && OwnedCount() > 0 && d.totalHatched > 0 && eggPlot < 0 && !invEgg && !carrying) s = SSkills;
            if (s == SGrow && eggPlot < 0 && !invEgg && dragonPlot < 0 && !carrying && !gm.Opening) s = OwnedCount() > 0 ? SSkills : SGoEgg;
            if (s == STake && (dragonPlot < 0 || OwnedCount() > ownedAtStep)) s = SSkills;
            if (s == SSkills && (UIManager.Instance != null && UIManager.Instance.SkillsOpen || Time.time - stepStart > 40f)) s = SSummary;
            if (s == SSummary && Time.time - stepStart > 9f) s = Done;
            if (s != Step) { GoTo(s); if (!Active) return; }

            // ===== цель для указателей =====
            bool hasTarget = false;
            Vector3 target = Vector3.zero;
            string uiTarget = null;
            switch (Step)
            {
                case STrain:
                    if (Treadmill.All.Count > 0 && !p.OnTreadmill) { target = Treadmill.All[0].transform.position; hasTarget = true; }
                    break;
                case SGoEgg:
                case SSteal:
                    if (ped != null) { target = ped.transform.position; hasTarget = true; }
                    break;
                case SReturn:
                    target = new Vector3(2f, 0, GameConfig.BaseMaxZ - 5f); // зелёная площадка "СДАЙ ЯЙЦО СЮДА" (WorldBuilder)
                    hasTarget = true;
                    break;
                case SGrow:
                    if (eggPlot >= 0)
                    {
                        growLeft = System.Math.Max(0, d.plots[eggPlot].readyAt - SaveData.Now());
                        growMode = growLeft <= 0 ? 1 : 0;
                        target = gm.Plots[eggPlot].transform.position;
                        hasTarget = true;
                    }
                    else { growMode = 2; uiTarget = "BtnEggs"; }
                    break;
                case STake:
                    if (dragonPlot >= 0) { target = gm.Plots[dragonPlot].transform.position; hasTarget = true; }
                    break;
                case SSkills:
                    uiTarget = "BtnShop";
                    break;
            }

            bool show = ShowPanel;
            if (!show || !hasTarget) HideAll();
            else
            {
                UpdatePath(p.transform.position, target);
                UpdateHighlight(target);
            }
            UpdateUiArrow(show ? uiTarget : null);
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0, v.z); }

        void HideAll()
        {
            foreach (var c in chevrons) if (c.gameObject.activeSelf) c.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            beam.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
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
                    c.position = a + dir * along + Vector3.up * 0.66f;
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
            ring.position = new Vector3(target.x, Mathf.Max(0.68f, target.y + 0.5f), target.z);
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

        // ===================== стрелка у кнопки HUD =====================
        static Transform FindDeep(Transform t, string name)
        {
            if (t == null) return null;
            foreach (Transform c in t)
            {
                if (c.name == name) return c;
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        void DestroyUiArrow()
        {
            if (uiArrow != null) Destroy(uiArrow.gameObject);
            uiArrow = null;
            uiArrowFor = null;
        }

        void UpdateUiArrow(string button)
        {
            if (button == null)
            {
                if (uiArrow != null && uiArrow.gameObject.activeSelf) uiArrow.gameObject.SetActive(false);
                return;
            }
            if (uiArrowFor != button || uiArrow == null)
            {
                if (uiArrowFor != button) uiSearchTimer = 0;
                if (uiArrow != null) Destroy(uiArrow.gameObject);
                uiArrow = null;
                uiArrowFor = button;
                uiSearchTimer -= Time.unscaledDeltaTime;
                if (uiSearchTimer > 0) return;
                uiSearchTimer = 1f; // не ищем кнопку каждый кадр, если её нет
                var btn = UIManager.Instance != null ? FindDeep(UIManager.Instance.transform, button) as RectTransform : null;
                if (btn == null) return;
                uiArrow = BuildUiArrow(btn, button == "BtnShop");
            }
            if (!uiArrow.gameObject.activeSelf) uiArrow.gameObject.SetActive(true);
            uiArrow.anchoredPosition = uiArrowBase + uiArrowDir * (Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 14f);
        }

        /// <summary>Жёлтая стрелка из двух плашек, дочерняя к кнопке: снизу (указывает вверх) или слева (указывает вправо).</summary>
        RectTransform BuildUiArrow(RectTransform btn, bool below)
        {
            var gold = new Color(1f, 0.84f, 0.2f);
            Vector2 anchor = below ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            uiArrowBase = below ? new Vector2(0, -60) : new Vector2(-40, 0);   // снизу — под подписью кнопки
            uiArrowDir = below ? new Vector2(0, -1) : new Vector2(-1, 0);
            var rt = UIKit.Rect(btn, "TutPointer", anchor, new Vector2(0.5f, 0.5f), uiArrowBase, new Vector2(44, 64));
            rt.localRotation = Quaternion.Euler(0, 0, below ? 0f : -90f);
            var stem = UIKit.Panel(rt, "Stem", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(18, 34), gold, 2f);
            stem.raycastTarget = false;
            var head = UIKit.Panel(rt, "Head", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(34, 34), gold, 2f);
            head.raycastTarget = false;
            head.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            return rt;
        }
    }
}
