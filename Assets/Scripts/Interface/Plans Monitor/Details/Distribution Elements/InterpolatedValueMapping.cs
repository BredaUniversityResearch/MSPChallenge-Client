using System;
using System.Collections.Generic;

/*
* Maps an input value to an output value using a sparsely populated mapping table.
* In other words: Takes inputs (0 -> 0, 1 -> 10, 2 -> 20) and maps any input value to an interpolated output value (0 -> 0, 1 -> 10, 1.5 -> 15)
*/
public class InterpolatedValueMapping
{
	private enum EMappingMode
	{
		InputToOutput,	//Default forward mapping
		OutputToInput	//Reverse mapping 
	}

	private List<KeyValuePair<float, float>> m_values = new List<KeyValuePair<float, float>>();
	private bool m_needsSorting = false;

	public void Add(float input, float output)
	{
		m_values.Add(new KeyValuePair<float, float>(input, output));
		m_needsSorting = true;
	}

	public void Clear()
	{
		m_values.Clear();
	}

	public float Map(float input)
	{
		return MapInternal(input, EMappingMode.InputToOutput);
	}

	public float InverseMap(float output, bool centerIfRangeEqual = false)
	{
		return MapInternal(output, EMappingMode.OutputToInput, centerIfRangeEqual);
	}

	private float MapInternal(float valueToMap, EMappingMode mappingMode, bool centerIfRangeEqual = false)
	{
		if (m_values.Count == 0)
		{
			throw new Exception("Tried mapping a value when no mapping values have been setup.");
		}

		SortValues();

		bool mappedValue = false;
		float result = default(float);
		for (int i = 0; i < m_values.Count; ++i)
		{
			if (!IsLess(GetInputValue(m_values[i], mappingMode), valueToMap))
			{
				if (i == 0)
				{
                    //First entry in our mapping table. Return the associated output.
                    //If first to entries equal, get average
                    if (centerIfRangeEqual && m_values.Count > 1 &&
                        GetInputValue(m_values[0], mappingMode) == GetInputValue(m_values[1], mappingMode))
                        result = (GetOutputValue(m_values[0], mappingMode) + GetOutputValue(m_values[1], mappingMode)) / 2f;
                    else
                        result = GetOutputValue(m_values[0], mappingMode);
					mappedValue = true;
					break;
				}
				else
				{
					KeyValuePair<float, float> from = m_values[i - 1];
					KeyValuePair<float, float> to = m_values[i];
					result = Interpolate(from, to, valueToMap, mappingMode);
					mappedValue = true;
					break;
				}
			}
		}

		if (!mappedValue)
		{
			result = m_values[m_values.Count - 1].Value;
		}

		return result;
	}

	private void SortValues()
	{
		if (m_needsSorting)
		{
			m_values.Sort((lhs, rhs) => lhs.Key.CompareTo(rhs.Key));
			m_needsSorting = false;
		}
	}

	private float GetInputValue(KeyValuePair<float, float> keyValuePair, EMappingMode mappingMode)
	{
		return (mappingMode == EMappingMode.InputToOutput) ? keyValuePair.Key : keyValuePair.Value;
	}

	private float GetOutputValue(KeyValuePair<float, float> keyValuePair, EMappingMode mappingMode)
	{
		return (mappingMode == EMappingMode.InputToOutput) ? keyValuePair.Value : keyValuePair.Key;
	}

	private bool IsLess(float lhs, float rhs)
	{
		return lhs < rhs;
	}

	private float Interpolate(KeyValuePair<float, float> from, KeyValuePair<float, float> to, float value, EMappingMode mappingMode)
	{
		float inputLerp = GetLerpValue(GetInputValue(from, mappingMode), GetInputValue(to, mappingMode), value);
		return Lerp(GetOutputValue(from, mappingMode), GetOutputValue(to, mappingMode), inputLerp);
	}

	private float GetLerpValue(float from, float to, float value)
	{
		return (value - from) / (to - from);
	}

	private float Lerp(float from, float to, float lerpValue)
	{
		return (from + lerpValue * (to - from));
	}
}