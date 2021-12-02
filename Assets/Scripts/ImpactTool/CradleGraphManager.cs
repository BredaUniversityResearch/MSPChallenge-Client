using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoronoiLib;
using VoronoiLib.Structures;

namespace CradleImpactTool
{
	public class OuterEdgeCollision
	{
		public bool Left, Top, Right, Bottom;
		public OuterEdgeCollision() { Top = Left = Right = Bottom = false; }
	}

	public enum CornerID
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	[RequireComponent(typeof(RectTransform))]
	public class CradleGraphManager : MonoBehaviour
	{
		public delegate void OnWikiLinkClickHandler(string a_Name);
		public static event OnWikiLinkClickHandler OnWikiLinkClick;

		public delegate void OnGraphInitHandler();
		public static event OnGraphInitHandler OnGraphInit;

		readonly int spacingBetweenLines = 10;

		[SerializeField]
		float m_zoomDelta = 0.1f;
		[SerializeField]
		float m_zoomMin = 0.3f;
		[SerializeField]
		float m_zoomMax = 1.0f;
		[SerializeField]
		GraphSettings m_graphSettings;
		[SerializeField]
		bool m_useDebugData;
		[SerializeField]
		TextAsset m_debugData;
		[SerializeField]
		RectTransform m_panelParent;

		static ImpactObjectData m_data = null;
		RectTransform m_graphPanel;
		GameObject m_lineContainer;
		RectTransform m_lineContainerTransform;
		GameObject m_categoryContainer;
		GraphicRaycaster m_raycaster;
		EventSystem m_eventSystem;
		ModalManager m_modal;
		SaveFile m_save;
		ImpactSave m_graphSave;

		List<CategoryManager> m_categories = new List<CategoryManager>();
		Dictionary<int, CategoryItemManager> m_categoryItems = new Dictionary<int, CategoryItemManager>();
		Pool<Line> m_linePool;
		List<Line> m_impactLines = new List<Line>();

		bool m_isMouseDown = false;
		Vector2 m_mousePosition;

        //float m_currentFocus = 0.0f;
        //float m_targetFocus = 0.0f;
        //Vector3 m_startFocusPosition = Vector3.zero;
        //Vector3 m_targetFocusPosition = Vector3.zero;
        //float m_startFocusScale = 1.0f;
        //float m_targetFocusScale = 1.0f;
        HashSet<CategoryItemManager> m_selectedItems = new HashSet<CategoryItemManager>();
		bool m_hasFocus = true;
		Vector2 m_minBounds = Vector2.one * 9e9f;
		Vector2 m_maxBounds = Vector2.one * -9e9f;

		void Awake()
		{
			instance = this;

			m_save = SaveFile.Load();
			m_graphPanel = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();

			m_linePool = new Pool<Line>(m_graphSettings.linePrefab);

			m_lineContainer = new GameObject("Line Container", typeof(RectTransform));
			m_categoryContainer = new GameObject("Category Container", typeof(RectTransform));
			m_lineContainer.transform.SetParent(transform, false);
			m_categoryContainer.transform.SetParent(transform, false);
			m_lineContainerTransform = m_lineContainer.GetComponent<RectTransform>();

			m_minBounds = Vector2.one * 9e9f;
			m_maxBounds = Vector2.one * -9e9f;

			Canvas canvas = GetComponentInParent<Canvas>();
			GameObject modalInstance = Instantiate(m_graphSettings.modalPrefab, canvas.transform);
			m_modal = modalInstance.GetComponent<ModalManager>();
			if (m_modal == null)
			{
				Debug.LogError($"CradleGraphManager is unable to instantiate the modal. Please make sure the GraphSettings has a modalPrefab with a ModalManager! Graph was not created.");
				return;
			}
			m_modal.Hide();

			Transform container = m_graphPanel.transform.parent;
			m_panelParent = container.GetComponent<RectTransform>();
			while (container != null && m_raycaster == null)
			{
				m_raycaster = container.GetComponent<GraphicRaycaster>();
				container = container.parent;
			}
			if (m_raycaster == null)
			{
				Debug.LogError($"CradleGraphManager is unable to determine a parent's GraphicRaycaster. Please make sure one of the parents of {m_graphPanel.name} has a GraphicRaycaster! Graph was not created.");
				return;
			}

			m_eventSystem = FindObjectOfType<EventSystem>();
			if (m_eventSystem == null)
			{
				Debug.LogError($"CradleGraphManager is unable to find an EventSystem in the scene. Please make sure an EventSystem exist in scene \"{m_graphPanel.gameObject.scene.name}\".");
				return;
			}

			if (m_data == null && m_useDebugData && m_debugData != null && string.IsNullOrWhiteSpace(m_debugData.text) == false)
			{
				m_data = JsonConvert.DeserializeObject<ImpactObjectData>(m_debugData.text);
			}

			if (m_data != null)
			{
				CreateGraph(m_data);
			}
		}

		void OnEnable()
		{
			m_graphPanel.anchoredPosition = Vector2.zero;
			m_graphPanel.localScale = Vector3.one;

			//BreakFocus();
			HideAllLinks();
		}

		public static void InvokeWikiLinkClick(string link)
		{
			if (OnWikiLinkClick != null)
				OnWikiLinkClick.Invoke(link);
		}

		public static void ForwardGraphInfo(ImpactObjectData a_data)
		{
			if (instance != null)
			{
				instance.CreateGraph(a_data);
			}
			else
			{
				m_data = a_data;

				CradleGraphManager graphManager = FindObjectsOfType<CradleGraphManager>(true).FirstOrDefault();
				if (graphManager != null)
					graphManager.gameObject.SetActive(true);
			}
		}

		public void CreateGraph(ImpactObjectData a_data)
		{
			m_data = a_data;
			if (m_graphSettings == null || m_graphSettings.Validate() == false)
			{
				Debug.LogError("CradleGraphManager.CreateGraph: GraphSettings do not seem to be valid. Graph was not created.");
				return;
			}

			if (a_data == null || a_data.Validate(m_graphSettings) == false)
			{
				Debug.LogError("CradleGraphManager.CreateGraph: ImpactObjectData source does not seem to be valid. Graph was not created.");
				return;
			}

			if (m_graphPanel == null)
			{
				Debug.LogError("CradleGraphManager.CreateGraph: RectTransform is missing from GameObject. Graph was not created.");
				return;
			}

			CreateCategories();
			CreateBackground();

			if (OnGraphInit != null)
				OnGraphInit.Invoke();
		}

		void CreateCategories()
		{
			if (m_save.impactSaves.TryGetValue("debug", out m_graphSave) == false) // TODO: Get filename of graph to load.
			{
				m_save.impactSaves.Add("debug", new ImpactSave());
				m_save.impactSaves.TryGetValue("debug", out m_graphSave);
			}

			float halfContainerWidth = m_graphPanel.rect.width * 15;
			int categoryCount = m_data.categories.Length;
			float categoryCircleStepSize = (Mathf.PI * 2) / categoryCount;
			for (int i = 0; i < categoryCount; i++)
			{
				// Get or create all required category data
				CategoryData categoryData = m_data.categories[i];
				GameObject categoryInstance = Instantiate(m_graphSettings.categoryTextPrefab, m_categoryContainer.transform);
				CategoryManager categoryManager = categoryInstance.GetComponent<CategoryManager>();
				RectTransform categoryTransform = categoryInstance.GetComponent<RectTransform>();

				categoryManager.text = categoryData.name.ToUpper();
				categoryInstance.name = categoryData.name;

				CategorySave categorySave = null;
				if (m_graphSave != null)
					m_graphSave.categories.TryGetValue(categoryData.name, out categorySave);

				// Position category relative to container (angle from center of panel)
				// TODO: Place categories in a more natural way
				float categoryAngle = categoryCircleStepSize * i;
				Vector2 categoryOffset = new Vector2(Mathf.Cos(categoryAngle), Mathf.Sin(categoryAngle)) * halfContainerWidth;
				categoryTransform.anchoredPosition = categoryOffset;

				categoryManager.graph = this;
				m_categories.Add(categoryManager);

				ItemData[] items = m_data.items.Where(d => d.category == categoryData.id).ToArray();
				int itemCount = items.Length;
				float itemCircleStepSize = (Mathf.PI * 2.0f) / (float)(itemCount + 1.0f);
				float itemCircleOffset = -Mathf.PI * 0.5f;
				for (int j = 0; j < itemCount; j++)
				{
					// Get or create all required category item data
					ItemData itemData = items[j];
					GameObject itemInstance = Instantiate(m_graphSettings.categoryItemPrefab, categoryInstance.transform);
					TMP_Text itemText = itemInstance.GetComponentInChildren<TMP_Text>();
					CategoryItemManager categoryItemManager = itemInstance.GetComponent<CategoryItemManager>();
					RectTransform itemTransform = itemInstance.GetComponent<RectTransform>();

					itemText.text = itemData.name;
					itemInstance.name = itemData.name;

					Sprite icon = m_graphSettings.itemIcons[itemData.icon];
					if (icon != null)
					{
						categoryItemManager.SetIcon(icon);
					}
					else
					{
						Debug.LogError($"Unable to determine icon of {itemData.name}. \"{itemData.icon}\" is not a valid icon qualifier, as it is not present in the GraphSettings.");
					}


					ItemSave itemSave = null;
					if (categorySave != null)
						categorySave.items.TryGetValue(itemData.name, out itemSave);

					if (itemSave != null)
					{
						itemTransform.localPosition = new Vector2(itemSave.X, itemSave.Y);
					}
					else
					{
						// Position category item relative to category (angle from center of category center)
						// TODO: Place category items in a more natural way
						float itemAngle = Mathf.Atan2(-categoryOffset.y, -categoryOffset.x) + (j + 1) * itemCircleStepSize + itemCircleOffset;

						float prefXOffset = (categoryManager.preferredWidth + itemText.preferredWidth) * 0.7f;
						float prefYOffset = (categoryManager.preferredHeight + itemText.preferredHeight);
						itemTransform.anchoredPosition = new Vector2(Mathf.Cos(itemAngle) * prefXOffset, Mathf.Sin(itemAngle) * prefYOffset);
					}

					// Get all related links
					IEnumerable<LinkData> links = m_data.links.Where(l => l.fromId == itemData.id || l.toId == itemData.id);
					categoryItemManager.AddLinks(links);

					categoryManager.AddItem(categoryItemManager);
					m_categoryItems[itemData.id] = categoryItemManager;
				}
			}
		}

		void CreateBackground()
		{
			gameObject.BroadcastMessage("InvalidateCradleUI"); // Ensure we invalidate all category/item bounds.

			// Keep trying to move each category as close to the middle as possible. Do this until you're sure we've tried to move them all.
			for (int i = 0; i < m_categories.Count; i++)
			{
				bool hasMoved = false;
				foreach (CategoryManager category in m_categories)
				{
					hasMoved |= category.MoveToCenter();
				}

				if (hasMoved == false)
					break;
			}

			float lineThickness = 3; // TODO: Remove magic number
			List<FortuneSite> sites = new List<FortuneSite>();

			// Gather all the category positions
			foreach (CategoryManager category in m_categories)
			{
				Vector2 categoryPos = category.GetTransform().anchoredPosition;
				sites.Add(new FortuneSite(category.name, categoryPos.x, categoryPos.y));

				Bounds bounds = category.GetCategoryBounds();
				Vector2 boundsMin = categoryPos + (Vector2)bounds.min;
				Vector2 boundsMax = categoryPos + (Vector2)bounds.max;
				if (boundsMin.x < m_minBounds.x)
					m_minBounds.x = boundsMin.x;
				if (boundsMin.y < m_minBounds.y)
					m_minBounds.y = boundsMin.y;
				if (boundsMax.x > m_maxBounds.x)
					m_maxBounds.x = boundsMax.x;
				if (boundsMax.y > m_maxBounds.y)
					m_maxBounds.y = boundsMax.y;
			}

			// Expand outer edges by some distance
			m_minBounds -= 750.0f * Vector2.one;
			m_maxBounds += 750.0f * Vector2.one;

			Vector2 center = (m_minBounds + m_maxBounds) * 0.5f;
			m_minBounds -= center;
			m_maxBounds -= center;
			m_graphPanel.sizeDelta = m_maxBounds - m_minBounds;

			// Generate a voronoi, draw its edges
			LinkedList<VEdge> results = FortunesAlgorithm.Run(sites, m_minBounds.x, m_minBounds.y, m_maxBounds.x, m_maxBounds.y);
			Dictionary<CategoryManager, OuterEdgeCollision> edgeCollisions = new Dictionary<CategoryManager, OuterEdgeCollision>();
			foreach (VEdge result in results)
			{
				Line lineInstance = m_linePool.Get(m_lineContainerTransform);
				if (lineInstance.SetDrawingData(new Vector2((float)result.Start.X, (float)result.Start.Y), new Vector2((float)result.End.X, (float)result.End.Y), 0, lineThickness) == false)
				{
					m_linePool.Release(lineInstance);
					continue;
				}

				lineInstance.name = $"Voronoi Edge: L {result.Left.Name} -> R {result.Right.Name}";
				IEnumerable<CategoryManager> relatedCategories = m_categories.Where((c) => c.name == result.Left.Name || c.name == result.Right.Name);
				foreach (CategoryManager category in relatedCategories)
				{
					category.AddEdge(lineInstance, result.Right.Name == category.name);
				}

				// Check if we're touching any edge here. This is to make sure the outer edges also are added to the related categories.
				foreach (Vector2 pos in new Vector2[] { lineInstance.fromPos, lineInstance.toPos })
				{
					Vector2 delta = pos - m_minBounds;
					if (Mathf.Abs(delta.x) < 0.03f) // Touches left edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_left: true);
					}
					if (Mathf.Abs(delta.y) < 0.03f) // Touches bottom edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_bottom: true);
					}

					delta = pos - m_maxBounds;
					if (Mathf.Abs(delta.x) < 0.03f) // Touches right edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_right: true);
					}
					if (Mathf.Abs(delta.y) < 0.03f) // Touches top edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_top: true);
					}
				}
			}

			// Draw the outer edges
			KeyValuePair<CornerID, Vector2>[] edges = new KeyValuePair<CornerID, Vector2>[]
			{
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomLeft, new Vector2(m_minBounds.x, m_minBounds.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomRight, new Vector2(m_maxBounds.x, m_minBounds.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.TopRight, new Vector2(m_maxBounds.x, m_maxBounds.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.TopLeft, new Vector2(m_minBounds.x, m_maxBounds.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomLeft, new Vector2(m_minBounds.x, m_minBounds.y)),
			};

			// Ensure the items are in the correct categories
			foreach (CategoryManager category in m_categories)
			{
				for (int i = 0; i < 1000; i++)
				{
					if (category.ResolveItemPositions() == false)
						break;
				}
			}
		}

		private static void CreateOuterEdgeCollision(IEnumerable<CategoryManager> a_categories, ref Dictionary<CategoryManager, OuterEdgeCollision> a_collisions, bool a_left = false, bool a_right = false, bool a_top = false, bool a_bottom = false)
		{
			foreach (CategoryManager category in a_categories)
			{
				OuterEdgeCollision collisions;
				if (a_collisions.TryGetValue(category, out collisions))
				{
					collisions.Left |= a_left;
					collisions.Right |= a_right;
					collisions.Top |= a_top;
					collisions.Bottom |= a_bottom;
					return;
				}

				collisions = new OuterEdgeCollision();

				collisions.Left |= a_left;
				collisions.Right |= a_right;
				collisions.Top |= a_top;
				collisions.Bottom |= a_bottom;

				a_collisions.Add(category, collisions);
			}
		}

		public void DrawLink(LinkData a_link)
		{
			if (a_link == null)
			{
				Debug.LogError("Invalid link passed into CradleGraphManager.DrawLink: Link cannot be null. Link was not drawn.");
				return;
			}

			CategoryItemManager fromItem = m_categoryItems[a_link.fromId];
			CategoryItemManager toItem = m_categoryItems[a_link.toId];
			if (fromItem == null)
			{
				Debug.LogError($"CradleGraphManager.DrawLink has an invalid \"from\" item: Item with id {a_link.fromId} was not valid. Link was not drawn.");
				return;
			}
			if (toItem == null)
			{
				Debug.LogError($"CradleGraphManager.DrawLink has an invalid \"to\" item: Item with id {a_link.toId} was not valid. Link was not drawn.");
				return;
			}

			m_selectedItems.Add(fromItem);
			m_selectedItems.Add(toItem);

			int lineCount = a_link.lines.Length;
			for (int i = 0; i < lineCount; i++)
			{
				LineData line = a_link.lines[i];
				float offsetFromCenter = ((float)i - ((float)(lineCount - 1) * 0.5f)) * spacingBetweenLines;
				Line lineInstance = m_linePool.Get(m_lineContainer.transform);

				ImpactTypeData impact = m_data.impactTypes.FirstOrDefault(type => type.id == line.impactId);
				Color colour = m_graphSettings.lineColors[impact.type];
				float thickness = m_graphSettings.lineThicknesses[line.thickness];
				if (lineInstance.SetDrawingData(fromItem.gameObject, toItem.gameObject, offsetFromCenter, thickness, colour) == false)
				{
					m_linePool.Release(lineInstance);
					continue;
				}

				lineInstance.name = $"Impact Line: {fromItem.name} -> {toItem.name}";
				lineInstance.SetImpactData(this, line, impact);
				m_impactLines.Add(lineInstance);
			}
		}

		public void HideAllLinks()
		{
			// Cannot do ReleaseAll because the background voronoi edges are also lines.
			m_linePool.ReleaseRange(m_impactLines);
			m_impactLines.Clear();
			//StopFocus();

			foreach (KeyValuePair<int, CategoryItemManager> item in m_categoryItems)
			{
				item.Value.toggle.SetIsOnWithoutNotify(false);
			}
		}

		void Update()
		{
			// If the application is in view
			if (Application.isFocused)
			{
				bool t_HasMoved = false;
				t_HasMoved |= HandleZoom();
				t_HasMoved |= HandlePan();
				//if (t_HasMoved)
				//	BreakFocus();
			}

			//UpdateFocus();
		}

		// This function makes sure that whenever you adjust the ro
		Vector3 ConfinePanelInScreen(Vector3 position)
		{
			// TODO: Due to the fact that the outer edge lines are drawn at position instead of anchoredPosition, we have to offset it for the anchor.
			// It's an unfortunate drawback of an old system incorrectly made with positions instead of anchoredPosition, and has to be corrected in the future,
			// or be accounted for in any case where screenspace-correct positions are required.
			Vector2 anchoredOffset = m_graphPanel.anchoredPosition;

			Vector2 min = m_minBounds * m_graphPanel.localScale + anchoredOffset;
			Vector2 max = m_maxBounds * m_graphPanel.localScale - anchoredOffset;

			if (-position.x < min.x)
				position.x = -min.x;
			if (-position.y < min.y)
				position.y = -min.y;

			if (-position.x > max.x)
				position.x = -max.x;
			if (-position.y > max.y)
				position.y = -max.y;

			return position;
		}

        //void UpdateFocus()
        //{
        //	float delta = m_targetFocus - m_currentFocus;
        //	if (delta == 0) // This pretty much can only happen if we force it to be 0.
        //		return;

        //	if (Mathf.Abs(delta) < 0.003f) // If pretty much near the target, force it to the end, draw one last frame
        //	{
        //		m_currentFocus = m_targetFocus;
        //		gameObject.BroadcastMessage("InvalidateCradleUI");
        //	}
        //	else
        //	{
        //		m_currentFocus += delta * Time.deltaTime * 3.0f; // TODO: remove magic value
        //	}

        //	m_graphPanel.localScale = Vector3.one * Mathf.Lerp(m_startFocusScale, m_targetFocusScale, m_currentFocus);
        //	m_graphPanel.anchoredPosition = ConfinePanelInScreen(Vector2.Lerp(m_startFocusPosition, m_targetFocusPosition * m_graphPanel.localScale.x, m_currentFocus));
        //}


        // This function completely unsets focus, and focusses on the
        //void BreakFocus()
        //{
        //    m_currentFocus = 0.0f;
        //    m_targetFocus = 0.0f;

        //    m_startFocusScale = m_graphPanel.localScale.x;
        //    m_targetFocusScale = m_graphPanel.localScale.x;
        //    m_startFocusPosition = m_graphPanel.anchoredPosition;
        //    m_targetFocusPosition = m_graphPanel.anchoredPosition;
        //}

        bool HandleZoom()
		{
			if (Input.mouseScrollDelta.y == 0)
				return false;

			Vector3 oldScale = m_graphPanel.transform.localScale;
			float delta = m_zoomDelta * -Input.mouseScrollDelta.y;
			m_graphPanel.transform.localScale -= Vector3.one * delta;
			if (m_graphPanel.transform.localScale.x < m_zoomMin)
			{
				m_graphPanel.transform.localScale = new Vector3(m_zoomMin, m_zoomMin, m_zoomMin);
			}
			else if (m_graphPanel.transform.localScale.x > m_zoomMax)
			{
				m_graphPanel.transform.localScale = new Vector3(m_zoomMax, m_zoomMax, m_zoomMax);
			}

			m_graphPanel.anchoredPosition = ConfinePanelInScreen(m_graphPanel.anchoredPosition - (oldScale - m_graphPanel.transform.localScale) * m_graphPanel.anchoredPosition);
			gameObject.BroadcastMessage("InvalidateCradleUI");
			return true;
		}

		bool HandlePan()
		{
			bool t_HasChanged = false;
			if (m_hasFocus == false)
			{
				return t_HasChanged;
			}

			bool isControlDragging = Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
			if (Input.GetMouseButton(2) || Input.GetMouseButton(1) || isControlDragging)
			{
				if (m_isMouseDown == false)
				{
					m_isMouseDown = true;
					m_mousePosition = Input.mousePosition;
				}

				Vector2 deltaPos = m_mousePosition - (Vector2)Input.mousePosition;
				m_mousePosition = Input.mousePosition;
				m_graphPanel.anchoredPosition = ConfinePanelInScreen(m_graphPanel.anchoredPosition - deltaPos);
				t_HasChanged = true;
			}
			else
			{
				m_isMouseDown = false;
			}

			return t_HasChanged;
		}

        //public void Focus()
        //{
        //	m_currentFocus = 0.0f;
        //	m_targetFocus = 1.0f;

        //	Bounds totalBounds = m_selectedItems.FirstOrDefault().GetBounds(false);
        //	foreach (CategoryItemManager item in m_selectedItems)
        //		totalBounds.Encapsulate(item.GetBounds(false));
        //	Vector2 posOffset = (totalBounds.center - m_graphPanel.position) / m_graphPanel.localScale.x;

        //	Vector2 viewportSize = m_panelParent.rect.size;
        //	Vector2 boundsSize = totalBounds.size / m_graphPanel.localScale.x;
        //	Vector2 sizeOffset = viewportSize / boundsSize;

        //	m_startFocusScale = m_graphPanel.localScale.x;
        //	m_targetFocusScale = Mathf.Min(sizeOffset.x, sizeOffset.y); // Try to make the view fit in
        //	m_targetFocusScale = Mathf.Max(m_zoomMin, m_targetFocusScale); 
        //	Debug.Log($"Bounds size: {boundsSize}, Viewport: {viewportSize}, Scale: {m_targetFocusScale}");

        //	m_startFocusPosition = m_graphPanel.anchoredPosition;
        //	m_targetFocusPosition = -posOffset;
        //}

        //public void StopFocus()
        //{
        //	m_targetFocus = 0.0f;
        //	m_selectedItems.Clear();
        //}

        void OnApplicationFocus(bool hasFocus)
        {
            m_hasFocus = hasFocus;
        }

        public static CradleGraphManager instance { get; private set; }
		public ImpactObjectData impactObjectData { get { return m_data; } }
		public GraphSettings graphSettings { get { return m_graphSettings; } }
		public Pool<Line> linePool { get { return m_linePool; } }
		public ModalManager modal { get { return m_modal; } }
		public List<CategoryManager> categories { get { return m_categories; } }
		//public bool isFocussing { get { return m_targetFocus > 0.01f; } }
		public ImpactSave graphSave { get { return m_graphSave; } }
		public SaveFile save { get { return m_save; } }
		public bool isDraggingView { get { return m_isMouseDown; } }
	}
}
