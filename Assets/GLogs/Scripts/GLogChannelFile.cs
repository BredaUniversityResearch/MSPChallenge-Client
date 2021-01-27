using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GLogChannelFile", menuName = "Log Channel/File", order = 302)]
public class GLogChannelFile : GLogChannel{
    protected System.IO.StreamWriter logFile = null;
    public string logPathname = System.IO.Path.Combine("Output","log.txt");

    public override void Open(){
        if (IsOpen()){
            Shutdown();
        }
        base.Open();
		string fullpath = System.IO.Path.Combine(Application.persistentDataPath, logPathname);
		(new System.IO.FileInfo(fullpath)).Directory.Create();
        logFile = new System.IO.StreamWriter(fullpath, true, System.Text.Encoding.ASCII);
    }

    /// <summary>Open is called when ever a channel is initialized</summary>
    public override void Shutdown(){
        if (logFile != null)
            logFile.Close();
    }

    /// <summary>Check if the channel is open</summary>
    public override bool IsOpen(){
        return logFile != null;
    }

    protected virtual string OutputString(GLog.LogInfo log){
        if (log.tag1 == null)
            return string.Format("{0:yyyy-MM-dd hh:mm:ss.fff}|{1}:{2}", log.time, GLog.VerboseLevelString(log.level), log.message);
        else if (log.tag2 == null)
            return string.Format("{0:yyyy-MM-dd hh:mm:ss.fff}|{1}|{2}:{3}", log.time, GLog.VerboseLevelString(log.level), log.tag1, log.message);
        else if (log.tag3 == null)
            return string.Format("{0:yyyy-MM-dd hh:mm:ss.fff}|{1}|{2}|{3}:{4}", log.time, GLog.VerboseLevelString(log.level), log.tag1, log.tag2, log.message);
        else
            return string.Format("{0:yyyy-MM-dd hh:mm:ss.fff}|{1}|{2}|{3}|{4}:{5}", log.time, GLog.VerboseLevelString(log.level), log.tag1, log.tag2, log.tag3, log.message);
    }

    public override void Log(GLog.LogInfo log){
        if (logFile == null)
            Open();
        if (logFile != null)
            logFile.WriteLine(OutputString(log));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GLogChannelFile))]
public class GLogChannelFileEditor : GLogChannelEditor{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Pathname", GUILayout.Width(60));

        EditorGUILayout.BeginHorizontal();
        ((GLogChannelFile)targetChannel).logPathname = EditorGUILayout.TextField(((GLogChannelFile)targetChannel).logPathname);

        if (GUILayout.Button("Open...", GUILayout.MaxWidth(60))){
			Debug.Log(System.IO.Path.Combine(Application.persistentDataPath, ((GLogChannelFile)targetChannel).logPathname));
			Application.OpenURL(System.IO.Path.Combine(Application.persistentDataPath, ((GLogChannelFile)targetChannel).logPathname));
        }
        EditorGUILayout.EndHorizontal();

    }
}
#endif