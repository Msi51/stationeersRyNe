using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PortableSolar : PowerTool, ILightActivated, IDensePoolable
{
	public Vector3 CenterOffset = new Vector3(0f, 0.5f, 0f);

	public float SolarPowerMaximum = 100f;

	public bool IsDestroying;

	[ReadOnly]
	public Vector3 WorldUpVector;

	[ReadOnly]
	public float GenerationEfficiency;

	public override Vector3 CenterPosition => base.Position + Rotation * CenterOffset;

	public float PowerGenerated
	{
		get
		{
			if (!HasLight)
			{
				return 0f;
			}
			return GenerationEfficiency * SolarPowerMaximum * OrbitalSimulation.EarthSolarRatio;
		}
	}

	public float SolarVisibility
	{
		get
		{
			if (!HasLight)
			{
				return 0f;
			}
			return GenerationEfficiency * 100f;
		}
	}

	public override void Start()
	{
		base.Start();
		if (GameManager.RunSimulation && base.ParentSlot == null)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		WorldUpVector = Transform.up;
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (IsOpen && base.ParentSlot == null)
		{
			if (!HasLight)
			{
				GenerationEfficiency = 0f;
				return;
			}
			float value = Vector3.Dot(WorldUpVector, OrbitalSimulation.WorldSunVector);
			GenerationEfficiency = Mathf.Clamp(value, 0f, 1f);
		}
	}

	public override void OnDestroy()
	{
		IsDestroying = true;
		base.OnDestroy();
	}

	public override void OnPowerTick()
	{
		if (HasLight && (bool)base.Battery && !base.Battery.IsCharged && PowerGenerated > 0f)
		{
			base.Battery.PowerStored += PowerGenerated;
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (GameManager.RunSimulation && IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
	}

	public override void Recycle()
	{
		IsDestroying = true;
		base.Recycle();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (!(RootParent != this) && GameManager.RunSimulation && !IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	public string SolarInfo()
	{
		return string.Format("Generating {0}\nVisibility <color=yellow>{1:F0}%</color>", PowerGenerated.ToStringPrefix("W", "yellow"), SolarVisibility);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		result.State = SolarInfo();
		return result;
	}
}
