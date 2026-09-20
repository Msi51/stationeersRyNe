using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TerrainSystem;
using TerrainSystem.Lods;
using UI.ImGuiUi;
using UI.ImGuiUi.Debug;
using UnityEngine;

namespace Objects.Items;

public class TerrainEditor : Tool
{
	[SerializeField]
	private TransformGizmo _transformGizmoPrefab;

	private TransformGizmo _transformGizmo;

	[SerializeField]
	private MeshStamp _meshStampPrefab;

	private MeshStamp _meshStamp;

	[SerializeField]
	private LayerMask _meshStampLayerMask;

	[SerializeField]
	private LavaObject _lavaPrefab;

	private LavaObject _activeLavaObject;

	[SerializeField]
	private LayerMask _terrainLayerMask;

	private Vector3 _position;

	private const float ACTION_INTERVAL_S = 0.1f;

	private float _lastActionTime;

	private Vector3 _randomSize = Vector3.one * ImGuiTerrainEditorTool.Size;

	public const float SMOOTH_SPEED = 5f;

	private readonly List<(Vector3Int worldPos, float density)> _densitiesToSet = new List<(Vector3Int, float)>(512);

	private readonly List<(Vector3Int vector, float power)> _smoothingOffsets = new List<(Vector3Int, float)>(512);

	private readonly List<(Vector3Int vector, float power)> _sphereOffsets = new List<(Vector3Int, float)>(4096);

	public static OpenSimplexNoise SimplexNoise { get; set; }

	public static void ShowWindow(TerrainEditor terrainEditor)
	{
		ImGuiTerrainEditorTool.Show(terrainEditor);
	}

	public static void HideWindow()
	{
		ImGuiTerrainEditorTool.Hide();
	}

	public void ResetMeshStampTransform()
	{
		_meshStamp?.ResetTransform();
		_activeLavaObject?.ResetTransform();
	}

	private void Update()
	{
		if (InventoryManager.ActiveHandSlot == base.ParentSlot)
		{
			switch (ImGuiTerrainEditorTool.DiggingMode)
			{
			case DiggingMode.Smooth:
			case DiggingMode.Paint:
				UpdateSmoothMode();
				break;
			case DiggingMode.Stamp:
			case DiggingMode.Cut:
				UpdateStampCutMode();
				break;
			case DiggingMode.Lava:
				UpdateLavaMode();
				break;
			case DiggingMode.Cracks:
				UpdateCracksMode();
				break;
			}
			CheckUserInput();
		}
		else
		{
			ClearLavaObject();
			ClearMeshStamp();
			ClearTransformGizmo();
		}
	}

	private void UpdateSmoothMode()
	{
		ClearMeshStamp();
		ClearLavaObject();
		ClearTransformGizmo();
		CachePosition();
		ImGuiDebugHelper.DrawWireSphere(_position, ImGuiTerrainEditorTool.Radius);
	}

	private void UpdateCracksMode()
	{
		ClearMeshStamp();
		ClearLavaObject();
		ClearTransformGizmo();
		CachePosition();
		ImGuiDebugHelper.DrawWireSphere(_position, ImGuiTerrainEditorTool.Radius);
	}

	private void UpdateStampCutMode()
	{
		CachePosition();
		PositionMeshStamp();
		PositionTransformGizmo();
		CheckGizmoInteraction();
	}

	private void UpdateLavaMode()
	{
		CachePosition();
		PositionLavaObject();
		PositionTransformGizmo();
		CheckGizmoInteraction();
	}

	private void CachePosition()
	{
		if (!ImGuiTerrainEditorTool.PinPosition)
		{
			Ray cameraRay = InputHelpers.GetCameraRay();
			switch (ImGuiTerrainEditorTool.PositionMode)
			{
			case PositionMode.Raycast:
			{
				RaycastHit hitInfo;
				bool flag = Physics.Raycast(cameraRay, out hitInfo, 100f, _terrainLayerMask);
				_position = (flag ? hitInfo.point : cameraRay.GetPoint(100f));
				break;
			}
			case PositionMode.Distance:
				_position = cameraRay.GetPoint(ImGuiTerrainEditorTool.PositionDistance);
				break;
			}
		}
	}

	private void CheckUserInput()
	{
		if (InputMouse.IsMouseControl)
		{
			return;
		}
		if (ImGuiTerrainEditorTool.ContinuousPlacement)
		{
			if (!(_lastActionTime + 0.1f > Time.time))
			{
				_lastActionTime = Time.time;
				if (Input.GetMouseButton(0))
				{
					DoAction();
				}
				if (Input.GetMouseButton(1) && ImGuiTerrainEditorTool.UndoStack.TryPop(out var result))
				{
					result.Apply();
				}
			}
		}
		else
		{
			if (Input.GetMouseButtonDown(0))
			{
				DoAction();
			}
			if (Input.GetMouseButtonDown(1) && ImGuiTerrainEditorTool.UndoStack.TryPop(out var result2))
			{
				result2.Apply();
			}
		}
	}

	private void DoAction()
	{
		switch (ImGuiTerrainEditorTool.DiggingMode)
		{
		case DiggingMode.Smooth:
			Smooth();
			break;
		case DiggingMode.Stamp:
			Stamp();
			break;
		case DiggingMode.Cut:
			Cut();
			break;
		case DiggingMode.Paint:
			Paint();
			break;
		case DiggingMode.Lava:
			Lava();
			break;
		case DiggingMode.Cracks:
			Cracks();
			break;
		}
	}

	private void Cracks()
	{
		SimplexNoise = new OpenSimplexNoise(ImGuiTerrainEditorTool.Seed);
		Vector3Int vector3Int = _position.FloorToInt();
		int num = ImGuiTerrainEditorTool.Radius + ImGuiTerrainEditorTool.Radius;
		Bounds bounds = new Bounds(vector3Int, new Vector3(num, num, num));
		TerrainEditorUndo terrainEditorUndo = new TerrainEditorUndo(bounds);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num; k++)
				{
					Vector3 vector = bounds.min + new Vector3(i, j, k);
					NodeInfo nodeInfoWorldSpace = VoxelTerrain.GetNodeInfoWorldSpace(vector);
					terrainEditorUndo.SetVoxelValue(i, j, k, nodeInfoWorldSpace.Density, (byte)nodeInfoWorldSpace.NodeType);
					if ((vector - vector3Int).magnitude < (float)ImGuiTerrainEditorTool.Radius)
					{
						float value = GetCracks(vector, ImGuiTerrainEditorTool.crackParam0) + GetCracks(vector, ImGuiTerrainEditorTool.crackParam1);
						value = Mathf.Clamp01(value);
						float num2 = 1f - value;
						if (VoxelTerrain.DensityToFloat(nodeInfoWorldSpace.Density) > num2)
						{
							VoxelTerrain.SetDensityWorldSpace(vector, num2, RoomChangeSource.VoxelRemove, dirtyLods: false, setNodeType: true, VoxelNodeType.Crust);
						}
					}
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(new Vector3(vector3Int.x - ImGuiTerrainEditorTool.Radius, vector3Int.y - ImGuiTerrainEditorTool.Radius, vector3Int.z - ImGuiTerrainEditorTool.Radius), new Vector3(vector3Int.x + ImGuiTerrainEditorTool.Radius, vector3Int.y + ImGuiTerrainEditorTool.Radius, vector3Int.z + ImGuiTerrainEditorTool.Radius));
		ImGuiTerrainEditorTool.UndoStack.Push(terrainEditorUndo);
	}

	private static float GetCracks(Vector3 pos, Vector3 parameter)
	{
		float x = parameter.x;
		float y = parameter.y;
		float z = parameter.z;
		return Mathf.Pow(RocketMath.MapToScale(-1f, 1f, 0f, 1f, SimplexNoise.Evaluate(pos.x * x, pos.y * x, pos.z * x) * y), (z == 0f) ? 1f : z);
	}

	private Vector3 GetMeshStampSize()
	{
		return ImGuiTerrainEditorTool.SizeMode switch
		{
			SizeMode.Range => _randomSize, 
			SizeMode.PerAxis => _randomSize, 
			SizeMode.Constant => Vector3.one * ImGuiTerrainEditorTool.Size, 
			_ => Vector3.one * ImGuiTerrainEditorTool.Size, 
		};
	}

	private void PositionLavaObject()
	{
		if ((object)_activeLavaObject == null)
		{
			_activeLavaObject = UnityEngine.Object.Instantiate(_lavaPrefab);
		}
		_activeLavaObject.transform.position = _position;
		Vector3 size = new Vector3(ImGuiTerrainEditorTool.LavaScaleX, ImGuiTerrainEditorTool.LavaScaleY, ImGuiTerrainEditorTool.LavaScaleZ);
		_activeLavaObject.SetSize(size);
	}

	private void PositionMeshStamp()
	{
		if ((object)_meshStamp == null)
		{
			_meshStamp = UnityEngine.Object.Instantiate(_meshStampPrefab);
		}
		_meshStamp.SetMesh(ImGuiTerrainEditorTool.GetSelectedMesh);
		_meshStamp.Transform.position = _position;
		_meshStamp.SetSize(GetMeshStampSize());
	}

	private void PositionTransformGizmo()
	{
		if ((object)_transformGizmo == null)
		{
			_transformGizmo = UnityEngine.Object.Instantiate(_transformGizmoPrefab);
		}
		if (ImGuiTerrainEditorTool.DiggingMode == DiggingMode.Cut || ImGuiTerrainEditorTool.DiggingMode == DiggingMode.Stamp)
		{
			_transformGizmo.Transform.position = _meshStamp.PivotTransform.position;
			_transformGizmo.Target = _meshStamp.PivotTransform;
		}
		else if (ImGuiTerrainEditorTool.DiggingMode == DiggingMode.Lava)
		{
			_transformGizmo.Transform.position = _activeLavaObject.PivotTransform.position;
			_transformGizmo.Target = _activeLavaObject.PivotTransform;
		}
		Vector3 cameraPosition = CameraController.CameraPosition;
		float num = Vector3.Distance(_transformGizmo.Transform.position, cameraPosition);
		_transformGizmo.SetScale(num * 0.02f);
	}

	private void CheckGizmoInteraction()
	{
		_transformGizmo.CheckInteraction();
	}

	private void AfterStampCutOperationComplete()
	{
		if (ImGuiTerrainEditorTool.RandomRotX)
		{
			_meshStamp.PivotTransform.Rotate(Vector3.right, UnityEngine.Random.Range(0, 360), Space.World);
		}
		if (ImGuiTerrainEditorTool.RandomRotY)
		{
			_meshStamp.PivotTransform.Rotate(Vector3.up, UnityEngine.Random.Range(0, 360), Space.World);
		}
		if (ImGuiTerrainEditorTool.RandomRotZ)
		{
			_meshStamp.PivotTransform.Rotate(Vector3.forward, UnityEngine.Random.Range(0, 360), Space.World);
		}
		_randomSize = ImGuiTerrainEditorTool.SizeMode switch
		{
			SizeMode.Range => UnityEngine.Random.Range(ImGuiTerrainEditorTool.SizeMin, ImGuiTerrainEditorTool.SizeMax) * Vector3.one, 
			SizeMode.PerAxis => new Vector3(UnityEngine.Random.Range(ImGuiTerrainEditorTool.SizeMinX, ImGuiTerrainEditorTool.SizeMaxX), UnityEngine.Random.Range(ImGuiTerrainEditorTool.SizeMinY, ImGuiTerrainEditorTool.SizeMaxY), UnityEngine.Random.Range(ImGuiTerrainEditorTool.SizeMinZ, ImGuiTerrainEditorTool.SizeMaxZ)), 
			_ => ImGuiTerrainEditorTool.Size * Vector3.one, 
		};
	}

	private void ClearMeshStamp()
	{
		if ((object)_meshStamp != null)
		{
			UnityEngine.Object.Destroy(_meshStamp.GameObject);
			_meshStamp = null;
		}
	}

	private void ClearLavaObject()
	{
		if ((object)_activeLavaObject != null)
		{
			UnityEngine.Object.Destroy(_activeLavaObject.gameObject);
			_activeLavaObject = null;
		}
	}

	private void ClearTransformGizmo()
	{
		if ((object)_transformGizmo != null)
		{
			UnityEngine.Object.Destroy(_transformGizmo.GameObject);
			_transformGizmo = null;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ClearMeshStamp();
		ClearLavaObject();
		ClearTransformGizmo();
	}

	private void Cut()
	{
		int num = 1;
		Bounds bounds = _meshStamp.GetBounds();
		Voxeliser voxeliser = new Voxeliser(num, bounds, _meshStampLayerMask, ImGuiTerrainEditorTool.SmoothingIterations);
		TerrainEditorUndo terrainEditorUndo = new TerrainEditorUndo(bounds);
		for (int i = 0; i < voxeliser.Xsize; i++)
		{
			for (int j = 0; j < voxeliser.Ysize; j++)
			{
				for (int k = 0; k < voxeliser.Zsize; k++)
				{
					float num2 = voxeliser.GetVoxel(i, j, k) * (float)num;
					Vector3 vector = new Vector3(i * num, j * num, k * num);
					Vector3 vector2 = bounds.min + vector;
					NodeInfo nodeInfoWorldSpace = VoxelTerrain.GetNodeInfoWorldSpace(vector2);
					terrainEditorUndo.SetVoxelValue(i, j, k, nodeInfoWorldSpace.Density, (byte)nodeInfoWorldSpace.NodeType);
					if (!Mathf.Approximately((int)nodeInfoWorldSpace.Density, 0f))
					{
						VoxelTerrain.SetDensityWorldSpace(vector2, nodeInfoWorldSpace.DensityAsFloat() - num2, RoomChangeSource.VoxelRemove, dirtyLods: false);
					}
					Vein.GetVeinAtPosition(vector2)?.TryRemoveServer(vector2.FloorToInt());
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(bounds.min, bounds.max);
		AfterStampCutOperationComplete();
		ImGuiTerrainEditorTool.UndoStack.Push(terrainEditorUndo);
	}

	private void Lava()
	{
		UnityEngine.Object.Instantiate(_activeLavaObject).Initialize();
	}

	private void Stamp()
	{
		Bounds bounds = _meshStamp.GetBounds();
		Voxeliser voxeliser = new Voxeliser(1f, bounds, _meshStampLayerMask, ImGuiTerrainEditorTool.SmoothingIterations);
		TerrainEditorUndo terrainEditorUndo = new TerrainEditorUndo(bounds);
		VoxelNodeType voxelNodeType = VoxelNodeType.None;
		if (ImGuiTerrainEditorTool.Texture1)
		{
			voxelNodeType |= VoxelNodeType.Dirt;
		}
		if (ImGuiTerrainEditorTool.Texture2)
		{
			voxelNodeType |= VoxelNodeType.Crust;
		}
		if (ImGuiTerrainEditorTool.MacroTexture)
		{
			voxelNodeType |= VoxelNodeType.Macro;
		}
		if (voxelNodeType == VoxelNodeType.None)
		{
			return;
		}
		for (int i = 0; i < voxeliser.Xsize; i++)
		{
			for (int j = 0; j < voxeliser.Ysize; j++)
			{
				for (int k = 0; k < voxeliser.Zsize; k++)
				{
					float voxel = voxeliser.GetVoxel(i, j, k);
					Vector3 worldPosition = bounds.min + new Vector3(i, j, k);
					NodeInfo nodeInfoWorldSpace = VoxelTerrain.GetNodeInfoWorldSpace(worldPosition);
					terrainEditorUndo.SetVoxelValue(i, j, k, nodeInfoWorldSpace.Density, (byte)nodeInfoWorldSpace.NodeType);
					if (nodeInfoWorldSpace.DensityAsFloat() < voxel)
					{
						VoxelTerrain.SetDensityWorldSpace(worldPosition, voxel, RoomChangeSource.VoxelAdd, dirtyLods: false, setNodeType: true, voxelNodeType);
					}
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(bounds.min, bounds.max);
		AfterStampCutOperationComplete();
		ImGuiTerrainEditorTool.UndoStack.Push(terrainEditorUndo);
	}

	private void Smooth()
	{
		GetSphereOffsets(ImGuiTerrainEditorTool.Radius, _sphereOffsets);
		_densitiesToSet.Clear();
		Vector3Int vector3Int = _position.FloorToInt();
		int num = ImGuiTerrainEditorTool.Radius + ImGuiTerrainEditorTool.Radius;
		Bounds bounds = new Bounds(vector3Int, new Vector3(num, num, num));
		TerrainEditorUndo terrainEditorUndo = new TerrainEditorUndo(bounds);
		foreach (var sphereOffset in _sphereOffsets)
		{
			Vector3Int vector3Int2 = vector3Int + sphereOffset.vector;
			float num2 = 0f;
			int num3 = 0;
			GetSphereOffsets(ImGuiTerrainEditorTool.SmoothingStrength, _smoothingOffsets);
			foreach (var smoothingOffset in _smoothingOffsets)
			{
				Vector3Int worldPositionInt = vector3Int2 + smoothingOffset.vector;
				num2 += VoxelTerrain.GetDensityWorldSpace(worldPositionInt);
				num3++;
			}
			float num4 = num2 / (float)num3;
			if (num4 < 0.2f)
			{
				num4 = 0f;
			}
			_densitiesToSet.Add((vector3Int2, num4));
		}
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num; k++)
				{
					NodeInfo nodeInfoWorldSpace = VoxelTerrain.GetNodeInfoWorldSpace(bounds.min + new Vector3(i, j, k));
					terrainEditorUndo.SetVoxelValue(i, j, k, nodeInfoWorldSpace.Density, (byte)nodeInfoWorldSpace.NodeType);
				}
			}
		}
		foreach (var item in _densitiesToSet)
		{
			if (!LodManager.IsAtOrUnderBedrock(item.worldPos))
			{
				float densityWorldSpace = VoxelTerrain.GetDensityWorldSpace(item.worldPos);
				VoxelTerrain.SetDensityWorldSpace(item.worldPos, Mathf.Lerp(densityWorldSpace, item.density, 0.5f), RoomChangeSource.VoxelRemove, dirtyLods: false);
				if (Mathf.Approximately(item.density, 0f))
				{
					Vein.GetVeinAtPosition(item.worldPos)?.TryRemoveServer(item.worldPos);
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(new Vector3(vector3Int.x - ImGuiTerrainEditorTool.Radius, vector3Int.y - ImGuiTerrainEditorTool.Radius, vector3Int.z - ImGuiTerrainEditorTool.Radius), new Vector3(vector3Int.x + ImGuiTerrainEditorTool.Radius, vector3Int.y + ImGuiTerrainEditorTool.Radius, vector3Int.z + ImGuiTerrainEditorTool.Radius));
		ImGuiTerrainEditorTool.UndoStack.Push(terrainEditorUndo);
	}

	private void Paint()
	{
		VoxelNodeType voxelNodeType = VoxelNodeType.None;
		if (ImGuiTerrainEditorTool.Texture1)
		{
			voxelNodeType |= VoxelNodeType.Dirt;
		}
		if (ImGuiTerrainEditorTool.Texture2)
		{
			voxelNodeType |= VoxelNodeType.Crust;
		}
		if (ImGuiTerrainEditorTool.MacroTexture)
		{
			voxelNodeType |= VoxelNodeType.Macro;
		}
		if (voxelNodeType == VoxelNodeType.None)
		{
			return;
		}
		Vector3Int vector3Int = _position.FloorToInt();
		int num = ImGuiTerrainEditorTool.Radius + ImGuiTerrainEditorTool.Radius;
		Bounds bounds = new Bounds(vector3Int, new Vector3(num, num, num));
		TerrainEditorUndo terrainEditorUndo = new TerrainEditorUndo(bounds);
		System.Random random = new System.Random(vector3Int.GetHashCode());
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num; k++)
				{
					Vector3 vector = bounds.min + new Vector3(i, j, k);
					NodeInfo nodeInfoWorldSpace = VoxelTerrain.GetNodeInfoWorldSpace(vector);
					terrainEditorUndo.SetVoxelValue(i, j, k, nodeInfoWorldSpace.Density, (byte)nodeInfoWorldSpace.NodeType);
					double num2 = (double)ImGuiTerrainEditorTool.Radius - (double)ImGuiTerrainEditorTool.Radius * (random.NextDouble() * (double)Mathf.Clamp01((float)ImGuiTerrainEditorTool.RadiusRandomPercent / 100f));
					if ((double)(vector - vector3Int).magnitude < num2 && VoxelTerrain.GetSize(nodeInfoWorldSpace.Depth) == 1)
					{
						VoxelTerrain.SetDensityWorldSpace(vector, VoxelTerrain.DensityToFloat(nodeInfoWorldSpace.Density), RoomChangeSource.VoxelAdd, dirtyLods: false, setNodeType: true, voxelNodeType);
					}
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(new Vector3(vector3Int.x - ImGuiTerrainEditorTool.Radius, vector3Int.y - ImGuiTerrainEditorTool.Radius, vector3Int.z - ImGuiTerrainEditorTool.Radius), new Vector3(vector3Int.x + ImGuiTerrainEditorTool.Radius, vector3Int.y + ImGuiTerrainEditorTool.Radius, vector3Int.z + ImGuiTerrainEditorTool.Radius));
		ImGuiTerrainEditorTool.UndoStack.Push(terrainEditorUndo);
	}

	private void GetSphereOffsets(int radius, List<(Vector3Int vector, float power)> sphereOffsets)
	{
		sphereOffsets.Clear();
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				for (int k = -radius; k <= radius; k++)
				{
					Vector3Int item = new Vector3Int(i, j, k);
					float magnitude = item.magnitude;
					if (!(magnitude > (float)radius))
					{
						float item2 = 1f - magnitude / (float)radius;
						sphereOffsets.Add((item, item2));
					}
				}
			}
		}
	}
}
