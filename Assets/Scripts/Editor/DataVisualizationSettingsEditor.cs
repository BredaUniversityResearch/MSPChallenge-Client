using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(DataVisualizationSettings))]
public class DataVisualizationSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DataVisualizationSettings settings = (DataVisualizationSettings)target;

		SerializedProperty valueConversionsField = serializedObject.FindProperty("valueConversions");

		EditorGUI.BeginChangeCheck();

		EditorGUILayout.LabelField("Value conversion collection", EditorStyles.boldLabel);
		valueConversionsField.objectReferenceValue = EditorGUILayout.ObjectField(valueConversionsField.objectReferenceValue, typeof(ValueConversionCollection), false);

		List<string> names = new List<string>(System.Enum.GetNames(typeof(SubEntityDrawMode)));
        if (settings.DrawModeSettings == null || settings.DrawModeSettings.Count == 0)
        {
            settings.DrawModeSettings = new List<DrawModeSettings>();

            foreach (string name in names)
            {
                settings.DrawModeSettings.Add(new DrawModeSettings(name));
            }
        }
        else
        {
            for (int i = 0; i < names.Count; ++i)
            {
                if (!settings.ContainsDrawMode(names[i]))
                {
                    settings.DrawModeSettings.Insert(i, new DrawModeSettings(names[i]));
                }
            }

            for (int i = settings.DrawModeSettings.Count - 1; i >= 0; --i)
            {
                DrawModeSettings settingsEntry = settings.DrawModeSettings[i];
                if (!names.Contains(settingsEntry.DrawModeName))
                {
                    settings.DrawModeSettings.Remove(settingsEntry);
                }
            }
        }

        EditorGUILayout.LabelField("Inner glow pixel size in world units", EditorStyles.boldLabel);
        settings.InnerGlowPixelSize = EditorGUILayout.FloatField("Inner glow pixel size", settings.InnerGlowPixelSize);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        int removeLOD = -1;
        for (int i = 0; i < settings.LODs.Count; ++i)
        {
            EditorGUILayout.LabelField("LOD " + i, EditorStyles.boldLabel);
            settings.LODs[i].MinScale = EditorGUILayout.FloatField("Minimum scale:", settings.LODs[i].MinScale);
            settings.LODs[i].SimplificationTolerance = EditorGUILayout.FloatField("Simplification tolerance:", settings.LODs[i].SimplificationTolerance);
            settings.LODs[i].MinPolygonArea = EditorGUILayout.FloatField("Minimum polygon area:", settings.LODs[i].MinPolygonArea);
            if (GUILayout.Button("Remove this LOD")) { removeLOD = i; }
            EditorGUILayout.Space();
        }
        if (removeLOD != -1) { settings.LODs.RemoveAt(removeLOD); }
        if (GUILayout.Button("Create new LOD"))
        {
            if (settings.LODs.Count == 0)
            {
                settings.LODs.Add(new LODSettings(20f, 0.5f, 10f));
            }
            else
            {
                LODSettings last = settings.LODs[settings.LODs.Count - 1];
                settings.LODs.Add(new LODSettings(last.MinScale, last.SimplificationTolerance, last.MinPolygonArea));
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        foreach (DrawModeSettings settingsEntry in settings.DrawModeSettings)
        {
            // skip default draw mode
            if (settingsEntry.DrawModeName == SubEntityDrawMode.Default.ToString()) { continue; }

            EditorGUILayout.LabelField(settingsEntry.DrawModeName, EditorStyles.boldLabel);
            settingsEntry.Color = EditorGUILayout.ColorField("Color", settingsEntry.Color);
            settingsEntry.DrawPolygon = EditorGUILayout.Toggle("Draw Polygon", settingsEntry.DrawPolygon);
            settingsEntry.DrawLines = EditorGUILayout.Toggle("Draw Lines", settingsEntry.DrawLines);
            settingsEntry.DrawPoints = EditorGUILayout.Toggle("Draw Points", settingsEntry.DrawPoints);
            EditorGUILayout.Separator();
        }

		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(target);
		}
	}
}
