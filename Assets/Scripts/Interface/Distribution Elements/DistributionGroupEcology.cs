﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DistributionGroupEcology : DistributionGroup<DistributionItem>
	{
		private const string TITLE_FORMAT = "Effort {0:0} %";

		[Header("Ecology Specific")]
		[SerializeField]
		private TextMeshProUGUI effortText = null;

		private void Start()
		{
			//outlineCol = distributionFillBar.outline.color;
			Initialise();
		}

		public override void ApplySliderValues(Plan plan, int index)
		{
			string fleetName = SimulationLogicMEL.Instance.fishingFleets[index];
			if (plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData))
			{
				SetFishingToSliderValues(fishingData.fishingDistributionDelta, fleetName);
			}
			else
			{
				Debug.LogError("Cannot apply slider values to plan without fishing policy");
			}
		}

		public override void UpdateDistributionItem(DistributionItem updatedItem, float currentValue)
		{
			UpdateDistributionItem(updatedItem, currentValue, true);
		}

		private void UpdateDistributionItem(DistributionItem updatedItem, float currentValue, bool normalizeValues)
		{
			base.UpdateDistributionItem(updatedItem, currentValue);

			if (normalizeValues)
			{
				NormalizeValues(updatedItem, currentValue);
			}
		}

		private void NormalizeValues(DistributionItem updatedItem = null, float currentValue = 0.0f)
		{
			float totalSum = 0.0f;
			float otherItemsSum = 0.0f;
			foreach (DistributionItem memberItem in items)
			{
				float memberItemValue = memberItem.GetDistributionValue();
				totalSum += memberItemValue;
				if (memberItem != updatedItem)
				{
					otherItemsSum += memberItemValue;
				}
			}

			if (totalSum > 1.0f)
			{
				float remainingValue = FishingDistributionDelta.MaxSummedFishingValue - currentValue;
				float multiplier = (FishingDistributionDelta.MaxSummedFishingValue / otherItemsSum) * remainingValue;

				foreach (DistributionItem memberItem in items)
				{
					if (memberItem != updatedItem)
					{
						UpdateDistributionItem(memberItem, memberItem.GetDistributionValue() * multiplier, false);
					}
				}
			}

			UpdateDistributionTitle();
		}

		private void UpdateDistributionTitle()
		{
			float totalSum = 0.0f;
			foreach (DistributionItem memberItem in items)
			{
				float memberItemValue = memberItem.GetDistributionValue();
				totalSum += memberItemValue;
			}

			effortText.text = string.Format(TITLE_FORMAT, Mathf.Clamp01(totalSum) * 100.0f);
		}

		private void SetFishingToSliderValues(FishingDistributionDelta distribution, string fleetName)
		{
			foreach (DistributionItem item in items)
			{
				if (item.changed)
				{
					distribution.SetFishingValue(fleetName, item.Country, item.GetDistributionValue());
				}
			}
		}

		public override void SetSliderValues(Dictionary<int, float> planDeltaValues, Dictionary<int, float> initialValues)
		{
			distributionFillBar.DestroyAllFills();

			if (initialValues != null)
			{
				foreach (KeyValuePair<int, float> kvp in initialValues)
				{
					DistributionItem item = FindDistributionItemByCountryId(kvp.Key);

					//Create new or use existing
					if (item == null)
					{
						CreateItem(kvp.Key, 1.0f);
					}
					else
					{
						item.SetTeam(SessionManager.Instance.GetTeamByTeamID(kvp.Key));
					}
				}

				//Update distributions with new values
				foreach (KeyValuePair<int, float> kvp in initialValues)
				{
					DistributionItem item = FindDistributionItemByCountryId(kvp.Key); 
					item.SetOldValue(kvp.Value);

					float currentValue;
					if (planDeltaValues == null || !planDeltaValues.TryGetValue(kvp.Key, out currentValue))
					{
						currentValue = kvp.Value;
					}
					UpdateDistributionItem(item, currentValue, false);
				}

				distributionFillBar.CreateEmptyFill(FishingDistributionDelta.MaxSummedFishingValue, true);
			}

			NormalizeValues();
			SortItems();
			distributionFillBar.SortFills();
			UpdateDistributionTitle();
		}
	}
}
