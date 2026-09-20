using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Audio;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class HydroponicTray : Pipe, IGrower, IReferencable, IEvaluable, IHarvestable, IAudioParent, ILightActivated, IDensePoolable
{
	[Header("Hydroponic Tray")]
	[ReadOnly]
	public Plant Plant;

	public Plant GetPlant => Plant;

	public new virtual Transform SoundPosition => InputSlot.Location;

	public bool Slot1Empty => InputSlot1.Occupant == null;

	public Slot InputSlot => Slots[0];

	public Slot InputSlot1 => Slots[1];

	public Slot FertilizerSlot => InputSlot1;

	public Atmosphere BreathingAtmosphere => null;

	public List<GrowLight> LinkedGrowLights { get; } = new List<GrowLight>();

	public virtual bool IsLitByGrowLight
	{
		get
		{
			foreach (GrowLight linkedGrowLight in LinkedGrowLights)
			{
				if (!(linkedGrowLight == null) && linkedGrowLight.OnOff && linkedGrowLight.Powered)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override Vector3 CenterPosition => base.Position + Up * GridSize * 0.2f;

	public float CurrentLightExposure
	{
		get
		{
			if (Plant != null && Plant.WorldAtmosphere != null)
			{
				float num = 0f;
				if (IsLitByGrowLight)
				{
					num += 0.8f;
				}
				if (Plant.WorldAtmosphere.HasLight)
				{
					num += OrbitalSimulation.EarthSolarRatio;
				}
				return num;
			}
			return 0f;
		}
	}

	public Atmosphere WaterAtmosphere => base.PipeNetwork?.Atmosphere;

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		if ((object)Plant != null && !Plant.IsBeingDestroyed)
		{
			Plant.SetOcclusion();
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocal(base.ThingTransformPosition + ThingTransform.up * 0.5f);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Slot1)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		return HydroponicsUtils.HandlePlantInteraction(this, interactable, interaction, doAction) ?? base.InteractWith(interactable, interaction, doAction);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (attack.SourceItem is Labeller labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	protected override DelayedActionInstance _HandleSwitchAddToStack(Slot handSlot, IMergeable inventoryStack, Slot selectedSlot, IMergeable currentStack, DelayedActionInstance result, bool doAction = true)
	{
		result.OverrideTitle = selectedSlot.Occupant.GetPassiveTooltip(null).Title;
		result.ActionMessage = ActionStrings.Collect;
		if (!selectedSlot.Occupant.CanEnter(handSlot) || !handSlot.Occupant.CanEnter(selectedSlot))
		{
			result.Fail(GameStrings.SlotCanNotUseWith, selectedSlot.ToTooltip(), handSlot.ToTooltip());
		}
		if (!currentStack.IsStackFull)
		{
			result.Fail(GameStrings.InventoryStackIsFull, inventoryStack.ToTooltip());
		}
		return result.Succeed();
	}

	protected override DelayedActionInstance _HandleSwitchSwapItems(Interaction interaction, Slot handSlot, Slot selectedSlot, int slot, DelayedActionInstance result, bool doAction = true)
	{
		return result.Fail();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is Plant plant)
		{
			Plant = plant;
			Plant.Planted(this);
			if (!Plant.IsFertilized && !Slot1Empty && InputSlot1.Occupant is Fertiliser fertiliser)
			{
				Plant.ApplyFertilizer(fertiliser);
				fertiliser.UseOneCycle();
			}
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant is Plant plant)
			{
				plant.PlanterName = CustomName;
			}
		}
	}

	public PlantToFertiliserSlotMapping PlantToFertiliserSlotMapping(InteractableType interactableType)
	{
		if (interactableType == InteractableType.Slot1)
		{
			return new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot1), GetSlot(InteractableType.Slot2));
		}
		throw new Exception("No slot mapping found");
	}
}
