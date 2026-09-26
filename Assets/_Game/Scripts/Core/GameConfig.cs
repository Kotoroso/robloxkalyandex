using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    public enum Tier { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5, Secret = 6 }

    public enum BrainrotKind { TungSahur, Lirili, Bombardiro, Tralalero, Patapim, Cappuccino, VacaSaturno }

    public class TierInfo
    {
        public Tier tier;
        public string nameRu, nameEn;
        public Color color;
        public int growSeconds;      // сколько растёт яйцо
        public float guardSpeed;     // скорость охранников в зоне
        public int guardCount;
        public float aggroRadius;
        public BrainrotKind guardKind;
        public float guardScale;
    }

    public class DragonDef
    {
        public int id;
        public Tier tier;
        public string nameRu, nameEn;
        public Color body, belly, wing;
        public float speedPct;       // +% к скорости бега
        public float coinsPerSec;    // доход в секунду
        public float trainPct;       // +% к прокачке на дорожках
        public float jumpBonus;      // + к силе прыжка
        public float weight;         // шанс выпадения внутри тира
    }

    public class TreadmillDef
    {
        public string nameRu, nameEn;
        public float gainPerSec;
        public double price;
        public int rebirthsRequired;
        public Color color;
    }

    public static class GameConfig
    {
        public const float BaseWalkSpeed = 12f;
        public const float MaxWalkSpeed = 90f;
        public const float SpeedPointsPerUnit = 40f;   // очков скорости за +1 к скорости бега
        public const float BaseJump = 7.5f;
        public const int StartPlots = 4;
        public const int MaxPlots = 12;
        public const float AdSpeedupFactor = 0.1f;      // реклама режет оставшееся время на 90%
        public const float InterstitialCooldown = 90f;
        public const float OfflineIncomeFactor = 0.5f;
        public const int OfflineMaxSeconds = 2 * 3600;

        // Геометрия мира
        public const float BaseMinZ = -34f, BaseMaxZ = 8f;
        public const float RunwayWidth = 34f;
        public const float FirstZoneZ = 52f;
        public const float ZoneStep = 50f;
        public const float ZoneLength = 40f;
        public const float LeashRadius = 26f;

        public static readonly TierInfo[] Tiers =
        {
            new TierInfo{ tier=Tier.Common,    nameRu="Обычное",     nameEn="Common",    color=new Color(0.75f,0.75f,0.75f), growSeconds=20,   guardSpeed=9f,  guardCount=1, aggroRadius=9f,  guardKind=BrainrotKind.TungSahur,  guardScale=1.0f },
            new TierInfo{ tier=Tier.Uncommon,  nameRu="Необычное",   nameEn="Uncommon",  color=new Color(0.35f,0.85f,0.35f), growSeconds=60,   guardSpeed=13f, guardCount=2, aggroRadius=10f, guardKind=BrainrotKind.Lirili,     guardScale=1.1f },
            new TierInfo{ tier=Tier.Rare,      nameRu="Редкое",      nameEn="Rare",      color=new Color(0.25f,0.55f,1f),    growSeconds=180,  guardSpeed=18f, guardCount=2, aggroRadius=11f, guardKind=BrainrotKind.Bombardiro, guardScale=1.2f },
            new TierInfo{ tier=Tier.Epic,      nameRu="Эпическое",   nameEn="Epic",      color=new Color(0.7f,0.3f,1f),      growSeconds=480,  guardSpeed=24f, guardCount=3, aggroRadius=12f, guardKind=BrainrotKind.Tralalero,  guardScale=1.3f },
            new TierInfo{ tier=Tier.Legendary, nameRu="Легендарное", nameEn="Legendary", color=new Color(1f,0.78f,0.1f),     growSeconds=1200, guardSpeed=31f, guardCount=3, aggroRadius=13f, guardKind=BrainrotKind.Patapim,    guardScale=1.45f },
            new TierInfo{ tier=Tier.Mythic,    nameRu="Мифическое",  nameEn="Mythic",    color=new Color(1f,0.25f,0.3f),     growSeconds=2700, guardSpeed=40f, guardCount=4, aggroRadius=14f, guardKind=BrainrotKind.Cappuccino, guardScale=1.6f },
            new TierInfo{ tier=Tier.Secret,    nameRu="Секретное",   nameEn="Secret",    color=new Color(0.1f,1f,0.95f),     growSeconds=5400, guardSpeed=52f, guardCount=4, aggroRadius=15f, guardKind=BrainrotKind.VacaSaturno,guardScale=1.8f },
        };

        public static readonly DragonDef[] Dragons =
        {
            D(0,  Tier.Common,    "Ящерок",           "Lizzy",           "#8FBF5A", "#E8E0A0", "#6E9A40", 2,  1,     2,  0.0f, 70),
            D(1,  Tier.Common,    "Пепельный",        "Ashling",         "#9A9A9A", "#D0D0D0", "#707070", 1,  2,     3,  0.2f, 30),
            D(2,  Tier.Uncommon,  "Листохвост",       "Leaftail",        "#3FBF4F", "#B5F07A", "#2A8A38", 4,  4,     4,  0.3f, 60),
            D(3,  Tier.Uncommon,  "Болотник",         "Swampy",          "#4E7A3A", "#9AB06A", "#33552A", 3,  6,     5,  0.2f, 40),
            D(4,  Tier.Rare,      "Ледяной Клык",     "Frostfang",       "#6EC8FF", "#E6F7FF", "#3A8FD0", 7,  12,    8,  0.5f, 60),
            D(5,  Tier.Rare,      "Грозовик",         "Stormy",          "#3050C0", "#A0B8FF", "#FFE040", 9,  10,    7,  0.6f, 40),
            D(6,  Tier.Epic,      "Аметистовый",      "Amethyst",        "#A040F0", "#E0B0FF", "#6A20B0", 12, 35,    12, 0.8f, 55),
            D(7,  Tier.Epic,      "Теневой",          "Shadow",          "#2A2438", "#6A5A8A", "#A040F0", 15, 28,    15, 1.0f, 45),
            D(8,  Tier.Legendary, "Золотой Император","Golden Emperor",  "#FFC820", "#FFF0A0", "#E08A10", 20, 120,   20, 1.2f, 50),
            D(9,  Tier.Legendary, "Солнечный Феникс", "Sun Phoenix",     "#FF8A20", "#FFE060", "#FF3A10", 24, 100,   25, 1.5f, 50),
            D(10, Tier.Mythic,    "Вулканорог",       "Volcanohorn",     "#D02020", "#FF9040", "#401010", 30, 400,   30, 1.8f, 55),
            D(11, Tier.Mythic,    "Кровавая Луна",    "Blood Moon",      "#8A0A2A", "#FF5070", "#200008", 35, 350,   40, 2.0f, 45),
            D(12, Tier.Secret,    "Космический",      "Cosmic",          "#1A1060", "#50F0FF", "#FF40E0", 50, 1500,  60, 3.0f, 60),
            D(13, Tier.Secret,    "Радужный Бог",     "Rainbow God",     "#FFFFFF", "#FF60A0", "#40FFB0", 60, 1200,  80, 3.5f, 40),
        };

        public static readonly TreadmillDef[] Treadmills =
        {
            new TreadmillDef{ nameRu="Дорожка",         nameEn="Treadmill",        gainPerSec=4f,   price=0,       rebirthsRequired=0, color=new Color(0.2f,0.8f,0.3f) },
            new TreadmillDef{ nameRu="Быстрая дорожка", nameEn="Fast Treadmill",   gainPerSec=15f,  price=400,     rebirthsRequired=0, color=new Color(0.2f,0.5f,1f) },
            new TreadmillDef{ nameRu="Турбо дорожка",   nameEn="Turbo Treadmill",  gainPerSec=60f,  price=6000,    rebirthsRequired=1, color=new Color(0.8f,0.3f,1f) },
            new TreadmillDef{ nameRu="Ракетная дорожка",nameEn="Rocket Treadmill", gainPerSec=250f, price=90000,   rebirthsRequired=3, color=new Color(1f,0.5f,0.1f) },
            new TreadmillDef{ nameRu="Космо дорожка",   nameEn="Cosmic Treadmill", gainPerSec=1000f,price=2000000, rebirthsRequired=6, color=new Color(0.1f,1f,0.95f) },
        };

        public static TierInfo GetTier(Tier t) { return Tiers[(int)t]; }
        public static TierInfo GetTier(int t) { return Tiers[Mathf.Clamp(t, 0, Tiers.Length - 1)]; }

        public static DragonDef GetDragon(int id)
        {
            for (int i = 0; i < Dragons.Length; i++) if (Dragons[i].id == id) return Dragons[i];
            return Dragons[0];
        }

        public static DragonDef RollDragon(Tier tier)
        {
            var list = new List<DragonDef>();
            float total = 0;
            foreach (var d in Dragons) if (d.tier == tier) { list.Add(d); total += d.weight; }
            float r = Random.value * total;
            foreach (var d in list) { r -= d.weight; if (r <= 0) return d; }
            return list[list.Count - 1];
        }

        public static float ZoneCenterZ(int tier) { return FirstZoneZ + tier * ZoneStep; }

        public static double RebirthCost(int rebirths) { return 2500 * System.Math.Pow(3.2, rebirths); }
        public static double PlotCost(int plotsOwned) { return 150 * System.Math.Pow(3, plotsOwned - StartPlots); }
        public static float RebirthMultiplier(int rebirths) { return 1f + 0.5f * rebirths; }
        public static double SellPrice(DragonDef d) { return d.coinsPerSec * 60; }

        static DragonDef D(int id, Tier t, string ru, string en, string body, string belly, string wing,
            float speedPct, float cps, float trainPct, float jump, float weight)
        {
            return new DragonDef
            {
                id = id, tier = t, nameRu = ru, nameEn = en,
                body = Hex(body), belly = Hex(belly), wing = Hex(wing),
                speedPct = speedPct, coinsPerSec = cps, trainPct = trainPct, jumpBonus = jump, weight = weight
            };
        }

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }
    }
}
