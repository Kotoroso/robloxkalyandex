using System.Collections.Generic;

namespace DragonHeist
{
    /// <summary>Простая локализация RU/EN. Язык берётся из Yandex SDK (environment.i18n.lang).</summary>
    public static class Loc
    {
        public static bool Ru = true;

        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            { "coins",        new[]{ "Монеты", "Coins" } },
            { "speed",        new[]{ "Скорость", "Speed" } },
            { "rebirths",     new[]{ "Перерождения", "Rebirths" } },
            { "steal",        new[]{ "Украсть яйцо", "Steal egg" } },
            { "carrying",     new[]{ "Несёшь яйцо: {0}! Беги на базу!", "Carrying {0} egg! Run to your base!" } },
            { "caught",       new[]{ "Тебя поймал {0}!", "{0} caught you!" } },
            { "stolen",       new[]{ "Яйцо украдено! Посажено на грядку.", "Egg stolen! Planted in your garden." } },
            { "stolen_inv",   new[]{ "Яйцо украдено! Нет свободных грядок — оно в инвентаре.", "Egg stolen! No free plots — kept in inventory." } },
            { "hatched",      new[]{ "Вылупился дракон: {0}!", "Dragon hatched: {0}!" } },
            { "speedup_ad",   new[]{ "Ускорить -90% (реклама)", "Speed up -90% (ad)" } },
            { "sell",         new[]{ "Продать за {0}", "Sell for {0}" } },
            { "buy_for",      new[]{ "Купить за {0}", "Buy for {0}" } },
            { "need_rebirth", new[]{ "Нужно перерождений: {0}", "Rebirths required: {0}" } },
            { "no_money",     new[]{ "Не хватает монет!", "Not enough coins!" } },
            { "rebirth",      new[]{ "Перерождение", "Rebirth" } },
            { "rebirth_desc", new[]{ "Сбрасывает монеты и скорость.\nНавсегда даёт:\nx{0} к доходу и прокачке\n+1 грядку\nНовые дорожки", "Resets coins and speed.\nPermanently gives:\nx{0} income & training\n+1 plot\nNew treadmills" } },
            { "rebirth_cost", new[]{ "Цена: {0} монет", "Cost: {0} coins" } },
            { "rebirth_done", new[]{ "Перерождение! Множитель x{0}", "Rebirth! Multiplier x{0}" } },
            { "dragons",      new[]{ "Драконы", "Dragons" } },
            { "shop",         new[]{ "Магазин", "Shop" } },
            { "close",        new[]{ "Закрыть", "Close" } },
            { "buy_plot",     new[]{ "Купить грядку ({0})", "Buy plot ({0})" } },
            { "plots_max",    new[]{ "Все грядки куплены", "All plots owned" } },
            { "plot_bought",  new[]{ "Новая грядка!", "New plot!" } },
            { "total_bonus",  new[]{ "Бонусы драконов:\nСкорость +{0}%\nДоход {1}/сек\nПрокачка +{2}%\nПрыжок +{3}", "Dragon bonuses:\nSpeed +{0}%\nIncome {1}/s\nTraining +{2}%\nJump +{3}" } },
            { "no_dragons",   new[]{ "Пока нет драконов.\nУкради яйцо у брейнротов!", "No dragons yet.\nSteal an egg from the brainrots!" } },
            { "dragon_line",  new[]{ "{0} [{1}]  +{2}% скор., {3}/сек, +{4}% прок.", "{0} [{1}]  +{2}% spd, {3}/s, +{4}% train" } },
            { "egg",          new[]{ "Яйцо", "Egg" } },
            { "ready_in",     new[]{ "{0}", "{0}" } },
            { "base",         new[]{ "ТВОЯ БАЗА", "YOUR BASE" } },
            { "zone",         new[]{ "ЗОНА: {0}", "ZONE: {0}" } },
            { "min_speed",    new[]{ "Охрана: скорость {0}", "Guards speed: {0}" } },
            { "offline",      new[]{ "Пока тебя не было драконы заработали {0} монет!", "Your dragons earned {0} coins while you were away!" } },
            { "sound",        new[]{ "Звук", "Sound" } },
            { "ad_fail",      new[]{ "Реклама недоступна, попробуй позже", "Ad unavailable, try later" } },
            { "sped_up",      new[]{ "Время роста сокращено на 90%!", "Grow time cut by 90%!" } },
            { "treadmill_hint", new[]{ "Беги вперёд, чтобы качать скорость!", "Run forward to train speed!" } },
            { "inventory",    new[]{ "Яиц в инвентаре: {0}", "Eggs in inventory: {0}" } },
            { "hint_start",   new[]{ "Качай скорость на дорожках и кради яйца драконов у брейнротов!", "Train speed on treadmills and steal dragon eggs from brainrots!" } },
            { "sold",         new[]{ "Дракон продан за {0}", "Dragon sold for {0}" } },
            { "unlocked",     new[]{ "Куплено!", "Unlocked!" } },
            { "growing",      new[]{ "Растёт", "Growing" } },
            { "jump",         new[]{ "Прыжок", "Jump" } },
            { "action",       new[]{ "Действие", "Action" } },
            { "tut_title",    new[]{ "ОБУЧЕНИЕ", "TUTORIAL" } },
            { "tut_1",        new[]{ "Иди по стрелкам к беговой дорожке и беги вперёд, пока скорость не станет {0}", "Follow the arrows to the treadmill and run forward until your speed is {0}" } },
            { "tut_2",        new[]{ "Отлично! Теперь иди по стрелкам к яйцу и {0}, чтобы украсть его", "Great! Now follow the arrows to the egg and {0} to steal it" } },
            { "tut_2_pc",     new[]{ "зажми E", "hold E" } },
            { "tut_2_mob",    new[]{ "нажми кнопку действия", "press the action button" } },
            { "tut_3",        new[]{ "Беги с яйцом на базу! Не дай брейнроту поймать тебя", "Run the egg back to your base! Don't let the brainrot catch you" } },
            { "tut_4",        new[]{ "Яйцо растёт на грядке. Подойди к нему: можно ускорить рост за рекламу", "The egg is growing. Walk up to it: you can speed it up with an ad" } },
            { "tut_5",        new[]{ "Дракон вылупился! Он приносит монеты и бонусы. Качай скорость, чтобы открывать новые зоны!", "Your dragon hatched! It earns coins and bonuses. Train speed to unlock new zones!" } },
            { "skip",         new[]{ "Пропустить", "Skip" } },
            { "gate_locked",  new[]{ "Нужна скорость {0}", "Speed {0} required" } },
            { "gate_block",   new[]{ "Нужна скорость {0}! Качайся на беговых дорожках", "You need speed {0}! Train on treadmills" } },
            { "gate_open",    new[]{ "ОТКРЫТО", "OPEN" } },
            { "watch_ad_unlock", new[]{ "Реклама, чтобы открыть ({0}/{1})", "Watch ad to unlock ({0}/{1})" } },
            { "ad_progress",  new[]{ "Реклама просмотрена: {0}/{1}", "Ads watched: {0}/{1}" } },
            { "take_dragon",  new[]{ "Забрать дракона", "Pick up dragon" } },
            { "place_dragon", new[]{ "Поставить дракона", "Place dragon" } },
            { "inv_full",     new[]{ "Слоты драконов заполнены (5/5)! Продай лишних продавцу", "Dragon slots are full (5/5)! Sell extras to the merchant" } },
            { "dragon_taken", new[]{ "Дракон в слотах! Нажми на слот (или 1-5), чтобы взять в руку", "Dragon added to slots! Tap a slot (or 1-5) to hold it" } },
            { "seller",       new[]{ "Продавец драконов", "Dragon Merchant" } },
            { "talk_seller",  new[]{ "Продать драконов", "Sell dragons" } },
            { "sell_title",   new[]{ "Продать драконов", "Sell dragons" } },
            { "sell_all",     new[]{ "Продать всех ({0})", "Sell all ({0})" } },
            { "sell_empty",   new[]{ "В слотах нет драконов.\nЗабери дракона с грядки, чтобы продать.", "No dragons in your slots.\nPick one up from a plot to sell it." } },
            { "empty_slot",   new[]{ "Пусто", "Empty" } },
            { "settings",     new[]{ "Настройки", "Settings" } },
            { "music",        new[]{ "Музыка", "Music" } },
            { "switch_sound", new[]{ "Звук клавиш на ASMR-полу", "ASMR floor key sound" } },
            { "sw_0",         new[]{ "Синий (клик)", "Blue (clicky)" } },
            { "sw_1",         new[]{ "Коричневый (тактильный)", "Brown (tactile)" } },
            { "sw_2",         new[]{ "Красный (мягкий)", "Red (linear)" } },
            { "play",         new[]{ "ИГРАТЬ", "PLAY" } },
            { "title",        new[]{ "УКРАДИ ДРАКОНА", "STEAL A DRAGON" } },
            { "subtitle",     new[]{ "Кради яйца у брейнротов, выращивай драконов!", "Steal eggs from brainrots, raise dragons!" } },
            { "asmr",         new[]{ "ASMR КЛАВИАТУРА", "ASMR KEYBOARD" } },
            { "held",         new[]{ "В руке: {0}  (+{1}% скорости)", "Holding: {0}  (+{1}% speed)" } },
            { "on",           new[]{ "ВКЛ", "ON" } },
            { "off",          new[]{ "ВЫКЛ", "OFF" } },
            { "zone_title",   new[]{ "{0}", "{0}" } },
        };

        public static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            return Ru ? v[0] : v[1];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static string TierName(Tier t)
        {
            var info = GameConfig.GetTier(t);
            return Ru ? info.nameRu : info.nameEn;
        }

        public static string DragonName(DragonDef d) { return Ru ? d.nameRu : d.nameEn; }

        // большие числа как в роблокс-симуляторах: K, M, B, T, Q, QK, QM, QB, QT, QQ, S, SK, ...
        static readonly string[] Suffix = { "", "K", "M", "B", "T", "Q", "QK", "QM", "QB", "QT", "QQ", "S", "SK", "SM", "SB", "ST", "SQ", "SS", "O", "OK", "OM", "OB", "OT", "OQ" };

        /// <summary>1 234 -> 1.2К, как в роблокс-симуляторах.</summary>
        public static string Num(double v)
        {
            if (v < 1000) return ((long)v).ToString();
            int i = 0;
            while (v >= 1000 && i < Suffix.Length - 1) { v /= 1000; i++; }
            string s = v >= 100 ? v.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                                : v.ToString(v >= 10 ? "0.#" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
            return s + Suffix[i];
        }

        public static string Time(double seconds)
        {
            if (seconds < 0) seconds = 0;
            int s = (int)System.Math.Ceiling(seconds);
            int h = s / 3600, m = (s % 3600) / 60, sec = s % 60;
            if (h > 0) return string.Format("{0}:{1:00}:{2:00}", h, m, sec);
            return string.Format("{0}:{1:00}", m, sec);
        }
    }
}
