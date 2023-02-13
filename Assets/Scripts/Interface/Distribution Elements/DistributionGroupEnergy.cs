using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DistributionGroupEnergy : MonoBehaviour
	{
		[Title("Header")]
		[SerializeField] Toggle m_headerToggleBar;
		[SerializeField] Image m_changeStateImage;
		[SerializeField] CustomInputField m_gridNameField;
		[SerializeField] CustomButton m_viewGridButton;
		[SerializeField] Sprite m_addedSprite, m_removedSprite, m_changedSprite, m_greenEnergySprite, m_greyEnergySprite;
		[SerializeField] Image m_greenGreyEnergyImage;
		[SerializeField] TextMeshProUGUI m_totalPowerText;

		[SerializeField] ValueConversionCollection valueConversionCollection = null;

		[Title("Content")]
		[SerializeField] TextMeshProUGUI m_sourcesHeaderText;
		[SerializeField] Transform m_teamBallParent;
		[SerializeField] GameObject m_teamBallPrefab;
		[SerializeField] Transform m_productionEntryParent;
		[SerializeField] GameObject m_productionEntryPrefab;
		[SerializeField] Transform m_socketEntryParent;
		[SerializeField] GameObject m_socketEntryPrefab;

		List<DistributionEnergyProductionEntry> m_productionEntries = new List<DistributionEnergyProductionEntry>();
		List<DistributionEnergySocketEntry> m_socketEntries = new List<DistributionEnergySocketEntry>();
		List<Image> m_teamBallEntries = new List<Image>();

		long m_totalSourcePower;
		long allSocketMaximum;
		EnergyGrid m_energyGrid;
		EnergyGrid.GridPlanState originalGridState;
		bool ignoreDistributionUpdate = false;
		bool changed;
		bool interactable = false;

		private void Start()
		{
			m_gridNameField.onValueChanged.AddListener((a) => CheckIfChanged());
			m_viewGridButton.onClick.AddListener(() =>
			{
				//TODO: fade UI out until mouse move (was in plansmonitor FadeUntilMouseMove)
				m_energyGrid.HighlightSockets();
				CameraManager.Instance.ZoomToBounds(m_energyGrid.GetGridRect());
			});
		}

		public void ApplySliderValues(Plan plan)
		{
			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				if (entry.Changed)
				{
					if (m_energyGrid.plan.ID != plan.ID) //Older distribution was changed: duplicate it to the new plan
						m_energyGrid = PolicyLogicEnergy.DuplicateEnergyGridToPlan(m_energyGrid, plan);
					long old = 0L;
					if (m_energyGrid.energyDistribution.distribution.TryGetValue(entry.Team.ID, out var oldExpected))
					{
						old = oldExpected.expected;
					}
					else
					{
						Debug.LogWarning($"Trying to get an old energy value from a distribution that doesn't contain the team: {entry.Team.ID}. Grid persisid: {m_energyGrid.persistentID}. New value: {entry.CurrentValue}");
					}
					if (entry.CurrentValue < 0)
					{
						if (old < 0)
							m_energyGrid.sharedPower -= entry.CurrentValue - old;
						else
							m_energyGrid.sharedPower -= entry.CurrentValue;
					}
					else if (old < 0)
					{
						m_energyGrid.sharedPower += old;
					}
					m_energyGrid.energyDistribution.distribution[entry.Team.ID].expected = entry.CurrentValue;
				}
			}
			if (m_gridNameField.text != m_energyGrid.name)
			{
				if (m_energyGrid.plan.ID != plan.ID) //Older distribution was changed: duplicate it to the new plan
				{
					m_energyGrid = PolicyLogicEnergy.DuplicateEnergyGridToPlan(m_energyGrid, plan);
					m_energyGrid.name = m_gridNameField.text;
				}
				else
				{
					m_energyGrid.name = m_gridNameField.text;
					//m_energyGrid.SubmitName();//TODO CHECK: this should not happen here, but in submit
				}
			}
		}

		public void SetName(string name)
		{
			m_gridNameField.text = name;
		}

		public void SetGrid(EnergyGrid grid, EnergyGrid.GridPlanState state, ToggleGroup a_toggleGroup, GridEnergyDistribution a_oldDistribution)
		{
			m_headerToggleBar.group = a_toggleGroup;
			m_energyGrid = grid;
			originalGridState = state;
			allSocketMaximum = grid.maxCountryCapacity;
			m_totalSourcePower = grid.sourcePower;
			m_greenGreyEnergyImage.sprite = grid.IsGreen ? m_greenEnergySprite : m_greyEnergySprite; 
			m_gridNameField.interactable = (state != EnergyGrid.GridPlanState.Removed);
			SetName(grid.name);

			int nextProductionEntryIndex = 0;
			int nextSocketEntryIndex = 0;
			int nextTeamBallIndex = 0;

			ignoreDistributionUpdate = true; //Ignore distr updates while creating (it sorts, messing up order)
			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in grid.energyDistribution.distribution)
			{
				Team team = SessionManager.Instance.GetTeamByTeamID(kvp.Key);
				long oldValue = kvp.Value.expected;
				if(a_oldDistribution != null)
				{
					if(a_oldDistribution.distribution.TryGetValue(kvp.Key, out var oldCountryValue))
						oldValue = oldCountryValue.expected;
				}

				//Socket entry
				if (kvp.Value.maximum > 0)
				{
					if (nextSocketEntryIndex < m_socketEntries.Count)
					{
						 m_socketEntries[nextSocketEntryIndex].SetContent(team, kvp.Value.maximum, allSocketMaximum, kvp.Value.expected, oldValue, this, interactable);
					}
					else
					{
						DistributionEnergySocketEntry socketEntry = Instantiate(m_socketEntryPrefab, m_socketEntryParent).GetComponent<DistributionEnergySocketEntry>();
						m_socketEntries.Add(socketEntry);
						socketEntry.SetContent(team, kvp.Value.maximum, allSocketMaximum, kvp.Value.expected, oldValue, this, interactable);
					}
					nextSocketEntryIndex++;
				}

				//Source production entry
				if(kvp.Value.sourceInput > 0)
				{
					if (nextProductionEntryIndex < m_productionEntries.Count)
					{
						m_productionEntries[nextProductionEntryIndex].SetContent(team, valueConversionCollection.ConvertUnit(kvp.Value.sourceInput, ValueConversionCollection.UNIT_WATT).FormatAsString());
					}
					else
					{
						DistributionEnergyProductionEntry productionEntry = Instantiate(m_productionEntryPrefab, m_productionEntryParent).GetComponent<DistributionEnergyProductionEntry>();
						m_productionEntries.Add(productionEntry);
						productionEntry.SetContent(team, valueConversionCollection.ConvertUnit(kvp.Value.sourceInput, ValueConversionCollection.UNIT_WATT).FormatAsString());
					}
					//m_totalSourcePower += kvp.Value.sourceInput;
					nextProductionEntryIndex++;
				}

				//Team ball
				if (nextTeamBallIndex < m_teamBallEntries.Count)
				{
					m_teamBallEntries[nextTeamBallIndex].color = team.color;
					m_teamBallEntries[nextTeamBallIndex].gameObject.SetActive(true);
				}
				else
				{
					Image teamBall = Instantiate(m_teamBallPrefab, m_teamBallParent).GetComponent<Image>();
					m_teamBallEntries.Add(teamBall);
					teamBall.color = team.color;
				}
				nextTeamBallIndex++;
			}
			ignoreDistributionUpdate = false;

			m_sourcesHeaderText.text = nextProductionEntryIndex == 0 ? "This grid contains no energy sources" : "Energy sources";
			//Disable unused items
			for (int i = nextProductionEntryIndex; i < m_productionEntries.Count; i++)
			{
				m_productionEntries[i].gameObject.SetActive(false);
			}
			for (int i = nextSocketEntryIndex; i < m_socketEntries.Count; i++)
			{
				m_socketEntries[i].gameObject.SetActive(false);
			}
			for (int i = nextTeamBallIndex; i < m_teamBallEntries.Count; i++)
			{
				m_teamBallEntries[i].gameObject.SetActive(false);
			}

			UpdateStateIndicator(false, true);
			UpdateEntireDistribution();
			gameObject.SetActive(true);
		}

		private void UpdateStateIndicator(bool hasChanged, bool force = false)
		{
			if (m_changeStateImage == null)
				return;

			if (originalGridState == EnergyGrid.GridPlanState.Normal)
			{
				if (hasChanged)
				{
					m_changeStateImage.gameObject.SetActive(true);
					m_changeStateImage.sprite = m_changedSprite;
				}
				else
				{
					m_changeStateImage.gameObject.SetActive(false);
				}
			}
			else if (force)
			{
				switch (originalGridState)
				{
					case EnergyGrid.GridPlanState.Added:
						m_changeStateImage.gameObject.SetActive(true);
						m_changeStateImage.sprite = m_addedSprite;
						m_changeStateImage.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);
						break;
					case EnergyGrid.GridPlanState.Removed:
						m_changeStateImage.gameObject.SetActive(true);
						m_changeStateImage.sprite = m_removedSprite;
						m_changeStateImage.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 16f);
						break;
					case EnergyGrid.GridPlanState.Changed:
						m_changeStateImage.gameObject.SetActive(true);
						m_changeStateImage.sprite = m_changedSprite;
						m_changeStateImage.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 16f);
						break;
				}
			}
		}

		bool CheckIfChanged()
		{
			changed = false;
			foreach(var socketEntry in m_socketEntries)
			{
				if(socketEntry.Changed)
				{
					changed = true;
					break;
				}
			}
			changed = changed || m_gridNameField.text != m_energyGrid.name;
			UpdateStateIndicator(changed);
			return changed;
		}

		public void SetInteractability(bool value)
		{
			interactable = originalGridState != EnergyGrid.GridPlanState.Removed && value;
			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				entry.SetInteractability(interactable);
			}
			if (m_gridNameField != null)
			{
				m_gridNameField.interactable = interactable;
			}
		}

		public void UpdateEntireDistribution()
		{
			if (ignoreDistributionUpdate)
				return;

			//Total input
			long totalInput = 0;
			long totalOutput = 0;
			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				if (entry.CurrentValue < 0)
				{
					totalInput -= entry.CurrentValue; //Is a negative value
				}
				else
					totalOutput += entry.CurrentValue;
			}
			totalInput += m_totalSourcePower;
			m_totalPowerText.text = valueConversionCollection.ConvertUnit(totalInput, ValueConversionCollection.UNIT_WATT).FormatAsString();

			//Update sliders with new remaining power
			long remaining = totalInput-totalOutput;
			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				entry.SetRemainingPower(remaining);
			}

			//If remaining was negative, sliders will automatically be reduced to fit, so recalculate output and update once (there won't be changes this time)
			totalOutput = 0;
			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				if (entry.CurrentValue > 0)
					totalOutput += entry.CurrentValue;
			}
			remaining = totalInput - totalOutput;

			foreach (DistributionEnergySocketEntry entry in m_socketEntries)
			{
				entry.SetRemainingPower(remaining);
			}
			CheckIfChanged();
		}
	}
}
