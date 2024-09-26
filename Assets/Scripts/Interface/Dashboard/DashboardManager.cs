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
		

		static DashboardManager m_instance;
		public static DashboardManager Instance => m_instance;

		[SerializeField] string m_widgetAssetPath;
		[SerializeField] string m_categoriesAssetPath;

		[SerializeField] Button m_closeButton;
		[SerializeField] Button m_catalogueButton;
		[SerializeField] Button m_sortButton;

		[SerializeField] Transform m_widgetParent;
		[SerializeField] GameObject m_categoryTogglePrefab;
		[SerializeField] Transform m_categoryToggleParent;
		[SerializeField] ToggleGroup m_categoryToggleGroup;
		[SerializeField] TextMeshProUGUI m_categoryNameText;

		//Categories
		List<DashboardCategoryToggle> m_categoryToggles;
		DashboardCategory m_favoriteCategory;
		DashboardCategory m_currentCategory;
		bool m_catalogueOpen;

		//Widgets
		List<ADashboardWidget> m_loadedWidgets;
		Dictionary<DashboardCategory, DashboardWidgetLayout> m_catSelectedWidgets;
		List<ADashboardWidget> m_visibleWidgets = new List<ADashboardWidget>();

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
			m_sortButton.onClick.AddListener(Sort);
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
					m_catSelectedWidgets.Add(cat, new DashboardWidgetLayout(true));
				}
				else
				{
					foreach (var kvp in SimulationManager.Instance.Settings)
					{
						if (kvp.Key.Equals(cat.name))
						{
							AddCategoryToggle(cat);
							m_catSelectedWidgets.Add(cat, new DashboardWidgetLayout(false));
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
						AddFromCatalogue(widget);
					}
				}
            }
        }

		void AddCategoryToggle(DashboardCategory a_category)
		{
			DashboardCategoryToggle toggle = Instantiate(m_categoryTogglePrefab, m_categoryToggleParent).GetComponent<DashboardCategoryToggle>();
			toggle.Initialise(a_category, m_categoryToggleGroup, OnCategorySelected);
			m_categoryToggles.Add(toggle);
			if (a_category.m_favorite)
				toggle.transform.SetAsFirstSibling();
		}

		void AddFromCatalogue(ADashboardWidget a_widget)
		{
			ADashboardWidget instance = Instantiate(a_widget, m_widgetParent).GetComponent<ADashboardWidget>();
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
			if(a_favorite)
				m_catSelectedWidgets[m_favoriteCategory].AddWidget(a_widget);
			else
				m_catSelectedWidgets[m_favoriteCategory].Remove(a_widget);
		}

		public void ShowWidgetMovePreview(ADashboardWidget a_widget, PointerEventData a_data)
		{

		}

		public void OnWidgetMoveRelease(ADashboardWidget a_widget, PointerEventData a_data)
		{

		}
	}
}