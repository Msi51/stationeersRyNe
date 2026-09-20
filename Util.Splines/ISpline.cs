using UnityEngine;

namespace Util.Splines;

public interface ISpline
{
	Vector3 Start { get; }

	Vector3 End { get; }

	float StraightLineLength { get; }

	float EstimatedActualLength { get; }

	Vector3 GetPosition(float t);

	Vector3 GetTangent(float t);

	Quaternion GetRotation(float t);
}
