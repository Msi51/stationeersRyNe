using UnityEngine;

namespace Util.Splines;

public class Line : ISpline
{
	public Vector3 Start { get; }

	public Vector3 End { get; }

	public float StraightLineLength { get; }

	public float EstimatedActualLength { get; }

	public Line(Vector3 start, Vector3 end)
	{
		Start = start;
		End = end;
		StraightLineLength = (Start - End).magnitude;
		EstimatedActualLength = StraightLineLength;
	}

	public Vector3 GetPosition(float t)
	{
		return Vector3.Lerp(Start, End, t);
	}

	public Vector3 GetTangent(float t)
	{
		return End - Start;
	}

	public Vector3 GetDirection(float t, bool flat = false)
	{
		Vector3 normalized = GetTangent(t).normalized;
		if (flat)
		{
			normalized.y = 0f;
		}
		return normalized;
	}

	public Quaternion GetRotation(float t)
	{
		Vector3 direction = GetDirection(t);
		if (!(direction.sqrMagnitude <= float.Epsilon))
		{
			return Quaternion.LookRotation(direction, Vector3.up);
		}
		return Quaternion.identity;
	}
}
