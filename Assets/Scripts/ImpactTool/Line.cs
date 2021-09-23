using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CradleImpactTool
{
	public class Line : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		static readonly Color DefaultColour = Color.grey;

		RectTransform m_rectTransform;
		Image m_image;
		Color m_colour;
		PositionGetter m_fromPos;
		PositionGetter m_toPos;
		float m_thickness;
		float m_offset;

		CradleGraphManager m_graph;
		LineData m_lineData;
		ImpactTypeData m_impact;
		Vector2 m_directionCache;
		private void Awake()
		{
			m_rectTransform = GetComponent<RectTransform>();
			m_image = GetComponent<Image>();
			Reset();
		}

		void Reset()
		{
			m_colour = DefaultColour;
			m_fromPos = null;
			m_toPos = null;
			m_thickness = 0.0f;
			m_offset = 0.0f;
			m_directionCache = Vector2.zero;

			isDirty = true;
		}

		public bool SetDrawingData(GameObject a_fromObject, GameObject a_toObject, float a_offset, float a_thickness, Color? a_colour = null)
		{
			Reset();

			m_fromPos = new PositionGetter(a_fromObject);
			m_toPos = new PositionGetter(a_toObject);
			if ((m_toPos.position - m_fromPos.position).sqrMagnitude == 0)
			{
				Debug.LogWarning($"{name} tried to SetDrawingData for a line of length 0, from GameObject {a_fromObject.name} to GameObject {a_toObject.name} which is invalid.");
				Reset();
				return false;
			}

			m_offset = a_offset;
			m_thickness = a_thickness;
			m_colour = a_colour.HasValue ? a_colour.Value : DefaultColour;
			isDirty = true;

			SetPosition();
			return true;
		}

		public bool SetDrawingData(Vector2 a_fromPosition, Vector2 a_toPosition, float a_offset, float a_thickness, Color? a_colour = null)
		{
			Reset();

			if ((a_toPosition - a_fromPosition).sqrMagnitude == 0)
			{
				Debug.LogWarning($"{name} tried to SetDrawingData for a line of length 0, which is invalid.");
				return false;
			}

			m_fromPos = new PositionGetter(a_fromPosition, transform.parent.gameObject);
			m_toPos = new PositionGetter(a_toPosition, transform.parent.gameObject);
			m_offset = a_offset;
			m_thickness = a_thickness;
			m_colour = a_colour.HasValue ? a_colour.Value : DefaultColour;
			isDirty = true;

			SetPosition();
			return true;
		}

		public void SetImpactData(CradleGraphManager a_graph, LineData a_lineData, ImpactTypeData a_impact)
		{
			m_graph = a_graph;
			m_lineData = a_lineData;
			m_impact = a_impact;
		}

		void SetPosition()
		{
			if (isDirty == false || m_fromPos == null || m_toPos == null)
				return;

			m_image.color = m_colour;

			// Original positions
			Vector3 fromPosition = m_fromPos.position;
			Vector3 toPosition = m_toPos.position;

			// Calculate offsets
			Vector3 positionDelta = toPosition - fromPosition;
			Vector3 dirNormalised = positionDelta.normalized;
			Vector3 right = new Vector3(-dirNormalised.y, dirNormalised.x, 0);
			Debug.Assert(positionDelta.sqrMagnitude != 0, $"Tried to create a line with distance being 0!");

			fromPosition += right * (m_offset * transform.lossyScale.x);
			toPosition += right * (m_offset * transform.lossyScale.x);

			m_rectTransform.position = (fromPosition + toPosition) * 0.5f;
			m_rectTransform.sizeDelta = new Vector2(positionDelta.magnitude, m_thickness) / transform.lossyScale.x;
			m_rectTransform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, dirNormalised));

			isDirty = false;
		}

		private void Update()
		{
			SetPosition();
		}

		// Called via broadcast
		public void InvalidateCradleUI()
		{
			isDirty = true;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (m_lineData == null || m_graph == null)
				return;

			Vector2 uiDir = new Vector2(1, -1);
			Vector2 windowSize = m_graph.modal.GetTargetSize();
			Vector2 windowOffset = windowSize * uiDir * 0.25f;
			Vector2 pixelOffset = uiDir * 15.0f; // TODO: Get rid of magic number
			Vector2 totalOffset = windowOffset + pixelOffset;
			Vector2 targetPosition = (Vector2)Input.mousePosition + totalOffset;

			string title = m_lineData.title;
			m_graph.modal.Show(m_lineData.description, targetPosition, a_titleText: title);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (m_lineData == null || m_graph == null)
				return;

			m_graph.modal.Hide();
		}

		public Vector2 GetClosestPoint(Vector2 a_point)
		{
			return GetClosestPoint(a_point, m_fromPos.position, m_toPos.position);
		}

		public static Vector2 GetClosestPoint(Vector2 a_point, Vector2 a_lineStart, Vector2 a_lineEnd)
		{
			Vector2 AP = a_point - a_lineStart;
			Vector2 AB = a_lineEnd - a_lineStart;

			float magnitudeSquared = AB.sqrMagnitude;
			float projection = Vector2.Dot(AP, AB);
			float distance = projection / magnitudeSquared;

			if (distance < 0)
			{
				return a_lineStart;
			}
			else if (distance > 1)
			{
				return a_lineEnd;
			}

			return a_lineStart + AB * distance;
		}

		public Vector2 GetDirection()
		{
			if (m_directionCache.sqrMagnitude == 0)
				m_directionCache = (m_toPos.localPosition - m_fromPos.localPosition).normalized;

			return m_directionCache;
		}

		public bool isDirty { get; set; }
		public RectTransform rectTransform { get { return m_rectTransform; } }
		public Vector2 fromPos { get { return m_fromPos.position; } }
		public Vector2 toPos { get { return m_toPos.position; } }
		public Vector2 center { get { return m_rectTransform.position; } }
	}

	internal class PositionGetter
	{
		private GameObject m_sourceObject;
		private Vector2? m_staticResult;

		public PositionGetter(GameObject a_gameObject)
		{
			Debug.Assert(a_gameObject != null);
			m_sourceObject = a_gameObject;
			m_staticResult = null;
		}

		public PositionGetter(Vector2 a_position, GameObject a_parent)
		{
			m_sourceObject = a_parent;
			m_staticResult = a_position;
		}

		public bool isSourced { get { return m_staticResult.HasValue == false; } }

		public Vector2 position
		{
			get
			{
				if (m_staticResult.HasValue)
					return (Vector2)m_sourceObject.transform.position + m_staticResult.Value * m_sourceObject.transform.lossyScale.x;

				return m_sourceObject.transform.position;
			}
		}

		public Vector2 localPosition
		{
			get
			{
				return m_staticResult.HasValue ? m_staticResult.Value : (Vector2)m_sourceObject.transform.localPosition;
			}
		}
	}
}