//using UnityEngine;
//using System.Collections;

//public class MELLayerManager : MonoBehaviour {
    
//	// Use this for initialization
//	void Start ()
//    {

//    }
	
//	// Update is called once per frame
//	void FixedUpdate () {
//        FixLayers();
//    }

//    public void FixLayers()
//    {
//        int presetNum = 0;
//        for (int i = 0; i < transform.childCount; i++)
//        {
//            if (transform.GetChild(i).gameObject.activeSelf && transform.GetChild(i).name.Substring(0, 4) == "mel_")
//            {
//                transform.GetChild(i).GetChild(0).GetComponent<Renderer>().material.SetFloat("_OffsetX", (float)(0.125 * ((Mathf.Floor(presetNum / 2) != presetNum / 2.0) ? 1.0 : 0.0)));
//                //transform.GetChild(i).GetChild(0).GetComponent<Renderer>().material.SetFloat("_Rotate", (float)(0.5 * ((presetNum % 4 > 1) ? 1 : 0)));

//                presetNum++;
//            }
//        }
//    }
//}