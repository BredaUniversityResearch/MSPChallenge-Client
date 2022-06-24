using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class VisualizationUtil : MonoBehaviour
	{
		private static VisualizationUtil singleton;
		public static VisualizationUtil Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<VisualizationUtil>();
				return singleton;
			}
		}

		public DataVisualizationSettings VisualizationSettings;

		private const float SELECT_MAX_DISTANCE = 10f;
		private const float MOUSE_MOVE_THRESHOLD = 2f;

		private GameObject pointPrefab;
		private GameObject pointRestrictionPrefab;
		private GameObject areaRestrictionPrefab;
		private GameObject linePrefab;
		private GameObject lineIconPrefab;
		private GameObject textPrefab;
		private GameObject polygonPrefab;
		private Dictionary<string, GameObject> rasterPrefabs;

		public enum PointRenderMode { Default, Outline, AddWithPoint, AddWithoutPoint, RemoveWithPoint, RemoveWithoutPoint, MoveWithPoint, MoveWithoutPoint };
		private Sprite pointOutline;
		private Sprite pointAdd;
		private Sprite pointRemove;
		private Sprite pointMove;

		[HideInInspector] public readonly Color DEFAULT_SELECTION_COLOR = new Color(255 / 255f, 147 / 255f, 30 / 255f); // orange
		[HideInInspector] public readonly Color INVALID_SELECTION_COLOR = new Color(255 / 255f, 255 / 255f, 00 / 255f); // yellow
		[HideInInspector] public Color SelectionColor = new Color(255 / 255f, 147 / 255f, 30 / 255f);
		private Color editColor = Color.white;

		[HideInInspector] public float DisplayScale = 185f;
		[HideInInspector] public float pointResolutionScale = 1f;
		[HideInInspector] public float textResolutionScale = 1f;

		private Camera camera;
		private Camera Camera
		{
			get
			{
				if (camera == null)
					camera = Camera.main;
				return camera;
			}
		}

		void Awake()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;

			pointPrefab = Resources.Load<GameObject>("Point");
			pointRestrictionPrefab = Resources.Load<GameObject>("PointRestriction");
			linePrefab = Resources.Load<GameObject>("Line");
			lineIconPrefab = Resources.Load<GameObject>("LineIcon");
			textPrefab = Resources.Load<GameObject>("Text");
			polygonPrefab = Resources.Load<GameObject>("Polygon");
			areaRestrictionPrefab = (GameObject)Resources.Load("RestrictionPolygon");

			rasterPrefabs = new Dictionary<string, GameObject>();
			Object[] rasterObjects = Resources.LoadAll("RasterPrefabs", typeof(GameObject));
			foreach (Object obj in rasterObjects)
				rasterPrefabs.Add(obj.name, obj as GameObject);

			pointOutline = Resources.Load<Sprite>("Point Outline");
			pointAdd = Resources.Load<Sprite>("Point Add");
			pointRemove = Resources.Load<Sprite>("Point Remove");
			pointMove = Resources.Load<Sprite>("Point Move");

			UpdateDisplayScale();
		}

		void OnDestroy()
		{
			singleton = null;
		}

		public float GetSelectMaxDistance()
		{
			return SELECT_MAX_DISTANCE * Camera.orthographicSize * 2 / Screen.height;
		}

		public float GetSelectMaxDistancePolygon()
		{
			return 0;
		}

		public float GetMouseMoveThreshold()
		{
			return MOUSE_MOVE_THRESHOLD * Camera.orthographicSize * 2 / Screen.height;
		}

		public void UpdateDisplayScale()
		{
			DisplayScale = Camera.orthographicSize * 2.0f / Screen.height * 100.0f;
			pointResolutionScale = Screen.height / 1080f;
			textResolutionScale = Screen.height / (1080f * 20f);
			InterfaceCanvas.SetLineMaterialTiling(1f / (DisplayScale / 5f));
		}

		public GameObject CreateText()
		{
			return GameObject.Instantiate<GameObject>(textPrefab);
		}

		public GameObject CreateRasterGameObject(string prefabName)
		{
			return createRaster(prefabName);
		}

		public GameObject CreatePointGameObject()
		{
			return CreatePoint();
		}

		public void UpdatePointSubEntity(GameObject go, Vector3 position, SubEntityDrawSettings drawSettings, SubEntityPlanState planState, bool selected, bool hover)
		{
			if (go == null)
				return;

			Color c = drawSettings.PointColor;
			if (selected) { c = editColor; }
			else if (hover) { c = SelectionColor; }

			PointRenderMode pointRenderMode = PointRenderMode.Default;
			if (selected)
			{
				//Points with special sprites don't get outlines
				if(drawSettings.PointSprite == null)
					pointRenderMode = PointRenderMode.Outline;
			}
			else if (planState == SubEntityPlanState.Added) { pointRenderMode = PointRenderMode.AddWithPoint; }
			else if (planState == SubEntityPlanState.Removed) { pointRenderMode = PointRenderMode.RemoveWithPoint; }
			else if (planState == SubEntityPlanState.Moved) { pointRenderMode = PointRenderMode.MoveWithPoint; }

			UpdatePoint(go, position, c, drawSettings.PointSize, pointRenderMode, drawSettings.PointSprite);
			UpdatePointScale(go.transform.gameObject, drawSettings);
		}

		public float UpdatePointScale(GameObject go, SubEntityDrawSettings drawSettings)
		{
			float newScale = drawSettings.PointSize * DisplayScale * pointResolutionScale;
			go.transform.localScale = new Vector3(newScale, newScale, 1f);
			return newScale;
		}

		public GameObject CreatePoint()
		{
			return GameObject.Instantiate<GameObject>(pointPrefab);
		}

		public GameObject CreateRestrictionPoint()
		{
			return GameObject.Instantiate<GameObject>(pointRestrictionPrefab);
		}

		public RestrictionArea CreateRestrictionArea()
		{
			return GameObject.Instantiate<GameObject>(areaRestrictionPrefab).GetComponent<RestrictionArea>();
		}

		private GameObject createRaster(string prefabName)
		{
			GameObject prefab;
			if (rasterPrefabs.TryGetValue(prefabName, out prefab))
				return GameObject.Instantiate<GameObject>(prefab);
			else
				Debug.LogError("No prefab with the name: \"" + prefabName + "\" exists in the RasterPrefabs folder. Using a default prefab.");
			return GameObject.Instantiate<GameObject>(rasterPrefabs.GetFirstValue());
		}

		public void UpdatePoint(GameObject go, Vector3 position, Color color, float scale, PointRenderMode renderMode, Sprite pointSprite = null)
		{
			go.transform.gameObject.GetComponent<SpriteRenderer>().color = color;
			go.transform.localPosition = position + new Vector3(0, 0, -0.02f);
			float pointDisplayScale = scale * DisplayScale * pointResolutionScale;
			go.transform.localScale = new Vector3(pointDisplayScale, pointDisplayScale, 1f);


			go.transform.GetChild(0).gameObject.SetActive(renderMode != PointRenderMode.Default);
			SpriteRenderer childRenderer = go.transform.GetChild(0).GetComponent<SpriteRenderer>();

			SpriteRenderer pointRenderer = go.transform.gameObject.GetComponent<SpriteRenderer>();
			if (pointSprite != null)
			{
				pointRenderer.sprite = pointSprite;
			}

			if (renderMode != PointRenderMode.Outline)
			{
				//Constant childrenderer size
				float childScale = 1f / Mathf.Max(0.01f, scale);
				childRenderer.transform.localScale = new Vector3(childScale, childScale, 0);
				childRenderer.sortingOrder = 2;
				childRenderer.color = Color.white;
			}
			else
			{
				childRenderer.transform.localScale = new Vector3(1.2f, 1.2f, 0);
				childRenderer.sortingOrder = 0;
				childRenderer.color = SelectionColor;
			}

			switch (renderMode)
			{
				case PointRenderMode.Outline:
					pointRenderer.enabled = true;
					childRenderer.sprite = pointOutline;
					break;
				case PointRenderMode.AddWithPoint:
					pointRenderer.enabled = true;
					childRenderer.sprite = pointAdd;
					break;
				case PointRenderMode.AddWithoutPoint:
					pointRenderer.enabled = false;
					childRenderer.sprite = pointAdd;
					break;
				case PointRenderMode.RemoveWithPoint:
					pointRenderer.enabled = true;
					childRenderer.sprite = pointRemove;
					break;
				case PointRenderMode.RemoveWithoutPoint:
					pointRenderer.enabled = false;
					childRenderer.sprite = pointRemove;
					break;
				case PointRenderMode.MoveWithPoint:
					pointRenderer.enabled = true;
					childRenderer.sprite = pointMove;
					break;
				case PointRenderMode.MoveWithoutPoint:
					pointRenderer.enabled = false;
					childRenderer.sprite = pointMove;
					break;
			}
		
		}

		public GameObject CreateLineStringGameObject()
		{
			return Object.Instantiate(linePrefab);
		}

		public GameObject CreateLineStringIconObject(string iconName, Color iconColor)
		{
			GameObject go = Object.Instantiate(lineIconPrefab);
			SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
			renderer.sprite = Resources.Load<Sprite>(iconName);
			renderer.color = iconColor;
			if (renderer.sprite == null)
			{
				Debug.Log(string.Format("Could not load sprite with name \"{0}\"", iconName));
			}
			return go;
		}
		
		public GameObject CreateLineSegment()
		{
			GameObject go = GameObject.Instantiate<GameObject>(linePrefab);
			return go;
		}

		public void updateLineSegment(GameObject go, Vector3 a, Vector3 b, Color color)
		{
			go.GetComponent<SpriteRenderer>().color = color;
			go.transform.position = (a + b) * 0.5f;
			go.transform.localPosition = new Vector3(go.transform.localPosition.x, go.transform.localPosition.y, -0.01f);
			go.transform.localScale = new Vector3((a - b).magnitude * 100, 1);
			go.transform.localRotation = Quaternion.identity;
			float angle = Mathf.Atan2((float)(a.y - b.y), (float)(a.x - b.x)) * 180 / Mathf.PI;
			go.transform.Rotate(Vector3.forward, angle, Space.Self);
		}

		public GameObject CreatePolygonGameObject()
		{
			return GameObject.Instantiate<GameObject>(polygonPrefab);
		}
		
		public class TriangulationException : System.Exception
		{
			public TriangulationException()
			{
			}

			public TriangulationException(string message)
				: base(message)
			{
			}

			public TriangulationException(string message, System.Exception inner)
				: base(message, inner)
			{
			}
		}

		public Mesh CreatePolygon(List<Vector3> vertices, List<List<Vector3>> holes, Vector2 patternRandomOffset, bool innerGlow, Rect innerGlowTextureBounds)
		{
			if (vertices.Count < 3) { return null; }
			
			Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();

			poly.outside = vertices;

			if (holes != null && holes.Count > 0)
			{
				poly.holes = holes;
			}

			Mesh mesh = Poly2Mesh.CreateMesh(poly);

			if (mesh != null && mesh.vertexCount > 0)
			{
				Vector2[] uvs = new Vector2[mesh.vertexCount];
				for (int i = 0; i < uvs.Length; i++)
				{
					uvs[i] = patternRandomOffset;
				}

				if (!innerGlow)
				{
					mesh.uv = uvs;
				}
				else
				{
					mesh.uv2 = uvs;

					uvs = mesh.uv;

					Vector2 offset = innerGlowTextureBounds.min;
					float xFactor = 1f / innerGlowTextureBounds.size.x;
					float yFactor = 1f / innerGlowTextureBounds.size.y;

					for (int i = 0; i < uvs.Length; i++)
					{
						uvs[i] -= offset;
						uvs[i] = new Vector2(uvs[i].x * xFactor, uvs[i].y * yFactor);
					}

					mesh.uv = uvs;
				}
			}

			return mesh;
		}

		public PointRenderMode GetPointRenderMode(SubEntityDrawSettings drawSettings, SubEntityPlanState planState, bool selected)
		{
			PointRenderMode pointRenderMode = PointRenderMode.Default;
			if (selected)
			{
				pointRenderMode = PointRenderMode.Outline;
			}
			else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Added)
			{
				pointRenderMode = PointRenderMode.AddWithPoint;
			}
			else if (planState == SubEntityPlanState.Added)
			{
				pointRenderMode = PointRenderMode.AddWithPoint;
			}
			else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Removed)
			{
				pointRenderMode = PointRenderMode.RemoveWithPoint;
			}
			else if (planState == SubEntityPlanState.Removed)
			{
				pointRenderMode = PointRenderMode.RemoveWithoutPoint;
			}
			else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Moved)
			{
				pointRenderMode = PointRenderMode.MoveWithPoint;
			}
			else if (planState == SubEntityPlanState.Moved)
			{
				pointRenderMode = PointRenderMode.MoveWithoutPoint;
			}
			return pointRenderMode;
		}

		public void DestroyChildren(GameObject subject)
		{
			foreach (Transform child in subject.transform)
			{
				GameObject.Destroy(child.gameObject);
			}
			subject.transform.DetachChildren();
		}
	}
}
