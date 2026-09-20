using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public class CrewBulkhead : SmallGrid, IRocketInternals, IRocketComponent
{
	public RocketInternalCellType InternalCellType => RocketInternalCellType.Bulkhead;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override CanConstructInfo CanConstruct()
	{
		SmallCell smallCell = base.GridController.GetSmallCell(base.ThingTransformPosition);
		if (smallCell != null && smallCell.Other != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Other.DisplayName));
		}
		if (smallCell?.Owner is RocketNetwork rocketNetwork && !IsOnCentralColumn(rocketNetwork))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.BulkheadMustBeCentered.DisplayString);
		}
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell2 = base.GridController.GetSmallCell(localGrid);
			if (smallCell2 != null && smallCell2.Other != null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell2.Other.DisplayName));
			}
		}
		return base.CanConstruct();
	}

	private bool IsOnCentralColumn(RocketNetwork rocketNetwork)
	{
		foreach (INetworkedStructure structure in rocketNetwork.StructureList)
		{
			if (structure is INetworkedRocketPart networkedRocketPart)
			{
				Vector3 vector = networkedRocketPart.GetAsThing.Transform.position;
				Vector3 thingTransformPosition = base.ThingTransformPosition;
				thingTransformPosition.x = vector.x;
				thingTransformPosition.z = vector.z;
				Grid3 grid = GridController.World.WorldToLocalGrid(base.ThingTransformPosition, 0.5f, 0.25f);
				Grid3 grid2 = GridController.World.WorldToLocalGrid(thingTransformPosition, 0.5f, 0.25f);
				return grid.x == grid2.x && grid.z == grid2.z;
			}
		}
		return true;
	}
}
