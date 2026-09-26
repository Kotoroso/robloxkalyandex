using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    public enum Tier { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5, Divine = 6, Secret = 7, Celestial = 8, Ancient = 9, Galactic = 10, Omega = 11 }

    public enum BrainrotKind { TungSahur, Lirili, Bombardiro, Tralalero, Patapim, Cappuccino, VacaSaturno }

    /// <summary>Визуальный эффект дракона.</summary>
    public enum DragonFx { None, Sparkle, Fire, Frost, Aura, Halo, Rainbow, Stars, Void }

    public class TierInfo
    {
        public Tier tier;
        public string nameRu, nameEn;
        public Color color;
        public int growSeconds;      // сколько растёт яйцо
        public float guardSpeed;     // скорость охранников в зоне (юниты/сек)
        public int guardCount;
        public float aggroRadius;
        public BrainrotKind guardKind;
        public float guardScale;
        public double reqPoints;     // очки скорости (с беговых дорожек), нужные чтобы пройти в зону
    }

    public class DragonDef
    {
        public int id;
        public Tier tier;
        public string nameRu, nameEn;
        public Color body, belly, wing;
        public float speedPct;       // +% к скорости бега
        public double coinsPerSec;   // доход в секунду
        public float trainPct;       // +% к прокачке на дорожках
        public float jumpBonus;      // + к силе прыжка
        public float weight;         // шанс выпадения внутри тира
        public DragonFx fx;
    }

    public class TreadmillDef
    {
        public string nameRu, nameEn;
        public double gainPerSec;
        public double price;
        public int rebirthsRequired;
        public int adsRequired;      // открыть за просмотр рекламы
        public Color color;
    }

    public class UpgradeDef
    {
        public string id, nameRu, nameEn, descRu, descEn;
        public double baseCost, costMult;
        public int maxLevel;
        public Color color;
    }

    /// <summary>
    /// Весь баланс игры. Числа растут экспоненциально (К → М → Б → Т), как в роблокс-симуляторах:
    /// каждая следующая зона требует примерно x15-25 скорости, доход драконов растёт примерно x8-12 за тир.
    /// </summary>
    public static class GameConfig
    {
        public const float BaseWalkSpeed = 12f;
        public const float BaseJump = 7.5f;
        public const int StartPlots = 4;
        public const int MaxPlots = 12;
        public const int InventorySlots = 5;
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

        /// <summary>Скорость бега растёт логарифмически от очков: 50 → 17, 8К → 28, 2.5М → 40, 40Б → 61.</summary>
        public static float WalkSpeedFromPoints(double points)
        {
            return BaseWalkSpeed + 5f * (float)System.Math.Log10(1.0 + System.Math.Max(0, points) / 5.0);
        }

        public static readonly TierInfo[] Tiers =
        {
            T(Tier.Common,    "Обычное",      "Common",    0.78f,0.78f,0.78f, 20,   9f,   1, 9f,  BrainrotKind.TungSahur,   1.0f, 0),
            T(Tier.Uncommon,  "Необычное",    "Uncommon",  0.35f,0.85f,0.35f, 45,   15.5f,2, 10f, BrainrotKind.Lirili,      1.1f, 50),
            T(Tier.Rare,      "Редкое",       "Rare",      0.25f,0.55f,1f,    90,   20.5f,2, 11f, BrainrotKind.Bombardiro,  1.2f, 600),
            T(Tier.Epic,      "Эпическое",    "Epic",      0.7f,0.3f,1f,      180,  26f,  3, 12f, BrainrotKind.Tralalero,   1.3f, 8e3),
            T(Tier.Legendary, "Легендарное",  "Legendary", 1f,0.78f,0.1f,     300,  32f,  3, 13f, BrainrotKind.Patapim,     1.45f, 1.2e5),
            T(Tier.Mythic,    "Мифическое",   "Mythic",    1f,0.25f,0.3f,     480,  38.5f,4, 14f, BrainrotKind.Cappuccino,  1.6f, 2.5e6),
            T(Tier.Divine,    "Божественное", "Divine",    1f,0.95f,0.6f,     720,  45f,  4, 15f, BrainrotKind.VacaSaturno, 1.7f, 6e7),
            T(Tier.Secret,    "Секретное",    "Secret",    0.1f,1f,0.95f,     1080, 52f,  4, 16f, BrainrotKind.Bombardiro,  2.0f, 1.5e9),
            T(Tier.Celestial, "Небесное",     "Celestial", 0.55f,0.45f,1f,    1500, 59f,  5, 17f, BrainrotKind.TungSahur,   2.3f, 4e10),
            T(Tier.Ancient,   "Древнее",      "Ancient",   0.85f,0.52f,0.25f, 1800, 66f,  5, 18f, BrainrotKind.Tralalero,   2.5f, 1.2e12),
            T(Tier.Galactic,  "Галактическое","Galactic",  0.25f,0.35f,0.95f, 2400, 73f,  5, 19f, BrainrotKind.Patapim,     2.7f, 4e13),
            T(Tier.Omega,     "Омега",        "Omega",     1f,0.15f,0.6f,     3000, 80f,  6, 20f, BrainrotKind.VacaSaturno, 3.0f, 1.5e15),
        };

        public static readonly DragonDef[] Dragons =
        {
            // id, тир, имя, цвета, +%скор, доход/с, +%прок, прыжок, вес, эффект
            D(0,  Tier.Common,    "Ящерок",            "Lizzy",            "#8FBF5A","#E8E0A0","#6E9A40", 2,  3,      3,  0.0f, 60, DragonFx.None),
            D(1,  Tier.Common,    "Пепельный",         "Ashling",          "#9A9A9A","#D0D0D0","#707070", 1,  4,      4,  0.2f, 30, DragonFx.None),
            D(2,  Tier.Common,    "Песчаник",          "Sandy",            "#D9B870","#F5E6B8","#B08A40", 3,  5,      2,  0.1f, 10, DragonFx.None),
            D(3,  Tier.Uncommon,  "Листохвост",        "Leaftail",         "#3FBF4F","#B5F07A","#2A8A38", 4,  15,     6,  0.3f, 55, DragonFx.None),
            D(4,  Tier.Uncommon,  "Болотник",          "Swampy",           "#4E7A3A","#9AB06A","#33552A", 3,  20,     8,  0.2f, 35, DragonFx.None),
            D(5,  Tier.Uncommon,  "Коралл",            "Coral",            "#FF7F7F","#FFD0C0","#E05050", 5,  25,     6,  0.3f, 10, DragonFx.Sparkle),
            D(6,  Tier.Rare,      "Ледяной Клык",      "Frostfang",        "#6EC8FF","#E6F7FF","#3A8FD0", 7,  90,     12, 0.5f, 55, DragonFx.Frost),
            D(7,  Tier.Rare,      "Грозовик",          "Stormy",           "#3050C0","#A0B8FF","#FFE040", 9,  110,    10, 0.6f, 35, DragonFx.Sparkle),
            D(8,  Tier.Rare,      "Сапфир",            "Sapphire",         "#2060FF","#80C0FF","#1030A0", 8,  140,    14, 0.5f, 10, DragonFx.Frost),
            D(9,  Tier.Epic,      "Аметистовый",       "Amethyst",         "#A040F0","#E0B0FF","#6A20B0", 12, 700,    18, 0.8f, 55, DragonFx.Sparkle),
            D(10, Tier.Epic,      "Теневой",           "Shadow",           "#2A2438","#6A5A8A","#A040F0", 15, 850,    20, 1.0f, 35, DragonFx.Void),
            D(11, Tier.Epic,      "Токсик",            "Toxic",            "#70FF40","#D0FF80","#308020", 14, 1000,   22, 0.9f, 10, DragonFx.Aura),
            D(12, Tier.Legendary, "Золотой Император", "Golden Emperor",   "#FFC820","#FFF0A0","#E08A10", 20, 6e3,    30, 1.2f, 50, DragonFx.Halo),
            D(13, Tier.Legendary, "Солнечный Феникс",  "Sun Phoenix",      "#FF8A20","#FFE060","#FF3A10", 24, 7.5e3,  32, 1.5f, 40, DragonFx.Fire),
            D(14, Tier.Legendary, "Молниевый Рык",     "Thunder Roar",     "#FFE640","#FFFFFF","#3060FF", 26, 9e3,    35, 1.6f, 10, DragonFx.Sparkle),
            D(15, Tier.Mythic,    "Вулканорог",        "Volcanohorn",      "#D02020","#FF9040","#401010", 30, 6e4,    45, 1.8f, 50, DragonFx.Fire),
            D(16, Tier.Mythic,    "Кровавая Луна",     "Blood Moon",       "#8A0A2A","#FF5070","#200008", 35, 7.5e4,  50, 2.0f, 40, DragonFx.Aura),
            D(17, Tier.Mythic,    "Изумрудный Страж",  "Emerald Warden",   "#10C080","#A0FFD0","#086040", 38, 9e4,    55, 2.1f, 10, DragonFx.Aura),
            D(18, Tier.Divine,    "Архангел",          "Archangel",        "#FFFFFF","#FFF4C0","#FFD860", 45, 6e5,    70, 2.4f, 50, DragonFx.Halo),
            D(19, Tier.Divine,    "Громовержец",       "Thunder God",      "#F0E0A0","#FFFFFF","#60A0FF", 50, 7.5e5,  75, 2.6f, 40, DragonFx.Sparkle),
            D(20, Tier.Divine,    "Солнцебог",         "Sun God",          "#FFB020","#FFF080","#FF6000", 55, 9e5,    80, 2.8f, 10, DragonFx.Fire),
            D(21, Tier.Secret,    "Космический",       "Cosmic",           "#1A1060","#50F0FF","#FF40E0", 65, 8e6,    100,3.0f, 50, DragonFx.Stars),
            D(22, Tier.Secret,    "Радужный Бог",      "Rainbow God",      "#FFFFFF","#FF60A0","#40FFB0", 70, 1e7,    110,3.5f, 40, DragonFx.Rainbow),
            D(23, Tier.Secret,    "Пустотник",         "Voidling",         "#100818","#8030FF","#000000", 75, 1.2e7,  120,3.5f, 10, DragonFx.Void),
            D(24, Tier.Celestial, "Звёздный Змей",     "Star Serpent",     "#3040C0","#C0D0FF","#FFFFFF", 90, 1.2e8,  150,4.0f, 50, DragonFx.Stars),
            D(25, Tier.Celestial, "Галактион",         "Galaxion",         "#6020A0","#FF80FF","#20E0FF", 100,1.6e8,  170,4.2f, 40, DragonFx.Rainbow),
            D(26, Tier.Celestial, "Бесконечность",     "Infinity",         "#000000","#FFFFFF","#FFD700", 120,2e8,    200,4.5f, 10, DragonFx.Halo),
            D(27, Tier.Ancient,   "Окаменелый Титан",  "Fossil Titan",     "#A08060","#E0D0B0","#604020", 140,2e9,    240,4.8f, 50, DragonFx.Sparkle),
            D(28, Tier.Ancient,   "Руный Змей",        "Rune Wyrm",        "#406080","#80FFFF","#203040", 150,2.5e9,  260,5.0f, 40, DragonFx.Aura),
            D(29, Tier.Ancient,   "Первородный",       "Primordial",       "#C06020","#FFD080","#FF2000", 170,3e9,    300,5.2f, 10, DragonFx.Fire),
            D(30, Tier.Galactic,  "Туманность",        "Nebula",           "#4020C0","#FF80E0","#20C0FF", 200,3e10,   360,5.5f, 50, DragonFx.Stars),
            D(31, Tier.Galactic,  "Квазар",            "Quasar",           "#FFFFFF","#80C0FF","#FFFF80", 220,4e10,   400,5.8f, 40, DragonFx.Halo),
            D(32, Tier.Galactic,  "Чёрная Дыра",       "Black Hole",       "#050008","#6000FF","#FF6000", 250,5e10,   450,6.0f, 10, DragonFx.Void),
            D(33, Tier.Omega,     "Омега Прайм",       "Omega Prime",      "#FF1060","#FFD0E0","#400010", 300,5e11,   550,6.5f, 50, DragonFx.Fire),
            D(34, Tier.Omega,     "Хронос",            "Chronos",          "#E0C060","#FFFFFF","#2040FF", 340,6.5e11, 620,7.0f, 40, DragonFx.Stars),
            D(35, Tier.Omega,     "Абсолют",           "The Absolute",     "#FFFFFF","#000000","#FF00FF", 400,8e11,   700,7.5f, 10, DragonFx.Rainbow),
        };

        public static readonly TreadmillDef[] Treadmills =
        {
            new TreadmillDef{ nameRu="Дорожка",          nameEn="Treadmill",        gainPerSec=2,     price=0,    rebirthsRequired=0, color=new Color(0.2f,0.8f,0.3f) },
            new TreadmillDef{ nameRu="Быстрая дорожка",  nameEn="Fast Treadmill",   gainPerSec=12,    price=0,    rebirthsRequired=0, adsRequired=2, color=new Color(0.2f,0.5f,1f) },
            new TreadmillDef{ nameRu="Турбо дорожка",    nameEn="Turbo Treadmill",  gainPerSec=250,   price=15e3, rebirthsRequired=0, color=new Color(0.8f,0.3f,1f) },
            new TreadmillDef{ nameRu="Ракетная дорожка", nameEn="Rocket Treadmill", gainPerSec=6e3,   price=2e6,  rebirthsRequired=1, color=new Color(1f,0.5f,0.1f) },
            new TreadmillDef{ nameRu="Космо дорожка",    nameEn="Cosmic Treadmill", gainPerSec=1.5e5, price=1e9,  rebirthsRequired=3, color=new Color(0.1f,1f,0.95f) },
            new TreadmillDef{ nameRu="Гипер дорожка",    nameEn="Hyper Treadmill",  gainPerSec=5e6,   price=1e12, rebirthsRequired=6, color=new Color(1f,0.85f,0.2f) },
            new TreadmillDef{ nameRu="Омега дорожка",    nameEn="Omega Treadmill",  gainPerSec=2e8,   price=5e15, rebirthsRequired=10, color=new Color(1f,0.15f,0.6f) },
        };

        public static readonly UpgradeDef[] Upgrades =
        {
            new UpgradeDef{ id="train",  nameRu="Тренер",        nameEn="Coach",        descRu="+25% к прокачке скорости",     descEn="+25% speed training",   baseCost=100,  costMult=2.1, maxLevel=60, color=new Color(0.3f,0.8f,1f) },
            new UpgradeDef{ id="income", nameRu="Кормушка",      nameEn="Feeder",       descRu="+20% к доходу драконов",       descEn="+20% dragon income",    baseCost=250,  costMult=2.2, maxLevel=60, color=new Color(1f,0.8f,0.2f) },
            new UpgradeDef{ id="grow",   nameRu="Удобрение",     nameEn="Fertilizer",   descRu="-5% ко времени роста яиц",      descEn="-5% egg grow time",     baseCost=1e3,  costMult=4,   maxLevel=10, color=new Color(0.4f,0.9f,0.4f) },
            new UpgradeDef{ id="luck",   nameRu="Удача",         nameEn="Luck",         descRu="+2% шанс на дракона тиром выше", descEn="+2% higher tier chance", baseCost=2e3, costMult=4,   maxLevel=10, color=new Color(0.9f,0.4f,1f) },
            new UpgradeDef{ id="jump",   nameRu="Пружины",       nameEn="Springs",      descRu="+0.5 к прыжку",                descEn="+0.5 jump power",       baseCost=300,  costMult=2.6, maxLevel=10, color=new Color(1f,0.5f,0.4f) },
        };

        public static TierInfo GetTier(Tier t) { return Tiers[(int)t]; }
        public static TierInfo GetTier(int t) { return Tiers[Mathf.Clamp(t, 0, Tiers.Length - 1)]; }

        public static DragonDef GetDragon(int id)
        {
            for (int i = 0; i < Dragons.Length; i++) if (Dragons[i].id == id) return Dragons[i];
            return Dragons[0];
        }

        public static DragonDef RollInTier(Tier tier)
        {
            var list = new List<DragonDef>();
            float total = 0;
            foreach (var d in Dragons) if (d.tier == tier) { list.Add(d); total += d.weight; }
            float r = Random.value * total;
            foreach (var d in list) { r -= d.weight; if (r <= 0) return d; }
            return list[list.Count - 1];
        }

        /// <summary>
        /// Шансы тира дракона из яйца тира T (как в кейсах): T-1: 25%, T: 60%, T+1: 12%, T+2: 3%.
        /// Каждый уровень удачи переносит 2% с T-1/T на T+1.
        /// </summary>
        public static float[] TierChances(Tier egg, int luckLevel)
        {
            int t = (int)egg, n = Tiers.Length;
            var c = new float[n];
            float lower = t > 0 ? 0.25f : 0f, same = t > 0 ? 0.60f : 0.85f, up1 = 0.12f, up2 = 0.03f;
            float shift = luckLevel * 0.02f;
            float fromLower = Mathf.Min(lower, shift); lower -= fromLower; up1 += fromLower;
            float rest = shift - fromLower; same -= rest; up1 += rest;
            if (t > 0) c[t - 1] += lower;
            c[t] += same;
            if (t + 1 < n) c[t + 1] += up1; else c[t] += up1;
            if (t + 2 < n) c[t + 2] += up2; else c[Mathf.Min(t + 1, n - 1)] += up2;
            return c;
        }

        public static Tier RollTier(Tier egg, int luckLevel)
        {
            var c = TierChances(egg, luckLevel);
            float r = Random.value;
            for (int i = 0; i < c.Length; i++) { r -= c[i]; if (r <= 0) return (Tier)i; }
            return egg;
        }

        public static float ZoneCenterZ(int tier) { return FirstZoneZ + tier * ZoneStep; }

        public static double RebirthCost(int rebirths) { return 1e5 * System.Math.Pow(4.5, rebirths); }
        public static double PlotCost(int plotsOwned) { return 500 * System.Math.Pow(3.5, plotsOwned - StartPlots); }
        public static float RebirthMultiplier(int rebirths) { return 1f + rebirths; }
        public static double SellPrice(DragonDef d) { return d.coinsPerSec * 90; }
        public static double UpgradeCost(int i, int level) { var u = Upgrades[i]; return u.baseCost * System.Math.Pow(u.costMult, level); }

        static TierInfo T(Tier t, string ru, string en, float r, float g, float b, int grow, float gs, int gc, float aggro, BrainrotKind k, float scale, double req)
        {
            return new TierInfo { tier = t, nameRu = ru, nameEn = en, color = new Color(r, g, b), growSeconds = grow, guardSpeed = gs, guardCount = gc,
                aggroRadius = aggro, guardKind = k, guardScale = scale, reqPoints = req };
        }

        static DragonDef D(int id, Tier t, string ru, string en, string body, string belly, string wing,
            float speedPct, double cps, float trainPct, float jump, float weight, DragonFx fx)
        {
            return new DragonDef
            {
                id = id, tier = t, nameRu = ru, nameEn = en,
                body = Hex(body), belly = Hex(belly), wing = Hex(wing),
                speedPct = speedPct, coinsPerSec = cps, trainPct = trainPct, jumpBonus = jump, weight = weight, fx = fx
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
