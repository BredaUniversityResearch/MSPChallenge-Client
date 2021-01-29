using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
using UnityEngine.Networking;
using Utility.Serialization;


public static class Util
{
	private const float SELF_INTERSECTION_MAX_DISTANCE = 0.001f;
	private const float SELF_INTERSECTION_MAX_DISTANCE_SQUARED = SELF_INTERSECTION_MAX_DISTANCE * SELF_INTERSECTION_MAX_DISTANCE;
	private const float SEPARATE_POLYGONS_MAX_DISTANCE = 0.01f;

	//http://answers.unity3d.com/questions/37756/how-to-turn-a-string-to-an-int.html
	public static int IntParseFast(string value)
	{
		int result = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char letter = value[i];
			result = 10 * result + (letter - 48);
		}
		return result;
	}

	public static string LayerListToString(List<AbstractLayer> layers)
	{
		if (layers == null || layers.Count == 0)
			return "";

		string layerIDs = layers[0].ID.ToString();

		for (int i = 1; i < layers.Count; i++)
		{
			layerIDs += "," + layers[i].ID;
		}

		return layerIDs;
	}

	public static string IntListToString(List<int> list)
	{
		string result = "" + list[0];

		for (int i = 1; i < list.Count; i++)
		{
			result += "," + list[i];
		}

		return result;
	}

	public static Vector2 ParseToVector2(string vectorAsString)
	{

		vectorAsString = vectorAsString.Replace("(", "")
									   .Replace(")", "")
									   .Replace("[", "")
									   .Replace("]", "")
									   .Replace(" ", "");

		string[] splitVector = vectorAsString.Split(',');

		Vector2 vector = Vector2.zero;

		if (splitVector.Length == 2)
		{
			float x = ParseToFloat(splitVector[0]);
			float y = ParseToFloat(splitVector[1]);
			vector.x = x;
			vector.y = y;
		}
		else
		{
			Debug.LogError(vectorAsString + " is not a vector!");
		}

		return vector;
	}

	public static bool ParseToBool(string parseThis, bool defaultValue = false)
	{
		bool value = defaultValue;

		if (!bool.TryParse(parseThis, out value))
		{
			Debug.LogError("Failed to parse '" + parseThis + "' to " + value.GetType() + ", using default value of " + defaultValue);
		}

		return value;
	}

	public static int ParseToInt(string parseThis, int defaultValue = 0)
	{
		int value = defaultValue;

		if (!int.TryParse(parseThis, out value))
		{
			Debug.LogError("Failed to parse '" + parseThis + "' to " + value.GetType() + ", using default value of " + defaultValue);
		}

		return value;
	}

	public static float ParseToFloat(string parseThis, float defaultValue = 0)
	{
		float value;
		if (!float.TryParse(parseThis, Localisation.FloatNumberStyle, Localisation.NumberFormatting, out value))
		{
			Debug.LogError("Failed to parse '" + parseThis + "' to " + value.GetType() + ", using default value of " + defaultValue);
			return defaultValue;
		}
		return value;
	}

	public static bool CanBeParsedToFloat(string parseThis)
	{
		float value;
		return float.TryParse(parseThis, out value);
	}

	public static T DeserializeObject<T>(UnityWebRequest request, bool throwOnError = false)
	{
		return DeserializeObject<T>(request.url, request.downloadHandler.text, throwOnError);
	}

	public static T DeserializeObject<T>(string url, string data, bool throwOnError = false)
	{
		MemoryTraceWriter traceWriter = new MemoryTraceWriter();
		traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Warning;
		try
		{
			T result = JsonConvert.DeserializeObject<T>(data, new JsonSerializerSettings
			{
				TraceWriter = traceWriter,
				Error = (sender, errorArgs) =>
				{
					if (!throwOnError)
					{
						Debug.LogError("Unable to deserialize: '" + data + "'");
						Util.HandleDeserializationError(sender, errorArgs);
						Debug.LogError("Deserialization error: " + errorArgs.ErrorContext.Error);
					}
				},
				Converters = new List<JsonConverter> { new JsonConverterBinaryBool() }
			});

			return result;
		}
		catch
		{
			if (throwOnError)
			{
				throw;
			}
			else
			{
				Debug.LogError("Deserialization error:" + Environment.NewLine + "url: " + url + Environment.NewLine + "url result: " + data + Environment.NewLine + "Backtrace:" + Environment.NewLine + traceWriter.ToString());
			}
		}
		return default(T);
	}

	public static int YearToGameTime(int year)
	{
        return (year - Main.MspGlobalData.start) * 12;
	}

	public static string MonthToText(int months, bool shortened = false)
	{
        if (Main.MspGlobalData == null)
            return "";
		int baseYear = Main.MspGlobalData.start;
		while (months < 0)
		{
			months += 12;
			--baseYear;
		}
		int year = months / 12 + baseYear;
		int month = months % 12 + 1;

		if(shortened)
			return Localisation.DateFormatting.GetMonthName(month).Substring(0, 3) + " " + year.ToString();
		else
			return Localisation.DateFormatting.GetMonthName(month) + " " + year.ToString();
	}

	public static string MonthToYearText(int months)
	{
        if (Main.MspGlobalData == null)
            return "";
        int baseYear = Main.MspGlobalData.start;
		while (months < 0)
		{
			months += 12;
			--baseYear;
		}
		int year = months / 12 + baseYear;
		return year.ToString();
	}

	public static string MonthToMonthText(int months, bool shortened = false)
	{
		while (months < 0)
		{
			months += 12;
		}
		int month = months % 12 + 1;

		if (shortened)
			return Localisation.DateFormatting.GetMonthName(month).Substring(0, 3);
		else
			return Localisation.DateFormatting.GetMonthName(month);

	}

	public static void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
	{
		string currentError = errorArgs.ErrorContext.Error.Message;
		//errorArgs.ErrorContext.Handled = true;
		Debug.LogError(currentError + " in " + sender.ToString());
		//throw new System.Exception("exception!");
	}

	public static void MoveInHierarchy(GameObject go, int offset)
	{
		int index = go.transform.GetSiblingIndex();
		go.transform.SetSiblingIndex(index + offset);
	}

	public static Vector3 GetCenter(List<Vector3> points)
	{
		Vector3 center = Vector3.zero;

		int total = points.Count;

		for (int i = 0; i < total; i++)
		{
			center += points[i];
		}

		center /= (float)total;

		return center;
	}

	//http://stackoverflow.com/questions/9815699/how-to-calculate-centroid
	public static Vector3 GetCentroid(List<Vector3> poly)
	{
		float accumulatedArea = 0.0f;
		float centerX = 0.0f;
		float centerY = 0.0f;

		for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
		{
			float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
			accumulatedArea += temp;
			centerX += (poly[i].x + poly[j].x) * temp;
			centerY += (poly[i].y + poly[j].y) * temp;
		}

		if (Math.Abs(accumulatedArea) < 1E-7f)
			return Vector3.zero;  // Avoid division by zero

		accumulatedArea *= 3f;
		return new Vector3(centerX / accumulatedArea, centerY / accumulatedArea);
	}

	public static string RichTextColor(string text, string hex)
	{
		if (hex == null)
		{
			hex = text;
		}

		if (IsStringAHexColor(hex))
		{
			string color = "<font color=\"" + hex + "\">";
			color += text + "</font>";
			return color;
		}
		else
		{
			Debug.Log("Invalid Hex! " + hex);
			return text;
		}
	}

	public static Color HexToColor(string hex)
	{
		return HexToColor(hex, Color.white);
	}

	public static Color HexToColor(string hex, Color defaultColor)
	{
		if (hex == null)
		{
			return defaultColor;
		}

		if (IsStringAHexColor(hex))
		{
			return hexToColor(hex);
		}

		Debug.LogError("Failed to parse " + hex + " to Color, using default value of " + defaultColor);

		return defaultColor;
	}

	public static bool IsStringAHexColor(string hex)
	{
		hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
		hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF

		if (hex.Length < 6)
		{
			return false; //can't parse this.
		}

		int color = -1;

		if (int.TryParse(hex,
		  System.Globalization.NumberStyles.HexNumber,
		  System.Globalization.CultureInfo.InvariantCulture, out color))
		{
			return true;
		}

		return false;
	}

	private static Color hexToColor(string hex)
	{
		hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
		hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
		byte a = 255;//assume fully visible unless specified in hex
		byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
		//Only use alpha if the string has enough characters
		if (hex.Length == 8)
		{
			a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
		}
		return new Color32(r, g, b, a);
	}

	public static string ColorToHex(Color color)
	{
		byte rByte = (byte)(color.r * 255);
		byte gByte = (byte)(color.g * 255);
		byte bByte = (byte)(color.b * 255);
		byte aByte = (byte)(color.a * 255);

		return rByte.ToString("X2") + gByte.ToString("X2") + bByte.ToString("X2") + aByte.ToString("X2");
	}

	public static bool PointCollidesWithPoint(Vector2 a, Vector2 b, float maxDistance)
	{
		return (b - a).sqrMagnitude < (maxDistance * maxDistance);
	}

	public static bool PointCollidesWithLineString(Vector2 point, List<Vector3> lineString, float maxDistance)
	{
		for (int i = 0; i < lineString.Count - 1; ++i)
		{
			if (pointCollidesWithLine(point, lineString[i], lineString[i + 1], maxDistance)) { return true; }
		}
		return false;
	}

    public static float PointDistanceFromLineString(Vector2 point, List<Vector3> lineString)
    {
        float result = float.MaxValue;
        for (int i = 0; i < lineString.Count - 1; ++i)
            result = Mathf.Min(result, GetSquaredDistanceToLine(point, lineString[i], lineString[i + 1]));       
        return result;
    }

    public static bool LineStringCollidesWithLineString(List<Vector3> lineA, List<Vector3> lineB, out Vector3 lineACollidingLineSegmentCenter)
	{
		int lineACount = lineA.Count;
		int lineBCount = lineB.Count;

		for (int i = 0; i < lineACount - 1; ++i)
		{
			for (int j = 0; j < lineBCount - 1; ++j)
			{
				if (Util.GetLineSegmentIntersection(lineA[i], lineA[(i + 1) % lineACount], lineB[j], lineB[(j + 1) % lineBCount]))
				{
					lineACollidingLineSegmentCenter = Vector3.Lerp(lineA[i], lineA[(i + 1) % lineACount], 0.5f);
					return true;
				}
			}
		}

		lineACollidingLineSegmentCenter = Vector3.zero;
		return false;
	}

	private static bool pointCollidesWithLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float maxDistance)
	{
		return GetSquaredDistanceToLine(point, lineStart, lineEnd) < maxDistance * maxDistance;
	}

	public static float GetSquaredDistanceToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
	{
		// algorithm based on first answer from http://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
		float lineLengthSquared = (lineEnd - lineStart).sqrMagnitude;
		if (lineLengthSquared == 0f) { return (point - lineStart).sqrMagnitude; }
		float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(point - lineStart, lineEnd - lineStart) / lineLengthSquared));
		Vector2 projection = lineStart + t * (lineEnd - lineStart);
		return (projection - point).sqrMagnitude;
	}

	public static bool PointCollidesWithPolygon(Vector2 point, List<Vector3> polygon, List<List<Vector3>> holes, float maxDistance)
	{
		if (pointCollidesWithLineLoop(point, polygon, maxDistance)) { return true; }
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				if (pointCollidesWithLineLoop(point, hole, maxDistance)) { return true; }
			}
		}

		return PointInPolygon(point, polygon, holes);
	}

	private static bool pointCollidesWithLineLoop(Vector2 point, List<Vector3> lineLoop, float maxDistance)
	{
		for (int i = 0; i < lineLoop.Count; ++i)
		{
			if (pointCollidesWithLine(point, lineLoop[i], lineLoop[(i + 1) % lineLoop.Count], maxDistance)) { return true; }
		}
		return false;
	}

	public static bool PointInPolygon(Vector2 point, List<Vector3> polygon, List<List<Vector3>> holes)
	{
		if (!pointInPolygon(point, polygon)) { return false; }

		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				if (pointInPolygon(point, hole)) { return false; }
			}
		}

		return true;
	}

	private static bool pointInPolygon(Vector2 v, List<Vector3> p)
	{
		// algorithm taken from: http://codereview.stackexchange.com/questions/108857/point-inside-polygon-check
		int j = p.Count - 1;
		bool c = false;
		for (int i = 0; i < p.Count; j = i++) c ^= p[i].y > v.y ^ p[j].y > v.y && v.x < (p[j].x - p[i].x) * (v.y - p[i].y) / (p[j].y - p[i].y) + p[i].x;
		return c;
	}

    public static Vector3 GetLineCenter(List<Vector3> linePoints)
    {
        if (linePoints.Count == 1)
            return linePoints[0];
        float center = GetLineStringLength(linePoints) / 2f;

        float segmentStartTraversed = 0;
        for (int i = 0; i < linePoints.Count - 1; ++i)
        {
            float segmentEndTraversed = segmentStartTraversed + Vector3.Distance(linePoints[i], linePoints[i + 1]);
            //Is the center on this segment?
            if (segmentEndTraversed > center)
                return Vector3.Lerp(linePoints[i], linePoints[i + 1], (center - segmentStartTraversed) / (segmentEndTraversed - segmentStartTraversed));
            //No? Move to next segment
            else
                segmentStartTraversed = segmentEndTraversed;
        }
        return Vector3.zero;
    }

	public static Vector3 Compute2DPolygonCentroid(List<Vector3> aVertices) // 
	{
		Vector2 centroid = new Vector2(0, 0);
		float signedArea = 0.0f;
		float x0 = 0.0f; // Current vertex X
		float y0 = 0.0f; // Current vertex Y
		float x1 = 0.0f; // Next vertex X
		float y1 = 0.0f; // Next vertex Y
		float a = 0.0f;  // Partial signed area

		// For all vertices
		int i = 0;
		for (i = 0; i < aVertices.Count - 1; ++i)
		{
			x0 = aVertices[i].x;
			y0 = aVertices[i].y;
			x1 = aVertices[i + 1].x;
			y1 = aVertices[i + 1].y;
			a = x0 * y1 - x1 * y0;
			signedArea += a;
			centroid.x += (x0 + x1) * a;
			centroid.y += (y0 + y1) * a;
		}

		// Do last vertex separately to avoid performing an expensive
		// modulus operation in each iteration.
		x0 = aVertices[i].x;
		y0 = aVertices[i].y;
		x1 = aVertices[0].x;
		y1 = aVertices[0].y;
		a = x0 * y1 - x1 * y0;
		signedArea += a;
		centroid.x += (x0 + x1) * a;
		centroid.y += (y0 + y1) * a;

		if (signedArea == 0)
		{
			//Would otherwise result in devide by 0
			centroid = aVertices[aVertices.Count / 2]; 
		}
		else
		{
			signedArea *= 0.5f;
			centroid.x /= (6.0f * signedArea);
			centroid.y /= (6.0f * signedArea);
		}

		return centroid;
	}

    public static Vector3 Compute2DPolygonCentroidAlt(List<Vector3> vertices)
    {
        // Find the centroid.
        float X = 0;
        float Y = 0;
        float second_factor;
        for (int i = 0; i < vertices.Count; i++)
        {
            int j = (i + 1) % vertices.Count;
            second_factor =
                vertices[i].x * vertices[j].y -
                vertices[j].x * vertices[i].y;
            X += (vertices[i].x + vertices[j].x) * second_factor;
            Y += (vertices[i].y + vertices[j].y) * second_factor;
        }

        // Divide by 6 times the polygon's area.
        float polygon_area = GetPolygonArea(vertices, null);
        X /= (6 * polygon_area);
        Y /= (6 * polygon_area);

        // If the values are negative, the polygon is
        // oriented counterclockwise so reverse the signs.
        if (X < 0)
        {
            X = -X;
            Y = -Y;
        }

        return new Vector3(X, Y);
    }

    //Algorithm taken from: https://stackoverflow.com/questions/451426/how-do-i-calculate-the-area-of-a-2d-polygon
    public static float Compute2DPolygonSurface(List<Vector3> aVertices)
	{
		float area = 0;
		for (int i = 0; i < aVertices.Count; i++)
		{
			int i1 = (i + 1) % aVertices.Count;
			area += (aVertices[i].y + aVertices[i1].y) * (aVertices[i1].x - aVertices[i].x) / 2.0f;
		}
		return area;
	}

	public static bool BoundingBoxCollidesWithLineString(Rect rect, List<Vector3> lineString)
	{
		foreach (Vector3 point in lineString)
		{
			if (rect.Contains(point)) { return true; }
		}

		List<Vector2> boundPoints = new List<Vector2>()
		{
			new Vector2(rect.min.x, rect.min.y),
			new Vector2(rect.min.x, rect.max.y),
			new Vector2(rect.max.x, rect.max.y),
			new Vector2(rect.max.x, rect.min.y)
		};

		for (int i = 0; i < 4; ++i)
		{
			Vector2 b0 = boundPoints[i];
			Vector2 b1 = boundPoints[(i + 1) % 4];

			for (int j = 0; j < lineString.Count - 1; ++j)
			{
				Vector2 p0 = lineString[j];
				Vector2 p1 = lineString[j + 1];

				if (LineSegmentsIntersect(b0, b1, p0, p1))
				{
					return true;
				}
			}
		}

		return false;
	}

	public static bool PolygonPointIntersection(PolygonSubEntity polygon, PointSubEntity point)
	{
		//Check point inside bounding box -> if no, false
		if (!polygon.BoundingBox.Contains(point.GetPosition()))
			return false;
		//Check point inside poly -> if yes, true
		if (pointInPolygon(point.GetPosition(), polygon.GetPoints()))
			return true;
		return false;
	}

	public static bool PolygonLineIntersection(PolygonSubEntity polygon, LineStringSubEntity line)
	{
		Vector3 issueLocation;
		return PolygonLineIntersection(polygon, line, out issueLocation);
	}

	public static bool PolygonLineIntersection(PolygonSubEntity polygon, LineStringSubEntity line, out Vector3 issueLocation)
	{
		//Check bounding box intersection -> if no, false
		if (!polygon.BoundingBox.Overlaps(line.BoundingBox))
		{
			issueLocation = Vector3.zero;
			return false;
		}

		//Check line intersects bounding box -> if no, false
		//if (!BoundingBoxCollidesWithLineString(polygon.BoundingBox, line.GetLine()))
		//    return false;
		//Check line points inside poly -> if yes, true
		foreach (Vector2 linePoint in line.GetPoints())
		{
			if (pointInPolygon(linePoint, polygon.GetPoints()))
			{
				issueLocation = linePoint;
				return true;
			}
		}

		//Check line segment intersect poly -> if yes, true
		return CheckLineSegmentListIntersection(polygon.GetPoints(), line.GetPoints(), true, false, out issueLocation);
	}

	public static bool PolygonPolygonIntersection(PolygonSubEntity polygon1, PolygonSubEntity polygon2)
	{
		//Check bounding box intersection -> if no, false
		if (!polygon1.BoundingBox.Overlaps(polygon2.BoundingBox))
			return false;
		//Check line points inside poly -> if yes, true
		foreach (Vector2 polyPoint in polygon2.GetPoints())
			if (pointInPolygon(polyPoint, polygon1.GetPoints()))
				return true;
		//Check line segments intersect poly -> if yes, true
		Vector3 intersectionLocation;
		return CheckLineSegmentListIntersection(polygon1.GetPoints(), polygon2.GetPoints(), true, true, out intersectionLocation);
        //try
        //{

        //}
        //catch(Exception e)
        //{
        //    Debug.LogError($"Error when intersecting {polygon1.GetDatabaseID()} and {polygon2.GetDatabaseID()}");
        //    return true;
        //}
	}

	private static bool CheckLineSegmentListIntersection(List<Vector3> lineSegments1, List<Vector3> lineSegments2, bool segments1closed, bool segments2closed, out Vector3 intersectionLocation)
	{
		int i = 0;
		for (; i < lineSegments2.Count - 2; i++)
		{
			for (int j = 0; j < lineSegments1.Count - 2; j++)
			{
				if (LineSegmentsIntersect(lineSegments2[i], lineSegments2[i + 1], lineSegments1[j], lineSegments1[j + 1]))
				{
					//This is not accurate but at the moment I don't want to change the LineSegmentsIntersect function to accurately calculate the intersection point in fear of breaking stuff.
					intersectionLocation = Vector3.Lerp(lineSegments2[i], lineSegments2[i + 1], 0.5f); 
					return true;
				}
			}

			if (segments1closed && LineSegmentsIntersect(lineSegments2[i], lineSegments2[i + 1], lineSegments1[lineSegments1.Count - 1], lineSegments1[0]))
			{
				intersectionLocation = Vector3.Lerp(lineSegments2[i], lineSegments2[i + 1], 0.5f); 
				return true;
			}
		}

		if (segments2closed)
		{
			i = lineSegments2.Count - 2;
			for (int j = 0; j < lineSegments1.Count - 2; j++)
			{
				if (LineSegmentsIntersect(lineSegments2[i], lineSegments2[0], lineSegments1[j], lineSegments1[j + 1]))
				{
					intersectionLocation = Vector3.Lerp(lineSegments2[i], lineSegments2[0], 0.5f); 
					return true;
				}
			}

			if (segments1closed && LineSegmentsIntersect(lineSegments2[i], lineSegments2[0], lineSegments1[lineSegments1.Count - 1], lineSegments1[0]))
			{
				intersectionLocation = Vector3.Lerp(lineSegments2[i], lineSegments2[0], 0.5f); 
				return true;
			}
		}

		intersectionLocation = Vector3.zero;
		return false;
	}

	public static bool BoundingBoxCollidesWithPolygon(Rect rect, List<Vector3> polygon, List<List<Vector3>> holes)
	{
		// there are four possible cases:
		// 1. Rect is outside of Poly
		// 2. Rect intersects Poly
		// 3. Rect is inside of Poly
		// 4. Poly is inside of Rect

		// check if an arbitrary (rect.min in this case) point of the rectangle collides with the polygon
		if (pointInPolygon(rect.min, polygon))
		{
			bool pointInHole = false;
			if (holes != null)
			{
				foreach (List<Vector3> hole in holes)
				{
					if (pointInPolygon(rect.min, hole)) { pointInHole = true; }
				}
			}
			if (!pointInHole)
			{
				// rect.min collides with the polygon, so either case 2 or 3 is true
				return true;
			}
		}
		// rect.min didn't collide with the polygon, so case 3 is ruled out

		// check if an arbitrary (point 0 in this case) point of the polygon is inside the rectangle
		if (rect.Contains(polygon[0]))
		{
			// point 0 is inside the rectangle, so either case 2 or case 4 is true
			return true;
		}
		// point 0 isn't inside the rectangle, so case 4 is ruled out

		// both case 3 and 4 are ruled out, so either the rectangle and polygon intersect or the rectangle is outside the polygon
		return boundingBoxIntersectsWithPolygon(rect, polygon, holes);
	}

	private static bool boundingBoxIntersectsWithPolygon(Rect rect, List<Vector3> polygon, List<List<Vector3>> holes)
	{
		List<Vector2> boundPoints = new List<Vector2>()
		{
			new Vector2(rect.min.x, rect.min.y),
			new Vector2(rect.min.x, rect.max.y),
			new Vector2(rect.max.x, rect.max.y),
			new Vector2(rect.max.x, rect.min.y)
		};

		for (int i = 0; i < 4; ++i)
		{
			Vector2 b0 = boundPoints[i];
			Vector2 b1 = boundPoints[(i + 1) % 4];

			for (int j = 0; j < polygon.Count; ++j)
			{
				Vector2 p0 = polygon[j];
				Vector2 p1 = polygon[(j + 1) % polygon.Count];

				if (LineSegmentsIntersect(b0, b1, p0, p1))
				{
					return true;
				}
			}

			if (holes != null)
			{
				foreach (List<Vector3> hole in holes)
				{
					for (int j = 0; j < hole.Count; ++j)
					{
						Vector2 h0 = hole[j];
						Vector2 h1 = hole[(j + 1) % hole.Count];

						if (LineSegmentsIntersect(b0, b1, h0, h1))
						{
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	public static bool LineSegmentAndLineIntersect(List<Vector3> lineString, int upToIndex, Vector2 segmentStart, Vector2 segmentEnd)
	{
		for (int i = 0; i < upToIndex; ++i)
			if (LineSegmentsIntersect(lineString[i], lineString[i + 1], segmentStart, segmentEnd))
				return true;
		return false;
	}

	// returns the points that are part of intersecting lines or null if there are none
	public static HashSet<int> LineStringSelfIntersects(List<Vector3> lineString)
	{
		HashSet<int> problems = null;

		// line intersections
		for (int i = 0; i < lineString.Count - 2; ++i)
		{
			for (int j = i + 1; j < lineString.Count - 1; ++j)
			{
				if (LineSegmentsIntersect(lineString[i], lineString[i + 1], lineString[j], lineString[j + 1]))
				{
					if (problems == null) { problems = new HashSet<int>(); }
					problems.Add(i);
					problems.Add(i + 1);
					problems.Add(j);
					problems.Add(j + 1);
				}
			}
		}

		// points too close to lines
		for (int i = 0; i < lineString.Count; ++i)
		{
			for (int j = 0; j < lineString.Count - 1; ++j)
			{
				if (i != j && i != j + 1 && GetSquaredDistanceToLine(lineString[i], lineString[j], lineString[j + 1]) < SELF_INTERSECTION_MAX_DISTANCE_SQUARED)
				{
					if (problems == null) { problems = new HashSet<int>(); }
					problems.Add(i);
					problems.Add(j);
					problems.Add(j + 1);
				}
			}
		}

		return problems;
	}

	public static void RemoveSpikes(List<Vector3> polygon, List<List<Vector3>> holes)
	{
		removeSpikes(polygon);
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				removeSpikes(hole);
			}
		}
	}

	private static void removeSpikes(List<Vector3> loop)
	{
		if (loop.Count < 3) { return; }
		List<Vector3> newLoop = new List<Vector3>();

		for (int i = 0; i < loop.Count; ++i)
		{
			int prev = (i + loop.Count - 1) % loop.Count;
			int next = (i + 1) % loop.Count;

			if (!pointsFormSpike(loop[prev], loop[i], loop[next]))
			{
				newLoop.Add(loop[i]);
			}
		}

		loop.Clear();
		loop.AddRange(newLoop);
	}

	private static bool pointsFormSpike(Vector2 a, Vector2 b, Vector2 c)
	{
		return AngleBetween(b - a, b - c) < 1;
	}

	public static double AngleBetween(Vector2 a, Vector2 b)
	{
		double sin = a.x * b.y - b.x * a.y;
		double cos = a.x * b.x + a.y * b.y;

		return Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
	}

	public static void PlacePointsAtSelfIntersections(List<Vector3> polygon, List<List<Vector3>> holes)
	{
		bool done = false;
		int counter = 0;
		while (!done && counter < 10000)
		{
			Vector3[] lineSegments;
			int[] lineEndPoints;
			getLineSegmentsAndEndPoints(polygon, holes, out lineSegments, out lineEndPoints);

			bool intersectionFound = false;
			for (int i = 0; i < lineSegments.Length - 3 && !intersectionFound; i += 2)
			{
				for (int j = i + 2; j < lineSegments.Length - 1 && !intersectionFound; j += 2)
				{
					if (LineSegmentsIntersect(lineSegments[i], lineSegments[i + 1], lineSegments[j], lineSegments[j + 1]))
					{
						Vector2 intersection = GetLineLineIntersection(lineSegments[i], lineSegments[i + 1], lineSegments[j], lineSegments[j + 1]);
						if (intersection != (Vector2)lineSegments[i] && intersection != (Vector2)lineSegments[i + 1] &&
							intersection != (Vector2)lineSegments[j] && intersection != (Vector2)lineSegments[j + 1])
						{
							if (intersection != Vector2.zero)
							{
								// !!! insert point at j+1 before i+1! (higher index first)
								insertPolygonPointAt(polygon, holes, lineEndPoints[j + 1], intersection);
								insertPolygonPointAt(polygon, holes, lineEndPoints[i + 1], intersection);
								intersectionFound = true;
							}
						}
					}
				}
			}

			if (!intersectionFound)
			{
				done = true;
			}
			counter++;
		}
	}

	// returns the points that are part of intersecting lines or null if there are none
	public static HashSet<int> PolygonSelfIntersects(List<Vector3> polygon, List<List<Vector3>> holes)
	{
		HashSet<int> problems = null;

		Vector3[] lineSegments;
		int[] lineEndPoints;
		getLineSegmentsAndEndPoints(polygon, holes, out lineSegments, out lineEndPoints);

		// line intersections
		for (int i = 0; i < lineSegments.Length - 3; i += 2)
		{
			for (int j = i + 2; j < lineSegments.Length - 1; j += 2)
			{
				if (LineSegmentsIntersect(lineSegments[i], lineSegments[i + 1], lineSegments[j], lineSegments[j + 1]))
				{
					if (problems == null) { problems = new HashSet<int>(); }
					problems.Add(lineEndPoints[i]);
					problems.Add(lineEndPoints[i + 1]);
					problems.Add(lineEndPoints[j]);
					problems.Add(lineEndPoints[j + 1]);
				}
			}
		}

		// points too close to lines
		for (int i = 0; i < lineSegments.Length - 1; i += 2)
		{
			for (int j = 0; j < lineSegments.Length - 1; j += 2)
			{
				if (lineEndPoints[i] != lineEndPoints[j] && lineEndPoints[i] != lineEndPoints[j + 1] &&
					GetSquaredDistanceToLine(lineSegments[i], lineSegments[j], lineSegments[j + 1]) < SELF_INTERSECTION_MAX_DISTANCE_SQUARED)
				{
					if (problems == null) { problems = new HashSet<int>(); }
					problems.Add(lineEndPoints[i]);
					problems.Add(lineEndPoints[j]);
					problems.Add(lineEndPoints[j + 1]);
				}
			}
		}

		return problems;
	}

	private static void getLineSegmentsAndEndPoints(List<Vector3> polygon, List<List<Vector3>> holes, out Vector3[] lineSegments, out int[] lineEndPoints)
	{
		int lineSegmentCount = polygon.Count;
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				lineSegmentCount += hole.Count;
			}
		}

		lineSegments = new Vector3[lineSegmentCount * 2];
		lineEndPoints = new int[lineSegmentCount * 2];
		for (int i = 0; i < polygon.Count; ++i)
		{
			lineSegments[i * 2] = polygon[i];
			lineEndPoints[i * 2] = i;

			lineSegments[i * 2 + 1] = polygon[(i + 1) % polygon.Count];
			lineEndPoints[i * 2 + 1] = (i + 1) % polygon.Count;
		}

		if (holes != null)
		{
			int indexOffset = polygon.Count;
			foreach (List<Vector3> hole in holes)
			{
				for (int i = 0; i < hole.Count; ++i)
				{
					lineSegments[(indexOffset + i) * 2] = hole[i];
					lineEndPoints[(indexOffset + i) * 2] = indexOffset + i;

					lineSegments[(indexOffset + i) * 2 + 1] = hole[(i + 1) % hole.Count];
					lineEndPoints[(indexOffset + i) * 2 + 1] = indexOffset + (i + 1) % hole.Count;
				}
				indexOffset += hole.Count;
			}
		}
	}

	// returns null if all self intersections are gone, or the endpoints of the remaing self intersecting lines if some are left
	public static HashSet<int> TryFixingSelfIntersections(List<Vector3> polygon, List<List<Vector3>> holes, HashSet<int> problems, float fixOffset)
	{
		List<Vector3> problemLines = getProblemLines(polygon, holes, problems);

		foreach (int problem in problems)
		{
			Vector3 problemPosition = getPolygonPointPosition(polygon, holes, problem);

			List<Vector3> offsets = new List<Vector3>();
			List<Vector3> lowPriorityOffsets = new List<Vector3>();

			for (int i = 0; i + 1 < problemLines.Count; i += 2)
			{
				if (problemLines[i] == problemPosition || problemLines[i + 1] == problemPosition)
				{
					Vector3 collinear = (problemLines[i + 1] - problemLines[i]).normalized * fixOffset;
					// calculate the offsets perpedicular to the problem line in both directions
					lowPriorityOffsets.Add(new Vector3(collinear.y, -collinear.x));
					lowPriorityOffsets.Add(new Vector3(-collinear.y, collinear.x));
				}
				else if (GetSquaredDistanceToLine(problemPosition, problemLines[i], problemLines[i + 1]) < fixOffset * fixOffset)
				{
					Vector3 collinear = (problemLines[i + 1] - problemLines[i]).normalized * fixOffset;
					// calculate the offsets perpedicular to the problem line in both directions
					offsets.Add(new Vector3(collinear.y, -collinear.x));
					offsets.Add(new Vector3(-collinear.y, collinear.x));
				}
			}

			offsets.AddRange(lowPriorityOffsets);

			int initialTooCloseLines = getNumberOfLinesThatAreTooClose(problemPosition, problemLines, fixOffset);

			bool improvementFound = false;
			for (int i = 0; i < offsets.Count && !improvementFound; ++i)
			{
				setPolygonPointPosition(polygon, holes, problem, problemPosition + offsets[0]);
				if (getNumberOfLinesThatAreTooClose(problemPosition, problemLines, fixOffset) < initialTooCloseLines)
				{
					improvementFound = true;
				}
			}

			if (!improvementFound)
			{
				setPolygonPointPosition(polygon, holes, problem, problemPosition);
			}
		}

		return PolygonSelfIntersects(polygon, holes);
	}

	private static int getNumberOfLinesThatAreTooClose(Vector3 point, List<Vector3> lines, float distanceThreshold)
	{
		int result = 0;
		float thresholdSquared = distanceThreshold * distanceThreshold;
		for (int i = 0; i + 1 < lines.Count; i += 2)
		{
			if (GetSquaredDistanceToLine(point, lines[i], lines[i + 1]) < thresholdSquared)
			{
				result++;
			}
		}
		return result;
	}

	//https://stackoverflow.com/questions/1934210/finding-a-point-on-a-line
	public static Vector3 GetPointAlongLine(Vector3 p1, Vector3 p2, float distance)
	{
		if (distance == 0.0f)
			return p1;

		float d = Mathf.Sqrt(((p2.x - p1.x) * (p2.x - p1.x)) + ((p2.y - p1.y) * (p2.y - p1.y)));

		float r = distance / d;

		Vector3 point = Vector3.zero;

		point.x = r * p2.x + (1 - r) * p1.x;
		point.y = r * p2.y + (1 - r) * p1.y;

		return point;
	}

	private static List<Vector3> getProblemLines(List<Vector3> polygon, List<List<Vector3>> holes, HashSet<int> problems)
	{
		List<Vector3> problemLines = new List<Vector3>();

		foreach (int problem in problems)
		{
			Vector3 problemPosition = getPolygonPointPosition(polygon, holes, problem);
			if (problem < polygon.Count)
			{
				int next = (problem + 1) % polygon.Count;
				if (problems.Contains(next))
				{
					problemLines.Add(problemPosition);
					problemLines.Add(getPolygonPointPosition(polygon, holes, next));
				}
			}
			else
			{
				int offset = polygon.Count;
				if (holes != null)
				{
					foreach (List<Vector3> hole in holes)
					{
						if (problem < offset + hole.Count)
						{
							int next = offset + ((problem - offset) + 1) % hole.Count;
							if (problems.Contains(next))
							{
								problemLines.Add(problemPosition);
								problemLines.Add(getPolygonPointPosition(polygon, holes, next));
								break;
							}
						}
						offset += hole.Count;
					}
				}
			}
		}

		return problemLines;
	}

	private static void insertPolygonPointAt(List<Vector3> polygon, List<List<Vector3>> holes, int index, Vector3 newPoint)
	{
		if (index < polygon.Count)
		{
			polygon.Insert(index, newPoint);
			return;
		}
		index -= polygon.Count;

		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				if (index < hole.Count)
				{
					hole.Insert(index, newPoint);
					return;
				}
				index -= hole.Count;
			}
		}

		Debug.LogError("Invalid index in Util.insertPolygonPointAt(): " + index);
	}

	private static Vector3 getPolygonPointPosition(List<Vector3> polygon, List<List<Vector3>> holes, int index)
	{
		if (index < polygon.Count)
		{
			return polygon[index];
		}
		index -= polygon.Count;

		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				if (index < hole.Count)
				{
					return hole[index];
				}
				index -= hole.Count;
			}
		}

		Debug.LogError("Invalid index in Util.getPolygonPointPosition(): " + index);
		return Vector3.zero;
	}

	private static void setPolygonPointPosition(List<Vector3> polygon, List<List<Vector3>> holes, int index, Vector3 newPosition)
	{
		if (index < polygon.Count)
		{
			polygon[index] = newPosition;
			return;
		}
		index -= polygon.Count;

		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				if (index < hole.Count)
				{
					hole[index] = newPosition;
					return;
				}
				index -= hole.Count;
			}
		}

		Debug.LogError("Invalid index in Util.setPolygonPointPosition(): " + index);
	}

	public static bool LineSegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		// algorithm from: http://gamedev.stackexchange.com/questions/26004/how-to-detect-2d-line-on-line-collision

		float denominator = ((b.x - a.x) * (d.y - c.y)) - ((b.y - a.y) * (d.x - c.x));
		float numerator1 = ((a.y - c.y) * (d.x - c.x)) - ((a.x - c.x) * (d.y - c.y));
		float numerator2 = ((a.y - c.y) * (b.x - a.x)) - ((a.x - c.x) * (b.y - a.y));

		bool intersection;

		// Detect coincident lines
		if (denominator == 0)
		{
			intersection = numerator1 == 0 && numerator2 == 0;
		}
		else
		{
			float r = numerator1 / denominator;
			float s = numerator2 / denominator;

			intersection = (r > 0 && r < 1) && (s > 0 && s < 1);
		}

		if (intersection)
		{
			// intersection is also true when the lines are coincident but don't overlap, so check if that is the case
			Rect bounds0 = new Rect(Vector2.Min(a, b), Vector2.Max(a, b) - Vector2.Min(a, b));
			Rect bounds1 = new Rect(Vector2.Min(c, d), Vector2.Max(c, d) - Vector2.Min(c, d));
			return bounds0.Overlaps(bounds1);
		}
		else
		{
			return false;
		}
	}

	// NOTE: returns Vector2.zero if lines are parallel
	//Also a very important note is that this actually uses lines which extend into infinty.
	private static Vector2 GetLineLineIntersection(Vector2 line0start, Vector2 line0end, Vector2 line1start, Vector2 line1end)
	{
		// first get A, B and C for line 0 where Ax+By=C
		float a0 = line0end.y - line0start.y;
		float b0 = line0start.x - line0end.x;
		float c0 = a0 * line0start.x + b0 * line0start.y;

		// same for line 1
		float a1 = line1end.y - line1start.y;
		float b1 = line1start.x - line1end.x;
		float c1 = a1 * line1start.x + b1 * line1start.y;

		float det = a0 * b1 - a1 * b0;
		if (det == 0)
		{
			return Vector2.zero; // lines are parallel
		}

		return new Vector2((b1 * c0 - b0 * c1) / det, (a0 * c1 - a1 * c0) / det);
	}

	public static bool GetLineSegmentIntersection(Vector2 line0From, Vector2 line0To, Vector2 line1From, Vector2 line1To)
	{
		// https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
		Vector3 thisLine = line0To - line0From;
		Vector3 otherLine = line1To - line1From;
		Vector3 deltaFromPoints = (line1From - line0From);

		Vector3 startPointCrossResult = Vector3.Cross(deltaFromPoints, thisLine);
		if (Mathf.Abs(startPointCrossResult.z) < float.Epsilon)
		{
			//Lines are collinear and intersect if they have any overlap.
			return ((line1From.x - line0From.x < 0.0) != (line1From.x - line0To.x < 0.0)) ||
			       ((line1From.y - line0From.y < 0.0) != (line1From.y - line0To.y < 0.0));
		}

		Vector3 crossResult = Vector3.Cross(thisLine, otherLine);
		if (Mathf.Abs(crossResult.z) < float.Epsilon)
		{
			return false; //parallel lines, no intersection.
		}

		float rcpCrossResult = 1.0f / crossResult.z;
		float thisLineIntersectionTime = Vector3.Cross(deltaFromPoints, otherLine).z * rcpCrossResult;
		float otherLineIntersectionTime = Vector3.Cross(deltaFromPoints, thisLine).z * rcpCrossResult;

		return (thisLineIntersectionTime >= 0.0 && thisLineIntersectionTime <= 1.0 && otherLineIntersectionTime >= 0.0 && otherLineIntersectionTime <= 1.0);
	}

	//public static List<Vector3> SimplifyLineString(List<Vector3> lineString, float tangDelta, float distDelta)
	//{
	//    return SimplifyPolygon(lineString, tangDelta, distDelta);
	//}

	//public static List<Vector3> SimplifyPolygon(List<Vector3> polygon, float tangDelta, float distDelta)
	//{
	//    List<Vector3> newPolygon = new List<Vector3>();
	//    int total = polygon.Count;
	//    //*
	//    float x0 = 0, y0 = 0; // First point
	//    float x1 = 0, y1 = 0; // Last point
	//    float cx = 0, cy = 0; // Curr point
	//    Vector2 newVertex;
	//    float origTang, newTang;
	//    float distance;
	//    int count;

	//    int startIndex = 0;
	//    if (polygon[0].x == polygon[total - 1].x && polygon[0].y == polygon[total - 1].y)
	//    {
	//        startIndex = 1;
	//    }
	//    for (int i = startIndex; i < total; i++)
	//    {
	//        cx = polygon[i].x;
	//        cy = polygon[i].y;
	//        newVertex = new Vector2(cx, cy);
	//        count = newPolygon.Count;

	//        // Calculate tangants
	//        origTang = (y1 - y0) / (x1 - x0);
	//        newTang = (cy - y1) / (cx - x1);
	//        distance = Mathf.Sqrt((cy - y1) * (cy - y1) + (cx - x1) * (cx - x1));
	//        if (count > 2 && (Mathf.Abs(newTang - origTang) < tangDelta || distance < distDelta))
	//        { // Check if point point lies on line segment delta
	//            // Replace last vector
	//            newPolygon[count - 1] = newVertex;
	//        }
	//        else
	//        {
	//            // update last points
	//            x0 = x1; y0 = y1;
	//            x1 = cx; y1 = cy;
	//            // Add Vertex
	//            newPolygon.Add(newVertex);
	//        }
	//    }

	//    //Debug.Log("Original number of vertices " + polygon.Count + " clean up  to " + newPolygon.Count);
	//    return newPolygon;
	//}

	public static bool PolygonIsClockwise(List<Vector3> polygon)
	{
		float result = 0;
		for (int i = 0; i < polygon.Count; ++i)
		{
			int j = (i + 1) % polygon.Count;
			result += (polygon[j].x - polygon[i].x) * (polygon[j].y + polygon[i].y);
		}
		return result > 0;
	}

	public static bool AreAllSame<T>(this IEnumerable<T> enumerable)
	{
		var enumerator = enumerable.GetEnumerator();

		var toCompare = default(T);
		if (enumerator.MoveNext())
		{
			toCompare = enumerator.Current;
		}

		while (enumerator.MoveNext())
		{
			if (!toCompare.Equals(enumerator.Current))
			{
				return false;
			}
		}

		return true;
	}

	// assumes the polygon and the holes don't intersect or self-intersect, the polygon contains all holes
	//  and none of the holes contain another hole.
	public static float GetPolygonArea(List<Vector3> polygon, List<List<Vector3>> holes)
	{
		float area = GetPolygonArea(polygon);
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				area -= GetPolygonArea(hole);
			}
		}
		return area;
	}

	// assumes the polygon doesn't self-intersect
	public static float GetPolygonArea(List<Vector3> polygon)
	{
		float area = 0;
		for (int i = 0; i < polygon.Count; ++i)
		{
			int j = (i + 1) % polygon.Count;
			area += polygon[i].y * polygon[j].x - polygon[i].x * polygon[j].y;
		}
		return Mathf.Abs(area * 0.5f);
	}

	public static float GetLineStringLength(List<Vector3> line)
	{
		float length = 0;
		for (int i = 1; i < line.Count; ++i)
			length += Vector3.Distance(line[i], line[i - 1]);
		return length;
	}

	// input should be a list with one polygon and a list with a single list containing the holes of that polygon
	// the result can be one or multiple polygons (the holes list should have the same number of items as the polygons list)
	public static void SeparatePolygons(ref List<List<Vector3>> polygons, ref List<List<List<Vector3>>> holes)
	{
		if (polygons.Count != 1 || holes.Count != 1) { Debug.LogError("invalid polygon or holes count"); return; }

		// make sure the polygon is clockwise and the holes are counter clockwise
		if (!PolygonIsClockwise(polygons[0]))
		{
			polygons[0].Reverse();
		}
		if (holes[0] != null)
		{
			for (int i = 0; i < holes[0].Count; ++i)
			{
				if (PolygonIsClockwise(holes[0][i]))
				{
					holes[0][i].Reverse();
				}
			}
		}

		polygons = separatePolygons(polygons[0]);
		List<List<Vector3>> reversedPolygons = getAndRemoveReversedPolygons(polygons, true);
		if (reversedPolygons.Count > 0)
		{
			if (holes[0] == null) { holes[0] = new List<List<Vector3>>(); }
			holes[0].AddRange(reversedPolygons);
		}

		// store all holes sorted by their area
		SortedList<float, List<Vector3>> newHolesSorted = new SortedList<float, List<Vector3>>();
		if (holes[0] != null)
		{
			foreach (List<Vector3> hole in holes[0])
			{
				List<List<Vector3>> separatedHoles = separatePolygons(hole);

				List<List<Vector3>> reversedHoles = getAndRemoveReversedPolygons(separatedHoles, false);
				polygons.AddRange(reversedHoles);

				foreach (List<Vector3> separatedHole in separatedHoles)
				{
					newHolesSorted.Add(GetPolygonArea(separatedHole), separatedHole);
				}
			}
		}

		holes = new List<List<List<Vector3>>>();
		for (int i = 0; i < polygons.Count; ++i)
		{
			holes.Add(null);
		}

		// process the holes in descending order (so largest hole first) to make sure it still works in the 'holes within holes' case
		for (int i = newHolesSorted.Count - 1; i >= 0; --i)
		{
			List<Vector3> hole = newHolesSorted.Values[i];

			for (int j = 0; j < polygons.Count; ++j)
			{
				if (PointInPolygon(hole[0], polygons[j], holes[j]))
				{
					bool anyPointsOutsidePolygon = false;
					foreach (Vector3 holePoint in hole)
					{
						if (!PointInPolygon(holePoint, polygons[j], holes[j])) { anyPointsOutsidePolygon = true; }
					}

					if (!anyPointsOutsidePolygon)
					{
						if (holes[j] == null) { holes[j] = new List<List<Vector3>>(); }
						holes[j].Add(hole);
						break;
					}
				}
			}
		}
	}

	private static List<List<Vector3>> getAndRemoveReversedPolygons(List<List<Vector3>> polygons, bool referencePolygonIsClockwise)
	{
		List<List<Vector3>> reversedPolygons = new List<List<Vector3>>();
		for (int i = polygons.Count - 1; i >= 0; i--)
		{
			if (PolygonIsClockwise(polygons[i]) != referencePolygonIsClockwise)
			{
				//Debug.Log("polygon " + i + " is reversed");
				reversedPolygons.Add(polygons[i]);
				polygons.RemoveAt(i);
			}
			//else
			//{
			//    Debug.Log("polygon " + i + " is not reversed");
			//}
		}
		return reversedPolygons;
	}

	private static List<List<Vector3>> separatePolygons(List<Vector3> polygon)
	{
		List<List<Vector3>> polygons = new List<List<Vector3>>();

		Stack<Vector3> pointStack = new Stack<Vector3>();
		foreach (Vector3 point in polygon)
		{
			bool createPolygon = false;
			foreach (Vector3 stackPoint in pointStack)
			{
				if ((stackPoint - point).sqrMagnitude < SEPARATE_POLYGONS_MAX_DISTANCE * SEPARATE_POLYGONS_MAX_DISTANCE)
				{
					createPolygon = true;
				}
			}

			if (createPolygon)
			{
				List<Vector3> newPolygon = createPolygonFromStack(point, pointStack);
				if (newPolygon != null) { newPolygon.Reverse(); polygons.Add(newPolygon); }
			}
			else
			{
				pointStack.Push(point);
			}
		}

		if (pointStack.Count > 0)
		{
			List<Vector3> newPolygon = createPolygonFromStack(pointStack.Pop(), pointStack);
			if (newPolygon != null) { newPolygon.Reverse(); polygons.Add(newPolygon); }
		}

		return polygons;
	}

	private static List<Vector3> createPolygonFromStack(Vector3 initialPoint, Stack<Vector3> stack)
	{
		List<Vector3> newPolygon = new List<Vector3> { initialPoint };
		while (stack.Count > 0 && (stack.Peek() - initialPoint).sqrMagnitude >= SEPARATE_POLYGONS_MAX_DISTANCE * SEPARATE_POLYGONS_MAX_DISTANCE)
		{
			newPolygon.Add(stack.Pop());
		}

		if (newPolygon.Count > 2)
		{
			return newPolygon;
		}
		else
		{
			return null;
		}
	}
}



public static class ExtensionMethods
{

	public static void Clear(this StringBuilder strbldr, int length = 0)
	{
		strbldr.Length = length;
	}

	public static T1 GetFirstValue<T0, T1>(this Dictionary<T0, T1> dictionary)
	{
		foreach (var kvp in dictionary)
		{
			return kvp.Value;
		}

		return default(T1);
	}

	public static T0 GetFirstKey<T0, T1>(this Dictionary<T0, T1> dictionary)
	{
		foreach (var kvp in dictionary)
		{
			return kvp.Key;
		}

		return default(T0);
	}

	public static bool TryGetKeyFromValue<T0, T1>(this Dictionary<T0, T1> dictionary, T1 value, out T0 key) where T1 : System.IEquatable<T1>
	{
		foreach (var kvp in dictionary)
		{
			if (kvp.Value.Equals(value))
			{
				key = kvp.Key;
				return true;
			}
		}

		key = default(T0);
		return false;
	}

	public static bool IsEqualTo(this Color col, Color other)
	{
		return col.r == other.r && col.g == other.g && col.b == other.b;
	}

	public static void SetAlpha(this Graphic graphic, float alpha)
	{
		Color next = graphic.color;
		next.a = Mathf.Clamp01(alpha);
		graphic.color = next;
	}

	public static float Remap(this float value, float from1, float to1, float from2, float to2)
	{
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	/// <summary>
	/// Remaps value from 0f, 1f to -1f, 1f
	/// </summary>
	public static float MinusToOne(this float value)
	{
		return value.Remap(0f, 1f, -1f, 1f);
	}

	/// <summary>
	/// Remaps value from -1f, 1f to 0f, 1f
	/// </summary>
	public static float ZeroToOne(this float value)
	{
		return value.Remap(-1f, 1f, 0f, 1f);
	}

	public static string Abbreviated(this float value)
	{
		string str = "";
		float sign = Mathf.Sign(value);
		value = Mathf.Abs(value);
		if (value >= 1000000f || value <= -1000000f)
			str = Mathf.Floor(value / 1000000).ToString() + " M";
		else if (value >= 1000f || value <= -1000f)
			str = Mathf.Floor(value / 1000).ToString() + " K";
		else if (value >= 1f || value <= -1f)
			str = value.ToString("0");
		else if (value == 0f)
			str = value.ToString("0");
		else
			str = value.ToString("N");

		str = (sign == -1) ? str.Insert(0, "- ") : str;

		return str;
	}

	public static bool IsInfluencingState(this Plan.PlanState state)
	{
		return state != Plan.PlanState.DESIGN && state != Plan.PlanState.DELETED;
	}

	public static string FormatAsCoordinateText(this float realWorldPos)
	{
		return (realWorldPos).ToString("n0", CultureInfo.CurrentUICulture) + " m";
	}

    public static string GetDisplayName(this Plan.PlanState state)
    {
        switch (state)
        {
            case Plan.PlanState.APPROVAL:
                return "Approval";
            case Plan.PlanState.APPROVED:
                return "Approved";
            case Plan.PlanState.CONSULTATION:
                return "Consultation";
            case Plan.PlanState.DELETED:
                return "Archived";
            case Plan.PlanState.DESIGN:
                return "Design";
            default:
                return "Implemented";
        }
    }
}