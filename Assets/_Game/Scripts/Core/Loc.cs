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
            { "shop",         new[]{ "Навыки", "Skills" } },
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
            // обучение: короткие шаги (панель сверху — до ~90 символов, чтобы влезало на телефоне)
            { "tut_train",     new[]{ "Встань на беговую дорожку (стрелки) и беги вперёд. Скорость: {0} / {1}", "Step onto the treadmill (follow the arrows) and run forward. Speed: {0} / {1}" } },
            { "tut_go_egg",    new[]{ "Отлично! Выйди в ворота «К ЯЙЦАМ» и беги по стрелкам к первому яйцу", "Great! Go through the TO THE EGGS gate and follow the arrows to the first egg" } },
            { "tut_steal",     new[]{ "{0}, чтобы украсть яйцо. Брейнроты проснутся и погонятся за тобой!", "{0} to steal the egg. The brainrots will wake up and chase you!" } },
            { "tut_steal_pc",  new[]{ "Удерживай <color=#FFD84A>E</color>", "Hold <color=#FFD84A>E</color>" } },
            { "tut_steal_mob", new[]{ "Нажми <color=#FFD84A>кнопку действия</color>", "Tap the <color=#FFD84A>action button</color>" } },
            { "tut_return",    new[]{ "Беги на базу в зелёную зону «СДАЙ ЯЙЦО СЮДА»! Не дай себя поймать", "Run back to the green BRING EGGS HERE zone! Don't get caught" } },
            { "tut_caught",    new[]{ "Тебя поймали — яйцо вернулось. Беги к яйцу и попробуй снова!", "You got caught and the egg went back. Try again!" } },
            { "tut_grow",      new[]{ "Яйцо растёт на грядке: {0}. Подойди к ней — реклама ускорит рост на 90%", "The egg is growing: {0}. Walk up to the plot: an ad speeds it up by 90%" } },
            { "tut_open",      new[]{ "Яйцо готово! Подойди к грядке и {0}, чтобы открыть его", "The egg is ready! Walk to the plot and {0} to open it" } },
            { "tut_plant",     new[]{ "Яйцо в инвентаре: нажми «Яйца» справа, выбери его и «Посадить»", "Your egg is in the inventory: tap Eggs on the right, pick it and tap Plant" } },
            { "tut_take",      new[]{ "На грядке дракон даёт монеты, в слоте — бонусы к скорости. {0} у грядки, чтобы взять его", "On a plot a dragon earns coins, in a slot it boosts you. {0} at the plot to take it" } },
            { "tut_skills",    new[]{ "За монеты прокачивай навыки: тренер, доход, удача. Нажми «Навыки» слева вверху", "Improve skills with coins: coach, income, luck. Tap Skills at the top left" } },
            { "tut_done",      new[]{ "Готово! Качай скорость, открывай зоны с редкими яйцами и собери всех драконов!", "All set! Train speed, unlock zones with rarer eggs and collect every dragon!" } },
            { "tut_act_pc",    new[]{ "нажми <color=#FFD84A>E</color>", "press <color=#FFD84A>E</color>" } },
            { "tut_act_mob",   new[]{ "нажми <color=#FFD84A>кнопку действия</color>", "tap the <color=#FFD84A>action button</color>" } },
            { "skip",         new[]{ "Пропустить", "Skip" } },
            { "gate_locked",  new[]{ "Нужна скорость {0}", "Speed {0} required" } },
            { "gate_block",   new[]{ "Нужна скорость {0}! Качайся на беговых дорожках", "You need speed {0}! Train on treadmills" } },
            { "gate_open",    new[]{ "ОТКРЫТО", "OPEN" } },
            { "watch_ad_unlock", new[]{ "Реклама, чтобы открыть ({0}/{1})", "Watch ad to unlock ({0}/{1})" } },
            { "ad_progress",  new[]{ "Реклама просмотрена: {0}/{1}", "Ads watched: {0}/{1}" } },
            { "take_dragon",  new[]{ "Забрать дракона", "Pick up dragon" } },
            { "place_dragon", new[]{ "Поставить дракона", "Place dragon" } },
            { "inv_full",     new[]{ "Слоты драконов заполнены (5/5)! Продай лишних продавцу", "Dragon slots are full (5/5)! Sell extras to the merchant" } },
            { "dragon_taken", new[]{ "Дракон надет! Он даёт бонусы и летает рядом", "Dragon equipped! It gives bonuses and flies next to you" } },
            { "seller",       new[]{ "Продавец драконов", "Dragon Merchant" } },
            { "talk_seller",  new[]{ "Продать драконов", "Sell dragons" } },
            { "sell_title",   new[]{ "Продать драконов", "Sell dragons" } },
            { "sell_all",     new[]{ "Продать всех ({0})", "Sell all ({0})" } },
            { "sell_empty",   new[]{ "В сумке нет драконов.\nСними дракона в окне «Драконы» — он попадёт в сумку, и его можно продать.", "Your bag is empty.\nUnequip a dragon in the Dragons menu — it goes to the bag and can be sold." } },
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
            { "held",         new[]{ "Выбран {0}: подойди к пустой грядке, чтобы поставить", "{0} selected: walk to an empty plot to place it" } },
            { "on",           new[]{ "ВКЛ", "ON" } },
            { "off",          new[]{ "ВЫКЛ", "OFF" } },
            { "zone_title",   new[]{ "{0}", "{0}" } },
        };

        /// <summary>
        /// Язык из Yandex SDK (environment.i18n.lang, например "ru", "en", "tr"; на всякий случай понимает и "ru-RU").
        /// ru, be, kk, uk, uz — русский интерфейс, всё остальное — английский.
        /// </summary>
        public static void SetLanguage(string lang)
        {
            string l = string.IsNullOrEmpty(lang) ? "en" : lang.Trim().ToLowerInvariant();
            int cut = l.IndexOfAny(new[] { '-', '_' });
            if (cut > 0) l = l.Substring(0, cut);
            Ru = l == "ru" || l == "be" || l == "kk" || l == "uk" || l == "uz";
        }

        /// <summary>Первая буква заглавная (для фраз, которые стоят и в начале, и в середине предложения).</summary>
        public static string Cap(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = 0;
            if (s[0] == '<') { int close = s.IndexOf('>'); if (close > 0 && close + 1 < s.Length) i = close + 1; }
            return s.Substring(0, i) + char.ToUpper(s[i]) + s.Substring(i + 1);
        }

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

        /// <summary>1 234 -> 1.2K, как в роблокс-симуляторах (латинские суффиксы — одинаковые для RU и EN).</summary>
        public static string Num(double v)
        {
            if (v < 1000) return ((long)v).ToString();
            int i = 0;
            while (v >= 1000 && i < Suffix.Length - 1) { v /= 1000; i++; }
            string s = v >= 100 ? v.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                                : v.ToString(v >= 10 ? "0.#" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
            return s + Suffix[i];
        }

        /// <summary>Таймер в формате 1:05:09 / 4:07 — одинаковый для всех языков.</summary>
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
