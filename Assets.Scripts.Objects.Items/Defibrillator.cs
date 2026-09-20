using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Defibrillator : PowerTool
{
	[SerializeField]
	private GameObject screen;

	public static readonly float TimeToUse = 1f;

	private static readonly int EquipTabletHash = Animator.StringToHash("EquipTablet");

	private static readonly int UnEquipTabletHash = Animator.StringToHash("UnEquipTablet");

	private const float DamageRemaining = 51f;

	public override bool IsOperable
	{
		get
		{
			if ((bool)base.Battery)
			{
				return base.Battery.PowerStored >= UsedPowerActive;
			}
			return false;
		}
	}

	public override int ConstructingSoundHash => Defines.Sounds.DisposableBatteryChargerHash;

	public override int FinishedConstructingSoundHash => Defines.Sounds.DisposableBatteryChargerFinishedHash;

	public override int EquipSoundHash => EquipTabletHash;

	public override int UnEquipSoundHash => UnEquipTabletHash;

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (!onUseThing)
		{
			return true;
		}
		Human human = onUseThing as Human;
		if (!human)
		{
			return true;
		}
		human.DamageState.HealAll(51f);
		if ((bool)human.OrganBrain)
		{
			human.OrganBrain.DamageState.HealAll(51f);
		}
		if ((bool)human.OrganLungs)
		{
			human.OrganLungs.DamageState.HealAll(51f);
		}
		if ((object)base.Battery == null || base.Battery.IsEmpty)
		{
			if (OnOff && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return false;
		}
		base.Battery.PowerStored -= UsedPowerActive;
		return true;
	}

	public virtual void CheckError()
	{
		if (GameManager.RunSimulation)
		{
			if ((!base.BatterySlot.Occupant || ((bool)base.BatterySlot.Occupant && base.Battery.PowerStored < UsedPowerActive)) && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if ((bool)base.BatterySlot.Occupant && base.Battery.PowerStored >= UsedPowerActive && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	protected void CheckScreen()
	{
		if (screen != null)
		{
			screen.SetActive(OnOff && Powered);
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		CheckError();
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		CheckScreen();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		CheckError();
		CheckScreen();
	}
}
