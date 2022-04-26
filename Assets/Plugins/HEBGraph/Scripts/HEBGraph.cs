using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;
using Sirenix.OdinInspector;
using ColourPalette;


namespace HEBGraph
{
	public class HEBGraph : SerializedMonoBehaviour
	{
		const float GROUP_BG_SPACING = 0.1f;

		[SerializeField] TextAsset m_HEBConfigFile;
		[SerializeField] GameObject m_entryPrefab;
		[SerializeField] Transform m_entryParent;
		[SerializeField] GameObject m_groupTextPrefab, m_groupTextPrefabDown;
		[SerializeField] Transform m_groupTextParent;
		[SerializeField] GameObject m_linePrefab;
		[SerializeField] GameObject m_modalPrefab;
		[SerializeField] UIRadialDrawer m_groupBackground;
		[SerializeField] Transform m_lineParent;
		[SerializeField] int m_lineSections;
		[SerializeField] ColourAsset m_defaultLineColour;
		[SerializeField] Dictionary<int, ColourAsset> m_severityColors;
		[SerializeField] Dictionary<int, ColourAsset> m_severityHLColors;

		[HideInInspector] public Action<string> m_linkClickCallback;
		Dictionary<int, HEBGraphLeaf> m_leaves;
		Dictionary<(int lowId, int highId), UILineDrawer> m_lines;
		List<HEBGraphBranch> m_branches;
		float m_totalRadialOffset;
		HEBGraphLeaf m_selectedLeaf;
		BezierDrawer m_bezierDrawer;

		void Start()
		{
			m_bezierDrawer = new BezierDrawer();
			HEBGraphData data = JsonConvert.DeserializeObject<HEBGraphData>(m_HEBConfigFile.text);

			m_leaves = new Dictionary<int, HEBGraphLeaf>();
			m_branches = new List<HEBGraphBranch>();
			m_lines = new Dictionary<(int low, int high), UILineDrawer>();
			List<(float start, float end)> gaps = new List<(float start, float end)>();
			List<float> gapStarts = new List<float>();

			//Create groups and items
			foreach (HEBGraphDataGroup group in data.groups)
			{
				m_branches.Add(new HEBGraphBranch(group, this)); //This adds radial offsets for all created leaves
				gapStarts.Add(m_totalRadialOffset);
				m_totalRadialOffset += 2f;
			}

			//Position elements and gaps
			foreach(HEBGraphBranch branch in m_branches)
			{
				branch.SetPosition(m_totalRadialOffset, this);
			}
			for (int i = 0; i < gapStarts.Count; i++)
			{
				gaps.Add(((gapStarts[i] + 0.5f) / m_totalRadialOffset * Mathf.PI * 2f, (gapStarts[i] + 1.5f) / m_totalRadialOffset * Mathf.PI * 2f));
			}
			m_groupBackground.SetGaps(gaps);

			//Assign links and create lines
			foreach (HEBGraphDataLink link in data.links)
			{
				if (link.toId < link.fromId ? !m_lines.ContainsKey((link.toId, link.fromId)) : !m_lines.ContainsKey((link.fromId, link.toId)))
				{
					UILineDrawer line = GameObject.Instantiate(m_linePrefab, m_lineParent).GetComponent<UILineDrawer>();
					SetLineToLink(line, link);
					m_lines.Add(link.toId < link.fromId ? (link.toId, link.fromId) : (link.fromId, link.toId), line);
				}
				m_leaves[link.fromId].AddLink(link);
				m_leaves[link.toId].AddLink(link);
			}
		}

		public HEBGraphLeaf[] CreateEntries(HEBGraphDataEntry[] a_entries, HEBGraphBranch a_parent)
		{
			HEBGraphLeaf[] result = new HEBGraphLeaf[a_entries.Length];
			for (int i = 0; i < a_entries.Length; i++)
			{
				HEBGraphLeaf leaf = GameObject.Instantiate(m_entryPrefab, m_entryParent).GetComponent<HEBGraphLeaf>();
				m_totalRadialOffset += leaf.Initialise(m_totalRadialOffset, a_entries[i], this, a_parent);
				m_leaves.Add(a_entries[i].id, leaf);
				result[i] = leaf;
			}
			return result;
		}

		public TextMeshProUGUI CreateGroupText(float a_angle, bool a_singleLine)
		{
			CircleText obj;
			if (a_angle > 0f && a_angle < 180f)
			{
				obj = GameObject.Instantiate(m_groupTextPrefabDown, m_groupTextParent).GetComponent<CircleText>();
				obj.m_angularOffset = a_angle - 180f;
			}
			else
			{
				obj = GameObject.Instantiate(m_groupTextPrefab, m_groupTextParent).GetComponent<CircleText>();
				obj.m_angularOffset = a_angle;
				if (a_singleLine)
					obj.m_radius -= 30f;
			}
			obj.UpdateCurve();

			return obj.GetComponent<TextMeshProUGUI>();
		}

		void SetLineToLink(UILineDrawer a_line, HEBGraphDataLink a_link)
		{
			HEBGraphLeaf from = m_leaves[a_link.fromId];
			HEBGraphLeaf to = m_leaves[a_link.toId];

			float[] splitPoints;
			if (from.Parent == to.Parent)
				splitPoints = m_bezierDrawer.Bezier2D(new List<Vector2> { from.Position, from.Parent.Position, to.Position }, m_lineSections);
			else
				splitPoints = m_bezierDrawer.Bezier2D(new List<Vector2> { from.Position, from.Parent.Position, new Vector2(0.5f, 0.5f), to.Parent.Position, to.Position }, m_lineSections);
			Vector2[] points = new Vector2[m_lineSections];
			points[0] = from.Position;
			for (int i = 2; i < splitPoints.Length; i += 2)
			{
				points[i / 2] = new Vector2(splitPoints[i], splitPoints[i + 1]);
			}
			a_line.Points = points;
		}

		public void OnLeafSelected(HEBGraphLeaf a_leaf)
		{
			if (m_selectedLeaf == null)
			{
				foreach (var kvp in m_lines)
					kvp.Value.gameObject.SetActive(false);
			}
			else
			{
				m_selectedLeaf.Deselect();
				foreach (HEBGraphDataLink link in m_selectedLeaf.Links)
				{
					m_leaves[link.fromId == m_selectedLeaf.ID ? link.toId : link.fromId].ClearHighlight();
					UILineDrawer line = link.toId < link.fromId ? m_lines[(link.toId, link.fromId)] : m_lines[(link.fromId, link.toId)];
					line.gameObject.SetActive(false);
				}
			}
			m_selectedLeaf = a_leaf;

			foreach (HEBGraphDataLink link in m_selectedLeaf.Links)
			{
				UILineDrawer line = link.toId < link.fromId ? m_lines[(link.toId, link.fromId)] : m_lines[(link.fromId, link.toId)];
				line.gameObject.SetActive(true);
				line.color = m_severityColors[link.severity].GetColour();
				m_leaves[link.fromId == m_selectedLeaf.ID ? link.toId : link.fromId].HighlightLink(link, m_severityColors[link.severity], m_severityHLColors[link.severity]);
			}
		}

		public void ClearLeafSelection()
		{
			if (m_selectedLeaf != null)
			{
				m_selectedLeaf.Deselect();
				foreach (HEBGraphDataLink link in m_selectedLeaf.Links)
				{
					m_leaves[link.fromId == m_selectedLeaf.ID ? link.toId : link.fromId].ClearHighlight();
				}
			}

			m_selectedLeaf = null;
			foreach (var kvp in m_lines)
			{
				kvp.Value.gameObject.SetActive(true);
				kvp.Value.color = m_defaultLineColour.GetColour();
			}
		}
	}
}
  
