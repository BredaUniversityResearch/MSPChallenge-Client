using System.Diagnostics;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ApplicationBuildIdentifier : MonoBehaviour
{
	private static ApplicationBuildIdentifier singleton;
	public static ApplicationBuildIdentifier Instance
	{
		get
		{
			if (singleton == null)
				singleton = FindObjectOfType<ApplicationBuildIdentifier>();
			return singleton;
		}
	}
	private string buildTime = "2022-08-24 13:24:12Z";
	private string gitTag = "";
	private bool hasInformation = false;

#if UNITY_CLOUD_BUILD
public static void UpdateBuildInformation(UnityEngine.CloudBuild.BuildManifestObject manifest)
{
    manifest.SetValue("buildNumber", UpdateTag());
    manifest.SetValue("buildStartTime", UpdateTime());
}
#endif

	void Awake()
	{
		if (singleton != null && singleton != this)
		{
			Destroy(this);
			return;
		}
		else
		{
			singleton = this;
			DontDestroyOnLoad(gameObject);
		}

		singleton.GetUCBManifest();
	}

	public static string UpdateTime()
	{
		string buildTime = System.DateTime.Now.ToString("u", CultureInfo.InvariantCulture);
        return buildTime;
	}

	public static string UpdateTag()
	{
        string gitTag = "";

		var proc = new Process
		{
			StartInfo = new ProcessStartInfo()
			{
				FileName = "git",
				Arguments = "rev-list --tags --max-count=1",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			}
		};

		proc.Start();
		string commitId = proc.StandardOutput.ReadToEnd();

		proc = new Process
		{
			StartInfo = new ProcessStartInfo()
			{
				FileName = "git",
				Arguments = $"describe --tags \"{commitId.Trim()}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
			}
		};

		proc.Start();
		while (!proc.StandardOutput.EndOfStream)
		{
			gitTag += $"{proc.StandardOutput.ReadLine()},";
		}
		proc.WaitForExit();

		gitTag = gitTag.Remove(gitTag.Length - 1);

		return gitTag;
	}


	public void GetUCBManifest()
	{
		UnityCloudBuildManifest manifest = UnityCloudBuildManifest.Load();

		gitTag = manifest.buildNumber;
		buildTime = manifest.buildStartTime;
		hasInformation = true;
	}

	public string GetBuildTime()
    {
		return buildTime;
    }
	public string GetGitTag()
	{
		return gitTag;
	}
	public bool GetHasInformation()
    {
		return hasInformation;
    }

}
