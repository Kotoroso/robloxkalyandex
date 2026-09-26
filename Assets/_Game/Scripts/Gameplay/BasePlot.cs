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
        TextMesh label;
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
            Blocky.Part(tr, new Vector3(0, 0.2f, 0), new Vector3(7f, 0.4f, 7f), Mats.Studs(new Color(0.3f, 0.75f, 0.3f), 7, 7), true);
            Blocky.Part(tr, new Vector3(0, 0.45f, 0), new Vector3(5.6f, 0.2f, 5.6f), Mats.Studs(new Color(0.45f, 0.3f, 0.18f), 6, 6), true);
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

            if (d.state == (int)PlotState.Egg && SaveData.Now() >= d.readyAt)
            {
                GameManager.Instance.Hatch(this);
                return;
            }

            if (shownState != d.state || shownDragon != d.dragonId) Rebuild();

            if (d.state == (int)PlotState.Egg)
            {
                long left = d.readyAt - SaveData.Now();
                label.text = Loc.TierName((Tier)d.tier) + "\n" + Loc.Time(left);
                label.color = GameConfig.GetTier(d.tier).color;
                if (content != null)
                {
                    // яйцо покачивается и растёт по мере созревания
                    float total = Mathf.Max(1, d.readyAt - d.plantedAt);
                    float prog = Mathf.Clamp01(1f - left / total);
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
        public float HoldTime { get { return Data.state == (int)PlotState.Dragon ? 1.0f : 0.3f; } }
        public bool CanInteract { get { return Owned && Data.state != (int)PlotState.Empty; } }
        public string Prompt
        {
            get
            {
                if (Data.state == (int)PlotState.Egg) return Loc.T("speedup_ad");
                var def = GameConfig.GetDragon(Data.dragonId);
                return Loc.F("sell", Loc.Num(GameConfig.SellPrice(def) * GameManager.Instance.CoinMultiplier));
            }
        }

        public void Interact(PlayerController p)
        {
            var gm = GameManager.Instance;
            if (Data.state == (int)PlotState.Egg) gm.SpeedUpWithAd(this);
            else if (Data.state == (int)PlotState.Dragon) gm.SellDragon(this);
        }
    }
}
