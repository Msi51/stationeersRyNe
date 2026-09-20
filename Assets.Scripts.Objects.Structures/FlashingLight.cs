using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class FlashingLight : WallLight
{
	[SerializeField]
	private Transform rotator;

	private const float DEGREES_ROTATION_PER_SECOND = 720f;

	protected override void ToggleLightsOn(bool on)
	{
		foreach (ThingLight light in Lights)
		{
			light.Light.enabled = on;
			if ((bool)lodFlare)
			{
				lodFlare.gameObject.SetActive(on);
			}
		}
		SetCustomColor(on);
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded && OnOff && Powered)
		{
			rotator.Rotate(Vector3.forward, 720f * GameManager.DeltaTime);
		}
	}
}
