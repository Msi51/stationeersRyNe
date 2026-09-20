using UnityEngine;

namespace Util.Splines;

public class Spline : ISpline
{
	public Vector3 Start { get; }

	public Vector3 ControlA { get; }

	public Vector3 ControlB { get; }

	public Vector3 End { get; }

	public float StraightLineLength { get; }

	public float EstimatedActualLength { get; }

	public Spline(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end)
	{
		Start = start;
		ControlA = controlA;
		ControlB = controlB;
		End = end;
		StraightLineLength = (Start - End).magnitude;
		EstimatedActualLength = CalculateLength();
	}

	private float CalculateLength()
	{
		int num = 16;
		float num2 = 0f;
		Vector3 a = GetPosition(0f);
		for (int i = 1; i <= num; i++)
		{
			Vector3 position = GetPosition((float)i / (float)num);
			num2 += Vector3.Distance(a, position);
			a = position;
		}
		return num2;
	}

	public Vector3 GetPosition(float t)
	{
		return Bezier.GetPoint(Start, ControlA, ControlB, End, t);
	}

	public Vector3 GetTangent(float t)
	{
		return Bezier.GetFirstDerivative(Start, ControlA, ControlB, End, t);
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
