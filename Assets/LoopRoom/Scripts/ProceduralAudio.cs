using UnityEngine;

namespace LoopRoom
{
    // Code-synthesized sound effects and ambience for LoopDemo (no audio assets).
    // Every clip is generated deterministically (fixed LCG seeds) at Start().
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        static float NextNoise(ref uint seed)
        {
            seed = 1664525u * seed + 1013904223u;
            return (seed >> 8) / (float)0xFFFFFF * 2f - 1f;
        }

        static void Normalize(float[] samples, float peak)
        {
            float max = 0f;
            for (int i = 0; i < samples.Length; i++) { float a = Mathf.Abs(samples[i]); if (a > max) max = a; }
            if (max <= 1e-6f) return;
            float scale = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
        }

        static AudioClip Build(string name, float[] samples, int rate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // Start of a loop: a bell-like tone, fundamental plus inharmonic overtones (real bell
        // partial ratios), each with its own decay so the sound brightens then settles into a hum.
        public static AudioClip Chime()
        {
            const int rate = SampleRate; const float duration = 1.3f; const float f0 = 600f;
            var ratio = new[] { .5f, 1f, 1.2f, 1.5f, 2f, 2.7f };
            var amp = new[] { .35f, 1f, .5f, .4f, .28f, .15f };
            var decay = new[] { 3.6f, 4.2f, 4.8f, 5.4f, 6.2f, 7.4f };
            int n = (int)(duration * rate); var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate; float s = 0f;
                for (int p = 0; p < ratio.Length; p++) s += amp[p] * Mathf.Sin(2 * Mathf.PI * f0 * ratio[p] * t) * Mathf.Exp(-decay[p] * t);
                samples[i] = s;
            }
            Normalize(samples, .5f);
            return Build("Chime", samples, rate);
        }

        // t=3 latch/footstep cue: a low thump with a very short noise crack for the "snap" of contact.
        public static AudioClip Latch()
        {
            const int rate = SampleRate; const float duration = .13f; uint seed = 12401;
            int n = (int)(duration * rate); var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float thump = Mathf.Sin(2 * Mathf.PI * 90f * t) * Mathf.Exp(-30f * t);
                float crack = NextNoise(ref seed) * Mathf.Exp(-55f * t);
                samples[i] = thump * .75f + crack * .4f;
            }
            Normalize(samples, .6f);
            return Build("Latch", samples, rate);
        }

        // Gunshot: sharp noise attack, a low impact tone, and a short quiet noise tail for reverb.
        public static AudioClip Shot()
        {
            const int rate = SampleRate; const float duration = .32f; uint seed = 55201;
            int n = (int)(duration * rate); var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float crack = NextNoise(ref seed) * Mathf.Exp(-45f * t);
                float thump = Mathf.Sin(2 * Mathf.PI * 65f * t) * Mathf.Exp(-14f * t);
                float tail = NextNoise(ref seed) * Mathf.Exp(-15f * t);
                samples[i] = crack * .7f + thump * .55f + tail * .22f;
            }
            Normalize(samples, .65f);
            return Build("Shot", samples, rate);
        }

        // Exit unlock: two short metallic tones in sequence (click, then a brighter clink).
        public static AudioClip Open()
        {
            const int rate = SampleRate; const float duration = .42f;
            const float t0 = 0f, f0 = 700f, decay0 = 15f;
            const float t1 = .10f, f1 = 1100f, decay1 = 14f;
            int n = (int)(duration * rate); var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate; float s = 0f;
                if (t >= t0) { float lt = t - t0; s += (Mathf.Sin(2 * Mathf.PI * f0 * lt) + .4f * Mathf.Sin(2 * Mathf.PI * f0 * 2.8f * lt)) * Mathf.Exp(-decay0 * lt); }
                if (t >= t1) { float lt = t - t1; s += (Mathf.Sin(2 * Mathf.PI * f1 * lt) + .35f * Mathf.Sin(2 * Mathf.PI * f1 * 2.6f * lt)) * Mathf.Exp(-decay1 * lt) * .85f; }
                samples[i] = s;
            }
            Normalize(samples, .55f);
            return Build("Open", samples, rate);
        }

        // Room ambience loop: a low hum (two close frequencies beating slowly) plus a thin
        // "air" texture built from harmonics of 1/loopSeconds. Because every partial completes
        // a whole number of cycles inside the buffer, the waveform repeats exactly at the loop
        // point (no crossfade needed, no click).
        public static AudioClip RoomTone()
        {
            const int rate = SampleRate; const float loopSeconds = 4f;
            float fundamental = 1f / loopSeconds; const float humA = 55f, humB = 58.5f;
            const int harmonics = 48; var freq = new float[harmonics]; var amp = new float[harmonics]; var phase = new float[harmonics];
            uint seed = 7001;
            for (int h = 0; h < harmonics; h++)
            {
                int k = 800 + h * 32; // integer harmonic index -> freq is an exact multiple of fundamental
                freq[h] = k * fundamental;
                phase[h] = (NextNoise(ref seed) + 1f) * .5f * 2f * Mathf.PI;
                amp[h] = (1f / (h + 3)) * (.6f + .4f * (NextNoise(ref seed) + 1f) * .5f);
            }
            int n = (int)(loopSeconds * rate); var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float hum = Mathf.Sin(2 * Mathf.PI * humA * t) * .5f + Mathf.Sin(2 * Mathf.PI * humB * t) * .5f;
                float air = 0f;
                for (int h = 0; h < harmonics; h++) air += amp[h] * Mathf.Sin(2 * Mathf.PI * freq[h] * t + phase[h]);
                samples[i] = hum * .6f + air * .06f;
            }
            Normalize(samples, .18f);
            return Build("RoomTone", samples, rate);
        }
    }
}
