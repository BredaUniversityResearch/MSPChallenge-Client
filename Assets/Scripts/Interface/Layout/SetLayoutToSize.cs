using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SetLayoutToSize : MonoBehaviour
{
    [SerializeField]
    private LayoutElement layout = null;

    private RectTransform rect;
    private float oldHeight;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        oldHeight = rect.sizeDelta.y;
    }
    
	void Update ()
    {
	    float newHeight = rect.sizeDelta.y;
        if (!Mathf.Approximately(newHeight, oldHeight))
        {
            layout.preferredHeight = newHeight;
        }
        oldHeight = newHeight;
    }
}
