using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Distribution : MonoBehaviour
{
    /*  Distribution structure:
     *  
     *  Distribution
     *      X DistributionGroup (for every fleet/grid)
     *          1-2 DistributionFillBar (2 for energy, 1 for others)
     *              X DistributionFill (for every relevant country)
     *          X DistributionItem (for every relevant country)
     *              1 DistributionSlider OR 1 DistributionSliderCentered
     *          0-1 DistributionEnergySources (energy only)
     */

    public enum Expertise { Layers, Shipping, Energy, Ecology }
	public Expertise expertise;
	
	[Header("Prefabs")]
	public GameObject groupPrefab;
	public Transform contentLocation;
	private List<AbstractDistributionGroup> groups = new List<AbstractDistributionGroup>();
	public int NumberGroups => groups == null ? 0 : groups.Count;

	bool interactable = false;

	void Start()
	{
        // Demo
        switch (expertise)
        {
            case Expertise.Layers:
                break;
            case Expertise.Shipping:
                break;
            case Expertise.Energy:
                break;
            case Expertise.Ecology:
                break;
        }
	}

	public AbstractDistributionGroup CreateGroup(string groupName)
	{
		// Generate item
		GameObject go = Instantiate(groupPrefab);
		AbstractDistributionGroup group = go.GetComponent<AbstractDistributionGroup>();
		groups.Add(group);
		go.transform.SetParent(contentLocation, false);

        group.SetName(groupName);
		group.parent = this;

		return group;
	}

	public void DestroyAllGroups()
	{
		for (int i = 0; i < groups.Count; ++i)
		{
			Destroy(groups[i].gameObject);
		}
		groups.Clear();
	}

	private AbstractDistributionGroup CreateGroup(EnergyGrid grid, EnergyGrid.GridPlanState state)
	{
		// Generate item
		AbstractDistributionGroup group = CreateGroup(grid.name);
		group.SetSliderValues(grid, state);
		group.SetSliderInteractability(interactable);
		return group;
	}


	public void SetInteractability(bool interactable)
	{
		SetSliderInteractability(interactable);
	}

	public void SetSliderValuesToFishingDistribution(FishingDistributionSet fishingDistributionBeforePlan, FishingDistributionDelta planDeltaSet)
	{
		if (groups == null || groups.Count == 0)
		{
			foreach (string fishingFleet in PlanManager.fishingFleets)
			{
				CreateGroup(fishingFleet);
			}
		}

		if (fishingDistributionBeforePlan != null && planDeltaSet != null)
		{
			for (int i = 0; i < PlanManager.fishingFleets.Count; i++)
			{
				string fleetName = PlanManager.fishingFleets[i];
				Dictionary<int, float> deltaValues = planDeltaSet.FindValuesForFleet(fleetName);
				Dictionary<int, float> unchangedValues = fishingDistributionBeforePlan.FindValuesForFleet(fleetName);
				if (unchangedValues != null)
				{
					groups[i].SetSliderValues(deltaValues, unchangedValues);
				}
				else
				{
					Debug.LogError("Could not find fishing fleet values for fleet " + fleetName + ". Have the initial fishing values been setup correctly and loaded?");
				}
			}
		}
		else
		{
			Debug.LogError("Got NULL fishing distribution or deltaSet for SetSliderValuesToFishingDistribution");
		}
	}

	public void SetSliderValuesToEnergyDistribution(Plan plan, List<EnergyGrid> energyDistribution)
	{
		if (energyDistribution == null || energyDistribution.Count == 0)
		{
			Debug.Log("No energy grids in distribution");
			foreach (AbstractDistributionGroup group in groups)
				GameObject.Destroy(group.gameObject);
			groups = new List<AbstractDistributionGroup>();
			return;
		}

        int j = 0;
		for (int i = 0; i < energyDistribution.Count; i++)
		{
			EnergyGrid.GridPlanState state = energyDistribution[i].GetGridPlanStateAtPlan(plan);
			if (state == EnergyGrid.GridPlanState.Hidden || (!Main.EditingPlanDetailsContent && state == EnergyGrid.GridPlanState.Normal))
				continue;

			if (j < groups.Count)
				groups[j].SetSliderValues(energyDistribution[i], state); //Set existing ones
			else
				CreateGroup(energyDistribution[i], state); //Create new ones if required
			j++;
		}

		for (int x = groups.Count - 1; x >= j; x--)
		{
			GameObject.Destroy(groups[x].gameObject);
			groups.RemoveAt(x); //Delete ones that aren't neccessary        
		}
	}

	/// <summary>
	/// Puts the values set in the distribution sliders back into the energy grids they represent.
	/// Also duplicates older grids to the new plan if they were changed.
	/// </summary>
	public void SetGridsToSliderValues(Plan plan)
	{
		foreach (AbstractDistributionGroup group in groups)
			group.ApplySliderValues(plan, -1);
	}

	/// <summary>
	/// Puts the values set in the distribution sliders back into the plan's fishing.
	/// </summary>
	public void SetFishingToSliderValues(Plan plan)
	{
		plan.fishingDistributionDelta.Clear();
		for (int i = 0; i < PlanManager.fishingFleets.Count; i++)
		{			
			groups[i].ApplySliderValues(plan, i);
		}
	}

	private void SetSliderInteractability(bool value)
	{
		if (interactable == value)
			return;

		interactable = value;
		foreach (AbstractDistributionGroup group in groups)
			group.SetSliderInteractability(value);
	}
}
