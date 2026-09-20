using System;
using Assets.Scripts.Util;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

[Serializable]
public struct Grid3 : IEquatable<Grid3>, IComparable<Grid3>
{
	public static class Face
	{
		public static readonly Grid3 North = Directions[0] * 0.5f;

		public static readonly Grid3 East = Directions[3] * 0.5f;

		public static readonly Grid3 South = Directions[1] * 0.5f;

		public static readonly Grid3 West = Directions[2] * 0.5f;

		public static readonly Grid3 Up = Directions[4] * 0.5f;

		public static readonly Grid3 Down = Directions[5] * 0.5f;
	}

	public static class Edge
	{
		public static readonly Grid3 NorthEast = Face.North + Face.East;

		public static readonly Grid3 NorthWest = Face.North + Face.West;

		public static readonly Grid3 NorthUp = Face.North + Face.Up;

		public static readonly Grid3 NorthDown = Face.North + Face.Down;

		public static readonly Grid3 EastUp = Face.East + Face.Up;

		public static readonly Grid3 EastDown = Face.East + Face.Down;

		public static readonly Grid3 SouthEast = Face.South + Face.East;

		public static readonly Grid3 SouthWest = Face.South + Face.West;

		public static readonly Grid3 SouthUp = Face.South + Face.Up;

		public static readonly Grid3 SouthDown = Face.South + Face.Down;

		public static readonly Grid3 WestUp = Face.West + Face.Up;

		public static readonly Grid3 WestDown = Face.West + Face.Down;
	}

	public static class Corner
	{
		public static readonly Grid3 NorthEastUp = Face.North + Face.East + Face.Up;

		public static readonly Grid3 NorthWestUp = Face.North + Face.West + Face.Up;

		public static readonly Grid3 SouthEastUp = Face.South + Face.East + Face.Up;

		public static readonly Grid3 SouthWestUp = Face.South + Face.West + Face.Up;

		public static readonly Grid3 NorthEastDown = Face.North + Face.East + Face.Down;

		public static readonly Grid3 NorthWestDown = Face.North + Face.West + Face.Down;

		public static readonly Grid3 SouthEastDown = Face.South + Face.East + Face.Down;

		public static readonly Grid3 SouthWestDown = Face.South + Face.West + Face.Down;
	}

	public static class GridVoxel
	{
		public static readonly Grid3 NorthWestUp = Corner.NorthWestUp * 0.5f;

		public static readonly Grid3 NorthEastUp = Corner.NorthEastUp * 0.5f;

		public static readonly Grid3 NorthWestDown = Corner.NorthWestDown * 0.5f;

		public static readonly Grid3 NorthEastDown = Corner.NorthEastDown * 0.5f;

		public static readonly Grid3 SouthWestUp = Corner.SouthWestUp * 0.5f;

		public static readonly Grid3 SouthEastUp = Corner.SouthEastUp * 0.5f;

		public static readonly Grid3 SouthWestDown = Corner.SouthWestDown * 0.5f;

		public static readonly Grid3 SouthEastDown = Corner.SouthEastDown * 0.5f;
	}

	public int x;

	public int y;

	public int z;

	public static readonly Grid3[] Directions;

	public static Grid3 North;

	public static Grid3 South;

	public static Grid3 West;

	public static Grid3 East;

	public static Grid3 Up;

	public static Grid3 Down;

	public static Grid3 zero => default(Grid3);

	public static Grid3 one => new Grid3(10, 10, 10);

	public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);

	public Grid3(Vector3 worldPosition)
	{
		x = Round(worldPosition.x * 10f);
		y = Round(worldPosition.y * 10f);
		z = Round(worldPosition.z * 10f);
	}

	public Grid3 GridCenter(Vector3 origin)
	{
		float num = (float)x / 10f;
		float num2 = (float)y / 10f;
		float num3 = (float)z / 10f;
		bool num4 = num <= origin.x;
		bool flag = num2 <= origin.y;
		bool flag2 = num3 <= origin.z;
		float num5 = 1f;
		float num6 = (num4 ? Mathf.Ceil((num - num5) / 2f) : Mathf.Floor((num - num5) / 2f)) * 2f + num5;
		float num7 = (flag ? Mathf.Ceil((num2 - num5) / 2f) : Mathf.Floor((num2 - num5) / 2f)) * 2f + num5;
		float num8 = (flag2 ? Mathf.Ceil((num3 - num5) / 2f) : Mathf.Floor((num3 - num5) / 2f)) * 2f + num5;
		return new Grid3(new Vector3(num6, num7, num8));
	}

	public Grid3(int nx, int ny, int nz)
	{
		x = nx;
		y = ny;
		z = nz;
	}

	public Grid3(int value)
	{
		x = value;
		y = value;
		z = value;
	}

	public Grid3(float nx, float ny, float nz)
	{
		x = Round(nx);
		y = Round(ny);
		z = Round(nz);
	}

	public Grid3(Vector3 worldPosition, float gridBy, float gridOffset)
	{
		worldPosition = worldPosition.GridCenter(gridBy, gridOffset);
		x = Round(worldPosition.x * 10f);
		y = Round(worldPosition.y * 10f);
		z = Round(worldPosition.z * 10f);
	}

	public Grid3 ToGridFace(Vector3 direction)
	{
		return this + (direction.ToCardinalDir() * 1f).ToGridPosition();
	}

	public static int Round(float value)
	{
		if (value >= 0f)
		{
			return (int)(value + 0.5f);
		}
		return (int)(value - 0.5f);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Grid3))
		{
			return false;
		}
		return Equals((Grid3)obj);
	}

	public bool Equals(Grid3 other)
	{
		if (x.Equals(other.x) && y.Equals(other.y))
		{
			return z.Equals(other.z);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((0x50C5D1F ^ x.GetHashCode()) * 16777619) ^ y.GetHashCode()) * 16777619) ^ z.GetHashCode();
	}

	public static Grid3 operator *(Quaternion rotation, Grid3 point)
	{
		Vector3 vector = point.ToVector3();
		float num = rotation.x * 2f;
		float num2 = rotation.y * 2f;
		float num3 = rotation.z * 2f;
		float num4 = rotation.x * num;
		float num5 = rotation.y * num2;
		float num6 = rotation.z * num3;
		float num7 = rotation.x * num2;
		float num8 = rotation.x * num3;
		float num9 = rotation.y * num3;
		float num10 = rotation.w * num;
		float num11 = rotation.w * num2;
		float num12 = rotation.w * num3;
		Vector3 worldPosition = default(Vector3);
		worldPosition.x = (1f - (num5 + num6)) * vector.x + (num7 - num12) * vector.y + (num8 + num11) * vector.z;
		worldPosition.y = (num7 + num12) * vector.x + (1f - (num4 + num6)) * vector.y + (num9 - num10) * vector.z;
		worldPosition.z = (num8 - num11) * vector.x + (num9 + num10) * vector.y + (1f - (num4 + num5)) * vector.z;
		return worldPosition.ToGridPosition();
	}

	public static Grid3 operator /(Grid3 left, int right)
	{
		return new Grid3(left.x / right, left.y / right, left.z / right);
	}

	public static Grid3 operator +(Grid3 left, Grid3 right)
	{
		left.x += right.x;
		left.y += right.y;
		left.z += right.z;
		return left;
	}

	public static Grid3 operator -(Grid3 left, Grid3 right)
	{
		left.x -= right.x;
		left.y -= right.y;
		left.z -= right.z;
		return left;
	}

	public static Grid3 operator -(Grid3 left, int right)
	{
		return left - new Grid3(right, right, right);
	}

	public static Grid3 operator -(Grid3 value)
	{
		value.x = -value.x;
		value.y = -value.y;
		value.z = -value.z;
		return value;
	}

	public static bool operator ==(Grid3 left, Grid3 right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Grid3 left, Grid3 right)
	{
		return !left.Equals(right);
	}

	public static Grid3 operator *(Grid3 left, float right)
	{
		left.x = (int)((float)left.x * right);
		left.y = (int)((float)left.y * right);
		left.z = (int)((float)left.z * right);
		return left;
	}

	public static Grid3 operator *(Grid3 left, int right)
	{
		return new Grid3(left.x * right, left.y * right, left.z * right);
	}

	public static implicit operator int3(Grid3 g)
	{
		return new int3(g.x, g.y, g.z);
	}

	public static Grid3 ModuloCorrect(Grid3 value, int mod)
	{
		return new Grid3(RocketMath.ModuloCorrect(value.x, mod), RocketMath.ModuloCorrect(value.y, mod), RocketMath.ModuloCorrect(value.z, mod));
	}

	public void ModuloCorrect(int mod)
	{
		x = RocketMath.ModuloCorrect(x, mod);
		y = RocketMath.ModuloCorrect(y, mod);
		z = RocketMath.ModuloCorrect(z, mod);
	}

	public override string ToString()
	{
		return $"Grid3({x}, {y}, {z})";
	}

	public readonly Vector3 ToVector3()
	{
		return new Vector3((float)x * 0.1f, (float)y * 0.1f, (float)z * 0.1f);
	}

	public Vector3 ToVector3Raw()
	{
		return new Vector3(x, y, z);
	}

	public int CompareTo(Grid3 other)
	{
		int num = x.CompareTo(other.x);
		if (num != 0)
		{
			return num;
		}
		int num2 = y.CompareTo(other.y);
		if (num2 != 0)
		{
			return num2;
		}
		return z.CompareTo(other.z);
	}

	public static bool IsWithinBounds(Grid3 value, Grid3 min, Grid3 max)
	{
		if (value.x >= min.x && value.x <= max.x && value.y >= min.y && value.y <= max.y && value.z >= min.z)
		{
			return value.z > max.z;
		}
		return true;
	}

	public void Deconstruct(out int x, out int y, out int z)
	{
		x = this.x;
		y = this.y;
		z = this.z;
	}

	static Grid3()
	{
		Directions = new Grid3[7]
		{
			(Vector3.forward * 2f).ToGridPosition(),
			(Vector3.back * 2f).ToGridPosition(),
			(Vector3.left * 2f).ToGridPosition(),
			(Vector3.right * 2f).ToGridPosition(),
			(Vector3.up * 2f).ToGridPosition(),
			(Vector3.down * 2f).ToGridPosition(),
			Vector3.zero.ToGridPosition()
		};
		North = Directions[0];
		South = Directions[1];
		West = Directions[2];
		East = Directions[3];
		Up = Directions[4];
		Down = Directions[5];
	}
}
