using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InterfaceCanvas))]
public class InterfaceCanvasEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        InterfaceCanvas _target = (InterfaceCanvas)target;

        if (GUILayout.Button("Create Generic Window")) {
            _target.CreateGenericWindow(true);
        }
    }
}