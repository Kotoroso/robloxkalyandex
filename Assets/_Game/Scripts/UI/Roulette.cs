using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonHeist
{
    /// <summary>
    /// Открытие яйца как кейса: лента карточек драконов прокручивается с замедлением,
    /// тикает на каждой карточке и останавливается на выпавшем драконе. Тап — пропустить.
    /// </summary>
    public class Roulette : MonoBehaviour
    {
        public static bool Active;

        RectTransform strip, rt;
        List<Image> cards = new List<Image>();
        DragonDef result;
        System.Action onDone;
        float t, duration = 4.6f;
        float startX, endX;
        int lastTick = -1;
        const float CardW = 150f, Gap = 12f;
        const int Count = 46, WinIndex = 40;
        bool finished;
        float finishTimer;
        Text resultText;
        Image winCard;

        public void Init(RectTransform root, Tier eggTier, DragonDef res, bool premium, System.Action done)
        {
            rt = root;
            result = res;
            onDone = done;
            Active = true;
            UIKit.Stretch(rt);
            var dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0.05f, 0.7f);
            var btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Skip);

            var title = UIKit.Label(UIKit.Rect(rt, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(900, 70)),
                premium ? (Loc.Ru ? "ДРАКОНЬЕ ЯЙЦО!" : "DRAGON EGG!") : (Loc.Ru ? "ОТКРЫВАЕМ ЯЙЦО: " : "OPENING EGG: ") + Loc.TierName(eggTier).ToUpper(), 44,
                premium ? new Color(1f, 0.8f, 0.2f) : GameConfig.GetTier(eggTier).color);
            title.gameObject.AddComponent<TitleWobble>();

            // окно ленты
            var frame = UIKit.Panel(rt, "Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(980, 210), new Color(0.1f, 0.09f, 0.16f, 1f), 5f);
            var mask = UIKit.Rect(frame.transform, "Mask", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 190));
            mask.gameObject.AddComponent<RectMask2D>();
            strip = UIKit.Rect(mask, "Strip", new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(Count * (CardW + Gap), 180));

            // карточки: случайные драконы по шансам яйца, выигрышная — на WinIndex
            var chances = GameConfig.TierChances(eggTier, GameManager.Instance != null ? GameManager.Instance.LuckLevel : 0);
            for (int i = 0; i < Count; i++)
            {
                DragonDef d;
                if (i == WinIndex) d = result;
                else if (premium) d = GameConfig.RollPremium();
                else if (i == WinIndex + 1 || i == WinIndex - 1) d = GameConfig.RollInTier((Tier)Mathf.Min((int)eggTier + 2, GameConfig.Tiers.Length - 1)); // "почти выпало!"
                else d = GameConfig.RollInTier(Pick(chances, eggTier));
                cards.Add(MakeCard(strip, i, d));
            }
            winCard = cards[WinIndex];

            // указатель по центру
            var ptr = UIKit.Panel(frame.transform, "Pointer", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 220), new Color(1f, 0.85f, 0.2f), 2f);
            ptr.raycastTarget = false;
            var tri = UIKit.Icon(frame.transform, Icons.Star, new Vector2(0.5f, 1), new Vector2(0, 14), 44);
            tri.color = new Color(1f, 0.85f, 0.2f);

            resultText = UIKit.Label(UIKit.Rect(rt, "Result", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(1000, 90)), "", 40, Color.white);
            UIKit.Label(UIKit.Rect(rt, "SkipHint", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(600, 30)),
                Loc.Ru ? "Нажми, чтобы пропустить" : "Tap to skip", 18, new Color(0.8f, 0.8f, 0.9f));

            float center = 0f;
            float jitter = Random.Range(-CardW * 0.35f, CardW * 0.35f);
            startX = center - (CardW * 0.5f);
            endX = center - (WinIndex * (CardW + Gap) + CardW * 0.5f) + jitter;
            strip.anchoredPosition = new Vector2(startX, 0);
            transform.localScale = Vector3.one;
        }

        static Tier Pick(float[] chances, Tier fallback)
        {
            float r = Random.value;
            for (int i = 0; i < chances.Length; i++) { r -= chances[i]; if (r <= 0) return (Tier)i; }
            return fallback;
        }

        Image MakeCard(RectTransform parent, int i, DragonDef d)
        {
            var tc = GameConfig.GetTier(d.tier).color;
            var card = UIKit.Panel(parent, "Card" + i, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * (CardW + Gap), 0), new Vector2(CardW, 170),
                new Color(tc.r * 0.45f, tc.g * 0.45f, tc.b * 0.45f, 1f), 3f);
            card.raycastTarget = false;
            var shine = UIKit.Panel(card.transform, "Shine", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(CardW - 10, 70), new Color(tc.r, tc.g, tc.b, 0.45f));
            shine.raycastTarget = false;
            var ic = UIKit.Icon(card.transform, Icons.Dragon, new Vector2(0.5f, 1), new Vector2(0, -52), 80);
            ic.color = Color.Lerp(d.body, Color.white, 0.25f);
            UIKit.Label(UIKit.Rect(card.transform, "N", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(CardW - 8, 44)), Loc.DragonName(d), 16, Color.white);
            UIKit.Label(UIKit.Rect(card.transform, "T", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(CardW - 8, 24)), Loc.TierName(d.tier), 14, Color.Lerp(tc, Color.white, 0.3f));
            return card;
        }

        void Skip()
        {
            if (!finished) t = duration;
            else if (finishTimer > 0.4f) Close();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!finished)
            {
                t = Mathf.Min(duration, t + dt);
                float k = t / duration;
                float e = 1f - Mathf.Pow(1f - k, 4f); // сильное замедление в конце
                float x = Mathf.Lerp(startX, endX, e);
                strip.anchoredPosition = new Vector2(x, 0);
                int idx = Mathf.FloorToInt((-x) / (CardW + Gap));
                if (idx != lastTick) { lastTick = idx; GameAudio.Play(Sfx.Tick, 0.6f, 0.9f + k * 0.4f); }
                if (t >= duration) Finish();
            }
            else
            {
                finishTimer += dt;
                float p = 1f + Mathf.Sin(finishTimer * 8f) * 0.06f;
                winCard.transform.localScale = Vector3.one * (1.15f * p);
                if (finishTimer > 3.2f) Close();
            }
        }

        void Finish()
        {
            finished = true;
            strip.anchoredPosition = new Vector2(endX, 0);
            var tc = GameConfig.GetTier(result.tier).color;
            resultText.text = (Loc.Ru ? "ВЫПАЛ: " : "YOU GOT: ") + "<color=#" + ColorUtility.ToHtmlStringRGB(tc) + ">" + Loc.DragonName(result).ToUpper() + "</color>\n<size=26>" + Loc.TierName(result.tier) + "</size>";
            foreach (var o in winCard.GetComponents<Outline>()) o.effectColor = new Color(1f, 0.9f, 0.3f);
            GameAudio.Play(result.tier >= Tier.Legendary ? Sfx.Rebirth : Sfx.Success);
            var p = PlayerController.Instance;
            if (p != null) Fx.Confetti(p.transform.position + Vector3.up * 3f, result.tier >= Tier.Legendary ? 120 : 50);
        }

        void Close()
        {
            Active = false;
            var cb = onDone;
            onDone = null;
            Destroy(gameObject);
            if (cb != null) cb();
        }

        void OnDestroy()
        {
            if (onDone != null) { Active = false; var cb = onDone; onDone = null; cb(); }
        }
    }
}
