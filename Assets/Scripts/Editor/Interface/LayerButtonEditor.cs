using MSP2050.Scripts;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LayerSubCategory))]
public class LayerButtonEditor : Editor
{

	public bool visibility = true;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		LayerSubCategory _target = (LayerSubCategory)target;

		GUILayout.Label("Editor", EditorStyles.boldLabel);

		visibility = EditorGUILayout.Toggle("Button Visibility", visibility);

		if (GUI.changed)
		{
			_target.SetVisibility(visibility);
		}
	}
}