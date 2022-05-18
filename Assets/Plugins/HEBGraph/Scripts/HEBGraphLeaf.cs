using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ColourPalette;
using UnityEditor;
using UnityEngine.Events;

namespace HEBGraph
{
	public class HEBGraphLeaf : MonoBehaviour
	{
		const float SINGLE_RADIAL_OFFSET = 1f;
		const float DOUBLE_RADIAL_OFFSET = 1.7f;

		[SerializeField] TextMeshProUGUI m_text;
		[SerializeField] Toggle m_toggle;
		[SerializeField] CustomToggleColorSet m_bgColourSet;
		[SerializeField] UIRadialSegmentDrawer m_bgImage;
		[SerializeField] Image m_arrowImage;
		[SerializeField] ColourAsset m_defaultHLColour;
		[SerializeField] Button m_infoButton;

		public UnityEvent<string> m_onInfoTextChange;

		RectTransform m_rectTransform;
		Vector2 m_position;
		float m_radialOffset;	//Weighted position within graph
		int m_id;				//id from config
		HEBGraphBranch m_parent;
		bool m_ignoreToggleCallback;
		HEBGraph m_root;
		List<HEBGraphDataLink> m_links;
		HEBGraphDataLink m_currentShowingLink;
		bool m_leftSide;
		bool m_multiline;
		string m_link;
		

		public Vector2 Position => m_position;
		public float RadialOffset => m_radialOffset;
		public int ID => m_id;
		public HEBGraphBranch Parent => m_parent;
		public List<HEBGraphDataLink> Links => m_links;

		public float Initialise(float a_currentRadialOffset, HEBGraphDataEntry a_data, HEBGraph a_root, HEBGraphBranch a_parent)
		{
			m_root = a_root;
			if(string.IsNullOrEmpty(a_data.link))
			{
				m_infoButton.gameObject.SetActive(false);
			}
			else
			{
				m_link = a_data.link;
				m_infoButton.onClick.AddListener(OnInfoButtonClick);
			}
			m_parent = a_parent;
			m_rectTransform = GetComponent<RectTransform>();
			m_text.text = a_data.name;
			m_id = a_data.id;
			m_multiline = a_data.multiline;
			m_links = new List<HEBGraphDataLink>();
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
			if (m_multiline)
			{
				m_radialOffset = a_currentRadialOffset + DOUBLE_RADIAL_OFFSET * 0.5f;
				return DOUBLE_RADIAL_OFFSET;
			}
			else
			{
				m_radialOffset = a_currentRadialOffset + SINGLE_RADIAL_OFFSET * 0.5f;
				return SINGLE_RADIAL_OFFSET;
			}
		}

		public void AddLink(HEBGraphDataLink a_link)
		{
			m_links.Add(a_link);
		}

		public void SetPosition(float a_totalEntries)
		{
			float theta = m_radialOffset / a_totalEntries * Mathf.PI * 2f;
			m_position = new Vector2(Mathf.Sin(theta) / 2f + 0.5f, Mathf.Cos(theta) / 2f + 0.5f);
			m_leftSide = m_position.x < 0.5f;
			if (m_leftSide)
			{
				m_rectTransform.pivot = new Vector2(1f, 0.5f);
				m_rectTransform.rotation = Quaternion.Euler(0, 0, 270f - Mathf.Rad2Deg * theta);

				m_text.alignment = TextAlignmentOptions.Right;
				RectTransform textRect = m_text.GetComponent<RectTransform>();
				textRect.offsetMin = new Vector2(16f, 0f);
				textRect.offsetMax = new Vector2(0f, 0f);

				RectTransform arrowRect = m_arrowImage.GetComponent<RectTransform>();
				arrowRect.anchorMax = new Vector2(1f, 1f);
				arrowRect.anchorMin = new Vector2(1f, 0f);
				arrowRect.anchoredPosition = new Vector2(12f, 0f);

				RectTransform bgRect = m_bgImage.GetComponent<RectTransform>();
				bgRect.pivot = new Vector2(1f, 0.5f);
				bgRect.localScale = new Vector3(-1f, 1f, 1f);

				RectTransform infoRect = m_infoButton.GetComponent<RectTransform>();
				infoRect.pivot = new Vector2(0f, 0.5f);
				infoRect.anchorMax = new Vector2(0f, 0.5f);
				infoRect.anchorMin = new Vector2(0f, 0.5f);
				infoRect.anchoredPosition = new Vector2(4f, 0f);

			}
			else
			{
				m_rectTransform.rotation = Quaternion.Euler(0, 0, 90f - Mathf.Rad2Deg * theta);
			}
			m_rectTransform.anchorMin = m_position;
			m_rectTransform.anchorMax = m_position;
			m_rectTransform.anchoredPosition = Vector2.zero;
			if (m_multiline)
			{
				m_bgImage.SetRange(DOUBLE_RADIAL_OFFSET / a_totalEntries * Mathf.PI * 2f);
			}
			else
			{
				m_bgImage.SetRange(SINGLE_RADIAL_OFFSET / a_totalEntries * Mathf.PI * 2f);
			}
		}

		void OnToggleChanged(bool a_value)
		{
			if (m_ignoreToggleCallback)
				return;

			if (a_value)
				m_root.OnLeafSelected(this);
			else
				m_root.ClearLeafSelection();
		}

		public void Deselect()
		{
			m_ignoreToggleCallback = true;
			m_toggle.isOn = false;
			m_ignoreToggleCallback = false;
		}

		public void HighlightLink(HEBGraphDataLink a_link, ColourAsset a_color, ColourAsset a_colorHL)
		{
			m_currentShowingLink = a_link;
			m_arrowImage.gameObject.SetActive(true);
			m_bgColourSet.colorNormal = a_color;
			m_bgColourSet.colorHighlight = a_colorHL;
			m_bgImage.color = a_color.GetColour();
			m_arrowImage.color = a_color.GetColour();
			if (m_leftSide)
				m_arrowImage.transform.localScale = a_link.fromId == m_id ? Vector3.one : new Vector3(-1f, 1f, 1f);
			else
				m_arrowImage.transform.localScale = a_link.fromId == m_id ? new Vector3(-1f, 1f, 1f) : Vector3.one;
			m_onInfoTextChange.Invoke(a_link.description);
		}

		public void ClearHighlight()
		{
			m_currentShowingLink = null;
			m_arrowImage.gameObject.SetActive(false);
			m_bgColourSet.colorNormal = new ConstColour(Color.clear);
			m_bgImage.color = Color.clear;
			m_bgColourSet.colorHighlight = m_defaultHLColour;
			m_onInfoTextChange.Invoke("");
		}

		void OnInfoButtonClick()
		{
			m_root.m_linkClickCallback.Invoke(m_link);
		}
	}
}
