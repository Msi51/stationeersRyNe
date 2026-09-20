using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Flag : SmallGrid, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (!(sourceItem is Labeller labeller))
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ActionStrings.Rename
		};
		if (!labeller.OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!labeller.IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		labeller.Rename(this);
		return delayedActionInstance;
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

	public new List<Connection> GetOpenEnds()
	{
		return null;
	}

	public new int ConnectedCount()
	{
		return 0;
	}

	public new int GetOpenEndsCount()
	{
		return 0;
	}

	public new float GetGridSize()
	{
		return 2f;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow() || HasVoxelBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrameOrGround.DisplayString);
	}
}
