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

        public void Normalize()
        {
            if (plots == null) plots = new List<PlotData>();
            if (inventory == null) inventory = new List<EggItem>();
            plotsOwned = Mathf.Clamp(plotsOwned, GameConfig.StartPlots, GameConfig.MaxPlots);
            while (plots.Count < GameConfig.MaxPlots) plots.Add(new PlotData());
            treadmillsMask |= 1;
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
