using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class GLogChannel : ScriptableObject{
    public string channelName = "Channel Name";
    public bool   active      = true;
    #region Verbose
    protected GLog.VerboseLevel level = GLog.VerboseLevel.ALL;

    /// <summary>
    /// Allow to get/set the channel verbose level
    /// </summary>
    public GLog.VerboseLevel Level{
        get { return level; }
        set { level = value; }
    }

    /// <summary>
    /// Allow to check if channel has a specific verbose level
    /// </summary>
    public bool HasLevel(GLog.VerboseLevel level) {
        return (this.level & level) != 0;
    }

    /// <summary>Add a Verbose level to the channel</summary>
    public void AddLevel(GLog.VerboseLevel level) {
        this.level = this.level | level;
    }

    /// <summary>Remove a Verbose level to the channel</summary>
    public void RemoveLevel(GLog.VerboseLevel level) {
        this.level = this.level & ~level;
    }

    /// <summary>Set the channel verbose level on or off</summary>
    public void SetLevel(GLog.VerboseLevel level, bool on){
        if (on)
            this.level = this.level | level;
        else
            this.level = this.level & ~level;
    }
    #endregion

    #region Channel
    /// <summary>Open is called when ever a channel is initialized</summary>
    public virtual void Open() { }

    /// <summary>Open is called when ever a channel is initialized</summary>
    public virtual void Shutdown() { }

    /// <summary>Check if the channel is open</summary>
    public virtual bool IsOpen(){
        return true;
    }

    /// <summary>
    /// Submits the list to the media (e.g. online, screen) and then clears the log list
    /// </summary>
    public virtual void Flush() { }

    /// <summary>
    /// Updates the log channel for temporal procedures
    /// </summary>
    public virtual void UpdateChannel(float deltaTime){}

    #endregion

    #region Session
    protected GLog.SessionInfo activeSession = null;
    /// <summary>Starts a new game session. 
    /// <remarks>If a new game session is already active, it closes that session. See <see cref="CloseSession"/></remarks>
    /// </summary>
    public virtual void StartSession(GLog.SessionInfo session){
        if (activeSession != null)
            CloseSession(activeSession);
        activeSession = session;
    }

    /// <summary>
    /// Close the current game session, and pushes/flushed all the cached logs
    /// </summary>
    public virtual void CloseSession(GLog.SessionInfo session){
        if (activeSession == null) return;
        activeSession = session;
        Flush();
        activeSession = null;
    }
    #endregion

    #region Logs
    /// <summary>Adds the log to the loglist</summary>
    /// <param name="log">Predefined Log Info</param>
    public virtual void Log(GLog.LogInfo log){}

    #endregion
}

#if UNITY_EDITOR


[CustomEditor(typeof(GLogChannel))]
public class GLogChannelEditor : Editor{
    protected GLogChannel targetChannel;
    
    void OnEnable(){
        targetChannel = (GLogChannel )target;        
    }

    public override void OnInspectorGUI(){
        bool result, level;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", GUILayout.Width(40));
        targetChannel.name = EditorGUILayout.TextField(targetChannel.name);
        targetChannel.active = EditorGUILayout.ToggleLeft("Active", targetChannel.active, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        level = targetChannel.HasLevel(GLog.VerboseLevel.DEBUG);
        result = EditorGUILayout.ToggleLeft("Debug", level, GUILayout.Width(60));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.DEBUG, result);

        level = targetChannel.HasLevel(GLog.VerboseLevel.PERFORMANCE);
        result = EditorGUILayout.ToggleLeft("Performance", level, GUILayout.Width(90));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.PERFORMANCE, result);

        level = targetChannel.HasLevel(GLog.VerboseLevel.SUBSYSTEM);
        result = EditorGUILayout.ToggleLeft("SubSystems", level, GUILayout.Width(90));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.SUBSYSTEM, result);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        level = targetChannel.HasLevel(GLog.VerboseLevel.INFO);
        result = EditorGUILayout.ToggleLeft("Info", level, GUILayout.Width(60));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.INFO, result);

        level = targetChannel.HasLevel(GLog.VerboseLevel.WARNING);
        result = EditorGUILayout.ToggleLeft("Warnings", level, GUILayout.Width(90));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.WARNING, result);

        level = targetChannel.HasLevel(GLog.VerboseLevel.ERROR);
        result = EditorGUILayout.ToggleLeft("Errors", level, GUILayout.Width(90));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.ERROR, result);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        level = targetChannel.HasLevel(GLog.VerboseLevel.PLAYER);
        result = EditorGUILayout.ToggleLeft("Player Behavior", level, GUILayout.Width(120));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.PLAYER, result);

        level = targetChannel.HasLevel(GLog.VerboseLevel.EVENT);
        result = EditorGUILayout.ToggleLeft("Application Events", level, GUILayout.Width(120));
        if (level != result) targetChannel.SetLevel(GLog.VerboseLevel.EVENT, result);
        EditorGUILayout.EndHorizontal();
    }
}
#endif