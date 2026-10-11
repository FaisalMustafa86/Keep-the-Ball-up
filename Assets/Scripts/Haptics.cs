using UnityEngine;

// Phone vibration. On Android, short "taps" are used for hits and a longer buzz for game over.
// iOS only offers one fixed vibration without a native plugin, so there it's used for game over only.
public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass vibrationEffect;
    private static bool initialised;
    private static int sdkVersion;

    private static void Init()
    {
        initialised = true;
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                sdkVersion = version.GetStatic<int>("SDK_INT");
            }
            if (sdkVersion >= 26) vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Vibration not available: " + e.Message);
            vibrator = null;
        }
    }

    private static bool VibrateAndroid(long milliseconds, int amplitude)
    {
        if (!initialised) Init();
        if (vibrator == null) return false;

        try
        {
            if (vibrationEffect != null)
            {
                using (AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
            return true;
        }
        catch (System.Exception)
        {
            // Some devices don't allow vibration; just skip it
            return false;
        }
    }
#endif

    // Tiny tap, for hitting the ball
    public static void Light()
    {
        if (!Settings.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(12, 60);
#endif
    }

    // Stronger tap, for perfect hits and level ups
    public static void Medium()
    {
        if (!Settings.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(25, 160);
#endif
    }

    // Long buzz, for game over
    public static void Heavy()
    {
        if (!Settings.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        // Handheld.Vibrate is also what makes Unity add the VIBRATE permission to the Android build
        if (!VibrateAndroid(250, 255)) Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
