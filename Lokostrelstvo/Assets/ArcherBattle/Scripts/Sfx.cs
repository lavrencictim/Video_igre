using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Synthesises all sound effects at startup (no audio files needed) and plays them
    /// through a small pool of AudioSources so pitch can vary per shot.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;
        const int SampleRate = 44100;

        AudioSource[] pool;
        int next;

        public static AudioClip Draw, Release, HitBody, HitHead, HitGround, Click, Beep, Fight, Victory, Defeat, Crowd, Step;

        public static void Init(Transform parent)
        {
            if (instance != null) return;
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<Sfx>();
            instance.pool = new AudioSource[12];
            for (int i = 0; i < instance.pool.Length; i++)
            {
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;
                instance.pool[i] = a;
            }
            Build();
        }

        /// <summary>Plays a clip and returns the AudioSource (so a long sound can be stopped early).</summary>
        public static AudioSource Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.06f, float pitch = 1f)
        {
            if (instance == null || clip == null) return null;
            var src = instance.pool[instance.next];
            instance.next = (instance.next + 1) % instance.pool.Length;
            src.Stop();
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.Play();
            return src;
        }

        // ------------------------------------------------------------------ synthesis

        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

        static AudioClip ToClip(string name, float[] data, float gain = 0.9f)
        {
            float peak = 0.0001f;
            foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < data.Length; i++) data[i] = data[i] / peak * gain;
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Rnd() => Random.value * 2f - 1f;

        static void Build()
        {
            var oldState = Random.state;
            Random.InitState(1234);

            // --- bow being drawn: wooden creak (accelerating clicks) + rising stretch noise
            {
                var d = Buf(0.9f);
                float lp = 0f, nextClick = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    lp += (Rnd() - lp) * 0.04f;
                    d[i] += lp * 0.35f * Mathf.Clamp01(t * 3f) * (0.4f + t);
                    if (t >= nextClick)
                    {
                        float f = Random.Range(380f, 620f);
                        int len = (int)(0.012f * SampleRate);
                        for (int k = 0; k < len && i + k < d.Length; k++)
                        {
                            float tk = k / (float)SampleRate;
                            d[i + k] += Mathf.Sin(2 * Mathf.PI * f * tk) * Mathf.Exp(-tk * 400f) * 0.5f;
                        }
                        nextClick = t + Mathf.Lerp(0.045f, 0.012f, t / 0.9f) * Random.Range(0.7f, 1.3f);
                    }
                }
                Draw = ToClip("draw", d, 0.5f);
            }

            // --- release: string twang + air whoosh
            {
                var d = Buf(0.5f);
                float lp = 0f, bp = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float f0 = 118f * (1f + 0.15f * Mathf.Exp(-t * 30f));
                    float s = 0f;
                    for (int k = 1; k <= 6; k++)
                        s += Mathf.Sin(2 * Mathf.PI * f0 * k * t) / k * Mathf.Exp(-t * (7f + k * 4f));
                    lp += (Rnd() - lp) * 0.25f;
                    bp += (lp - bp) * 0.1f;
                    float whoosh = (lp - bp) * Mathf.Exp(-Mathf.Pow((t - 0.07f) / 0.05f, 2f)) * 0.9f;
                    float click = t < 0.004f ? Rnd() * (1f - t / 0.004f) : 0f;
                    d[i] = s * 0.8f + whoosh + click * 0.6f;
                }
                Release = ToClip("release", d);
            }

            // --- body hit: meaty thud
            float[] thud = Buf(0.32f);
            {
                float lp = 0f;
                float phase = 0f;
                for (int i = 0; i < thud.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float f = Mathf.Lerp(55f, 170f, Mathf.Exp(-t * 25f));
                    phase += 2 * Mathf.PI * f / SampleRate;
                    lp += (Rnd() - lp) * 0.3f;
                    thud[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 13f) + lp * Mathf.Exp(-t * 40f) * 0.8f
                              + (t < 0.006f ? Rnd() * 0.7f : 0f);
                }
                HitBody = ToClip("hitBody", (float[])thud.Clone());
            }

            // --- head hit: thud + bright metallic ding
            {
                var d = Buf(0.8f);
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float ding = (Mathf.Sin(2 * Mathf.PI * 1318f * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1976f * t)
                                  + 0.3f * Mathf.Sin(2 * Mathf.PI * 2637f * t)) * Mathf.Exp(-t * 5f);
                    d[i] = (i < thud.Length ? thud[i] : 0f) + ding * 0.45f;
                }
                HitHead = ToClip("hitHead", d);
            }

            // --- arrow hitting the ground / wood
            {
                var d = Buf(0.2f);
                float lp = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    lp += (Rnd() - lp) * 0.15f;
                    d[i] = lp * Mathf.Exp(-t * 35f) + Mathf.Sin(2 * Mathf.PI * 210f * t) * Mathf.Exp(-t * 45f) * 0.6f;
                }
                HitGround = ToClip("hitGround", d, 0.7f);
            }

            // --- footstep (soft scuff)
            {
                var d = Buf(0.09f);
                float lp = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    lp += (Rnd() - lp) * 0.1f;
                    d[i] = lp * Mathf.Exp(-t * 50f);
                }
                Step = ToClip("step", d, 0.5f);
            }

            Click = Tone(0.06f, 880f, 60f, 0.5f);
            Beep = Tone(0.18f, 660f, 14f, 0.6f);
            Fight = Tone(0.45f, 990f, 5f, 0.7f);
            Victory = Melody(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f }, new[] { 0.12f, 0.12f, 0.12f, 0.22f, 0.1f, 0.6f });
            Defeat = Melody(new[] { 392f, 349.23f, 311.13f, 261.63f }, new[] { 0.25f, 0.25f, 0.25f, 0.8f });

            // --- crowd cheer: band-limited noise with many small swells
            {
                var d = Buf(1.4f);
                float lp1 = 0f, lp2 = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    lp1 += (Rnd() - lp1) * 0.2f;
                    lp2 += (lp1 - lp2) * 0.05f;
                    float env = Mathf.Clamp01(t * 6f) * Mathf.Clamp01((1.4f - t) * 1.5f);
                    float wobble = 0.7f + 0.3f * Mathf.Sin(t * 23f) * Mathf.Sin(t * 7f);
                    d[i] = (lp1 - lp2) * env * wobble;
                }
                Crowd = ToClip("crowd", d, 0.5f);
            }

            Random.state = oldState;
        }

        static AudioClip Tone(float seconds, float freq, float decay, float gain)
        {
            var d = Buf(seconds);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                d[i] = (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * freq * 2f * t))
                       * Mathf.Exp(-t * decay) * Mathf.Clamp01(t * 400f);
            }
            return ToClip("tone" + freq, d, gain);
        }

        static AudioClip Melody(float[] notes, float[] durations)
        {
            float total = 0f;
            foreach (var x in durations) total += x;
            var d = Buf(total + 0.3f);
            float start = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                int s0 = (int)(start * SampleRate);
                float len = durations[n] + 0.25f;
                for (int i = 0; i < len * SampleRate && s0 + i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float f = notes[n];
                    float v = Mathf.Sin(2 * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2 * Mathf.PI * f * 3f * t) + 0.2f * Mathf.Sin(2 * Mathf.PI * f * 2f * t);
                    d[s0 + i] += v * Mathf.Exp(-t * 3.5f) * Mathf.Clamp01(t * 300f);
                }
                start += durations[n];
            }
            return ToClip("melody", d, 0.6f);
        }
    }
}
