using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LayerPanel))]
public class LayerPanelEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        LayerPanel _target = (LayerPanel)target;

        if (GUILayout.Button("Create Layer Group")) {
            _target.CreateLayerGroup();
        }
    }
}