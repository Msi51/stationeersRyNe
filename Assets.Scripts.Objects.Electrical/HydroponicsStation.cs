using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class HydroponicsStation : DeviceInternal, IGrower, IReferencable, IEvaluable, ILightActivated, IDensePoolable
{
	public static readonly int PlantingPlantHash = Animator.StringToHash("PlantingPlant");

	public static readonly int PlantingFinishedHash = Animator.StringToHash("PlantingFinished");

	public static readonly int HarvestPlantActiveHash = Animator.StringToHash("HarvestPlantActive");

	public static readonly int HarvestPlantHash = Animator.StringToHash("HarvestPlant");

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

	public Atmosphere BreathingAtmosphere => null;

	public float CurrentLightExposure
	{
		get
		{
			float num = 0f;
			if (Powered)
			{
				num += 0.8f;
			}
			if (HasLight)
			{
				num += OrbitalSimulation.EarthSolarRatio;
			}
			return num;
		}
	}

	public Atmosphere WaterAtmosphere => base.InternalAtmosphere;

	public bool SlotEmpty(int slot)
	{
		return !Slots[slot].Occupant;
	}

	public Slot Slot(int slot)
	{
		return Slots[slot];
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public Plant Plant(int slot)
	{
		return (Plant)Slot(slot).Occupant;
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

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		switch (logicSlotType)
		{
		case LogicSlotType.Efficiency:
		case LogicSlotType.Health:
		case LogicSlotType.Growth:
		case LogicSlotType.Mature:
		case LogicSlotType.Seeding:
		case LogicSlotType.MaturityRatio:
		case LogicSlotType.SeedingRatio:
			return true;
		default:
			return base.CanLogicRead(logicSlotType, slotId);
		}
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		switch (logicSlotType)
		{
		case LogicSlotType.Growth:
			return Plant(slotId) ? ((float)Plant(slotId).Stage) : (-1f);
		case LogicSlotType.Health:
			return Plant(slotId) ? Plant(slotId).DamageState.TotalRatioClampedUndamaged : (-1f);
		case LogicSlotType.Efficiency:
			return Plant(slotId) ? Plant(slotId).lifeRequirements.GrowthEfficiency() : (-1f);
		case LogicSlotType.Mature:
			return (!Plant(slotId)) ? (-1f) : (Plant(slotId).IsMature ? 1f : 0f);
		case LogicSlotType.Seeding:
			return (!Plant(slotId)) ? (-1f) : (Plant(slotId).IsSeeding ? 1f : 0f);
		case LogicSlotType.MaturityRatio:
			if (!Plant(slotId))
			{
				return -1.0;
			}
			return Plant(slotId).MaturityRatio;
		case LogicSlotType.SeedingRatio:
			if (!Plant(slotId))
			{
				return -1.0;
			}
			return Plant(slotId).SeedingRatio;
		default:
			return base.GetLogicValue(logicSlotType, slotId);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Slot1 && interactable.Action != InteractableType.Slot2 && interactable.Action != InteractableType.Slot3 && interactable.Action != InteractableType.Slot4)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		return HydroponicsUtils.HandlePlantInteraction(this, interactable, interaction, doAction) ?? base.InteractWith(interactable, interaction, doAction);
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
			Plant plant2 = plant;
			plant2.Planted(this);
			if (!plant2.IsFertilized && Slot(plant2.ParentSlot.SlotIndex + 4).Occupant is Fertiliser fertiliser)
			{
				plant2.ApplyFertilizer(fertiliser);
				fertiliser.UseOneCycle();
			}
		}
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant != null)
			{
				slot.Occupant.PrintDebugInfo();
			}
		}
	}

	public PlantToFertiliserSlotMapping PlantToFertiliserSlotMapping(InteractableType interactableType)
	{
		return interactableType switch
		{
			InteractableType.Slot1 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot1), GetSlot(InteractableType.Slot5)), 
			InteractableType.Slot2 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot2), GetSlot(InteractableType.Slot6)), 
			InteractableType.Slot3 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot3), GetSlot(InteractableType.Slot7)), 
			InteractableType.Slot4 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot4), GetSlot(InteractableType.Slot8)), 
			_ => throw new Exception("No slot mapping found"), 
		};
	}
}
