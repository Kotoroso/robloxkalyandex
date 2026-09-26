using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Беговая дорожка: бежишь вперёд по ленте — качается скорость. Неоновые поручни, экран с приростом,
    /// бегущие стрелки на ленте, линии скорости вокруг игрока. Закрытые открываются за монеты,
    /// перерождения или просмотр рекламы (встать на дорожку и нажать E).
    /// </summary>
    public class Treadmill : MonoBehaviour, IInteractable
    {
        public static readonly List<Treadmill> All = new List<Treadmill>();

        public int index;
        public TreadmillDef def;
        public Vector2 halfSize = new Vector2(2.3f, 4.8f);
        Material beltMat;
        Label3D label, screen;
        GameObject lockRope;
        float gainPopupTimer, activeTimer;
        double gainAccum;
        Transform[] neon;
        float beltSpeed;

        public bool Owned { get { return (SaveManager.Data.treadmillsMask & (1 << index)) != 0; } }
        public bool Training { get { return activeTimer > 0; } }
        float botTimer;
        public Transform BotSpot;   // сюда встаёт фейк-игрок
        public bool BotOccupied;
        /// <summary>Фейк-игрок бежит по дорожке — только визуал ленты.</summary>
        public void BotRun() { botTimer = 0.2f; }
        int AdsWatched { get { return SaveManager.Data.treadmillAds[index]; } }

        public static Treadmill Build(Transform parent, int index, Vector3 pos, float yaw)
        {
            var def = GameConfig.Treadmills[index];
            var go = new GameObject("Treadmill_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = go.AddComponent<Treadmill>();
            t.index = index;
            t.def = def;
            var tr = go.transform;

            var frame = Mats.Plastic(new Color(0.18f, 0.18f, 0.22f));
            var trim = Mats.Plastic(Color.Lerp(def.color, Color.white, 0.15f));
            var glow = Mats.Glow(Color.Lerp(def.color, Color.white, 0.25f));

            Blocky.Round = true; Blocky.RoundFactor = 0.35f;
            // корпус (коллайдер отдельным плоским кубом ниже)
            Blocky.Part(tr, new Vector3(0, 0.3f, 0), new Vector3(5.4f, 0.6f, 10.4f), frame);
            // боковые бортики цвета дорожки
            Blocky.Part(tr, new Vector3(-2.55f, 0.65f, 0), new Vector3(0.5f, 0.25f, 10.2f), trim);
            Blocky.Part(tr, new Vector3(2.55f, 0.65f, 0), new Vector3(0.5f, 0.25f, 10.2f), trim);
            // передний валик
            Blocky.Part(tr, new Vector3(0, 0.55f, 4.9f), new Vector3(4.6f, 0.35f, 0.5f), trim);
            Blocky.Part(tr, new Vector3(0, 0.55f, -4.9f), new Vector3(4.6f, 0.35f, 0.5f), trim);
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            var colGo = new GameObject("Collider");
            colGo.transform.SetParent(tr, false);
            colGo.transform.localPosition = new Vector3(0, 0.33f, 0);
            colGo.AddComponent<BoxCollider>().size = new Vector3(5.4f, 0.66f, 10.4f);

            // лента со стрелками (UV в юнитах → стрелка каждые ~1.6 юнита)
            t.beltMat = Mats.Unique(new Color(0.22f, 0.22f, 0.26f), BeltTexture());
            t.beltMat.mainTextureScale = new Vector2(0.23f, 0.62f);
            Blocky.Part(tr, new Vector3(0, 0.63f, 0), new Vector3(4.5f, 0.04f, 9.4f), t.beltMat);

            // неоновые полоски вдоль бортиков
            t.neon = new[]
            {
                Blocky.Part(tr, new Vector3(-2.3f, 0.66f, 0), new Vector3(0.08f, 0.06f, 9.6f), glow),
                Blocky.Part(tr, new Vector3(2.3f, 0.66f, 0), new Vector3(0.08f, 0.06f, 9.6f), glow)
            };

            // поручни
            var rail = Mats.Plastic(new Color(0.75f, 0.78f, 0.82f));
            Blocky.Round = true; Blocky.RoundFactor = 0.5f;
            Blocky.Part(tr, new Vector3(-2.55f, 1.8f, 3.8f), new Vector3(0.22f, 2.4f, 0.22f), rail);
            Blocky.Part(tr, new Vector3(2.55f, 1.8f, 3.8f), new Vector3(0.22f, 2.4f, 0.22f), rail);
            Blocky.Part(tr, new Vector3(-2.55f, 2.9f, 1.8f), new Vector3(0.18f, 0.18f, 4f), rail);
            Blocky.Part(tr, new Vector3(2.55f, 2.9f, 1.8f), new Vector3(0.18f, 0.18f, 4f), rail);
            // консоль с экраном
            Blocky.Part(tr, new Vector3(0, 3.1f, 4.1f), new Vector3(5.3f, 1.3f, 0.55f), trim);
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            var scr = Blocky.Part(tr, new Vector3(0, 3.15f, 3.8f), new Vector3(3.8f, 0.9f, 0.05f), Mats.Glow(new Color(0.05f, 0.08f, 0.12f)));
            scr.localRotation = Quaternion.Euler(-12f, 0, 0);
            t.screen = Blocky.Label(tr, "", new Vector3(0, 3.2f, 3.65f), 0.9f, new Color(0.4f, 1f, 0.7f));
            t.screen.maxDistance = 45f;

            t.label = Blocky.Label(tr, "", new Vector3(0, 5f, 4.1f), 1.05f, Color.white);

            // красная лента-ограждение у закрытой дорожки (без коллайдера)
            t.lockRope = new GameObject("Lock");
            t.lockRope.transform.SetParent(tr, false);
            var red = Mats.Plastic(new Color(0.95f, 0.2f, 0.2f));
            Blocky.Part(t.lockRope.transform, new Vector3(-2.6f, 0.9f, -5.3f), new Vector3(0.3f, 1.8f, 0.3f), Mats.Plastic(new Color(0.9f, 0.8f, 0.2f)));
            Blocky.Part(t.lockRope.transform, new Vector3(2.6f, 0.9f, -5.3f), new Vector3(0.3f, 1.8f, 0.3f), Mats.Plastic(new Color(0.9f, 0.8f, 0.2f)));
            Blocky.Part(t.lockRope.transform, new Vector3(0, 1.4f, -5.3f), new Vector3(5.2f, 0.2f, 0.1f), red);

            All.Add(t);
            PlayerController.Interactables.Add(t);
            t.Refresh();
            return t;
        }

        static Texture2D beltTex;
        static Texture2D BeltTexture()
        {
            if (beltTex != null) return beltTex;
            const int N = 32;
            beltTex = new Texture2D(N, N, TextureFormat.RGB24, true);
            beltTex.name = "belt";
            beltTex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float cx = Mathf.Abs(x + 0.5f - N / 2f);
                    float yy = Mathf.Repeat(y + 0.5f - cx * 0.8f, N);
                    bool chev = yy > 10 && yy < 17 && cx < 12;
                    bool groove = y % 8 == 0;
                    float v = chev ? 1.8f : (groove ? 0.7f : 1f);
                    px[y * N + x] = new Color(Mathf.Min(1, 0.55f * v), Mathf.Min(1, 0.55f * v), Mathf.Min(1, 0.6f * v));
                }
            beltTex.SetPixels(px);
            beltTex.Apply(true);
            return beltTex;
        }

        string GainText { get { return "+" + Loc.Num(def.gainPerSec * (GameManager.Instance != null ? GameManager.Instance.TrainMultiplier : 1f)) + (Loc.Ru ? " скорости/сек" : " speed/s"); } }

        public void Refresh()
        {
            string name = Loc.Ru ? def.nameRu : def.nameEn;
            if (Owned) label.text = name;
            else if (SaveManager.Data.rebirths < def.rebirthsRequired)
                label.text = name + "\n<color=#FF8080>" + Loc.F("need_rebirth", def.rebirthsRequired) + "</color>";
            else if (def.adsRequired > 0)
                label.text = name + "\n<color=#FFE060>" + Loc.F("watch_ad_unlock", AdsWatched, def.adsRequired) + "</color>";
            else label.text = name + "\n<color=#FFE060>" + Loc.F("buy_for", Loc.Num(def.price)) + "</color>";
            if (lockRope != null) lockRope.SetActive(!Owned);
        }

        public bool Contains(Vector3 worldPos)
        {
            Vector3 lp = transform.InverseTransformPoint(worldPos);
            return Mathf.Abs(lp.x) < halfSize.x && Mathf.Abs(lp.z) < halfSize.y && lp.y > -0.5f && lp.y < 2.5f;
        }

        public void Train(float dt)
        {
            var gm = GameManager.Instance;
            if (gm == null || !Owned) return;
            activeTimer = 0.15f;
            double g = def.gainPerSec * gm.TrainMultiplier * dt;
            gm.AddSpeedPoints(g);
            gainAccum += g;
            gainPopupTimer -= dt;
            if (gainPopupTimer <= 0)
            {
                gainPopupTimer = 0.45f;
                FloatingText.Spawn(PlayerController.Instance.transform.position + Vector3.up * 3.6f,
                    "+" + Loc.Num(System.Math.Max(1, gainAccum)), new Color(0.4f, 1f, 1f));
                gainAccum = 0;
                GameAudio.Play(Sfx.Tick, 0.35f, 1f + Random.value * 0.2f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (activeTimer > 0) activeTimer -= dt;
            if (botTimer > 0) botTimer -= dt;
            beltSpeed = Mathf.MoveTowards(beltSpeed, (Training || botTimer > 0) ? 3.5f : (Owned ? 0.4f : 0f), dt * 6f);
            beltMat.mainTextureOffset += new Vector2(0, -dt * beltSpeed);

            // экран: прирост, мигает во время бега
            if (Owned)
            {
                screen.text = GainText;
                float pulse = Training ? 0.7f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.3f : 1f;
                screen.color = new Color(0.4f * pulse, 1f * pulse, 0.7f * pulse);
            }
            else screen.text = Loc.Ru ? "ЗАКРЫТО" : "LOCKED";

            // неон "дышит", во время бега светится сильнее (через масштаб толщины)
            float k = Training ? 1.6f + Mathf.Sin(Time.time * 12f) * 0.3f : 1f + Mathf.Sin(Time.time * 2f) * 0.1f;
            for (int i = 0; i < neon.Length; i++)
            {
                var s = neon[i].localScale;
                s.x = k; s.y = k;
                neon[i].localScale = s;
            }
        }

        // ===== IInteractable (открытие) =====
        public Vector3 InteractPos { get { return transform.position; } }
        public float InteractRange { get { return 6f; } }
        public float HoldTime { get { return 0.15f; } }
        public bool CanInteract { get { return !Owned; } }
        public string Prompt
        {
            get
            {
                if (SaveManager.Data.rebirths < def.rebirthsRequired) return Loc.F("need_rebirth", def.rebirthsRequired);
                if (def.adsRequired > 0) return Loc.F("watch_ad_unlock", AdsWatched, def.adsRequired);
                return Loc.F("buy_for", Loc.Num(def.price));
            }
        }

        public void Interact(PlayerController p)
        {
            var gm = GameManager.Instance;
            if (SaveManager.Data.rebirths < def.rebirthsRequired)
            {
                UIManager.Instance.Toast(Loc.F("need_rebirth", def.rebirthsRequired), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return;
            }
            if (def.adsRequired > 0)
            {
                YandexSDK.ShowRewarded(ok =>
                {
                    if (!ok) { UIManager.Instance.Toast(Loc.T("ad_fail"), new Color(1f, 0.6f, 0.4f)); return; }
                    SaveManager.Data.treadmillAds[index]++;
                    UIManager.Instance.Toast(Loc.F("ad_progress", AdsWatched, def.adsRequired), new Color(1f, 0.9f, 0.4f));
                    if (AdsWatched >= def.adsRequired) Unlock();
                    else { GameAudio.Play(Sfx.Coin); Refresh(); gm.SaveNow(true); }
                });
                return;
            }
            if (!gm.TrySpend(def.price))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return;
            }
            Unlock();
        }

        void Unlock()
        {
            SaveManager.Data.treadmillsMask |= 1 << index;
            GameAudio.Play(Sfx.Buy);
            GameAudio.Play(Sfx.Success);
            Fx.Confetti(transform.position + Vector3.up * 2f, 60);
            UIManager.Instance.Toast(Loc.T("unlocked"), new Color(0.5f, 1f, 0.5f));
            Refresh();
            GameManager.Instance.SaveNow(true);
        }
    }
}
