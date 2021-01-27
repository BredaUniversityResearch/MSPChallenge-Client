using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GLogChannelGURaaS", menuName = "Log Channel/Remote (GURaaS)", order = 303)]
public class GLogChannelGURaaS : GLogChannel{
    public string logPathname = Path.Combine("Output", "guraasChannel.log");
    public float updateFrequency = 0;

    protected float updateNext = 0;
    protected StringBuilder logLine = new StringBuilder();
    protected StreamWriter  logFile = null;
    protected bool          logComplete = false;
    //const string GRG_URL = "http://localhost:9002/";
    const string GRG_URL = "https://grg.service.guraas.com";


    public override void Open(){
        if (IsOpen()){
            Shutdown();
        }
        base.Open();

        //
        FileInfo f = new FileInfo(Path.Combine(Application.persistentDataPath, logPathname));

        if (f.Exists && f.Length > 0){ // check if file exists and upload any remaining data
            RecoverData();
        }

        //  Recreate the file
        f.Directory.Create();
        logFile = new StreamWriter(Path.Combine(Application.persistentDataPath, logPathname), false);
        logLine.Length = 0;
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

    public override void Log(GLog.LogInfo log){
        if (logFile == null || activeSession == null) {
            return; // GLog.Error("GLogCannelGURaaS", "Log", "Submit", "Failed to save log, channel was not open and/or session not started");
        }
        else {
            string logEncoded = LogToString(log);
            logLine.Append(logEncoded);
            logFile.Write(logEncoded);
        }
    }

    protected string LogToString(GLog.LogInfo log){
        return 
            string.Format("{{\"time\":\"{0:yyyy-MM-dd HH:mm:ss}\",\"tag1\":\"{1}\",\"tag2\":\"{2}\",\"tag3\":\"{3}\",\"tag4\":\"{4}\",\"data\":\"{5}\"}},",
                log.time, GLog.VerboseLevelString(log.level), log.tag1, log.tag2, log.tag3, log.message);
    }

    /// <summary>Starts a new game session. 
    /// <remarks>If a new game session is already active, it closes that session. See <see cref="CloseSession"/></remarks>
    /// </summary>
    public override void StartSession(GLog.SessionInfo session){
        base.StartSession(session);
        string logString = string.Format("{{\"id_session\":\"{0}\",\"id_player\":\"{1}\",\"version\":\"{2}\",\"start\":\"{3:yyyy-MM-dd HH:mm:ss}\",\"context\":\"{4}\",\"data\": [",
            session.sessionId, session.playerId, session.version, session.start, session.context);
        logLine.Length = 0;
        logLine.Append(logString);
        logFile.Write(session.gameId + "|"+logString);

        logComplete = false;
    }

    /// <summary>Close the current game session, and pushes/flushed all the cached logs</summary>
    public override void CloseSession(GLog.SessionInfo session){
        if (activeSession == null) return;
        activeSession = session;

        string logString = string.Format("{{\"time\":\"{0:yyyy-MM-dd HH:mm:ss}\",\"tag1\":\"{1}\",\"tag2\":\"GLogChannelGURaaS\",\"tag3\":\"Remote\",\"tag4\":\"Close\",\"data\":\"\"}}],\"end\":\"{2:yyyy-MM-dd HH:mm:ss}\"}}",
            session.end, GLog.VerboseLevelString(GLog.VerboseLevel.EVENT), session.end);

        logLine.Append(logString);
        logFile.Write(logString);
        logComplete = true;
        Flush();
        activeSession = null;
    }

    /// <summary>Flush send session to remote server</summary>
    public override void Flush() {

        if (activeSession == null || activeSession.gameId == null || activeSession.gameId.Equals("")){
            return;
        }

        if (logComplete) {                  // Session ended correctly just submit and clear the file
            
            Upload(activeSession.gameId, logLine.ToString());
            // close the logFile and reopen it to empty it
            logFile.Close();
            logFile = new StreamWriter(Path.Combine(Application.persistentDataPath, logPathname), false);
        }
        else {                            // Regular Submit (session is still open)
            string logString = string.Format("{{\"time\":\"{0:yyyy-MM-dd HH:mm:ss}\",\"tag1\":\"{1}\",\"tag2\":\"GLogChannelGURaaS\",\"tag3\":\"Regular\",\"tag4\":\"Submit\",\"data\":\"\"}}],\"end\":\"{2:yyyy-MM-dd HH:mm:ss}\"}}",
                    activeSession.end, GLog.VerboseLevelString(GLog.VerboseLevel.EVENT), activeSession.end);
            logLine.Append(logString);
            logFile.Write(logString);

            Upload(activeSession.gameId, logLine.ToString());

            // close the logFile and reopen it to empty it
            logFile.Close();
            logFile = new StreamWriter(Path.Combine(Application.persistentDataPath, logPathname), false);

            // add the initial session information
            logString = string.Format("{{\"id_session\":\"{0}\",\"id_player\":\"{1}\",\"version\":\"{2}\",\"start\":\"{3:yyyy-MM-dd HH:mm:ss}\",\"context\":\"{4}\",\"data\": [",
                      activeSession.sessionId, activeSession.playerId, activeSession.version, activeSession.start, activeSession.context);

            logLine.Length = 0;
            logLine.Append(logString);
            logFile.Write(activeSession.gameId + "|" + logString);
            logComplete = false;
        }
    }

    private void RecoverData(){
        StreamReader f = new StreamReader(Path.Combine(Application.persistentDataPath, logPathname));

        System.DateTime now = System.DateTime.Now;
        string logString = f.ReadToEnd();
        int index = logString.IndexOf('|');
        string gameId = logString.Substring(0, index);
        logString = logString.Substring(index + 1);

        if (logString.EndsWith(",")){
            logString += string.Format("{{\"time\":\"{0:yyyy-MM-dd HH:mm:ss}\",\"tag1\":\"{1}\",\"tag2\":\"GLogChannelGURaaS\",\"tag3\":\"Remote\",\"tag4\":\"Data Recovery\",\"data\":\"Data from a previous session was recovered and submitted.\"}}],\"end\":\"{2:yyyy-MM-dd HH:mm:ss}\"}}",
            now, GLog.VerboseLevelString(GLog.VerboseLevel.EVENT), now);
        }
        Upload(gameId, logString);
        f.Close();
    }


    protected void Upload(string gameId, string jsonString) {
        string url = GRG_URL + "/v1/games/" + gameId + "/data";
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers["Content-Type"] = "application/json";

        byte[] content = System.Text.Encoding.ASCII.GetBytes(jsonString);
        GLog.Instance.Monitor.HttpPost(url, headers, content);

        // Clear the log line
        logLine.Length = 0;
    }

    /// <summary>Update the log system, and if required flush the cached values</summary>        
    public override void UpdateChannel(float deltaTime) {
        if (updateFrequency == 0){
            Flush();
        }else{
            updateNext -= deltaTime;
            if (updateNext < 0){
                updateNext = updateFrequency;
                Flush();
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GLogChannelGURaaS))]
public class GLogChannelFileGURaaS : GLogChannelEditor{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Submit Frequency Seconds");
        ((GLogChannelGURaaS)targetChannel).updateFrequency = EditorGUILayout.Slider(((GLogChannelGURaaS)targetChannel).updateFrequency,2,600);

        EditorGUILayout.LabelField("Backup Pathname");

        EditorGUILayout.BeginHorizontal();
        ((GLogChannelGURaaS)targetChannel).logPathname = EditorGUILayout.TextField(((GLogChannelGURaaS)targetChannel).logPathname);
        
        if (GUILayout.Button("Open...", GUILayout.MaxWidth(60))) {
            Debug.Log(Path.Combine(Application.dataPath, ((GLogChannelGURaaS)targetChannel).logPathname));
            Application.OpenURL(Path.Combine(Application.dataPath, ((GLogChannelGURaaS)targetChannel).logPathname));
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif