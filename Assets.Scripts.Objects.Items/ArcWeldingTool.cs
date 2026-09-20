using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ArcWeldingTool : PowerTool, IWelder, IConstructor, IUsed
{
	[Header("Arc Welder")]
	public ParticleSystem Sparks;

	public bool IsEmpty
	{
		get
		{
			if ((object)base.Battery != null)
			{
				return base.Battery.IsEmpty;
			}
			return true;
		}
	}

	public void SendStartWelding()
	{
		OnServer.WeldEffect(base.ReferenceId, CursorManager.CursorHit.point, isEnabled: true);
		SetSparks(CursorManager.CursorHit.point);
	}

	public void SendStopWelding()
	{
		OnServer.WeldEffect(base.ReferenceId, default(Vector3), isEnabled: false);
		ResetSparks();
	}

	public void SetSparks(Vector3 position)
	{
		Sparks.transform.parent = ThingTransform;
		Sparks.transform.localPosition = Vector3.zero;
		Sparks.gameObject.SetActive(value: true);
	}

	public void ResetSparks()
	{
		Sparks.transform.parent = null;
		Sparks.transform.position = position;
		Sparks.gameObject.SetActive(value: false);
	}

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		if (Activate != 1)
		{
			SendStartWelding();
			Thing.Interact(base.InteractActivate, 1);
		}
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		SendStopWelding();
		Thing.Interact(base.InteractActivate, 0);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (Activate == 1 && !(base.Battery == null))
		{
			base.Battery.PowerStored -= UsedPowerPassive;
		}
	}
}
