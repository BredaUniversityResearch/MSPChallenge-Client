using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class MeasurementState : FSMState
	{
		private FSM.CursorType m_previousCursorType;

		private enum ToolState { JUSTACTIVE, DRAGGING, DROPPED };
		private ToolState m_currentState = ToolState.JUSTACTIVE;

		private Vector3 m_pointA = Vector3.zero;
		private Vector3 m_pointB = Vector3.zero;
		private Vector3 m_diff = Vector3.zero;

		private LineRenderer m_measureLine;
		private TextMesh m_measureText;

		private GameObject m_measureObj;
		private GameObject m_textObj;
		private GameObject m_textObjectsParent;
		readonly List<GameObject> m_textObjList = new List<GameObject>();
		private float m_lineSizeMultiplier = 0.8f;
		private Material m_lineMat;

		private float m_edgeSize = 5.0f;
		private float m_segmentHeight = 2.0f;
		private float m_currentScale = 0.0f;

		private CustomToggle m_stateToggle;

		public MeasurementState(FSM a_fsm, CustomToggle a_stateToggle) : base(a_fsm)
		{
			this.m_stateToggle = a_stateToggle;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			//Cache previous cursor & Set cursor
			m_previousCursorType = m_fsm.CurrentCursorType;
			m_fsm.SetCursor(FSM.CursorType.Ruler);

			m_currentState = ToolState.JUSTACTIVE;
			m_lineMat = Resources.Load<Material>("MeasurementLineMaterial");
			m_textObjectsParent = new GameObject("TextObjectsParent");
			m_measureObj = new GameObject("Measure Object");
			m_textObj = new GameObject("Text");
			m_measureObj.SetActive(false);
			m_textObjectsParent.transform.parent = InterfaceCanvas.Instance.transform;
			m_measureObj.transform.parent = InterfaceCanvas.Instance.transform;
			m_textObj.transform.parent = InterfaceCanvas.Instance.transform;
			m_measureLine = m_measureObj.AddComponent<LineRenderer>();
			m_measureLine.startColor = Color.black;
			m_measureLine.endColor = Color.black;
			m_measureLine.material = m_lineMat;
			m_measureText = m_textObj.AddComponent<TextMesh>();

			m_measureText.alignment = TextAlignment.Center;
			m_measureText.anchor = TextAnchor.MiddleCenter;
			m_measureText.characterSize = 1.0f;
			m_measureText.fontSize = 40;
			m_measureText.color = Color.black;
			m_stateToggle.isOn = true;
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			MonoBehaviour.Destroy(m_textObj);
			MonoBehaviour.Destroy(m_measureObj);
			MonoBehaviour.Destroy(m_textObjectsParent);
			base.ExitState(a_currentMousePosition);
			m_fsm.SetCursor(m_previousCursorType);
			m_stateToggle.isOn = false;
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_currentState != ToolState.JUSTACTIVE)
				return;
			//Create line
			m_pointA = GetMousePos();
			m_pointB = GetMousePos();
			m_measureObj.SetActive(true);
			m_textObjectsParent.SetActive(true);
			m_textObj.SetActive(true);
			m_currentState = ToolState.DRAGGING;
		}

		public override void LeftMouseButtonDown(Vector3 a_position)
		{
			if (m_currentState == ToolState.DROPPED)
				m_fsm.SetInterruptState(null);        
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_currentState == ToolState.DRAGGING)
				m_currentState = ToolState.DROPPED;        
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_currentState != ToolState.DRAGGING)
				return;
			//Update line
			m_pointB = GetMousePos();
			RedrawLineAndText();
		}

		private void RedrawLineAndText()
		{
			float lineWidth = (m_lineSizeMultiplier * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
			List<Vector3> vertices = MakeLine(lineWidth);
			m_measureLine.positionCount = vertices.Count;
			m_measureLine.SetPositions(vertices.ToArray());

			//Create Text under Line
			m_diff = (m_pointB - m_pointA);
			Vector3 upVec = Vector3.Cross(m_diff.normalized, Vector3.forward);
			if (Vector3.Dot(upVec, Vector3.up) > 0)
			{
				m_textObj.transform.up = upVec;
			}
			else
			{
				m_textObj.transform.up = -upVec;
			}

			Vector3 realSize = InterfaceCanvas.Instance.mapScale.GetRealWorldSize(m_diff);
			m_measureText.text = realSize.magnitude.ToString("F2") + "km ( " + (realSize.magnitude * 0.539957f).ToString("n2") + "nm)";

			m_measureLine.widthMultiplier = lineWidth;
			m_measureText.characterSize = lineWidth * 2.5f;
			m_textObj.transform.position = m_pointA + m_diff / 2.0f - m_textObj.transform.up * (m_measureText.characterSize * 2.0f);

			if (lineWidth == m_currentScale) //Update on Scroll
				return;
			m_currentScale = lineWidth;

			vertices = MakeLine(lineWidth);
			m_measureLine.positionCount = vertices.Count;
			m_measureLine.SetPositions(vertices.ToArray());
		}

		private Vector3 GetMousePos()
		{
			Vector3 position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
			position.z = -450.0f;
			return position;
		}

		private List<Vector3> MakeLine(float a_scalar)
		{
			Vector3 lineDir = (m_pointB - m_pointA).normalized;
			List<Vector3> vertices = CreateEdge(m_pointA, lineDir, a_scalar * m_edgeSize);
			vertices.AddRange(CreateMiddle(m_pointA, m_pointB, a_scalar * m_segmentHeight));
			vertices.AddRange(CreateEdge(m_pointB, lineDir, a_scalar * m_edgeSize));
			return vertices;
		}

		private List<Vector3> CreateEdge(Vector3 a_center, Vector3 a_lineDir, float a_scalar)
		{
			List<Vector3> vertices = new List<Vector3>();
			vertices.Add(a_center + Vector3.Cross(Vector3.forward, a_lineDir) * a_scalar);
			vertices.Add(a_center - Vector3.Cross(Vector3.forward, a_lineDir) * a_scalar);
			vertices.Add(a_center);
			return vertices;
		}

		public override void HandleCameraZoomChanged()
		{
			RedrawLineAndText();
		}

		private List<Vector3> CreateMiddle(Vector3 a_start, Vector3 a_end, float a_scalar)
		{
			List<Vector3> textLocations = new List<Vector3>();
			List<Vector3> vertices = new List<Vector3>();
			Vector3 ingameDiff = a_end - a_start;
			Vector3 ingameDir = ingameDiff.normalized;
			float ingameDiffDistance = ingameDiff.magnitude;
			Vector3 realSize = InterfaceCanvas.Instance.mapScale.GetRealWorldSize(ingameDiff);
			float realDistance = realSize.magnitude;
			float ratio = ingameDiffDistance / realDistance;
			float segmentSize = 10.0f;
			int smallSegmentsPerBigSegment = 5;
			//Change segmentsize depending on depth
			if (a_scalar < 0.5f)
			{
				segmentSize = 2.0f;
			}
			if (a_scalar < 0.1)
			{
				segmentSize = 1.0f;
				smallSegmentsPerBigSegment = 2;
			}

			//Calculate direction for segments
			Vector3 upVec = Vector3.Cross(ingameDir, Vector3.forward);
			Vector3 perpendicularDir = Vector3.zero;
			if (Vector3.Dot(upVec, Vector3.up) > 0)
			{
				perpendicularDir = upVec;
			}
			else
			{
				perpendicularDir = -upVec;
			}

			//Create line
			vertices.Add(a_start);
			for (int i = 1; i < (realDistance / segmentSize); i++)
			{
				Vector3 offset = a_start + (ingameDir * segmentSize * ratio) * i;
				vertices.Add(offset);
				if (i % smallSegmentsPerBigSegment == 0) //Big Segment Line
				{
					textLocations.Add(offset + perpendicularDir * 2.0f * a_scalar);
					vertices.Add(offset + perpendicularDir * 2.0f * a_scalar);
				}
				else
				{
					vertices.Add(offset + perpendicularDir * a_scalar);
				}
				vertices.Add(offset);
			}
			vertices.Add(a_end);

			// Just to make the line render properly
			vertices.Add(a_start);
			vertices.Add(a_end);

			AddTextToSegments(textLocations, perpendicularDir, smallSegmentsPerBigSegment, segmentSize, a_scalar);

			return vertices;
		}

		private void AddTextToSegments(List<Vector3> a_textLocations, Vector3 a_upDir, int a_smallSegmentsPerBigSegment, float a_segmentSize, float a_scalar)
		{
			for (int i = 0; i < a_textLocations.Count; i++)
			{
				if (m_textObjList.Count <= i)
				{
					GameObject tObj = new GameObject("Text" + i);
					tObj.transform.parent = m_textObjectsParent.transform;
					TextMesh tTextMesh = tObj.AddComponent<TextMesh>();
					tTextMesh.text = ((i + 1) * a_segmentSize * a_smallSegmentsPerBigSegment).ToString();
					tTextMesh.characterSize = 1.0f * a_scalar;
					tTextMesh.fontSize = 30;
					tTextMesh.alignment = TextAlignment.Center;
					tTextMesh.anchor = TextAnchor.MiddleCenter;
					tTextMesh.color = Color.black;
					m_measureLine.material = m_lineMat;
					tObj.transform.position = a_textLocations[i] + a_upDir * 1.5f * a_scalar;
					m_textObjList.Add(tObj);
				}
				else
				{
					GameObject tObj = m_textObjList[i];
					TextMesh tTextMesh = tObj.GetComponent<TextMesh>();
					tTextMesh.text = ((i + 1) * a_segmentSize * a_smallSegmentsPerBigSegment).ToString();
					tTextMesh.characterSize = 1.0f * a_scalar;
					tObj.transform.position = a_textLocations[i] + a_upDir * 1.5f * a_scalar;
					tObj.SetActive(true);
				}
			}
			for (int i = a_textLocations.Count; i < m_textObjList.Count; i++)
			{
				m_textObjList[i].SetActive(false);
			}
		}
	}
}
