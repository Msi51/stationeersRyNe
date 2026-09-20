using System;
using UnityEngine;

namespace Assets.Scripts.Voxel;

[Serializable]
public struct Voxel
{
	public static float[] DensityArray;

	public byte Type;

	public byte Density;

	public Voxel(byte vid, byte density)
	{
		Type = vid;
		Density = density;
	}

	public Voxel(Voxel v)
	{
		Type = v.Type;
		Density = v.Density;
	}

	public bool ReleaseMineable()
	{
		if (GetDensityAsFloat() < 0.6f)
		{
			return Type > 1;
		}
		return false;
	}

	public float GetDensityAsFloat()
	{
		return DensityArray[Density];
	}

	public float SetDensityAsFloat(float density)
	{
		return (int)(Density = (byte)(density * 255f));
	}

	public Color GetColor()
	{
		if (Type <= 1 || Type >= byte.MaxValue)
		{
			return Color.white;
		}
		return Color.black * 0.8f;
	}

	public static bool operator ==(Voxel lhs, Voxel rhs)
	{
		if (lhs.Type == rhs.Type)
		{
			return lhs.Density == rhs.Density;
		}
		return false;
	}

	public static bool operator !=(Voxel lhs, Voxel rhs)
	{
		return !(lhs == rhs);
	}

	static Voxel()
	{
		DensityArray = new float[256]
		{
			0f,
			0.00390625f,
			1f / 128f,
			0.01171875f,
			1f / 64f,
			0.01953125f,
			3f / 128f,
			0.02734375f,
			1f / 32f,
			0.03515625f,
			5f / 128f,
			0.04296875f,
			3f / 64f,
			0.05078125f,
			7f / 128f,
			0.05859375f,
			0.0625f,
			0.06640625f,
			9f / 128f,
			0.07421875f,
			5f / 64f,
			0.08203125f,
			11f / 128f,
			0.08984375f,
			3f / 32f,
			0.09765625f,
			13f / 128f,
			0.10546875f,
			7f / 64f,
			0.11328125f,
			15f / 128f,
			0.12109375f,
			0.125f,
			0.12890625f,
			17f / 128f,
			0.13671875f,
			9f / 64f,
			0.14453125f,
			19f / 128f,
			0.15234375f,
			5f / 32f,
			0.16015625f,
			21f / 128f,
			0.16796875f,
			11f / 64f,
			0.17578125f,
			23f / 128f,
			0.18359375f,
			0.1875f,
			0.19140625f,
			25f / 128f,
			0.19921875f,
			13f / 64f,
			0.20703125f,
			27f / 128f,
			0.21484375f,
			7f / 32f,
			0.22265625f,
			29f / 128f,
			0.23046875f,
			15f / 64f,
			0.23828125f,
			31f / 128f,
			0.24609375f,
			0.25f,
			0.25390625f,
			33f / 128f,
			0.26171875f,
			17f / 64f,
			0.26953125f,
			35f / 128f,
			0.27734375f,
			9f / 32f,
			0.28515625f,
			37f / 128f,
			0.29296875f,
			19f / 64f,
			0.30078125f,
			39f / 128f,
			0.30859375f,
			0.3125f,
			0.31640625f,
			41f / 128f,
			0.32421875f,
			21f / 64f,
			0.33203125f,
			43f / 128f,
			0.33984375f,
			11f / 32f,
			0.34765625f,
			45f / 128f,
			0.35546875f,
			23f / 64f,
			0.36328125f,
			47f / 128f,
			0.37109375f,
			0.375f,
			0.37890625f,
			49f / 128f,
			0.38671875f,
			25f / 64f,
			0.39453125f,
			51f / 128f,
			0.40234375f,
			13f / 32f,
			0.41015625f,
			53f / 128f,
			0.41796875f,
			27f / 64f,
			0.42578125f,
			55f / 128f,
			0.43359375f,
			0.4375f,
			0.44140625f,
			57f / 128f,
			0.44921875f,
			29f / 64f,
			0.45703125f,
			59f / 128f,
			0.46484375f,
			15f / 32f,
			0.47265625f,
			61f / 128f,
			0.48046875f,
			31f / 64f,
			0.48828125f,
			63f / 128f,
			0.49609375f,
			0.5f,
			0.50390625f,
			65f / 128f,
			0.51171875f,
			33f / 64f,
			0.51953125f,
			67f / 128f,
			0.52734375f,
			17f / 32f,
			0.53515625f,
			69f / 128f,
			0.54296875f,
			35f / 64f,
			0.55078125f,
			71f / 128f,
			0.55859375f,
			0.5625f,
			0.56640625f,
			73f / 128f,
			0.57421875f,
			37f / 64f,
			0.58203125f,
			75f / 128f,
			0.58984375f,
			19f / 32f,
			0.59765625f,
			77f / 128f,
			0.60546875f,
			39f / 64f,
			0.61328125f,
			79f / 128f,
			0.62109375f,
			0.625f,
			0.62890625f,
			81f / 128f,
			0.63671875f,
			41f / 64f,
			0.64453125f,
			83f / 128f,
			0.65234375f,
			21f / 32f,
			0.66015625f,
			85f / 128f,
			0.66796875f,
			43f / 64f,
			0.67578125f,
			87f / 128f,
			0.68359375f,
			0.6875f,
			0.69140625f,
			89f / 128f,
			0.69921875f,
			45f / 64f,
			0.70703125f,
			91f / 128f,
			0.71484375f,
			23f / 32f,
			0.72265625f,
			93f / 128f,
			0.73046875f,
			47f / 64f,
			0.73828125f,
			95f / 128f,
			0.74609375f,
			0.75f,
			0.75390625f,
			97f / 128f,
			0.76171875f,
			49f / 64f,
			0.76953125f,
			99f / 128f,
			0.77734375f,
			25f / 32f,
			0.78515625f,
			101f / 128f,
			0.79296875f,
			51f / 64f,
			0.80078125f,
			103f / 128f,
			0.80859375f,
			0.8125f,
			0.81640625f,
			105f / 128f,
			0.82421875f,
			53f / 64f,
			0.83203125f,
			107f / 128f,
			0.83984375f,
			27f / 32f,
			0.84765625f,
			109f / 128f,
			0.85546875f,
			55f / 64f,
			0.86328125f,
			111f / 128f,
			0.87109375f,
			0.875f,
			0.87890625f,
			113f / 128f,
			0.88671875f,
			57f / 64f,
			0.89453125f,
			115f / 128f,
			0.90234375f,
			29f / 32f,
			0.91015625f,
			117f / 128f,
			0.91796875f,
			59f / 64f,
			0.92578125f,
			119f / 128f,
			0.93359375f,
			0.9375f,
			0.94140625f,
			121f / 128f,
			0.94921875f,
			61f / 64f,
			0.95703125f,
			123f / 128f,
			0.96484375f,
			31f / 32f,
			0.97265625f,
			125f / 128f,
			0.98046875f,
			63f / 64f,
			0.98828125f,
			127f / 128f,
			1f
		};
	}
}
