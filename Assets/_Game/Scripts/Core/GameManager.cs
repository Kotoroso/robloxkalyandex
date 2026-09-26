using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Экономика и прогрессия: монеты, скорость, драконы-статы, грядки, перерождения, сохранения.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public readonly List<BasePlot> Plots = new List<BasePlot>();

        // Суммарные бонусы драконов
        public float DragonSpeedPct { get; private set; }
        public float DragonTrainPct { get; private set; }
        public float DragonJump { get; private set; }
        public double DragonCps { get; private set; }
        public int DragonCount { get; private set; }

        float saveTimer, cloudTimer;
        float lastCaughtAd = 0f;

        SaveData D { get { return SaveManager.Data; } }

        public float CoinMultiplier { get { return GameConfig.RebirthMultiplier(D.rebirths); } }
        public float TrainMultiplier { get { return GameConfig.RebirthMultiplier(D.rebirths) * (1f + DragonTrainPct / 100f); } }
        public double CoinsPerSec { get { return DragonCps * CoinMultiplier; } }

        public float WalkSpeed
        {
            get
            {
                float s = GameConfig.BaseWalkSpeed + (float)(D.speedPoints / GameConfig.SpeedPointsPerUnit);
                s = Mathf.Min(s, GameConfig.MaxWalkSpeed);
                return s * (1f + DragonSpeedPct / 100f);
            }
        }

        public float JumpPower { get { return GameConfig.BaseJump + DragonJump; } }

        public void Init()
        {
            Instance = this;
            RecalcStats();
            // оффлайн-доход
            if (D.savedAt > 0)
            {
                long sec = SaveData.Now() - D.savedAt;
                sec = System.Math.Max(0, System.Math.Min(sec, GameConfig.OfflineMaxSeconds));
                double earned = CoinsPerSec * sec * GameConfig.OfflineIncomeFactor;
                if (earned >= 1)
                {
                    D.coins += earned;
                    UIManager.Instance.Toast(Loc.F("offline", Loc.Num(earned)), new Color(1f, 0.9f, 0.3f), 5f);
                }
            }
        }

        public void RecalcStats()
        {
            float sp = 0, tp = 0, j = 0; double cps = 0; int n = 0;
            for (int i = 0; i < D.plotsOwned && i < D.plots.Count; i++)
            {
                var p = D.plots[i];
                if (p.state != (int)PlotState.Dragon) continue;
                var d = GameConfig.GetDragon(p.dragonId);
                sp += d.speedPct; tp += d.trainPct; j += d.jumpBonus; cps += d.coinsPerSec; n++;
            }
            DragonSpeedPct = sp; DragonTrainPct = tp; DragonJump = j; DragonCps = cps; DragonCount = n;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            D.coins += CoinsPerSec * dt;

            // сажаем яйца из инвентаря в освободившиеся грядки
            if (D.inventory.Count > 0)
            {
                int free = FindFreePlot();
                if (free >= 0)
                {
                    var e = D.inventory[0];
                    D.inventory.RemoveAt(0);
                    Plant(free, e.tier, e.dragonId);
                }
            }

            saveTimer += Time.unscaledDeltaTime;
            cloudTimer += Time.unscaledDeltaTime;
            if (saveTimer > 15f)
            {
                saveTimer = 0;
                bool cloud = cloudTimer > 45f;
                if (cloud) cloudTimer = 0;
                SaveManager.Save(cloud);
            }
        }

        public void SaveNow(bool cloud)
        {
            saveTimer = 0;
            if (cloud) cloudTimer = 0;
            SaveManager.Save(cloud);
        }

        public void AddSpeedPoints(double v) { D.speedPoints += v; }

        public bool TrySpend(double price)
        {
            if (D.coins + 0.001 < price) return false;
            D.coins -= price;
            return true;
        }

        int FindFreePlot()
        {
            for (int i = 0; i < D.plotsOwned; i++)
                if (D.plots[i].state == (int)PlotState.Empty) return i;
            return -1;
        }

        void Plant(int plot, int tier, int dragonId)
        {
            var p = D.plots[plot];
            p.state = (int)PlotState.Egg;
            p.tier = tier;
            p.dragonId = dragonId;
            p.plantedAt = SaveData.Now();
            p.readyAt = p.plantedAt + GameConfig.GetTier(tier).growSeconds;
            GameAudio.Play(Sfx.Plant);
        }

        public void DepositEgg(CarriedEgg egg)
        {
            D.totalStolen++;
            int free = FindFreePlot();
            if (free >= 0)
            {
                Plant(free, (int)egg.tier, egg.dragonId);
                UIManager.Instance.Toast(Loc.T("stolen"), new Color(0.5f, 1f, 0.5f));
            }
            else
            {
                D.inventory.Add(new EggItem { tier = (int)egg.tier, dragonId = egg.dragonId });
                UIManager.Instance.Toast(Loc.T("stolen_inv"), new Color(1f, 0.85f, 0.4f));
            }
            GameAudio.Play(Sfx.Success);
            SaveNow(true);
        }

        public void Hatch(BasePlot plot)
        {
            var p = plot.Data;
            p.state = (int)PlotState.Dragon;
            var def = GameConfig.GetDragon(p.dragonId);
            RecalcStats();
            GameAudio.Play(Sfx.Hatch);
            UIManager.Instance.Toast(Loc.F("hatched", Loc.DragonName(def)), GameConfig.GetTier(def.tier).color, 4f);
            FloatingText.Spawn(plot.transform.position + Vector3.up * 5f, Loc.TierName(def.tier) + "!", GameConfig.GetTier(def.tier).color);
            plot.ForceRefresh();
            SaveNow(true);
        }

        public void SpeedUpWithAd(BasePlot plot)
        {
            int idx = plot.index;
            YandexSDK.ShowRewarded(ok =>
            {
                var p = D.plots[idx];
                if (!ok) { UIManager.Instance.Toast(Loc.T("ad_fail"), new Color(1f, 0.6f, 0.4f)); return; }
                if (p.state != (int)PlotState.Egg) return;
                long now = SaveData.Now();
                long left = p.readyAt - now;
                if (left > 0) p.readyAt = now + (long)Mathf.Ceil(left * GameConfig.AdSpeedupFactor);
                GameAudio.Play(Sfx.Success);
                UIManager.Instance.Toast(Loc.T("sped_up"), new Color(0.5f, 1f, 1f));
                SaveNow(true);
            });
        }

        public void SellDragon(BasePlot plot)
        {
            var p = plot.Data;
            if (p.state != (int)PlotState.Dragon) return;
            var def = GameConfig.GetDragon(p.dragonId);
            double price = GameConfig.SellPrice(def) * CoinMultiplier;
            D.coins += price;
            p.state = (int)PlotState.Empty;
            RecalcStats();
            GameAudio.Play(Sfx.Coin);
            UIManager.Instance.Toast(Loc.F("sold", Loc.Num(price)), new Color(1f, 0.9f, 0.3f));
            SaveNow(true);
        }

        public bool TryBuyPlot()
        {
            if (D.plotsOwned >= GameConfig.MaxPlots) return false;
            double cost = GameConfig.PlotCost(D.plotsOwned);
            if (!TrySpend(cost))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return false;
            }
            D.plotsOwned++;
            GameAudio.Play(Sfx.Buy);
            UIManager.Instance.Toast(Loc.T("plot_bought"), new Color(0.5f, 1f, 0.5f));
            SaveNow(true);
            return true;
        }

        public double RebirthCost { get { return GameConfig.RebirthCost(D.rebirths); } }

        public bool TryRebirth()
        {
            if (!TrySpend(RebirthCost))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return false;
            }
            D.rebirths++;
            D.coins = 0;
            D.speedPoints = 0;
            D.plotsOwned = Mathf.Min(GameConfig.MaxPlots, D.plotsOwned + 1);
            RecalcStats();
            foreach (var t in Treadmill.All) t.Refresh();
            GameAudio.Play(Sfx.Rebirth);
            UIManager.Instance.Toast(Loc.F("rebirth_done", GameConfig.RebirthMultiplier(D.rebirths).ToString("0.#")), new Color(1f, 0.6f, 1f), 4f);
            PlayerController.Instance.DropCarried(true);
            PlayerController.Instance.Respawn();
            SaveNow(true);
            YandexSDK.ShowInterstitial();
            return true;
        }

        public void OnCaught()
        {
            // полноэкранная реклама не чаще раза в 3 минуты и только в естественной паузе
            if (Time.realtimeSinceStartup - lastCaughtAd > 180f)
            {
                lastCaughtAd = Time.realtimeSinceStartup;
                YandexSDK.ShowInterstitial();
            }
        }
    }
}
