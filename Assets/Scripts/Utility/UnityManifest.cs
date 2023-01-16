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
        var manifest = (TextAsset)Resources.Load("Manifest/UnityManifest.json");
        return manifest == null ? null : JsonUtility.FromJson<UnityManifest>(manifest.text);
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

    public void Write()
    {
        string jsonString = JsonUtility.ToJson(this, true);
        //System.IO.File.WriteAllText(Application.persistentDataPath + ".json", );
    }

}