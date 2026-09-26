using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    public enum PlotState { Empty = 0, Egg = 1, Dragon = 2 }

    [Serializable]
    public class PlotData
    {
        public int state;       // PlotState
        public int tier;
        public int dragonId;
        public long plantedAt;  // unix sec
        public long readyAt;    // unix sec
        public int level = 1;   // уровень дракона (прокачка за монеты)
    }

    [Serializable]
    public class EggItem
    {
        public int tier;
        public int dragonId;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public double coins;
        public double speedPoints;
        public int rebirths;
        public int plotsOwned = GameConfig.StartPlots;
        public int treadmillsMask = 1;    // бит i = дорожка i куплена
        public List<PlotData> plots = new List<PlotData>();
        public List<EggItem> inventory = new List<EggItem>();
        public int totalStolen;
        public long savedAt;
        public bool soundOn = true;
        public bool musicOn = true;
        public int switchType;            // звук клавиш ASMR-пола: 0 синий, 1 коричневый, 2 красный
        public int tutorialStep;
        public List<int> treadmillAds = new List<int>();   // сколько реклам просмотрено для открытия дорожки
        public List<int> dragonInv = new List<int>();      // 5 слотов, -1 = пусто
        public int selectedSlot = -1;
        public List<int> upgrades = new List<int>();       // уровни улучшений из магазина
        public int totalHatched;
        public List<int> dragonInvLvl = new List<int>();   // уровни драконов в слотах
        public List<int> dragonStore = new List<int>();    // хранилище: до 15 драконов (бонусов не дают)
        public List<int> dragonStoreLvl = new List<int>();
        public bool controlsSeen;
        public long lastDailyDay = -1;                     // номер дня (UTC), когда забрана награда
        public int dailyStreak;
        public List<string> ownedProducts = new List<string>(); // постоянные покупки за Яны
        public List<int> ownedTrails = new List<int>();
        public int equippedTrail = -1;

        public void Normalize()
        {
            if (plots == null) plots = new List<PlotData>();
            if (inventory == null) inventory = new List<EggItem>();
            plotsOwned = GameConfig.MaxPlots;   // покупки грядок нет: старые сохранения с 3-5 грядками получают все 6
            while (plots.Count < GameConfig.MaxPlots) plots.Add(new PlotData());
            treadmillsMask |= 1;
            if (treadmillAds == null) treadmillAds = new List<int>();
            while (treadmillAds.Count < GameConfig.Treadmills.Length) treadmillAds.Add(0);
            if (dragonInv == null) dragonInv = new List<int>();
            while (dragonInv.Count < GameConfig.InventorySlots) dragonInv.Add(-1);
            if (selectedSlot >= GameConfig.InventorySlots) selectedSlot = -1;
            switchType = Mathf.Clamp(switchType, 0, 2);
            if (dragonInvLvl == null) dragonInvLvl = new List<int>();
            while (dragonInvLvl.Count < GameConfig.InventorySlots) dragonInvLvl.Add(1);
            for (int i = 0; i < dragonInvLvl.Count; i++) if (dragonInvLvl[i] < 1) dragonInvLvl[i] = 1;
            foreach (var p in plots) if (p.level < 1) p.level = 1;
            if (ownedProducts == null) ownedProducts = new List<string>();
            if (ownedTrails == null) ownedTrails = new List<int>();
            if (equippedTrail >= GameConfig.Trails.Length || (equippedTrail >= 0 && !ownedTrails.Contains(equippedTrail))) equippedTrail = -1;
            if (upgrades == null) upgrades = new List<int>();
            while (upgrades.Count < GameConfig.Upgrades.Length) upgrades.Add(0);
            // старые сохранения: уровни могли быть выше нового максимума
            for (int i = 0; i < upgrades.Count && i < GameConfig.Upgrades.Length; i++)
                upgrades[i] = Mathf.Clamp(upgrades[i], 0, GameConfig.Upgrades[i].maxLevel);
            for (int i = 0; i < dragonInvLvl.Count; i++) dragonInvLvl[i] = Mathf.Min(dragonInvLvl[i], GameConfig.DragonMaxLevel);
            foreach (var p in plots) p.level = Mathf.Min(p.level, GameConfig.DragonMaxLevel);
            // старые сохранения: выкидываем несуществующих драконов
            for (int i = 0; i < dragonInv.Count; i++) if (dragonInv[i] >= GameConfig.Dragons.Length) dragonInv[i] = -1;
            foreach (var p in plots) if (p.state == 2 && (p.dragonId < 0 || p.dragonId >= GameConfig.Dragons.Length)) p.state = 0;
            if (dragonStore == null) dragonStore = new List<int>();
            if (dragonStoreLvl == null) dragonStoreLvl = new List<int>();
            for (int i = dragonStore.Count - 1; i >= 0; i--)
                if (dragonStore[i] < 0 || dragonStore[i] >= GameConfig.Dragons.Length) { dragonStore.RemoveAt(i); if (i < dragonStoreLvl.Count) dragonStoreLvl.RemoveAt(i); }
            while (dragonStoreLvl.Count < dragonStore.Count) dragonStoreLvl.Add(1);
            while (dragonStoreLvl.Count > dragonStore.Count) dragonStoreLvl.RemoveAt(dragonStoreLvl.Count - 1);
            for (int i = 0; i < dragonStoreLvl.Count; i++) dragonStoreLvl[i] = Mathf.Clamp(dragonStoreLvl[i], 1, GameConfig.DragonMaxLevel);
        }

        public static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
    }

    /// <summary>Локальное сохранение (PlayerPrefs) + облачное через Yandex (player.setData).</summary>
    public static class SaveManager
    {
        const string Key = "dragon_heist_save";
        public static SaveData Data = new SaveData();

        public static SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d != null) d.Normalize();
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save parse error: " + e.Message);
                return null;
            }
        }

        public static SaveData LoadLocal()
        {
            return Parse(PlayerPrefs.GetString(Key, ""));
        }

        /// <summary>Выбирает самое свежее сохранение из локального и облачного.</summary>
        public static void Resolve(string cloudJson)
        {
            var local = LoadLocal();
            var cloud = Parse(cloudJson);
            SaveData best = local;
            if (cloud != null && (best == null || cloud.savedAt > best.savedAt)) best = cloud;
            if (best == null) best = new SaveData();
            best.Normalize();
            Data = best;
        }

        public static void Save(bool cloud)
        {
            Data.savedAt = SaveData.Now();
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
            if (cloud) YandexSDK.SaveCloud(json);
        }
    }
}
