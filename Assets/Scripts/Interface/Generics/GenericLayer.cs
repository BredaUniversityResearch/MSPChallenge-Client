using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GenericLayer : MonoBehaviour {
    
    public TextMeshProUGUI title;
    public CustomToggle toggle;
    //public CustomButton barButton;
    public Image icon;
    public Transform contentLocation;

	[HideInInspector]
	public string SubCategory;
	[HideInInspector]
	public int layerID;

    [Header("Prefabs")]
	public GameObject mapKeyPrefab;

    [Header("Content")]
    public List<MapKey> mapKeyContent = new List<MapKey>();

    //void Start()
    //{
    //    if (toggle)
    //    {
    //        barButton.interactable = true;
    //    }
    //}

    public void SetTitle(string text)
    {
        title.text = text;
    }

	/// <summary>
	/// Destroy this
	/// </summary>
	//public void Destroy()
	//{
	//	for (int i = 0; i < InterfaceCanvas.instance.genericWindow.Count; i++)
	//	{
	//		for (int j = 0; j < InterfaceCanvas.instance.genericWindow[i].genericContent.Count; j++)
	//		{
	//			for (int k = 0; k < InterfaceCanvas.instance.genericWindow[i].genericContent[j].genericLayer.Count; k++)
	//			{
	//				if (InterfaceCanvas.instance.genericWindow[i].genericContent[j].genericLayer[k] == this)
	//				{
	//					InterfaceCanvas.instance.genericWindow[i].genericContent[j].DestroyGenericLayer(this);
	//				}
	//			}
	//		}
	//	}
	//}

    public void CreateMapKey()
    {
        CreateKey();
    }

    public MapKey CreateMapKey(Color col, string label)
    {
        MapKey key = CreateKey();

        key.areaKey.color = col;
        key.lineKey.color = col;
        key.pointKey.color = col;
        key.label.text = label;

        return key;
    }

    public MapKey CreateMapKey(Color col, string label, Texture2D pattern)
    {
        MapKey key = CreateKey();

        key.areaKey.texture = pattern;
        key.areaKey.color = col;
        key.lineKey.color = col;
        key.pointKey.color = col;
        key.label.text = label;

        return key;
    }

    public void ClearMapKeys()
    {
        for (int i = 0; i < mapKeyContent.Count; i++)
        {
            if(mapKeyContent[i].gameObject != null)
            {
                GameObject.Destroy(mapKeyContent[i].gameObject);
            }
        }
        
        mapKeyContent.Clear();
    }


    private MapKey CreateKey()
    {
        // Instantiate prefab
        GameObject go = Instantiate(mapKeyPrefab);

        // Store component
        MapKey content = go.GetComponent<MapKey>();

        // Add to list
        mapKeyContent.Add(go.GetComponent<MapKey>());

        // Assign parent
        go.transform.SetParent(contentLocation, false);

        // Color demo
        content.areaKey.color = new Color(Random.Range(.5f, 1f), Random.Range(.5f, 1f), Random.Range(.5f, 1f));

        return content;
    }
}