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
}