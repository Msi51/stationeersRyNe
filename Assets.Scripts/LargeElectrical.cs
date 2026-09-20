using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public abstract class LargeElectrical : Electrical
{
	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override int WreckageQuantity => 2;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.ThingTransformPosition - ThingTransform.up * GridSize;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
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
			if (!allStructure)
			{
				continue;
			}
			switch (allStructure.StructureCollisionType)
			{
			case CollisionType.BlockGrid:
				return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(allStructure.DisplayName));
			case CollisionType.BlockFace:
			case CollisionType.BlockCustom:
				if (Vector3.Distance(allStructure.ThingTransformPosition, position) < (Bounds.extents * 0.9f).magnitude)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(allStructure.DisplayName));
				}
				break;
			}
		}
		return CanConstructInfo.ValidPlacement;
	}
}
