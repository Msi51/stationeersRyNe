using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class DeviceCableMounted : SmallDevice, ISmartRotatable, ICableMounted, IMounted
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public CableNetwork RegisteredNetwork;

	public CableNetwork CableNetwork
	{
		get
		{
			if (!base.SmallCell.Cable)
			{
				return null;
			}
			return base.SmallCell.Cable.CableNetwork;
		}
	}

	public bool IsValidCable()
	{
		if ((bool)base.SmallCell.Cable)
		{
			return IsSameOrientation(base.SmallCell.Cable);
		}
		return false;
	}

	public override CanConstructInfo CanConstruct()
	{
		SmallCell smallCell = base.GridController.GetSmallCell(base.ThingTransformPosition);
		if (smallCell != null && smallCell.Cable != null)
		{
			if (smallCell.Cable.IsBroken)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.CannotPlaceOnBrokenCable.DisplayString);
			}
			if (!smallCell.Cable.IsStraight || !IsSameOrientation(smallCell.Cable))
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(smallCell.Cable.DisplayName));
			}
			return CanConstructInfo.ValidPlacement;
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.MustMountToCable.DisplayString);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
	}

	public virtual void CheckForCable()
	{
		if (!IsValidCable())
		{
			RemoveFromNetwork();
		}
		else if (IsValidCable())
		{
			AddToNetwork();
		}
	}

	public virtual void AddToNetwork()
	{
		if (RegisteredNetwork != CableNetwork)
		{
			RemoveFromNetwork();
		}
		if (CableNetwork != null)
		{
			RegisteredNetwork = CableNetwork;
		}
	}

	public virtual void RemoveFromNetwork()
	{
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		RemoveFromNetwork();
	}

	public override void OnGridUpdated(SmallGrid updatingOccupant)
	{
		base.OnGridUpdated(updatingOccupant);
		CheckForCable();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		CheckForCable();
	}

	public override void OnGridPlaced(SmallGrid newOccupant)
	{
		base.OnGridPlaced(newOccupant);
		CheckForCable();
	}

	public override void OnGridRemoved(SmallGrid newOccupant)
	{
		base.OnGridRemoved(newOccupant);
		CheckForCable();
	}

	public void Mount(Grid3 grid)
	{
		Cable cable = base.GridController.GetCable(grid);
		if ((object)cable != null && !RocketMath.Approximately(cable.ThingTransform.forward, ThingTransform.forward, 1f) && !RocketMath.Approximately(-cable.ThingTransform.forward, ThingTransform.forward, 1f))
		{
			if (Vector3.Distance(cable.ThingTransform.forward, ThingTransform.forward) < Vector3.Distance(-cable.ThingTransform.forward, ThingTransform.forward))
			{
				ThingTransform.forward = cable.ThingTransform.forward;
			}
			else
			{
				ThingTransform.forward = -cable.ThingTransform.forward;
			}
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
