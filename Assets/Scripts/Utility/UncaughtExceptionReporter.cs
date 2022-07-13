using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MSP2050.Scripts
{
	class UncaughtExceptionReporter : MonoBehaviour
	{
		private class AdditionalDebugInfo
		{
			public readonly string region;
			public readonly string serverEndpoint;
			public readonly string country;
			public readonly string userName;
			public readonly string[] visibleLayers;
			public readonly string gameState;
			public readonly string gameTime;
			public readonly string currentlyEditingPlan;
			public readonly string currentlyEditingLayer;
			public readonly string lastUpdateTimeStamp;

			public AdditionalDebugInfo()
			{
				if (SessionManager.Instance.MspGlobalData != null)
				{
					region = SessionManager.Instance.MspGlobalData.region;
				}

				serverEndpoint = Server.Url;

				if (SessionManager.Instance.CurrentTeam != null)
				{
					country = SessionManager.Instance.CurrentTeam.name;
				}

				userName = SessionManager.Instance.CurrentUserName;
				visibleLayers = GatherVisibleLayers();
				gameState = TimeManager.Instance.CurrentState.ToString();
				gameTime = TimeManager.Instance.GetCurrentMonth().ToString();
				currentlyEditingPlan = Main.CurrentlyEditingPlan != null ? Main.CurrentlyEditingPlan.ID.ToString() : "None";
				//currentlyEditingLayer = Main.CurrentlyEditingPlanLayer != null ? Main.CurrentlyEditingPlanLayer.ID.ToString() : "None";
				lastUpdateTimeStamp = UpdateManager.Instance.LastUpdateTimeStamp.ToString();
			}

			private string[] GatherVisibleLayers()
			{
				List<string> layers = new List<string>();
				foreach (AbstractLayer layer in LayerManager.Instance.GetVisibleLayers())
				{
					layers.Add(layer.FileName);
				}
				return layers.ToArray();
			}

			public string FormatToString()
			{
				StringBuilder output = new StringBuilder(1024);
				output.Append("Server: ").Append(serverEndpoint).AppendLine();
				output.Append("Region: ").Append(region).AppendLine();
				output.Append("Country: ").Append(country).AppendLine();
				output.Append("UserName: ").Append(userName).AppendLine();
				output.Append("Game State: ").Append(gameState).AppendLine();
				output.Append("Game Time: ").Append(gameTime).AppendLine();
				output.Append("Editing plan: ").Append(currentlyEditingPlan).AppendLine();
				output.Append("Editing layer: ").Append(currentlyEditingLayer).AppendLine();
				output.Append("Last update time: ").Append(lastUpdateTimeStamp).AppendLine();
				output.Append("Last update: ").Append(JsonConvert.SerializeObject(UpdateManager.Instance.LastUpdate)).AppendLine();
				output.Append("Visible Layers:").AppendLine();
				foreach (string layer in visibleLayers)
				{
					output.Append(" - ").Append(layer).AppendLine();
				}
				return output.ToString();
			}
		};

		public enum EErrorReportingMode {
			ShowDialog, QuitInstantly
		};

		public static EErrorReportingMode ErrorReportingMode
		{
			get;
			set;
		}

		private static DialogBox errorDialogBox = null;

		private string delayedMessage = null;

		static UncaughtExceptionReporter()
		{
			ErrorReportingMode = EErrorReportingMode.ShowDialog;
		}

		private void Awake()
		{
			Application.logMessageReceived += OnLogMessage;
		}

		private void Update()
		{
			if (delayedMessage != null)
			{
				Debug.Log(delayedMessage);
				delayedMessage = null;
			}
		}

		private void OnLogMessage(string condition, string stacktrace, LogType type)
		{
			if (type == LogType.Exception || type == LogType.Assert)
			{
				if (ErrorReportingMode == EErrorReportingMode.ShowDialog)
				{
					if (errorDialogBox == null)
					{
						errorDialogBox = DialogBoxManager.instance.NotificationWindow("Something went wrong", "The game has encountered an unrecoverable error and needs to be restarted.\n\nPlease call your game master to restart the game.",
							OnErrorDialogDismissed, "Close Game");
						SubmitExceptionAnalytics(condition, stacktrace, "Fatal");
					}
				}
				else if (ErrorReportingMode == EErrorReportingMode.QuitInstantly)
				{
					Main.QuitGame();
				}
			}
			else if (type == LogType.Error)
			{
				SubmitExceptionAnalytics(condition, stacktrace, "Error"); 
			}
		}
	
		//Test behavior of Quit button (Dev)
		private void OnErrorDialogDismissed()
		{
			if (!Input.GetKey(KeyCode.RightShift))
			{
				Main.QuitGame();
			}
		}

		private void SubmitExceptionAnalytics(string condition, string stackTrace, string severity)
		{
			AdditionalDebugInfo debugInfo = new AdditionalDebugInfo();
			string dataMessage = "Message: " + condition + "\nStacktrace: " + stackTrace + "\nAdditional Info:" + debugInfo.FormatToString();

			delayedMessage = debugInfo.FormatToString();

			try
			{
				WWWForm errorEventData = new WWWForm();
				errorEventData.AddField("source", "Client");
				errorEventData.AddField("severity", severity);
				errorEventData.AddField("message", dataMessage);
				errorEventData.AddField("stack_trace", stackTrace);
				UnityWebRequest serverReport = UnityWebRequest.Post(Server.Url + Server.SubmitErrorEvent(), errorEventData);
				ServerCommunication.Instance.AddDefaultHeaders(serverReport);
				serverReport.SendWebRequest();
				while (!serverReport.isDone)
				{
					continue;
				}
			}
			catch (System.Exception)
			{
				// Ignore all exceptions since we are in the exception handler.
			}

			GLog.GLog.Event("Exception", dataMessage);
		}
	}
}
