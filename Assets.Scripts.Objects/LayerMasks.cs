using UnityEngine;

namespace Assets.Scripts.Objects;

public static class LayerMasks
{
	public static LayerMask CursorVoxel;

	public static void Initialize()
	{
		CursorVoxel = 1 << LayerMask.NameToLayer("CursorVoxel");
	}
}
