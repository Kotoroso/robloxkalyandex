using UnityEngine;

namespace DragonHeist
{
    public enum Sfx { Jump, Step, Grab, Caught, Laugh, Success, Plant, Hatch, Coin, Buy, Error, Rebirth, Tick, Click }

    /// <summary>
    /// Весь звук синтезируется кодом при старте (чиптюн-музыка + эффекты):
    /// ноль мегабайт в билде и никаких проблем с авторскими правами на модерации Яндекса.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const int SR = 22050;
        static GameAudio inst;
        AudioClip[] clips;
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

        public static void Play(Sfx s, float vol = 1f)
        {
            if (inst == null || inst.clips == null) return;
            var src = inst.sources[inst.next];
            inst.next = (inst.next + 1) % inst.sources.Length;
            src.pitch = s == Sfx.Step ? Random.Range(0.85f, 1.15f) : 1f;
            src.PlayOneShot(inst.clips[(int)s], vol);
        }

        void Build()
        {
            sources = new AudioSource[6];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0;
            }
            clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
            clips[(int)Sfx.Jump] = Make("jump", 0.16f, (t, d) => Sq(Lerp(300, 750, t / d), t) * Env(t, d, 0.005f) * 0.35f);
            clips[(int)Sfx.Step] = Make("step", 0.05f, (t, d) => Noise() * Env(t, d, 0.002f) * 0.25f);
            clips[(int)Sfx.Grab] = Make("grab", 0.3f, (t, d) => Tri(Arp(t, 0.1f, 523, 659, 784), t) * Env(t, d, 0.005f) * 0.5f);
            clips[(int)Sfx.Caught] = Make("caught", 0.55f, (t, d) => Sq(Lerp(620, 140, t / d), t) * Env(t, d, 0.01f) * 0.3f);
            clips[(int)Sfx.Laugh] = Make("laugh", 0.6f, (t, d) =>
            {
                float seg = (t % 0.2f) / 0.2f;
                return Saw(260 + Mathf.Sin(t * 60) * 20, t) * (seg < 0.6f ? Mathf.Sin(seg / 0.6f * Mathf.PI) : 0) * 0.25f;
            });
            clips[(int)Sfx.Success] = Make("success", 0.5f, (t, d) => Sq(Arp(t, 0.1f, 523, 659, 784, 1047), t) * Env(t, d, 0.005f) * 0.25f);
            clips[(int)Sfx.Plant] = Make("plant", 0.25f, (t, d) => Sin(Lerp(180, 70, t / d), t) * Env(t, d, 0.003f) * 0.7f);
            clips[(int)Sfx.Hatch] = Make("hatch", 1.0f, (t, d) =>
                (Tri(Arp(t, 0.08f, 523, 659, 784, 1047, 1319, 1568, 2093), t) * 0.4f + Noise() * 0.05f * Mathf.Exp(-t * 8)) * Env(t, d, 0.005f));
            clips[(int)Sfx.Coin] = Make("coin", 0.3f, (t, d) => Sq(t < 0.07f ? 988 : 1319, t) * Env(t, d, 0.002f) * 0.25f);
            clips[(int)Sfx.Buy] = Make("buy", 0.3f, (t, d) => Tri(t < 0.1f ? 784 : 1175, t) * Env(t, d, 0.004f) * 0.5f);
            clips[(int)Sfx.Error] = Make("error", 0.25f, (t, d) => Saw(150, t) * Env(t, d, 0.004f) * 0.3f);
            clips[(int)Sfx.Rebirth] = Make("rebirth", 1.4f, (t, d) =>
                (Sq(Lerp(200, 1200, t / d), t) * 0.15f + Tri(Arp(t, 0.12f, 523, 784, 1047, 1568), t) * 0.3f) * Env(t, d, 0.01f));
            clips[(int)Sfx.Tick] = Make("tick", 0.04f, (t, d) => Sin(1400, t) * Env(t, d, 0.001f) * 0.4f);
            clips[(int)Sfx.Click] = Make("click", 0.05f, (t, d) => Sq(900, t) * Env(t, d, 0.001f) * 0.2f);

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.22f;
            music.clip = BuildMusic();
            music.Play();
            SetEnabled(SaveManager.Data.soundOn);
        }

        delegate float Synth(float t, float dur);

        static AudioClip Make(string name, float dur, Synth f)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SR, dur), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static readonly System.Random rng = new System.Random(1);
        static float Noise() { return (float)(rng.NextDouble() * 2 - 1); }
        static float Lerp(float a, float b, float t) { return a + (b - a) * Mathf.Clamp01(t); }
        static float Sin(float f, float t) { return Mathf.Sin(2 * Mathf.PI * f * t); }
        static float Sq(float f, float t) { return Mathf.Repeat(f * t, 1f) < 0.5f ? 1f : -1f; }
        static float Saw(float f, float t) { return Mathf.Repeat(f * t, 1f) * 2f - 1f; }
        static float Tri(float f, float t) { float p = Mathf.Repeat(f * t, 1f); return p < 0.5f ? p * 4 - 1 : 3 - p * 4; }
        static float Arp(float t, float step, params float[] notes) { return notes[Mathf.Min((int)(t / step), notes.Length - 1)]; }
        static float Env(float t, float d, float attack)
        {
            float a = Mathf.Clamp01(t / attack);
            float r = Mathf.Clamp01((d - t) / (d * 0.6f));
            return a * r;
        }

        static float Midi(int n) { return 440f * Mathf.Pow(2f, (n - 69) / 12f); }

        /// <summary>Весёлый чиптюн-луп в стиле симуляторов: C - Am - F - G, 128 BPM.</summary>
        static AudioClip BuildMusic()
        {
            const float bpm = 128f;
            float beat = 60f / bpm;
            int bars = 8;
            float dur = bars * 4 * beat;
            int n = Mathf.CeilToInt(dur * SR);
            var data = new float[n];
            int[][] chords = { new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 } };
            int[] melody = { 72, 74, 76, 79, 76, 74, 72, 67,  69, 72, 76, 74, 72, 69, 67, 64,
                             65, 69, 72, 74, 72, 69, 65, 69,  67, 71, 74, 79, 77, 74, 71, 67 };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float beatPos = t / beat;
                int bar = (int)(beatPos / 4) % bars;
                var ch = chords[bar % 4];
                float eighth = beatPos * 2f;
                int e = (int)eighth;
                float ef = eighth - e;
                // бас восьмыми
                int bassNote = ch[0] - 24 + ((e % 2 == 1) ? 12 : 0);
                float bass = Sq(Midi(bassNote), t) * 0.18f * Mathf.Exp(-ef * 3f);
                // арпеджио шестнадцатыми
                float sixteenth = beatPos * 4f;
                int s = (int)sixteenth;
                float arp = Tri(Midi(ch[s % 3] + 12), t) * 0.08f * Mathf.Exp(-(sixteenth - s) * 4f);
                // мелодия (со второй половины лупа)
                float mel = 0;
                if (bar >= 4)
                {
                    int m = e % melody.Length;
                    mel = Sq(Midi(melody[m]), t) * 0.07f * Mathf.Exp(-ef * 2.5f);
                }
                // барабаны
                float bf = beatPos - Mathf.Floor(beatPos);
                float kick = Mathf.Sin(2 * Mathf.PI * (60 + 120 * Mathf.Exp(-bf * 30)) * bf * beat) * Mathf.Exp(-bf * 10f) * 0.35f;
                float hat = (e % 2 == 1) ? Noise() * 0.05f * Mathf.Exp(-ef * 20f) : 0;
                float snare = ((int)beatPos % 2 == 1) ? Noise() * 0.12f * Mathf.Exp(-bf * 14f) : 0;
                data[i] = Mathf.Clamp(bass + arp + mel + kick + hat + snare, -1f, 1f);
            }
            var clip = AudioClip.Create("music", n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
