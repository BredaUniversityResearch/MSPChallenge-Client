using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
public class NormalizeHudElement : MonoBehaviour
{
    [Header("NormalizeButton (Executed at editTime)")]
    public bool normalizeObject = false;
    [Header("Tweakvariables")]
    public bool recursivelyForChildren = true;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (normalizeObject)
        {
            normalizeObject = false;
            NormalizeObject(this.transform, Vector2.one);
        }
    }



    public void NormalizeObject(Transform aObj, Vector2 aParentScale)
    {
        var tRectTrans = aObj.GetComponent<RectTransform>();
            
        if (tRectTrans != null)
        {
            Vector2 tScale = new Vector2(tRectTrans.localScale.x, tRectTrans.localScale.y);
            Vector2 tSize = tRectTrans.sizeDelta;
            tRectTrans.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            tRectTrans.sizeDelta = new Vector2(tScale.x * tSize.x * aParentScale.x, tScale.y * tSize.y * aParentScale.y);

            Text tText = aObj.GetComponent<Text>();
            if(tText)
            {
                tText.fontSize = (int)(tText.fontSize * tScale.x * aParentScale.x);
            }

            if (recursivelyForChildren)
            {
                foreach (Transform t in aObj)
                {
                    NormalizeObject(t, new Vector2(tScale.x * aParentScale.x, tScale.y * aParentScale.y));
                }
            }
        }
    }
}
