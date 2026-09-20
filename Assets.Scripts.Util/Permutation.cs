namespace Assets.Scripts.Util;

public class Permutation
{
	private int[][] _Permutation;

	public Permutation(int[][] permutation)
	{
		_Permutation = (int[][])permutation.Clone();
	}

	public int[] Permute(int[] array)
	{
		int[][] permutation = _Permutation;
		foreach (int[] array2 in permutation)
		{
			int num = array[array2[0]];
			for (int j = 0; j < array2.Length - 1; j++)
			{
				array[array2[j]] = array[array2[j + 1]];
			}
			array[array2[^1]] = num;
		}
		return array;
	}

	public int[] InversePermute(int[] array)
	{
		int[][] permutation = _Permutation;
		foreach (int[] array2 in permutation)
		{
			int num = array[array2[^1]];
			for (int num2 = array2.Length - 1; num2 > 0; num2--)
			{
				array[array2[num2]] = array[array2[num2 - 1]];
			}
			array[array2[0]] = num;
		}
		return array;
	}
}
