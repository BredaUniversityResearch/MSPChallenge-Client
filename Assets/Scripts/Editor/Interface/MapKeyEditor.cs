using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapKey))]
public class MapKeyEditor : Editor
{

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		//MapKey _target = (MapKey)target;
	}
}