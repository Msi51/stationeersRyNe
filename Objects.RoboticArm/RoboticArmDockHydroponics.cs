using Assets.Scripts;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmDockHydroponics : RoboticArmDock, IProxySlot
{
	private FaceState _currentFaceState;

	[SerializeField]
	private MaterialSetChanger _materialSetChanger;

	public const int PROXY_SLOT_ID = 255;

	private Slot HandSlot => Slots[0];

	private Slot ExtraSlot => Slots[1];

	private SmallGrid TargetSmallGrid { get; set; }

	public IHarvestable TargetHarvestable => TargetSmallGrid as IHarvestable;

	public ILogicable TargetLogicable => TargetSmallGrid as ILogicable;

	private int TargetSlotIndex
	{
		get
		{
			if (TargetLogicable is ChuteBin chuteBin)
			{
				return chuteBin.InputSlot.SlotIndex;
			}
			if (TargetLogicable is ChuteExportBin chuteExportBin)
			{
				return chuteExportBin.TransportSlot.SlotIndex;
			}
			if (TargetHarvestable != null)
			{
				return TargetHarvestable.InputSlot.SlotIndex;
			}
			return -1;
		}
	}

	private void UpdateFaceStateFromInteractionCell(SmallCell cell)
	{
		_currentFaceState = FaceStateForPlant(FindCellPlant(cell));
	}

	private Plant FindCellPlant(SmallCell cell)
	{
		if (cell == null)
		{
			return null;
		}
		Plant occupant2;
		if (cell.Pipe is IHarvestable harvestable)
		{
			if (harvestable.InputSlot.Contains<Plant>(out var occupant))
			{
				return occupant;
			}
		}
		else if (cell.Device is IHarvestable harvestable2 && harvestable2.InputSlot.Contains<Plant>(out occupant2))
		{
			return occupant2;
		}
		return null;
	}

	private FaceState FaceStateForPlant(Plant plant)
	{
		if ((bool)plant && OnOff && Powered)
		{
			if (!plant.IsDead)
			{
				if (plant.PlantStatus.CanHeal(plant))
				{
					if (!plant.PlantStatus.GetCurrentState(PlantStatusType.UnDesiredGas))
					{
						if (!plant.PlantStatus.GetCurrentState(PlantStatusType.Dehydrated))
						{
							return FaceState.Happy;
						}
						return FaceState.UnHappy;
					}
					return FaceState.UnHappy;
				}
				return FaceState.UnHappy;
			}
			return FaceState.Dead;
		}
		return FaceState.Idle;
	}

	private void RefreshFaceDisplay()
	{
		if (!GameManager.IsBatchMode)
		{
			int currentFaceState = (int)_currentFaceState;
			if (_materialSetChanger.TargetSetIndex != currentFaceState)
			{
				_materialSetChanger.TargetSetIndex = currentFaceState;
				RefreshAnimState(skipAnimation: true);
			}
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		RefreshSlotStack();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmDockHydroponicsSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)_currentFaceState);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_currentFaceState = (FaceState)reader.ReadByte();
		RefreshFaceDisplay();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteByte((byte)_currentFaceState);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			_currentFaceState = (FaceState)reader.ReadByte();
			RefreshFaceDisplay();
		}
	}

	protected override void SetTargetSmallGrid()
	{
		if (base.CurrentBypass != null)
		{
			TargetSmallGrid = null;
			UpdateFaceStateFromInteractionCell(null);
		}
		else
		{
			SmallCell armInteractionCell = GetArmInteractionCell();
			TargetSmallGrid = armInteractionCell?.Device;
			if (!TargetSmallGrid && (bool)armInteractionCell?.Pipe && armInteractionCell.Pipe is HydroponicTray targetSmallGrid)
			{
				TargetSmallGrid = targetSmallGrid;
			}
			UpdateFaceStateFromInteractionCell(armInteractionCell);
		}
		if (!GameManager.IsBatchMode)
		{
			RefreshFaceDisplay();
		}
	}

	public override int GetNextSlotId(int slotIndex, bool isForward)
	{
		if (Slots == null)
		{
			return -1;
		}
		if (Slots.Count == 0)
		{
			return 0;
		}
		int num = slotIndex;
		if (num == 255)
		{
			if (!isForward)
			{
				return Slots.Count - 1;
			}
			return 0;
		}
		num += (isForward ? 1 : (-1));
		if (num < 0 || num >= Slots.Count)
		{
			num = 255;
		}
		return num;
	}

	public override Slot GetSlot(int slotIndex)
	{
		if (slotIndex == 255)
		{
			ILogicable targetLogicable = TargetLogicable;
			if (targetLogicable != null && targetLogicable.HasAnySlots)
			{
				return TargetLogicable.GetSlot(TargetSlotIndex);
			}
			IHarvestable targetHarvestable = TargetHarvestable;
			if (targetHarvestable != null && targetHarvestable.HasAnySlots)
			{
				return TargetHarvestable.GetSlot(TargetSlotIndex);
			}
		}
		return base.GetSlot(slotIndex);
	}

	protected override void AnimateDownFinished()
	{
		if (GameManager.RunSimulation)
		{
			DoContextualAction();
		}
	}

	private async UniTaskVoid WaitThenSetActivate()
	{
		await UniTask.Delay(200);
		OnServer.Interact(base.InteractActivate, 1);
	}

	private void DoContextualAction()
	{
		SmallCell armInteractionCell = GetArmInteractionCell();
		if (armInteractionCell == null)
		{
			WaitThenSetActivate().Forget();
			return;
		}
		if (HandSlot.Contains<DynamicThing>(out var occupant))
		{
			DoHandOccupied(armInteractionCell, occupant);
		}
		else
		{
			DoHandEmpty(armInteractionCell);
		}
		RefreshSlotStack();
		WaitThenSetActivate().Forget();
	}

	private void DoHandOccupied(SmallCell cell, DynamicThing inHand)
	{
		Device device = cell.Device;
		if (device is ChuteBin chuteBin && device.IsOpen)
		{
			if (chuteBin.InputSlot.IsEmpty())
			{
				OnServer.MoveToSlot(inHand, chuteBin.InputSlot);
			}
			return;
		}
		device = cell.Device;
		if (device is ChuteExportBin chuteExportBin && device.IsOpen)
		{
			if (!chuteExportBin.TransportSlot.IsEmpty())
			{
				TryMoveToHand(chuteExportBin.TransportSlot, inHand);
			}
		}
		else if (cell.Pipe is IHarvestable harvestable)
		{
			DoHarvestable(harvestable, inHand);
		}
		else if (cell.Device is IHarvestable harvestable2)
		{
			DoHarvestable(harvestable2, inHand);
		}
	}

	private void TryMoveToHand(Slot fromSlot, DynamicThing inHand)
	{
		DynamicThing dynamicThing = fromSlot.Get();
		Stackable stackable = dynamicThing as Stackable;
		Stackable stackable2 = inHand as Stackable;
		if (dynamicThing.PrefabHash == inHand.PrefabHash && (bool)stackable && (bool)stackable2 && !stackable2.IsStackFull)
		{
			OnServer.Merge(stackable2, stackable);
		}
		else if (!ExtraSlot.IsNotEmpty())
		{
			OnServer.MoveToSlot(inHand, ExtraSlot);
			OnServer.MoveToSlot(dynamicThing, HandSlot);
		}
	}

	private void DoHarvestable(IHarvestable harvestable, DynamicThing inHand)
	{
		Stackable stackable = inHand as Stackable;
		Plant plant = inHand as Plant;
		Fertiliser fertiliser = inHand as Fertiliser;
		Plant occupant;
		Thing thing;
		if (harvestable.InputSlot.IsEmpty() && (bool)plant)
		{
			TryPlant(plant, harvestable);
		}
		else if (harvestable.FertilizerSlot.IsEmpty() && (bool)fertiliser)
		{
			TryFertilize(fertiliser, harvestable);
		}
		else if (harvestable.InputSlot.Contains<Plant>(out occupant) && occupant.WillHarvest(out thing))
		{
			if ((bool)stackable && !stackable.IsStackFull && thing.PrefabHash == stackable.PrefabHash)
			{
				DoHarvest(harvestable, occupant);
			}
			else if (!ExtraSlot.IsNotEmpty())
			{
				OnServer.MoveToSlot(inHand, ExtraSlot);
				DoHarvest(harvestable, occupant);
			}
		}
	}

	private void DoHarvest(IHarvestable harvestable, Plant plant)
	{
		AudioEvent.Create(harvestable as IReferencable, Defines.Sounds.HarvestPlant);
		plant.Harvest(this, HandSlot, plant.IsSeeding && plant.SeedQuantity > 0);
	}

	private void DoHandEmpty(SmallCell cell)
	{
		Device device = cell.Device;
		if (device is ChuteExportBin chuteExportBin && device.IsOpen)
		{
			if (!chuteExportBin.TransportSlot.IsEmpty())
			{
				OnServer.MoveToSlot(chuteExportBin.TransportSlot.Get(), HandSlot);
			}
		}
		else if (cell.Pipe is IHarvestable harvestable)
		{
			DoHarvestOrClear(harvestable, HandSlot, this);
		}
		else if (cell.Device is IHarvestable harvestable2)
		{
			DoHarvestOrClear(harvestable2, HandSlot, this);
		}
	}

	private static void TryPlant(Plant plant, IHarvestable harvestable)
	{
		GeneCollection genes = ((GameManager.RunSimulation && (bool)plant) ? GeneCollection.Copy(plant.Genes) : null);
		Plant plant2 = null;
		if (plant.OnUseItem(1f, null))
		{
			plant2 = ((!(plant is Seed seed)) ? OnServer.Create<Plant>(plant.SourcePrefab, harvestable.InputSlot) : OnServer.Create<Plant>(seed.PlantType, harvestable.InputSlot));
			if ((bool)plant)
			{
				plant2.ApplySeedTraits(genes);
			}
			AudioEvent.Create(plant2, Defines.Sounds.PlantingFinished);
		}
		if ((bool)plant2)
		{
			plant2.PlanterName = (harvestable as IGrower)?.CustomName;
			OnServer.MoveToSlot(plant2, harvestable.InputSlot);
			AudioEvent.Create(plant2, Defines.Sounds.PlantingFinished);
		}
	}

	private static void TryFertilize(Fertiliser fertiliser, IHarvestable harvestable)
	{
		if (fertiliser.OnUseItem(1f, null))
		{
			Fertiliser newFertiliser = OnServer.Create<Fertiliser>(fertiliser, harvestable.FertilizerSlot);
			fertiliser.CopyStats(newFertiliser);
			AudioEvent.Create(harvestable as IReferencable, Defines.Sounds.PlantingFinished);
		}
	}

	private static void DoHarvestOrClear(IHarvestable harvestable, Slot armSlot, Thing harvester)
	{
		Fertiliser occupant2;
		if (harvestable.InputSlot.Contains<Plant>(out var occupant))
		{
			if (occupant.WillHarvest(out var _))
			{
				AudioEvent.Create(harvestable as IReferencable, Defines.Sounds.HarvestPlant);
				occupant.Harvest(harvester, armSlot, occupant.IsSeeding && occupant.SeedQuantity > 0);
			}
			else
			{
				OnServer.Destroy(occupant);
				AudioEvent.Create(harvestable as IReferencable, Defines.Sounds.HarvestPlant);
			}
		}
		else if (harvestable.FertilizerSlot.Contains<Fertiliser>(out occupant2))
		{
			OnServer.Destroy(occupant2);
			AudioEvent.Create(harvestable as IReferencable, Defines.Sounds.PlantingFinished);
		}
	}

	private void RefreshSlotStack()
	{
		if (HandSlot.IsEmpty() && ExtraSlot.IsNotEmpty())
		{
			OnServer.MoveToSlot(ExtraSlot.Get(), HandSlot);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TargetSlotIndex => true, 
			LogicType.TargetPrefabHash => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TargetSlotIndex => TargetSlotIndex, 
			LogicType.TargetPrefabHash => ((double?)TargetSmallGrid?.GetPrefabHash()) ?? 0.0, 
			_ => base.GetLogicValue(logicType), 
		};
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
			if (slotId == 255)
			{
				return TargetSlotIndex == 0;
			}
			return false;
		default:
			return base.CanLogicRead(logicSlotType, slotId);
		}
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		Slot slot = GetSlot(slotId);
		if (slot != null && slot.Contains<Plant>(out var occupant))
		{
			switch (logicSlotType)
			{
			case LogicSlotType.Growth:
				return occupant ? ((float)occupant.Stage) : (-1f);
			case LogicSlotType.Health:
				return occupant ? occupant.DamageState.TotalRatioClampedUndamaged : (-1f);
			case LogicSlotType.Efficiency:
				return occupant ? occupant.lifeRequirements.GrowthEfficiency() : (-1f);
			case LogicSlotType.Mature:
				return (!occupant) ? (-1f) : (occupant.IsMature ? 1f : 0f);
			case LogicSlotType.MaturityRatio:
				if (!occupant)
				{
					return -1.0;
				}
				return occupant.MaturityRatio;
			case LogicSlotType.SeedingRatio:
				if (!occupant)
				{
					return -1.0;
				}
				return occupant.SeedingRatio;
			case LogicSlotType.Seeding:
				if (!occupant || !occupant.IsSeeding)
				{
					return -1.0;
				}
				if (occupant.IsSeeding && occupant.SeedQuantity == 0)
				{
					return 0.0;
				}
				return 1.0;
			case LogicSlotType.HarvestedHash:
				if (!occupant)
				{
					return 0.0;
				}
				if (occupant.IsSeeding)
				{
					return occupant.SeedObject.PrefabHash;
				}
				if (occupant.IsMature)
				{
					return occupant.FruitObject.PrefabHash;
				}
				return 0.0;
			}
		}
		return base.GetLogicValue(logicSlotType, slotId);
	}
}
