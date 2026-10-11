using UnityEngine;

// Player settings and best score, saved on the device between sessions.
public static class Settings
{
    // Sound effects (bounces, clicks...)
    public static bool SoundOn
    {
        get => PlayerPrefs.GetInt("Sound", 1) == 1;
        set => SetBool("Sound", value);
    }

    // Background music
    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt("Music", 1) == 1;
        set => SetBool("Music", value);
    }

    public static bool VibrationOn
    {
        get => PlayerPrefs.GetInt("Vibration", 1) == 1;
        set => SetBool("Vibration", value);
    }

    // Difficulty mode picked on the menu
    public static GameMode Mode
    {
        get => (GameMode)Mathf.Clamp(PlayerPrefs.GetInt("Mode", (int)GameMode.Normal), 0, Difficulty.All.Length - 1);
        set
        {
            PlayerPrefs.SetInt("Mode", (int)value);
            PlayerPrefs.Save();
        }
    }

    // Best score in the current mode
    public static int BestScore
    {
        get => PlayerPrefs.GetInt(BestScoreKey(Mode), 0);
        set
        {
            PlayerPrefs.SetInt(BestScoreKey(Mode), value);
            PlayerPrefs.Save();
        }
    }

    // Normal keeps the original key, so best scores from before modes existed aren't lost
    private static string BestScoreKey(GameMode mode) => mode == GameMode.Normal ? "BestScore" : "BestScore" + mode;

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
