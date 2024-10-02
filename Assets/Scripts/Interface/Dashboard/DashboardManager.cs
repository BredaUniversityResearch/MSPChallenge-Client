using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class DashboardManager : MonoBehaviour
	{
		public const float cellsize = 300f;

		static DashboardManager m_instance;
		public static DashboardManager Instance => m_instance;

		[SerializeField] string m_widgetAssetPath;
		[SerializeField] string m_categoriesAssetPath;

		[SerializeField] Button m_closeButton;
		[SerializeField] Button m_catalogueButton;
		[SerializeField] Button m_sortButton;

		[SerializeField] RectTransform m_widgetParent;
		[SerializeField] GameObject m_categoryTogglePrefab;
		[SerializeField] Transform m_categoryToggleParent;
		[SerializeField] ToggleGroup m_categoryToggleGroup;
		[SerializeField] TextMeshProUGUI m_categoryNameText;
		[SerializeField] RectTransform m_rowInsertPreview;

		//Categories
		List<DashboardCategoryToggle> m_categoryToggles;
		DashboardCategory m_favoriteCategory;
		DashboardCategory m_currentCategory;
		bool m_catalogueOpen;

		//Widgets
		List<ADashboardWidget> m_loadedWidgets;
		Dictionary<DashboardCategory, DashboardWidgetLayout> m_catSelectedWidgets;
		List<ADashboardWidget> m_visibleWidgets = new List<ADashboardWidget>();
		int m_numberColumns = 5; // TODO: determine this based on screen size

		private void Awake()
		{
			m_instance = this;
			if (SimulationManager.Instance.Initialised)
			{
				InitialiseDashboard();
			}
			else
				SimulationManager.Instance.m_onSimulationsInitialised += InitialiseDashboard;
			m_closeButton.onClick.AddListener(CloseDashboard);
			m_catalogueButton.onClick.AddListener(OpenCatalogue);
			//m_sortButton.onClick.AddListener(Sort);
		}

		void CloseDashboard()
		{
			gameObject.SetActive(false);
		}

		void InitialiseDashboard()
		{
			//Load dashboard categories & widgets
			GameObject[] widgetObjs = Resources.LoadAll<GameObject>(m_widgetAssetPath);
			DashboardCategory[] categories = Resources.LoadAll<DashboardCategory>(m_categoriesAssetPath);

			m_categoryToggles = new List<DashboardCategoryToggle>();
			m_catSelectedWidgets = new Dictionary<DashboardCategory, DashboardWidgetLayout>();
			m_loadedWidgets = new List<ADashboardWidget>();

			foreach (var cat in categories)
			{
				if (cat.m_favorite)
				{
					m_favoriteCategory = cat;
					AddCategoryToggle(cat);
					m_catSelectedWidgets.Add(cat, new DashboardWidgetLayout(true, m_numberColumns));
				}
				else
				{
					foreach (var kvp in SimulationManager.Instance.Settings)
					{
						if (kvp.Key.Equals(cat.m_name, StringComparison.InvariantCultureIgnoreCase))
						{
							AddCategoryToggle(cat);
							m_catSelectedWidgets.Add(cat, new DashboardWidgetLayout(false, m_numberColumns));
							break;
						}
					}
				}
			}

            foreach (GameObject widgetObj in widgetObjs)
            {
				ADashboardWidget widget = widgetObj.GetComponent<ADashboardWidget>();

				if (widget != null)
				{
					m_loadedWidgets.Add(widget);
					if (widget.m_startingWidget && m_catSelectedWidgets.ContainsKey(widget.m_category))
					{
						AddFromCatalogue(widget, false);
					}
				}
            }
			m_categoryToggles[0].ForceActive();
		}

		void AddCategoryToggle(DashboardCategory a_category)
		{
			DashboardCategoryToggle toggle = Instantiate(m_categoryTogglePrefab, m_categoryToggleParent).GetComponent<DashboardCategoryToggle>();
			toggle.Initialise(a_category, m_categoryToggleGroup, OnCategorySelected);
			m_categoryToggles.Add(toggle);
			if (a_category.m_favorite)
				toggle.transform.SetAsFirstSibling();
		}

		public void AddFromCatalogue(ADashboardWidget a_widget, bool a_copyPosition = true)
		{
			ADashboardWidget instance = Instantiate(a_widget, m_widgetParent).GetComponent<ADashboardWidget>();
			instance.Initialise(a_copyPosition ? a_widget : null);
			instance.gameObject.SetActive(false);
			m_catSelectedWidgets[a_widget.m_category].AddWidget(instance);
		}

		void OnCategorySelected(DashboardCategory a_category)
		{
			ClearVisibleWidgets();
			SetWidgetsToCategory(a_category);
			m_catalogueButton.interactable = !a_category.m_favorite;
		}

		void ClearVisibleWidgets()
		{
			if (m_catalogueOpen)
			{
				foreach (ADashboardWidget widget in m_visibleWidgets)
				{
					Destroy(widget.gameObject);
				}
			}
			else
			{
				foreach (ADashboardWidget widget in m_visibleWidgets)
				{
					widget.Hide();
				}
			}
			m_visibleWidgets = new List<ADashboardWidget>();
			m_catalogueOpen = false;
		}

		void SetWidgetsToCategory(DashboardCategory a_category)
		{
			m_currentCategory = a_category;
			m_categoryNameText.text = a_category.m_displayName;
			foreach(ADashboardWidget widget in m_catSelectedWidgets[a_category].Widgets)
			{
				widget.Show();
				m_visibleWidgets.Add(widget);
			}
		}

		void OpenCatalogue()
		{
			ClearVisibleWidgets();
			m_catalogueOpen = true;
			m_categoryNameText.text = $"{m_currentCategory.m_displayName} widget catalogue";
			foreach (ADashboardWidget widget in m_loadedWidgets)
			{
				if(widget.m_category == m_currentCategory)
				{
					ADashboardWidget instance = Instantiate(widget.gameObject, m_widgetParent).GetComponent<ADashboardWidget>();
					instance.Initialise();
					m_visibleWidgets.Add(instance);
				}
			}
			Sort();
		}

		void Sort()
		{
			//TODO
		}

		public void SetWidgetAsFavorite(ADashboardWidget a_widget, bool a_favorite)
		{
			if (a_favorite)
				m_catSelectedWidgets[m_favoriteCategory].AddWidget(a_widget);
			else
			{
				m_catSelectedWidgets[m_favoriteCategory].Remove(a_widget);
				a_widget.Hide();
			}
		}

		public void OnWidgetMoveStart(ADashboardWidget a_widget)
		{
			m_catSelectedWidgets[m_currentCategory].Remove(a_widget, true);

		}

		public void ShowWidgetMovePreview(ADashboardWidget a_widget, PointerEventData a_data)
		{
			var pos = GetPointerPosition(a_data);
			DashboardWidgetPosition layout = m_currentCategory.m_favorite ? a_widget.m_favPosition : a_widget.m_position;
			if (m_catSelectedWidgets[m_currentCategory].WidgetFitsAt(a_widget, pos.x, pos.y, layout.W, layout.H, out int maxW, out int maxH))
			{
				//Show placed preview
				a_widget.RepositionToPreview(pos.x, pos.y, maxW, maxH);
				a_widget.SetContentActive(true);
				m_rowInsertPreview.gameObject.SetActive(false);
			}
			else if(m_catSelectedWidgets[m_currentCategory].WidgetInsertRowPossible(a_widget, pos.y, pos.x, layout.W, out int maxRowW))
			{
				//Show above preview
				a_widget.SetContentActive(false);
				m_rowInsertPreview.gameObject.SetActive(true);
				m_rowInsertPreview.sizeDelta = new Vector2(maxRowW * DashboardManager.cellsize, 0f);
				m_rowInsertPreview.localPosition = new Vector3(pos.x * DashboardManager.cellsize, -pos.y * DashboardManager.cellsize);
			}
			else
			{
				//Widget can't fit
				a_widget.SetContentActive(false);
				m_rowInsertPreview.gameObject.SetActive(false);
				//TODO: cross on preview?
			}
		}

		public void OnWidgetMoveRelease(ADashboardWidget a_widget, PointerEventData a_data)
		{
			m_rowInsertPreview.gameObject.SetActive(false);
			var pos = GetPointerPosition(a_data);
			DashboardWidgetPosition layout = m_currentCategory.m_favorite ? a_widget.m_favPosition : a_widget.m_position;
			if (m_catSelectedWidgets[m_currentCategory].WidgetFitsAt(a_widget, pos.x, pos.y, layout.W, layout.H, out int maxW, out int maxH))
			{
				//Move to available position
				m_catSelectedWidgets[m_currentCategory].MoveWidget(a_widget, pos.x, pos.y, maxW, maxH);
			}
			else if (m_catSelectedWidgets[m_currentCategory].WidgetInsertRowPossible(a_widget, pos.y, pos.x, layout.W, out int maxRowW))
			{
				//Create new rows to fit
				m_catSelectedWidgets[m_currentCategory].MoveWidgetAboveRow(a_widget, pos.x, pos.y, maxRowW);
			}
			else
			{
				//Widget can't fit; insert into old position
				m_catSelectedWidgets[m_currentCategory].InsertWidget(a_widget);
			}
		}

		(int x, int y) GetPointerPosition(PointerEventData a_data)
		{
			int x = 0, y = 0;
			if(RectTransformUtility.ScreenPointToLocalPointInRectangle(m_widgetParent, a_data.position, a_data.enterEventCamera, out Vector2 localPos))
			{
				x = (int)(localPos.x / cellsize);
				y = (int)(-localPos.y / cellsize);
			}
			if (y < 0) 
				y = 0;
			if(x < 0) 
				x = 0;
			if (x >= m_numberColumns)
				x = m_numberColumns - 1;
			return (x, y);
		}
	}
}