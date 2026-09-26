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
        public bool exclusive;       // эксклюзив из донатного Драконьего яйца (не выпадает из обычных)
        public float premiumChance;  // шанс в донатном яйце
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

    public enum ProductKind { Coins, Speed, Egg, Permanent, Trail, NoAdsTimed }

    /// <summary>Товар за Яны. ID должны совпадать с товарами в консоли разработчика Яндекс Игр.</summary>
    public class ProductDef
    {
        public string id, nameRu, nameEn, descRu, descEn;
        public ProductKind kind;
        public float amount;         // множитель награды (для монет/скорости — "минут дохода")
        public Color color;
        public string fallbackPrice; // показывается, пока не загрузился каталог
    }

    /// <summary>Трейл — эффект при беге, умножает прокачку скорости на дорожках.</summary>
    public class TrailDef
    {
        public string nameRu, nameEn, rarityRu, rarityEn;
        public Color a, b;
        public bool rainbow, sparkles;
        public float mult;
        public double coinPrice;
        public string productId, fallbackPrice;
    }

    public class UpgradeDef
    {
        public string id, nameRu, nameEn, descRu, descEn;
        public double baseCost, costMult;
        public double costAccel = 1.0; // ускорение роста цены: каждый следующий уровень дорожает сильнее предыдущего
        public float perLevel;       // эффект за уровень (доля: 0.10 = +10%; для прыжка — абсолютное значение)
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
        /// <summary>Во сколько раз реальное ускорение слабее показанного числа скорости (число на экране прежнее).</summary>
        public const float SpeedScale = 3.5f;
        /// <summary>Реальная скорость движения из "показанной": базовая остаётся, прибавка сверх базы делится на SpeedScale.</summary>
        public static float MoveSpeed(float shown) { return shown <= BaseWalkSpeed ? shown : BaseWalkSpeed + (shown - BaseWalkSpeed) / SpeedScale; }
        public const float BaseJump = 7.5f;
        public const int StartPlots = 6;   // покупки грядок нет: все грядки доступны сразу (= MaxPlots)
        public const int MaxPlots = 6;
        public const int InventorySlots = 5;
        public const int StorageSlots = 15;   // хранилище драконов (без бонусов)
        public const float AdSpeedupFactor = 0.1f;      // реклама режет оставшееся время на 90%
        public const float InterstitialCooldown = 60f;
        public const float AdInterval = 120f;           // полноэкранная реклама каждые 2 минуты игры
        public const float OfflineIncomeFactor = 0.5f;
        public const int OfflineMaxSeconds = 2 * 3600;

        // Геометрия мира
        public const float BaseMinZ = -50f, BaseMaxZ = 8f;
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
            T(Tier.Uncommon,  "Необычное",    "Uncommon",  0.35f,0.85f,0.35f, 45,   15.5f,2, 10f, BrainrotKind.Lirili,      1.1f, 60),
            T(Tier.Rare,      "Редкое",       "Rare",      0.25f,0.55f,1f,    90,   20.5f,2, 11f, BrainrotKind.Bombardiro,  1.2f, 1500),
            T(Tier.Epic,      "Эпическое",    "Epic",      0.7f,0.3f,1f,      180,  26f,  1, 12f, BrainrotKind.Tralalero,   1.3f, 5.4e4),
            T(Tier.Legendary, "Легендарное",  "Legendary", 1f,0.78f,0.1f,     300,  33f,  3, 13f, BrainrotKind.Patapim,     1.45f, 2.7e5),
            T(Tier.Mythic,    "Мифическое",   "Mythic",    1f,0.25f,0.3f,     480,  40f,  4, 14f, BrainrotKind.Cappuccino,  1.6f, 7.5e6),
            T(Tier.Divine,    "Божественное", "Divine",    1f,0.95f,0.6f,     720,  45f,  4, 15f, BrainrotKind.VacaSaturno, 1.7f, 6.5e7),
            T(Tier.Secret,    "Секретное",    "Secret",    0.1f,1f,0.95f,     1080, 51f,  4, 16f, BrainrotKind.Bombardiro,  2.0f, 1.3e9),
            T(Tier.Celestial, "Небесное",     "Celestial", 0.55f,0.45f,1f,    1500, 56f,  5, 17f, BrainrotKind.TungSahur,   2.3f, 1.4e10),
            T(Tier.Ancient,   "Древнее",      "Ancient",   0.85f,0.52f,0.25f, 1800, 64f,  5, 18f, BrainrotKind.Tralalero,   2.5f, 7.6e11),
            T(Tier.Galactic,  "Галактическое","Galactic",  0.25f,0.35f,0.95f, 2400, 72f,  5, 19f, BrainrotKind.Patapim,     2.7f, 3e13),
            T(Tier.Omega,     "Омега",        "Omega",     1f,0.15f,0.6f,     3000, 80f,  6, 20f, BrainrotKind.VacaSaturno, 3.0f, 1.5e15),
        };

        public static readonly DragonDef[] Dragons =
        {
            // id, тир, имя, цвета, +%скор, доход/с, +%прок, прыжок, вес, эффект
            D(0,  Tier.Common,    "Ящерок",            "Lizzy",            "#8FBF5A","#E8E0A0","#6E9A40", 2,  1.8,      3,  0.0f, 60, DragonFx.None),
            D(1,  Tier.Common,    "Пепельный",         "Ashling",          "#9A9A9A","#D0D0D0","#707070", 1,  2.4,      4,  0.2f, 30, DragonFx.None),
            D(2,  Tier.Common,    "Песчаник",          "Sandy",            "#D9B870","#F5E6B8","#B08A40", 3,  3,      2,  0.1f, 4, DragonFx.None),
            D(3,  Tier.Uncommon,  "Листохвост",        "Leaftail",         "#3FBF4F","#B5F07A","#2A8A38", 4,  7.5,     6,  0.3f, 55, DragonFx.None),
            D(4,  Tier.Uncommon,  "Болотник",          "Swampy",           "#4E7A3A","#9AB06A","#33552A", 3,  10,     8,  0.2f, 35, DragonFx.None),
            D(5,  Tier.Uncommon,  "Коралл",            "Coral",            "#FF7F7F","#FFD0C0","#E05050", 5,  12.5,     6,  0.3f, 4, DragonFx.Sparkle),
            D(6,  Tier.Rare,      "Ледяной Клык",      "Frostfang",        "#6EC8FF","#E6F7FF","#3A8FD0", 7,  40.5,     12, 0.5f, 55, DragonFx.Frost),
            D(7,  Tier.Rare,      "Грозовик",          "Stormy",           "#3050C0","#A0B8FF","#FFE040", 9,  49.5,    10, 0.6f, 35, DragonFx.Sparkle),
            D(8,  Tier.Rare,      "Сапфир",            "Sapphire",         "#2060FF","#80C0FF","#1030A0", 8,  63,    14, 0.5f, 4, DragonFx.Frost),
            D(9,  Tier.Epic,      "Аметистовый",       "Amethyst",         "#A040F0","#E0B0FF","#6A20B0", 12, 245,    18, 0.8f, 55, DragonFx.Sparkle),
            D(10, Tier.Epic,      "Теневой",           "Shadow",           "#2A2438","#6A5A8A","#A040F0", 15, 298,    20, 1.0f, 35, DragonFx.Void),
            D(11, Tier.Epic,      "Токсик",            "Toxic",            "#70FF40","#D0FF80","#308020", 14, 350,   22, 0.9f, 4, DragonFx.Aura),
            D(12, Tier.Legendary, "Золотой Император", "Golden Emperor",   "#FFC820","#FFF0A0","#E08A10", 20, 1500,    30, 1.2f, 50, DragonFx.Halo),
            D(13, Tier.Legendary, "Солнечный Феникс",  "Sun Phoenix",      "#FF8A20","#FFE060","#FF3A10", 24, 1.88e3,  32, 1.5f, 28, DragonFx.Fire),
            D(14, Tier.Legendary, "Молниевый Рык",     "Thunder Roar",     "#FFE640","#FFFFFF","#3060FF", 26, 2.25e3,    35, 1.6f, 4, DragonFx.Sparkle),
            D(15, Tier.Mythic,    "Вулканорог",        "Volcanohorn",      "#D02020","#FF9040","#401010", 30, 1.02e4,    45, 1.8f, 50, DragonFx.Fire),
            D(16, Tier.Mythic,    "Кровавая Луна",     "Blood Moon",       "#8A0A2A","#FF5070","#200008", 35, 1.28e4,  50, 2.0f, 28, DragonFx.Aura),
            D(17, Tier.Mythic,    "Изумрудный Страж",  "Emerald Warden",   "#10C080","#A0FFD0","#086040", 38, 1.53e4,    55, 2.1f, 4, DragonFx.Aura),
            D(18, Tier.Divine,    "Архангел",          "Archangel",        "#FFFFFF","#FFF4C0","#FFD860", 45, 7.8e4,    70, 2.4f, 50, DragonFx.Halo),
            D(19, Tier.Divine,    "Громовержец",       "Thunder God",      "#F0E0A0","#FFFFFF","#60A0FF", 50, 9.75e4,  75, 2.6f, 28, DragonFx.Sparkle),
            D(20, Tier.Divine,    "Солнцебог",         "Sun God",          "#FFB020","#FFF080","#FF6000", 55, 1.17e5,    80, 2.8f, 4, DragonFx.Fire),
            D(21, Tier.Secret,    "Космический",       "Cosmic",           "#1A1060","#50F0FF","#FF40E0", 65, 7.2e5,    100,3.0f, 50, DragonFx.Stars),
            D(22, Tier.Secret,    "Радужный Бог",      "Rainbow God",      "#FFFFFF","#FF60A0","#40FFB0", 70, 9e5,    110,3.5f, 28, DragonFx.Rainbow),
            D(23, Tier.Secret,    "Пустотник",         "Voidling",         "#100818","#8030FF","#000000", 75, 1.08e6,  120,3.5f, 4, DragonFx.Void),
            D(24, Tier.Celestial, "Звёздный Змей",     "Star Serpent",     "#3040C0","#C0D0FF","#FFFFFF", 90, 6e6,  150,4.0f, 50, DragonFx.Stars),
            D(25, Tier.Celestial, "Галактион",         "Galaxion",         "#6020A0","#FF80FF","#20E0FF", 100,8e6,  170,4.2f, 28, DragonFx.Rainbow),
            D(26, Tier.Celestial, "Бесконечность",     "Infinity",         "#000000","#FFFFFF","#FFD700", 120,1e7,    200,4.5f, 4, DragonFx.Halo),
            D(27, Tier.Ancient,   "Окаменелый Титан",  "Fossil Titan",     "#A08060","#E0D0B0","#604020", 140,7e7,    240,4.8f, 50, DragonFx.Sparkle),
            D(28, Tier.Ancient,   "Руный Змей",        "Rune Wyrm",        "#406080","#80FFFF","#203040", 150,8.75e7,  260,5.0f, 28, DragonFx.Aura),
            D(29, Tier.Ancient,   "Первородный",       "Primordial",       "#C06020","#FFD080","#FF2000", 170,1.05e8,    300,5.2f, 4, DragonFx.Fire),
            D(30, Tier.Galactic,  "Туманность",        "Nebula",           "#4020C0","#FF80E0","#20C0FF", 200,6e8,   360,5.5f, 50, DragonFx.Stars),
            D(31, Tier.Galactic,  "Квазар",            "Quasar",           "#FFFFFF","#80C0FF","#FFFF80", 220,8e8,   400,5.8f, 28, DragonFx.Halo),
            D(32, Tier.Galactic,  "Чёрная Дыра",       "Black Hole",       "#050008","#6000FF","#FF6000", 250,1e9,   450,6.0f, 4, DragonFx.Void),
            D(33, Tier.Omega,     "Омега Прайм",       "Omega Prime",      "#FF1060","#FFD0E0","#400010", 300,6e9,   550,6.5f, 50, DragonFx.Fire),
            D(34, Tier.Omega,     "Хронос",            "Chronos",          "#E0C060","#FFFFFF","#2040FF", 340,7.8e9, 620,7.0f, 28, DragonFx.Stars),
            D(35, Tier.Omega,     "Абсолют",           "The Absolute",     "#FFFFFF","#000000","#FF00FF", 400,9.6e9,   700,7.5f, 4, DragonFx.Rainbow),
            // ===== Эксклюзивы Драконьего яйца (только за донат) =====
            X(36, Tier.Mythic,    "Кристальный Страж", "Crystal Guardian", "#60E0FF","#E0FFFF","#20A0FF", 45, 1.85e4, 65, 2.3f, 45, DragonFx.Frost),
            X(37, Tier.Divine,    "Лавовый Титан",     "Lava Titan",       "#FF4010","#FFC040","#300800", 65, 1.4e5, 95, 3.0f, 28, DragonFx.Fire),
            X(38, Tier.Secret,    "Неоновый Кибердракон","Neon Cyberdragon","#101020","#00FFC8","#FF00C8", 88, 1.3e6, 140, 3.6f, 17, DragonFx.Aura),
            X(39, Tier.Celestial, "Солнечный Бог",     "Solar Deity",      "#FFD020","#FFFFFF","#FF8000", 140, 1.2e7, 230, 4.6f, 7, DragonFx.Halo),
            X(40, Tier.Galactic,  "Галактический Кит", "Galaxy Leviathan", "#200050","#80FFFF","#FF60FF", 290, 1.2e9, 520, 6.1f, 2.5f,  DragonFx.Stars),
            X(41, Tier.Omega,     "Дракон Бесконечности","Infinity Dragon","#FFFFFF","#FFD700","#000000", 460, 1.15e10, 800, 7.7f, 0.5f,  DragonFx.Rainbow),
        };

        public static readonly TreadmillDef[] Treadmills =
        {
            new TreadmillDef{ nameRu="Дорожка",          nameEn="Treadmill",        gainPerSec=2,     price=0,    rebirthsRequired=0, color=new Color(0.2f,0.8f,0.3f) },
            new TreadmillDef{ nameRu="Быстрая дорожка",  nameEn="Fast Treadmill",   gainPerSec=8,     price=0,    rebirthsRequired=0, adsRequired=2, color=new Color(0.2f,0.5f,1f) },
            new TreadmillDef{ nameRu="Турбо дорожка",    nameEn="Turbo Treadmill",  gainPerSec=120,   price=3e4, rebirthsRequired=0, color=new Color(0.8f,0.3f,1f) },
            new TreadmillDef{ nameRu="Ракетная дорожка", nameEn="Rocket Treadmill", gainPerSec=6e3,   price=4e6,  rebirthsRequired=0, color=new Color(1f,0.5f,0.1f) },
            new TreadmillDef{ nameRu="Космо дорожка",    nameEn="Cosmic Treadmill", gainPerSec=1.5e5, price=2.5e9,  rebirthsRequired=0, color=new Color(0.1f,1f,0.95f) },
            new TreadmillDef{ nameRu="Гипер дорожка",    nameEn="Hyper Treadmill",  gainPerSec=5e6,   price=1.5e11, rebirthsRequired=0, color=new Color(1f,0.85f,0.2f) },
            new TreadmillDef{ nameRu="Омега дорожка",    nameEn="Omega Treadmill",  gainPerSec=2e8,   price=1e14, rebirthsRequired=0, color=new Color(1f,0.15f,0.6f) },
        };

        public static readonly UpgradeDef[] Upgrades =
        {
            new UpgradeDef{ id="train",  nameRu="Тренер",        nameEn="Coach",        descRu="+10% к прокачке скорости",     descEn="+10% speed training",   baseCost=60,   costMult=1.6,  costAccel=1.045, perLevel=0.10f, maxLevel=30, color=new Color(0.3f,0.8f,1f) },
            new UpgradeDef{ id="income", nameRu="Кормушка",      nameEn="Feeder",       descRu="+8% к доходу драконов",        descEn="+8% dragon income",     baseCost=150,  costMult=1.6,  costAccel=1.045, perLevel=0.08f, maxLevel=30, color=new Color(1f,0.8f,0.2f) },
            new UpgradeDef{ id="grow",   nameRu="Удобрение",     nameEn="Fertilizer",   descRu="-3% ко времени роста яиц",      descEn="-3% egg grow time",     baseCost=800,  costMult=2.2,  costAccel=1.25,  perLevel=0.03f, maxLevel=10, color=new Color(0.4f,0.9f,0.4f) },
            new UpgradeDef{ id="luck",   nameRu="Удача",         nameEn="Luck",         descRu="+1% шанс на дракона тиром выше", descEn="+1% higher tier chance", baseCost=1500, costMult=2.2, costAccel=1.25, perLevel=0.01f, maxLevel=10, color=new Color(0.9f,0.4f,1f) },
            new UpgradeDef{ id="jump",   nameRu="Пружины",       nameEn="Springs",      descRu="+0.3 к прыжку",                descEn="+0.3 jump power",       baseCost=200,  costMult=2,    costAccel=1.3,   perLevel=0.3f,  maxLevel=8,  color=new Color(1f,0.5f,0.4f) },
        };

        public static readonly ProductDef[] Products =
        {
            new ProductDef{ id="coins_small",  kind=ProductKind.Coins,  amount=15,  nameRu="Мешок монет",     nameEn="Coin Bag",       descRu="Доход за 15 минут",        descEn="15 minutes of income",   color=new Color(1f,0.8f,0.2f),  fallbackPrice="19" },
            new ProductDef{ id="coins_medium", kind=ProductKind.Coins,  amount=90,  nameRu="Сундук монет",    nameEn="Coin Chest",     descRu="Доход за 1.5 часа",        descEn="1.5 hours of income",    color=new Color(1f,0.65f,0.1f), fallbackPrice="49" },
            new ProductDef{ id="coins_big",    kind=ProductKind.Coins,  amount=600, nameRu="Гора монет",      nameEn="Coin Mountain",  descRu="Доход за 10 часов",        descEn="10 hours of income",     color=new Color(1f,0.5f,0.1f),  fallbackPrice="149" },
            new ProductDef{ id="speed_pack",   kind=ProductKind.Speed,  amount=30,  nameRu="Энергетик",       nameEn="Energy Drink",   descRu="Скорость за 30 минут бега", descEn="30 minutes of training", color=new Color(0.3f,0.9f,1f),  fallbackPrice="29" },
            new ProductDef{ id="legend_egg",   kind=ProductKind.Egg,    amount=1,   nameRu="Золотое яйцо",    nameEn="Golden Egg",     descRu="Яйцо высшего открытого тира +1", descEn="Egg of your best tier +1", color=new Color(1f,0.85f,0.3f), fallbackPrice="79" },
            new ProductDef{ id="dragon_egg_1",  kind=ProductKind.Egg, amount=1,  nameRu="1 Драконье яйцо",   nameEn="1 Dragon Egg",   descRu="Эксклюзивный дракон", descEn="Exclusive dragon", color=new Color(1f,0.75f,0.1f), fallbackPrice="149" },
            new ProductDef{ id="dragon_egg_3",  kind=ProductKind.Egg, amount=3,  nameRu="3 Драконьих яйца",  nameEn="3 Dragon Eggs",  descRu="Эксклюзивные драконы", descEn="Exclusive dragons", color=new Color(1f,0.75f,0.1f), fallbackPrice="399" },
            new ProductDef{ id="dragon_egg_10", kind=ProductKind.Egg, amount=10, nameRu="10 Драконьих яиц",  nameEn="10 Dragon Eggs", descRu="Эксклюзивные драконы", descEn="Exclusive dragons", color=new Color(1f,0.75f,0.1f), fallbackPrice="699" },
            new ProductDef{ id="x2_grow",      kind=ProductKind.Permanent, amount=2, nameRu="x2 Скорость роста яиц", nameEn="x2 Egg Growth", descRu="Яйца растут вдвое быстрее", descEn="Eggs grow twice as fast", color=new Color(0.4f,0.9f,1f), fallbackPrice="199" },
            new ProductDef{ id="x2_income",    kind=ProductKind.Permanent, amount=2, nameRu="x2 Доход навсегда",  nameEn="x2 Income forever",   descRu="Все драконы приносят вдвое больше", descEn="All dragons earn double", color=new Color(0.3f,0.85f,0.4f), fallbackPrice="299" },
            new ProductDef{ id="noads_2h",     kind=ProductKind.NoAdsTimed, amount=2, nameRu="Без рекламы 2 часа", nameEn="No ads for 2 hours", descRu="Никакой рекламы между играми 2 часа", descEn="No ads between rounds for 2 hours", color=new Color(0.55f,0.4f,0.95f), fallbackPrice="50" },
            new ProductDef{ id="noads_forever", kind=ProductKind.Permanent, amount=1, nameRu="Без рекламы навсегда", nameEn="No ads forever", descRu="Реклама отключается навсегда", descEn="Ads are removed forever", color=new Color(0.95f,0.3f,0.45f), fallbackPrice="500" },
            new ProductDef{ id="x2_train",     kind=ProductKind.Permanent, amount=2, nameRu="x2 Прокачка навсегда", nameEn="x2 Training forever", descRu="Дорожки качают вдвое быстрее",   descEn="Treadmills train twice as fast", color=new Color(0.35f,0.6f,1f), fallbackPrice="99" },
        };

        public static readonly TrailDef[] Trails =
        {
            new TrailDef{ nameRu="Серый трейл",       nameEn="Gray Trail",     rarityRu="Обычный",      rarityEn="Common",    a=new Color(0.85f,0.85f,0.9f), b=new Color(0.6f,0.6f,0.65f), mult=1.5f, coinPrice=150,   productId="trail_gray",    fallbackPrice="19" },
            new TrailDef{ nameRu="Зелёный трейл",     nameEn="Green Trail",    rarityRu="Необычный",    rarityEn="Uncommon",  a=new Color(0.5f,1f,0.3f),     b=new Color(0.2f,0.7f,0.15f), mult=2f,   coinPrice=1e4,   productId="trail_green",   fallbackPrice="29" },
            new TrailDef{ nameRu="Синий трейл",       nameEn="Blue Trail",     rarityRu="Редкий",       rarityEn="Rare",      a=new Color(0.4f,0.75f,1f),    b=new Color(0.1f,0.35f,1f),   mult=3f,   coinPrice=2e5, productId="trail_blue",    fallbackPrice="49" },
            new TrailDef{ nameRu="Фиолетовый трейл",  nameEn="Purple Trail",   rarityRu="Эпический",    rarityEn="Epic",      a=new Color(0.85f,0.45f,1f),   b=new Color(0.5f,0.1f,0.9f),  mult=5f,   coinPrice=2e6,   productId="trail_purple",  fallbackPrice="79", sparkles=true },
            new TrailDef{ nameRu="Золотой трейл",     nameEn="Golden Trail",   rarityRu="Легендарный",  rarityEn="Legendary", a=new Color(1f,0.95f,0.5f),    b=new Color(1f,0.65f,0.1f),   mult=8f,   coinPrice=8e7,   productId="trail_gold",    fallbackPrice="99", sparkles=true },
            new TrailDef{ nameRu="Огненный трейл",    nameEn="Fire Trail",     rarityRu="Мифический",   rarityEn="Mythic",    a=new Color(1f,0.85f,0.2f),    b=new Color(1f,0.15f,0.05f),  mult=12f,  coinPrice=5e9,   productId="trail_fire",    fallbackPrice="149", sparkles=true },
            new TrailDef{ nameRu="Радужный трейл",    nameEn="Rainbow Trail",  rarityRu="Секретный",    rarityEn="Secret",    a=Color.white, b=Color.white, rainbow=true,               mult=20f,  coinPrice=4e11,  productId="trail_rainbow", fallbackPrice="199", sparkles=true },
            new TrailDef{ nameRu="Космический трейл", nameEn="Cosmic Trail",   rarityRu="Небесный",     rarityEn="Celestial", a=new Color(0.3f,0.9f,1f),     b=new Color(0.6f,0.1f,1f),    mult=35f,  coinPrice=5e13,  productId="trail_cosmic",  fallbackPrice="299", sparkles=true },
        };

        // ===== Прокачка драконов за монеты =====
        public const int DragonMaxLevel = 15;
        public static float DragonLevelIncome(int lvl) { return 1f + 0.35f * (lvl - 1); }
        public static float DragonLevelStat(int lvl) { return 1f + 0.15f * (lvl - 1); }
        public static double DragonUpgradeCost(DragonDef d, int lvl) { int k = lvl - 1; return d.coinsPerSec * 25 * System.Math.Pow(1.6, k) * System.Math.Pow(1.05, k * (k - 1) / 2.0); }

        // ===== Ежедневная награда (7 дней по кругу) =====
        public const int DailyDays = 7;

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
            foreach (var d in Dragons) if (d.tier == tier && !d.exclusive) { list.Add(d); total += d.weight; }
            float r = Random.value * total;
            foreach (var d in list) { r -= d.weight; if (r <= 0) return d; }
            return list[list.Count - 1];
        }

        /// <summary>
        /// Шансы тира дракона из яйца тира T (как в кейсах): T-1: 25%, T: 67%, T+1: 6.5%, T+2: 1.5%.
        /// Каждый уровень удачи переносит Upgrades[3].perLevel (1%) с T-1/T на T+1.
        /// </summary>
        public static float[] TierChances(Tier egg, int luckLevel)
        {
            int t = (int)egg, n = Tiers.Length;
            var c = new float[n];
            float lower = t > 0 ? 0.25f : 0f, same = t > 0 ? 0.67f : 0.92f, up1 = 0.065f, up2 = 0.015f;
            float shift = Mathf.Clamp(luckLevel, 0, Upgrades[3].maxLevel) * Upgrades[3].perLevel;
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

        public static double RebirthCost(int rebirths) { return 2e5 * System.Math.Pow(4.5, rebirths); }
        /// <summary>Для перерождения нужно в этой жизни дойти до зоны этого тира (скорость сбрасывается).</summary>
        public static int RebirthReqTier(int rebirths) { return Mathf.Min(3 + rebirths, 11); }
        public static double PlotCost(int plotsOwned) { return 400 * System.Math.Pow(12, plotsOwned - StartPlots); }
        public static float RebirthMultiplier(int rebirths) { return 1f + rebirths; }
        public static double SellPrice(DragonDef d, int lvl = 1) { return d.coinsPerSec * 90 * DragonLevelIncome(lvl); }
        /// <summary>Цена уровня: base · mult^lvl · accel^(lvl·(lvl−1)/2) — первые уровни дешёвые, дальше всё круче и круче.</summary>
        public static double UpgradeCost(int i, int level) { var u = Upgrades[i]; return u.baseCost * System.Math.Pow(u.costMult, level) * System.Math.Pow(u.costAccel, level * (level - 1) / 2.0); }

        static TierInfo T(Tier t, string ru, string en, float r, float g, float b, int grow, float gs, int gc, float aggro, BrainrotKind k, float scale, double req)
        {
            return new TierInfo { tier = t, nameRu = ru, nameEn = en, color = new Color(r, g, b), growSeconds = grow, guardSpeed = gs, guardCount = gc,
                aggroRadius = aggro, guardKind = k, guardScale = scale, reqPoints = req };
        }

        static DragonDef X(int id, Tier t, string ru, string en, string body, string belly, string wing,
            float speedPct, double cps, float trainPct, float jump, float chance, DragonFx fx)
        {
            var d = D(id, t, ru, en, body, belly, wing, speedPct, cps, trainPct, jump, 0, fx);
            d.exclusive = true;
            d.premiumChance = chance;
            return d;
        }

        /// <summary>Эксклюзивный дракон из донатного яйца по шансам 39/25/20/10/5/1%.</summary>
        public static DragonDef RollPremium()
        {
            float total = 0;
            foreach (var d in Dragons) if (d.exclusive) total += d.premiumChance;
            float r = Random.value * total;
            DragonDef last = null;
            foreach (var d in Dragons) if (d.exclusive) { last = d; r -= d.premiumChance; if (r <= 0) return d; }
            return last;
        }

        public const int PremiumEggMarker = -2;   // dragonId яйца = донатное яйцо
        public const int PremiumGrowSeconds = 60;

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
