using UnityEngine;

namespace Assets.Scripts.Objects;

public static class CableCoils
{
	private static readonly int[] Hashes = new int[3]
	{
		Animator.StringToHash("ItemCableCoil"),
		Animator.StringToHash("ItemCableCoilHeavy"),
		Animator.StringToHash("ItemCableCoilSuperHeavy")
	};

	public static int[] ToArray()
	{
		return (int[])Hashes.Clone();
	}
}
