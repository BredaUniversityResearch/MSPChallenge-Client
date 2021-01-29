using System;
using System.Collections;
using UnityEngine;

namespace AutomatedTest
{
	[RequireComponent(typeof(TeamImporter))]
	public class AutomatedTestGameRunner: MonoBehaviour
	{
		private const int ADMIN_COUNTRY_ID = 1;

		[SerializeField]
		private GameObject persistentObject = null;

		[SerializeField]
		private TeamImporter teamImporter = null;

		private void Awake()
		{
			DontDestroyOnLoad(this);

			UncaughtExceptionReporter.ErrorReportingMode = UncaughtExceptionReporter.EErrorReportingMode.QuitInstantly;

			Server.Host = "localhost";
			Server.Endpoint = "stable";
			teamImporter.ImportGlobalData();
			teamImporter.OnImportComplete += OnGlobalDataImportComplete;
			LayerImporter.OnDoneImporting += OnDoneImportingLayers;
		}

		private void OnGlobalDataImportComplete(bool success)
		{
			if (success)
			{
				LogIntoTeam();
			}
			else
			{
				Debug.LogError("Failed to import global data");
                Main.QuitGame();
            }
		}

		private void LogIntoTeam()
		{
			//If Successful load next scene
			GameObject tObj = Instantiate(persistentObject);

			tObj.GetComponent<PersistentDataLogIn>().Initialize(ADMIN_COUNTRY_ID, "AUTOMATED_TEST_USER", teamImporter.MspGlobalData, teamImporter.teams);
		}

		private void OnDoneImportingLayers()
		{
			StartCoroutine(TestRunToCompletion());
		}

		private IEnumerator TestRunToCompletion()
		{
			int secondsPerEra = 10;
			Debug.Log(string.Format("Setting era time to {0} seconds for all eras", secondsPerEra));
			TimeManager.instance.SetEraRealtimeValues(new[] {secondsPerEra, secondsPerEra, secondsPerEra, secondsPerEra});
			yield return new WaitForSeconds(3.0f);
			
			Debug.Log("Sending state PAUSE to end Setup phase");
			GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.Pause));
			while (GameState.CurrentState == GameState.PlanningState.Setup)
			{
				yield return new WaitForSeconds(1.0f);
			}

			yield return new WaitForSeconds(3.0f);

			Debug.Log("Sending state PLAY to start game");
			GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.Play));

			while (GameState.CurrentState != GameState.PlanningState.End)
			{
				yield return new WaitForSeconds(1.0f);
			}

            Main.QuitGame();
            yield return null;
		}
	}
}
