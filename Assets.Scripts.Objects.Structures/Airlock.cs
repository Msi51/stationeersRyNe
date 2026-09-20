using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects.Structures;

public class Airlock : Door
{
	public bool CheckSidesBlocked = true;

	public override WreckageSize WreckageSize => WreckageSize.None;

	public override int WreckageQuantity => 0;

	public virtual CanConstructInfo CanConstructCheckWalls()
	{
		if ((object)IsSideBlocked(allStructual: true) != null)
		{
			return CanConstructInfo.ValidPlacement;
		}
		Structure structure = IsSideBlocked(ThingTransform.right);
		if ((object)structure != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(structure.DisplayName));
		}
		Structure structure2 = IsSideBlocked(-ThingTransform.right);
		if ((object)structure2 != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(structure2.DisplayName));
		}
		return CanConstructInfo.ValidPlacement;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (CheckSidesBlocked)
		{
			CanConstructInfo result = CanConstructCheckWalls();
			if (!result.CanConstruct)
			{
				return result;
			}
		}
		return base.CanConstruct();
	}
}
