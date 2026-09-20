using UnityEngine;

namespace Trading;

public struct EvaluablePosition(float x, float y, float z) : IEvaluable
{
	public Vector3 Value = new Vector3(x, y, z);
}
