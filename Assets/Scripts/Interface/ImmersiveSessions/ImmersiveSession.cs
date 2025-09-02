using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MSP2050.Scripts
{
    public class ImmersiveSession
    {
        public enum ImmersiveSessionState { Active, Inactive };
        public enum ImmersiveSessionType { AugGIS, IO };

        public int id;
        public string name;
        public ImmersiveSessionConnection connection;
        public int month;
        [JsonConverter(typeof(StringEnumConverter))]
        public ImmersiveSessionState state;
        [JsonConverter(typeof(StringEnumConverter))]
        public ImmersiveSessionType type;
        public int bottom_left_x;
        public int bottom_left_y;
        public int top_right_x;
        public int top_right_y;
    }

    public class ImmersiveSessionConnection
    {
        public int id;
        public string session;
        public int dockerApiID;
        public int port;
        public string dockerContainerID;
    }
}
