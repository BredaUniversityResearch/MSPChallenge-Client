using UnityEngine;
using UnityEditor;

[CustomEditor(typeof (GenericLayer))]
public class GenericLayerEditor : Editor {

    public string title = "New Layer";
    public bool visibility = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //GenericLayer _target = (GenericLayer)target;

        //GUILayout.Label("Editor", EditorStyles.boldLabel);

        //title = EditorGUILayout.TextField("Layer Title", title);
        //visibility = EditorGUILayout.Toggle("Show Edit Button", visibility);

        //if (GUILayout.Button("Create Key"))
        //{
        //    _target.CreateMapKey();
        //}

        //if (GUILayout.Button("Destroy Layer"))
        //{
        //    _target.Destroy();
        //}

        //if (GUI.changed)
        //{
        //    _target.SetTitle(title);
        //    _target.ShowEditButton(visibility);
        //}
    }
}
