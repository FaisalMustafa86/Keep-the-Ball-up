using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Tools > Keep the Ball Up > Build Android APK: builds an installable APK into Builds/Android.
// Can also run without opening the Editor:
//   Unity -batchmode -quit -projectPath <project> -executeMethod AndroidBuilder.BuildApk
public static class AndroidBuilder
{
    // Also used as the iOS bundle ID
    public const string PackageName = "com.faisalmustafa.keeptheballup";
    private const string OutputPath = "Builds/Android/KeepTheBallUp.apk";

    [MenuItem("Tools/Keep the Ball Up/Build Android APK")]
    public static void BuildApk()
    {
        // Save first, so Unity doesn't stop to ask (and cancel the build if the dialog is missed)
        if (!Application.isBatchMode) SceneBuilder.SaveScenes();

        // The project only had a placeholder ID, which isn't a valid Android package name
        if (!PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android).StartsWith("com.faisalmustafa"))
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        }

        // A plain APK to install directly (not an .aab for the Play Store), portrait only,
        // for both 64-bit and older 32-bit phones
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        bool succeeded = summary.result == BuildResult.Succeeded;

        if (succeeded)
        {
            Debug.Log($"[AndroidBuilder] APK built: {Path.GetFullPath(OutputPath)} ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalMinutes:F1} min)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(OutputPath);
        }
        else
        {
            Debug.LogError($"[AndroidBuilder] Build {summary.result} with {summary.totalErrors} error(s). See the Console for details.");
        }

        if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
    }
}
