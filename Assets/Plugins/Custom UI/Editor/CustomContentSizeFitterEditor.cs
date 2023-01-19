#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.UI;

[CustomEditor(typeof(CustomContentSizeFitter), true)]
[CanEditMultipleObjects]
public class CustomContentSizeFitterEditor : SelfControllerEditor
{
	SerializedProperty m_HorizontalFit;
	SerializedProperty m_VerticalFit;
	SerializedProperty m_maxSize;

	protected virtual void OnEnable()
	{
		m_HorizontalFit = serializedObject.FindProperty("m_HorizontalFit");
		m_VerticalFit = serializedObject.FindProperty("m_VerticalFit");
		m_maxSize = serializedObject.FindProperty("m_maxSize");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_HorizontalFit, true);
		EditorGUILayout.PropertyField(m_VerticalFit, true);
		EditorGUILayout.PropertyField(m_maxSize, true);
		serializedObject.ApplyModifiedProperties();

		base.OnInspectorGUI();
	}
}
#endif
