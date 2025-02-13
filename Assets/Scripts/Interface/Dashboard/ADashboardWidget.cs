using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public abstract class ADashboardWidget : MonoBehaviour
	{
		[SerializeField] DashboardWidgetHeader m_header;
		[SerializeField] GameObject m_contentContainer;
		[SerializeField] int m_defaultW = 1;
		[SerializeField] int m_defaultH = 1;
		[SerializeField] int m_minW = 1;
		[SerializeField] int m_minH = 1;
		[SerializeField] Button m_addButton;
		[SerializeField] Button m_removeButton;
		[SerializeField] Toggle m_favouriteToggle;
		[SerializeField] Toggle m_optionsToggle;
		[SerializeField] ResizeHandle m_resizeHandle;
		[SerializeField] GameObject m_optionsWindowPrefab;
		[SerializeField] Transform m_optionsWindowParent;

		//Set in individual prefabs
		public DashboardCategory m_category;
		public bool m_startingWidget;

		public DashboardWidgetPosition m_position;
		public DashboardWidgetPosition m_favPosition;

		protected WidgetOptionsWindow m_optionsWindow;

		public int DefaultW => m_defaultW;
		public int DefaultH => m_defaultH;
		public int MinW => m_minW;
		public int MinH => m_minH;

		private void Awake()
		{
			m_header.m_onDragStart = OnDragStart;
			m_header.m_onDrag = OnDrag;
			m_header.m_onDragEnd = OnDragEnd;
			m_resizeHandle.onHandleDragged = HandleResize;
			m_optionsToggle.onValueChanged.AddListener(ToggleOptionsWindow);
		}

		public virtual void InitialiseCatalogue()
		{
			//Catalogue widget
			m_position = new DashboardWidgetPosition();
			m_position.SetSize(m_defaultW, m_defaultH);

			//Show right buttons
			m_addButton.gameObject.SetActive(true);
			m_removeButton.gameObject.SetActive(false);
			m_addButton.onClick.AddListener(AddFromCatalogue);
			m_favouriteToggle.gameObject.SetActive(false);
			UpdateData();
		}

		public virtual void Initialise(ADashboardWidget a_original)
		{
			//Regular widget
			m_position = new DashboardWidgetPosition();
			if(a_original != null)
				m_position.SetSize(a_original.m_position.W, a_original.m_position.H);
			else
				m_position.SetSize(m_defaultW, m_defaultH);

			//Show right buttons
			m_addButton.gameObject.SetActive(false);
			m_removeButton.gameObject.SetActive(true);
			m_removeButton.onClick.AddListener(RemoveWidget);
			m_favouriteToggle.gameObject.SetActive(true);
			m_favouriteToggle.onValueChanged.AddListener(OnFavouriteToggled);
			UpdateData();
		}

		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		public virtual void Show()
		{
			gameObject.SetActive(true);
		}

		public void OnDragStart(PointerEventData a_data)
		{
			DashboardManager.Instance.OnWidgetMoveStart(this, a_data);
			m_contentContainer.SetActive(false);
		}

		public void OnDrag(PointerEventData a_data)
		{
			DashboardManager.Instance.ShowWidgetMovePreview(this, a_data);
			transform.position = a_data.position;
		}

		public void OnDragEnd(PointerEventData a_data)
		{
			DashboardManager.Instance.OnWidgetMoveRelease(this, a_data);
			m_contentContainer.SetActive(true);
		}

		public void Reposition(bool a_favoriteLayout = false)
		{
			RectTransform rect = GetComponent<RectTransform>();
			DashboardWidgetPosition pos = a_favoriteLayout ? m_favPosition : m_position;
			rect.sizeDelta = new Vector2(pos.W * DashboardManager.Instance.m_cellsize, pos.H * DashboardManager.Instance.m_cellsize);
			rect.localPosition = new Vector3(pos.X * DashboardManager.Instance.m_cellsize, -pos.Y * DashboardManager.Instance.m_cellsize);
			OnSizeChanged(pos.W, pos.H);
		}

		void AddFromCatalogue()
		{
			DashboardManager.Instance.AddFromCatalogue(this);
		}

		void RemoveWidget()
		{
			DashboardManager.Instance.RemoveWidget(this);
			Destroy(gameObject);
		}

		void OnFavouriteToggled(bool a_value)
		{
			DashboardManager.Instance.SetWidgetAsFavorite(this, a_value);
		}

		void HandleResize(PointerEventData data, RectTransform handleRect, ResizeHandle.RescaleDirectionHor hor, ResizeHandle.RescaleDirectionVer ver)
		{
			DashboardManager.Instance.OnWidgetResize(this, data);
		}

		protected virtual void OnSizeChanged(int a_w, int a_h) { }
		public abstract void UpdateData();

		public int CompareTo(ADashboardWidget a_other, bool a_fav)
		{
			if(a_fav)
			{
				if(m_favPosition.Y == a_other.m_favPosition.Y)
					return m_favPosition.X.CompareTo(a_other.m_favPosition.X);
				return m_favPosition.Y.CompareTo(a_other.m_favPosition.Y);
			}
			if (m_position.Y == a_other.m_position.Y)
				return m_position.X.CompareTo(a_other.m_position.X);
			return m_position.X.CompareTo(a_other.m_position.X);
		}

		void ToggleOptionsWindow(bool a_value)
		{
			if (a_value)
			{
				m_optionsWindow = Instantiate(m_optionsWindowPrefab, m_optionsWindowParent).GetComponent<WidgetOptionsWindow>();
				PopulateOptions();
			}
			else
			{
				Destroy(m_optionsWindow.gameObject);
				m_optionsWindow = null;
			}
		}

		protected abstract void PopulateOptions();
	}
}