using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class VideoEntry
{
	public string name;
	//public MovieTexture movieTexture;
}

public class PropertyWindowVideoAssigner : MonoBehaviour
{
	public static PropertyWindowVideoAssigner instance;

	//private Dictionary<string, MovieTexture> mVideos = new Dictionary<string, MovieTexture>();

	[SerializeField]
	public List<VideoEntry> mVideoList;

	PropertyWindowVideoAssigner()
	{
		instance = this;
	}

	public bool HasVideo(string videoName)
	{
		return mVideoList.Find(obj => obj.name == videoName) != null;
	}

	public bool GetVideo(string videoName/*, ref MovieTexture movieTexture*/)
	{
		//foreach (VideoEntry entry in mVideoList)
		//{
		//	if (entry.name == videoName)
		//	{
		//		movieTexture = entry.movieTexture;
		//		return true;
		//	}
		//}
		return false;
	}
}
