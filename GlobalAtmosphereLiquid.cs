using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using UnityEngine;

public static class GlobalAtmosphereLiquid
{
	private const float DEFAULT_LIQUID_HEIGHT = -1000f;

	private const float MAX_WATER_HEIGHT = 10f;

	private const double LiquidRenderThresholdLitres = 0.3;

	public static readonly VolumeLitres RenderThreshold = new VolumeLitres(1500000.0) * PlanetaryAtmosphereSimulation.LiquidVolumeMultiplier();

	public static float LiquidHeight { get; private set; } = -1000f;

	public static bool IsRendered { get; private set; }

	public static void UpdateLiquid()
	{
		if (GameManager.GameState == GameState.None)
		{
			IsRendered = false;
			return;
		}
		IsRendered = PlanetaryAtmosphereSimulation.LiquidVolume > RenderThreshold;
		LiquidHeight = (IsRendered ? Mathf.Lerp(2f, 10f, PlanetaryAtmosphereSimulation.LiquidVolumeRatio) : (-1000f));
	}

	public static bool IsUnderGlobalLiquid(Vector3 pos)
	{
		if (pos.y >= LiquidHeight)
		{
			return false;
		}
		return RoomController.World?.GetRoom(pos) == null;
	}

	public static float GetWorldGridLiquidVolumeRatio(WorldGrid wgrid)
	{
		Vector3 vector = wgrid.Value.ToVector3();
		float num = Mathf.Min(2f, LiquidHeight - (vector.y - 1f));
		if (num > 0f)
		{
			return num / 2f;
		}
		return 0f;
	}

	public static void Clear()
	{
		LiquidHeight = -1000f;
	}
}
