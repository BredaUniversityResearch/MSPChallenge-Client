using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

class ProjectBuilder
{
    [MenuItem("MSP 2050/CI/Build Win64")]
    public static void BuildWin64()
    {
		PreBuild();

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

	[MenuItem("MSP 2050/Build project")]
	public static void MyBuild()
	{
		PreBuild();

		//Build a dev and non-dev player
		string path = EditorUtility.SaveFolderPanel("Choose folder to build game", "", "");
		if (path.Length != 0)
		{
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path + "/msp.exe", EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path + "/DevBuild/msp_dev.exe", EditorUserBuildSettings.activeBuildTarget, BuildOptions.Development);
		}
	}

	private static void PreBuild()
	{
		//Put build date into the game
		ApplicationBuildIdentifier buildIdentifier = ApplicationBuildIdentifier.FindBuildIdentifier();
		if (buildIdentifier != null)
		{
			buildIdentifier.UpdateBuildTime();
			EditorUtility.SetDirty(buildIdentifier);

			GLog.Instance.gameVersion = "Rev " + buildIdentifier.GetSvnRevisionNumber();
			EditorUtility.SetDirty(GLog.Instance);
			AssetDatabase.SaveAssets();
		}
	}
}
