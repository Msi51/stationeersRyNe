using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Piping : Pipe, IGridMergeable, ISmartRotatable
{
	public enum Type
	{
		normal,
		Insulated,
		NormalLowVolume,
		InsulatedLowVolume,
		Duct
	}

	public bool DontAllowMergingWithWrench;

	[SerializeField]
	private Vector3 _burstEffectLocalPositionOffset;

	protected override Vector3 BurstEffectLocalPositionOffset => _burstEffectLocalPositionOffset;

	protected override bool _IsCollision(SmallGrid smallGrid)
	{
		if (smallGrid is Piping piping)
		{
			if (piping.PipeType == PipeType)
			{
				return piping.PipeContentType != base.PipeContentType;
			}
			return true;
		}
		return base._IsCollision(smallGrid);
	}

	public CanConstructInfo CanReplace(MultiConstructor constructor, Item inactiveHandItem)
	{
		if (base.Indestructable)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeable.AsString(ToTooltip()));
		}
		if (!(constructor is MultiMergeConstructor multiMergeConstructor))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeable.AsString(ToTooltip()));
		}
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell == null || (object)smallCell.Pipe == null)
			{
				continue;
			}
			if (DontAllowMergingWithWrench || smallCell.Pipe is Piping { DontAllowMergingWithWrench: not false })
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeable.AsString(ToTooltip()));
			}
			if ((object)inactiveHandItem == null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.MergeRequiresTool.AsString(multiMergeConstructor.ToolExit.DisplayName));
			}
			if (multiMergeConstructor.ToolExit.PrefabHash == inactiveHandItem.PrefabHash || ((object)inactiveHandItem.ReplacementOf != null && multiMergeConstructor.ToolExit.PrefabHash == inactiveHandItem.ReplacementOf.PrefabHash))
			{
				if (smallCell.Pipe.PipeType != PipeType || smallCell.Pipe.PipeContentType != base.PipeContentType)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeableOfDifferentType.AsString(smallCell.Pipe.DisplayName));
				}
				return CanConstructInfo.ValidPlacement;
			}
			return CanConstructInfo.InvalidPlacement(GameStrings.MergeRequiresTool.AsString(multiMergeConstructor.ToolExit.DisplayName));
		}
		return CanConstructInfo.ValidPlacement;
	}

	public bool WillMergeWhenPlaced()
	{
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null && (object)smallCell.Pipe != null)
			{
				if (smallCell.Pipe.PipeType == PipeType)
				{
					return smallCell.Pipe.PipeContentType == base.PipeContentType;
				}
				return false;
			}
		}
		return false;
	}
}
