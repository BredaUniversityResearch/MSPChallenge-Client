using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GLog
{
	public class GLogMonitor : MonoBehaviour
	{
		public float updateTime = 1.0f;
		private bool running = true;

		public delegate void HttpGetEvent(string url, string message);
		public HttpGetEvent HttpGetSuccess;
		public HttpGetEvent HttpGetFailed;

		public delegate void HttpPostEvent(string url, Dictionary<string, string> headers, byte[] content, string message);
		public HttpPostEvent HttpPostSuccess;
		public HttpPostEvent HttpPostFailed;

		private IEnumerator GLogUpdate()
		{
			while (running)
			{
				GLog.Update(updateTime);
				yield return new WaitForSeconds(updateTime);
			}
		}

		public void HttpPost(string url, Dictionary<string, string> headers, byte[] content)
		{
			StartCoroutine(CoroutineHttpPost(url, headers, content));
		}

		private IEnumerator CoroutineHttpPost(string url, Dictionary<string, string> headers, byte[] content)
		{
			//Upload
			UnityWebRequest upload = new UnityWebRequest(url, "POST", new DownloadHandlerBuffer(), new UploadHandlerRaw(content));
			foreach (KeyValuePair<string, string> header in headers)
			{
				upload.SetRequestHeader(header.Key, header.Value);
			}

			yield return upload.SendWebRequest();

			//If an error occurs when uploading: store message in cache
			if (!string.IsNullOrEmpty(upload.error) && HttpPostFailed != null)
			{
				HttpPostFailed(url, headers, content, upload.error);
			}
			else if (HttpPostSuccess != null)
			{
				HttpPostSuccess(url, headers, content, upload.downloadHandler.text);
			}
		}

		public void HttpGet(string url)
		{
			StartCoroutine(CoroutineHttpGet(url));
		}

		private IEnumerator CoroutineHttpGet(string url)
		{
			//Upload
			UnityWebRequest upload = UnityWebRequest.Get(url);
			yield return upload.SendWebRequest();

			//If an error occurs when uploading: store message in cache
			if (!string.IsNullOrEmpty(upload.error) && HttpGetFailed != null)
			{
				HttpGetFailed(url, upload.error);
			}
			else if (HttpGetSuccess != null)
			{
				HttpGetSuccess(url, upload.downloadHandler.text);
			}
		}

		// Use this for initialization
		void Start()
		{
			StartCoroutine(GLogUpdate());
		}

		private void OnApplicationQuit()
		{
			GLog.Shutdown();
		}

		private void OnApplicationPause(bool pause)
		{
			//Disabled this as it was causing errors on certain machines and was unnecessary 
			//GLog.Event("GLog", "Application", "Pause", pause.ToString());
		}

		private void OnApplicationFocus(bool focus)
		{
			//Disabled this as it was causing errors on certain machines and was unnecessary 
			//GLog.Event("GLog", "Application", "Focus", focus.ToString());
		}
	}
}