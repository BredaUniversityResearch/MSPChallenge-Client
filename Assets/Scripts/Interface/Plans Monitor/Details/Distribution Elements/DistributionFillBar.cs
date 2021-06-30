using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DistributionFillBar : MonoBehaviour
{
	public const int EMPTY_FILL_TEAM_ID = -1;
	private const float LAYOUT_ELEMENT_WIDTH_SCALE_FACTOR = 1000.0f; //Layout elements don't like to work with values < 1

	[SerializeField]
	private GameObject fillPrefab = null;
	[SerializeField]
	private Transform contentLocation = null;
	public Image outline;

	private List<DistributionFill> fills = new List<DistributionFill>();
	private bool fillsRequireUpdate = false;
	private float totalFillSize = -1.0f;

	private Dictionary<int, DistributionFill> fillsDict = new Dictionary<int, DistributionFill>();

	private void Update()
	{
		if (fillsRequireUpdate)
		{
			UpdateFillSizes();

			fillsRequireUpdate = false;
		}
	}

	private void UpdateFillSizes()
	{
		float sum = 0.0f;
		foreach (DistributionFill fill in fills)
		{
			if (fill.TeamID != EMPTY_FILL_TEAM_ID || totalFillSize < 0.0f)
			{
				sum += fill.CurrentValue;
			}
		}

		//If there's a total fill size set, update the empty fill to be the remaining size.
		if (totalFillSize > 0.0f)
		{
			float remainingFill = totalFillSize - sum;
			DistributionFill fill;
			if (fillsDict.TryGetValue(EMPTY_FILL_TEAM_ID, out fill))
			{
				fill.CurrentValue = remainingFill;
			}

			sum = totalFillSize;
		}

		float scaleFactor = (1.0f / sum) * LAYOUT_ELEMENT_WIDTH_SCALE_FACTOR;
		foreach (DistributionFill fill in fills)
		{
			fill.SetFillRelativeSize(fill.CurrentValue * scaleFactor);
		}
	}

	private DistributionFill CreateFill(int teamID)
	{
        Color teamColor = teamID > 0 ? TeamManager.GetTeamByTeamID(teamID).color : Color.white;

		GameObject go = Instantiate(fillPrefab);
		DistributionFill fill = go.GetComponent<DistributionFill>();
		fills.Add(fill);
		fillsDict.Add(teamID, fill);
		go.transform.SetParent(contentLocation, false);

        fill.TeamID = teamID;
		fill.SetColor(teamColor);

		return fill;
	}

	public void DestroyFill(int team)
	{
        DistributionFill fill = fillsDict[team];

        fills.Remove(fill);
        fillsDict.Remove(team);
		Destroy(fill.gameObject);
	}

	public void DestroyAllFills()
	{
		foreach (DistributionFill fill in fills)
			Destroy(fill.gameObject);
		fills.Clear();
        fillsDict.Clear();
	}

    public DistributionFill SetFill(int team, float value)
    {
        DistributionFill fill;
        fillsDict.TryGetValue(team, out fill);
		if (fill == null)
		{
			fill = CreateFill(team);
		}
		else
		{
			fill.gameObject.SetActive(true);
		}

		fill.CurrentValue = value;
		fillsRequireUpdate = true;
		return fill;
	}

	//Sets the total capacity of this fill bar and adds an empty fill in to cover the empty space.
	public void CreateEmptyFill(float totalAmount, bool visible)
	{
		DistributionFill fill = SetFill(EMPTY_FILL_TEAM_ID, totalAmount);
		fill.SetVisible(visible);

		totalFillSize = totalAmount;
	}

	/// <summary>
	/// Sorts the fills based on color
	/// </summary>
	public void SortFills()
	{
		List<DistributionFill> sortedFills = new List<DistributionFill>(fills);
		sortedFills.Sort((lhs, rhs) =>
		{
			//Make sure any entry that's set to a negative team id (empty fills) are always set to last
			if (lhs.TeamID < 0 && rhs.TeamID > 0)
				return 1;
			if (rhs.TeamID < 0 && lhs.TeamID > 0)
				return -1;
			return lhs.TeamID.CompareTo(rhs.TeamID);
		});

		for (int i = 0; i < sortedFills.Count; ++i)
		{
			sortedFills[i].transform.SetSiblingIndex(i);
		}

		// Set player color first
		for (int i = 0; i < fills.Count; i++)
		{
			if (fills[i].TeamID == TeamManager.CurrentUserTeamID)
			{
				fills[i].transform.SetAsFirstSibling();
			}
		}
	}
}