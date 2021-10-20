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
		TextAsset m_jsonSource;

		static ImpactObjectData m_data = null;
		RectTransform m_root;
		GameObject m_lineContainer;
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
		Vector3 m_mousePosition;
		bool m_hasFocus = true;

		void Awake()
		{
			instance = this;

			m_save = SaveFile.Load();
			m_root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();

			m_linePool = new Pool<Line>(m_graphSettings.linePrefab);

			m_lineContainer = new GameObject("Line Container");
			m_categoryContainer = new GameObject("Category Container");
			m_lineContainer.transform.parent = transform;
			m_categoryContainer.transform.parent = transform;

			Canvas canvas = GetComponentInParent<Canvas>();
			GameObject modalInstance = Instantiate(m_graphSettings.modalPrefab, canvas.transform);
			m_modal = modalInstance.GetComponent<ModalManager>();
			if (m_modal == null)
			{
				Debug.LogError($"CradleGraphManager is unable to instantiate the modal. Please make sure the GraphSettings has a modalPrefab with a ModalManager! Graph was not created.");
				return;
			}
			m_modal.Hide();

			Transform container = m_root.transform.parent;
			while (container != null && m_raycaster == null)
			{
				m_raycaster = container.GetComponent<GraphicRaycaster>();
				container = container.parent;
			}
			if (m_raycaster == null)
			{
				Debug.LogError($"CradleGraphManager is unable to determine a parent's GraphicRaycaster. Please make sure one of the parents of {m_root.name} has a GraphicRaycaster! Graph was not created.");
				return;
			}

			m_eventSystem = FindObjectOfType<EventSystem>();
			if (m_eventSystem == null)
			{
				Debug.LogError($"CradleGraphManager is unable to find an EventSystem in the scene. Please make sure an EventSystem exist in scene \"{m_root.gameObject.scene.name}\".");
				return;
			}

			if (m_data != null)
			{
				CreateGraph(m_data);
			}
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

			if (m_root == null)
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

			float halfContainerWidth = m_root.GetComponent<RectTransform>().rect.width * 15.0f;
			int categoryCount = m_data.categories.Length;
			float categoryCircleStepSize = (Mathf.PI * 2) / categoryCount;
			for (int i = 0; i < categoryCount; i++)
			{
				// Get or create all required category data
				CategoryData categoryData = m_data.categories[i];
				GameObject categoryInstance = Instantiate(m_graphSettings.categoryTextPrefab, m_categoryContainer.transform);
				CategoryManager categoryManager = categoryInstance.GetComponent<CategoryManager>();
				RectTransform categoryTransform = categoryInstance.GetComponent<RectTransform>();
				TMP_Text categoryText = categoryManager.GetText();

				categoryText.text = categoryData.name.ToUpper();
				categoryInstance.name = categoryData.name;

				CategorySave categorySave = null;
				if (m_graphSave != null)
					m_graphSave.categories.TryGetValue(categoryData.name, out categorySave);

				// Position category relative to container (angle from center of panel)
				// TODO: Place categories in a more natural way
				float categoryAngle = categoryCircleStepSize * i;
				Vector2 categoryOffset = new Vector2(Mathf.Cos(categoryAngle), Mathf.Sin(categoryAngle)) * halfContainerWidth;
				categoryTransform.localPosition = (Vector2)m_root.position + categoryOffset;

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

						float prefXOffset = (categoryText.preferredWidth + itemText.preferredWidth) * 0.7f;
						float prefYOffset = (categoryText.preferredHeight + itemText.preferredHeight);
						itemTransform.localPosition = new Vector2(Mathf.Cos(itemAngle) * prefXOffset, Mathf.Sin(itemAngle) * prefYOffset);
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
			Vector2 min = Vector2.one * 9e9f;
			Vector2 max = Vector2.one * -9e9f;

			// Gather all the category positions
			foreach (CategoryManager category in m_categories)
			{
				Vector2 categoryPos = category.GetComponent<RectTransform>().position;
				sites.Add(new FortuneSite(category.name, categoryPos.x, categoryPos.y));

				Bounds bounds = category.GetCategoryBounds();
				Vector2 boundsMin = categoryPos + (Vector2)bounds.min;
				Vector2 boundsMax = categoryPos + (Vector2)bounds.max;
				if (boundsMin.x < min.x)
					min.x = boundsMin.x;
				if (boundsMin.y < min.y)
					min.y = boundsMin.y;
				if (boundsMax.x > max.x)
					max.x = boundsMax.x;
				if (boundsMax.y > max.y)
					max.y = boundsMax.y;
			}

			// Expand outer edges by some distance
			min -= 50.0f * Vector2.one; // TODO: Remove magic number
			max += 50.0f * Vector2.one;

			m_root.sizeDelta = max - min;

			// Generate a voronoi, draw its edges
			LinkedList<VEdge> results = FortunesAlgorithm.Run(sites, min.x, min.y, max.x, max.y);
			Dictionary<CategoryManager, OuterEdgeCollision> edgeCollisions = new Dictionary<CategoryManager, OuterEdgeCollision>();
			foreach (VEdge result in results)
			{
				Line lineInstance = m_linePool.Get(m_lineContainer.transform);
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
					Vector2 delta = pos - min;
					if (Mathf.Abs(delta.x) < 0.03f) // Touches left edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_left: true);
					}
					if (Mathf.Abs(delta.y) < 0.03f) // Touches bottom edge
					{
						CreateOuterEdgeCollision(relatedCategories, ref edgeCollisions, a_bottom: true);
					}

					delta = pos - max;
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
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomLeft, new Vector2(min.x, min.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomRight, new Vector2(max.x, min.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.TopRight, new Vector2(max.x, max.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.TopLeft, new Vector2(min.x, max.y)),
				new KeyValuePair<CornerID, Vector2>(CornerID.BottomLeft, new Vector2(min.x, min.y)),
			};

			for (int i = 1; i < edges.Length; i++)
			{
				var edge1 = edges[i - 1];
				var edge2 = edges[i];
				Vector2 start = edge1.Value;
				Vector2 end = edge2.Value;

				bool isLeftEdge =
					(edge1.Key == CornerID.BottomLeft && edge2.Key == CornerID.TopLeft) ||
					(edge2.Key == CornerID.BottomLeft && edge1.Key == CornerID.TopLeft);
				bool isRightEdge =
					(edge1.Key == CornerID.BottomRight && edge2.Key == CornerID.TopRight) ||
					(edge2.Key == CornerID.BottomRight && edge1.Key == CornerID.TopRight);
				bool isTopEdge =
					(edge1.Key == CornerID.TopLeft && edge2.Key == CornerID.TopRight) ||
					(edge2.Key == CornerID.TopLeft && edge1.Key == CornerID.TopRight);
				bool isBottomEdge =
					(edge1.Key == CornerID.BottomLeft && edge2.Key == CornerID.BottomRight) ||
					(edge2.Key == CornerID.BottomLeft && edge1.Key == CornerID.BottomRight);

				Line outerEdge = m_linePool.Get(m_lineContainer.transform);
				if (outerEdge.SetDrawingData(start, end, 0, lineThickness) == false)
				{
					m_linePool.Release(outerEdge);
					continue;
				}

				outerEdge.name = $"Voronoi Outer Edge {i}";

				foreach (var data in edgeCollisions)
				{
					CategoryManager category = data.Key;
					OuterEdgeCollision collision = data.Value;

					// Note: All edges are on the left side because we draw them in a clockwise order (top left, top right, bottom right, bottom left),
					// meaning that the right side of the line is always facing the category.

					if (isLeftEdge && collision.Left)
					{
						category.AddEdge(outerEdge, a_lineIsOnLeftSide: true);
					}
					else if (isRightEdge && collision.Right)
					{
						category.AddEdge(outerEdge, a_lineIsOnLeftSide: true);
					}

					if (isBottomEdge && collision.Bottom)
					{
						category.AddEdge(outerEdge, a_lineIsOnLeftSide: true);
					}
					else if (isTopEdge && collision.Top)
					{
						category.AddEdge(outerEdge, a_lineIsOnLeftSide: true);
					}
				}
			}

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

			foreach (KeyValuePair<int, CategoryItemManager> item in m_categoryItems)
			{
				CustomToggle toggle = item.Value.GetComponent<CustomToggle>();
				toggle.SetIsOnWithoutNotify(false);
			}
		}

		void Update()
		{
			if (Application.isFocused == false)
			{
				return;
			}

			HandleZoom();
			HandlePan();
		}

		void HandleZoom()
		{
			if (m_hasFocus == false)
			{
				return;
			}

			if (Input.mouseScrollDelta.y != 0)
			{
				var delta = m_zoomDelta * -Input.mouseScrollDelta.y;
				m_root.transform.localScale -= Vector3.one * delta;
				if (m_root.transform.localScale.x < m_zoomMin)
				{
					m_root.transform.localScale = new Vector3(m_zoomMin, m_zoomMin, m_zoomMin);
				}
				else if (m_root.transform.localScale.x > m_zoomMax)
				{
					m_root.transform.localScale = new Vector3(m_zoomMax, m_zoomMax, m_zoomMax);
				}

				gameObject.BroadcastMessage("InvalidateCradleUI");
			}
		}

		void HandlePan()
		{
			if (m_hasFocus == false)
			{
				return;
			}

			bool isControlDragging = Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
			if (Input.GetMouseButton(2) || Input.GetMouseButton(1) || isControlDragging)
			{
				if (m_isMouseDown == false)
				{
					m_isMouseDown = true;
					m_mousePosition = Input.mousePosition;
				}

				Vector3 deltaPos = m_mousePosition - Input.mousePosition;
				m_mousePosition = Input.mousePosition;

				m_root.transform.position -= deltaPos;
			}
			else
			{
				m_isMouseDown = false;
			}
		}

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
		public ImpactSave graphSave { get { return m_graphSave; } }
		public SaveFile save { get { return m_save; } }
		public bool isDraggingView { get { return m_isMouseDown; } }
	}
}
