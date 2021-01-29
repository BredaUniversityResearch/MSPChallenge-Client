using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LayerButton))]
public class LayerButtonEditor : Editor
{

	public bool visibility = true;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		LayerButton _target = (LayerButton)target;

		GUILayout.Label("Editor", EditorStyles.boldLabel);

		visibility = EditorGUILayout.Toggle("Button Visibility", visibility);

		if (GUI.changed)
		{
			_target.SetVisibility(visibility);
		}
	}
}