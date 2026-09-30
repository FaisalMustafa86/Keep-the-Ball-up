using UnityEngine;

// Player settings and best score, saved on the device between sessions.
public static class Settings
{
    public static bool SoundOn
    {
        get => PlayerPrefs.GetInt("Sound", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("Sound", value ? 1 : 0);
            PlayerPrefs.Save();
            ApplySound();
        }
    }

    public static bool VibrationOn
    {
        get => PlayerPrefs.GetInt("Vibration", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("Vibration", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int BestScore
    {
        get => PlayerPrefs.GetInt("BestScore", 0);
        set
        {
            PlayerPrefs.SetInt("BestScore", value);
            PlayerPrefs.Save();
        }
    }

    public static void ApplySound()
    {
        AudioListener.volume = SoundOn ? 1f : 0f;
    }

    public static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (VibrationOn) Handheld.Vibrate();
#endif
    }
}
