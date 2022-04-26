using System;
using System.Collections.Generic;

namespace HEBGraph
{
	[Serializable]
	public class HEBGraphData
	{
		public HEBGraphDataGroup[] groups;
		public HEBGraphDataLink[] links;
	}

	[Serializable]
	public class HEBGraphDataGroup
	{
		public string name;
		public HEBGraphDataEntry[] entries;
	}

	[Serializable]
	public class HEBGraphDataEntry
	{
		public string name;
		public int id;
		public bool multiline;
		public string link;
	}
	
	[Serializable]
	public class HEBGraphDataLink
	{
		public int fromId;
		public int toId;
		public int severity;
		public string description;
	}
}