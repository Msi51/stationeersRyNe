using UnityEngine;
using Util.Splines;

namespace Objects.Electrical;

public class CableRenderData
{
	public Mesh CableMesh;

	public Matrix4x4 Matrix;

	private const float SAG_FACTOR = 0.06f;

	private static readonly Vector2[] CableProfile = BoxProfile(0.03f, 0.03f);

	public CableRenderData(PylonNode a, PylonNode b)
	{
		Spline spline = BuildSpline(a.Transform.position, b.Transform.position);
		CableMesh = CableMeshBuilder.Build(spline, CableProfile, 1f, a.Transform);
		Matrix = a.Transform.localToWorldMatrix;
	}

	public static Spline BuildSpline(Vector3 start, Vector3 end)
	{
		Vector3 normalized = (end - start).normalized;
		float num = Vector3.Distance(start, end);
		float num2 = num / 3f;
		Vector3 vector = Vector3.down * Mathf.Min(num * 0.06f * 4f / 3f, 1f);
		return new Spline(start, start + normalized * num2 + vector, end - normalized * num2 + vector, end);
	}

	public void Clear()
	{
		if (CableMesh != null)
		{
			Object.Destroy(CableMesh);
			CableMesh = null;
		}
	}

	private static Vector2[] BoxProfile(float w, float t)
	{
		return new Vector2[4]
		{
			new Vector2((0f - w) * 0.5f, 0f),
			new Vector2(w * 0.5f, 0f),
			new Vector2(w * 0.5f, t),
			new Vector2((0f - w) * 0.5f, t)
		};
	}
}
