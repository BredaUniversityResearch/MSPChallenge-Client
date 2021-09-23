using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CradleImpactTool
{
	public struct CategoryEdge
	{
		public Line Line;
		public bool IsOnLeft;

		public CategoryEdge(Line a_line, bool a_isOnLeft)
		{
			Line = a_line;
			IsOnLeft = a_isOnLeft;
		}
	};

	public class CategoryManager : MonoBehaviour
	{
		[SerializeField]
		RectTransform m_child;

		RectTransform m_rectTransform;
		TMP_Text m_text;
		List<CategoryItemManager> m_items = new List<CategoryItemManager>();
		Bounds? m_categoryBounds;
		Bounds? m_textBounds;
		List<CategoryEdge> m_edges = new List<CategoryEdge>();

		private void Start()
		{
			GetTransform();
			GetText();
		}

		//private void OnDrawGizmos()
		//{
		//	if (m_ownBounds.HasValue)
		//	{
		//		transform.DebugDrawBox(m_ownBounds.Value, Color.blue);
		//	}
		//}

		public void AddItem(CategoryItemManager item)
		{
			item.SetData(this, graph);
			m_items.Add(item);
			m_categoryBounds = null;
		}

		public void AddEdge(Line a_edge, bool a_lineIsOnLeftSide)
		{
			m_edges.Add(new CategoryEdge(a_edge, a_lineIsOnLeftSide));
		}

		public RectTransform GetTransform()
		{
			if (m_rectTransform == null)
			{
				m_rectTransform = GetComponent<RectTransform>();
			}

			return m_rectTransform;
		}

		public TMP_Text GetText()
		{
			if (m_text == null)
			{
				m_text = m_child.GetComponent<TMP_Text>();
			}

			return m_text;
		}

		public Bounds GetCategoryBounds(bool a_IsLocal = true)
		{
			if (m_categoryBounds.HasValue == false)
			{
				m_categoryBounds = GetTransform().GetAbsoluteUIBounds();
			}

			if (a_IsLocal)
			{
				return m_categoryBounds.Value;
			}

			return new Bounds(transform.position + m_categoryBounds.Value.center, m_categoryBounds.Value.size);
		}

		public Bounds GetTextBounds(bool a_IsLocal = true)
		{
			if (m_textBounds.HasValue == false)
			{
				m_textBounds = m_child.GetOwnUIBounds();
			}

			if (a_IsLocal)
			{
				return m_textBounds.Value;
			}

			return new Bounds(m_child.position + m_textBounds.Value.center, m_textBounds.Value.size);
		}

		// Binary search algorithm to find the closest free space towards the middle of the panel.
		public bool MoveToCenter()
		{
			Vector3 closestPosition = Vector3.zero;
			Vector3 furthestPosition = transform.position;
			Vector3 currentPos = closestPosition;
			bool hasMoved = false;

			for (int i = 0; i < 100; i++)
			{
				Bounds ourBounds = GetCategoryBounds();
				ourBounds.center += currentPos;

				// If we're barely moving now, just stop
				float distance = (closestPosition - furthestPosition).sqrMagnitude;
				if (distance < 3 * 3)
					break;

				currentPos = closestPosition + (furthestPosition - closestPosition) * 0.5f;

				bool hasCollision = false;
				foreach (CategoryManager category in graph.categories)
				{
					if (category == this)
						continue;

					var bounds = category.GetCategoryBounds(false);
					if (bounds.Intersects(ourBounds))
					{
						hasCollision = true;
						break;
					}
				}

				if (hasCollision)
				{
					closestPosition = currentPos;
				}
				else
				{
					hasMoved = true;
					furthestPosition = currentPos;
				}
			}

			transform.position = currentPos;
			return hasMoved;
		}

		private void Update()
		{
			GetCategoryBounds();
			GetTextBounds();
		}

		public bool ResolveItemPositions()
		{
			bool hasMoved = false;
			foreach (CategoryItemManager item in m_items)
			{
				hasMoved |= ResolveItemPosition(item);
			}

			return hasMoved;
		}

		public bool ResolveItemPosition(CategoryItemManager a_item)
		{
			Debug.Assert(m_items.Contains(a_item));

			bool hasMoved = false;
			Vector3 force = Vector3.zero;
			Bounds itemBounds = a_item.GetBounds(false);
			foreach (CategoryEdge edge in m_edges)
			{
				// First check if the box's closest point to the line is on the correct side of the line
				Vector2 closestPointOnLine = edge.Line.GetClosestPoint(a_item.transform.position);
				Vector2 closestPointOnBounds = itemBounds.GetClosestPoint(closestPointOnLine);

				Vector2 localUp = edge.Line.GetDirection();
				Vector2 delta = (closestPointOnBounds - edge.Line.center).normalized;

				float isOnLeft = localUp.x * delta.y - localUp.y * delta.x;
				bool isOnCorrectSide = isOnLeft > 0 ? edge.IsOnLeft : !edge.IsOnLeft;

				// Make sure the line is not colliding with the box
				if (isOnCorrectSide)
					isOnCorrectSide = !CohenSutherland.Calculate(edge.Line.fromPos, edge.Line.toPos, itemBounds.min, itemBounds.max);

				if (!isOnCorrectSide)
				{
					force += edge.IsOnLeft ? edge.Line.transform.up : -edge.Line.transform.up;
					hasMoved = true;
				}
			}

			if (hasMoved)
				a_item.transform.position += force.normalized * 15.0f;
			return hasMoved;
		}

		// Called via broadcast
		public void InvalidateCradleUI()
		{
			m_categoryBounds = null;
			m_textBounds = null;
		}

		public CradleGraphManager graph { get; set; }
		public List<CategoryItemManager> items { get { return m_items; } }
		public List<CategoryEdge> edges { get { return m_edges; } }
		public RectTransform textTransform { get { return m_child; } }
	}
}