using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Экономика и прогрессия: монеты, скорость, драконы-статы, грядки, перерождения, сохранения.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public readonly List<BasePlot> Plots = new List<BasePlot>();
        public readonly List<EggPedestal> Pedestals = new List<EggPedestal>();
        public event System.Action OnHeldChanged;

        // Суммарные бонусы драконов
        public float DragonSpeedPct { get; private set; }
        public float DragonTrainPct { get; private set; }
        public float DragonJump { get; private set; }
        public double DragonCps { get; private set; }
        public int DragonCount { get; private set; }

        float saveTimer, cloudTimer;

        SaveData D { get { return SaveManager.Data; } }

        public int UpgLevel(int i) { return i < D.upgrades.Count ? D.upgrades[i] : 0; }
        public bool Owns(string id) { return D.ownedProducts.Contains(id); }
        public float CoinMultiplier { get { return GameConfig.RebirthMultiplier(D.rebirths) * (1f + 0.2f * UpgLevel(1)) * (Owns("x2_income") ? 2f : 1f); } }
        public float TrainMultiplier { get { return GameConfig.RebirthMultiplier(D.rebirths) * (1f + 0.25f * UpgLevel(0)) * (1f + DragonTrainPct / 100f) * (Owns("x2_train") ? 2f : 1f); } }
        public float GrowFactor { get { return 1f - 0.05f * UpgLevel(2); } }
        public int LuckLevel { get { return UpgLevel(3); } }
        public double CoinsPerSec { get { return DragonCps * CoinMultiplier; } }

        public float WalkSpeed
        {
            get
            {
                return GameConfig.WalkSpeedFromPoints(D.speedPoints) * (1f + DragonSpeedPct / 100f);
            }
        }

        public float JumpPower { get { return GameConfig.BaseJump + DragonJump + 0.5f * UpgLevel(4); } }
        public bool Opening { get; private set; }

        void Awake() { Instance = this; }

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
                float ls = GameConfig.DragonLevelStat(p.level);
                sp += d.speedPct * ls; tp += d.trainPct * ls; j += d.jumpBonus; cps += d.coinsPerSec * GameConfig.DragonLevelIncome(p.level); n++;
            }
            // дракон в руке тоже даёт свой бонус скорости и прыжка
            int held = HeldDragonId;
            if (held >= 0)
            {
                var hd = GameConfig.GetDragon(held);
                sp += hd.speedPct * GameConfig.DragonLevelStat(D.dragonInvLvl[D.selectedSlot]); j += hd.jumpBonus;
            }
            DragonSpeedPct = sp; DragonTrainPct = tp; DragonJump = j; DragonCps = cps; DragonCount = n;
        }

        // ===================== Слоты драконов =====================
        public int HeldDragonId
        {
            get
            {
                int s = D.selectedSlot;
                return (s >= 0 && s < D.dragonInv.Count) ? D.dragonInv[s] : -1;
            }
        }

        public int FreeSlot()
        {
            for (int i = 0; i < D.dragonInv.Count; i++) if (D.dragonInv[i] < 0) return i;
            return -1;
        }

        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= GameConfig.InventorySlots) return;
            D.selectedSlot = (D.selectedSlot == slot || D.dragonInv[slot] < 0) ? -1 : slot;
            GameAudio.Play(Sfx.Click);
            RecalcStats();
            if (OnHeldChanged != null) OnHeldChanged();
        }

        /// <summary>Снять дракона с грядки в слот.</summary>
        public void TakeDragon(BasePlot plot)
        {
            var p = plot.Data;
            if (p.state != (int)PlotState.Dragon) return;
            int slot = FreeSlot();
            if (slot < 0)
            {
                UIManager.Instance.Toast(Loc.T("inv_full"), new Color(1f, 0.6f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return;
            }
            D.dragonInv[slot] = p.dragonId;
            D.dragonInvLvl[slot] = p.level;
            p.state = (int)PlotState.Empty;
            D.selectedSlot = slot;
            RecalcStats();
            if (OnHeldChanged != null) OnHeldChanged();
            GameAudio.Play(Sfx.Grab);
            Fx.Burst(plot.transform.position + Vector3.up * 1.5f, Color.white, 15, 4f, 0.4f, 0.6f);
            UIManager.Instance.Toast(Loc.T("dragon_taken"), new Color(0.6f, 1f, 0.8f));
            SaveNow(true);
        }

        /// <summary>Поставить дракона из руки на пустую грядку.</summary>
        public void PlaceDragon(BasePlot plot)
        {
            int id = HeldDragonId;
            var p = plot.Data;
            if (id < 0 || p.state != (int)PlotState.Empty) return;
            p.state = (int)PlotState.Dragon;
            p.dragonId = id;
            p.tier = (int)GameConfig.GetDragon(id).tier;
            p.level = D.dragonInvLvl[D.selectedSlot];
            D.dragonInv[D.selectedSlot] = -1;
            D.dragonInvLvl[D.selectedSlot] = 1;
            D.selectedSlot = -1;
            RecalcStats();
            if (OnHeldChanged != null) OnHeldChanged();
            plot.ForceRefresh();
            GameAudio.Play(Sfx.Plant);
            Fx.Burst(plot.transform.position + Vector3.up * 1.5f, GameConfig.GetTier(p.tier).color, 25, 5f, 0.5f, 0.8f);
            SaveNow(true);
        }

        public double SlotSellPrice(int slot)
        {
            int id = D.dragonInv[slot];
            return id < 0 ? 0 : GameConfig.SellPrice(GameConfig.GetDragon(id), D.dragonInvLvl[slot]) * CoinMultiplier;
        }

        public void SellSlot(int slot)
        {
            if (slot < 0 || slot >= D.dragonInv.Count || D.dragonInv[slot] < 0) return;
            double price = SlotSellPrice(slot);
            D.coins += price;
            D.dragonInv[slot] = -1;
            if (D.selectedSlot == slot) D.selectedSlot = -1;
            RecalcStats();
            if (OnHeldChanged != null) OnHeldChanged();
            GameAudio.Play(Sfx.Coin);
            UIManager.Instance.Toast(Loc.F("sold", Loc.Num(price)), new Color(1f, 0.9f, 0.3f));
            SaveNow(true);
        }

        public void SellAll()
        {
            double total = 0;
            for (int i = 0; i < D.dragonInv.Count; i++)
                if (D.dragonInv[i] >= 0) { total += SlotSellPrice(i); D.dragonInv[i] = -1; }
            if (total <= 0) return;
            D.coins += total;
            D.selectedSlot = -1;
            RecalcStats();
            if (OnHeldChanged != null) OnHeldChanged();
            GameAudio.Play(Sfx.Coin);
            UIManager.Instance.Toast(Loc.F("sold", Loc.Num(total)), new Color(1f, 0.9f, 0.3f));
            SaveNow(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            D.coins += CoinsPerSec * dt;
            UpdateAdTimer(Time.unscaledDeltaTime);

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
            p.readyAt = p.plantedAt + Mathf.Max(5, Mathf.RoundToInt(GameConfig.GetTier(tier).growSeconds * GrowFactor));
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

        /// <summary>
        /// Открыть созревшее яйцо: рулетка как в кейсах. Первое яйцо в игре — гарантированно
        /// легендарный или мифический дракон (чтобы сразу зацепить игрока).
        /// </summary>
        public void OpenEgg(BasePlot plot)
        {
            if (Opening) return;
            var p = plot.Data;
            if (p.state != (int)PlotState.Egg || SaveData.Now() < p.readyAt) return;
            Tier eggTier = (Tier)p.tier;
            Tier result = D.totalHatched == 0
                ? (Random.value < 0.5f ? Tier.Legendary : Tier.Mythic)
                : GameConfig.RollTier(eggTier, LuckLevel);
            var def = GameConfig.RollInTier(result);
            Opening = true;
            GameAudio.Play(Sfx.Whoosh);
            UIManager.Instance.ShowRoulette(eggTier, def, () =>
            {
                Opening = false;
                FinishHatch(plot, def);
            });
        }

        void FinishHatch(BasePlot plot, DragonDef def)
        {
            var p = plot.Data;
            p.state = (int)PlotState.Dragon;
            p.dragonId = def.id;
            p.tier = (int)def.tier;
            p.level = 1;
            D.totalHatched++;
            RecalcStats();
            GameAudio.Play(Sfx.Hatch);
            UIManager.Instance.Toast(Loc.F("hatched", Loc.DragonName(def)), GameConfig.GetTier(def.tier).color, 4f);
            FloatingText.Spawn(plot.transform.position + Vector3.up * 5f, Loc.TierName(def.tier) + "!", GameConfig.GetTier(def.tier).color);
            Fx.Confetti(plot.transform.position + Vector3.up * 1f, 60);
            Fx.Burst(plot.transform.position + Vector3.up * 1.5f, GameConfig.GetTier(def.tier).color, 40, 7f, 0.6f, 1f);
            plot.ForceRefresh();
            SaveNow(true);
        }

        public bool TryBuyUpgrade(int i)
        {
            var u = GameConfig.Upgrades[i];
            int lvl = UpgLevel(i);
            if (lvl >= u.maxLevel) return false;
            if (!TrySpend(GameConfig.UpgradeCost(i, lvl)))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return false;
            }
            D.upgrades[i] = lvl + 1;
            RecalcStats();
            foreach (var t in Treadmill.All) t.Refresh();
            GameAudio.Play(Sfx.Buy);
            if (PlayerController.Instance != null)
                Fx.Burst(PlayerController.Instance.transform.position + Vector3.up * 2f, u.color, 20, 5f, 0.4f, 0.7f);
            SaveNow(false);
            return true;
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
            double price = GameConfig.SellPrice(def, p.level) * CoinMultiplier;
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
            Fx.Confetti(PlayerController.Instance.transform.position + Vector3.up * 2f, 80);
            UIManager.Instance.Toast(Loc.F("rebirth_done", GameConfig.RebirthMultiplier(D.rebirths).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)), new Color(1f, 0.6f, 1f), 4f);
            PlayerController.Instance.DropCarried(true);
            PlayerController.Instance.Respawn();
            SaveNow(true);
            return true;
        }

        // ===================== Прокачка драконов =====================
        public double DragonUpgradeCost(int plot)
        {
            var p = D.plots[plot];
            return GameConfig.DragonUpgradeCost(GameConfig.GetDragon(p.dragonId), p.level);
        }

        public bool TryUpgradeDragon(int plot)
        {
            var p = D.plots[plot];
            if (p.state != (int)PlotState.Dragon || p.level >= GameConfig.DragonMaxLevel) return false;
            if (!TrySpend(DragonUpgradeCost(plot)))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return false;
            }
            p.level++;
            RecalcStats();
            GameAudio.Play(Sfx.Buy);
            var def = GameConfig.GetDragon(p.dragonId);
            if (plot < Plots.Count)
            {
                Fx.Burst(Plots[plot].transform.position + Vector3.up * 2f, GameConfig.GetTier(def.tier).color, 30, 6f, 0.5f, 0.8f);
                FloatingText.Spawn(Plots[plot].transform.position + Vector3.up * 5f, (Loc.Ru ? "Ур. " : "Lv. ") + p.level + "!", new Color(1f, 0.9f, 0.3f));
                Plots[plot].ForceRefresh();
            }
            SaveNow(false);
            return true;
        }

        /// <summary>Самый высокий тир зоны, куда игрок уже может войти.</summary>
        public int HighestUnlockedTier
        {
            get
            {
                int best = 0;
                for (int i = 0; i < GameConfig.Tiers.Length; i++) if (D.speedPoints >= GameConfig.Tiers[i].reqPoints) best = i;
                return best;
            }
        }

        /// <summary>Скорость тренировки в секунду на лучшей открытой дорожке (для наград).</summary>
        public double BestTrainRate
        {
            get
            {
                double best = 0;
                for (int i = 0; i < GameConfig.Treadmills.Length; i++)
                    if ((D.treadmillsMask & (1 << i)) != 0) best = System.Math.Max(best, GameConfig.Treadmills[i].gainPerSec);
                return best * TrainMultiplier;
            }
        }

        void AddEgg(int tier)
        {
            tier = Mathf.Clamp(tier, 0, GameConfig.Tiers.Length - 1);
            D.inventory.Add(new EggItem { tier = tier, dragonId = -1 });
        }

        // ===================== Ежедневная награда =====================
        public static long Today { get { return SaveData.Now() / 86400; } }
        public bool DailyAvailable { get { return D.lastDailyDay != Today; } }

        /// <summary>Какой день серии будет забран сейчас (1..7).</summary>
        public int DailyNextDay
        {
            get
            {
                int streak = (D.lastDailyDay == Today - 1) ? D.dailyStreak : 0;
                return streak % GameConfig.DailyDays + 1;
            }
        }

        public double DailyCoins(int day)
        {
            double baseC = System.Math.Max(300, CoinsPerSec * 300);
            double[] mult = { 1, 0, 2, 0, 4, 0, 6 };
            return baseC * mult[(day - 1) % 7];
        }

        public double DailySpeed(int day)
        {
            double baseS = System.Math.Max(100, BestTrainRate * 300);
            return (day == 2 ? 1 : day == 6 ? 2.5 : 0) * baseS;
        }

        public int DailyEggTier(int day)
        {
            if (day == 4) return HighestUnlockedTier;
            if (day == 7) return HighestUnlockedTier + 1;
            return -1;
        }

        public string DailyText(int day)
        {
            var sb = new System.Text.StringBuilder();
            if (DailyCoins(day) > 0) sb.Append("+" + Loc.Num(DailyCoins(day)) + (Loc.Ru ? " монет" : " coins"));
            if (DailySpeed(day) > 0) { if (sb.Length > 0) sb.Append("\n"); sb.Append("+" + Loc.Num(DailySpeed(day)) + (Loc.Ru ? " скорости" : " speed")); }
            int egg = DailyEggTier(day);
            if (egg >= 0) { if (sb.Length > 0) sb.Append("\n"); sb.Append((Loc.Ru ? "Яйцо: " : "Egg: ") + Loc.TierName((Tier)Mathf.Clamp(egg, 0, GameConfig.Tiers.Length - 1))); }
            return sb.ToString();
        }

        public void ClaimDaily(bool doubled)
        {
            if (!DailyAvailable) return;
            int day = DailyNextDay;
            int mult = doubled ? 2 : 1;
            D.coins += DailyCoins(day) * mult;
            D.speedPoints += DailySpeed(day) * mult;
            int egg = DailyEggTier(day);
            if (egg >= 0) { AddEgg(egg); if (doubled) AddEgg(egg); }
            D.dailyStreak = day;
            D.lastDailyDay = Today;
            GameAudio.Play(Sfx.Rebirth);
            if (PlayerController.Instance != null) Fx.Confetti(PlayerController.Instance.transform.position + Vector3.up * 3f, 80);
            UIManager.Instance.Toast((Loc.Ru ? "Награда за день " : "Day reward ") + day + "!", new Color(1f, 0.9f, 0.3f), 3f);
            SaveNow(true);
        }

        // ===================== Покупки за Яны =====================
        /// <summary>Выдать товар. Возвращает true, если это расходуемый товар (его нужно consume).</summary>
        public bool GrantProduct(string id)
        {
            ProductDef def = null;
            foreach (var p in GameConfig.Products) if (p.id == id) def = p;
            if (def == null) return true;
            string msg = "";
            switch (def.kind)
            {
                case ProductKind.Coins:
                {
                    double c = System.Math.Max(2000 * def.amount / 15.0, CoinsPerSec * 60 * def.amount);
                    D.coins += c;
                    msg = "+" + Loc.Num(c) + (Loc.Ru ? " монет" : " coins");
                    break;
                }
                case ProductKind.Speed:
                {
                    double sp = System.Math.Max(500, BestTrainRate * 60 * def.amount);
                    D.speedPoints += sp;
                    msg = "+" + Loc.Num(sp) + (Loc.Ru ? " скорости" : " speed");
                    break;
                }
                case ProductKind.Egg:
                    AddEgg(HighestUnlockedTier + 1);
                    msg = Loc.Ru ? "Золотое яйцо в инвентаре!" : "Golden egg added!";
                    break;
                case ProductKind.Permanent:
                    if (!D.ownedProducts.Contains(id)) D.ownedProducts.Add(id);
                    msg = Loc.Ru ? def.nameRu + " активирован!" : def.nameEn + " activated!";
                    break;
            }
            RecalcStats();
            foreach (var t in Treadmill.All) t.Refresh();
            GameAudio.Play(Sfx.Rebirth);
            if (PlayerController.Instance != null) Fx.Confetti(PlayerController.Instance.transform.position + Vector3.up * 3f, 90);
            UIManager.Instance.Toast(msg, new Color(1f, 0.9f, 0.3f), 3.5f);
            SaveNow(true);
            return def.kind != ProductKind.Permanent;
        }

        // ===================== Реклама каждые 2 минуты =====================
        float adTimer;
        float adCountdown = -1f;

        /// <summary>
        /// Полноэкранная реклама раз в 2 минуты игры. Показывается только в безопасный момент
        /// (не несём яйцо, нет рулетки/меню) и с предупреждением за 3 секунды — как рекомендует Яндекс.
        /// Сам SDK дополнительно ограничивает частоту (настраивается в консоли разработчика).
        /// </summary>
        void UpdateAdTimer(float dt)
        {
            var ui = UIManager.Instance;
            if (ui == null || InputState.Blocked || YandexSDK.AdOpen) return;
            var p = PlayerController.Instance;
            bool safe = p != null && p.Carrying == null && !Roulette.Active && !Opening;
            if (adCountdown >= 0)
            {
                if (!safe) { adCountdown = -1f; return; }
                adCountdown -= dt;
                ui.ShowAdCountdown(Mathf.CeilToInt(adCountdown));
                if (adCountdown <= 0)
                {
                    adCountdown = -1f;
                    adTimer = 0;
                    ui.ShowAdCountdown(0);
                    YandexSDK.ShowInterstitial(true);
                }
                return;
            }
            adTimer += dt;
            if (adTimer >= GameConfig.AdInterval && safe) adCountdown = 3f;
        }

        public void ResetAdTimer() { adTimer = 0; }

        public void OnCaught()
        {
            // реклама теперь показывается по таймеру (каждые 2 минуты), см. UpdateAdTimer
        }
    }
}
