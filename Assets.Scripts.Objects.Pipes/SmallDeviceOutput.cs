using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class SmallDeviceOutput : DeviceOutput
{
	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(25f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
