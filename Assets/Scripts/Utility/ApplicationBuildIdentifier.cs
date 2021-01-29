using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Small scriptable object that acts as the build identifier. Contains information about the build date.
/// </summary>
[CreateAssetMenu]
public class ApplicationBuildIdentifier: ScriptableObject
{
	//Actual file reside in Assets/Utility/Resources/
	private const string BUILD_IDENTIFIER_ASSET_PATH = "BuildIdentifier";

	[SerializeField] private string buildTime;
	[SerializeField] private int svnRevisionNumber;

	public static ApplicationBuildIdentifier FindBuildIdentifier()
	{
		ApplicationBuildIdentifier identifier = Resources.Load<ApplicationBuildIdentifier>(BUILD_IDENTIFIER_ASSET_PATH);
		return identifier;
	}

#if UNITY_EDITOR
	public void UpdateBuildTime()
	{
		buildTime = System.DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

		string svnInfo = GetSVNInfo();
		svnRevisionNumber = GetCurrentRevisionFromSvnInfo(svnInfo);
	}

	/// <summary>
	/// Output should resemble the following format: 
	/// 
	/// SubWCRev: 'D:\Projects\MSP\Unity\MSP2050\Assets'
	/// Last committed at revision 3331
	/// Mixed revision range 3317:3331
	/// Local modifications found
	/// Unversioned items found
	/// </summary>
	/// <returns></returns>
	private string GetSVNInfo()
	{
		Process myProcess = new Process();
		myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		myProcess.StartInfo.CreateNoWindow = true;
		myProcess.StartInfo.UseShellExecute = false;
		myProcess.StartInfo.FileName = Application.dataPath + "/../SubWCRev.exe";
		myProcess.StartInfo.Arguments = Application.dataPath;
		myProcess.StartInfo.RedirectStandardOutput = true;
		myProcess.EnableRaisingEvents = true;
		myProcess.Start();
		myProcess.WaitForExit();

		string stdOut = myProcess.StandardOutput.ReadToEnd();
		return stdOut;
	}

	private int GetCurrentRevisionFromSvnInfo(string svnInfoOutput)
	{
		int revisionNumber;
		Regex regex = new Regex("Last committed at revision ([0-9]+)");
		Match match = regex.Match(svnInfoOutput);
		if (match.Success)
		{
			revisionNumber = int.Parse(match.Groups[1].Value);
		}
		else
		{
			revisionNumber = -1;
			UnityEngine.Debug.LogError("Could not find revision number from SVN info string \n" + svnInfoOutput);
		}
		return revisionNumber;
	}

#endif

	public string GetBuildTime()
	{
		return buildTime;
	}

	public int GetSvnRevisionNumber()
	{
		return svnRevisionNumber;
	}
}
