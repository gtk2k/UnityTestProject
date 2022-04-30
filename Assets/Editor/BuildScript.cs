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

        public string AndroidBuildStartTime = "None";
        public string iOSBuildStartTime = "None";
        public string WebGLBuildStartTime = "None";
        public string WindowsBuildStartTime = "None";
        public string LinuxBuildStartTime = "None";
        public string OSXBuildStartTime = "None";

        public string AndroidBuildEndTime = "None";
        public string iOSBuildEndTime = "None";
        public string WebGLBuildEndTime = "None";
        public string WindowsBuildEndTime = "None";
        public string LinuxBuildEndTime = "None";
        public string OSXBuildEndTime = "None";

        public double AndroidBuildTotalSeconds = 0;
        public double iOSBuildTotalSeconds = 0;
        public double WebGLBuildTotalSeconds = 0;
        public double WindowsBuildTotalSeconds = 0;
        public double LinuxBuildTotalSeconds = 0;
        public double OSXBuildTotalSeconds = 0;

        public string pushId = "None";
        public string repositoryName = "None";
        public string branchName = "None";
        public string ProductName = "None";
    }

    public static async void MesonBuild()
    {
        var args = System.Environment.GetCommandLineArgs().ToList();
        var i = args.FindIndex((arg) => arg == "-outputDir");
        var outputDir = args[i + 1];
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
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.scenes = paths.ToArray();

        Debug.Log($"Documents Folder Path > {documentsPath}");
        Debug.Log($"buildConfigPath > {buildConfigPath}");
        Debug.Log($"Output Directory > {outputDir}");

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        await platformBuild(buildPlayerOptions, BuildTarget.Android, buildSteps, outputDir, ".apk");

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        await platformBuild(buildPlayerOptions, BuildTarget.StandaloneWindows64, buildSteps, outputDir, ".exe");

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        //PlayerSettings.WebGL.template = "PROJECT:Better2020";
        await platformBuild(buildPlayerOptions, BuildTarget.WebGL, buildSteps, outputDir, "");

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        await platformBuild(buildPlayerOptions, BuildTarget.StandaloneLinux64, buildSteps, outputDir, ".x86_x64");

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

        BuildStartTimeUpdate(buildSteps, buildTarget, DateTime.Now.ToString("o"));
        BuildStepsUpdate(buildSteps, buildTarget, "Building");
        await SendBuildSteps(buildSteps);

        buildPlayerOptions.locationPathName = Path.Combine(locationPathName, $@"{platformName}\{PlayerSettings.productName}{ext}");
        buildPlayerOptions.target = buildTarget;
        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var result = buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? "Successed" : "Failed";
        Debug.LogError($"{platformName} Build {result}");
        
        if (result == "Successed")
        {
            BuildEndTimeUpdate(buildSteps, buildTarget, DateTime.Now.ToString("o"));
            BuildStepsUpdate(buildSteps, buildTarget, "Zipping");
            await SendBuildSteps(buildSteps);

            var zipFilePath = Path.Combine(locationPathName, platformName);
            ZipFile.CreateFromDirectory(zipFilePath, $"{zipFilePath}.zip");

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
    private static void BuildStartTimeUpdate(BuildSteps buildSteps, BuildTarget buildTarget, string startTime)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                buildSteps.AndroidBuildStartTime = startTime;
                break;
            case BuildTarget.iOS:
                buildSteps.iOSBuildStartTime = startTime;
                break;
            case BuildTarget.WebGL:
                buildSteps.WebGLBuildStartTime = startTime;
                break;
            case BuildTarget.StandaloneWindows64:
                buildSteps.WindowsBuildStartTime = startTime;
                break;
            case BuildTarget.StandaloneOSX:
                buildSteps.OSXBuildStartTime = startTime;
                break;
            case BuildTarget.StandaloneLinux64:
                buildSteps.LinuxBuildStartTime = startTime;
                break;
        }
    }

    private static void BuildEndTimeUpdate(BuildSteps buildSteps, BuildTarget buildTarget, string endTime)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                buildSteps.AndroidBuildEndTime = endTime;
                buildSteps.AndroidBuildTotalSeconds =  (int)(DateTime.Parse(endTime)- DateTime.Parse(buildSteps.AndroidBuildStartTime)).TotalSeconds;
                break;
            case BuildTarget.iOS:
                buildSteps.iOSBuildEndTime = endTime;
                buildSteps.iOSBuildTotalSeconds = (int)(DateTime.Parse(endTime) - DateTime.Parse(buildSteps.iOSBuildStartTime)).TotalSeconds;
                break;
            case BuildTarget.WebGL:
                buildSteps.WebGLBuildEndTime = endTime;
                buildSteps.WebGLBuildTotalSeconds = (int)(DateTime.Parse(endTime) - DateTime.Parse(buildSteps.WebGLBuildStartTime)).TotalSeconds;
                break;
            case BuildTarget.StandaloneWindows64:
                buildSteps.WindowsBuildEndTime = endTime;
                buildSteps.WindowsBuildTotalSeconds = (int)(DateTime.Parse(endTime) - DateTime.Parse(buildSteps.WindowsBuildStartTime)).TotalSeconds;
                break;
            case BuildTarget.StandaloneOSX:
                buildSteps.OSXBuildEndTime = endTime;
                buildSteps.OSXBuildTotalSeconds = (int)(DateTime.Parse(endTime) - DateTime.Parse(buildSteps.OSXBuildStartTime)).TotalSeconds;
                break;
            case BuildTarget.StandaloneLinux64:
                buildSteps.LinuxBuildEndTime = endTime;
                buildSteps.LinuxBuildTotalSeconds = (int)(DateTime.Parse(endTime) - DateTime.Parse(buildSteps.LinuxBuildStartTime)).TotalSeconds;
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