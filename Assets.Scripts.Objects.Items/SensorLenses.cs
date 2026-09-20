using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SensorLenses : PowerTool
{
	[ReadOnly]
	public SensorProcessingUnit Sensor;

	public override bool IsBurnable
	{
		get
		{
			if (base.ParentSlot?.Parent is Human human)
			{
				GasMask gasMask = human.HelmetSlot.Get<GasMask>();
				if (gasMask != null && !gasMask.IsOpen)
				{
					return false;
				}
			}
			return base.IsBurnable;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && (object)base.Battery != null && (object)Sensor != null)
		{
			base.Battery.PowerStored -= Sensor.AdditionalPowerUsePerTick;
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			PlayPooledAudioSound(OnOff ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, Vector3.zero);
		}
	}

	public override void UpdateEachFrame()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		base.UpdateEachFrame();
		if (GameManager.GameState == GameState.Running && !(InventoryManager.ParentHuman == null) && !(InventoryManager.ParentHuman != RootParentHuman) && base.ParentSlot == InventoryManager.ParentHuman.GlassesSlot)
		{
			if (Sensor != null && IsOperable && base.InteractOnOff.State == 1)
			{
				CameraController.Instance.IsSensorLensesFxActive = true;
				Sensor.Render();
			}
			else
			{
				CameraController.Instance.IsSensorLensesFxActive = false;
			}
		}
	}

	public override void CheckPower()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (base.Battery != null && !base.Battery.IsEmpty && OnOff && (bool)Sensor)
		{
			if (!Powered)
			{
				OnServer.Interact(base.InteractPowered, 1);
			}
		}
		else if (Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
		_checkPower = false;
	}

	public override void OnChildEnterInventory(DynamicThing child)
	{
		base.OnChildEnterInventory(child);
		if (child is SensorProcessingUnit sensor)
		{
			Sensor = sensor;
			CheckPower();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild == Sensor)
		{
			Sensor = null;
			CheckPower();
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalEyeWear);
	}

	public override void OnExitInventory(Thing oldParent)
	{
		if (!GameManager.IsBatchMode && oldParent == InventoryManager.ParentHuman && base.ParentSlot != InventoryManager.ParentHuman.GlassesSlot)
		{
			CameraController.Instance.IsSensorLensesFxActive = false;
		}
		base.OnExitInventory(oldParent);
	}
}
