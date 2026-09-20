using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public class RocketGrid
{
	public static class Face
	{
		public static readonly Vector3 North = Directions[0] * 1f / 2f;

		public static readonly Vector3 East = Directions[3] * 1f / 2f;

		public static readonly Vector3 South = Directions[1] * 1f / 2f;

		public static readonly Vector3 West = Directions[2] * 1f / 2f;

		public static readonly Vector3 Up = Directions[4] * 1f / 2f;

		public static readonly Vector3 Down = Directions[5] * 1f / 2f;
	}

	public static class FaceInt
	{
		public static readonly int North = 0;

		public static readonly int East = 1;

		public static readonly int South = 2;

		public static readonly int West = 3;

		public static readonly int Up = 4;

		public static readonly int Down = 5;

		public static bool IsHorizontalFace(int face)
		{
			if (face >= North)
			{
				return face <= West;
			}
			return false;
		}

		public static int GetNextClockwiseHorizontalFace(int face)
		{
			face++;
			if (face > West)
			{
				face = North;
			}
			return face;
		}

		public static int GetNextAnticlockwiseHorizontalFace(int face)
		{
			face--;
			if (face < North)
			{
				face = West;
			}
			return face;
		}

		public static int FaceIntFromDir(Dir dir)
		{
			switch (dir)
			{
			case Dir.North:
				return North;
			case Dir.East:
				return East;
			case Dir.South:
				return South;
			case Dir.West:
				return West;
			case Dir.Up:
				return Up;
			case Dir.Down:
				return Down;
			default:
				Debug.LogError("Dir " + dir.ToString() + " doesn't correspond to any FaceInt");
				return 0;
			}
		}
	}

	public static class Edge
	{
		public static readonly Vector3 NorthEast = Face.North + Face.East;

		public static readonly Vector3 NorthWest = Face.North + Face.West;

		public static readonly Vector3 NorthUp = Face.North + Face.Up;

		public static readonly Vector3 NorthDown = Face.North + Face.Down;

		public static readonly Vector3 EastUp = Face.East + Face.Up;

		public static readonly Vector3 EastDown = Face.East + Face.Down;

		public static readonly Vector3 SouthEast = Face.South + Face.East;

		public static readonly Vector3 SouthWest = Face.South + Face.West;

		public static readonly Vector3 SouthUp = Face.South + Face.Up;

		public static readonly Vector3 SouthDown = Face.South + Face.Down;

		public static readonly Vector3 WestUp = Face.West + Face.Up;

		public static readonly Vector3 WestDown = Face.West + Face.Down;
	}

	public static class Corner
	{
		public static readonly Vector3 NorthEastUp = Face.North + Face.East + Face.Up;

		public static readonly Vector3 NorthWestUp = Face.North + Face.West + Face.Up;

		public static readonly Vector3 SouthEastUp = Face.South + Face.East + Face.Up;

		public static readonly Vector3 SouthWestUp = Face.South + Face.West + Face.Up;

		public static readonly Vector3 NorthEastDown = Face.North + Face.East + Face.Down;

		public static readonly Vector3 NorthWestDown = Face.North + Face.West + Face.Down;

		public static readonly Vector3 SouthEastDown = Face.South + Face.East + Face.Down;

		public static readonly Vector3 SouthWestDown = Face.South + Face.West + Face.Down;
	}

	public static class GridVoxel
	{
		public static readonly Vector3 NorthWestUp = Corner.NorthWestUp * 0.5f;

		public static readonly Vector3 NorthEastUp = Corner.NorthEastUp * 0.5f;

		public static readonly Vector3 NorthWestDown = Corner.NorthWestDown * 0.5f;

		public static readonly Vector3 NorthEastDown = Corner.NorthEastDown * 0.5f;

		public static readonly Vector3 SouthWestUp = Corner.SouthWestUp * 0.5f;

		public static readonly Vector3 SouthEastUp = Corner.SouthEastUp * 0.5f;

		public static readonly Vector3 SouthWestDown = Corner.SouthWestDown * 0.5f;

		public static readonly Vector3 SouthEastDown = Corner.SouthEastDown * 0.5f;

		public static readonly Vector3 Cube = new Vector3(1f, 1f, 1f);

		public static readonly Vector3[] AllDirections = new Vector3[8] { NorthEastUp, NorthEastDown, NorthWestDown, NorthWestUp, SouthEastUp, SouthEastDown, SouthWestDown, SouthWestUp };
	}

	public static readonly int Faces = 6;

	public static readonly Vector3[] Directions = new Vector3[7]
	{
		Vector3.forward * 2f,
		Vector3.back * 2f,
		Vector3.left * 2f,
		Vector3.right * 2f,
		Vector3.up * 2f,
		Vector3.down * 2f,
		Vector3.zero
	};

	public static readonly Grid3[] SmallGridDirections = new Grid3[7]
	{
		ToSmallGrid(Vector3.forward),
		ToSmallGrid(Vector3.back),
		ToSmallGrid(Vector3.left),
		ToSmallGrid(Vector3.right),
		ToSmallGrid(Vector3.up),
		ToSmallGrid(Vector3.down),
		ToSmallGrid(Vector3.zero)
	};

	private static Vector3[] _Directions = new Vector3[6]
	{
		Vector3.forward,
		Vector3.back,
		Vector3.left,
		Vector3.right,
		Vector3.up,
		Vector3.down
	};

	public static Vector3 North = Directions[0];

	public static Vector3 South = Directions[1];

	public static Vector3 West = Directions[2];

	public static Vector3 East = Directions[3];

	public static Vector3 Up = Directions[4];

	public static Vector3 Down = Directions[5];

	public static Grid3 SmallNorth = SmallGridDirections[0];

	public static Grid3 SmallSouth = SmallGridDirections[1];

	public static Grid3 SmallWest = SmallGridDirections[2];

	public static Grid3 SmallEast = SmallGridDirections[3];

	public static Grid3 SmallUp = SmallGridDirections[4];

	public static Grid3 SmallDown = SmallGridDirections[5];

	public const float HalfGridSize = 1f;

	public const float GridSize = 2f;

	public const float SmallGridSize = 0.5f;

	public const float SmallGridOffset = 0.25f;

	public static readonly float GridLength = Mathf.Sqrt(8f);

	public static readonly Vector3 GridSquare = Vector3.one * 2f;

	public static readonly Vector3 GridHalfSquare = Vector3.one * 2f * 0.5f;

	public static readonly Vector3 SmallGridSquare = Vector3.one * 0.5f;

	private const int CLOSEST_BUFFER = 12;

	public static float PositionScale => 0.05f;

	private static Grid3 ToSmallGrid(Vector3 position)
	{
		return (position * 0.5f).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
	}

	public static Vector3 RandomDirection()
	{
		return _Directions.Pick();
	}

	public static Dir GetVectorDir(Vector3 vector)
	{
		if (vector == Vector3.zero)
		{
			return Dir.Center;
		}
		float num = Vector3.Dot(vector, North);
		float num2 = Vector3.Dot(vector, East);
		float num3 = Vector3.Dot(vector, Up);
		if ((num != 0f && num2 != 0f) || (num != 0f && num3 != 0f) || (num2 != 0f && num3 != 0f))
		{
			return Dir.Undefined;
		}
		if (num > 0f)
		{
			return Dir.North;
		}
		if (num < 0f)
		{
			return Dir.South;
		}
		if (num2 > 0f)
		{
			return Dir.East;
		}
		if (num2 < 0f)
		{
			return Dir.West;
		}
		if (num3 > 0f)
		{
			return Dir.Up;
		}
		return Dir.Down;
	}

	public static float GetGridLength(float gridSize)
	{
		return Mathf.Sqrt(gridSize * gridSize * 2f);
	}

	public static Dir CardinalFacing(Vector3 direction)
	{
		float num = Vector3.Dot(direction, North);
		float num2 = Vector3.Dot(direction, East);
		float num3 = Vector3.Dot(direction, Up);
		float num4 = Mathf.Abs(num);
		float num5 = Mathf.Abs(num2);
		float num6 = Mathf.Abs(num3);
		if (num4 > num5 && num4 > num6)
		{
			if (!(num > 0f))
			{
				return Dir.South;
			}
			return Dir.North;
		}
		if (num5 > num6)
		{
			if (!(num2 > 0f))
			{
				return Dir.West;
			}
			return Dir.East;
		}
		if (!(num3 > 0f))
		{
			return Dir.Down;
		}
		return Dir.Up;
	}

	public static Vector3 GetVertex(int index, float y)
	{
		return index switch
		{
			0 => new Vector3(-0.9999f, y, -0.9999f), 
			1 => new Vector3(-0.9999f, y, 0.9999f), 
			2 => new Vector3(0.9999f, y, 0.9999f), 
			3 => new Vector3(0.9999f, y, -0.9999f), 
			_ => Vector3.zero, 
		};
	}

	public static Dir GetFaceDir(Vector3 face, Vector3 grid, Quaternion gridRotation)
	{
		return GetFaceDir(face, grid, 2f, 0f, gridRotation);
	}

	public static Dir GetFaceDir(Vector3 face, Vector3 grid, float gridSize, Quaternion gridRotation)
	{
		return GetFaceDir(face, grid, gridSize, 0f, Quaternion.identity);
	}

	public static Dir GetFaceDir(Vector3 face, Vector3 grid, float gridSize = 2f, float gridOffset = 0f)
	{
		return GetFaceDir(face, grid, gridSize, gridOffset, Quaternion.identity);
	}

	public static Dir GetFaceDir(Vector3 face, Vector3 grid, float gridSize, float gridOffset, Quaternion gridRotation)
	{
		_ = gridSize / 2f;
		for (int i = 0; i < 3; i++)
		{
			face[i] -= gridOffset;
			face[i] -= grid[i];
		}
		face = Quaternion.Inverse(gridRotation) * face;
		return _GetDirBase(-face);
	}

	public static Dir GetForwardDir(Vector3 forward)
	{
		return GetForwardDir(forward, Quaternion.identity);
	}

	public static Dir GetForwardDir(Vector3 forward, Quaternion gridRotation)
	{
		forward = Quaternion.Inverse(gridRotation) * forward;
		return _GetDirBase(forward);
	}

	private static Dir _GetDirBase(Vector3 forward)
	{
		int num = 0;
		if (Mathf.Abs(forward[1]) > Mathf.Abs(forward[num]))
		{
			num = 1;
		}
		if (Mathf.Abs(forward[2]) > Mathf.Abs(forward[num]))
		{
			num = 2;
		}
		switch (num)
		{
		case 0:
			if (!(forward[0] > 0f))
			{
				return Dir.East;
			}
			return Dir.West;
		case 1:
			if (!(forward[1] > 0f))
			{
				return Dir.Up;
			}
			return Dir.Down;
		case 2:
			if (!(forward[2] > 0f))
			{
				return Dir.North;
			}
			return Dir.South;
		default:
			return Dir.Undefined;
		}
	}

	public static void PopulateGridFaces(Span<Vector3> buf, ref int count, Vector3 position, float gridSize = 2f, float gridOffset = 0f)
	{
		position = position.GridCenter(gridSize, gridOffset);
		float num = gridSize / 2f;
		buf[0] = position + Face.North * num;
		buf[1] = position + Face.East * num;
		buf[2] = position + Face.South * num;
		buf[3] = position + Face.West * num;
		buf[4] = position + Face.Up * num;
		buf[5] = position + Face.Down * num;
		count = 6;
	}

	public static void PopulateGridNeighbours(Span<WorldGrid> buf, ref int count, WorldGrid worldGrid, bool horizontalOnly = false)
	{
		Grid3 value = worldGrid.Value;
		buf[0] = new WorldGrid(value + Grid3.North);
		buf[1] = new WorldGrid(value + Grid3.East);
		buf[2] = new WorldGrid(value + Grid3.South);
		buf[3] = new WorldGrid(value + Grid3.West);
		count = 4;
		if (!horizontalOnly)
		{
			buf[4] = new WorldGrid(value + Grid3.Up);
			buf[5] = new WorldGrid(value + Grid3.Down);
			count = 6;
		}
	}

	public static Location GridLocation(Vector3 position, float gridSize = 2f, float gridCenter = 0f)
	{
		Vector3 vector = position.GridCenter(gridSize, gridCenter);
		float num = Mathf.Abs(vector.x - position.x);
		float num2 = Mathf.Abs(vector.y - position.y);
		float num3 = Mathf.Abs(vector.z - position.z);
		float num4 = 0.1f;
		if (Vector3.SqrMagnitude(position - vector) < num4 * num4)
		{
			return Location.Center;
		}
		return (((Math.Abs(num - gridSize / 2f) < num4) ? 1 : 0) + ((Math.Abs(num2 - gridSize / 2f) < num4) ? 1 : 0) + ((Math.Abs(num3 - gridSize / 2f) < num4) ? 1 : 0)) switch
		{
			1 => Location.Face, 
			2 => Location.Edge, 
			3 => Location.Corner, 
			_ => Location.Invalid, 
		};
	}

	public static Vector3 GetClosest(Span<Vector3> checkPositions, Vector3 masterPosition)
	{
		if (checkPositions.Length == 0)
		{
			return masterPosition;
		}
		Vector3 result = masterPosition;
		float num = float.PositiveInfinity;
		Span<Vector3> span = checkPositions;
		for (int i = 0; i < span.Length; i++)
		{
			Vector3 vector = span[i];
			float num2 = vector.DistanceSquared(masterPosition);
			if (!(num2 >= num))
			{
				result = vector;
				num = num2;
			}
		}
		return result;
	}

	[SkipLocalsInit]
	public static Vector3 GetClosestFace(Vector3 position, Vector3 comparePosition)
	{
		Vector3 position2 = position.GridCenter();
		Span<Vector3> obj = stackalloc Vector3[12];
		int count = 0;
		PopulateGridFaces(obj, ref count, position2);
		Span<Vector3> span = obj;
		return GetClosest(span.Slice(0, count), comparePosition).GridPosition();
	}

	public static Vector3 GetDirection(Vector3 gridPosition, Vector3 gridCentre)
	{
		return gridPosition.DirectionTo(gridCentre);
	}

	public static Vector3 GridCenterBetween(Vector3 position1, Vector3 position2)
	{
		return Vector3.Lerp(position1, position2, 0.5f).GridCenter();
	}
}
