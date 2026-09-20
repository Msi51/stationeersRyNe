using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Igniter : SmallDevice, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public ParticleSystem Sparks;

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (OnOff)
		{
			Sparks.Emit(20);
			Sparks.gravityModifier = ((base.GridController.RoomController.GetRoom(base.WorldGrid) == null) ? 0f : 0.3f);
			base.AtmosphericsController.IgniteAtmosphere(base.WorldGrid, new MoleEnergy(500.0));
			OnOff = false;
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
