using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Tools > Keep the Ball Up > Build iOS (Xcode project): exports an Xcode project into Builds/iOS.
// Unity can't make the final iPhone app itself: copy the folder to a Mac, open Unity-iPhone.xcodeproj in Xcode,
// pick your Apple ID under Signing & Capabilities, and press Run with an iPhone connected.
// Can also run without opening the Editor:
//   Unity -batchmode -quit -projectPath <project> -executeMethod IOSBuilder.BuildXcodeProject
public static class IOSBuilder
{
    private const string OutputPath = "Builds/iOS";
    private const string MinimumIOSVersion = "13.0";

    [MenuItem("Tools/Keep the Ball Up/Build iOS (Xcode project)")]
    public static void BuildXcodeProject()
    {
        // Save first, so Unity doesn't stop to ask (and cancel the build if the dialog is missed)
        if (!Application.isBatchMode) SceneBuilder.SaveScenes();

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("[IOSBuilder] iOS Build Support isn't loaded. If you just installed it, restart Unity.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        if (!PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS).StartsWith("com.faisalmustafa"))
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AndroidBuilder.PackageName);
        }

        // Portrait only, 64-bit iPhones running iOS 13 or newer
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.iOS.targetOSVersionString = MinimumIOSVersion;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        bool succeeded = summary.result == BuildResult.Succeeded;

        if (succeeded)
        {
            Debug.Log($"[IOSBuilder] Xcode project exported to {Path.GetFullPath(OutputPath)}. Open Unity-iPhone.xcodeproj in Xcode on a Mac to build it.");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(OutputPath);
        }
        else
        {
            Debug.LogError($"[IOSBuilder] Build {summary.result} with {summary.totalErrors} error(s). See the Console for details.");
        }

        if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
    }
}
