using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ValueConversionUnit))]
class ValueConversionUnitEditor: Editor
{
	public override void OnInspectorGUI()
	{
		SerializedProperty baseUnitProperty = serializedObject.FindProperty("baseUnit");
		SerializedProperty decimalPlacesProperty = serializedObject.FindProperty("decimalPlaces");
		SerializedProperty unitsProperty = serializedObject.FindProperty("conversionUnits");

		EditorGUI.BeginChangeCheck();

		baseUnitProperty.stringValue = EditorGUILayout.TextField("Base Unit", baseUnitProperty.stringValue);
		decimalPlacesProperty.intValue = EditorGUILayout.IntField("Format Decimal Places", decimalPlacesProperty.intValue);

		if (unitsProperty != null)
		{
			bool requiresSort = false;
			int removeEntry = -1;
			for (int i = 0; i < unitsProperty.arraySize; ++i)
			{
				SerializedProperty unit = unitsProperty.GetArrayElementAtIndex(i);
				SerializedProperty postFix = unit.FindPropertyRelative("unitPostfix");
				SerializedProperty size = unit.FindPropertyRelative("unitSize");

				EditorGUILayout.BeginHorizontal();
				float newUnitSize = EditorGUILayout.DelayedFloatField(size.floatValue);
				if (size.floatValue != newUnitSize)
				{
					requiresSort = true;
					size.floatValue = newUnitSize;
				}

				postFix.stringValue = EditorGUILayout.TextField(postFix.stringValue);

				if (GUILayout.Button("Remove"))
				{
					removeEntry = i;
				}

				EditorGUILayout.EndHorizontal();
			}

			if (requiresSort)
			{
				SortArray(unitsProperty);
			}

			if (GUILayout.Button("Add Entry"))
			{
				unitsProperty.arraySize = unitsProperty.arraySize + 1;
				serializedObject.ApplyModifiedProperties();
			}

			if (removeEntry != -1)
			{
				unitsProperty.DeleteArrayElementAtIndex(removeEntry);
			}
		}

		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
	}

	private void SortArray(SerializedProperty unitsProperty)
	{
		//THE most efficient sort in the world™
		for (int i = 0; i < unitsProperty.arraySize; ++i)
		{
			SerializedProperty lhs = unitsProperty.GetArrayElementAtIndex(i);
			SerializedProperty lhsSize = lhs.FindPropertyRelative("unitSize");

			float lowestValue = lhsSize.floatValue;
			int lowestIndex = i;

			for (int j = i + 1; j < unitsProperty.arraySize; ++j)
			{
				SerializedProperty rhs = unitsProperty.GetArrayElementAtIndex(j);
				SerializedProperty rhsSize = rhs.FindPropertyRelative("unitSize");

				if (lowestValue > rhsSize.floatValue)
				{
					lowestValue = rhsSize.floatValue;
					lowestIndex = j;
				}
			}

			if (lowestIndex != i)
			{
				unitsProperty.MoveArrayElement(lowestIndex, i);
			}
		}
	}
}
