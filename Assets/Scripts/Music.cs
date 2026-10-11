using UnityEngine;

// Background music. Plays the clip on the Music object, or Assets/Resources/Music/Background (any .wav/.mp3/.ogg with that name),
// gets louder and slightly faster as the levels go up, and sounds muffled while paused or on game over.
public class Music : MonoBehaviour
{
    public enum Mood { Menu, Playing, Muffled }

    private const float MenuVolume = 0.45f;
    private const float PlayingVolume = 0.6f;
    private const float MuffledVolume = 0.3f;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private Mood mood = Mood.Menu;
    private float targetPitch = 1f;

    public void Init()
    {
        // The low-pass filter needs its own object so it only affects the music, not the sound effects
        if (SceneObjects.GetOrCreate(transform, "Music", out source))
        {
            source.loop = true;
            source.playOnAwake = false;
            source.priority = 0;
        }
        lowPass = SceneObjects.GetOrAdd<AudioLowPassFilter>(source.gameObject);
        lowPass.cutoffFrequency = 22000f;
        source.volume = 0f;

        if (source.clip == null) source.clip = Resources.Load<AudioClip>("Music/Background");
        if (source.clip == null)
        {
            Debug.LogWarning("No music found at Assets/Resources/Music/Background");
            source = null;
            return;
        }

        if (Application.isPlaying) source.Play();
    }

    public void SetMood(Mood newMood)
    {
        mood = newMood;
    }

    // Music speeds up a touch each level to add tension
    public void SetLevel(int level)
    {
        targetPitch = 1f + Mathf.Min(level - 1, 6) * 0.01f;
    }

    void Update()
    {
        if (source == null) return;

        float targetVolume = !Settings.MusicOn ? 0f
                           : mood == Mood.Menu ? MenuVolume
                           : mood == Mood.Playing ? PlayingVolume
                           : MuffledVolume;
        float targetCutoff = mood == Mood.Muffled ? 700f : 22000f;

        // Smooth fades, using unscaled time so they still work while the game is paused
        float dt = Time.unscaledDeltaTime;
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, dt * 0.8f);
        source.pitch = Mathf.MoveTowards(source.pitch, targetPitch, dt * 0.05f);
        lowPass.cutoffFrequency = Mathf.Lerp(lowPass.cutoffFrequency, targetCutoff, 1f - Mathf.Exp(-dt * 8f));
    }
}
