using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

class ProjectBuilder
{
    [MenuItem("MSP 2050/CI/Build Win64")]
    public static void BuildWin64()
    {
        //PreBuild();

        List<string> scenes = FindEnabledEditorScenes();
        GenericBuild(scenes.ToArray(), "Builds/Win64/MSP2050/MSP2050.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);

        scenes.Insert(0, "Assets/UnitTestScene.unity");
        GenericBuild(scenes.ToArray(), "Builds/Win64/MSP2050_Test/MSP2050_Test.exe", BuildTarget.StandaloneWindows64, BuildOptions.AllowDebugging | BuildOptions.ForceEnableAssertions);
    }

    private static List<string> FindEnabledEditorScenes()
    {
        List<string> editorScenes = new List<string>(EditorBuildSettings.scenes.Length);
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            editorScenes.Add(scene.path);
        }
        return editorScenes;
    }

    private static void GenericBuild(string[] scenes, string target_dir, BuildTarget build_target, BuildOptions build_options)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, build_target);
        BuildReport res = BuildPipeline.BuildPlayer(scenes, target_dir, build_target, build_options);
        if (res.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("BuildPlayer failure: " + res);
        }
    }
	
	private static void WindowsDevBuilder()
	{
        PreBuild();
		var outputDir = GetArg("-customBuildPath");
		BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputDir, BuildTarget.StandaloneWindows64, BuildOptions.Development);
	}

    private static void MacOSDevBuilder()
    {
        PreBuild();
        var outputDir = GetArg("-customBuildPath");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputDir, BuildTarget.StandaloneOSX, BuildOptions.Development);
    }

    private static void WindowsBuilder()
    {
        PreBuild();
        var outputDir = GetArg("-customBuildPath");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputDir, BuildTarget.StandaloneWindows64, 0);
    }

    private static void MacOSBuilder()
    {
        PreBuild();
        var outputDir = GetArg("-customBuildPath");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputDir, BuildTarget.StandaloneOSX, 0);
    }

    [MenuItem("MSP 2050/Build project")]
    public static void MyBuild()
    {
        //PreBuild();

        //Build a dev and non-dev player
        string path = EditorUtility.SaveFolderPanel("Choose folder to build game", "", "");
        if (path.Length != 0)
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path + "/Windows/msp.exe", EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path + "/Windows_Dev/msp_dev.exe", EditorUserBuildSettings.activeBuildTarget, BuildOptions.Development);
        }
    }
	
	private static string GetArg(string name)
	{
		var args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == name && args.Length > i + 1)
			{
				return args[i + 1];
            }
		}
		return null;
	}

    private static void PreBuild()
    {
        UnityManifest manifest = UnityManifest.Load();
        ApplicationBuildIdentifier.UpdateBuildInformation(manifest);
    }
}
