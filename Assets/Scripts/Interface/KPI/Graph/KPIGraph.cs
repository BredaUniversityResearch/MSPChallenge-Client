using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using KPI;

public class KPIGraph : MonoBehaviour
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
		private MinMax currentBounds = null;
		public MinMax Bounds { get { return currentBounds; } }

		public void AddPoint(int month, float value)
		{
			if (currentBounds == null)
			{
				if (value == 0.0f)
				{
					currentBounds = new MinMax(-0.05f, 0.05f);
				}
				else
				{
					currentBounds = new MinMax(value * 0.95f, value * 1.05f);
				}
			}

			GraphPoint point = new GraphPoint(month, value);
			graphPoints.Add(point);
			scaledGraphPoints.Add(RescalePoint(point));

			RescaleGraphIfRequired(value);
		}

		private void RescaleGraphIfRequired(float addedValue)
		{
			bool shouldRescale = false;

			if (currentBounds.min > addedValue)
			{
				currentBounds.min = addedValue;
				shouldRescale = true;
			}
			if (currentBounds.max < addedValue)
			{
				currentBounds.max = addedValue;
				shouldRescale = true;
			}

			if (shouldRescale)
			{
				if (currentBounds.max == currentBounds.min)
				{
					currentBounds.min *= 0.95f;
					currentBounds.max *= 1.05f;
				}

				RescaleAllGraphPoints();
			}
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
			return new Vector2(referencePoint.month, currentBounds.GetRelative(referencePoint.value));
		}

		public IEnumerable<Vector2> GetScaledGraphPoints()
		{
			return scaledGraphPoints;
		}

		public GraphPoint GetPointByMonth(int gameMonth)
		{
			//Lol
			return graphPoints[gameMonth / 12];
		}
	}

	private class GraphEntry
	{
		public readonly string graphName;
		public readonly Color graphColor;
		public WMG_Series series = null;
		public GraphPointList graphPoints = new GraphPointList();

		public GraphEntry(string name, Color color)
		{
			graphName = name;
			graphColor = color;
		}
	}

	public WMG_Axis_Graph graph;
	public RawImage dateMarker;
	public Image nodeMarker;

	private List<GraphEntry> registeredGraphs = new List<GraphEntry>();
	private Dictionary<int, Image> planMarkers = new Dictionary<int, Image>();
	public GameObject planMarkerPrefab;
	public Transform planMarkerLocation;

	public void CreateGraph(string graphName, Color graphColor)
	{
		registeredGraphs.Add(new GraphEntry(graphName, graphColor));
	}

	private string CreateTooltipTextLabel(WMG_Series aSeries, WMG_Node aNode)
	{
		// Find out the point value data for this node
		GraphEntry graphEntry = FindGraphBySeries(aSeries);
		Vector2 nodeData = aSeries.getNodeValue(aNode);
		int month = (int)nodeData.x;
		GraphPoint point = graphEntry.graphPoints.GetPointByMonth(month);
		float min = graphEntry.graphPoints.Bounds.min;
		float max = graphEntry.graphPoints.Bounds.max;

		int year = Main.MspGlobalData.start + (month / 12);

		return string.Format("{0} in {1}: {2}\nGraph range: ({3},{4})", 
			aSeries.seriesName, year, point.value.ToString("N2"), min.ToString("N2"), max.ToString("N2"));
	}

	public void CreateSeries()
	{
		GenerateSeries();
	}

	public void SetAccent(Color col)
	{
		foreach (Image graphic in planMarkers.Values)
		{
			graphic.color = col;
		}
	}

	public void SetDateMarker(int month)
	{
		float year = (float) month / 12.0f;
		float timePercentage = Mathf.Floor(year) / (float)Main.MspGlobalData.session_num_years;

		dateMarker.rectTransform.anchorMin = new Vector2(timePercentage, 0f);
		dateMarker.rectTransform.anchorMax = new Vector2(timePercentage, 1f);
	}

	// maybe first store all the points, and then only at year ticks add them
	public void AddPoint(string subcategoryName, int month, float value)
	{
		GraphEntry entry = FindGraphByName(subcategoryName);
		entry.graphPoints.AddPoint(month, value);
		entry.series.pointValues.SetList(entry.graphPoints.GetScaledGraphPoints());
	}

	// maybe first store all the points, and then only at year ticks add them
	public void SetPoints(string subcategoryName, List<float> points)
	{
		GraphEntry entry = FindGraphByName(subcategoryName);
		//entry.graphPoints.AddPoint(month, value);
		List<Vector2> scaledPoints = new List<Vector2>(points.Count);
		float min = float.PositiveInfinity, max = float.NegativeInfinity;
		for (int i = 0; i < points.Count; i++)
		{
			if (points[i] < min)
				min = points[i];
			if (points[i] > max)
				max = points[i];
		}
		if (min == max)
		{
			//Avoids dividing by 0
			for (int i = 0; i < points.Count; i++)
			{
				scaledPoints.Add(new Vector2(i, 0.5f));
			}
		}
		else
		{
			float diff = max - min;
			for (int i = 0; i < points.Count; i++)
			{
				if (points[i] < min)
					scaledPoints.Add(new Vector2(i, 0));
				else if (points[i] > max)
					scaledPoints.Add(new Vector2(i, 1f));
				else
					scaledPoints.Add(new Vector2(i, (points[i] - min) / diff));
			}
		}

		entry.series.pointValues.SetList(scaledPoints);
	}

	private GraphEntry FindGraphByName(string graphName)
	{
		return registeredGraphs.Find(obj => obj.graphName == graphName);
	}

	private GraphEntry FindGraphBySeries(WMG_Series graphSeries)
	{
		return registeredGraphs.Find(obj => obj.series == graphSeries);
	}

	// Create plan marker at month
	public void CreatePlanMarker(int month)
	{
		if (!planMarkers.ContainsKey(month))
		{
			GameObject go = Instantiate(planMarkerPrefab);
			Image marker = go.GetComponent<Image>();
			go.transform.SetParent(planMarkerLocation, false);
			planMarkers.Add(month, marker);
			marker.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp((float)month, 0f, (float)Main.MspGlobalData.session_end_month), 0f);
			marker.color = TeamManager.CurrentTeamColor;
			marker.gameObject.GetComponent<AddTooltip>().text = "Month: " + month.ToString();
		}
	}

	// Remove plan marker at month
	public void RemovePlanMarker(int month)
	{
		if (planMarkers.ContainsKey(month))
		{
			Destroy(planMarkers[month].gameObject);
			planMarkers.Remove(month);
		}
	}

	// Generate series and assign behaviour
	private void GenerateSeries()
	{
		/*graph.theTooltip.tooltipLabeler = CreateTooltipTextLabel;

		for (int i = 0; i < registeredGraphs.Count; i++)
		{
			GraphEntry graphEntry = registeredGraphs[i];
			WMG_Series serie = graph.addSeries();
			graphEntry.series = serie;
			serie.name = graphEntry.graphName;
			serie.lineColor = graphEntry.graphColor;
			serie.pointColor = graphEntry.graphColor;
			serie.seriesName = graphEntry.graphName;
			serie.hidePoints = true;
			serie.hideLines = true;
			//serie.animOffset = Random.value;

			if (!root.overview.KPIBars[i].isParent)
			{
				serie.linkPrefab = 1;
			}

			int index = i;
			root.overview.KPIBars[i].toggle.onValueChanged.AddListener(delegate (bool dir)
			{
				root.overview.KPIBars[index].title.color = (dir) ? graphEntry.graphColor : Color.white;
				root.overview.KPIBars[index].toggle.graphic.color = (dir) ? graphEntry.graphColor : Color.white;
				graphEntry.series.hidePoints = !dir;
				graphEntry.series.hideLines = !dir;
				graph.ManualResize();
			});

			// Disable child graphs when parent is collapsed
			if (root.overview.KPIBars[i].isParent)
			{
				KPIBar bar = root.overview.KPIBars[i];
				int length = bar.childContainer.GetComponentsInChildren<KPIBar>().Length;
				for (int j = 0; j < length; j++)
				{
					int child = i + j + 1;
					root.overview.KPIBars[i].td.OnMouseDownDelegate += () =>
					{
						if (root.overview.KPIBars[child].toggle.isOn)
						{
							registeredGraphs[child].series.hidePoints = !bar.isExpanded;
							registeredGraphs[child].series.hideLines = !bar.isExpanded;
						}
					};
				}
			}

			root.overview.KPIBars[i].td.OnMouseEnterDelegate += () =>
			{
				graphEntry.series.lineScale = 0.5f;
			};

			root.overview.KPIBars[i].td.OnMouseExitDelegate += () =>
			{
				graphEntry.series.lineScale = 0.25f;
			};

			// Set start values
			root.overview.KPIBars[i].title.color = (root.overview.KPIBars[i].toggle.isOn) ? graphEntry.graphColor : Color.white;
			root.overview.KPIBars[i].toggle.graphic.color = (root.overview.KPIBars[i].toggle.isOn) ? graphEntry.graphColor : Color.white;
			graphEntry.series.hidePoints = !root.overview.KPIBars[i].toggle.isOn;
			graphEntry.series.hideLines = !root.overview.KPIBars[i].toggle.isOn;
		}*/
	}
}