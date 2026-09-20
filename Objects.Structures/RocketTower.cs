using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Structures;

public class RocketTower : Frame, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public override CanConstructInfo CanConstruct()
	{
		Cell cell = base.GridController.GetCell(GetLocalGrid());
		WorldGrid worldGrid = new WorldGrid(base.ThingTransformPosition - ThingTransform.up * GridSize);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if ((object)structure == null || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrameOrTower.DisplayString);
		}
		if (cell?.AllStructures == null)
		{
			return base.CanConstruct();
		}
		foreach (Structure allStructure in cell.AllStructures)
		{
			if (allStructure is RocketTower)
			{
				return new CanConstructInfo(canConstruct: false, GameStrings.LaunchTowerAlreadyExists.DisplayString);
			}
		}
		return base.CanConstruct();
	}

	public int ConnectedCount()
	{
		return 0;
	}

	public int GetOpenEndsCount()
	{
		return 0;
	}

	public float GetGridSize()
	{
		return GridSize;
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public List<Connection> GetOpenEnds()
	{
		return new List<Connection>();
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
