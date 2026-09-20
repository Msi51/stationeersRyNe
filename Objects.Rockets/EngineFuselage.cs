using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Networks;
using Objects.Electrical;

namespace Objects.Rockets;

public class EngineFuselage : StructureFuselage
{
	public RocketSize RocketSize;

	public override float MassContribution => 100f;

	public override bool CanMerge()
	{
		WorldGrid worldGrid = new WorldGrid(base.ThingTransformPosition);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if (structure is EngineFuselage && structure.PrefabHash != PrefabHash)
		{
			return true;
		}
		return false;
	}

	public override CanConstructInfo CanConstruct()
	{
		Grid3 grid = new Grid3(base.ThingTransformPosition);
		if (CanMerge())
		{
			Item item = InventoryManager.Parent.Slots[InventoryManager.Instance.InactiveHand.SlotId].Occupant as Item;
			Item toolExit = BuildStates[0].Tool.ToolExit;
			if ((object)item == null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.MergeRequiresTool.AsString(toolExit.DisplayName));
			}
			if (toolExit.PrefabHash == item.PrefabHash || ((object)item.ReplacementOf != null && toolExit.PrefabHash == item.ReplacementOf.PrefabHash))
			{
				return CanConstructInfo.ValidPlacement;
			}
		}
		WorldGrid worldGrid = new WorldGrid(grid + Grid3.Down);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if (!(structure is ISupportsRocketConstruction) || !structure.IsStructureCompleted)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.FuselageRequiresLaunchMount.DisplayString);
		}
		Span<WorldGrid> span = stackalloc WorldGrid[8]
		{
			new WorldGrid(grid + Grid3.North),
			new WorldGrid(grid + Grid3.South),
			new WorldGrid(grid + Grid3.East),
			new WorldGrid(grid + Grid3.West),
			new WorldGrid(grid + Grid3.North + Grid3.East),
			new WorldGrid(grid + Grid3.South + Grid3.East),
			new WorldGrid(grid + Grid3.North + Grid3.West),
			new WorldGrid(grid + Grid3.South + Grid3.West)
		};
		for (int i = 0; i < span.Length; i++)
		{
			WorldGrid worldGrid2 = span[i];
			if (base.GridController.Get<Structure>(worldGrid2) is INetworkedRocketPart)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.RocketAdjacentFail.DisplayString);
			}
		}
		return base.CanConstruct();
	}
}
