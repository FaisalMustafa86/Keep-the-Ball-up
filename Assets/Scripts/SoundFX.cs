using UnityEngine;

public enum Sound { Paddle, Perfect, Wall, Launch, GameOver, Click, LevelUp, Tick, Go }

// Simple retro sounds made in code, so the game needs no audio files.
public static class SoundFX
{
    private const int SampleRate = 44100;

    private static AudioSource source;
    private static AudioClip[] clips;

    public static void Init(GameObject host)
    {
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;

        clips = new AudioClip[System.Enum.GetValues(typeof(Sound)).Length];
        clips[(int)Sound.Paddle] = Tone(520f, 880f, 0.09f, 0.35f);
        clips[(int)Sound.Perfect] = Arpeggio(new[] { 880f, 1320f }, 0.07f, 0.3f);
        clips[(int)Sound.Wall] = Tone(320f, 260f, 0.05f, 0.18f);
        clips[(int)Sound.Launch] = Tone(380f, 1000f, 0.18f, 0.3f);
        clips[(int)Sound.GameOver] = Tone(420f, 70f, 0.7f, 0.4f);
        clips[(int)Sound.Click] = Tone(700f, 600f, 0.04f, 0.25f);
        clips[(int)Sound.LevelUp] = Arpeggio(new[] { 523f, 659f, 784f, 1047f }, 0.08f, 0.3f);
        clips[(int)Sound.Tick] = Tone(600f, 600f, 0.08f, 0.25f);
        clips[(int)Sound.Go] = Tone(900f, 900f, 0.2f, 0.3f);

        Settings.ApplySound();
    }

    public static void Play(Sound sound, float pitch = 1f)
    {
        if (source == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clips[(int)sound]);
    }

    // A short "blip" that slides from one pitch to another and fades out
    private static AudioClip Tone(float startFrequency, float endFrequency, float duration, float volume)
    {
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        WriteTone(samples, 0, samples.Length, startFrequency, endFrequency, volume);
        return MakeClip(samples);
    }

    // Several quick notes one after another
    private static AudioClip Arpeggio(float[] frequencies, float noteLength, float volume)
    {
        int noteSamples = Mathf.CeilToInt(SampleRate * noteLength);
        float[] samples = new float[noteSamples * frequencies.Length];
        for (int i = 0; i < frequencies.Length; i++)
        {
            WriteTone(samples, i * noteSamples, noteSamples, frequencies[i], frequencies[i], volume);
        }
        return MakeClip(samples);
    }

    private static void WriteTone(float[] samples, int start, int count, float startFrequency, float endFrequency, float volume)
    {
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            phase += 2f * Mathf.PI * Mathf.Lerp(startFrequency, endFrequency, t) / SampleRate;

            // Square-ish wave for a retro sound, with a quick fade in and a fade out
            float wave = Mathf.Clamp(Mathf.Sin(phase) * 3f, -1f, 1f);
            float envelope = (1f - t) * (1f - t) * Mathf.Min(1f, i / (SampleRate * 0.004f));
            samples[start + i] = wave * envelope * volume;
        }
    }

    private static AudioClip MakeClip(float[] samples)
    {
        AudioClip clip = AudioClip.Create("Sound", samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
