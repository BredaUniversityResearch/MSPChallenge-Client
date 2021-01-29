using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LayerProbeWindow : MonoBehaviour
{
	[SerializeField]
	private GenericWindow window = null;
	[SerializeField]
	private Transform contentLocation = null;
	[SerializeField]
	private GameObject layerProbeEntryPrefab = null;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowLayerProbeWindow(List<SubEntity> subentities, Vector3 worldSamplePosition, Vector3 windowPosition)
    {
		gameObject.SetActive(true);

		//Remove old entries
		foreach (Transform child in contentLocation)		
			GameObject.Destroy(child.gameObject);		

		//Add new entries
		for (int i = 0; i < subentities.Count; i++)
		{
			SubEntity tmpSubentity = subentities[i]; 
			LayerProbeEntry entry = GameObject.Instantiate(layerProbeEntryPrefab).GetComponent<LayerProbeEntry>();
			entry.SetToSubEntity(tmpSubentity);
			entry.transform.SetParent(contentLocation, false);
			entry.barButton.onClick.AddListener(() =>
			{
				UIManager.CreatePropertiesWindow(tmpSubentity, worldSamplePosition, windowPosition);
			});
		}

		//Clamp and position window
		//Vector3 clampedPosition = new Vector3(
		//	Mathf.Clamp(windowPosition.x, 0, Screen.width - window.GetSize().x),
		//	Mathf.Clamp(windowPosition.y, -Screen.height + window.GetSize().y, 0),
		//	windowPosition.z);
  //      window.SetPosition(clampedPosition);
        StartCoroutine(RepositionOnFrameEnd(windowPosition));
    }

    IEnumerator RepositionOnFrameEnd(Vector3 position)
    {
        yield return new WaitForEndOfFrame();

        Rect rect = window.windowTransform.rect;
        float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
        window.SetPosition(new Vector3(
            Mathf.Clamp(position.x / scale, 0f, (Screen.width - (rect.width * scale)) / scale),
            Mathf.Clamp(position.y / scale, (-Screen.height + (rect.height * scale)) / scale, 0f),
            position.z));
    }
}
