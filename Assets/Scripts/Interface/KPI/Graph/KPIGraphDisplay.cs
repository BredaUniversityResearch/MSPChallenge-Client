using System.Collections.Generic;
using KPI;
using UnityEngine;

public class KPIGraphDisplay: MonoBehaviour
{
	private class GraphPoint
	{
		public readonly int month;
		public readonly float value;

		public GraphPoint(int month, float value)
		{
			this.month = month;
			this.value = value;
		}
	};

	private class GraphPointList
	{
		private List<GraphPoint> graphPoints = new List<GraphPoint>();
		private List<Vector2> scaledGraphPoints = new List<Vector2>();

		private GraphPoint baseValuePoint = null;

		private readonly MinMax currentBounds = null;
		private readonly float scaleLogExponent;

		public GraphPointList(float logExponent)
		{
			currentBounds = new MinMax(-1.0f, 1.0f);
			scaleLogExponent = logExponent;
		}

		public void AddPoint(int month, float value)
		{
			GraphPoint point = new GraphPoint(month, value);

			if (baseValuePoint == null || baseValuePoint.month > point.month)
			{
				baseValuePoint = point;
				RescaleAllGraphPoints();
			}

			graphPoints.Add(point);
			scaledGraphPoints.Add(RescalePoint(point));
		}

		private void RescaleAllGraphPoints()
		{
			if (graphPoints.Count != scaledGraphPoints.Count)
			{
				Debug.LogErrorFormat("GraphPoints count doesn't match up to the scale graph points count. Points: {0} / Scaled points: {1}", graphPoints.Count, scaledGraphPoints.Count);
				return;
			}

			for (int i = 0; i < graphPoints.Count; ++i)
			{
				scaledGraphPoints[i] = RescalePoint(graphPoints[i]);
			}
		}

		private Vector2 RescalePoint(GraphPoint referencePoint)
		{
			float baseValue = baseValuePoint.value;
			float rescaledValue = 0.0f;
			if (baseValue != 0.0f)
			{
				float relativeBaseValue = referencePoint.value / baseValue;
				rescaledValue = Mathf.Log(relativeBaseValue, scaleLogExponent);
			}
			return new Vector2(referencePoint.month, currentBounds.GetRelative(rescaledValue));
		}

		public IEnumerable<Vector2> GetScaledGraphPoints()
		{
			return scaledGraphPoints;
		}

		public GraphPoint GetPointByMonth(int gameMonth, int graphMonthInterval)
		{
            //Lol
            return graphPoints[gameMonth / graphMonthInterval];
		}

		public void Clear()
		{
			baseValuePoint = null;
			graphPoints.Clear();
			scaledGraphPoints.Clear();
		}
	}

	private class GraphEntry
	{
		public readonly WMG_Series series = null;
		public GraphPointList graphPoints = new GraphPointList(10.0f);
		public KPIValue activeValue = null;

		public GraphEntry(WMG_Series graphSeries)
		{
			series = graphSeries;
		}
	}

	[SerializeField]
	private WMG_Axis_Graph targetGraph = null;

	[SerializeField]
	private RectTransform hoverNodeMarker = null;

	[SerializeField]
	private KPIGraphMouseMarker mouseMarker = null;

	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;

	private List<GraphEntry> availableGraphSeries = new List<GraphEntry>(16);

    [SerializeField]
    private int graphMonthInterval = 12;

    private void Awake()
    {
        if (Main.MspGlobalData != null)
        {
            SetXScale();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }
    }

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        SetXScale();
    }

    void SetXScale()
    {
        targetGraph.xAxis.AxisMaxValue = Main.MspGlobalData.session_end_month;
    }

    private string CreateTooltipTextLabel(WMG_Series aSeries, WMG_Node aNode)
	{
		// Find out the point value data for this node
		GraphEntry graphEntry = FindGraphBySeries(aSeries);
		Vector2 nodeData = aSeries.getNodeValue(aNode);
		int month = (int)nodeData.x;
		GraphPoint point = graphEntry.graphPoints.GetPointByMonth(month, graphMonthInterval);

		float startValue = graphEntry.activeValue.GetKpiValueForMonth(0);

		string changePercentage = KPIValue.FormatRelativePercentage(startValue, point.value);

		ConvertedUnit displayUnit;
		if (valueConversionCollection != null)
		{
			displayUnit = valueConversionCollection.ConvertUnit(point.value, graphEntry.activeValue.unit);
		}
		else
		{
			displayUnit = new ConvertedUnit(point.value, graphEntry.activeValue.unit, 2);
		}

		return string.Format(Localisation.NumberFormatting, "{0} ({1})\n{2} ({3})",
			aSeries.seriesName, Util.MonthToText(month, true), displayUnit.FormatAsString(), changePercentage);
	}

	private void KPIGraphPointAnimator(WMG_Series series, WMG_Node aNode, bool state)
	{
		RectTransform nodeTransform = (RectTransform)aNode.transform;
		if (hoverNodeMarker != null)
		{
			float x = nodeTransform.anchoredPosition.x;

			hoverNodeMarker.anchoredPosition = new Vector3(x, hoverNodeMarker.anchoredPosition.y, 0.0f);
		}

		if (mouseMarker != null)
		{
			mouseMarker.SetFixedMarkerPoint(nodeTransform.anchoredPosition, state);
		}
	}

	private GraphEntry FindGraphBySeries(WMG_Series graphSeries)
	{
		return availableGraphSeries.Find(obj => obj.series == graphSeries);
	}

	private GraphEntry FindGraphByActiveKPIValue(KPIValue activeValue)
	{
		return availableGraphSeries.Find(obj => obj.activeValue == activeValue);
	}

	//Reference set from UnityEditor
	public void ToggleGraph(KPIValue targetValue, bool isVisible)
	{
		if (isVisible)
		{
			if (availableGraphSeries.Count == 0)
			{
				PrimeGraphs();
			}

			ShowGraph(targetValue);
		}
		else
		{
			HideGraph(targetValue);
		}
	}

	//Reference set from UnityEditor
	public void GraphColorChanged(KPIValue targetValue, Color newGraphColor)
	{
		GraphEntry entry = FindGraphByActiveKPIValue(targetValue);
		if (entry != null)
		{
			entry.series.lineColor = newGraphColor;
			entry.series.pointColor = newGraphColor;
		}
		else
		{
			Debug.LogWarning("Got color change request for a KPI that is not currently in the active graphs list.");
		}
	}

	private WMG_Series CreateGraph()
	{
		WMG_Series serie = targetGraph.addSeries();
		
		serie.hidePoints = true;
		serie.hideLines = true;

		serie.tooltipPointAnimator = KPIGraphPointAnimator;
		//serie.animOffset = Random.value;

		return serie;
	}

	private void PrimeGraphs()
	{
		targetGraph.theTooltip.tooltipLabeler = CreateTooltipTextLabel;

		for (int i = 0; i < availableGraphSeries.Capacity; ++i)
		{
			GraphEntry graphEntry = new GraphEntry(CreateGraph());
			availableGraphSeries.Add(graphEntry);
		}
	}

	private void ShowGraph(KPIValue valueToShow)
	{
		GraphEntry targetGraphEntry = null;

		foreach (GraphEntry entry in availableGraphSeries)
		{
			if (targetGraphEntry == null && entry.activeValue == null)
			{
				targetGraphEntry = entry;
			}

			if (entry.activeValue != null && entry.activeValue.name == valueToShow.name)
			{
				HideGraph(entry.activeValue);
				if (targetGraphEntry == null)
				{
					targetGraphEntry = entry;
				}
			}
		}

		if (targetGraphEntry != null)
		{
			targetGraphEntry.activeValue = valueToShow;

			targetGraphEntry.series.name = valueToShow.name;
			targetGraphEntry.series.seriesName = valueToShow.displayName;
			targetGraphEntry.series.lineScale = 0.5f;

			targetGraphEntry.series.hidePoints = false;
			targetGraphEntry.series.hideLines = false;

			targetGraphEntry.series.transform.SetAsLastSibling();


			UpdateGraphValues(targetGraphEntry);

			valueToShow.OnValueUpdated += OnKPIValueUpdated;
		}
		else
		{
			Debug.LogError("Could not find an inactive graph to display the value. Need to increase the number of graphs!");
		}
	}

	private void OnKPIValueUpdated(KPIValue value)
	{
		GraphEntry entry = FindGraphByActiveKPIValue(value);
		if (entry != null)
		{
			UpdateGraphValues(entry);
		}
		else
		{
			Debug.LogError("Got a KPI Update request for a KPI that is not currently active in the graph.");
		}
	}

	private void UpdateGraphValues(GraphEntry entry)
	{
		entry.graphPoints.Clear(); 
		for (int i = 0; i < entry.activeValue.MostRecentMonth; i += graphMonthInterval)
		{
			entry.graphPoints.AddPoint(i, (float)entry.activeValue.GetKpiValueForMonth(i));
		}

		entry.series.pointValues.SetList(entry.graphPoints.GetScaledGraphPoints());
	}

	private void HideGraph(KPIValue valueToShow)
	{
		GraphEntry entry = FindGraphByActiveKPIValue(valueToShow);
		if (entry != null)
		{
			entry.series.hidePoints = true;
			entry.series.hideLines = true;
			entry.series.name = "EmptySeries";
			entry.graphPoints.Clear();

			entry.activeValue.OnValueUpdated -= OnKPIValueUpdated;
			entry.activeValue = null;
		}
		else
		{
			Debug.LogError("Got a hide request for a KPI Value which is not currently active.");
		}
	}
}
