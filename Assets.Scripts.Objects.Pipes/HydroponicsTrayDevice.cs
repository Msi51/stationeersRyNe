using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Audio;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class HydroponicsTrayDevice : DeviceMixAtmosphere, ISmartRotatable, IGrower, IReferencable, IEvaluable, IHarvestable, IAudioParent, IThermal
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[Header("Hydroponic Tray")]
	[ReadOnly]
	public Plant Plant;

	public new virtual Transform SoundPosition => InputSlot.Location;

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

	public Plant GetPlant => Plant;

	public bool SlotEmpty => Slots[0].Occupant == null;

	public bool Slot1Empty => Slots[1].Occupant == null;

	public Slot InputSlot => Slots[0];

	public Slot InputSlot1 => Slots[1];

	public Slot FertilizerSlot => InputSlot1;

	public Atmosphere BreathingAtmosphere => null;

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

	public Atmosphere WaterAtmosphere => base.InternalAtmosphere;

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = permutation;
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocal(base.ThingTransformPosition + ThingTransform.up * 0.5f);
	}

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		if ((object)Plant != null && !Plant.IsBeingDestroyed)
		{
			Plant.SetOcclusion();
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

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!Singleton<GameManager>.IsQuitting && GameManager.RunSimulation)
		{
			_ = GameManager.GameState;
			_ = 3;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Slot1)
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
			Plant = plant;
			Plant.Planted(this);
			if (!Plant.IsFertilized && !Slot1Empty && InputSlot1.Occupant is Fertiliser fertiliser)
			{
				Plant.ApplyFertilizer(fertiliser);
				fertiliser.UseOneCycle();
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
			return slotId == InputSlot.SlotIndex;
		default:
			return base.CanLogicRead(logicSlotType, slotId);
		}
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (slotId != InputSlot.SlotIndex)
		{
			return base.GetLogicValue(logicSlotType, slotId);
		}
		switch (logicSlotType)
		{
		case LogicSlotType.Growth:
			return Plant ? ((float)Plant.Stage) : (-1f);
		case LogicSlotType.Health:
			return Plant ? Plant.DamageState.TotalRatioClampedUndamaged : (-1f);
		case LogicSlotType.Efficiency:
			return Plant ? Plant.lifeRequirements.GrowthEfficiency() : (-1f);
		case LogicSlotType.Mature:
			return (!Plant) ? (-1f) : (Plant.IsMature ? 1f : 0f);
		case LogicSlotType.Seeding:
			if (!Plant || !Plant.IsSeeding)
			{
				return -1.0;
			}
			if (Plant.IsSeeding && Plant.SeedQuantity == 0)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.HarvestedHash:
			if (!Plant)
			{
				return 0.0;
			}
			if (Plant.IsSeeding)
			{
				return Plant.SeedObject.PrefabHash;
			}
			if (Plant.IsMature)
			{
				return Plant.FruitObject.PrefabHash;
			}
			return 0.0;
		case LogicSlotType.MaturityRatio:
			if (!Plant)
			{
				return -1.0;
			}
			return Plant.MaturityRatio;
		case LogicSlotType.SeedingRatio:
			if (!Plant)
			{
				return -1.0;
			}
			return Plant.SeedingRatio;
		default:
			return base.GetLogicValue(logicSlotType, slotId);
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
