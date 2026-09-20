using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TerrainSystem;

public class Vein : IThreadable
{
	public const int MAX_MINABLES_VEIN = 254;

	public const int MAX_VEIN_TREE_DEPTH = 127;

	public const int ALL_VEIN_COUNT = 1048576;

	public static readonly ConcurrentDictionary<Vector3Int, List<Vein>> VeinsLookup = new ConcurrentDictionary<Vector3Int, List<Vein>>(Environment.ProcessorCount, 1048576);

	public static readonly HashSet<int> AllAimeeQueuedMinables = new HashSet<int>();

	public const int DIRECTION_COUNT = 26;

	public static readonly Vector3Int[] DirectionLookup = new Vector3Int[27]
	{
		new Vector3Int(0, 0, 0),
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 0, -1),
		new Vector3Int(1, 1, 0),
		new Vector3Int(1, -1, 0),
		new Vector3Int(-1, 1, 0),
		new Vector3Int(-1, -1, 0),
		new Vector3Int(1, 0, 1),
		new Vector3Int(1, 0, -1),
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, 0, -1),
		new Vector3Int(0, 1, 1),
		new Vector3Int(0, 1, -1),
		new Vector3Int(0, -1, 1),
		new Vector3Int(0, -1, -1),
		new Vector3Int(1, 1, 1),
		new Vector3Int(1, 1, -1),
		new Vector3Int(1, -1, 1),
		new Vector3Int(1, -1, -1),
		new Vector3Int(-1, 1, 1),
		new Vector3Int(-1, 1, -1),
		new Vector3Int(-1, -1, 1),
		new Vector3Int(-1, -1, -1)
	};

	private static readonly Vector3[] DirectionLookupNormalised = new Vector3[27]
	{
		new Vector3(0f, 0f, 0f),
		Vector3.Normalize(new Vector3Int(1, 0, 0)),
		Vector3.Normalize(new Vector3Int(-1, 0, 0)),
		Vector3.Normalize(new Vector3Int(0, 1, 0)),
		Vector3.Normalize(new Vector3Int(0, -1, 0)),
		Vector3.Normalize(new Vector3Int(0, 0, 1)),
		Vector3.Normalize(new Vector3Int(0, 0, -1)),
		Vector3.Normalize(new Vector3Int(1, 1, 0)),
		Vector3.Normalize(new Vector3Int(1, -1, 0)),
		Vector3.Normalize(new Vector3Int(-1, 1, 0)),
		Vector3.Normalize(new Vector3Int(-1, -1, 0)),
		Vector3.Normalize(new Vector3Int(1, 0, 1)),
		Vector3.Normalize(new Vector3Int(1, 0, -1)),
		Vector3.Normalize(new Vector3Int(-1, 0, 1)),
		Vector3.Normalize(new Vector3Int(-1, 0, -1)),
		Vector3.Normalize(new Vector3Int(0, 1, 1)),
		Vector3.Normalize(new Vector3Int(0, 1, -1)),
		Vector3.Normalize(new Vector3Int(0, -1, 1)),
		Vector3.Normalize(new Vector3Int(0, -1, -1)),
		Vector3.Normalize(new Vector3Int(1, 1, 1)),
		Vector3.Normalize(new Vector3Int(1, 1, -1)),
		Vector3.Normalize(new Vector3Int(1, -1, 1)),
		Vector3.Normalize(new Vector3Int(1, -1, -1)),
		Vector3.Normalize(new Vector3Int(-1, 1, 1)),
		Vector3.Normalize(new Vector3Int(-1, 1, -1)),
		Vector3.Normalize(new Vector3Int(-1, -1, 1)),
		Vector3.Normalize(new Vector3Int(-1, -1, -1))
	};

	public readonly MinableType Type;

	public readonly Vector3Int VeinWorldPosition;

	public readonly Vector3Int ClusterPosition;

	private Minable[] _minables;

	private Dictionary<int, byte> _minableLookup;

	public BoundsInt VeinBounds;

	public readonly VeinGenerationData Data;

	public readonly HSVColor TerrainColor;

	private const float BIAS_POWER = 3f;

	private const float BIAS_OFFSET = 0.75f;

	private const float CloseNeighbourRatio = 0.05f;

	private const float ActiveRatio = 0.15f;

	private static Vector3Int[] _visibleMinablesCheck = new Vector3Int[5]
	{
		new Vector3Int(0, 0, 0),
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 0, -1)
	};

	public const float LowFidelityVisibleThreshold = 0.8f;

	public const float HIGH_FIDELITY_VISIBLE_SQUARE_DISTANCE = 1024f;

	public const float ALWAYS_VISIBLE_SQUARE_DISTANCE = 256f;

	private const int MIN_DEBUG_RENDER_DISTANCE = 32;

	private const int MAX_DEBUG_RENDER_DISTANCE = 128;

	private const int TEXT_RENDER_RANGE = 24;

	public static bool IsDrawDebug = false;

	private static int _debugRenderRange = 32;

	private static List<Vein> _debugVeinsToDrawList = new List<Vein>(2048);

	private static readonly Vector3 DebugMinableDrawSize = new Vector3(0.5f, 0.5f, 0.5f);

	private static readonly Vector3 DebugMinableInactiveSize = new Vector3(0.1f, 0.1f, 0.1f);

	private static readonly StringBuilder DebugSb = new StringBuilder();

	private const string COUNT = "count: ";

	private static uint[] DebugMinableColor = new uint[17]
	{
		ImGuiColor.Integer.DefaultColor,
		ImGuiColor.Integer.White,
		ImGuiColor.Integer.Magenta,
		ImGuiColor.Integer.Blue,
		ImGuiColor.Integer.Yellow,
		ImGuiColor.Integer.DarkGrey,
		ImGuiColor.Integer.Orange,
		ImGuiColor.Integer.Green,
		ImGuiColor.Integer.LightBrown,
		ImGuiColor.Integer.MustardYellow,
		ImGuiColor.Integer.Grey,
		ImGuiColor.Integer.White,
		ImGuiColor.Integer.LightBlue,
		ImGuiColor.Integer.Red,
		ImGuiColor.Integer.DefaultColor,
		ImGuiColor.Integer.Violet,
		ImGuiColor.Integer.LightGreen
	};

	public int ThreadCost => _minables.Length;

	public bool IsModified { get; private set; }

	public static int DebugRenderRange
	{
		get
		{
			return _debugRenderRange;
		}
		set
		{
			_debugRenderRange = Mathf.Clamp(value, 32, 128);
		}
	}

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return "Vein_" + EnumCollections.MinableTypes.GetName(Type) + "_" + StringManager.Get(VeinWorldPosition);
	}

	public static void Register(Vein vein)
	{
		RegisterToBoundsLookup(vein);
		VeinCluster.Register(vein);
	}

	private static void RegisterToBoundsLookup(Vein vein)
	{
		Vector3Int vector3Int = VoxelTerrain.WorldToOctreeSpaceClamped(vein.VeinBounds.min);
		Vector3Int vector3Int2 = VoxelTerrain.WorldToOctreeSpaceClamped(vein.VeinBounds.max);
		vector3Int /= 32;
		vector3Int *= 32;
		vector3Int2 /= 32;
		vector3Int2 *= 32;
		for (int i = vector3Int.x; i <= vector3Int2.x; i += 32)
		{
			for (int j = vector3Int.z; j <= vector3Int2.z; j += 32)
			{
				for (int k = vector3Int.y; k <= vector3Int2.y; k += 32)
				{
					if (VeinsLookup.TryGetValue(new Vector3Int(i, k, j), out List<Vein> value))
					{
						lock (value)
						{
							value.Add(vein);
						}
					}
					else
					{
						VeinsLookup.TryAdd(new Vector3Int(i, k, j), new List<Vein> { vein });
					}
				}
			}
		}
	}

	public static bool TryGetVeinsAtPosition(Vector3Int worldPosInt, out List<Vein> veins)
	{
		Vector3Int key = VoxelTerrain.WorldToOctreeSpaceClamped(worldPosInt);
		key /= 32;
		key *= 32;
		return VeinsLookup.TryGetValue(key, out veins);
	}

	public int MakeMinableHash(int index)
	{
		int key = _minables[index].GetKey();
		int hashCode = VeinWorldPosition.GetHashCode();
		int type = (int)Type;
		return key ^ hashCode ^ type;
	}

	public void GetAimeeMinables(List<TargetMinableData> queue, int maxDepth, BoundsInt searchBounds)
	{
		for (int i = 0; i < _minables.Length; i++)
		{
			if (!GetActive(i))
			{
				continue;
			}
			Vector3Int position = _minables[i].WorldPositionInt(VeinWorldPosition);
			if (searchBounds.Contains(position) && IsNearSurface(position, maxDepth))
			{
				int item = MakeMinableHash(i);
				if (!AllAimeeQueuedMinables.Contains(item))
				{
					AllAimeeQueuedMinables.Add(item);
					queue.Add(new TargetMinableData
					{
						Vein = this,
						MinableIndex = i
					});
				}
			}
		}
	}

	public void RemoveAimeeMinableHash(int index)
	{
		int item = MakeMinableHash(index);
		AllAimeeQueuedMinables.Remove(item);
	}

	public static Vein GetVeinAtPosition(Vector3 worldPosition)
	{
		Vector3Int vector3Int = worldPosition.FloorToInt();
		if (TryGetVeinsAtPosition(vector3Int, out List<Vein> veins))
		{
			lock (veins)
			{
				foreach (Vein item in veins)
				{
					if (item.HasActivePosition(vector3Int, out var _))
					{
						return item;
					}
				}
			}
		}
		return null;
	}

	public static void ClearAll()
	{
		foreach (List<Vein> item in VeinsLookup.Values.ToList())
		{
			item.Clear();
		}
		VeinsLookup.Clear();
		IsDrawDebug = false;
	}

	private Vein(VeinTemplate template)
	{
		Data = template.Data;
		Type = Data.Type;
		if (MinableVisualiserData.MinableVisualizers.TryGetValue(Type, out var value))
		{
			TerrainColor = new HSVColor(value.TerrainColorReference);
		}
		VeinWorldPosition = template.WorldPosition;
		ClusterPosition = template.ClusterPosition;
	}

	private Vein(VeinGenerationData data, Vector3Int worldPosition, Vector3Int clusterPosition, byte minablesCount)
	{
		Data = data;
		Type = data.Type;
		if (MinableVisualiserData.MinableVisualizers.TryGetValue(Type, out var value))
		{
			TerrainColor = new HSVColor(value.TerrainColorReference);
		}
		VeinWorldPosition = worldPosition;
		ClusterPosition = clusterPosition;
		_minables = new Minable[minablesCount];
	}

	public void Clear()
	{
		Array.Clear(_minables, 0, _minables.Length);
		_minableLookup.Clear();
	}

	public static void Generate(VeinTemplate template, System.Random random, float[] directionWeightTable)
	{
		Vein vein = new Vein(template);
		Span<Minable> span = stackalloc Minable[254];
		Vector3Int worldPosition = template.WorldPosition;
		VeinGenerationData data = template.Data;
		int index = -1;
		int depth = -1;
		byte b = 0;
		if (template.Data.SeekSurface != null)
		{
			for (byte b2 = 0; b2 < template.Data.SeekSurface.GetMaxAttempts(); b2++)
			{
				if (VoxelTerrain.GetReadonlyDensityWorldSpace(Minable.WorldPositionInt(127, (byte)(127 + b2), 127, worldPosition)) <= 127)
				{
					b = b2;
					break;
				}
			}
		}
		else if (template.Data.SeekDepth != null)
		{
			int value = template.Data.SeekDepth.Value;
			if (Mathf.Abs(value - worldPosition.y) < template.Data.SeekDepth.GetMaxAttempts())
			{
				b = (byte)(value - worldPosition.y);
			}
		}
		Vector3Int vector3Int = Minable.WorldPositionInt(127, (byte)(127 + b), 127, worldPosition);
		bool isActive = !WorldSetting.Current.IsUnderLava(vector3Int) && VoxelTerrain.GetReadonlyDensityWorldSpace(vector3Int) > 127;
		Minable parent = new Minable(127, (byte)(127 + b), 127, 0, isActive);
		int maxDepth = Mathf.Min(data.MaxDepth, 127);
		int maxSize = Mathf.Min(data.MaxSize, 254);
		vein.Branch(parent, data, random, directionWeightTable, maxDepth, maxSize, ref depth, ref index, span);
		Span<Minable> span2 = span;
		vein._minables = span2.Slice(0, index + 1).ToArray();
		span.Clear();
		vein.GenerateBounds();
		vein.PopulateLookup();
		Register(vein);
	}

	private void PopulateLookup()
	{
		_minableLookup = new Dictionary<int, byte>(_minables.Length);
		for (int i = 0; i < _minables.Length; i++)
		{
			_minableLookup.Add(_minables[i].GetKey(), (byte)i);
		}
	}

	private void Branch(Minable parent, VeinGenerationData data, System.Random random, float[] directionWeightTable, int maxDepth, int maxSize, ref int depth, ref int index, Span<Minable> tempMinables)
	{
		depth++;
		index++;
		tempMinables[index] = parent;
		int num = index;
		if (depth >= maxDepth || index >= maxSize - 1)
		{
			return;
		}
		int branchAttempts = data.GetBranchAttempts(depth);
		for (int i = 0; i < branchAttempts; i++)
		{
			GetNextPositionInVein(data, depth, parent, random, directionWeightTable, out var x, out var y, out var z, tempMinables);
			int thickness = data.Thickness.GetThickness(depth);
			Vector3.Normalize(parent.LocalPosition - tempMinables[parent.ParentIndex].LocalPosition);
			Vector3Int vector3Int = Minable.WorldPositionInt(x, y, z, VeinWorldPosition);
			bool isActive = !WorldSetting.Current.IsUnderLava(vector3Int) && VoxelTerrain.GetReadonlyDensityWorldSpace(vector3Int) > 127;
			Minable minable = new Minable(x, y, z, (byte)num, isActive);
			if (thickness > 0)
			{
				for (int j = 0; j < thickness; j++)
				{
					Vector3Int vector3Int2 = DirectionLookup[random.Next(1, DirectionLookupNormalised.Length)];
					x = (byte)(vector3Int2.x + parent.X);
					y = (byte)(vector3Int2.y + parent.Y);
					z = (byte)(vector3Int2.z + parent.Z);
					Vector3Int vector3Int3 = Minable.WorldPositionInt(x, y, z, VeinWorldPosition);
					bool isActive2 = !WorldSetting.Current.IsUnderLava(vector3Int3) && VoxelTerrain.GetReadonlyDensityWorldSpace(vector3Int3) > 127;
					Minable minable2 = new Minable(x, y, z, (byte)num, isActive2);
					if (!HasVisited(minable2, tempMinables, index + 1) && !minable.Equals(minable2))
					{
						index++;
						tempMinables[index] = minable2;
					}
				}
			}
			if (!HasVisited(minable, tempMinables, index + 1))
			{
				Branch(minable, data, random, directionWeightTable, maxDepth, maxSize, ref depth, ref index, tempMinables);
			}
			if (index >= maxSize - 1)
			{
				return;
			}
		}
		depth--;
	}

	public void GetNextPositionInVein(VeinGenerationData data, int depth, Minable parent, System.Random random, float[] directionWeightTable, out byte x, out byte y, out byte z, Span<Minable> tempMinables)
	{
		float momentumBias = data.Momentum.GetMomentumBias(depth);
		float bias = data.Direction.GetBias(depth);
		Vector3 vector = Vector3.Normalize(data.Direction.Direction);
		Vector3 vector2 = Vector3.Normalize(parent.LocalPosition - tempMinables[parent.ParentIndex].LocalPosition);
		Vector3 lhs = vector * bias + vector2 * momentumBias;
		float num = (lhs.Equals(Vector3.zero) ? 0f : lhs.magnitude);
		if (num == 0f)
		{
			Vector3Int vector3Int = DirectionLookup[random.Next(1, DirectionLookupNormalised.Length)];
			x = (byte)(vector3Int.x + parent.X);
			y = (byte)(vector3Int.y + parent.Y);
			z = (byte)(vector3Int.z + parent.Z);
			return;
		}
		float num2 = 0f;
		for (int i = 1; i < directionWeightTable.Length; i++)
		{
			float num3 = Vector3.Dot(lhs, DirectionLookupNormalised[i]);
			float num4 = Mathf.Max(0f, Mathf.Pow(0.75f + num3 * num, 3f));
			num2 += num4;
			directionWeightTable[i] = num4;
		}
		float num5 = (float)random.NextDouble() * num2;
		Vector3Int vector3Int2 = Vector3Int.zero;
		for (int j = 1; j < DirectionLookup.Length; j++)
		{
			vector3Int2 = DirectionLookup[j];
			if (num5 < directionWeightTable[j])
			{
				break;
			}
			num5 -= directionWeightTable[j];
		}
		x = (byte)(vector3Int2.x + parent.X);
		y = (byte)(vector3Int2.y + parent.Y);
		z = (byte)(vector3Int2.z + parent.Z);
	}

	private bool HasVisited(Minable toCheck, Span<Minable> tempMinables, int count)
	{
		for (int i = 0; i < count; i++)
		{
			Minable other = tempMinables[i];
			if (toCheck.Equals(other))
			{
				return true;
			}
		}
		return false;
	}

	private void GenerateBounds()
	{
		byte b = byte.MaxValue;
		byte b2 = 0;
		byte b3 = byte.MaxValue;
		byte b4 = 0;
		byte b5 = byte.MaxValue;
		byte b6 = 0;
		for (int i = 0; i < _minables.Length; i++)
		{
			if (_minables[i].X < b)
			{
				b = _minables[i].X;
			}
			if (_minables[i].X > b2)
			{
				b2 = _minables[i].X;
			}
			if (_minables[i].Y < b3)
			{
				b3 = _minables[i].Y;
			}
			if (_minables[i].Y > b4)
			{
				b4 = _minables[i].Y;
			}
			if (_minables[i].Z < b5)
			{
				b5 = _minables[i].Z;
			}
			if (_minables[i].Z > b6)
			{
				b6 = _minables[i].Z;
			}
		}
		Vector3Int vector3Int = Minable.WorldPositionInt(b, b3, b5, VeinWorldPosition);
		VeinBounds = new BoundsInt(vector3Int.x, vector3Int.y, vector3Int.z, b2 - b, b4 - b3, b6 - b5);
	}

	private void WorldPositionToVeinSpace(Vector3Int worldPositionInt, out byte x, out byte y, out byte z)
	{
		x = (byte)(worldPositionInt.x - VeinWorldPosition.x + 127);
		y = (byte)(worldPositionInt.y - VeinWorldPosition.y + 127);
		z = (byte)(worldPositionInt.z - VeinWorldPosition.z + 127);
	}

	public bool HasActivePosition(Vector3Int worldPositionInt, out byte activeIndex)
	{
		activeIndex = 0;
		WorldPositionToVeinSpace(worldPositionInt, out var x, out var y, out var z);
		if (_minableLookup.TryGetValue(Minable.MakeKey(x, y, z), out var value) && _minables[value].IsActive)
		{
			activeIndex = value;
			return true;
		}
		return false;
	}

	public bool HasUnMinedMinables()
	{
		Minable[] minables = _minables;
		for (int i = 0; i < minables.Length; i++)
		{
			if (minables[i].IsActive)
			{
				return true;
			}
		}
		return false;
	}

	public Vector2 GetCrackedEffectColor(Vector3 worldPosition)
	{
		Vector3Int vector3Int = worldPosition.RoundToInt();
		float num = 0f;
		float h = TerrainColor.H;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (HasActivePosition(new Vector3Int(vector3Int.x + i, vector3Int.y + j, vector3Int.z + k), out var _))
					{
						if (i == 0)
						{
							num += (1f - num) * 0.05f;
						}
						if (j == 0)
						{
							num += (1f - num) * 0.05f;
						}
						if (k == 0)
						{
							num += (1f - num) * 0.05f;
						}
						num += (1f - num) * 0.15f;
					}
				}
			}
		}
		num = Mathf.Clamp01(num);
		return new Vector2(h, Mathf.Lerp(0f, TerrainColor.V, num));
	}

	public int GetClosestActiveIndex(Vector3 position)
	{
		float num = float.MaxValue;
		int result = -1;
		for (int i = 0; i < _minables.Length; i++)
		{
			if (_minables[i].IsActive)
			{
				float num2 = Vector3.SqrMagnitude(position - _minables[i].WorldPositionInt(VeinWorldPosition));
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	public static void MineAtPositionClient(Vector3Int minedPosition)
	{
		if (GameManager.RunSimulation)
		{
			throw new Exception("TryMineClient should only run on client");
		}
		Vein veinAtPosition = GetVeinAtPosition(minedPosition);
		if (veinAtPosition != null && veinAtPosition.HasActivePosition(minedPosition, out var activeIndex))
		{
			veinAtPosition.IsModified = true;
			veinAtPosition._minables[activeIndex] = Minable.Mined(veinAtPosition._minables[activeIndex]);
			VoxelTerrain.DirtyAllMinables(minedPosition).Forget();
		}
	}

	public static bool ShouldReleaseMinables(byte newDensity)
	{
		return newDensity < 127;
	}

	public bool TryMineServer(Vector3Int minedPosition, out Ore createdOre, Vector3 dropPosition)
	{
		createdOre = null;
		if (HasActivePosition(minedPosition, out var activeIndex))
		{
			IsModified = true;
			_minables[activeIndex] = Minable.Mined(_minables[activeIndex]);
			int num = VoxelTerrain.OrePrefabHash(Data.Type);
			if (num != PrefabHashmap.Invalid)
			{
				createdOre = Thing.Create<Ore>(num, dropPosition, UnityEngine.Random.rotation, 0L);
				float minedQuantity = Data.GetMinedQuantity();
				createdOre.SetQuantity(Mathf.CeilToInt(Mathf.Clamp(minedQuantity, 1f, createdOre.MaxQuantity)));
			}
			VoxelTerrain.DirtyAllMinables(minedPosition).Forget();
			return true;
		}
		return false;
	}

	public bool TryRemoveServer(Vector3Int minedPosition)
	{
		if (HasActivePosition(minedPosition, out var activeIndex))
		{
			IsModified = true;
			_minables[activeIndex] = Minable.SetActive(_minables[activeIndex], isActive: false);
			VoxelTerrain.DirtyAllMinables(minedPosition).Forget();
			return true;
		}
		return false;
	}

	public bool GetActive(int minableIndex)
	{
		if (minableIndex > -1)
		{
			return _minables[minableIndex].IsActive;
		}
		return false;
	}

	public void AddToDrawCall(InstancedIndirectDrawCall drawCall, bool randomRotation, bool hideInTerrain)
	{
		for (int i = 0; i < _minables.Length; i++)
		{
			if (_minables[i].IsActive && (!hideInTerrain || IsMinableVisible(_minables[i])))
			{
				Quaternion q = (randomRotation ? _minables[i].Rotation : Quaternion.identity);
				drawCall.AddInstance(Matrix4x4.TRS(_minables[i].WorldRenderPosition(VeinWorldPosition) + VoxelConstants.TerrainMeshOffset, q, Minable.MinableRenderScale));
			}
		}
	}

	public Vector3 GetMinableWorldPosition(int minableIndex)
	{
		return _minables[minableIndex].WorldPositionInt(VeinWorldPosition) + VoxelConstants.TerrainMeshOffset;
	}

	public bool IsMinableVisible(Minable minable)
	{
		Vector3Int vector3Int = minable.WorldPositionInt(VeinWorldPosition);
		if (Vector3.SqrMagnitude(InventoryManager.WorldPosition - vector3Int) < 256f)
		{
			return true;
		}
		if (Vector3.SqrMagnitude(InventoryManager.WorldPosition - vector3Int) < 1024f)
		{
			for (int i = 0; i < _visibleMinablesCheck.Length; i++)
			{
				if (VoxelTerrain.GetDensityWorldSpace(vector3Int + _visibleMinablesCheck[i]) < 1f)
				{
					return true;
				}
			}
			return false;
		}
		return VoxelTerrain.GetDensityWorldSpace(vector3Int) < 0.8f;
	}

	public static void GetVeinsInBounds(BoundsInt boundsInt, ref List<Vein> veins)
	{
		Vector3Int min = VoxelTerrain.WorldToOctreeSpaceClamped(boundsInt.min);
		Vector3Int max = VoxelTerrain.WorldToOctreeSpaceClamped(boundsInt.max);
		GetVeinsOctreeSpace(min, max, ref veins);
	}

	public static Vein? GetNearestVeinOfType(Vector3 position, float size, MinableType type)
	{
		float num = size * 0.5f;
		BoundsInt boundsInt = new BoundsInt((int)Math.Floor(position.x - num), (int)Math.Floor(position.y - num), (int)Math.Floor(position.z - num), (int)size + 1, (int)size + 1, (int)size + 1);
		List<Vein> veins = new List<Vein>();
		GetVeinsInBounds(boundsInt, ref veins);
		float num2 = float.MaxValue;
		Vein result = null;
		foreach (Vein item in veins)
		{
			if (item.Type == type && item.HasUnMinedMinables())
			{
				float num3 = Vector3.SqrMagnitude(item.VeinWorldPosition - boundsInt.center);
				if (num3 < num2)
				{
					num2 = num3;
					result = item;
				}
			}
		}
		return result;
	}

	public static float DistanceToNearestMinableInVein(Vector3 position, Vein vein)
	{
		float num = float.MaxValue;
		Minable[] minables = vein._minables;
		foreach (Minable minable in minables)
		{
			Vector3Int vector3Int = minable.WorldPositionInt(vein.VeinWorldPosition);
			float num2 = Vector3.SqrMagnitude(position - vector3Int);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return Mathf.Sqrt(num);
	}

	public static Vector3 GetClosestMinablePosition(Vector3 position, Vein vein)
	{
		float num = float.MaxValue;
		Vector3 result = Vector3.zero;
		Minable[] minables = vein._minables;
		foreach (Minable minable in minables)
		{
			Vector3Int vector3Int = minable.WorldPositionInt(vein.VeinWorldPosition);
			float num2 = Vector3.SqrMagnitude(position - vector3Int);
			if (num2 < num)
			{
				num = num2;
				result = vector3Int;
			}
		}
		return result;
	}

	public static void GetVeinsOctreeSpace(Vector3Int min, Vector3Int max, ref List<Vein> veins)
	{
		min /= 32;
		min *= 32;
		max /= 32;
		max *= 32;
		for (int i = min.x; i <= max.x; i += 32)
		{
			for (int j = min.y; j <= max.y; j += 32)
			{
				for (int k = min.z; k <= max.z; k += 32)
				{
					if (!VeinsLookup.TryGetValue(new Vector3Int(i, j, k), out List<Vein> value))
					{
						continue;
					}
					for (int l = 0; l < value.Count; l++)
					{
						if (!veins.Contains(value[l]))
						{
							veins.Add(value[l]);
						}
					}
				}
			}
		}
	}

	public int GetNumberOfReachableMinables(int maxDepth)
	{
		int num = 0;
		Minable[] minables = _minables;
		for (int i = 0; i < minables.Length; i++)
		{
			Minable minable = minables[i];
			if (minable.IsActive && IsNearSurface(minable.WorldPositionInt(VeinWorldPosition), maxDepth))
			{
				num++;
			}
		}
		return num;
	}

	private bool IsNearSurface(Vector3Int position, int maxDepth)
	{
		return VoxelTerrain.GetDensityWorldSpace(position + Vector3Int.up * maxDepth) < 0.49803922f;
	}

	public bool IsMinableNearSurface(int index, int maxDepth)
	{
		return IsNearSurface(_minables[index].WorldPositionInt(VeinWorldPosition), maxDepth);
	}

	public static BoundsInt MakeRenderBounds(Vector3 worldPosition, int x, int y, int z)
	{
		Vector3Int vector3Int = new Vector3Int((int)worldPosition.x, (int)worldPosition.y, (int)worldPosition.z);
		vector3Int /= 32;
		vector3Int *= 32;
		int num = x / 32 * 32;
		int num2 = y / 32 * 32;
		int num3 = z / 32 * 32;
		return new BoundsInt(new Vector3Int(-num + vector3Int.x, -num2 + vector3Int.y, -num3 + vector3Int.z), new Vector3Int(num + num, num2 + num2, num3 + num3));
	}

	public void Shake(byte minableIndex, float t)
	{
		MinableVisualiser dummyObject = VoxelTerrain.GetDummyObject(Type);
		dummyObject.SetActive(active: true);
		t = Mathf.Clamp01(t);
		Vector3 vector = _minables[minableIndex].WorldRenderPosition(VeinWorldPosition) + VoxelConstants.TerrainMeshOffset;
		Quaternion rotation = _minables[minableIndex].Rotation;
		float num = 0.008f * t;
		float num2 = 0.6f * t;
		float num3 = 1.5f * t;
		float time = Time.time;
		Vector3 vector2 = new Vector3(Mathf.Sin(time * num3 + 0f), Mathf.Sin(time * num3 + 1f), Mathf.Sin(time * num3 + 2f)) * num * t;
		Vector3 euler = new Vector3(Mathf.Sin(time * num3 + 3f), Mathf.Sin(time * num3 + 4f), Mathf.Sin(time * num3 + 5f)) * num2 * t;
		float num4 = Minable.MinableRenderScale.x * 1.001f + Minable.MinableRenderScale.x * 0.02f * Mathf.Clamp01(t * 2f);
		dummyObject.Transform.position = vector + vector2;
		dummyObject.Transform.rotation = rotation * Quaternion.Euler(euler);
		dummyObject.Transform.localScale = new Vector3(num4, num4, num4);
	}

	public static void DeduplicateMinables(Vein vein, ref List<Vein> neighbourVeins)
	{
		neighbourVeins.Clear();
		GetVeinsInBounds(vein.VeinBounds, ref neighbourVeins);
		foreach (Vein neighbourVein in neighbourVeins)
		{
			if (neighbourVein == vein || !IsNeighbourOverlap(vein, neighbourVein))
			{
				continue;
			}
			bool flag = IsNeighbourPriorityVein(vein, neighbourVein);
			for (int i = 0; i < vein._minables.Length; i++)
			{
				Vector3Int worldPositionInt = vein._minables[i].WorldPositionInt(vein.VeinWorldPosition);
				if (neighbourVein.HasActivePosition(worldPositionInt, out var activeIndex))
				{
					if (flag)
					{
						vein._minables[i] = Minable.SetActive(vein._minables[i], isActive: false);
					}
					else
					{
						neighbourVein._minables[activeIndex] = Minable.SetActive(neighbourVein._minables[activeIndex], isActive: false);
					}
				}
			}
		}
	}

	private static bool IsNeighbourPriorityVein(Vein vein, Vein neighbour)
	{
		if ((int)neighbour.Type > (int)vein.Type)
		{
			return true;
		}
		if ((int)neighbour.Type < (int)vein.Type)
		{
			return false;
		}
		if (neighbour.VeinWorldPosition.x <= vein.VeinWorldPosition.x && neighbour.VeinWorldPosition.y <= vein.VeinWorldPosition.y)
		{
			return neighbour.VeinWorldPosition.z > vein.VeinWorldPosition.z;
		}
		return true;
	}

	private static bool IsNeighbourOverlap(Vein vein, Vein neighbour)
	{
		BoundsInt veinBounds = vein.VeinBounds;
		BoundsInt veinBounds2 = neighbour.VeinBounds;
		if (veinBounds.xMin < veinBounds2.xMax && veinBounds.xMax > veinBounds2.xMin && veinBounds.yMin < veinBounds2.yMax && veinBounds.yMax > veinBounds2.yMin && veinBounds.zMin < veinBounds2.zMax)
		{
			return veinBounds.zMax > veinBounds2.zMin;
		}
		return false;
	}

	public static void SerializeSave(BinaryWriter writer)
	{
		long position = writer.BaseStream.Position;
		int num = 0;
		writer.Write(num);
		foreach (Vein modifiedVein in VeinCluster.GetModifiedVeins())
		{
			writer.Write((short)modifiedVein.VeinWorldPosition.x);
			writer.Write((short)modifiedVein.VeinWorldPosition.y);
			writer.Write((short)modifiedVein.VeinWorldPosition.z);
			writer.Write((short)modifiedVein.ClusterPosition.x);
			writer.Write((short)modifiedVein.ClusterPosition.y);
			writer.Write((short)modifiedVein.ClusterPosition.z);
			writer.Write(modifiedVein.Data.IdHash);
			writer.Write((byte)modifiedVein._minables.Length);
			for (int i = 0; i < modifiedVein._minables.Length; i++)
			{
				modifiedVein._minables[i].Serialize(writer);
			}
			num++;
		}
		writer.BaseStream.Position = position;
		writer.Write(num);
		writer.Seek(0, SeekOrigin.End);
	}

	public static void DeserializeSave(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			short z = reader.ReadInt16();
			short x2 = reader.ReadInt16();
			short y2 = reader.ReadInt16();
			short z2 = reader.ReadInt16();
			int idHash = reader.ReadInt32();
			byte b = reader.ReadByte();
			Vein vein = new Vein(DataCollection.Get<VeinGenerationData>(idHash), new Vector3Int(x, y, z), new Vector3Int(x2, y2, z2), b);
			for (int j = 0; j < b; j++)
			{
				vein._minables[j] = Minable.Deserialize(reader);
			}
			vein.IsModified = true;
			vein.GenerateBounds();
			Register(vein);
			vein.PopulateLookup();
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		foreach (Vein modifiedVein in VeinCluster.GetModifiedVeins())
		{
			writer.WriteInt16((short)modifiedVein.VeinWorldPosition.x);
			writer.WriteInt16((short)modifiedVein.VeinWorldPosition.y);
			writer.WriteInt16((short)modifiedVein.VeinWorldPosition.z);
			writer.WriteInt16((short)modifiedVein.ClusterPosition.x);
			writer.WriteInt16((short)modifiedVein.ClusterPosition.y);
			writer.WriteInt16((short)modifiedVein.ClusterPosition.z);
			writer.WriteInt32(modifiedVein.Data.IdHash);
			writer.WriteByte((byte)modifiedVein._minables.Length);
			for (int i = 0; i < modifiedVein._minables.Length; i++)
			{
				modifiedVein._minables[i].Write(writer);
			}
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenApplyingMinedMinables.DisplayString);
		Network.ReadIndex<uint>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			short z = reader.ReadInt16();
			short x2 = reader.ReadInt16();
			short y2 = reader.ReadInt16();
			short z2 = reader.ReadInt16();
			int idHash = reader.ReadInt32();
			byte b = reader.ReadByte();
			Vein vein = new Vein(DataCollection.Get<VeinGenerationData>(idHash), new Vector3Int(x, y, z), new Vector3Int(x2, y2, z2), b);
			for (int j = 0; j < b; j++)
			{
				vein._minables[j] = Minable.Read(reader);
			}
			vein.GenerateBounds();
			vein.IsModified = true;
			Register(vein);
			vein.PopulateLookup();
		}
	}

	public static void DrawDebug()
	{
		if (!IsDrawDebug)
		{
			return;
		}
		_debugVeinsToDrawList.Clear();
		GetVeinsInBounds(MakeRenderBounds(InventoryManager.ParentPosition, _debugRenderRange, _debugRenderRange, _debugRenderRange), ref _debugVeinsToDrawList);
		foreach (Vein debugVeinsToDraw in _debugVeinsToDrawList)
		{
			debugVeinsToDraw?.Draw();
		}
	}

	private void Draw()
	{
		if (Vector3.SqrMagnitude(VeinWorldPosition - InventoryManager.ParentPosition) > (float)(DebugRenderRange * DebugRenderRange))
		{
			return;
		}
		Mathf.Min(24, DebugRenderRange);
		bool flag = false;
		ImGuiExtensions.Rendering.RenderingColor = DebugMinableColor[(uint)Type];
		for (int i = 0; i < _minables.Length; i++)
		{
			Vector3 vector = _minables[i].WorldRenderPosition(VeinWorldPosition) + VoxelConstants.TerrainMeshOffset;
			Vector3 position = _minables[_minables[i].ParentIndex].WorldRenderPosition(VeinWorldPosition) + VoxelConstants.TerrainMeshOffset;
			ImGuiExtensions.Rendering.DrawCube(vector, _minables[i].IsActive ? DebugMinableDrawSize : DebugMinableInactiveSize);
			if (_minables[i].ParentIndex != i)
			{
				ImGuiExtensions.Rendering.DrawClippedLine(ImGuiExtensions.WorldToScreen(vector), ImGuiExtensions.WorldToScreen(position));
			}
			if (i == 0 && flag)
			{
				ImGuiExtensions.Rendering.DrawTextInWorld(StringManager.Get(i), vector, VeinWorldPosition.GetHashCode() + i, 24f);
			}
			if (flag)
			{
				ImGuiExtensions.Rendering.DrawTextInWorld("Index " + StringManager.Get(i) + " (" + StringManager.Get(_minables[i].X) + "," + StringManager.Get(_minables[i].Y) + "," + StringManager.Get(_minables[i].Z) + ")", vector, vector.GetHashCode() + i, 24f);
			}
		}
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
		if (flag)
		{
			DebugSb.Clear();
			DebugSb.Append(EnumCollections.MinableTypes.GetName(Type));
			DebugSb.Newline();
			DebugSb.Append(StringManager.Get(VeinWorldPosition));
			DebugSb.Newline();
			DebugSb.Append("count: ").Append(StringManager.Get(_minables.Length));
			ImGuiExtensions.Rendering.DrawTextInWorld(DebugSb.ToString(), VeinWorldPosition + VoxelConstants.TerrainMeshOffset, VeinWorldPosition.GetHashCode(), 24f);
		}
	}
}
