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

        static readonly string[] SuffixRu = { "", "К", "М", "Б", "Т", "Кв", "Кт", "Сп" };
        static readonly string[] SuffixEn = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx" };

        /// <summary>1 234 -> 1.2К, как в роблокс-симуляторах.</summary>
        public static string Num(double v)
        {
            if (v < 1000) return ((long)v).ToString();
            int i = 0;
            while (v >= 1000 && i < SuffixEn.Length - 1) { v /= 1000; i++; }
            string s = v >= 100 ? v.ToString("0") : v.ToString("0.#");
            return s + (Ru ? SuffixRu[i] : SuffixEn[i]);
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
