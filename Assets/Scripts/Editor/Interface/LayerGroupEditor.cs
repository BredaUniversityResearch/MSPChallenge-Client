using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(LayerCategoryGroup))]
public class LayerGroupEditor : Editor {

    public string title = "New Title";
    public bool visibility = true;

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        LayerCategoryGroup _target = (LayerCategoryGroup)target;

        GUILayout.Label("Editor", EditorStyles.boldLabel);

        title = EditorGUILayout.TextField("Layer Group Title", title);
        visibility = EditorGUILayout.Toggle("Layer Group Visibility", visibility);

        if (GUILayout.Button("Create Button"))
        {
            _target.CreateLayerButton("New Subcategory");
        }

        if (GUILayout.Button("Destroy Layer Group"))
        {
            _target.Destroy();
        }

        if (GUI.changed)
        {
            _target.SetTitle(title);
            _target.Hide(visibility);
        }
    }
}