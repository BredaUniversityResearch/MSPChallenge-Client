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
        //public enum ImmersiveSessionState { Active, Inactive };
        public enum ImmersiveSessionType { mr, vr };

        public int id;
        public string name;
        public ImmersiveSessionConnection connection;
        public int month;
        //[JsonConverter(typeof(StringEnumConverter))]
        //public ImmersiveSessionState state;
        [JsonConverter(typeof(StringEnumConverter))]
        public ImmersiveSessionType type;
        public float bottomLeftX;
        public float bottomLeftY;
        public float topRightX;
        public float topRightY;
    }

    public class ImmersiveSessionConnection
    {
        public int id;
        public string session;
        public int dockerApiID;
        public int port;
        public string dockerContainerID;
    }

	public class ImmersiveSessionSubmit
	{

		public string name;
		public int month;
		//[JsonConverter(typeof(StringEnumConverter))]
		//public ImmersiveSessionState state;
		[JsonConverter(typeof(StringEnumConverter))]
		public ImmersiveSession.ImmersiveSessionType type;
		public float bottomLeftX;
		public float bottomLeftY;
		public float topRightX;
		public float topRightY;
	}
}
