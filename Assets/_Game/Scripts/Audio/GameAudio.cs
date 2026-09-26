using UnityEngine;

namespace DragonHeist
{
    public enum Sfx { Jump, Step, Grab, Caught, Laugh, Success, Plant, Hatch, Coin, Buy, Error, Rebirth, Tick, Click, Land, Whoosh }

    /// <summary>
    /// Звук: если в Resources/Audio лежит файл с именем эффекта (jump, coin, ...) — используется он (CC0-ассеты),
    /// иначе эффект синтезируется кодом. Музыка — Resources/Audio/music, иначе синтезированный луп
    /// (маримба на Karplus-Strong + бас + барабаны + реверб). 3 набора звуков механических свитчей для ASMR-пола.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const int SR = 22050;
        static GameAudio inst;
        AudioClip[] clips;
        AudioClip[,] switches; // [тип, 0 = нажатие / 1 = отпускание]
        AudioSource[] sources;
        AudioSource music;
        int next;

        public static void Create()
        {
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<GameAudio>();
            inst.Build();
        }

        public static void SetEnabled(bool on) { AudioListener.volume = on ? 1f : 0f; }

        public static void SetMusic(bool on)
        {
            if (inst == null || inst.music == null) return;
            inst.music.mute = !on;
        }

        public static void Play(Sfx s, float vol = 1f, float pitch = 1f)
        {
            if (inst == null || inst.clips == null) return;
            var clip = inst.clips[(int)s];
            if (clip == null) return;
            var src = inst.NextSource();
            src.pitch = s == Sfx.Step ? Random.Range(0.9f, 1.1f) * pitch : pitch;
            src.PlayOneShot(clip, vol * SfxVolume);
        }

        /// <summary>Общая громкость эффектов (было громко — теперь спокойнее).</summary>
        public const float SfxVolume = 0.45f;

        public static void PlaySwitch(int type, bool down)
        {
            if (inst == null || inst.switches == null) return;
            var src = inst.NextSource();
            src.pitch = Random.Range(0.94f, 1.06f);
            src.PlayOneShot(inst.switches[Mathf.Clamp(type, 0, 2), down ? 0 : 1], (down ? 0.55f : 0.35f) * SfxVolume * 1.6f);
        }

        AudioSource NextSource()
        {
            var src = sources[next];
            next = (next + 1) % sources.Length;
            return src;
        }

        static AudioClip Load(string name)
        {
            return Resources.Load<AudioClip>("Audio/" + name);
        }

        void Build()
        {
            sources = new AudioSource[8];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0;
            }

            clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
            clips[(int)Sfx.Jump] = Load("jump") ?? Make("jump", 0.18f, (t, d) => Tri(Lerp(320, 820, t / d), t) * Env(t, d, 0.004f) * 0.5f);
            clips[(int)Sfx.Land] = Load("land") ?? Make("land", 0.12f, (t, d) => (Sin(Lerp(160, 60, t / d), t) * 0.7f + LP(Noise(), 0.2f) * 0.3f) * Env(t, d, 0.002f));
            clips[(int)Sfx.Step] = Load("footstep") ?? Make("step", 0.07f, (t, d) => LP(Noise(), 0.25f) * Env(t, d, 0.002f) * 0.6f);
            clips[(int)Sfx.Grab] = Load("grab") ?? Make("grab", 0.35f, (t, d) => Pluck(Arp(t, 0.07f, 659, 880, 1175), t % 0.07f) * 0.7f);
            clips[(int)Sfx.Caught] = Load("caught") ?? Make("caught", 0.6f, (t, d) => (Sq(Lerp(520, 110, t / d), t) * 0.25f + LP(Noise(), 0.1f) * 0.2f * Mathf.Exp(-t * 8)) * Env(t, d, 0.01f));
            clips[(int)Sfx.Laugh] = Load("laugh") ?? Make("laugh", 0.6f, (t, d) =>
            {
                float seg = (t % 0.15f) / 0.15f;
                return Saw(240 + Mathf.Sin(t * 50) * 30, t) * (seg < 0.6f ? Mathf.Sin(seg / 0.6f * Mathf.PI) : 0) * 0.2f;
            });
            clips[(int)Sfx.Success] = Make("success", 0.7f, (t, d) => Bell(Arp(t, 0.08f, 659, 784, 988), t % 0.08f + (t > 0.16f ? t - 0.16f : 0)) * 0.35f);
            clips[(int)Sfx.Plant] = Load("plant") ?? Make("plant", 0.3f, (t, d) => (Sin(Lerp(200, 80, t / d), t) * 0.8f + LP(Noise(), 0.15f) * 0.3f * Mathf.Exp(-t * 20)) * Env(t, d, 0.003f));
            clips[(int)Sfx.Hatch] = Load("hatch") ?? Make("hatch", 1.3f, (t, d) =>
                (Bell(Arp(t, 0.07f, 523, 659, 784, 1047, 1319, 1568, 2093), t % 0.07f + (t > 0.42f ? t - 0.42f : 0)) * 0.5f + LP(Noise(), 0.5f) * 0.15f * Mathf.Exp(-t * 12)));
            clips[(int)Sfx.Coin] = Load("coin") ?? Make("coin", 0.4f, (t, d) => Bell(t < 0.06f ? 1319 : 1760, t < 0.06f ? t : t - 0.06f) * 0.6f);
            clips[(int)Sfx.Buy] = Load("buy") ?? Make("buy", 0.45f, (t, d) => (Bell(t < 0.08f ? 880 : 1319, t < 0.08f ? t : t - 0.08f) * 0.6f));
            clips[(int)Sfx.Error] = Make("error", 0.3f, (t, d) => (Sq(180, t) * 0.15f + Sq(190, t) * 0.15f) * Env(t, d, 0.004f));
            clips[(int)Sfx.Rebirth] = Load("rebirth") ?? Make("rebirth", 1.8f, (t, d) =>
                (Sin(Lerp(150, 900, t / d), t) * 0.2f + Bell(Arp(t, 0.12f, 523, 659, 784, 1047, 1319, 1568), t % 0.12f + (t > 0.6f ? t - 0.6f : 0)) * 0.45f) * Env(t, d, 0.01f));
            clips[(int)Sfx.Tick] = Load("tick") ?? Make("tick", 0.05f, (t, d) => Sin(1600, t) * Mathf.Exp(-t * 90) * 0.35f);
            clips[(int)Sfx.Click] = Load("ui_click") ?? Make("click", 0.06f, (t, d) => (Sin(1200, t) * 0.5f + LP(Noise(), 0.6f) * 0.3f) * Mathf.Exp(-t * 70) * 0.6f);
            clips[(int)Sfx.Whoosh] = Load("whoosh") ?? Make("whoosh", 0.4f, (t, d) => LP(Noise(), Lerp(0.05f, 0.4f, Mathf.Sin(t / d * Mathf.PI))) * Mathf.Sin(t / d * Mathf.PI) * 0.5f);

            BuildSwitches();

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            var mclip = Load("music");
            music.volume = mclip != null ? 0.18f : 0.16f;
            music.clip = mclip != null ? mclip : BuildMusic();
            music.Play();
            SetEnabled(SaveManager.Data.soundOn);
            SetMusic(SaveManager.Data.musicOn);
        }

        // ================== Механические свитчи ==================
        void BuildSwitches()
        {
            switches = new AudioClip[3, 2];
            // Синий: звонкий щелчок (click jacket) + удар о дно
            switches[0, 0] = Make("sw_blue_down", 0.09f, (t, d) =>
                Damped(4200, t, 260) * 0.45f + Damped(2600, t - 0.004f, 200) * 0.3f + HP(Noise()) * 0.5f * Mathf.Exp(-t * 900)
                + Damped(420, t - 0.012f, 90) * 0.35f + Damped(1300, t - 0.012f, 160) * 0.2f);
            switches[0, 1] = Make("sw_blue_up", 0.06f, (t, d) => Damped(3600, t, 320) * 0.35f + HP(Noise()) * 0.3f * Mathf.Exp(-t * 1200));
            // Коричневый: мягкий тактильный "тук"
            switches[1, 0] = Make("sw_brown_down", 0.09f, (t, d) =>
                Damped(320, t, 70) * 0.5f + Damped(1100, t, 140) * 0.25f + LP(Noise(), 0.35f) * 0.35f * Mathf.Exp(-t * 400)
                + Damped(2000, t - 0.003f, 300) * 0.08f);
            switches[1, 1] = Make("sw_brown_up", 0.05f, (t, d) => Damped(900, t, 200) * 0.2f + LP(Noise(), 0.4f) * 0.2f * Mathf.Exp(-t * 600));
            // Красный: глубокий линейный "thock"
            switches[2, 0] = Make("sw_red_down", 0.1f, (t, d) =>
                Damped(190, t, 55) * 0.6f + Damped(700, t, 110) * 0.25f + LP(Noise(), 0.2f) * 0.3f * Mathf.Exp(-t * 350));
            switches[2, 1] = Make("sw_red_up", 0.05f, (t, d) => Damped(500, t, 150) * 0.15f + LP(Noise(), 0.25f) * 0.12f * Mathf.Exp(-t * 500));
        }

        delegate float Synth(float t, float dur);

        static AudioClip Make(string name, float dur, Synth f)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var data = new float[n];
            lpState = 0; hpPrev = 0; hpOut = 0;
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SR, dur), -1f, 1f);
            // короткий фейд в конце — без щелчков
            int fade = Mathf.Min(64, n);
            for (int i = 0; i < fade; i++) data[n - 1 - i] *= i / (float)fade;
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static readonly System.Random rng = new System.Random(1);
        static float lpState, hpPrev, hpOut;
        static float Noise() { return (float)(rng.NextDouble() * 2 - 1); }
        static float LP(float x, float k) { lpState += (x - lpState) * k; return lpState; }
        static float HP(float x) { hpOut = 0.7f * (hpOut + x - hpPrev); hpPrev = x; return hpOut; }
        static float Lerp(float a, float b, float t) { return a + (b - a) * Mathf.Clamp01(t); }
        static float Sin(float f, float t) { return Mathf.Sin(2 * Mathf.PI * f * t); }
        static float Sq(float f, float t) { return Mathf.Repeat(f * t, 1f) < 0.5f ? 1f : -1f; }
        static float Saw(float f, float t) { return Mathf.Repeat(f * t, 1f) * 2f - 1f; }
        static float Tri(float f, float t) { float p = Mathf.Repeat(f * t, 1f); return p < 0.5f ? p * 4 - 1 : 3 - p * 4; }
        static float Damped(float f, float t, float decay) { return t < 0 ? 0 : Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * decay); }
        /// <summary>Колокольчик: основной тон + обертоны с быстрым затуханием.</summary>
        static float Bell(float f, float t) { return t < 0 ? 0 : (Sin(f, t) + 0.5f * Sin(f * 2.01f, t) * Mathf.Exp(-t * 6) + 0.25f * Sin(f * 3.9f, t) * Mathf.Exp(-t * 12)) * Mathf.Exp(-t * 5f) * Mathf.Clamp01(t * 400f); }
        static float Pluck(float f, float t) { return t < 0 ? 0 : (Tri(f, t) * 0.7f + Sin(f * 2, t) * 0.3f) * Mathf.Exp(-t * 18f); }
        static float Arp(float t, float step, params float[] notes) { return notes[Mathf.Min((int)(t / step), notes.Length - 1)]; }
        static float Env(float t, float d, float attack)
        {
            float a = Mathf.Clamp01(t / attack);
            float r = Mathf.Clamp01((d - t) / (d * 0.6f));
            return a * r;
        }
        static float Midi(int n) { return 440f * Mathf.Pow(2f, (n - 69) / 12f); }

        /// <summary>Karplus-Strong: реалистичный щипок/маримба, добавляется в буфер.</summary>
        static void KarplusStrong(float[] buf, int start, float freq, float dur, float vol, float damp)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(SR / freq));
            var ring = new float[period];
            for (int i = 0; i < period; i++) ring[i] = Noise() * 0.5f + Mathf.Sin(i * 2f * Mathf.PI / period) * 0.5f;
            int n = Mathf.Min(buf.Length - start, (int)(dur * SR));
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (idx + 1) % period;
                float v = ring[idx];
                ring[idx] = (ring[idx] + ring[j]) * 0.5f * damp;
                idx = j;
                buf[start + i] += v * vol;
            }
        }

        /// <summary>Весёлый луп лобби роблокс-симулятора: F - Dm - Bb - C, 118 BPM, 16 тактов.</summary>
        static AudioClip BuildMusic()
        {
            const float bpm = 118f;
            float beat = 60f / bpm;
            const int bars = 16;
            float dur = bars * 4 * beat;
            int n = Mathf.CeilToInt(dur * SR);
            var mel = new float[n];
            var data = new float[n];
            int[][] chords = { new[] { 53, 57, 60, 65 }, new[] { 50, 53, 57, 62 }, new[] { 46, 50, 53, 58 }, new[] { 48, 52, 55, 60 } };
            // мелодия восьмыми (-1 = пауза), два разных куплета
            int[] melody = {
                72, -1, 69, 72, 74, -1, 72, 69,   74, -1, 72, 69, 65, -1, 67, 69,
                70, -1, 69, 67, 65, -1, 67, 69,   67, -1, 65, 64, 65, -1, -1, -1,
                77, -1, 76, 74, 72, -1, 74, 76,   77, -1, 74, 72, 69, -1, 72, 74,
                74, -1, 72, 70, 69, -1, 70, 72,   72, 74, 72, 70, 69, -1, -1, -1 };

            // арпеджио-маримба шестнадцатыми и мелодия (Karplus-Strong)
            for (int bar = 0; bar < bars; bar++)
            {
                var ch = chords[bar % 4];
                for (int s16 = 0; s16 < 16; s16++)
                {
                    int start = (int)((bar * 4 + s16 * 0.25f) * beat * SR);
                    int note = ch[(s16 % 4 == 3) ? 2 : s16 % 4] + 12;
                    if (s16 % 2 == 0 || bar >= 8) KarplusStrong(data, start, Midi(note), 0.35f, 0.16f, 0.994f);
                }
                if (bar >= 4)
                {
                    for (int e = 0; e < 8; e++)
                    {
                        int m = melody[((bar - 4) % 8) * 8 + e];
                        if (m < 0) continue;
                        int start = (int)((bar * 4 + e * 0.5f) * beat * SR);
                        KarplusStrong(mel, start, Midi(m), 0.6f, 0.28f, 0.997f);
                    }
                }
            }

            float bassLp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float beatPos = t / beat;
                int bar = (int)(beatPos / 4) % bars;
                var ch = chords[bar % 4];
                float bf = beatPos - Mathf.Floor(beatPos);
                float eighth = beatPos * 2f;
                float ef = eighth - Mathf.Floor(eighth);
                // бас: корень на долю, октава на слабую восьмую, мягкий фильтр
                int bassNote = ch[0] - 12 + (((int)eighth % 2 == 1) ? 12 : 0);
                float braw = Saw(Midi(bassNote), t) * 0.5f + Sin(Midi(bassNote), t);
                bassLp += (braw - bassLp) * 0.08f;
                float bass = bassLp * 0.22f * (1f - ef * 0.6f);
                // барабаны
                float kick = Mathf.Sin(2 * Mathf.PI * (50 + 110 * Mathf.Exp(-bf * 28)) * bf * beat) * Mathf.Exp(-bf * 9f) * 0.42f;
                bool snareBeat = ((int)beatPos % 2 == 1);
                float snare = snareBeat ? (Noise() * 0.16f + Sin(190, bf * beat) * 0.1f) * Mathf.Exp(-bf * 16f) : 0;
                float hat = (((int)eighth) % 2 == 1) ? Noise() * 0.05f * Mathf.Exp(-ef * 30f) : 0;
                if (bar < 2) { kick *= 0.0f; snare *= 0f; }
                // сайдчейн от бочки
                float duck = 0.7f + 0.3f * Mathf.Clamp01(bf * 4f);
                data[i] = (data[i] + mel[i]) * duck + bass * duck + kick + snare + hat;
            }

            // простой реверб: 3 гребенчатых фильтра
            int[] delays = { 1116, 1422, 1617 };
            var outb = new float[n];
            foreach (int dl in delays)
            {
                var comb = new float[dl];
                int ci = 0;
                for (int i = 0; i < n; i++)
                {
                    float y = comb[ci];
                    comb[ci] = data[i] + y * 0.72f;
                    ci = (ci + 1) % dl;
                    outb[i] += y * 0.12f;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp((data[i] + outb[i]) * 0.9f, -1f, 1f);

            var clip = AudioClip.Create("music", n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
