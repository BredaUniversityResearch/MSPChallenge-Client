using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Sirenix.Utilities;

namespace MSP2050.Scripts
{
	public class DashboardManager : MonoBehaviour
	{

		static DashboardManager m_instance;
		public static DashboardManager Instance => m_instance;

		[SerializeField] string m_widgetAssetPath;
		[SerializeField] string m_categoriesAssetPath;
		[SerializeField] int m_numberColumns = 10; 
		[SerializeField] DashboardColourList m_colourList;

		[SerializeField] Button m_closeButton;
		[SerializeField] Button m_catalogueButton;

		[SerializeField] RectTransform m_widgetParent;
		[SerializeField] GameObject m_categoryTogglePrefab;
		[SerializeField] Transform m_categoryToggleParent;
		[SerializeField] ToggleGroup m_categoryToggleGroup;
		[SerializeField] TextMeshProUGUI m_categoryNameText;
		[SerializeField] RectTransform m_rowInsertPreview;
		[SerializeField] RectTransform m_movePreview;
		[SerializeField] DashboardCategory m_otherCategory;
		[SerializeField] ADashboardWidget m_genericOtherWidgetPrefab;

		//Categories
		List<DashboardCategoryToggle> m_categoryToggles;
		DashboardCategory m_favoriteCategory;
		DashboardCategory m_currentCategory;

		//Catalogue
		//DashboardWidgetLayout m_catalogueLayout;
		bool m_showingCatalogue;
		[SerializeField] VerticalLayoutGroup m_catalogueParent;
		List<ADashboardWidget> m_catalogueWidgets;

		//Widgets
		public float m_cellsize = 150f;
		List<ADashboardWidget> m_loadedWidgets;
		Dictionary<DashboardCategory, DashboardWidgetLayout> m_catSelectedWidgets;

		public DashboardColourList ColourList => m_colourList;

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
			HandleResolutionOrScaleChange();
		}


		public void HandleResolutionOrScaleChange()
		{
			m_cellsize = Mathf.Round((Screen.width -36f) * InterfaceCanvas.Instance.canvas.scaleFactor / m_numberColumns);
			if (m_showingCatalogue)
			{
				foreach (ADashboardWidget widget in m_catalogueWidgets)
					widget.Reposition(false, true);
			}
			else if (m_currentCategory != null)
			{
				m_catSelectedWidgets[m_currentCategory].RepositionAllWidgets();
				m_widgetParent.sizeDelta = new Vector2(0f, m_cellsize * m_catSelectedWidgets[m_currentCategory].Rows);
			}
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
			if(SimulationManager.Instance.TryGetSettings(SimulationManager.OTHER_SIM_NAME, out var rawSettings))
			{
				SimulationSettingsOther settings = (SimulationSettingsOther)rawSettings;
				foreach(KPICategoryDefinition catDef in settings.kpis)
				{
					ADashboardWidget widget = CreateGenericWidget(catDef);
					m_loadedWidgets.Add(widget);
					AddFromCatalogue(widget, false);
				}
			}
			m_catalogueParent.gameObject.SetActive(false);
			m_categoryToggles[0].ForceActive();
			OnCategorySelected(m_catSelectedWidgets.GetFirstKey());
		}

		void AddCategoryToggle(DashboardCategory a_category)
		{
			DashboardCategoryToggle toggle = Instantiate(m_categoryTogglePrefab, m_categoryToggleParent).GetComponent<DashboardCategoryToggle>();
			toggle.Initialise(a_category, m_categoryToggleGroup, OnCategorySelected);
			m_categoryToggles.Add(toggle);
			if (a_category.m_favorite)
				toggle.transform.SetAsFirstSibling();
		}

		public void AddFromCatalogue(ADashboardWidget a_widget, bool a_copySize = true)
		{
			ADashboardWidget instance = Instantiate(a_widget, m_widgetParent).GetComponent<ADashboardWidget>();
			instance.Initialise(a_copySize ? a_widget : null);
			instance.gameObject.SetActive(false);
			m_catSelectedWidgets[a_widget.m_category].AddWidget(instance);		
		}

		ADashboardWidget CreateGenericWidget(KPICategoryDefinition a_definition)
		{
			ADashboardWidget instance = Instantiate(m_genericOtherWidgetPrefab.gameObject, m_widgetParent).GetComponent<ADashboardWidget>();
			instance.gameObject.SetActive(false);
			instance.m_category = m_otherCategory;
			instance.m_title.text = string.IsNullOrEmpty(a_definition.categoryDisplayName) ? a_definition.categoryName : a_definition.categoryDisplayName;
			GraphContentSelectFixedCategory cs = instance.GetComponentInChildren<GraphContentSelectFixedCategory>();
			cs.m_categoryNames = new string[1] { a_definition.categoryName };
			cs.m_kpiSource = GraphContentSelectFixedCategory.KPISource.Other;
			instance.Initialise(null);
			return instance;
		}

		public void RemoveWidget(ADashboardWidget a_widget)
		{
			m_catSelectedWidgets[a_widget.m_category].Remove(a_widget);
		}

		public void RemoveWidget(ADashboardWidget a_widget)
		{
			m_catSelectedWidgets[a_widget.m_category].Remove(a_widget);
		}

		void OnCategorySelected(DashboardCategory a_category)
		{
			if (m_showingCatalogue)
			{
				foreach (ADashboardWidget widget in m_catalogueWidgets)
					Destroy(widget.gameObject);
				m_catalogueWidgets = null;
				m_catalogueParent.gameObject.SetActive(false);
				m_showingCatalogue = false;
			}
			else if(m_currentCategory != null)
				m_catSelectedWidgets[m_currentCategory].Visible = false;

			SetWidgetsToCategory(a_category);
			m_catalogueButton.gameObject.SetActive(!a_category.m_favorite);
		}

		void SetWidgetsToCategory(DashboardCategory a_category)
		{
			m_currentCategory = a_category;
			m_categoryNameText.text = a_category.m_displayName;
			m_catSelectedWidgets[a_category].Visible = true;
			OnNumberRowsChanged(m_catSelectedWidgets[a_category].Rows);
		}

		public void OnNumberRowsChanged(int a_numberRows)
		{
			m_widgetParent.sizeDelta = new Vector2(0f, m_cellsize * a_numberRows);
		}

		void OpenCatalogue()
		{
			if (m_currentCategory != null)
				m_catSelectedWidgets[m_currentCategory].Visible = false;

			m_catalogueWidgets = new List<ADashboardWidget>();
			m_catalogueParent.gameObject.SetActive(true);
			m_showingCatalogue = true;
			m_catalogueButton.gameObject.SetActive(false);
			m_categoryNameText.text = $"{m_currentCategory.m_displayName} - Widget Catalogue";
			foreach (ADashboardWidget widget in m_loadedWidgets)
			{
				if(widget.m_category == m_currentCategory)
				{
					ADashboardWidget instance = Instantiate(widget.gameObject, m_catalogueParent.transform).GetComponent<ADashboardWidget>();
					instance.InitialiseCatalogue();
					m_catalogueWidgets.Add(instance);
				}
			}
			m_widgetParent.sizeDelta = new Vector2(0f, m_catalogueParent.preferredHeight);
		}

		public void SetWidgetAsFavorite(ADashboardWidget a_widget, bool a_favorite)
		{
			if (a_favorite)
				m_catSelectedWidgets[m_favoriteCategory].AddWidget(a_widget);
			else
			{
				m_catSelectedWidgets[m_favoriteCategory].Remove(a_widget);
				if(m_currentCategory.m_favorite)
					a_widget.Hide();
			}
		}

		public void OnWidgetMoveStart(ADashboardWidget a_widget, PointerEventData a_data)
		{ }

		public void ShowWidgetMovePreview(ADashboardWidget a_widget, PointerEventData a_data)
		{
			var pos = GetPointerPosition(a_data);
			DashboardWidgetPosition layout = m_currentCategory.m_favorite ? a_widget.m_favPosition : a_widget.m_position;
			if (m_catSelectedWidgets[m_currentCategory].WidgetFitsAt(a_widget, pos.x, pos.y, layout.W, layout.H, out int maxW, out int maxH))
			{
				//Show placed preview
				m_movePreview.gameObject.SetActive(true);
				m_rowInsertPreview.gameObject.SetActive(false);
				m_movePreview.sizeDelta = new Vector2(maxW * DashboardManager.Instance.m_cellsize, maxH * DashboardManager.Instance.m_cellsize);
				m_movePreview.localPosition = new Vector3(pos.x * DashboardManager.Instance.m_cellsize, -pos.y * DashboardManager.Instance.m_cellsize);
			}
			else if(m_catSelectedWidgets[m_currentCategory].WidgetInsertRowPossible(a_widget, pos.y, pos.x, layout.W, out int maxRowW))
			{
				//Show above preview
				m_movePreview.gameObject.SetActive(false);
				m_rowInsertPreview.gameObject.SetActive(true);
				m_rowInsertPreview.sizeDelta = new Vector2(maxRowW * DashboardManager.Instance.m_cellsize, 8f);
				m_rowInsertPreview.localPosition = new Vector3(pos.x * DashboardManager.Instance.m_cellsize, -pos.y * DashboardManager.Instance.m_cellsize);
			}
			else
			{
				//Widget can't fit
				m_movePreview.gameObject.SetActive(false);
				m_rowInsertPreview.gameObject.SetActive(false);
			}
		}

		public void OnWidgetMoveRelease(ADashboardWidget a_widget, PointerEventData a_data)
		{
			m_rowInsertPreview.gameObject.SetActive(false);
			m_movePreview.gameObject.SetActive(false);
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

		public void OnWidgetResize(ADashboardWidget a_widget, PointerEventData a_data)
		{
			var pos = GetPointerPosition(a_data);
			DashboardWidgetPosition layout = m_currentCategory.m_favorite ? a_widget.m_favPosition : a_widget.m_position;
			int targetW = Math.Max(a_widget.MinW, pos.x - layout.X + 1);
			int targetH = Math.Max(a_widget.MinH, pos.y - layout.Y + 1);
			if (m_catSelectedWidgets[m_currentCategory].WidgetFitsAt(a_widget, layout.X, layout.Y, targetW, targetH, out int maxW, out int maxH))
			{
				//Move to available position
				m_catSelectedWidgets[m_currentCategory].MoveWidget(a_widget, layout.X, layout.Y, maxW, maxH);
			}
		}

		(int x, int y) GetPointerPosition(PointerEventData a_data)
		{
			int x = 0, y = 0;
			if(RectTransformUtility.ScreenPointToLocalPointInRectangle(m_widgetParent, a_data.position, a_data.enterEventCamera, out Vector2 localPos))
			{
				x = (int)(localPos.x / m_cellsize);
				y = (int)(-localPos.y / m_cellsize);
			}
			if (y < 0) 
				y = 0;
			if(x < 0) 
				x = 0;
			if (x >= m_numberColumns)
				x = m_numberColumns - 1;
			return (x, y);
		}

		public static Color GetLerpedCountryColour(Color a_teamColour, float a_t)
		{
			if (a_t > 0.5f)
			{
				float t = (a_t - 0.5f) * 2f;
				return new Color(Mathf.Lerp(a_teamColour.r, 1f, t), Mathf.Lerp(a_teamColour.g, 1f, t), Mathf.Lerp(a_teamColour.b, 1f, t), 1f);
			}
			else
			{
				float t = a_t * 2f;
				return new Color(a_teamColour.r *t, a_teamColour.g * t, a_teamColour.b * t, 1f);
			}
		}
	}
}