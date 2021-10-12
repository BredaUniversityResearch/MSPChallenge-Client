using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CradleImpactTool
{
	public class CategoryItemManager : MonoBehaviour
	{
		[SerializeField]
		float m_dragDelay = 0.1f;
		[SerializeField]
		Image m_iconImage;

		static CategoryItemManager m_selectedItem = null;
		static float m_timeDragging = 0;
		static Vector3 m_prevMousePos = Vector3.zero;

		CradleGraphManager m_graph;
		RectTransform m_rectTransform;
		CustomToggle m_toggle;
		Bounds? m_bounds;
		List<LinkData> m_links = new List<LinkData>();

		void Awake()
		{
			m_toggle = GetComponentInChildren<CustomToggle>();
			m_rectTransform = GetComponentInChildren<RectTransform>();
			Debug.Assert(m_toggle, "Missing CustomToggle in Category Item!");

			m_toggle.onValueChanged.AddListener(OnToggle);
		}

		void OnToggle(bool a_activated)
		{
			m_graph.HideAllLinks();
			if (a_activated)
			{
				foreach (LinkData link in m_links)
				{
					m_graph.DrawLink(link);
				}

				// HideAllLinks turns this toggle off, toggle it back on.
				m_toggle.SetIsOnWithoutNotify(true);
			}
		}

		private void OnDrawGizmos()
		{
			if (m_bounds.HasValue)
			{
				DebugDraw.DrawBox(transform.position, m_bounds.Value, Color.blue);
			}
		}

		private void Update()
		{
			UpdateDrag();
		}

		private void UpdateDrag()
		{
			bool isDraggable = m_selectedItem == this || m_selectedItem == null;
			bool isMouseInside = m_selectedItem == this || GetBounds(false).Contains(Input.mousePosition);
			bool isNotDraggingView = CradleGraphManager.instance.isDraggingView == false;
			if (isDraggable && Input.GetMouseButton(0) && isMouseInside && isNotDraggingView)
			{
				// If we're not on the first drag frame, and we've confirmed we're dragging 
				if (m_selectedItem && m_timeDragging > m_dragDelay)
				{
					m_graph.HideAllLinks();
					Vector3 delta = Input.mousePosition - m_prevMousePos;

					Vector3 origPos = transform.position;
					transform.position += delta;

					// Prevent dragging out of the category
					if (category.ResolveItemPosition(this))
						transform.position = origPos;
				}

				// Only update the previous position if we started, or if we're actually dragging. This prevents misaligned mouse cursors
				if (m_timeDragging == 0 || m_timeDragging > m_dragDelay)
				{
					m_prevMousePos = Input.mousePosition;
				}

				m_selectedItem = this;
				m_timeDragging += Time.deltaTime;
			}
			else if (m_selectedItem == this)
			{
				if (m_timeDragging > m_dragDelay)
				{
					m_graph.HideAllLinks();
					ItemSave item = m_graph.graphSave.GetItem(category.name, name);
					item.X = transform.localPosition.x;
					item.Y = transform.localPosition.y;
					m_graph.save.Save();
				}

				m_selectedItem = null;
				m_prevMousePos = Vector3.zero;
				m_timeDragging = 0;
			}
		}

		public Bounds GetBounds(bool a_IsLocal = true)
		{
			if (m_bounds.HasValue == false)
			{
				m_bounds = GetTransform().GetAbsoluteUIBounds();
			}

			if (a_IsLocal)
			{
				return m_bounds.Value;
			}

			return new Bounds(transform.position + m_bounds.Value.center, m_bounds.Value.size);
		}

		// Called via broadcast
		public void InvalidateCradleUI()
		{
			m_bounds = null;
		}

		public RectTransform GetTransform()
		{
			if (m_rectTransform == null)
			{
				m_rectTransform = GetComponent<RectTransform>();
			}

			return m_rectTransform;
		}

		public void SetData(CategoryManager a_category, CradleGraphManager a_graph)
		{
			category = a_category;
			m_graph = a_graph;
		}

		public void AddLink(LinkData a_link)
		{
			m_links.Add(a_link);
		}

		public void AddLinks(IEnumerable<LinkData> a_links)
		{
			m_links.AddRange(a_links);
		}

		public void SetIcon(Sprite a_sprite)
		{
			m_iconImage.sprite = a_sprite;
		}

		public CustomToggle toggle { get { return m_toggle; } }
		public CategoryManager category { get; set; }
	}
}
