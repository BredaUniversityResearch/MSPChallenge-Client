using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CradleImpactTool
{
	public class ModalManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		[SerializeField]
		Image m_iconImage;
		[SerializeField]
		Image m_closeButtonImage;
		[SerializeField]
		TMP_Text m_titleText;
		[SerializeField]
		TMP_Text m_bodyText;
		[SerializeField, Range(1, 20)]
		float m_moveSpeed;
		[SerializeField, Range(1, 20)]
		float m_appearSpeed;
		[SerializeField, Range(1, 20)]
		float m_disappearSpeed;
		RectTransform m_rectTransform;
		Camera m_camera;

		enum CurrentAnimation
		{
			None,
			IsAppearing,
			IsDisappearing
		}

		Vector2 m_startPosition;
		Vector2 m_targetPosition;

		[SerializeField, Range(0, 1)] // TODO: remove this
		float m_targetScale;
		[SerializeField, Range(0,1)] // TODO: remove this
		float m_movementProgress;
		[SerializeField, Range(0, 1)] // TODO: remove this
		float m_visibilityProgress;
		CurrentAnimation m_currentAnimation;
		[SerializeField] // TODO: remove this
		int m_showStack;

		void Reset(bool a_includingPosition = true)
		{
			m_showStack = 0;
			m_movementProgress = 0.0f;
			m_visibilityProgress = 0.0f;
			m_targetScale = 0.0f;
			m_currentAnimation = CurrentAnimation.None;

			if (a_includingPosition)
			{
				m_startPosition.Set(Screen.width / 2.0f, Screen.height / 2.0f);
				m_targetPosition = Vector3.zero;
			}
		}

		private void Awake()
		{
			Reset();

			m_rectTransform = GetComponent<RectTransform>();
			m_camera = Camera.main;
		}

		public void SetTargetPosition(Vector2 a_targetPosition)
		{
			m_targetPosition = a_targetPosition;
		}

		private string FormatText(string a_bodyText)
		{
			string resultString = "";
			for (int i = 0; i < a_bodyText.Length && i >= 0; )
			{
				int startIndex = a_bodyText.IndexOf('[', i);
				int endIndex = a_bodyText.IndexOf(']', startIndex);

				int modStartIndex = startIndex >= i ? startIndex : a_bodyText.Length;
				resultString += a_bodyText.Substring(i, modStartIndex - i);
				if (startIndex < 0 || endIndex < 0)
					break;

				startIndex += 2;
				i = endIndex + 2;

				string linkContents = a_bodyText.Substring(startIndex, endIndex - startIndex);

				startIndex = linkContents.StartsWith("wiki://") ? "wiki://".Length : 0;
				endIndex = linkContents.LastIndexOf("|");
				if (endIndex < 0)
					endIndex = linkContents.Length - 1;

				string link = linkContents.Substring(startIndex, endIndex - startIndex);
				string linkText = linkContents.Substring(endIndex + 1, linkContents.Length - endIndex - 1);
				if (string.IsNullOrWhiteSpace(linkText))
					linkText = link;

				resultString += $"<color=#ADD8E6><link=\"{link}\">{linkText}</link></color>";
			}

			return resultString;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_bodyText, eventData.position, m_camera);
			if (linkIndex != -1 || m_bodyText.textInfo.linkCount == 1)
			{
				TMP_LinkInfo linkInfo = m_bodyText.textInfo.linkInfo[Math.Max(0, linkIndex)];
				CradleGraphManager.InvokeWikiLinkClick(linkInfo.GetLinkID()); // Notify listeners that a link has been clicked
			}
		}

		public void Show(string a_bodyText, Vector2 a_position, Sprite a_icon = null, string a_titleText = "", bool a_showCloseButton = false)
		{
			gameObject.SetActive(true);

			m_showStack++;
			m_targetScale = 1.0f;

			bool isDifferentOrNotVisible = m_bodyText.text != a_bodyText ||
				m_titleText.text != a_titleText ||
				m_closeButtonImage.enabled != a_showCloseButton ||
				m_iconImage.sprite != a_icon ||
				m_visibilityProgress < 1.0f;

			m_bodyText.text = FormatText(a_bodyText);
			m_titleText.text = a_titleText;
			m_closeButtonImage.enabled = a_showCloseButton;
			m_currentAnimation = CurrentAnimation.IsAppearing;

			m_iconImage.enabled = a_icon != null;
			if (a_icon != null)
			{
				m_iconImage.sprite = a_icon;
			}

			// Only update the position if the contents are different
			if (isDifferentOrNotVisible)
			{
				m_targetPosition = a_position;
			}

			UpdatePosition();
		}

		public void KeepOpen()
		{
			gameObject.SetActive(true);

			m_showStack++;
			m_targetScale = 1.0f;
			m_currentAnimation = CurrentAnimation.IsAppearing;

			UpdatePosition();
		}

		public void Hide()
		{
			m_showStack--;
			if (m_showStack < 0)
			{
				m_showStack = 0;
			}

			m_targetScale = 0.0f;
			m_currentAnimation = CurrentAnimation.IsDisappearing;

			// If it wasn't visible, and nothing is showing the modal
			if (m_visibilityProgress <= 0.0f)
			{
				m_visibilityProgress = 0.0f;
				m_currentAnimation = CurrentAnimation.None;
				OnHidden();
			}
		}

		void OnHidden()
		{
			if (m_showStack == 0)
			{
				gameObject.SetActive(false);
				Reset();
			}
		}

		void Update()
		{
			if (m_currentAnimation == CurrentAnimation.IsAppearing && m_visibilityProgress < 1.0f)
			{
				m_visibilityProgress += Time.deltaTime * m_appearSpeed;
				if (m_visibilityProgress >= 1.0f)
				{
					m_visibilityProgress = 1.0f;
					m_currentAnimation = CurrentAnimation.None;
				}
			}
			else if (m_currentAnimation == CurrentAnimation.IsDisappearing && m_visibilityProgress > 0.0f)
			{
				m_visibilityProgress -= Time.deltaTime * m_disappearSpeed;
				if (m_visibilityProgress <= 0.0f)
				{
					m_visibilityProgress = 0.0f;
					m_currentAnimation = CurrentAnimation.None;
					OnHidden();
				}
			}

			if (m_movementProgress < 1.0f)
			{
				m_movementProgress += Time.deltaTime * m_moveSpeed;
				if (m_movementProgress > 1.0f)
				{
					m_movementProgress = 1.0f;
				}
			}

			UpdatePosition();
		}

		void UpdatePosition()
		{
			float targetScale = GetTargetScale();

			if (targetScale > float.Epsilon)
			{
				transform.position = Vector2.Lerp(m_startPosition, m_targetPosition, m_movementProgress);
			}
			else
			{
				transform.position = Vector3.zero;
			}

			float easeInCubic = m_visibilityProgress * m_visibilityProgress * m_visibilityProgress;
			transform.localScale = Vector3.one * Mathf.Lerp(0.0f, targetScale, easeInCubic);
		}

		float GetTargetScale()
		{
			return m_targetScale * (m_rectTransform.rect.height / Screen.safeArea.yMax) * 0.5f; // TODO: Remove magic number here
		}

		public Vector2 GetTargetSize()
		{
			return m_rectTransform.rect.size * GetTargetScale();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			KeepOpen();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Hide();
		}

		public Vector2 targetPosition { get { return m_targetPosition; } }
	}
}
