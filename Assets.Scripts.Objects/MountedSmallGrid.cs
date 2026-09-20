using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class MountedSmallGrid : SmallGrid, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
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
