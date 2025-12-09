using System;
using System.Collections.Generic;

public class RasterScaleConfig
{
	public enum DensitymapInterpolation { Lin, Log, Quad, LinGrouped }

	public string unit;
	public float min_value;
	public float max_value;
	public DensitymapInterpolation interpolation;
	public ScaleConfigGrouping[] groups;

	public float EvaluateOutput(float a_t)
	{
		if (interpolation == DensitymapInterpolation.Log)
			return (max_value - min_value) * a_t * a_t + min_value;
		else if (interpolation == DensitymapInterpolation.Quad)
		{
			float inv = 1f - a_t;
			return (max_value - min_value) * (1f - inv * inv) + min_value;
		}
		else if (interpolation == DensitymapInterpolation.LinGrouped)
		{
			for (int i = groups.Length - 2; i >= 0; i--)
			{
				if (a_t >= groups[i].normalised_input_value)
				{
					float remappedT = (a_t - groups[i].normalised_input_value) / (groups[i + 1].normalised_input_value - groups[i].normalised_input_value);
					return groups[i].min_output_value + remappedT * (groups[i + 1].min_output_value - groups[i].min_output_value);
				}
			}
			return groups[0].min_output_value;
		}
		return (max_value - min_value) * a_t + min_value;
	}

	public float EvaluateT(float a_t)
	{
		if (interpolation == DensitymapInterpolation.Log)
			return a_t * a_t;
		else if (interpolation == DensitymapInterpolation.Quad)
		{
			float inv = 1f - a_t;
			return 1f - inv * inv;
		}
		else if (interpolation == DensitymapInterpolation.LinGrouped)
		{
			for (int i = groups.Length - 2; i >= 0; i--)
			{
				if (a_t >= groups[i].normalised_input_value)
				{
					float remappedT = (a_t - groups[i].normalised_input_value) / (groups[i + 1].normalised_input_value - groups[i].normalised_input_value);
					float output = groups[i].min_output_value + remappedT * (groups[i + 1].min_output_value - groups[i].min_output_value);
					return (output - min_value) / (max_value - min_value);
				}
			}
			return 0f;
		}
		return a_t;
	}
}

public class ScaleConfigGrouping
{
	public float normalised_input_value;
	public float min_output_value;
}