using System.Diagnostics;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

	/// <summary>
	/// Small scriptable object that acts as the build identifier. Contains information about the build date.
	/// </summary>
	[CreateAssetMenu]
public class ApplicationBuildIdentifier : ScriptableObject
{
	//Actual file reside in Assets/Resources/
	private const string BUILD_IDENTIFIER_ASSET_PATH = "BuildIdentifier";

	[SerializeField] private string buildTime;
	[SerializeField] private string gitTag;

#if UNITY_CLOUD_BUILD
public static void UpdateBuildInformation(UnityEngine.CloudBuild.BuildManifestObject manifest)
{
    ApplicationBuildIdentifier identifier = Resources.Load<ApplicationBuildIdentifier>(BUILD_IDENTIFIER_ASSET_PATH);
    identifier.UpdateBuildTime();
    manifest.SetValue("buildNumber", identifier.gitTag);
    manifest.SetValue("buildStartTime", identifier.buildTime);
}
#endif

	public static ApplicationBuildIdentifier FindBuildIdentifier()
	{
		ApplicationBuildIdentifier identifier = Resources.Load<ApplicationBuildIdentifier>(BUILD_IDENTIFIER_ASSET_PATH);
		UnityCloudBuildManifest manifest = UnityCloudBuildManifest.Load();

		if(manifest != null)
		{
			identifier.buildTime = manifest.GetBuildTime();
			identifier.gitTag = manifest.GetGitTag();
		}

		return identifier;
	}

	public void UpdateBuildTime()
	{
		buildTime = System.DateTime.Now.ToString("u", CultureInfo.InvariantCulture);
		gitTag = RunGitCommand();
	}

	public string RunGitCommand()
	{
		string result = "";
		var proc = new Process
		{
			StartInfo = new ProcessStartInfo()
			{
				FileName = "git",
				Arguments = $"git describe --tags `git rev-list --tags --max-count=1`",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
			}
		};

		proc.Start();
		while (!proc.StandardOutput.EndOfStream)
		{
			result += $"{proc.StandardOutput.ReadLine()},";
		}
		proc.WaitForExit();
		return result;
	}

	public string GetBuildTime()
	{
		return buildTime;
	}

	public string GetGitTag()
	{
		return gitTag;
	}
}