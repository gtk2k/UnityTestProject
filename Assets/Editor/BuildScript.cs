using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;

public static class BuildScript
{
    private class BuildConfig
    {
        public string outputDir;
    }

    private class BuildSteps
    {
        public string type = "build_steps";
        public string Android = "None";
        public string iOS = "None";
        public string WebGL = "None";
        public string Windows = "None";
        public string Linux = "None";
        public string OSX = "None";
        public string ProductName = "None";
    }

    public static async void MesonBuild()
    {
        var paths = GetBuildScenePaths();
        var buildSteps = new BuildSteps();

        buildSteps.Android = "Waiting";
        buildSteps.WebGL = "Waiting";
        buildSteps.Windows = "Waiting";
        buildSteps.Linux = "Waiting";
        buildSteps.ProductName = PlayerSettings.productName;

        await SendBuildSteps(buildSteps);

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
        await platformBuild(buildPlayerOptions, BuildTarget.Android, buildSteps, config.outputDir, ".apk");

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        await platformBuild(buildPlayerOptions, BuildTarget.StandaloneWindows64, buildSteps, config.outputDir, ".exe");
        
        await platformBuild(buildPlayerOptions, BuildTarget.WebGL,  buildSteps, config.outputDir, "");

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        await platformBuild(buildPlayerOptions, BuildTarget.StandaloneLinux64, buildSteps, config.outputDir, ".x86_x64");

        File.WriteAllText(buildResultPath, JsonUtility.ToJson(buildSteps, true));
    }

    private static async Task platformBuild(BuildPlayerOptions buildPlayerOptions, BuildTarget buildTarget, BuildSteps buildSteps, string locationPathName, string ext)
    {
        // TODO Apple Silicon Mac 
        var platformName =
            buildTarget == BuildTarget.Android ? "Android" :
            buildTarget == BuildTarget.iOS ? "iOS" :
            buildTarget == BuildTarget.WebGL ? "WebGL" :
            buildTarget == BuildTarget.StandaloneWindows64 ? "Windows" :
            buildTarget == BuildTarget.StandaloneOSX ? "OSX" :
            buildTarget == BuildTarget.StandaloneLinux64 ? "Linux" : "";

        if (platformName == "") return;

        BuildStepsUpdate(buildSteps, buildTarget, "Building");
        await SendBuildSteps(buildSteps);

        buildPlayerOptions.locationPathName = Path.Combine(locationPathName, $@"{platformName}\{PlayerSettings.productName}{ext}");
        buildPlayerOptions.target = buildTarget;
        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var result = buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? "Successed" : "Failed";
        Debug.LogError($"{platformName} Build {result}");

        if (result == "Successed")
        {
            BuildStepsUpdate(buildSteps, buildTarget, "Zipping");
            await SendBuildSteps(buildSteps);

            ZipFile.CreateFromDirectory(
            locationPathName,
            @"C:\Temp\myzip1.zip");

            BuildStepsUpdate(buildSteps, buildTarget, "Successed");
            await SendBuildSteps(buildSteps);
        }
        else
        {
            BuildStepsUpdate(buildSteps, buildTarget, "Failed");
            await SendBuildSteps(buildSteps);
        }
    }

    private static void BuildStepsUpdate(BuildSteps buildSteps, BuildTarget buildTarget, string step)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                buildSteps.Android = step;
                break;
            case BuildTarget.iOS:
                buildSteps.iOS = step;
                break;
            case BuildTarget.WebGL:
                buildSteps.WebGL = step;
                break;
            case BuildTarget.StandaloneWindows64:
                buildSteps.Windows = step;
                break;
            case BuildTarget.StandaloneOSX:
                buildSteps.OSX = step;
                break;
            case BuildTarget.StandaloneLinux64:
                buildSteps.Linux = step;
                break;
        }
    }

    private static async Task<string> SendBuildSteps(BuildSteps buildResult)
    {
        var msg = JsonUtility.ToJson(buildResult);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://mesonwebdev.tk/buildSteps");
        request.Content = new StringContent(msg, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        var res = await response.Content.ReadAsStringAsync();
        return res;
    }

    private static IEnumerable<string> GetBuildScenePaths()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        return scenes
            .Where((arg) => arg.enabled)
            .Select((arg) => arg.path);
    }
}