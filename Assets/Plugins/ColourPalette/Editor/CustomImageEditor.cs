#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ColourPalette
{
    [CustomEditor(typeof(CustomImage))]
    public class CustomImageEditor : UnityEditor.UI.ImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();//Draw inspector UI of ImageEditor

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(serializedObject.FindProperty("colourAsset"), typeof(ColourAsset));
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                ((CustomImage) serializedObject.targetObject).ColourPaletteChanged();
            }
        }
    }
}
#endif