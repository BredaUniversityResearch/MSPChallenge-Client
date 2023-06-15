#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Linq;
using System.Text;
using MSP2050.Scripts;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class OutlinerGenerator : OdinEditorWindow
{
    [MenuItem("Tools/Outliner Generator")]
    private static void OpenWindow()
    {

        GameObject[] objs = Selection.gameObjects;
        int childIndex = objs[objs.Length-1].transform.GetSiblingIndex();
        foreach(GameObject obj in objs)
        {
            GameObject newObj = GameObject.Instantiate(obj, obj.transform.parent);
            newObj.transform.SetSiblingIndex(childIndex);
            RectTransform rect = newObj.GetComponent<RectTransform>();
            if(Mathf.Abs(rect.anchorMin.x - rect.anchorMax.x) < 0.01f)
            {
                //vert
                rect.sizeDelta = new Vector2(8f, 4f);
                //rect.offsetMax = new Vector2(0f, 2f);
                //rect.offsetMin = new Vector2(0f, -2f);
            }
            else
            {
                //hor
                rect.sizeDelta = new Vector2(4f, 8f);
                //rect.offsetMax = new Vector2(2f, 0f);
                //rect.offsetMin = new Vector2(-2f, 0f);
            }

            //rect.offsetMax = new Vector2(rect.offsetMax.x-2f, rect.offsetMax.y - 2f);
            //rect.offsetMin = new Vector2(rect.offsetMin.x - 2f, rect.offsetMin.y - 2f);  
            newObj.GetComponent<Image>().color = Color.black;
        }
        
    }
}
#endif