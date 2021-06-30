#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ColourPalette
{
    [CustomEditor(typeof(CustomText))]
    public class CustomTextEditor : UnityEditor.UI.TextEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();//Draw inspector UI of ImageEditor

            serializedObject.Update();
            EditorGUILayout.ObjectField(serializedObject.FindProperty("colourAsset"), typeof(ColourAsset));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
