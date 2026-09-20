using Assets.Scripts;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.RoboticArm;

public static class RoboticArmActionHelper
{
	public static void DoContextualAction(Slot armSlot, Transform armTransform, RoboticArmDock dock)
	{
		SmallCell targetCell = GetTargetCell(armTransform);
		bool flag = armSlot.IsNotEmpty();
		if (targetCell == null)
		{
			if (flag)
			{
				OnServer.MoveToWorld(armSlot.Get());
			}
		}
		else if (flag)
		{
			DoArmOccupied(armSlot, targetCell);
		}
		else
		{
			DoArmEmpty(armSlot, targetCell, dock);
		}
	}

	private static void DoArmEmpty(Slot armSlot, SmallCell cell, RoboticArmDock dock)
	{
		Device device = cell.Device;
		if (device is ChuteExportBin chuteExportBin && device.IsOpen)
		{
			if (!chuteExportBin.TransportSlot.IsEmpty())
			{
				OnServer.MoveToSlot(chuteExportBin.TransportSlot.Get(), armSlot);
			}
		}
		else if (cell.Pipe is IHarvestable harvestable)
		{
			DoHarvestOrClear(harvestable, armSlot, dock);
		}
		else if (cell.Device is IHarvestable harvestable2)
		{
			DoHarvestOrClear(harvestable2, armSlot, dock);
		}
	}

	private static void DoArmOccupied(Slot armSlot, SmallCell cell)
	{
		Device device = cell.Device;
		if (device is ChuteBin chuteBin && device.IsOpen)
		{
			if (chuteBin.InputSlot.IsEmpty())
			{
				OnServer.MoveToSlot(armSlot.Get(), chuteBin.InputSlot);
			}
		}
		else if (cell.Pipe is IHarvestable harvestable)
		{
			DoPlantOrFertilize(armSlot.Get(), harvestable);
		}
		else if (cell.Device is IHarvestable harvestable2)
		{
			DoPlantOrFertilize(armSlot.Get(), harvestable2);
		}
	}

	private static void DoHarvestOrClear(IHarvestable harvestable, Slot armSlot, Thing harvester)
	{
		Fertiliser occupant2;
		if (harvestable.InputSlot.Contains<Plant>(out var occupant))
		{
			bool num = occupant.IsMature && occupant.HarvestQuantity > 0;
			bool flag = occupant.IsSeeding && (occupant.HarvestQuantity > 0 || occupant.SeedQuantity > 0);
			if (num || flag)
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

	private static void DoPlantOrFertilize(DynamicThing thing, IHarvestable harvestable)
	{
		if (thing is Plant plant)
		{
			if (!harvestable.InputSlot.IsNotEmpty())
			{
				TryPlant(plant, harvestable);
			}
		}
		else if (thing is Fertiliser fertiliser && !harvestable.FertilizerSlot.IsNotEmpty())
		{
			TryFertilize(fertiliser, harvestable);
		}
	}

	private static void TryPlant(Plant plant, IHarvestable harvestable)
	{
		GeneCollection genes = ((GameManager.RunSimulation && plant != null) ? GeneCollection.Copy(plant.Genes) : null);
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
		if (plant2 != null)
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

	public static SmallCell GetTargetCell(Transform armTransform)
	{
		return GetSmallCellBelow((armTransform.position - armTransform.up * 0.25f).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3(), -armTransform.up, 3);
	}

	public static SmallCell GetSmallCellBelow(Vector3 startPosition, Vector3 direction, int smallCellCount)
	{
		Vector3 worldPosition = startPosition + direction.normalized * ((float)smallCellCount * 0.5f);
		Grid3 localGrid = new Grid3(worldPosition);
		return GridController.World.GetSmallCell(localGrid);
	}
}
