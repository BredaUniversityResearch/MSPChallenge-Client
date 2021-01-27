//using UnityEditor;
//using UnityEngine;
//using System.IO;
//using System.Collections.Generic;

//public class CaptureCams : EditorWindow
//{

//	#region Initialize

//	static private string root = "CaptureCams";
//	static private string shotFolder;
//	static private Camera captureCam;

//	// Resolution modes
//	private enum SETTINGS
//	{
//		AnyResolution = 0,
//		CustomResolution = 1
//	}
//	static private SETTINGS resolutionSettings;

//	// When using AnyResolution 
//	static public int resolutionMultiplier = 1;

//	// When using CustomResolution
//	static public int resWidth = 1920;
//	static public int resHeight = 1080;
//	static public int resFactor = 5;

//	#endregion

//	#region Menu: Capture Scene Cameras

//	[MenuItem("Tools/Capture/Scene Cameras %#l")]
//	static void CaptureScene()
//	{
//		shotFolder = "Test Captures";
//		Capture(GetCams);
//	}

//	#endregion

//	#region Menu: Capture Project Cameras

//	[MenuItem("Tools/Capture/Project Cameras")]
//	static void CaptureAllScenes()
//	{
//		if (EditorUtility.DisplayDialog(
//				"Capture Project Cams",
//				"You are about to capture the Achievement and Scene Thumbnail cameras found in all Story scenes.\n\nCapture process may take a while. Please consult Unity's app title for a progress update.",
//				"Go for it!",
//				"Rather not"))
//		{

//			// Console message asking for patience
//			Debug.Log("C");

//			shotFolder = "Project Captures";
//			string[] sceneFiles = Directory.GetFiles("Assets/Scenes/Story/");
//			// string[] sceneFiles = Directory.GetFiles("Assets/Scenes/Tests scenes/CaptureCam"); // Test scene

//			List<string> scenePaths = new List<string>();
//			for (int i = 0; i < sceneFiles.Length; i++)
//			{
//				if (!sceneFiles[i].Contains("meta"))
//					scenePaths.Add(sceneFiles[i]);
//			}

//			// List of functions
//			List<ProjectUtility.LoopMethods> methods = new List<ProjectUtility.LoopMethods>();
//			methods.Add(() => LoadAdditiveIfPresent());
//			methods.Add(() => EnableCaptureCams(true));
//			methods.Add(() => CaptureAll(GetCams));
//			methods.Add(() => EnableCaptureCams(false));

//			// List of callbacks
//			List<ProjectUtility.DelegateCallbacks> callbacks = new List<ProjectUtility.DelegateCallbacks>();
//			callbacks.Add(() => NewScene());
//			callbacks.Add(() => Done());

//			// Delegate looping functions through scenes with a callback loop
//			ProjectUtility.LoopThroughScenes(scenePaths.ToArray(), methods.ToArray(), callbacks.ToArray());
//		}
//		else
//			return;
//	}

//	#endregion

//	#region Capture Checks

//	static void Capture(Camera[][] cams)
//	{
//		if (Selection.gameObjects.Length > 0)
//		{
//			CaptureSelection(cams);
//		}
//		else if (cams[0].Length > 0 || cams[1].Length > 0)
//		{
//			CaptureAll(cams);
//		}
//		else if (cams[0].Length == 0 && cams[1].Length == 0)
//			Debug.Log("No CaptureCam in " + SceneName);
//	}

//	static void CaptureSelection(Camera[][] cams)
//	{
//		for (int i = 0; i < Selection.gameObjects.Length; i++)
//		{
//			for (int x = 0; x < cams.Length; x++)
//			{
//				for (int y = 0; y < cams[x].Length; y++)
//				{
//					if (Selection.gameObjects[i].name == cams[x][y].name)
//					{
//						List<Camera> camSelection = new List<Camera>();
//						camSelection.Add(cams[x][y]);
//						for (int c = 0; c < camSelection.Count; c++)
//						{
//							string shotPath = ShotPath(shotFolder, camSelection[c]);
//							TakePicture(camSelection[c], shotPath);
//						}
//					}
//				}
//			}
//		}
//	}

//	static void CaptureAll(Camera[][] cams)
//	{
//		cams = DisableSceneCams(cams);
//		for (int x = 0; x < cams.Length; x++)
//		{
//			for (int y = 0; y < cams[x].Length; y++)
//			{
//				string shotPath = ShotPath(shotFolder, cams[x][y]);
//				cams[x][y].gameObject.SetActive(true);
//				TakePicture(cams[x][y], shotPath);
//				cams[x][y].gameObject.SetActive(false);
//			}
//		}
//	}

//	#endregion

//	#region Paths & Labels

//	static string ShotPath(string folderName, Camera camera)
//	{
//		string shotLabel = ShotLabel(camera);
//		string shotPath = string.Format("{0}/{1}", root, folderName);
//		Directory.CreateDirectory(shotPath);
//		return string.Format("{0}/{1}", shotPath, shotLabel);
//	}

//	static string ShotLabel(Camera cam)
//	{
//		string sceneName = SceneName;
//		string camera = cam.name;
//		string shotLabel = "";

//		if (shotFolder == "Test Captures")
//			shotLabel = string.Format("{0}_{1}_{2:D02}.png", sceneName, camera, System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"));
//		else if (shotFolder == "Project Captures")
//			shotLabel = string.Format("{0}_{1}.png", sceneName, camera);
//		else
//			return shotLabel;
//		return shotLabel;
//	}

//	#endregion

//	#region Picture Renderers

//	static void TakePicture(Camera cam, string shotPath)
//	{
//		captureCam = cam;
//		//cam = UseMainCam(cam);
//		cam.cullingMask = captureMask;
//		cam.allowHDR = true;

//		resolutionSettings = SETTINGS.CustomResolution;
//		switch (resolutionSettings)
//		{
//		case SETTINGS.AnyResolution:
//			AnyResolution(cam, shotPath);
//			break;
//		case SETTINGS.CustomResolution:
//			CustomResolution(cam, shotPath);
//			break;
//		}
//	}

//	static void AnyResolution(Camera cam, string shotPath)
//	{
//		ScreenCapture.CaptureScreenshot(shotPath, resolutionMultiplier);
//		PrintCameraName(captureCam);
//	}

//	static void CustomResolution(Camera cam, string shotPath)
//	{
//		RenderTexture rt = new RenderTexture(resWidth * resFactor, resHeight * resFactor, 24);
//		cam.targetTexture = rt;
//		Texture2D screenShot = new Texture2D(resWidth * resFactor, resHeight * resFactor, TextureFormat.RGB24, false);
//		cam.Render();
//		RenderTexture.active = rt;
//		screenShot.ReadPixels(new Rect(0, 0, resWidth * resFactor, resHeight * resFactor), 0, 0);
//		cam.targetTexture = null;
//		RenderTexture.active = null; // JC: added to avoid errors
//									 //DestroyImmediate(rt);
//		byte[] bytes = screenShot.EncodeToPNG();
//		File.WriteAllBytes(shotPath, bytes);
//		PrintCameraName(captureCam);
//	}

//	#endregion

//	#region Utility

//	static Camera[] FindCamerasByTag(string camTag)
//	{
//		List<Camera> cameras = new List<Camera>();
//		GameObject[] go = GameObject.FindGameObjectsWithTag(camTag);
//		for (int i = 0; i < go.Length; i++)
//			cameras.Add(go[i].GetComponent<Camera>());
//		return cameras.ToArray();
//	}

//	static Camera[] CombineCamArrays(Camera[] camArrayOne, Camera[] camArrayTwo)
//	{
//		List<Camera> cameras = new List<Camera>();
//		cameras.AddRange(camArrayOne);
//		cameras.AddRange(camArrayTwo);
//		return cameras.ToArray();
//	}

//	static Camera[][] Create2DCamArray(Camera[] firstArray, Camera[] secondArray)
//	{

//		Camera[][] camsArray = new Camera[2][] {
//			firstArray,
//			secondArray
//		};

//		return camsArray;

//		//// 2D Debug loop
//		//for (int x = 0; x < camsArray.Length; x++) {
//		//    for (int y = 0; y < camsArray[x].Length; y++) {
//		//        Debug.Log(camsArray[x][y].name);
//		//    }
//		//}
//	}

//	// Use Main Camera instead of Capture Camera
//	static Camera UseMainCam(Camera cam)
//	{
//		Camera mainCam = Camera.main;
//		cam = TransferCamSettings(cam, mainCam);
//		return cam;
//	}

//	// Transfer settings from Capture Camera to Main Camera
//	static Camera TransferCamSettings(Camera cam, Camera mainCam)
//	{
//		mainCam.transform.position = cam.transform.position;
//		mainCam.transform.rotation = cam.transform.rotation;
//		mainCam.transform.localScale = cam.transform.localScale;
//		mainCam.fieldOfView = cam.fieldOfView;
//		mainCam.rect = cam.rect;
//		mainCam.cullingMask = captureMask;
//		return mainCam;
//	}

//	static bool EnableCaptureCams(bool value)
//	{
//		if (GameObject.FindGameObjectWithTag("CaptureCams") != null)
//			for (int i = 0; i < GameObject.FindGameObjectWithTag("CaptureCams").transform.childCount; i++)
//				GameObject.FindGameObjectWithTag("CaptureCams").transform.GetChild(i).gameObject.SetActive(value);
//		else
//			Debug.Log("Could not find an active CaptureCams parent with proper tag.");
//		return GameObject.FindGameObjectWithTag("CaptureCams").activeSelf;
//	}

//	static Camera[][] DisableSceneCams(Camera[][] cams)
//	{
//		for (int x = 0; x < cams.Length; x++)
//		{
//			for (int y = 0; y < cams[x].Length; y++)
//			{
//				cams[x][y].gameObject.SetActive(false);
//			}
//		}
//		return cams;
//	}

//	static void PrintCameraName(Camera cam)
//	{
//		string sceneName = SceneName;
//		Debug.Log((string.Format("Captured: {0}_{1}", sceneName, cam.name)), cam);
//	}

//	static void NewScene()
//	{
//		EditorApplication.NewScene();
//	}

//	static void Done()
//	{
//		EditorApplication.Beep();
//		EditorUtility.DisplayDialog(
//			"Capture Project Cams",
//			"Capture Cams is done",
//			"Cool beans");
//	}

//	static void LoadAdditiveIfPresent()
//	{
//		//foreach (LoadAdditive loadAdditive in FindObjectsOfType<LoadAdditive>()) {
//		//    if (GameObject.Find(loadAdditive.setSceneToLoad))
//		//        return;
//		//    else
//		//        ProjectUtility.LoadAdditives();
//		//}
//	}

//	static GameObject CaptureCamParent
//	{
//		get
//		{
//			return GameObject.FindGameObjectWithTag("CaptureCams");
//		}
//	}
//	#endregion

//	#region Read & Write

//	static Camera[][] GetCams
//	{
//		get
//		{
//			Camera[] achieveCams = FindCamerasByTag("MainCamera");
//			Camera[] sceneThumbCams = FindCamerasByTag("MainCamera");

//			//// Combine camera arrays and capture
//			//Camera[] cams = CombineCamArrays(achieveCams, sceneThumbCams);

//			// Create 2D camera array and capture
//			Camera[][] cams = Create2DCamArray(achieveCams, sceneThumbCams);
//			return cams;
//		}
//	}

//	static string SceneName
//	{
//		get
//		{
//			string sceneName = EditorApplication.currentScene;
//			if (sceneName.Contains("MainMenu"))
//			{
//				sceneName = sceneName.Replace("Assets/Scenes/", "MainMenu");
//				sceneName = sceneName.Remove(sceneName.Length - 14, 14);
//			}
//			else if (sceneName.Contains("BoxArt"))
//			{
//				sceneName = sceneName.Replace("Assets/Scenes/", "BoxArt");
//				sceneName = sceneName.Remove(sceneName.Length - 14, 14);
//			}
//			else
//			{
//				sceneName = sceneName.Replace("Assets/Scenes/Story/", "s");
//				sceneName = sceneName.Remove(sceneName.Length - 6, 6);
//			}
//			// sceneName = sceneName.Replace("Assets/Scenes/Tests scenes/CaptureCam/", "s"); // Test scene
//			return sceneName;
//		}
//	}

//	// Create LayerMask and invert the combined bitmask
//	static LayerMask captureMask
//	{
//		get
//		{
//			LayerMask captureMask = ~(
//				//(1 << LayerMask.NameToLayer("Character")) |
//				//(1 << LayerMask.NameToLayer("BakeMesh")) |
//				(1 << LayerMask.NameToLayer("Outline"))
//			//(1 << LayerMask.NameToLayer("Grimm")) |
//			//(1 << LayerMask.NameToLayer("Glow")) |
//			//(1 << LayerMask.NameToLayer("GameplayObject")) |
//			//(1 << LayerMask.NameToLayer("CharacterProp"))
//			);
//			return captureMask;
//		}
//	}

//	#endregion
//}