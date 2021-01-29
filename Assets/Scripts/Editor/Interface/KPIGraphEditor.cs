using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KPIGraph))]
public class KPIGraphEditor : Editor
{
    //public override void OnInspectorGUI()
    //{
    //    DrawDefaultInspector();

    //    KPIGraph _target = (KPIGraph)target;

    //    EditorGUILayout.Space();

    //    EditorGUILayout.LabelField("Names & Colors", EditorStyles.boldLabel);

    //    GUILayout.BeginVertical();

    //    for (int i = 0; i < _target.names.Count; i++) {
    //        EditorGUILayout.BeginHorizontal();
    //        EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(50));
    //        _target.names[i] = EditorGUILayout.TextField(_target.names[i]);
    //        _target.colors[i] = EditorGUILayout.ColorField(_target.colors[i]);
    //        EditorGUILayout.EndHorizontal();
    //    }

    //    GUILayout.EndVertical();
    //}
}