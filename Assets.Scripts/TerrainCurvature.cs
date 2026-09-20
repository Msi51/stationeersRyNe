using UnityEngine;

namespace Assets.Scripts;

public static class TerrainCurvature
{
	private static readonly int StartId = Shader.PropertyToID("_GlobalSphereStart");

	private static readonly int AmountId = Shader.PropertyToID("_GlobalSphereAmount");

	private static readonly int FoldId = Shader.PropertyToID("_GlobalSphereFold");

	private static readonly int FoldStartId = Shader.PropertyToID("_GlobalSphereFoldStart");

	private static readonly int LiftId = Shader.PropertyToID("_GlobalSphereLift");

	private static float _start;

	private static float _amount;

	private static float _fold;

	private static float _foldStart;

	private static float _lift;

	private static float _recenterX;

	private static float _recenterZ;

	public static void Refresh()
	{
		_start = Shader.GetGlobalFloat(StartId);
		_amount = Shader.GetGlobalFloat(AmountId);
		_fold = Shader.GetGlobalFloat(FoldId);
		_foldStart = Shader.GetGlobalFloat(FoldStartId);
		_lift = Shader.GetGlobalFloat(LiftId);
		_recenterX = OrbitalViewController.RecenterX;
		_recenterZ = OrbitalViewController.RecenterZ;
	}

	public static float Drop(Vector3 worldPos, Vector3 cameraPos)
	{
		float num = worldPos.x + _recenterX - cameraPos.x;
		float num2 = worldPos.z + _recenterZ - cameraPos.z;
		float num3 = Mathf.Sqrt(num * num + num2 * num2);
		float num4 = Mathf.Max(0f, num3 - _start) * _amount;
		float num5 = Mathf.Max(0f, num3 - _foldStart) * _fold;
		return num4 * num4 * 0.001f + num5 * num5 * 0.001f - _lift;
	}

	public static Vector3 Curve(Vector3 flatWorldPos, Vector3 cameraPos)
	{
		float num = Drop(flatWorldPos, cameraPos);
		return new Vector3(flatWorldPos.x + _recenterX, flatWorldPos.y - num, flatWorldPos.z + _recenterZ);
	}
}
