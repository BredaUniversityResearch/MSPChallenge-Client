using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CaptureEntry
{
    public string Name;
    public GameObject CapturePrefab;
}

//This class creates scenes from prefabs and links them to the given RenderTexture (If given key was already given it returns the corresponding rendertexture)
public class SceneCaptureManager : MonoBehaviour
{
    public static SceneCaptureManager instance;

    //Dictionary of scenes that contains a camera
    private Dictionary<string, GameObject> mRenderScenes = new Dictionary<string, GameObject>();

    //Array of renderprocesses which are instantiated and streaming to a 
    private Dictionary<string, KeyValuePair<GameObject, RenderTexture>> mRenderProcesses = new Dictionary<string, KeyValuePair<GameObject, RenderTexture>>();

    //Makes sure that you can have multiple usages for rendertextures
    private Dictionary<string, int> mRenderProcessCounts = new Dictionary<string, int>();

    [Header("Names must match any property window name.")]

    //List for in the inspector used to populate the RenderScenes
    [SerializeField]
    private List<CaptureEntry> m_CaptureEntries = null;

    private float mRotationOffset = 0.0f;

    private void Awake()
    {
        instance = this;
        //Copy the scenes from the list to the dictionary
        for(int i = 0; i < m_CaptureEntries.Count; i++)
        {
            mRenderScenes.Add(m_CaptureEntries[i].Name, m_CaptureEntries[i].CapturePrefab);
        }
    }

    //Creates a render texture in mRenderTextures mapped to a string
    public bool OpenSceneRenderer(string aEntryName, ref RenderTexture aRenderTex)
    {
        if(!mRenderScenes.ContainsKey(aEntryName))
        {
            return false; //Scene does not exist
        }
        else
        {
            //Attempt to create a new renderprocess
            return OpenRenderProcess(aEntryName, ref aRenderTex);
        }
    }

    //Deletes render texture from mRenderTextures
    public bool CloseSceneRenderer(string aEntryName)
    {
        if(!mRenderScenes.ContainsKey(aEntryName))
        {
            return false; // Couldn't find asset to close
        }
        else
        {
            //Attempt to close the given renderprocess
            return CloseRenderProcess(aEntryName);
        }
    }

    private bool OpenRenderProcess(string aEntryName, ref RenderTexture aRenderTex)
    {
        //If requested render process already exists return it, and increment the process counter
        if(mRenderProcessCounts.ContainsKey(aEntryName))
        {
            if(mRenderProcessCounts[aEntryName] > 0)
            {
                aRenderTex = mRenderProcesses[aEntryName].Value;
                IncrementRenderProcess(aEntryName, true);
                return true;
            }
        }

        //Create RenderInstance
        GameObject tRenderInstance = Instantiate(mRenderScenes[aEntryName]);
        Transform tRenderTransform = tRenderInstance.transform;
        tRenderTransform.parent = this.transform;

        //Offsetting every instance and making them point away from the start, making sure that they don't see eachother
        tRenderTransform.position = transform.position + Quaternion.AngleAxis(mRotationOffset += 15.0f, tRenderTransform.up) * tRenderTransform.forward * 100000.0f;
        tRenderTransform.forward = (tRenderTransform.position - transform.position).normalized;

        Camera tCamRef = tRenderInstance.GetComponentInChildren<Camera>();
        if(tCamRef == null)
        {
            Destroy(tRenderInstance); // Clean up the stuff you created
            return false; //The camera in the prefab could not be found
        }
        aRenderTex.name = aEntryName + " RT";
        if(!aRenderTex.IsCreated())
        {
            aRenderTex.Create(); // if the rendertexture is not create yet create it.
        }
        tCamRef.targetTexture = aRenderTex;

        mRenderProcesses.Add(aEntryName, new KeyValuePair<GameObject, RenderTexture>(tRenderInstance, aRenderTex));
        
        IncrementRenderProcess(aEntryName, true);

        return true; // Succesfully created Process
    }

    private bool CloseRenderProcess(string aEntryName)
    {
        if(!mRenderProcesses.ContainsKey(aEntryName))
        {
            return false; // Can't find the Process to close
        }
        //Close the render process but only if theres no other instances using it
        if (mRenderProcessCounts.ContainsKey(aEntryName))
        {
            if (mRenderProcessCounts[aEntryName] > 1)
            {
                IncrementRenderProcess(aEntryName, false);
                return true;
            }
        }

        KeyValuePair<GameObject, RenderTexture> tKVP = mRenderProcesses[aEntryName];
        Destroy(tKVP.Key); // Destroy the gameobject(scene)
        if(tKVP.Value.IsCreated())
        {
            tKVP.Value.Release(); // Release the rendertexture if it exists
        }

        mRenderProcesses.Remove(aEntryName);

        IncrementRenderProcess(aEntryName, false);
        return true; // Succesfull close
    }

    //Initializes, increments and decrements render processes in the list depening on aIncremental
    private void IncrementRenderProcess(string aEntryName, bool aIncremental)
    {
        if(mRenderProcessCounts.ContainsKey(aEntryName))
        {
            mRenderProcessCounts[aEntryName] += aIncremental ? 1 : -1;
        }
        else
        {
            mRenderProcessCounts[aEntryName] = 1;
        }
    }
}
