using System.Diagnostics;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MSP2050.Scripts
{
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
		private string buildTime = "2023-01-01 13:24:12Z";
		private string gitTag = "";
		private bool hasInformation = false;
		private int[] version;

		public static void UpdateBuildInformation(UnityManifest manifest)
		{
			manifest.SetGitTag(UpdateTag());
			manifest.SetBuildTime(UpdateTime());
			manifest.Save();
		}

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

			singleton.GetManifest();
		}

		public static string UpdateTime()
		{
			string buildTime = System.DateTime.Now.ToString("u", CultureInfo.InvariantCulture);
			return buildTime;
		}

		public static string UpdateTag()
		{
			string gitTag = "";

			Process proc = new Process
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

		public void GetManifest()
		{
			UnityManifest manifest = UnityManifest.Load();

			gitTag = manifest.buildNumber;
			buildTime = manifest.buildStartTime;
			hasInformation = true;
			string[] versionStrings = gitTag.Split('.');
			if (versionStrings.Length != 3)
				UnityEngine.Debug.LogError("Invalid client version string specified: " + gitTag);
			else
			{
				version = new int[3];
				if (int.TryParse(versionStrings[0], out int value) && int.TryParse(versionStrings[1], out int value1) && int.TryParse(versionStrings[2], out int value2))
				{
					version[0] = value;
					version[1] = value1;
					version[2] = value2;
				}
				else
					UnityEngine.Debug.LogError("Invalid client version string specified: " + gitTag);
			}
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

		public bool ServerVersionCompatible(string a_serverVersion)
		{
			if (version == null || a_serverVersion == null)
				return false;

			string[] versionStrings = a_serverVersion.Split('.');
			if (versionStrings.Length != 3)
			{
				return false;
			}
			else
			{
				return int.TryParse(versionStrings[0], out int value) && value >= version[0] &&
					int.TryParse(versionStrings[1], out int value1) && value1 >= version[1];
			}
		}
	}
}