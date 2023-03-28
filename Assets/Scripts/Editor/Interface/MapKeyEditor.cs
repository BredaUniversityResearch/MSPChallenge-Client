using MSP2050.Scripts;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ActiveLayerEntityType))]
public class MapKeyEditor : Editor
{

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		//MapKey _target = (MapKey)target;
	}
}