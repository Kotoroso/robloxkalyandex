using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Грядка на базе (как в "Украсть брейнрота"): сюда кладётся яйцо, оно растёт по таймеру
    /// (зависит от тира), потом вылупляется дракон и даёт статы + доход. Реклама режет время на 90%.
    /// </summary>
    public class BasePlot : MonoBehaviour, IInteractable
    {
        public int index;
        GameObject content;
        Label3D label;
        GameObject lockedVisual;
        int shownState = -1;
        int shownDragon = -1;
        Transform eggRoot;

        public PlotData Data { get { return SaveManager.Data.plots[index]; } }
        public bool Owned { get { return index < SaveManager.Data.plotsOwned; } }

        public static BasePlot Build(Transform parent, int index, Vector3 pos)
        {
            var go = new GameObject("Plot_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var p = go.AddComponent<BasePlot>();
            p.index = index;
            var tr = go.transform;
            Blocky.Part(tr, new Vector3(0, 0.2f, 0), new Vector3(7f, 0.4f, 7f), Mats.Studs(new Color(0.3f, 0.75f, 0.3f)), true);
            Blocky.Part(tr, new Vector3(0, 0.45f, 0), new Vector3(5.6f, 0.2f, 5.6f), Mats.Studs(new Color(0.45f, 0.3f, 0.18f)), true);
            // деревянный бортик
            var wood = Mats.Plastic(new Color(0.62f, 0.42f, 0.25f));
            Blocky.Round = true; Blocky.RoundFactor = 0.3f;
            Blocky.Part(tr, new Vector3(0, 0.6f, 3.3f), new Vector3(7f, 0.4f, 0.4f), wood);
            Blocky.Part(tr, new Vector3(0, 0.6f, -3.3f), new Vector3(7f, 0.4f, 0.4f), wood);
            Blocky.Part(tr, new Vector3(3.3f, 0.6f, 0), new Vector3(0.4f, 0.4f, 6.2f), wood);
            Blocky.Part(tr, new Vector3(-3.3f, 0.6f, 0), new Vector3(0.4f, 0.4f, 6.2f), wood);
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            p.lockedVisual = Blocky.Part(tr, new Vector3(0, 0.6f, 0), new Vector3(5.8f, 0.2f, 5.8f), Mats.Studs(new Color(0.4f, 0.4f, 0.4f), 6, 6)).gameObject;
            p.label = Blocky.Label(tr, "", new Vector3(0, 5.5f, 0), 1f, Color.white);
            p.eggRoot = Blocky.Pivot(tr, "Content", new Vector3(0, 0.55f, 0));
            PlayerController.Interactables.Add(p);
            return p;
        }

        void Update()
        {
            var d = Data;
            if (!Owned)
            {
                if (shownState != -2) { Clear(); shownState = -2; lockedVisual.SetActive(true); label.text = ""; }
                return;
            }
            if (lockedVisual.activeSelf) lockedVisual.SetActive(false);

            if (shownState != d.state || shownDragon != d.dragonId) Rebuild();

            if (d.state == (int)PlotState.Egg)
            {
                long left = d.readyAt - SaveData.Now();
                bool ready = left <= 0;
                label.text = Loc.TierName((Tier)d.tier) + "\n" + (ready ? (Loc.Ru ? "<color=#7CFF7C>ГОТОВО! Открой</color>" : "<color=#7CFF7C>READY! Open it</color>") : Loc.Time(left));
                label.color = GameConfig.GetTier(d.tier).color;
                if (content != null)
                {
                    // яйцо покачивается и растёт по мере созревания
                    float total = Mathf.Max(1, d.readyAt - d.plantedAt);
                    float prog = ready ? 1f : Mathf.Clamp01(1f - left / total);
                    float wob = Mathf.Sin(Time.time * (3f + prog * 10f)) * (4f + prog * 10f);
                    content.transform.localRotation = Quaternion.Euler(0, 0, wob);
                    content.transform.localScale = Vector3.one * (0.8f + prog * 0.4f);
                }
            }
        }

        void Clear()
        {
            if (content != null) Destroy(content);
            content = null;
        }

        void Rebuild()
        {
            Clear();
            var d = Data;
            shownState = d.state;
            shownDragon = d.dragonId;
            if (d.state == (int)PlotState.Egg)
            {
                content = Blocky.BuildEgg(eggRoot, (Tier)d.tier, 1.6f);
                Fx.Sparkles(content.transform, new Vector3(0, 1.2f, 0), GameConfig.GetTier(d.tier).color, 1.2f, 6f);
            }
            else if (d.state == (int)PlotState.Dragon)
            {
                var def = GameConfig.GetDragon(d.dragonId);
                content = Blocky.BuildDragon(eggRoot, def);
                content.transform.localRotation = Quaternion.Euler(0, 180, 0);
                label.text = Loc.DragonName(def) + "\n+" + Loc.Num(def.coinsPerSec) + "/" + (Loc.Ru ? "сек" : "s");
                label.color = GameConfig.GetTier(def.tier).color;
            }
            else label.text = "";
        }

        public void ForceRefresh() { shownState = -1; }

        // ===== IInteractable =====
        public Vector3 InteractPos { get { return transform.position; } }
        public float InteractRange { get { return 4.5f; } }
        public float HoldTime { get { return 0.15f; } }
        public bool CanInteract
        {
            get
            {
                if (!Owned) return false;
                if (Data.state != (int)PlotState.Empty) return true;
                return GameManager.Instance != null && GameManager.Instance.HeldDragonId >= 0;
            }
        }
        public string Prompt
        {
            get
            {
                if (Data.state == (int)PlotState.Egg)
                    return SaveData.Now() >= Data.readyAt ? (Loc.Ru ? "Открыть яйцо!" : "Open egg!") : Loc.T("speedup_ad");
                if (Data.state == (int)PlotState.Dragon) return Loc.T("take_dragon");
                return Loc.T("place_dragon");
            }
        }

        public void Interact(PlayerController p)
        {
            var gm = GameManager.Instance;
            if (Data.state == (int)PlotState.Egg)
            {
                if (SaveData.Now() >= Data.readyAt) gm.OpenEgg(this);
                else gm.SpeedUpWithAd(this);
            }
            else if (Data.state == (int)PlotState.Dragon) gm.TakeDragon(this);
            else gm.PlaceDragon(this);
        }
    }
}
