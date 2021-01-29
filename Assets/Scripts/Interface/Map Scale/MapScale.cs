using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

public class MapScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Info Display")]
	[SerializeField]
	private BoxCollider2D gameBounds = null;
	[SerializeField]
    private RectTransform scaleIndicatorRect = null;
	[SerializeField]
    private TextMeshProUGUI mapScaleText = null;
    [SerializeField]
    private TextMeshProUGUI xCoordinateText = null;
    [SerializeField]
    private TextMeshProUGUI yCoordinateText = null;
    [SerializeField]
    private TextMeshProUGUI zoomText = null;

    [Header("Buttons")]
    [SerializeField]
    private RectTransform expandButtonLabel = null;
    [SerializeField]
    private Image expandButtonBorder = null;
    [SerializeField]
    private AnimationCurve openAnimationCurve = null;
    [SerializeField]
    private float animationDuration = 0.2f;
    [SerializeField]
    private float closeDelay = 0.2f;

    [Header("Button references")]
    [SerializeField]
    private MapScaleToolButton zoomToAreaButton = null;
    [SerializeField]
    private MapScaleToolButton layerProbeButton = null;
    [SerializeField]
    private MapScaleToolButton rulerButton = null;
    [SerializeField]
    private MapScaleToolButton issueVisibilityButton = null;

    private bool animationActive, opening, pointerOnObject;
    private float animationProgress = 1f, timeSincePointOnObject, xPositionTarget;
    private Vector2 labelOriginalPosition;
    private Color originalLabelBorderColor;

    //Size of the currently loaded play area scaled by Main.SCALE.
    private Vector2 currentScaledWorldAreaSize = Vector2.one;

	public void Awake()
	{
		LayerImporter.OnDoneImporting += OnDoneImportingLayers;
        labelOriginalPosition = expandButtonLabel.localPosition;
        xPositionTarget = labelOriginalPosition .x - (GetComponent<RectTransform>().sizeDelta.x - expandButtonLabel.sizeDelta.x);
        originalLabelBorderColor = expandButtonBorder.color;

        zoomToAreaButton.button.onClick.AddListener(ZoomToAreaClicked);
        layerProbeButton.button.onClick.AddListener(LayerProbeClicked);
        rulerButton.button.onClick.AddListener(RulerClicked);
        issueVisibilityButton.button.onClick.AddListener(IssueVisibilityClicked);
        issueVisibilityButton.SetSelected(IssueManager.instance.GetIssueVisibility());
    }

    private void Update()
    {
        double x, y;
        Main.GetRealWorldMousePosition(out x, out y);
        xCoordinateText.text = ((float)x).FormatAsCoordinateText();
        yCoordinateText.text = ((float)y).FormatAsCoordinateText();

        //double x_deg, y_deg;
        ////UTMToDegrees(x, y, out x_deg, out y_deg);
        //ToLatLon(x, y, 29, true, out x_deg, out y_deg);
        //xCoordinateText.text = x_deg.ToString("n2", CultureInfo.CurrentUICulture) + "(" + x.ToString("n2", CultureInfo.CurrentUICulture) + ")";
        //yCoordinateText.text = y_deg.ToString("n2", CultureInfo.CurrentUICulture) + "(" + y.ToString("n2", CultureInfo.CurrentUICulture) + ")";

        if (animationActive)
        {
            if (opening && !pointerOnObject)
            {
                timeSincePointOnObject += Time.deltaTime;
                if (timeSincePointOnObject > closeDelay)
                {
                    opening = false;
                    animationProgress = 1f - animationProgress;
                    expandButtonBorder.color = originalLabelBorderColor;
                }
            }
            animationProgress += Time.deltaTime / animationDuration;
            float newX = 0f;
            if (animationProgress >= 1f)
            {
                newX = opening ? xPositionTarget : labelOriginalPosition.x;
                animationProgress = 1f;

                //Only stop being active if we were closing
                if(!opening)
                    animationActive = false;
            }
            else if (opening)
                newX = Mathf.Lerp(labelOriginalPosition.x, xPositionTarget, openAnimationCurve.Evaluate(animationProgress));
            else
                newX = Mathf.Lerp(xPositionTarget, labelOriginalPosition.x, openAnimationCurve.Evaluate(animationProgress));
            expandButtonLabel.localPosition = new Vector3(newX, labelOriginalPosition.y);
        }
    }

    public void OnDestroy()
	{
		LayerImporter.OnDoneImporting -= OnDoneImportingLayers;
	}

	private void OnDoneImportingLayers()
	{
		AbstractLayer layer = LayerManager.FindFirstLayerContainingName("_PLAYAREA");
		if (layer == null)
		{
			throw new Exception("Could not find the play area layer.");
		}

		currentScaledWorldAreaSize = layer.GetLayerBounds().size;
	}

	/// <summary>
	/// Set the scale using kilometers
	/// </summary>
	public void SetScale(float kilometer)
    {
		Vector3[] fourcorners = new Vector3[4];
		scaleIndicatorRect.GetWorldCorners(fourcorners);
		float km = Mathf.Abs(fourcorners[3].x - fourcorners[0].x)					//Pixel size
			/ InterfaceCanvas.Instance.canvas.pixelRect.width * gameBounds.size.x	//World size
			* (kilometer / CameraManager.Instance.cameraZoom.GetMaxZoom());			//Camera based world size
        float nm = km * 0.539957f;

        mapScaleText.text = km.ToString("n0") + "km (" + nm.ToString("n2") + "nm)";
        zoomText.text = CameraManager.Instance.cameraZoom.currentZoom.ToString("P0");
    }

	public Vector3 GetRealWorldSize(Vector3 size)
	{
		float gameToWorldScale = GameToRealWorldScale;
		return new Vector3(gameToWorldScale * size.x, gameToWorldScale * size.y, 0);
	}

	public float GetRealWorldWidth(float inputWidth)
	{
		float gameToWorldScale = GameToRealWorldScale;
		return gameToWorldScale * inputWidth;
	}

	public float GetRealWorldLineLength(List<Vector3> line)
	{
		//float xConversion = (currentScaledWorldAreaSize.x) / gameBounds.size.x;
		//float yConversion = (currentScaledWorldAreaSize.y) / gameBounds.size.y;
		float gameToWorldScale = GameToRealWorldScale;

		float length = 0;
		for (int i = 1; i < line.Count; ++i)
			length += Mathf.Abs(new Vector2((line[i].x - line[i - 1].x) * gameToWorldScale, (line[i].y - line[i - 1].y) * gameToWorldScale).magnitude);
		return length;
	}

	public float GetRealWorldPolygonAreaInSquareKm(List<Vector3> polygon, List<List<Vector3>> holes = null)
	{
		//float xConversion = (currentScaledWorldAreaSize.x) / gameBounds.size.x;
		//float yConversion = (currentScaledWorldAreaSize.y) / gameBounds.size.y;
		float gameToWorldScale = GameToRealWorldScale;

		float area = 0;
		for (int i = 0; i < polygon.Count; ++i)
		{
			int j = (i + 1) % polygon.Count;
			//area += (polygon[i].y * yConversion) * (polygon[j].x * xConversion) - (polygon[i].x * xConversion) * (polygon[j].y * yConversion);
			area += (polygon[i].x * gameToWorldScale) * 
                (polygon[j].y * gameToWorldScale) - 
                (polygon[i].y * gameToWorldScale) * 
                (polygon[j].x * gameToWorldScale);
		}
		area = Mathf.Abs(area * 0.5f);

		if (holes != null)
			foreach (List<Vector3> hole in holes)
				area -= GetRealWorldPolygonAreaInSquareKm(hole);

		return area;
	}

    public void OnPointerEnter(PointerEventData eventData)
    {
        //If we were closing before, inverse the progress
        if(!opening)
            animationProgress = 1f - animationProgress;
        opening = true;
        animationActive = true;
        pointerOnObject = true;
        expandButtonBorder.color = TeamManager.CurrentTeamColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Only indicate the pointer has left the object, will start closing after a delay
        pointerOnObject = false;
        timeSincePointOnObject = 0;
    }

    public float GameToRealWorldScale
	{
		//Shouldnt matter if we use X or Y, as scale is uniform
		get { return currentScaledWorldAreaSize.x / gameBounds.size.x; }
	}

    public void ZoomIn()
    {
        //Called by UI button
        CameraManager.Instance.cameraZoom.SetZoomLevel(CameraManager.Instance.cameraZoom.currentZoom - 0.05f);
    }

    public void ZoomOut()
    {

        //Called by UI button
        CameraManager.Instance.cameraZoom.SetZoomLevel(CameraManager.Instance.cameraZoom.currentZoom + 0.05f);
    }

    public void ZoomToAreaClicked()
    {
        if (!zoomToAreaButton.selected)
            Main.InterruptFSMState((fsm) => new ZoomToAreaState(fsm, zoomToAreaButton));
        else
            Main.CancelFSMInterruptState();
    }

    public void ZoomAllTheWayOut()
    {
        //Called by UI button
        CameraManager.Instance.cameraZoom.SetZoomLevel(1f);
    }

    public void LayerProbeClicked()
    {
        if (!layerProbeButton.selected)
            Main.InterruptFSMState((fsm) => new LayerProbeState(fsm, layerProbeButton));
        else
            Main.CancelFSMInterruptState();
    }

    public void RulerClicked()
    {
        if (!rulerButton.selected)
            Main.InterruptFSMState((fsm) => new MeasurementState(fsm, rulerButton));
        else
            Main.CancelFSMInterruptState();
    }

    public void IssueVisibilityClicked()
    {
        bool oldVisibility = issueVisibilityButton.selected;
        issueVisibilityButton.SetSelected(!oldVisibility);
        IssueManager.instance.SetIssueVisibility(!oldVisibility);
    }

    public void ShowHideGrid()
    {
        //Currently unused
    }


    void UTMToDegrees(double UTM_Coord_x, double UTM_Coord_y, out double WGS_Coord_x, out double WGS_Coord_y)
    {
        double UTM_x_min_bot = 2426378.0132;
        double UTM_x_max_bot = 6293974.6215;
        double UTM_x_min_top = 3574615.981542;
        double UTM_x_max_top = 5097862.794109;

        double UTM_y_min = 1528101.2618;
        double UTM_y_max = 5446513.5222;

        double WGS_x_min = -10.6700;
        double WGS_x_max = 31.5500;
        double WGS_y_min = 34.5000;
        double WGS_y_max = 71.0500;

        double relativeY = (UTM_Coord_y - UTM_y_min) / (UTM_y_max - UTM_y_min);

        double UTM_x_min_combined = relativeY * (UTM_x_min_top - UTM_x_min_bot) + UTM_x_min_bot;
        double UTM_x_max_combined = relativeY * (UTM_x_max_top - UTM_x_max_bot) + UTM_x_max_bot;
        double relativeX = (UTM_Coord_x - UTM_x_min_combined) / (UTM_x_max_combined - UTM_x_min_combined);

        WGS_Coord_x = relativeX * (WGS_x_max - WGS_x_min) + WGS_x_min;
        WGS_Coord_y = relativeY * (WGS_y_max - WGS_y_min) + WGS_y_min;
    }

    void ToLatLon(double utmX, double utmY, int utmZone, bool northHemi, out double latitude, out double longitude)
    {
        bool isNorthHemisphere = northHemi;

        var diflat = -0.00066286966871111111111111111111111111;
        var diflon = -0.0003868060578;

        var zone = utmZone;
        var c_sa = 6378137.000000;
        var c_sb = 6356752.314245;
        var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
        var e2cuadrada = Math.Pow(e2, 2);
        var c = Math.Pow(c_sa, 2) / c_sb;
        var x = utmX - 500000;
        var y = isNorthHemisphere ? utmY : utmY - 10000000;

        var s = ((zone * 6.0) - 183.0);
        var lat = y / (c_sa * 0.9996);
        var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
        var a = x / v;
        var a1 = Math.Sin(2 * lat);
        var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
        var j2 = lat + (a1 / 2.0);
        var j4 = ((3 * j2) + a2) / 4.0;
        var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
        var alfa = (3.0 / 4.0) * e2cuadrada;
        var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
        var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
        var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
        var b = (y - bm) / v;
        var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
        var eps = a * (1 - (epsi / 3.0));
        var nab = (b * (1 - epsi)) + lat;
        var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
        var delt = Math.Atan(senoheps / (Math.Cos(nab)));
        var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

        longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
        latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
    }
}