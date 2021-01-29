using UnityEngine;
using System.Collections.Generic;

public class MeasurementState : FSMState
{
    FSM.CursorType previousCursorType;

    private enum ToolState { JUSTACTIVE, DRAGGING, DROPPED };
    private ToolState currentState = ToolState.JUSTACTIVE;

    private Vector3 pointA = Vector3.zero;
    private Vector3 pointB = Vector3.zero;
    private Vector3 diff = Vector3.zero;

    LineRenderer measureLine;
    TextMesh measureText;

    GameObject measureObj;
    GameObject textObj;
    GameObject textObjectsParent;
    List<GameObject> textObjList = new List<GameObject>();
    private float lineSizeMultiplier = 0.8f;
    private Material lineMat;

    float edgeSize = 5.0f;
    float segmentHeight = 2.0f;
    float currentScale = 0.0f;

    MapScaleToolButton stateToggle;

    public MeasurementState(FSM fsm, MapScaleToolButton stateToggle) : base(fsm)
    {
        this.stateToggle = stateToggle;
    }

    public override void EnterState(Vector3 currentMousePosition)
    {
        base.EnterState(currentMousePosition);

        //Cache previous cursor & Set cursor
        previousCursorType = fsm.CurrentCursorType;
        fsm.SetCursor(FSM.CursorType.Ruler);

        currentState = ToolState.JUSTACTIVE;
        lineMat = Resources.Load<Material>("MeasurementLineMaterial");
        textObjectsParent = new GameObject("TextObjectsParent");
        measureObj = new GameObject("Measure Object");
        textObj = new GameObject("Text");
        measureObj.SetActive(false);
        textObjectsParent.transform.parent = InterfaceCanvas.Instance.transform;
        measureObj.transform.parent = InterfaceCanvas.Instance.transform;
        textObj.transform.parent = InterfaceCanvas.Instance.transform;
        measureLine = measureObj.AddComponent<LineRenderer>();
        measureLine.startColor = Color.black;
        measureLine.endColor = Color.black;
        measureLine.material = lineMat;
        measureText = textObj.AddComponent<TextMesh>();

        measureText.alignment = TextAlignment.Center;
        measureText.anchor = TextAnchor.MiddleCenter;
        measureText.characterSize = 1.0f;
        measureText.fontSize = 40;
        measureText.color = Color.black;
        stateToggle.SetSelected(true);
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        MonoBehaviour.Destroy(textObj);
        MonoBehaviour.Destroy(measureObj);
        MonoBehaviour.Destroy(textObjectsParent);
        base.ExitState(currentMousePosition);
        fsm.SetCursor(previousCursorType);
        stateToggle.SetSelected(false);
    }

    public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        if (currentState == ToolState.JUSTACTIVE)
        {
            //Create line
            pointA = GetMousePos();
            pointB = GetMousePos();
            measureObj.SetActive(true);
            textObjectsParent.SetActive(true);
            textObj.SetActive(true);
            currentState = ToolState.DRAGGING;
        }
    }

    public override void LeftMouseButtonDown(Vector3 position)
    {
        if (currentState == ToolState.DROPPED)
            fsm.SetInterruptState(null);        
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        if (currentState == ToolState.DRAGGING)
            currentState = ToolState.DROPPED;        
    }

    public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        if (currentState == ToolState.DRAGGING)
        {
            //Update line
            pointB = GetMousePos();
            RedrawLineAndText();
        }
    }

    private void RedrawLineAndText()
    {
        float lineWidth = (lineSizeMultiplier * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
        List<Vector3> vertices = MakeLine(lineWidth);
        measureLine.positionCount = vertices.Count;
        measureLine.SetPositions(vertices.ToArray());

        //Create Text under Line
        diff = (pointB - pointA);
        Vector3 upVec = Vector3.Cross(diff.normalized, Vector3.forward);
        if (Vector3.Dot(upVec, Vector3.up) > 0)
        {
            textObj.transform.up = upVec;
        }
        else
        {
            textObj.transform.up = -upVec;
        }

        Vector3 realSize = InterfaceCanvas.Instance.mapScale.GetRealWorldSize(diff);
        measureText.text = realSize.magnitude.ToString("F2") + "km ( " + (realSize.magnitude * 0.539957f).ToString("n2") + "nm)";

        measureLine.widthMultiplier = lineWidth;
        measureText.characterSize = lineWidth * 2.5f;
        textObj.transform.position = pointA + diff / 2.0f - textObj.transform.up * (measureText.characterSize * 2.0f);

        if (lineWidth != currentScale) //Update on Scroll
        {
            currentScale = lineWidth;

            vertices = MakeLine(lineWidth);
            measureLine.positionCount = vertices.Count;
            measureLine.SetPositions(vertices.ToArray());
        }
    }

    private Vector3 GetMousePos()
    {
        Vector3 position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
        position.z = -450.0f;
        return position;
    }

    private List<Vector3> MakeLine(float scalar)
    {
        Vector3 lineDir = (pointB - pointA).normalized;
        List<Vector3> vertices = CreateEdge(pointA, lineDir, scalar * edgeSize);
        vertices.AddRange(CreateMiddle(pointA, pointB, scalar * segmentHeight));
        vertices.AddRange(CreateEdge(pointB, lineDir, scalar * edgeSize));
        return vertices;
    }

    private List<Vector3> CreateEdge(Vector3 center, Vector3 lineDir, float scalar)
    {
        List<Vector3> vertices = new List<Vector3>();
        vertices.Add(center + Vector3.Cross(Vector3.forward, lineDir) * scalar);
        vertices.Add(center - Vector3.Cross(Vector3.forward, lineDir) * scalar);
        vertices.Add(center);
        return vertices;
    }

    public override void HandleCameraZoomChanged()
    {
        RedrawLineAndText();
    }

    private List<Vector3> CreateMiddle(Vector3 start, Vector3 end, float scalar)
    {
        List<Vector3> textLocations = new List<Vector3>();
        List<Vector3> vertices = new List<Vector3>();
        Vector3 ingameDiff = end - start;
        Vector3 ingameDir = ingameDiff.normalized;
        float ingameDiffDistance = ingameDiff.magnitude;
        Vector3 realSize = InterfaceCanvas.Instance.mapScale.GetRealWorldSize(ingameDiff);
        float realDistance = realSize.magnitude;
        float ratio = ingameDiffDistance / realDistance;
        float segmentSize = 10.0f;
        int smallSegmentsPerBigSegment = 5;
        //Change segmentsize depending on depth
        if (scalar < 0.5f)
        {
            segmentSize = 2.0f;
        }
        if (scalar < 0.1)
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
        vertices.Add(start);
        for (int i = 1; i < (realDistance / segmentSize); i++)
        {
            Vector3 offset = start + (ingameDir * segmentSize * ratio) * i;
            vertices.Add(offset);
            if (i % smallSegmentsPerBigSegment == 0) //Big Segment Line
            {
                textLocations.Add(offset + perpendicularDir * 2.0f * scalar);
                vertices.Add(offset + perpendicularDir * 2.0f * scalar);
            }
            else
            {
                vertices.Add(offset + perpendicularDir * scalar);
            }
            vertices.Add(offset);
        }
        vertices.Add(end);

        // Just to make the line render properly
        vertices.Add(start);
        vertices.Add(end);

        AddTextToSegments(textLocations, perpendicularDir, smallSegmentsPerBigSegment, segmentSize, scalar);

        return vertices;
    }

    private void AddTextToSegments(List<Vector3> textLocations, Vector3 upDir, int smallSegmentsPerBigSegment, float segmentSize, float scalar)
    {
        for (int i = 0; i < textLocations.Count; i++)
        {
            if (textObjList.Count <= i)
            {
                GameObject tObj = new GameObject("Text" + i);
                tObj.transform.parent = textObjectsParent.transform;
                TextMesh tTextMesh = tObj.AddComponent<TextMesh>();
                tTextMesh.text = ((i + 1) * segmentSize * smallSegmentsPerBigSegment).ToString();
                tTextMesh.characterSize = 1.0f * scalar;
                tTextMesh.fontSize = 30;
                tTextMesh.alignment = TextAlignment.Center;
                tTextMesh.anchor = TextAnchor.MiddleCenter;
                tTextMesh.color = Color.black;
                measureLine.material = lineMat;
                tObj.transform.position = textLocations[i] + upDir * 1.5f * scalar;
                textObjList.Add(tObj);
            }
            else
            {
                GameObject tObj = textObjList[i];
                TextMesh tTextMesh = tObj.GetComponent<TextMesh>();
                tTextMesh.text = ((i + 1) * segmentSize * smallSegmentsPerBigSegment).ToString();
                tTextMesh.characterSize = 1.0f * scalar;
                tObj.transform.position = textLocations[i] + upDir * 1.5f * scalar;
                tObj.SetActive(true);
            }
        }
        for (int i = textLocations.Count; i < textObjList.Count; i++)
        {
            textObjList[i].SetActive(false);
        }
    }
}

