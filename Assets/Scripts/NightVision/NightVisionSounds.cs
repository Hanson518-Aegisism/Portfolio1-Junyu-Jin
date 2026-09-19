using UnityEngine;

/// <summary>
/// Short electronic power-up and power-down clips for the night-vision goggles.
/// </summary>
public static class NightVisionSounds
{
    const int SampleRate = 44100;

    public static AudioClip CreatePowerOn()
    {
        return Build("NightVisionPowerOn", 0.5f, true);
    }

    public static AudioClip CreatePowerOff()
    {
        return Build("NightVisionPowerOff", 0.4f, false);
    }

    static AudioClip Build(string name, float duration, bool poweringOn)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[sampleCount];
        float tonePhase = 0f;
        float beepPhase = 0f;
        float noise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            noise = noise * 0.82f + Hash(i) * 0.18f;

            float click = 0f;
            if (t < 0.022f)
                click = (1f - t / 0.022f) * noise * 0.85f;

            float sweep = Mathf.Clamp01(t / (duration * 0.86f));
            float freq = poweringOn
                ? Mathf.Lerp(140f, 920f, sweep * sweep)
                : Mathf.Lerp(880f, 70f, sweep);
            tonePhase += freq / SampleRate;
            float tone = Mathf.Sin(tonePhase * Mathf.PI * 2f);
            float harmonic = Mathf.Sin(tonePhase * Mathf.PI * 4f) * 0.35f;

            float envelope = poweringOn
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.08f)) * (1f - sweep * 0.25f)
                : (1f - sweep) * (1f - sweep);

            if (t > duration - 0.05f)
                envelope *= Mathf.Clamp01((duration - t) / 0.05f);

            float beep = 0f;
            if (poweringOn && t > 0.16f && t < 0.28f)
            {
                float beepT = (t - 0.16f) / 0.12f;
                beepPhase += 1680f / SampleRate;
                beep = Mathf.Sin(beepPhase * Mathf.PI * 2f) * Mathf.Sin(beepT * Mathf.PI) * 0.32f;
            }

            float sample = click + (tone + harmonic) * envelope * 0.42f + beep;
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Hash(int index)
    {
        uint x = (uint)(index * 1103515245 + 12345);
        return ((x >> 16) & 32767) / 32767f * 2f - 1f;
    }
}
