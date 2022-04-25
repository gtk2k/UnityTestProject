using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public static class BuildScript
{
    private class BuildConfig
    {
        public string outputDir;
    }

    private class BuildResult
    {
        public string Android = "None";
        public string Windows = "None";
        public string Linux = "None";
        public string WebGL = "None";
        public string ProductName;
    }

    public static void MesonBuild()
    {
        var paths = GetBuildScenePaths();
        var buildResult = new BuildResult();

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        var buildConfigPath = Path.Combine(documentsPath, "buildConfig.json");
        var buildResultPath = Path.Combine(documentsPath, "buildResult.json");
        var configJson = File.ReadAllText(buildConfigPath);
        var config = JsonUtility.FromJson<BuildConfig>(configJson);
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.scenes = paths.ToArray();

        Debug.Log($"Documents Folder Path > {documentsPath}");
        Debug.Log($"buildConfigPath > {buildConfigPath}");
        Debug.Log($"configJson > {configJson}");
        Debug.Log($"Output Directory > {config.outputDir}");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        buildResult.Android = platformBuild(buildPlayerOptions, BuildTarget.Android, config.outputDir, ".apk");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        buildResult.Windows = platformBuild(buildPlayerOptions, BuildTarget.StandaloneWindows64, config.outputDir, ".exe");
        buildResult.WebGL = platformBuild(buildPlayerOptions, BuildTarget.WebGL, config.outputDir, "");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        buildResult.Linux = platformBuild(buildPlayerOptions, BuildTarget.StandaloneLinux64, config.outputDir, ".x86_x64");

        buildResult.ProductName = PlayerSettings.productName;

        File.WriteAllText(buildResultPath, JsonUtility.ToJson(buildResult, true));
    }

    private static string platformBuild(BuildPlayerOptions buildPlayerOptions, BuildTarget target, string locationPathName, string ext)
    {
        var platformName = "";
        switch (target)
        {
            case BuildTarget.Android: platformName = "Android"; break;
            case BuildTarget.StandaloneWindows64: platformName = "Windows"; break;
            case BuildTarget.WebGL: platformName = "WebGL"; break;
            case BuildTarget.StandaloneLinux64: platformName = "Linux"; break;
        }
        buildPlayerOptions.locationPathName = Path.Combine(locationPathName, $@"{platformName}\{PlayerSettings.productName}{ext}");
        buildPlayerOptions.target = target;
        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"{platformName} Build Success");
            return "Success";
        }
        else
        {
            Debug.LogError($"{platformName} Build Fail");
            return "Fail";
        }
    }

    private static IEnumerable<string> GetBuildScenePaths()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        return scenes
            .Where((arg) => arg.enabled)
            .Select((arg) => arg.path);
    }
}