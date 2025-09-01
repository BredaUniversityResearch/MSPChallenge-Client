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

        public string name;
        public string address;
        public int number_users;
        public int viewing_time;
		[JsonConverter(typeof(StringEnumConverter))]
		public ImmersiveSessionState state;
		[JsonConverter(typeof(StringEnumConverter))]
        public ImmersiveSessionType session_type;
        public int bottom_left_x;
        public int bottom_left_y;
        public int top_right_x;
        public int top_right_y;
    }
}
