using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum OptimizationTarget { COUNTRIES, COUNCILS, TEST };

//ALL CODE THAT IS IMPLEMENTED IN THIS CLASS IS FOR OPTIMIZATION PURPOSES ONLY
//ANYTHING WRITTEN HERE SHOULD AT SOME POINT BE RELOCATED TO ITS APROPRIATE LOCATION
public class LayerOptimizer : MonoBehaviour
{
    public static LayerOptimizer instance;
    public Material mBorderMat;
    [Range(0.0f, 5.0f)]
    public float mLineSizeMultiplier = 1.0f;
    private List<LineRenderer> mLineRenderers = new List<LineRenderer>();

    [SerializeField]
    public List<OptimizationTarget> OptimizeLayersList = new List<OptimizationTarget>();

    [HideInInspector]
    public List<OptimizationTarget> AlreadyOptimized = new List<OptimizationTarget>();

    private float mLastSize = -1;

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        LayerImporter.OnDoneImporting += OptimizeLayers;
    }

    public void Update()
    {
        if(CameraManager.Instance.gameCamera.orthographicSize != mLastSize)
        {
            mLastSize = CameraManager.Instance.gameCamera.orthographicSize;
            foreach (LineRenderer lineRenderer in mLineRenderers)
            {
                float lineWidth = (mLineSizeMultiplier * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
                lineRenderer.widthMultiplier = lineWidth;

                if (lineRenderer.sharedMaterial != mBorderMat)
                {
                    lineRenderer.sharedMaterial = mBorderMat;
                }
            }
        }
    }

    public void OptimizeLayers()
    {
        foreach (AbstractLayer layer in LayerManager.GetLoadedLayers())        
            if (layer.Optimized && layer is PolygonLayer)
                OptimizePolygonLayer(layer as PolygonLayer);
    }

    private void OptimizePolygonLayer(PolygonLayer target)
    {
        CombineInstance[] combine = new CombineInstance[target.GetEntityCount()];
        Material mat = null;

        GameObject tLineParent = new GameObject("Combined Outlines");
        tLineParent.transform.SetParent(target.LayerGameObject.transform, false);
        Gradient gradient = new Gradient();
        Color outlineColor = Color.black;
        if(target.EntityTypes != null)
            outlineColor = target.EntityTypes.GetFirstValue().DrawSettings.LineColor;
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(outlineColor, 0.0f), new GradientColorKey(outlineColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
        );


        int i = 0;
        foreach (PolygonSubEntity sub in target.GetAllSubEntities())
        {
            if (mat == null)
            {
                mat = MaterialManager.GetDefaultPolygonMaterial(sub.DrawSettings.PolygonPatternName, sub.DrawSettings.PolygonColor);
            }

            Mesh mesh = VisualizationUtil.CreatePolygon(sub.GetPoints(), sub.GetHoles(), sub.Entity.patternRandomOffset, sub.DrawSettings.InnerGlowEnabled, target.InnerGlowBounds);

            //Add mesh to combined one
            combine[i].mesh = mesh;
            combine[i].transform = Matrix4x4.identity;

            //Create outline
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
            Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int j = 0; j < triangles.Length; j += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int vert1 = triangles[j + e];
                    int vert2 = triangles[j + e + 1 > j + 2 ? j : j + e + 1];
                    string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                    if (edges.ContainsKey(edge))
                    {
                        edges.Remove(edge);
                    }
                    else
                    {
                        edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                    }
                }
            }

            // Create edge lookup Dictionary
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> edge in edges.Values)
            {
                if (lookup.ContainsKey(edge.Key) == false)
                {
                    lookup.Add(edge.Key, edge.Value);
                }
            }

            // Create line prefab
            LineRenderer linePrefab = new GameObject().AddComponent<LineRenderer>();
            linePrefab.colorGradient = gradient;
            linePrefab.transform.name = sub.GetDatabaseID().ToString();
            linePrefab.positionCount = 0;
            linePrefab.sharedMaterial = mBorderMat;
            linePrefab.transform.SetParent(tLineParent.transform, false);
            linePrefab.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linePrefab.receiveShadows = false;
            float lineWidthScale = (mLineSizeMultiplier * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
            linePrefab.widthMultiplier = lineWidthScale;
            linePrefab.numCapVertices = 1;
            linePrefab.numCornerVertices = 1;

            // Create first line
            mLineRenderers.Add(linePrefab);

            // This vector3 gets added to each line position, so it sits in front of the mesh
            // Change the -0.1f to a positive number and it will sit behind the mesh
            //TODO make this not hardcoded
            Vector3 basePosition = linePrefab.transform.position - new Vector3(0.0f, 0.0f, 0.1f);

            // Loop through edge vertices in order
            int startVert = 0;
            int nextVert = startVert;
            int highestVert = startVert;
            int vertexCount = 0;
            linePrefab.positionCount = vertices.Length;
            while (true)
            {
                // Add to line
                vertexCount++;
                linePrefab.SetPosition(vertexCount - 1, vertices[nextVert] + basePosition);

                // Get next vertex
                nextVert = lookup[nextVert];

                // Store highest vertex (to know what shape to move to next)
                if (nextVert > highestVert)
                {
                    highestVert = nextVert;
                }

                // Shape complete
                if (nextVert == startVert)
                {
                    // Finish this shape's line
                    linePrefab.positionCount = vertexCount + 1;
                    linePrefab.SetPosition(vertexCount, vertices[nextVert] + basePosition);

                    // No more verts
                    break;
                }
            }
            i++;
        }

        //Create combined mesh object
        GameObject tNewObj = new GameObject("Combined Mesh");
        tNewObj.transform.SetParent(target.LayerGameObject.transform, false);
        MeshFilter RootMeshFilt = tNewObj.AddComponent<MeshFilter>();
        MeshRenderer RootMeshRend = tNewObj.AddComponent<MeshRenderer>();
        RootMeshRend.sharedMaterial = mat;
        RootMeshFilt.mesh = new Mesh();
        RootMeshFilt.mesh.CombineMeshes(combine);
    }

    public void MergeMeshes(GameObject Root)
    {
        MeshFilter[] meshFilters = Root.transform.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        Material mat = null;
        while (i < meshFilters.Length)
        {
            if (i == 0)
            {
                mat = meshFilters[0].gameObject.GetComponent<MeshRenderer>().sharedMaterial;
            }

            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = Matrix4x4.identity;
            //Destroy(meshFilters[i].gameObject);
            Destroy(meshFilters[i]);
            Destroy(meshFilters[i].GetComponent<MeshRenderer>());
            i++;
        }
        GameObject tNewObj = new GameObject("CountryMesh");
        tNewObj.transform.SetParent(Root.transform, false); 
        MeshFilter RootMeshFilt = tNewObj.AddComponent<MeshFilter>();
        MeshRenderer RootMeshRend = tNewObj.AddComponent<MeshRenderer>();

        if (mat != null)
        {
            RootMeshRend.sharedMaterial = mat;
        }
        else
        {
            Debug.LogError("You tried to optimize a layer without consistent materials.");
        }

        RootMeshFilt.mesh = new Mesh();
        RootMeshFilt.mesh.CombineMeshes(combine);
        tNewObj.SetActive(true);
    }

    public void ConvertMeshToLines(GameObject Root)
    {
        GameObject tLineParent = new GameObject("CountryLines");
	    tLineParent.transform.SetParent(Root.transform, false);
        var tRenderers = Root.GetComponentsInChildren<MeshFilter>();

		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.black, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
		);
		
		foreach (MeshFilter MR in tRenderers)
        {
            // Get triangles and vertices from mesh
            Mesh tMesh = MR.mesh;
            int[] triangles = tMesh.triangles;
            Vector3[] vertices = tMesh.vertices;

            // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
            Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int vert1 = triangles[i + e];
                    int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                    string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                    if (edges.ContainsKey(edge))
                    {
                        edges.Remove(edge);
                    }
                    else
                    {
                        edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                    }
                }
            }

            // Create edge lookup Dictionary
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> edge in edges.Values)
            {
                if (lookup.ContainsKey(edge.Key) == false)
                {
                    lookup.Add(edge.Key, edge.Value);
                }
            }

            // Create line prefab
            LineRenderer linePrefab = new GameObject().AddComponent<LineRenderer>();
			linePrefab.colorGradient = gradient;
			linePrefab.transform.name = "Line";
            linePrefab.positionCount = 0;
            linePrefab.sharedMaterial = mBorderMat;
            linePrefab.transform.SetParent(tLineParent.transform, false);
            linePrefab.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linePrefab.receiveShadows = false;
            float lineWidthScale = (mLineSizeMultiplier * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
            linePrefab.widthMultiplier = lineWidthScale;
			linePrefab.numCapVertices = 1;
			linePrefab.numCornerVertices = 1;

            // Create first line
            //LineRenderer line = Instantiate(linePrefab.gameObject).GetComponent<LineRenderer>();
            mLineRenderers.Add(linePrefab);

            // This vector3 gets added to each line position, so it sits in front of the mesh
            // Change the -0.1f to a positive number and it will sit behind the mesh
            //TODO make this not hardcoded
            Vector3 basePosition = linePrefab.transform.position - new Vector3(0.0f, 0.0f, 0.1f);

            // Loop through edge vertices in order
            int startVert = 0;
            int nextVert = startVert;
            int highestVert = startVert;
            int vertexCount = 0;
            linePrefab.positionCount = vertices.Length;
            while (true)
			{
                // Add to line
                vertexCount++;
                linePrefab.SetPosition(vertexCount - 1, vertices[nextVert] + basePosition);

                // Get next vertex
                nextVert = lookup[nextVert];

                // Store highest vertex (to know what shape to move to next)
                if (nextVert > highestVert)
                {
                    highestVert = nextVert;
                }

                // Shape complete
                if (nextVert == startVert)
                {
                    // Finish this shape's line
                    linePrefab.positionCount = vertexCount + 1;
                    linePrefab.SetPosition(vertexCount, vertices[nextVert] + basePosition);

                    // No more verts
                    break;
                }
            }
        }
    }

    //public void ConvertSpritesToLines(GameObject Root)
    //{
    //    GameObject tNewObj = new GameObject("LineContainer");
    //    tNewObj.transform.parent = Root.transform;
    //    
    //    SpriteRenderer[] SpriteList = Root.GetComponentsInChildren<SpriteRenderer>();
    //    mLineRenderer = tNewObj.AddComponent<LineRenderer>();
    //    mLineRenderer.material = mBorderMat;
    //    mLineRenderer.SetWidth(1.0f, 1.0f);
    //    mLineRenderer.SetVertexCount(SpriteList.Length);
    //
    //    Vector3[] tVList = new Vector3[SpriteList.Length];
    //    int i = 0;
    //    foreach (SpriteRenderer SR in SpriteList)
    //    {
    //        tVList[i] = SR.transform.position;
    //        i++;
    //    }
    //
    //    mLineRenderer.SetPositions(tVList);
    //}

    public void MergeSprites(GameObject Root, float aSpriteSize)
    {
        SpriteRenderer[] SpriteList = Root.GetComponentsInChildren<SpriteRenderer>();
        CombineInstance[] combine = new CombineInstance[SpriteList.Length];
		for (int i = 0; i < SpriteList.Length; ++i)
        {
            //Create a new mesh from the sprite
            Mesh tMesh = new Mesh();
            Vector3[] tVerticeList = new Vector3[4]
            {
                new Vector3(-0.5f, 0.5f, 0) * aSpriteSize,
                new Vector3(0.5f, 0.5f, 0)  * aSpriteSize,
                new Vector3(0.5f, -0.5f, 0)   * aSpriteSize,
                new Vector3(-0.5f, -0.5f, 0)  * aSpriteSize
            };
            tMesh.vertices = tVerticeList;
            Vector2[] tUVList = new Vector2[4]
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f),
            };
            tMesh.uv = tUVList;
            
            int[] tTrianglesList = new int[SpriteList[i].sprite.triangles.Length];
            for (int t = 0; t < SpriteList[i].sprite.triangles.Length; t++)
            {
                tTrianglesList[t] = (int)SpriteList[i].sprite.triangles[t];
            }
            tMesh.triangles = tTrianglesList;
            
            combine[i].mesh = tMesh;
            combine[i].transform = SpriteList[i].transform.localToWorldMatrix;
        }

        GameObject tNewObj = new GameObject();
        tNewObj.transform.parent = Root.transform;
        MeshFilter RootMeshFilt = tNewObj.AddComponent<MeshFilter>();
        MeshRenderer RootMeshRend = tNewObj.AddComponent<MeshRenderer>();

        if (mBorderMat != null)
        {
            RootMeshRend.sharedMaterial = mBorderMat;
        }
        else
        {
            Debug.LogError("You tried to optimize a layer without consistent materials.");
        }
        Mesh tFinalMesh = new Mesh();
        tFinalMesh.CombineMeshes(combine);
        RootMeshFilt.mesh = tFinalMesh;
        tNewObj.SetActive(true);
    }
}
