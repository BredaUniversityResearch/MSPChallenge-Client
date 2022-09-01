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
		[SerializeField] RectTransform m_contentRect;
		[SerializeField] private GameObject m_titleContainer;
		[SerializeField] private TextMeshProUGUI m_titleHeader;
		[SerializeField] private TextMeshProUGUI m_titleContent;
		[SerializeField] private TextMeshProUGUI m_titlePart;
		[SerializeField] private Button m_titleContinueButton;
		[SerializeField] private Button m_titleQuitButton;
		[SerializeField] private TextMeshProUGUI m_titleContinueButtonText;
		[SerializeField] private TextMeshProUGUI m_titleQuitButtonText;

		[SerializeField] private GameObject m_regularContainer;
		[SerializeField] private Transform m_regularGraphicsParent;
		[SerializeField] private TextMeshProUGUI m_regularHeader;
		[SerializeField] private TextMeshProUGUI m_regularContent;
		[SerializeField] private GameObject m_regularCheckbox;
		[SerializeField] private GameObject m_regularCheckmark;
		[SerializeField] float m_regularHeight;

		[SerializeField] private Button m_nextButton;
		[SerializeField] private Button m_previousButton;


		public void Initialise(UnityAction a_nextButtonCallback, UnityAction a_previousButtonCallback, UnityAction a_quitButtonCallback)
		{
			m_nextButton.onClick.AddListener(a_nextButtonCallback);
			m_previousButton.onClick.AddListener(a_previousButtonCallback);
			m_titleContinueButton.onClick.AddListener(a_nextButtonCallback);
			m_titleQuitButton.onClick.AddListener(a_quitButtonCallback);
		}

		public void SetUIToTitle(string a_header, string a_content, string a_part, bool a_hasPreviousButton = true, bool m_hasNextButton = true)
		{
			m_regularContainer.SetActive(false);
			m_titleContainer.SetActive(true);
			ClearGraphics();

			m_titleHeader.text = a_header;
			m_titleContent.text = a_content;
			m_titlePart.text = a_part;
			m_previousButton.gameObject.SetActive(a_hasPreviousButton);
			m_nextButton.gameObject.SetActive(a_hasPreviousButton);
			m_nextButton.interactable = true;
			m_titleContinueButton.gameObject.SetActive(false);
			m_titleQuitButton.gameObject.SetActive(false);

			m_contentRect.anchorMin = Vector2.zero;
			m_contentRect.anchorMax = Vector2.one;
			m_contentRect.sizeDelta = Vector2.zero;
		}

		public void SetUIToTitle(string a_header, string a_content, string a_part, string a_continueButtonText, string a_quitButtonText)
		{
			m_regularContainer.SetActive(false);
			m_titleContainer.SetActive(true);
			ClearGraphics();

			m_titleHeader.text = a_header;
			m_titleContent.text = a_content;
			m_titlePart.text = a_part;
			m_nextButton.gameObject.SetActive(false);
			m_nextButton.interactable = true;
			m_titleContinueButton.gameObject.SetActive(!string.IsNullOrEmpty(a_continueButtonText));
			m_previousButton.gameObject.SetActive(string.IsNullOrEmpty(a_quitButtonText));
			m_titleQuitButton.gameObject.SetActive(!string.IsNullOrEmpty(a_quitButtonText));
			m_titleQuitButtonText.text = a_quitButtonText;
			m_titleContinueButtonText.text = a_continueButtonText;

			m_contentRect.anchorMin = Vector2.zero;
			m_contentRect.anchorMax = Vector2.one;
			m_contentRect.sizeDelta = Vector2.zero;
		}

		public void SetUIToRegular(string a_header, string a_content, bool a_hasRequirements, bool a_alignTop, GameObject a_graphicPrefab = null)
		{
			m_regularContainer.SetActive(true);
			m_titleContainer.SetActive(false);
			ClearGraphics();

			m_regularHeader.text = a_header;
			m_regularContent.text = a_content;

			if (a_hasRequirements)
			{
				m_regularCheckbox.SetActive(true);
				SetRequirementChecked(false);
			}
			else
			{
				m_regularCheckbox.SetActive(false);
				m_nextButton.interactable = true;
			}

			if(a_alignTop)
			{
				m_contentRect.anchorMin = new Vector2(0f, 1f);
				m_contentRect.anchorMax = Vector2.one;
				m_contentRect.pivot = new Vector2(0.5f, 1f);
				m_contentRect.sizeDelta = new Vector2(0f, m_regularHeight);
			}
			else
			{
				m_contentRect.anchorMin = Vector2.zero;
				m_contentRect.anchorMax = new Vector2(1f, 0f);
				m_contentRect.pivot = new Vector2(0.5f, 0f);
				m_contentRect.sizeDelta = new Vector2(0f, m_regularHeight);
			}

			if (a_graphicPrefab != null)
				Instantiate(a_graphicPrefab, m_regularGraphicsParent);
		}

		public void SetRequirementChecked(bool a_checked)
		{
			m_regularCheckmark.SetActive(a_checked);
			m_nextButton.interactable = a_checked;
		}

		void ClearGraphics()
		{
			foreach (Transform child in m_regularGraphicsParent)
			{
				Destroy(child.gameObject);
			}
		}
	}
}
