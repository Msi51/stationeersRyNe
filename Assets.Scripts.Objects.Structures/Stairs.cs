using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Stairs : Structure, ISmartRotatable
{
	public Transform EntryPoint;

	public Transform ExitPoint;

	private Grid3 _entryPosition;

	private Grid3 _exitPosition;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public bool RequiresFrame;

	public Grid3 Entry => _entryPosition;

	public Grid3 Exit => _exitPosition;

	public override CanConstructInfo CanConstruct()
	{
		if (RequiresFrame)
		{
			Vector3 vector = base.ThingTransformPosition - ThingTransform.up * GridSize;
			bool flag = false;
			Span<Grid3> span = stackalloc Grid3[GridBounds._grids.Length];
			GridBounds.GetLocalGrids(vector, base.ThingTransformRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 grid = span2[i];
				Structure structure = base.GridController.Get<Structure>(new WorldGrid(grid));
				if ((bool)structure && structure.AllowMounting)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
			}
		}
		return base.CanConstruct();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if ((bool)EntryPoint && (bool)ExitPoint)
		{
			_entryPosition = base.GridController.WorldToLocalGrid(EntryPoint.position);
			_exitPosition = base.GridController.WorldToLocalGrid(ExitPoint.position);
		}
	}

	public virtual bool IsLowCell(Cell cell)
	{
		if (Vector3.SqrMagnitude(Cell.Position - base.ThingTransformPosition) < 0.1f)
		{
			return true;
		}
		return false;
	}

	protected override CanConstructInfo CanConstructCell(Cell cell, Vector3 position)
	{
		if (cell == null)
		{
			return CanConstructInfo.ValidPlacement;
		}
		if (StructureCollisionType == CollisionType.BlockGrid)
		{
			Structure first = cell.GetFirst();
			if ((object)first != null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(first.DisplayName));
			}
		}
		foreach (Structure allStructure in cell.AllStructures)
		{
			switch (allStructure.StructureCollisionType)
			{
			case CollisionType.BlockGrid:
				return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(allStructure.DisplayName));
			case CollisionType.BlockFace:
			case CollisionType.BlockCustom:
				if (allStructure.IsStairs || Vector3.SqrMagnitude(allStructure.GetGridPosition() - GetGridPosition()) < 0.01f)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(allStructure.DisplayName));
				}
				break;
			}
		}
		return CanConstructInfo.ValidPlacement;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.SafetyCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.SafetyCategory;
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

	public List<Connection> GetOpenEnds()
	{
		return new List<Connection>(0);
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
		return 2f;
	}
}
