using System;
using UnityEngine;


[Serializable]
public class UnityManifest
{
    public string scmCommitId;
    public string scmBranch;
    public string buildNumber;
    public string buildStartTime;
    public string projectId;
    public string bundleId;
    public string unityVersion;
    public string xcodeVersion;
    public string cloudBuildTargetName;

    public static UnityManifest Load()
    {
        TextAsset manifest = Resources.Load<TextAsset>("Manifest/UnityManifest");
        return manifest == null ? null : JsonUtility.FromJson<UnityManifest>(manifest.text);
    }

    public void Save()
    {
        string jsonString = JsonUtility.ToJson(this, true);
        System.IO.File.WriteAllText(Application.dataPath + "/Resources/Manifest/UnityManifest.json", jsonString);
    }

    public string GetBuildTime()
    {
        return buildStartTime;
    }

    public string GetGitTag()
    {
        return buildNumber;
    }

    public void SetBuildTime(string value)
    {
        buildStartTime = value;
    }

    public void SetGitTag(string value)
    {
        buildNumber = value;
    }

}