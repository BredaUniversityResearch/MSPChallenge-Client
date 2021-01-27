using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///  GLog or Games User Research Log is a library to allow developer to have control over the log usign multiple channels
///  <para>This efficient library allows to set parallel log channels which allow developers to register events for different spaces, like console, 
///  multiple files with different remote verboses, and even remote logging of player behavior, and errors. Note: Not thread-safe</para> 
/// </summary>
public class GLog : ScriptableObject {
    public class SessionInfo
    {
        public string gameId;
        public string version;
        public string sessionId;
        public string playerId;
        public string context;
        public System.DateTime start;
        public System.DateTime end;

        /// <summary>Defines a Log session</summary>
        ///  <param name="version"> Optional: To identify the game version</param>
        ///  <param name="playerId">Optional: To identify players across multiple game sessions</param>
        ///  <param name="context"> Optional: To game context, for example platform, device specs, geolocation, etc.</param>
        public SessionInfo(string gameUUID, string version = null, string playerId = null, string context = null)
        {
            this.gameId = gameUUID;
            this.version = version;
            this.playerId = playerId;
            this.context = context;
            this.sessionId = System.Guid.NewGuid().ToString();
            this.start = System.DateTime.Now;
            this.end = start;
        }

    }
    public class LogInfo
    {
        public System.DateTime time;
        public VerboseLevel level;
        public string tag1 = null;
        public string tag2 = null;
        public string tag3 = null;
        public string message = null;

        /// <summary>Defines a log message or the Log Channel.
        /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
        /// </summary>
        /// <param name="message">List of tags (up to three) and message </param>
        public LogInfo(VerboseLevel level, params string[] message)
        {
            this.time = System.DateTime.Now;
            this.level = level;
            if (message.Length == 4)
            {
                this.tag1 = message[0];
                this.tag2 = message[1];
                this.tag3 = message[2];
                this.message = message[3];
            }
            else if (message.Length == 3)
            {
                this.tag1 = message[0];
                this.tag2 = message[1];
                this.message = message[2];
            }
            else if (message.Length == 2)
            {
                this.tag1 = message[0];
                this.message = message[1];
            }
            else if (message.Length == 1)
            {
                this.message = message[0];
            }
            else
            {
                throw new System.Exception("Missing a log message");
            }
        }
    }

    #region GLogMonitor
    /// <summary>
    /// Game object deployed to keep an eye on the scene
    /// </summary>
    private GLogMonitor monitor = null;
    public GLogMonitor Monitor{
        get{
            return monitor;
        }
    }
    #endregion

    #region GLogChannel
    public List<GLogChannel> channels = new List<GLogChannel>();
    public string gameId;
    public string gameVersion="V1.0";
    public bool autoStartSession=true;
    #endregion

    #region Singleton
    #if UNITY_EDITOR
    private static GLog CreateDefaultGLog() {
        // first create a console channel
        GLogChannelConsole console = ScriptableObject.CreateInstance<GLogChannelConsole>();
        console.name = "Console";
        
        string path = Path.Combine("Assets", Path.Combine("Resources", "GLogChannelConsole.asset"));
        (new System.IO.FileInfo(path)).Directory.Create();
        AssetDatabase.CreateAsset(console, path);

        GLogChannelFile file = ScriptableObject.CreateInstance<GLogChannelFile>();
        file.name = "File";
        file.logPathname = "log.txt";
        path = Path.Combine("Assets", Path.Combine("Resources", "GLogChannelFile.asset"));
        AssetDatabase.CreateAsset(file, path);


        GLog asset = ScriptableObject.CreateInstance<GLog>();
        asset.channels.Add(console);
        asset.channels.Add(file);
        asset.gameVersion = "V0.0";
        path = Path.Combine("Assets", Path.Combine("Resources", "GLogConfig.asset"));
        AssetDatabase.CreateAsset(asset, path);
        
        AssetDatabase.SaveAssets();

        return asset;
    }
    #endif

    private static GLog instance = null;
    public static GLog Instance
    {
        get{
            if(instance == null){ // Load from default location

                // instance = AssetDatabase.LoadAssetAtPath<GLog>(Path.Combine("Assets", Path.Combine("Resources", "GLogConfig.asset")));
                instance = Resources.Load<GLog>("GLogConfig");
            }
#if UNITY_EDITOR
            if (instance == null)
                instance = CreateDefaultGLog();

            if(EditorApplication.isPlaying)
#endif
                if (instance && instance.monitor == null ) {
                try{
                    //add GLog Game Object to the scene
                    //this will allow to monitor the scene 
                    GameObject monitorObj = new GameObject("GLog Monitor");            
                    // make sure it is persistent
                    GameObject.DontDestroyOnLoad(monitorObj);
                    // add the Logs Monitor component
                    instance.monitor = monitorObj.AddComponent<GLogMonitor>();

                    foreach (GLogChannel channel in instance.channels){ 
                        if (channel!= null) channel.Open();
                    }
                    if (instance.autoStartSession){
                        StartSession();
                    }
                }
                catch (Exception) { }
            }
            return instance;
        }
    }
    
    void OnEnable(){}

    /// <summary>
    /// Shutdown should be called on application shutdown, and make sure all the GLogChannels exit correctly including closing file, push information or push any delayed information to the server.
    /// GLogs Monitor will be responsible to detect application shutdown
    /// </summary>
    void OnDestroy(){
        Shutdown();
    }

    public static void Shutdown() {
        GLog log = Instance;
        if (log.activeSession!=null)
            CloseSession();
        foreach (GLogChannel channel in log.channels){
            if (channel != null) channel.Shutdown();
        }
        if (log.monitor != null) {
            GameObject.Destroy(log.monitor.gameObject);
            log.monitor = null;
        }
    }

    /// <summary>
    /// Forces all GLogs Log Channels empty the cached logs and submit all logs
    /// </summary>
    public static void Flush(){
        GLog log = Instance;
        foreach (GLogChannel channel in log.channels) {
            if (channel != null) channel.Flush();
        }
    }

    /// <summary>
    /// Update the Log Channel, mainly for time base log channels to push information to files and remote servers
    /// </summary>
    public static void Update(float deltaTime){
        GLog log = Instance;
        foreach (GLogChannel channel in log.channels){
            if (channel != null) channel.UpdateChannel(deltaTime);
        }
    }
    #endregion

    #region Session 
    protected SessionInfo activeSession = null;
    
    /// <summary>
    /// Starts a new game session. 
    /// <remarks>If a new game session is already active, it closes that session. See <see cref="CloseSession"/></remarks>
    /// /// <param name="gameUUID"> For identifying the game, when using files. This should be based on the UUID of game provided by www.guraas.com. Otherwise leave it null, but, remote features will not work.</param>
    /// <param name="version"> Optional: To identify the game version</param>
    /// <param name="playerId">Optional: To identify players across multiple game sessions</param>
    /// <param name="context"> Optional: To game context, for example platform, device specs, geolocation, etc.</param>
    public static void StartSession(string version = null, string playerId = null, string context = null){
        GLog log = Instance;
        if (log.activeSession != null)
            CloseSession();

        if (version == null)
            log.activeSession = new SessionInfo(log.gameId, log.gameVersion, playerId, context);
        else
            log.activeSession = new SessionInfo(log.gameId, version, playerId, context);

        foreach (GLogChannel channel in log.channels){
            if (channel != null) channel.StartSession(instance.activeSession);
        }
        GLog.Event("GLog", "Session", "Start", instance.activeSession.version);
    }

    /// <summary>
    /// Close the current game session, and pushes/flushed all the cached logs
    /// </summary>
    public static void CloseSession(){
        GLog log = Instance;
        if (log.activeSession == null) return;

        GLog.Event("GLog", "Session", "Close");

        foreach (GLogChannel channel in log.channels){
            if (channel != null){
                channel.CloseSession(instance.activeSession);
                channel.Flush();
            }
        }
    }
    #endregion

    #region Logs
    /// <summary>
    /// Verbose level allows to control verbose levels for different log mechanisms using a mask, example: GLogs.RemoteVerbose = PLAYER | EXCEPTION | ERROR;
    /// </summary>
    /// <example> 
    /// This sample shows how to set a mask which only stores remote logs for Player Behavior, Exceptions and Error
    /// <code>
    ///   GLogs.RemoteVerbose = PLAYER | EXCEPTION | ERROR;
    /// </code>
    /// </example> 
    public enum VerboseLevel
    {
        ///<summary>All logs</summary>
        ALL = 255,
        ///<summary>Debug related logs</summary>
        DEBUG = 1,
        ///<summary>General log information</summary>
        INFO = 2,
        ///<summary>Player Behavior related logs</summary>
        PLAYER = 4,
        ///<summary>System performance related logs</summary>
        PERFORMANCE = 8,
        ///<summary>Subchannels and 3rd party related logs</summary>
        SUBSYSTEM = 16,
        ///<summary>Event related logs</summary>
        EVENT = 32,
        ///<summary>Warning related logs</summary>
        WARNING = 64,
        ///<summary>Error related logs</summary>
        ERROR = 128,
        ///<summary>No logs will be saved</summary>
        NONE = 0
    };

    public static string VerboseLevelString(VerboseLevel level)
    {
        switch (level)
        {
            case VerboseLevel.ALL:
                return "All";
            case VerboseLevel.DEBUG:
                return "Debug";
            case VerboseLevel.INFO:
                return "Info";
            case VerboseLevel.PLAYER:
                return "Player";
            case VerboseLevel.PERFORMANCE:
                return "Performance";
            case VerboseLevel.SUBSYSTEM:
                return "Subsystem";
            case VerboseLevel.EVENT:
                return "Event";
            case VerboseLevel.WARNING:
                return "Warning";
            case VerboseLevel.ERROR:
                return "Error";
            case VerboseLevel.NONE:
                return "None";
            default:
                return "Unknown";
        }
    }

    public static string VerboseLevelColor(VerboseLevel level)
    {
        switch (level)
        {
            case VerboseLevel.ALL:
                return "#FFFFFF";
            case VerboseLevel.DEBUG:
                return "#333333";
            case VerboseLevel.INFO:
                return "#720DF7";
            case VerboseLevel.PLAYER:
                return "#007F00";
            case VerboseLevel.PERFORMANCE:
                return "#00009F";
            case VerboseLevel.SUBSYSTEM:
                return "#7C7C42";
            case VerboseLevel.EVENT:
                return "#0B3974";
            case VerboseLevel.WARNING:
                return "#DD5A5F";
            case VerboseLevel.ERROR:
                return "#C81D25";
            case VerboseLevel.NONE:
                return "#000000";
            default:
                return "#000000";
        }
    }

    public static void Log(VerboseLevel level, params string[] message){
        GLog log = Instance;

        LogInfo logInfo = new LogInfo(level, message);
        if (instance.activeSession != null)
            instance.activeSession.end = logInfo.time;

        foreach (GLogChannel channel in log.channels){
            if (channel != null && channel.active && channel.HasLevel(logInfo.level))
                channel.Log(logInfo);
        }
    }

    #region debug
    /// <summary>Push a debug message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Debug(string message)
    {
        Log(VerboseLevel.DEBUG, message);
    }
    /// <summary>Push a debug message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the scene</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Debug(string tag1, string message)
    {
        Log(VerboseLevel.DEBUG, tag1, message);
    }
    /// <summary>Push a debug message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Debug(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.DEBUG, tag1, tag2, message);
    }
    /// <summary>Push a debug message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Debug(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.DEBUG, tag1, tag2, tag3, message);
    }
    #endregion

    #region info

    /// <summary>Push a Information message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Info(string message)
    {
        Log(VerboseLevel.INFO, message);
    }

    /// <summary>Push a Information message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>    
    public static void Info(string tag1, string message)
    {
        Log(VerboseLevel.INFO, tag1, message);
    }
    /// <summary>Push a Information message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Info(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.INFO, tag1, tag2, message);
    }
    /// <summary>Push a Information message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Info(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.INFO, tag1, tag2, tag3, message);
    }
    #endregion

    #region warning
    /// <summary>Push a Warning message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Warning(string message)
    {
        Log(VerboseLevel.WARNING, message);
    }
    /// <summary>Push a Warning message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Warning(string tag1, string message)
    {
        Log(VerboseLevel.WARNING, tag1, message);
    }
    /// <summary>Push a Warning message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Warning(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.WARNING, tag1, tag2, message);
    }
    /// <summary>Push a Warning message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Warning(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.WARNING, tag1, tag2, tag3, message);
    }
    #endregion

    #region Error
    /// <summary>Push a Error message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Error(string message)
    {
        Log(VerboseLevel.ERROR, message);
    }

    /// <summary>Push a Error message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Error(string tag1, string message)
    {
        Log(VerboseLevel.ERROR, tag1, message);
    }
    /// <summary>Push a Error message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Error(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.ERROR, tag1, tag2, message);
    }
    /// <summary>Push a Error message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Error(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.ERROR, tag1, tag2, tag3, message);
    }
    #endregion

    #region Event
    /// <summary>Push a Game Event message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Event(string message)
    {
        Log(VerboseLevel.EVENT, message);
    }

    /// <summary>Push a Exception message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Event(string tag1, string message)
    {
        Log(VerboseLevel.EVENT, tag1, message);
    }
    /// <summary>Push a Exception message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Event(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.EVENT, tag1, tag2, message);
    }
    /// <summary>Push a Exception message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Event(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.EVENT, tag1, tag2, tag3, message);
    }
    #endregion

    #region Player
    /// <summary>Push a Player Behavior message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Player(string message)
    {
        Log(VerboseLevel.PLAYER, message);
    }
    /// <summary>Push a Player Behavior message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Player(string tag1, string message)
    {
        Log(VerboseLevel.PLAYER, tag1, message);
    }
    /// <summary>Push a Player Behavior message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Player(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.PLAYER, tag1, tag2, message);
    }
    /// <summary>Push a Player Behavior message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Player(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.PLAYER, tag1, tag2, tag3, message);
    }
    #endregion

    #region Performance
    /// <summary>Push a Performance message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Performance(string message)
    {
        Log(VerboseLevel.PERFORMANCE, message);
    }
    /// <summary>Push a Performance message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Performance(string tag1, string message)
    {
        Log(VerboseLevel.PERFORMANCE, tag1, message);
    }
    /// <summary>Push a Performance message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Performance(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.PERFORMANCE, tag1, tag2, message);
    }
    /// <summary>Push a Performance message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Performance(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.PERFORMANCE, tag1, tag2, tag3, message);
    }
    #endregion

    #region Subsystem
    /// <summary>Push a Subsystem message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Subsystem(string message)
    {
        Log(VerboseLevel.SUBSYSTEM, message);
    }

    /// <summary>Push a Subsystem message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Subsystem(string tag1, string message)
    {
        Log(VerboseLevel.SUBSYSTEM, tag1, message);
    }
    /// <summary>Push a Subsystem message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Subsystem(string tag1, string tag2, string message)
    {
        Log(VerboseLevel.SUBSYSTEM, tag1, tag2, message);
    }
    /// <summary>Push a Subsystem message to the Log Channel.
    /// <para>For log record keeping it is advised to use the tags, and make the message consistent to allow batch processing</para>
    /// </summary>
    /// <param name="tag1">Tag1 will allow you to filter the log messages, advised to focus on the Scene, for example: level</param>
    /// <param name="tag2">Tag2 will allow you to filter the log messages, advised to focus on the Object/Element, for example: window, NPC, player</param>
    /// <param name="tag3">Tag3 will allow you to filter the log messages, advised to focus on the activity, for example: Start, Jump, Falling, Open, Cancel, etc.</param>
    /// <param name="message">Your log message, advised to focus on parameters. Make them consistent to allow batch processing, for example, position (X,Y), velocity (ms)</param>
    public static void Subsystem(string tag1, string tag2, string tag3, string message)
    {
        Log(VerboseLevel.SUBSYSTEM, tag1, tag2, tag3, message);
    }
    #endregion

    #endregion

#if UNITY_EDITOR
    // [MenuItem("Logs/Clear")]
    public static void MenuClearLogs(){
        instance = null;
    }

    [MenuItem("Edit/Project Settings/Log Config")]
    public static void MenuProjectSettingsLogs(){
        Selection.activeObject = Instance;
        EditorApplication.ExecuteMenuItem("Window/Inspector");
    }
#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(GLog))]
public class GLogEditor : Editor{
    SerializedObject logObj;
    List<bool> unfolded = new List<bool>();
    
    void OnEnable(){
        try {
            logObj = new SerializedObject(target);
        } catch (Exception) {} // ignore the exception when it is not  able to fined the target
    }

    public override void OnInspectorGUI() {
        int i;
        logObj.Update();

        EditorGUILayout.LabelField("GLog Configuration");

        // game Id
        SerializedProperty gameId = logObj.FindProperty("gameId");
        gameId.stringValue = EditorGUILayout.TextField("Game Unique Id", gameId.stringValue);

        // game Version
        SerializedProperty gameVersion = logObj.FindProperty("gameVersion");
        gameVersion.stringValue = EditorGUILayout.TextField("Game Version", gameVersion.stringValue);

        SerializedProperty autoStartSession = logObj.FindProperty("autoStartSession");
        autoStartSession.boolValue = EditorGUILayout.ToggleLeft("Autostart session", autoStartSession.boolValue);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        SerializedProperty channels = logObj.FindProperty("channels");
        SerializedProperty channel = null;
        GUIContent empty = new GUIContent();

        EditorGUILayout.LabelField("GLog Channels (#" + channels.arraySize+")");
        for (i = 0; i < channels.arraySize; i++) {
            channel = channels.GetArrayElementAtIndex(i);
            if (i >= unfolded.Count)
                unfolded.Add(false);

            // header
            EditorGUILayout.BeginHorizontal();
            // folding
            unfolded[i] = EditorGUILayout.Foldout(unfolded[i], "Channel" + i, true);

            EditorGUILayout.ObjectField(channel, empty);

            if (GUILayout.Button("-", GUILayout.MaxWidth(16), GUILayout.MaxHeight(16))) {
                channel.objectReferenceValue = null;
                channel = null;
                channels.DeleteArrayElementAtIndex(i);
            }
            EditorGUILayout.EndHorizontal();

            if (unfolded[i] && channel!=null && channel.objectReferenceValue != null) { 
                Editor editor = Editor.CreateEditor(channel.objectReferenceValue, typeof(GLogChannelEditor));
                editor.OnInspectorGUI();
            }
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add new Log Channel")){
            channels.arraySize += 1;
            channel = channels.GetArrayElementAtIndex(channels.arraySize-1);
            channel.objectReferenceValue = null;
        }
        logObj.ApplyModifiedProperties();
    }
}
#endif