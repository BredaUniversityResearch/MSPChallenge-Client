using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GLogChannelConsole", menuName = "Log Channel/Console", order = 301)]
public class GLogChannelConsole : GLogChannel{

    private string OutputString(GLog.LogInfo log){
        if (log.tag1 == null)
            return string.Format("{0:(dd)hh:mm:ss.fff} <color={1}>{2}</color>: {3}", log.time, GLog.VerboseLevelColor(log.level), GLog.VerboseLevelString(log.level), log.message);
        else if (log.tag2 == null)
            return string.Format("{0:(dd)hh:mm:ss.fff} <color={1}>{2}</color> <b>{3}: {4}", log.time, GLog.VerboseLevelColor(log.level), GLog.VerboseLevelString(log.level), log.tag1, log.message);
        else if (log.tag3 == null)
            return string.Format("{0:(dd)hh:mm:ss.fff} <color={1}>{2}</color> <b>{3}</b> <i>{4}</i>: {5}", log.time, GLog.VerboseLevelColor(log.level), GLog.VerboseLevelString(log.level), log.tag1, log.tag2, log.message);
        else
            return string.Format("{0:(dd)hh:mm:ss.fff} <color={1}>{2}</color> <b>{3}</b> <i>{4}</i> {5}: {6}", log.time, GLog.VerboseLevelColor(log.level), GLog.VerboseLevelString(log.level), log.tag1, log.tag2, log.tag3, log.message);
    }
    
    public override void Log(GLog.LogInfo log){
        if (log.level == GLog.VerboseLevel.ERROR)
            Debug.LogError(OutputString(log));
        else if (log.level == GLog.VerboseLevel.WARNING)
            Debug.LogWarning(OutputString(log));
        else
            Debug.Log(OutputString(log));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GLogChannelConsole))]
public class GLogChannelConsoleEditor : GLogChannelEditor{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();
    }
}
#endif