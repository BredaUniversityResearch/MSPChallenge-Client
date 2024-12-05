using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace MSP2050.Scripts
{
	public class KPICategoryPopulator: MonoBehaviour
	{
		[Serializable]
		private class KPIValueToggledEvent : UnityEvent<KPIValue, bool>
		{
		}

		[Serializable]
		private class KPIColorChangedEvent : UnityEvent<KPIValue, Color>
		{
		}
		[SerializeField] string m_targetSimulation;
		[SerializeField] RectTransform m_contentParent = null;
		[SerializeField] GameObject m_groupPrefab = null;
		[SerializeField] GameObject m_entryPrefab = null;

		[SerializeField] KPIGraphDisplay m_graphDisplay;
		[SerializeField] KPIValueProceduralColorScheme m_colorScheme = null;

		private Dictionary<string, KPIBar> kpiBarsByValueName = new Dictionary<string, KPIBar>(16); //Also includes the categories
		private Dictionary<string, KPIGroupDisplay> kpiCategoriesByCategoryName = new Dictionary<string, KPIGroupDisplay>(8);
		private KPIValueCollection targetCollection = null;
		private int targetTeamId = -1;

		private int displayMonth = -1;
		private bool automaticallyFollowLatestMonth = true;

		private void Awake()
		{
			if(Main.Instance.GameLoaded)
			{
				Initialise();
			}
			else
			{
				Main.Instance.OnPostFinishedLoadingLayers += Initialise;
			}
		}

		private void Initialise()
		{
			if (targetTeamId == -1)
			{
				if (SessionManager.Instance.CurrentTeam.IsManager)
				{
					//Setup target country for first team.
					foreach (Team team in SessionManager.Instance.GetTeams())
					{
						targetTeamId = team.ID;
						break;
					}
				}
				else
				{
					targetTeamId = SessionManager.Instance.CurrentTeam.ID;
				}
			}

			KPIValueCollection collection = SimulationManager.Instance.GetKPIValuesForSimulation(m_targetSimulation, targetTeamId);
			TimeManager.Instance.OnCurrentMonthChanged += OnCurrentMonthChanged;
			displayMonth = TimeManager.Instance.GetCurrentMonth();

			if (collection != null)
			{
				CreateValuesForCollection(collection);
				SetTargetCollection(collection);
			}
		}

		private void OnDestroy()
		{
			SetTargetCollection(null);
		}

		private void DestroyAllValueBars()
		{
			foreach (KPIBar bar in kpiBarsByValueName.Values)
			{
				Destroy(bar.gameObject);
			}

			kpiBarsByValueName.Clear();
		}

		//Callback to use with UnityEvents.
		public void DisplayValuesForMonth(int month)
		{
			displayMonth = month;
			automaticallyFollowLatestMonth = false;
			if (targetCollection != null)
			{
				UpdateDisplayValues(targetCollection, displayMonth);
			}
		}

		//Callback to use with UnityEvents
		public void DisplayValuesForTeam(int teamId)
		{
			if (targetTeamId != teamId)
			{
				List<string> toggledValueNames = null;
				if (targetCollection != null)
				{
					toggledValueNames = ClearActiveToggledValues();
				}

				targetTeamId = teamId;
				SetTargetCollection(SimulationManager.Instance.GetKPIValuesForSimulation(m_targetSimulation, targetTeamId));

				if (targetCollection != null)
				{
					if (toggledValueNames != null)
					{
						ToggleValuesOn(toggledValueNames);
					}
				}
			}
		}

		private void SetTargetCollection(KPIValueCollection newTarget)
		{
			if (targetCollection != null)
			{
				targetCollection.OnKPIValuesUpdated -= OnKPIValuesUpdated;
				targetCollection.OnKPIValueDefinitionsChanged -= OnKPIValueDefinitionsChanged;
			}

			targetCollection = newTarget;

			if (targetCollection != null)
			{
				UpdateDisplayValues(targetCollection, automaticallyFollowLatestMonth ? TimeManager.Instance.GetCurrentMonth() : displayMonth);
				//UpdateDisplayValues(targetCollection, automaticallyFollowLatestMonth ? targetCollection.MostRecentMonthReceived : displayMonth);
				targetCollection.OnKPIValuesUpdated += OnKPIValuesUpdated;
				targetCollection.OnKPIValueDefinitionsChanged += OnKPIValueDefinitionsChanged;
			}
		}

		private void OnCurrentMonthChanged(int oldCurrentMonth, int newCurrentMonth)
		{
			if (automaticallyFollowLatestMonth)
			{
				displayMonth = newCurrentMonth;
				UpdateDisplayValues(targetCollection, newCurrentMonth);
			}
		}

		private void OnKPIValueDefinitionsChanged(KPIValueCollection sourceCollection)
		{
			DestroyAllValueBars();
			CreateValuesForCollection(sourceCollection);
		}

		private void OnKPIValuesUpdated(KPIValueCollection sourceCollection, int previousMostRecentMonthReceived, int mostRecentMonthReceived)
		{
			//if (automaticallyFollowLatestMonth)
			//{
			//	displayMonth = mostRecentMonthReceived;
			//}

			if (displayMonth == mostRecentMonthReceived)
			{
				UpdateDisplayValues(sourceCollection, mostRecentMonthReceived);
			}
		}

		private void UpdateDisplayValues(KPIValueCollection valueCollection, int month)
		{
			if (kpiBarsByValueName.Count == 0)
			{
				return; //Prevent the game from logging warnings before the category has been initialized, as would happen with Energy on load.
			}

			HashSet<string> valuesToRemove = new HashSet<string>(kpiBarsByValueName.Keys);
			foreach (KPIValue value in valueCollection.GetValues())
			{
				valuesToRemove.Remove(value.name);

				KPIBar bar;
				if (!kpiBarsByValueName.TryGetValue(value.name, out bar))
				{
					if (kpiCategoriesByCategoryName.TryGetValue(value.owningCategoryName, out KPIGroupDisplay group))
					{
						bar = CreateKPIBar(m_entryPrefab, group.EntryParent, value);
					}
					else
					{
						Debug.LogWarning("Tried updating KPI Value " + value.name + " but a KPI bar could not be found for the value and the category has no group entry");
					}
				}

				bar.SetStartValue((float)(value.GetKpiValueForMonth(0) ?? value.GetKpiValueForMonth(-1) ?? 0f));
				bar.SetActual(value.GetKpiValueForMonth(month)/*, value.targetCountryId == KPIValue.CountryGlobal? 0 : value.targetCountryId*/);
			}

			foreach (KPICategory category in valueCollection.GetCategories()) //Categories also have entries
			{
				valuesToRemove.Remove(category.name);

				KPIBar bar;
				KPIGroupDisplay group = kpiCategoriesByCategoryName[category.name];
				if (!kpiBarsByValueName.TryGetValue(category.name, out bar))
				{
					bar = CreateKPIBar(m_entryPrefab, group.EntryParent, category);
				}
				bar.SetStartValue((float)(category.GetKpiValueForMonth(0) ?? category.GetKpiValueForMonth(-1) ?? 0f));
				bar.SetActual(category.GetKpiValueForMonth(month)/*, category.targetCountryId == KPIValue.CountryGlobal ? 0 : category.targetCountryId*/);
				bar.transform.SetAsLastSibling();
				group.PositionSeparator();
			}

			foreach (string valueToRemove in valuesToRemove)
			{
				if (kpiBarsByValueName.TryGetValue(valueToRemove, out KPIBar bar))
				{
					kpiBarsByValueName.Remove(valueToRemove);
					Destroy(bar.gameObject);
				}
				else
				{
					Debug.LogError("Could not find value bar for a value that we are supposed to remove...");
				}
			}
		}

		private void CreateValuesForCollection(KPIValueCollection collection)
		{
			if (collection != null)
			{
				foreach (KPICategory category in collection.GetCategories())
				{
					KPIGroupDisplay kpiGroup = Instantiate(m_groupPrefab, GetTargetContainerForCategory(category)).GetComponent<KPIGroupDisplay>();
					kpiGroup.SetName(category.displayName);

					kpiCategoriesByCategoryName.Add(category.name, kpiGroup);

					foreach (KPIValue value in category.GetChildValues())
					{
						CreateKPIBar(m_entryPrefab, kpiGroup.EntryParent, value);
					}
					CreateKPIBar(m_entryPrefab, kpiGroup.EntryParent, category);
					kpiGroup.PositionSeparator();
				}
			}
		}

		protected virtual RectTransform GetTargetContainerForCategory(KPICategory category)
		{
			return m_contentParent;
		}

		private KPIBar CreateKPIBar(GameObject prefab, Transform parent, KPIValue value)
		{
			KPIBar kpiBar = Instantiate(prefab, parent).GetComponent<KPIBar>();
			kpiBar.SetContent(value, ToggleGraph);
			kpiBarsByValueName.Add(value.name, kpiBar);
			return kpiBar;
		}

		private void ToggleGraph(bool isOnState, KPIBar targetBar)
		{
			KPIValue targetValue = targetCollection.FindValueByName(targetBar.ValueName);

			if (!isOnState)
			{
				Color graphColor = Color.white;
				targetBar.SetDisplayedGraphColor(graphColor);
			}

			if(m_graphDisplay != null)
			{
				if(!m_graphDisplay.ToggleGraph(targetValue, isOnState))
				{
					targetBar.SetGraphToggled(false);
				}
			}

			KPIValueProceduralColorScheme.Context context = new KPIValueProceduralColorScheme.Context();
			foreach (string toggledKPIValue in GetActiveToggledValues())
			{
				KPIValue value = targetCollection.FindValueByName(toggledKPIValue);
				Color newColor = GetCurrentKPIColor(value, context);
				if (kpiBarsByValueName.TryGetValue(value.name, out KPIBar kpiBar))
				{
					kpiBar.SetDisplayedGraphColor(newColor);
				}
				else
				{
					Debug.LogError("No KPI bar found for value: " + value.name);
				}

				if (m_graphDisplay != null)
				{
					m_graphDisplay.GraphColorChanged(value, newColor);
				}
			}
		}

		private Color GetCurrentKPIColor(KPIValue targetValue, KPIValueProceduralColorScheme.Context context)
		{
			Color graphColor;
			KPICategory category = targetCollection.FindCategoryByName(targetValue.owningCategoryName);
			if (category != null && category.kpiValueColorScheme == EKPIValueColorScheme.ProceduralColor)
			{
				graphColor = m_colorScheme.GetColor(context);
			}
			else
			{
				graphColor = targetValue.graphColor;
			}

			return graphColor;
		}

		private List<string> GetActiveToggledValues()
		{
			List<string> result = new List<string>(16);
			foreach (KPIBar value in kpiBarsByValueName.Values)
			{
				if (value.GraphToggled)
				{
					result.Add(value.ValueName);
				}
			}
			return result;
		}

		private List<string> ClearActiveToggledValues()
		{
			List<string> result = new List<string>(16);
			foreach (KPIBar value in kpiBarsByValueName.Values)
			{
				if (value.GraphToggled)
				{
					value.SetGraphToggled(false);
					result.Add(value.ValueName);
				}
			}

			return result;
		}

		private void ToggleValuesOn(List<string> valueNamesToToggle)
		{
			foreach (string valueName in valueNamesToToggle)
			{
				KPIBar bar;
				if (kpiBarsByValueName.TryGetValue(valueName, out bar))
				{
					bar.SetGraphToggled(true);
				}
			}
		}

	}
}

