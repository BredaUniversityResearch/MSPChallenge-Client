using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class EraDropdown : MonoBehaviour
{
	private class EraEntry
	{
		public int eraDeadlineMonth;
		public int dropdownIndex;
	}

	[SerializeField]
	private CustomDropdown dropdown = null;

	[SerializeField]
	private bool enableAllErasOption = false;

	private List<EraEntry> eraEntries = new List<EraEntry>(8);

	public TMP_Dropdown.DropdownEvent OnValueChanged
	{
		get
		{
			return dropdown.onValueChanged;
		}
	}

	private void Awake()
	{
		if (Main.MspGlobalData != null)
		{
			PopulateOptions();
		}
		else
		{
			Main.OnGlobalDataLoaded += OnGlobalDataLoaded;
		}

	}

	private void OnGlobalDataLoaded()
	{
		Main.OnGlobalDataLoaded -= OnGlobalDataLoaded;
		PopulateOptions();
	}

	private void PopulateOptions()
	{
		dropdown.ClearOptions();

		if (enableAllErasOption)
		{
			CreateDropdownEntry("All Eras", -1);
		}

		int numEras = MspGlobalData.num_eras;
		for (int i = 0; i < numEras; ++i)
		{
			int deadlineMonth = (Main.MspGlobalData.era_total_months * (i + 1));
			CreateDropdownEntry(Util.MonthToYearText(deadlineMonth), deadlineMonth); 
		}

		dropdown.RefreshShownValue();
	}

	private void CreateDropdownEntry(string entryText, int eraDeadline)
	{
		EraEntry entry = new EraEntry
		{
			eraDeadlineMonth = eraDeadline,
			dropdownIndex = dropdown.options.Count
		};
		dropdown.options.Add(new TMP_Dropdown.OptionData(entryText));
		eraEntries.Add(entry);
		eraEntries.Sort((lhs, rhs) => rhs.eraDeadlineMonth.CompareTo(lhs.eraDeadlineMonth));
	}

	public int GetSelectedMonth()
	{
		//+1 to indicate that we are targeting the end month. So Era 0 translates to month 120 (10 years)
		return GetEraEntryForDropdownIndex(dropdown.value).eraDeadlineMonth;
	}

	public void SetSelectedMonth(int eraMonth)
	{
		EraEntry entry = FindEraEntryForDeadlineMonth(eraMonth);
		dropdown.value = entry != null ? entry.dropdownIndex : 0;
	}

	private EraEntry GetEraEntryForDropdownIndex(int index)
	{
		EraEntry entry = eraEntries.Find(obj => obj.dropdownIndex == index);
		if (entry == null)
		{
			throw new Exception("Unknown dropdown index " + index + " for dropdown entries in EraDropdown");
		}

		return entry;
	}

	private EraEntry FindEraEntryForDeadlineMonth(int eraMonth)
	{
		EraEntry result = null;
		foreach (EraEntry entry in eraEntries)
		{
			if (entry.eraDeadlineMonth <= eraMonth)
			{
				result = entry;
				break;
			}
		}

		return result;
	}

	public void Reset()
	{
		dropdown.value = 0;
	}
}