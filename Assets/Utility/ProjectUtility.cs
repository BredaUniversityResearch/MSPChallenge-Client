#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class ProjectUtility : Editor
{


	public delegate void LoopMethods();
	public delegate void DelegateCallbacks();

	#region LoopThroughScenes without callback

	/// <summary>
	/// Loops through scenes and runs methods.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.(</param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods)
	{
		LoopMethods loopMethods = null;

		for (int i = 0; i < methods.Length; i++)
			loopMethods += methods[i];

		if (scenePaths != null && loopMethods != null)
		{
			for (int i = 0; i < scenePaths.Length; i++)
			{
				EditorSceneManager.OpenScene(scenePaths[i]);
				loopMethods();
			}
		}
		else
			Debug.Log("Either no scenes or functions to loop through");
	}

	/// <summary>
	/// Loops through scenes and runs methods after asking for confirmation.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.(</param>
	/// <param name="confirmationCheck">Show a confirmation dialogue.</param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods, bool confirmationCheck)
	{
		if (ConfirmationCheck())
		{
			LoopMethods loopMethods = null;

			for (int i = 0; i < methods.Length; i++)
				loopMethods += methods[i];

			if (scenePaths != null && loopMethods != null)
			{
				for (int i = 0; i < scenePaths.Length; i++)
				{
					EditorSceneManager.OpenScene(scenePaths[i]);
					loopMethods();
				}
			}
			else
				Debug.Log("Either no scenes or functions to loop through");
		}
	}

	/// <summary>
	/// Loops through scenes and runs methods after asking for confirmation and show progress bar.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.(</param>
	/// <param name="confirmationCheck">Show a confirmation dialogue.</param>
	/// <param name="progressBar">Show a progress bar.</param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods, bool confirmationCheck, bool progressBar)
	{
		if (ConfirmationCheck())
		{
			LoopMethods loopMethods = null;

			for (int i = 0; i < methods.Length; i++)
				loopMethods += methods[i];

			if (scenePaths != null && loopMethods != null)
			{
				for (int i = 0; i < scenePaths.Length; i++)
				{
					if (progressBar)
					{
						if (UpdateProgress((1f / scenePaths.Length) * (i + 1f), "Opening scene: " + scenePaths[i].ToString()))
						{
							Debug.Log("Stopped process");
							return;
						}
					}
					EditorSceneManager.OpenScene(scenePaths[i]);
					loopMethods();
				}
			}
			else
				Debug.Log("Either no scenes or methods to loop through");
		}
	}

	#endregion

	#region LoopThroughScenes with callback

	/// <summary>
	/// 
	/// Loops through scenes, runs methods, and finally runs callback methods.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.</param>
	/// <param name="callback">Pass callback methods in an array as a delegate. Example: { () => FixDialogueLinks() }. </param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods, DelegateCallbacks[] callback)
	{
		LoopMethods loopMethods = null;

		for (int i = 0; i < methods.Length; i++)
			loopMethods += methods[i];

		if (scenePaths != null && loopMethods != null)
		{
			for (int i = 0; i < scenePaths.Length; i++)
			{
				EditorSceneManager.OpenScene(scenePaths[i]);
				loopMethods();
			}
			Callback(callback);
		}
		else
			Debug.Log("Either no scenes or functions to loop through");
	}

	/// <summary>
	/// Loops through scenes, runs methods, and finally runs callback methods after asking for confirmation.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.(</param>
	/// <param name="confirmationCheck">Show a confirmation dialogue.</param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods, DelegateCallbacks[] callback, bool confirmationCheck)
	{
		if (ConfirmationCheck())
		{
			LoopMethods loopMethods = null;

			for (int i = 0; i < methods.Length; i++)
				loopMethods += methods[i];

			if (scenePaths != null && loopMethods != null)
			{
				for (int i = 0; i < scenePaths.Length; i++)
				{
					EditorSceneManager.OpenScene(scenePaths[i]);
					loopMethods();
				}
				Callback(callback);
			}
			else
				Debug.Log("Either no scenes or functions to loop through");
		}
	}

	/// <summary>
	/// Loops through scenes, runs methods, and finally runs callback methods after asking for confirmation.
	/// </summary>
	/// <param name="scenePaths">Pass scene path relative to Assets folder.</param>
	/// <param name="methods">Pass methods in an array as a delegate. Example: { () => FixDialogueLinks() }.(</param>
	/// <param name="confirmationCheck">Show a confirmation dialogue.</param>
	public static void LoopThroughScenes(string[] scenePaths, LoopMethods[] methods, DelegateCallbacks[] callback, bool confirmationCheck, bool progressBar)
	{
		if (ConfirmationCheck())
		{
			LoopMethods loopMethods = null;

			for (int i = 0; i < methods.Length; i++)
				loopMethods += methods[i];

			if (scenePaths != null && loopMethods != null)
			{
				for (int i = 0; i < scenePaths.Length; i++)
				{
					if (progressBar)
					{
						if (UpdateProgress((1f / scenePaths.Length) * (i + 1f), "Opening scene: " + scenePaths[i].ToString()))
						{
							Debug.Log("Stopped process");
							return;
						}
					}
					EditorSceneManager.OpenScene(scenePaths[i]);
					loopMethods();
				}
				Callback(callback);
			}
			else
				Debug.Log("Either no scenes or functions to loop through");
		}
	}



	public static void Callback(DelegateCallbacks[] callback)
	{
		DelegateCallbacks delegateCallbacks = null;

		for (int i = 0; i < callback.Length; i++)
			delegateCallbacks += callback[i];

		if (delegateCallbacks != null)
			delegateCallbacks();
		else
			Debug.Log("Either no scenes or methods to loop through");
	}

	#endregion

	#region LoopThroughScenes Confirmation and Progress Display

	/// <summary>
	/// Show a confirmation dialogue.
	/// </summary>
	public static bool ConfirmationCheck()
	{
		if (EditorUtility.DisplayDialog("Are you sure?", "This will run through all story scenes and apply the given methods.", "Yes, I'm sure", "No, I changed my mind."))
			return true;
		else
			return false;
	}

	/// <summary>
	/// Show a progress bar and update when it's called.
	/// </summary>
	public static bool UpdateProgress(float progress, string status)
	{
		if (progress < 1f)
		{
			if (EditorUtility.DisplayCancelableProgressBar("Fixing your shit. Please wait...", status, progress))
			{
				EditorUtility.ClearProgressBar();
				return true;
			}
		}
		else
			EditorUtility.ClearProgressBar();

		return false;
	}

	#endregion

	#region Utility

	/// <summary>
	/// Return scene paths of all scenes in given folder
	/// </summary>
	/// <param name="folderPath">Folder path containing scenes</param>
	public static string[] GetScenesAtPath(string folderPath)
	{
		// Create a list to story scene paths in
		List<string> scenes = new List<string>();
		// Get directory
		DirectoryInfo dir = new DirectoryInfo(folderPath);
		// Get files from directory
		FileInfo[] info = dir.GetFiles("*.*");
		// Loop through scene paths and add filepaths not containing '.meta' and are relative to the Assets directory
		for (int i = 0; i < info.Length; i++)
		{
			if (!info[i].Name.EndsWith(".meta"))
			{
				string relativeFilePath = info[i].FullName.Substring(info[i].FullName.IndexOf("Assets"));
				scenes.Add(relativeFilePath);
			}
		}
		return scenes.ToArray();
	}
	#endregion

	#region LoadAdditives

	public static void LoadAdditives()
	{
		//foreach (LoadAdditive loadAdditive in FindObjectsOfType<LoadAdditive>()) {
		//    if (loadAdditive.enabled) {
		//        string sceneName = "Assets/Scenes/Source/" + loadAdditive.setSceneToLoad + ".unity";
		//        EditorApplication.OpenSceneAdditive(sceneName);
		//    }
		//}
	}

	public static void DestroyAdditives()
	{
		//foreach (LoadAdditive loadAdditive in FindObjectsOfType<LoadAdditive>()) {
		//    if (loadAdditive.enabled) {
		//        DestroyImmediate(GameObject.Find(loadAdditive.setSceneToLoad));
		//    }
		//}
	}

	#endregion

}
#endif
