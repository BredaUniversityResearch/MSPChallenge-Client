using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class DistributionEnergyProductionEntry : MonoBehaviour
	{
		[SerializeField] Image m_teamBall;
		[SerializeField] TextMeshProUGUI m_teamName;
		[SerializeField] TextMeshProUGUI m_energyAmount;

		public void SetContent(Team a_team, string a_energyAmount)
		{
			m_teamBall.color = a_team.color;
			m_teamName.text = a_team.name;
			m_energyAmount.text = a_energyAmount; //TODO: add name of generating types
			gameObject.SetActive(true);
		}
	}
}
