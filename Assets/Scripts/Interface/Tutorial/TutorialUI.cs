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
		[SerializeField] CanvasGroup m_fade;
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
		[SerializeField] private float m_fadeTime = 0.5f;

		enum EScreenPosition {Top, Center, Bottom}
		private EScreenPosition m_screenPosition = EScreenPosition.Center;


		public void Initialise(UnityAction a_nextButtonCallback, UnityAction a_previousButtonCallback, UnityAction a_quitButtonCallback)
		{
			m_nextButton.onClick.AddListener(a_nextButtonCallback);
			m_previousButton.onClick.AddListener(a_previousButtonCallback);
			m_titleContinueButton.onClick.AddListener(a_nextButtonCallback);
			m_titleQuitButton.onClick.AddListener(a_quitButtonCallback);
		}

		public void SetUIToTitle(string a_header, string a_content, string a_part, bool a_hasPreviousButton = true, bool m_hasNextButton = true)
		{
			StartCoroutine(SetUIToTitleFade(a_header, a_content, a_part, a_hasPreviousButton, m_hasNextButton));
		}

		IEnumerator SetUIToTitleFade(string a_header, string a_content, string a_part, bool a_hasPreviousButton = true, bool m_hasNextButton = true)
		{
			m_fade.interactable = false;
			yield return StartCoroutine(FadeOutCenter());
			m_screenPosition = EScreenPosition.Center;

			m_regularContainer.SetActive(false);
			m_titleContainer.SetActive(true);
			ClearGraphics();

			m_titleHeader.text = a_header;
			m_titleContent.text = a_content;
			m_titlePart.text = a_part;

			m_previousButton.gameObject.SetActive(a_hasPreviousButton);
			m_nextButton.gameObject.SetActive(m_hasNextButton);
			m_nextButton.interactable = true;
			m_titleContinueButton.gameObject.SetActive(false);
			m_titleQuitButton.gameObject.SetActive(false);

			m_fade.interactable = true;
			StartCoroutine(FadeIn());
		}

		public void SetUIToTitle(string a_header, string a_content, string a_part, string a_continueButtonText, string a_quitButtonText, bool a_hasPreviousButton)
		{
			StartCoroutine(SetUIToTitleFade(a_header, a_content, a_part, a_continueButtonText, a_quitButtonText, a_hasPreviousButton));

		}

		IEnumerator SetUIToTitleFade(string a_header, string a_content, string a_part, string a_continueButtonText, string a_quitButtonText, bool a_hasPreviousButton)
		{
			m_fade.interactable = false;
			yield return StartCoroutine(FadeOutCenter());
			m_screenPosition = EScreenPosition.Center;

			m_regularContainer.SetActive(false);
			m_titleContainer.SetActive(true);
			ClearGraphics();

			m_titleHeader.text = a_header;
			m_titleContent.text = a_content;
			m_titlePart.text = a_part;

			m_titleContinueButton.gameObject.SetActive(!string.IsNullOrEmpty(a_continueButtonText));
			m_previousButton.gameObject.SetActive(a_hasPreviousButton);
			m_titleQuitButton.gameObject.SetActive(!string.IsNullOrEmpty(a_quitButtonText));
			m_nextButton.gameObject.SetActive(false);

			m_titleQuitButtonText.text = a_quitButtonText;
			m_titleContinueButtonText.text = a_continueButtonText;

			m_fade.interactable = true;
			StartCoroutine(FadeIn());
		}

		public void SetUIToRegular(string a_header, string a_content, bool a_hasRequirements, bool a_alignTop, GameObject a_graphicPrefab, bool a_preComplete)
		{
			
			if(a_alignTop)
			{
				StartCoroutine(SetUIToRegularFade(a_header, a_content, a_hasRequirements, a_alignTop, a_graphicPrefab, a_preComplete, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f)));
			}
			else
			{
				StartCoroutine(SetUIToRegularFade(a_header, a_content, a_hasRequirements, a_alignTop, a_graphicPrefab, a_preComplete, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f)));
			}
		}

		IEnumerator SetUIToRegularFade(string a_header, string a_content, bool a_hasRequirements, bool a_alignTop, GameObject a_graphicPrefab, bool a_preComplete,
			Vector2 a_targetAnchorMin, Vector2 a_targetAnchorMax, Vector2 a_targetPivot)
		{
			m_fade.interactable = false;
			float timePassed = 0f;
			bool moving = (a_alignTop && m_screenPosition == EScreenPosition.Bottom) || (!a_alignTop && m_screenPosition == EScreenPosition.Top);
			if (!moving)
			{
				Vector2 originalAnchorMin = m_contentRect.anchorMin;
				Vector2 originalAnchorMax = m_contentRect.anchorMax;
				Vector2 originalSizeDelta = m_contentRect.sizeDelta;
				Vector2 originalPivot = m_contentRect.pivot;

				//Fade out
				while (true)
				{
					yield return 0;
					timePassed += Time.deltaTime;
					if (timePassed >= m_fadeTime)
					{
						break;
					}

					float t = timePassed / m_fadeTime;
					m_fade.alpha = 1f - t;
					m_contentRect.anchorMin = Vector2.Lerp(originalAnchorMin, a_targetAnchorMin, t);
					m_contentRect.anchorMax = Vector2.Lerp(originalAnchorMax, a_targetAnchorMax, t);
					m_contentRect.sizeDelta = Vector2.Lerp(originalSizeDelta, new Vector2(0f, m_regularHeight), t);
					m_contentRect.pivot = Vector2.Lerp(originalPivot, a_targetPivot, t);
				}

				m_fade.alpha = 0f;
			}
			else
			{
				yield return StartCoroutine(MoveOut(a_alignTop ? new Vector2(0f, -m_regularHeight) : new Vector2(0f, m_regularHeight)));
				m_contentRect.anchoredPosition = a_alignTop ? new Vector2(0f, m_regularHeight) : new Vector2(0f, -m_regularHeight);
			}

			m_contentRect.anchorMin = a_targetAnchorMin;
			m_contentRect.anchorMax = a_targetAnchorMax;
			m_contentRect.sizeDelta = new Vector2(0f, m_regularHeight);
			m_contentRect.pivot = a_targetPivot;

			m_screenPosition = a_alignTop ? EScreenPosition.Top : EScreenPosition.Bottom;
			m_regularContainer.SetActive(true);
			m_titleContainer.SetActive(false);
			ClearGraphics();

			m_regularHeader.text = a_header;
			m_regularContent.text = a_content;

			if (a_hasRequirements)
			{
				m_regularCheckbox.SetActive(true);
				SetRequirementChecked(a_preComplete);
			}
			else
			{
				m_regularCheckbox.SetActive(false);
				m_nextButton.interactable = true;
			}

			if (a_graphicPrefab != null)
				Instantiate(a_graphicPrefab, m_regularGraphicsParent);

			m_fade.interactable = true;

			if (!moving)
			{
				StartCoroutine(FadeIn());
			}
			else
			{
				StartCoroutine(MoveIn());
			}
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

		IEnumerator FadeOutCenter()
		{
			float timePassed = 0f;
			Vector2 originalAnchorMin = m_contentRect.anchorMin;
			Vector2 originalAnchorMax = m_contentRect.anchorMax;
			Vector2 originalSizeDelta = m_contentRect.sizeDelta;

			//Fade out
			while (true)
			{
				yield return 0;
				timePassed += Time.deltaTime;
				if (timePassed >= m_fadeTime)
				{
					break;
				}

				float t = timePassed / m_fadeTime;
				m_fade.alpha = 1f - t;
				m_contentRect.anchorMin = Vector2.Lerp(originalAnchorMin, Vector2.zero, t);
				m_contentRect.anchorMax = Vector2.Lerp(originalAnchorMax, Vector2.one, t);
				m_contentRect.sizeDelta = Vector2.Lerp(originalSizeDelta, Vector2.zero, t);
			}

			m_fade.alpha = 0f;
			m_contentRect.anchorMin = Vector2.zero;
			m_contentRect.anchorMax = Vector2.one;
			m_contentRect.sizeDelta = Vector2.zero;
		}

		IEnumerator FadeIn()
		{
			float timePassed = 0f;
			while (true)
			{
				yield return 0;
				timePassed += Time.deltaTime;
				if (timePassed >= m_fadeTime)
				{
					break;
				}
				m_fade.alpha = timePassed / m_fadeTime;
			}
			m_fade.alpha = 1f;
		}

		IEnumerator MoveOut(Vector2 a_targetOffset)
		{
			float timePassed = 0f;
			while (true)
			{
				yield return 0;
				timePassed += Time.deltaTime;
				if (timePassed >= m_fadeTime)
				{
					break;
				}
				m_contentRect.anchoredPosition = Vector2.Lerp(Vector2.zero, a_targetOffset, timePassed / m_fadeTime);
			}
			m_contentRect.anchoredPosition = a_targetOffset;
		}

		IEnumerator MoveIn()
		{
			float timePassed = 0f;
			Vector2 originalPosition = m_contentRect.anchoredPosition;
			while (true)
			{
				yield return 0;
				timePassed += Time.deltaTime;
				if (timePassed >= m_fadeTime)
				{
					break;
				}
				m_contentRect.anchoredPosition = Vector2.Lerp(originalPosition, Vector2.zero, timePassed / m_fadeTime);

			}
			m_contentRect.anchoredPosition = Vector2.zero;
		}

	}
}
