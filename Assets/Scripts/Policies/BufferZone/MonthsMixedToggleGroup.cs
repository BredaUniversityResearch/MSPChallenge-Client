using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class MonthsMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue[] m_monthToggles;

		public Action<bool, int> m_monthChangedCallback;
		bool m_ignoreCallback;

		public bool? CombinedValue
		{
			set
			{
				m_ignoreCallback = true;
				for (int i = 0; i < m_monthToggles.Length; i++)
				{
					m_monthToggles[i].Value = value;
				}
				m_ignoreCallback = false;
			}
			get
			{
				if (!m_monthToggles[0].Value.HasValue)
				{
					return null;
				}
				bool reference = m_monthToggles[0].Value.Value;
				for (int i = 1; i < m_monthToggles.Length; i++)
				{
					if (!m_monthToggles[i].Value.HasValue || m_monthToggles[i].Value.Value != reference)
					{
						return null;
					}
				}
				return reference;
			}
		}

		private void Start()
		{
			for (int i = 0; i < m_monthToggles.Length; i++)
			{
				int month = i;
				m_monthToggles[i].m_onValueChangeCallback = (b) => OnMonthToggleChanged(b, month);
			}
		}

		void OnMonthToggleChanged(bool a_newValue, int a_month)
		{
			if (m_ignoreCallback)
				return;

			m_monthChangedCallback.Invoke(a_newValue, a_month);
		}

		public bool? SetValue(List<Months> a_months)
		{
			if (a_months.Count == 0)
			{
				CombinedValue = false;
				return false;
			}
			else
			{
				m_ignoreCallback = true;
				bool? totalValue = null;
				for (int i = 0; i < m_monthToggles.Length; i++)
				{
					bool? monthValue = a_months[0].MonthSet(i);//TODO: is Month+1 needed here?
					for (int j = 1; j < a_months.Count; j++)
					{
						if (a_months[j].MonthSet(i) != monthValue.Value)//TODO: is Month+1 needed here?
						{
							monthValue = null;
							break;
						}
					}
					m_monthToggles[i].Value = monthValue;
					if (i == 0)
						totalValue = monthValue;
					else if (!totalValue.HasValue || !monthValue.HasValue || totalValue.Value != monthValue.Value)
						totalValue = null;
				}
				m_ignoreCallback = false;
				return totalValue;
			}
		}
	}
}
