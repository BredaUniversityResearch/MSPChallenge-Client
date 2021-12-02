using UnityEngine;
using UnityEngine.UI;

public class PlanLayerFoldable : PlanLayerBase
{
    public Button foldButton;
    public RectTransform foldButtonRect;

    void Awake()
    {
		foldButton.onClick.AddListener(Fold);
    }

    public void Fold()
    {
        Vector3 rot = foldButtonRect.eulerAngles;
        bool tOpening = (rot.z == 0);

        foldButtonRect.eulerAngles = tOpening ? new Vector3(rot.x, rot.y, 90f) : new Vector3(rot.x, rot.y, 0f);
        int childIndex = transform.GetSiblingIndex();
        for (int i = childIndex + 1; i < transform.parent.childCount; i++)
        {
            PlanLayer_Layer tPlanLayer = transform.parent.GetChild(i).GetComponent<PlanLayer_Layer>();
            if (tPlanLayer)
            {
                tPlanLayer.gameObject.SetActive(!tOpening);
            }
            else
            {
                //Leave the for loop because you hit an object that is not within your layer anymore
                break;
            }
        }
    }
}