using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class TutorialUI : MonoBehaviour
	{
		[SerializeField] private GameObject m_titleContainer;
		[SerializeField] private TextMeshProUGUI m_titleHeader;
		[SerializeField] private TextMeshProUGUI m_titleContent;
		[SerializeField] private TextMeshProUGUI m_titlePart;
		[SerializeField] private Button m_titleNextButton;
		[SerializeField] private Button m_titlePreviousButton;


		[SerializeField] private RectTransform m_regularContainer;
		[SerializeField] private Transform m_regularGraphicsParent;
		[SerializeField] private TextMeshProUGUI m_regularHeader;
		[SerializeField] private TextMeshProUGUI m_regularContent;
		[SerializeField] private Button m_regularNextButton;
		[SerializeField] private Button m_regularPreviousButton;
		[SerializeField] private GameObject m_regularCheckbox;
		[SerializeField] private GameObject m_regularCheckmark;


		public void Initialise(UnityAction a_nextButtonCallback, UnityAction a_previousButtonCallback)
		{
			m_regularNextButton.onClick.AddListener(a_nextButtonCallback);
			m_regularPreviousButton.onClick.AddListener(a_previousButtonCallback);
			m_titleNextButton.onClick.AddListener(a_nextButtonCallback);
			m_titlePreviousButton.onClick.AddListener(a_previousButtonCallback);
		}

		public void SetUIToTitle(string a_header, string a_content, string a_part, bool a_hasPreviousButton = true, bool m_hasNextButton = true)
		{
			//TODO
		}
		
		public void SetUIToRegular(string a_header, string a_content, bool a_hasRequirements, bool a_alignTop, GameObject a_graphicPrefab = null)
		{
			//TODO
		}

		public void SetRequirementChecked(bool a_checked)
		{
			//TODO
		}
	}
}
