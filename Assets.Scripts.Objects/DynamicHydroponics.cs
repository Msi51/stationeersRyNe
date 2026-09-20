using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;

namespace Assets.Scripts.Objects;

public class DynamicHydroponics : PortableAtmospherics, IGrower, IReferencable, IEvaluable, ILightActivated, IDensePoolable
{
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

	public float CurrentLightExposure
	{
		get
		{
			float num = 0f;
			if (IsLitByGrowLight)
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

	public GasCanister GasCanister => Slots[4].Occupant as GasCanister;

	public Atmosphere WaterAtmosphere => GasCanister?.InternalAtmosphere;

	public override void InitInternalAtmosphere()
	{
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		AtmosphericsManager.DeregisterFromMainThead(base.InternalAtmosphere);
		base.InternalAtmosphere = null;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is Plant plant)
		{
			Plant plant2 = plant;
			plant2.Planted(this);
			if (plant2.IsFertilized)
			{
				return;
			}
			if (Slot(plant2.ParentSlot.SlotIndex + 5).Occupant is Fertiliser fertiliser)
			{
				plant2.ApplyFertilizer(fertiliser);
				fertiliser.UseOneCycle();
			}
		}
		if (GameManager.RunSimulation && newChild is GasCanister)
		{
			OnServer.Interact(base.InteractOpen, 1);
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

	public bool SlotEmpty(int slot)
	{
		return Slots[slot].Occupant == null;
	}

	public Slot Slot(int slot)
	{
		return Slots[slot];
	}

	public Plant Plant(int slot)
	{
		return (Plant)Slot(slot).Occupant;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (GasCanister != null)
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, GasCanister.InternalAtmosphere, AtmosphereHelper.MatterState.Liquid);
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
			return result.Fail(GameStrings.SlotCanNotUseWith, selectedSlot.ToTooltip(), handSlot.ToTooltip());
		}
		if (!currentStack.IsStackFull)
		{
			return result.Fail(GameStrings.InventoryStackIsFull, inventoryStack.ToTooltip());
		}
		return result.Succeed();
	}

	protected override DelayedActionInstance _HandleSwitchSwapItems(Interaction interaction, Slot handSlot, Slot selectedSlot, int slot, DelayedActionInstance result, bool doAction = true)
	{
		return result.Fail();
	}

	public PlantToFertiliserSlotMapping PlantToFertiliserSlotMapping(InteractableType interactableType)
	{
		return interactableType switch
		{
			InteractableType.Slot1 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot1), GetSlot(InteractableType.Slot6)), 
			InteractableType.Slot2 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot2), GetSlot(InteractableType.Slot7)), 
			InteractableType.Slot3 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot3), GetSlot(InteractableType.Slot8)), 
			InteractableType.Slot4 => new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot4), GetSlot(InteractableType.Slot9)), 
			_ => throw new Exception("No slot mapping found"), 
		};
	}
}
