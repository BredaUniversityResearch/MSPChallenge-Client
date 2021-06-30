using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LayerCategoryGroup : MonoBehaviour
{

	public LayerPanel layerPanel;
	public Transform buttonLocation;
	public TextMeshProUGUI title;

	[Header("Prefabs")]
	public GameObject buttonPrefab;

	[Header("Content")]
	public Dictionary<string, LayerButton> layerButtons = new Dictionary<string, LayerButton>();

	/// <summary>
	/// Set window title
	/// </summary>
	public void SetTitle(string text)
	{
		title.text = text;
	}

	public LayerButton CreateLayerButton(string subCategory)
	{
		// Instantiate prefab
		GameObject go = Instantiate(buttonPrefab, buttonLocation, false);

		// Store component
		LayerButton button = go.GetComponent<LayerButton>();

		// Add to list
		layerButtons.Add(subCategory, button);
		
		return button;
	}

	/// <summary>
	/// Destroy this
	/// </summary>
	public void Destroy()
	{
		for (int i = 0; i < layerPanel.layerGroup.Count; i++)
		{
			if (layerPanel.layerGroup[i] == this)
			{
				layerPanel.DestroyLayerGroup(this);
			}
		}
	}

	/// <summary>
	/// Properly destroys a layer button
	/// </summary>
	public void DestroyLayerButton(string subCategory, LayerButton button)
	{
		layerButtons.Remove(subCategory);
		Destroy(button.gameObject);
	}

	/// <summary>
	/// Hide the button
	/// </summary>
	public void Hide(bool toggle)
	{
		gameObject.SetActive(toggle);
	}
}
