using System;
using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts;
using ImGuiNET;
using Objects.Items;
using TerrainSystem;
using UnityEngine;

namespace UI.ImGuiUi;

public static class ImGuiTerrainEditorTool
{
	public static DiggingMode DiggingMode;

	public static PositionMode PositionMode;

	public static float PositionDistance = 10f;

	public static bool PinPosition = false;

	public static bool ContinuousPlacement;

	public static int SmoothingStrength = 2;

	public static int Radius = 5;

	private const int SMOOTHING_STRENGTH_DEFAULT = 2;

	private const int RADIUS_DEFAULT = 5;

	public static int SmoothingIterations = 1;

	public static float Size = 5f;

	public static bool RandomRotX;

	public static bool RandomRotY;

	public static bool RandomRotZ;

	public static SizeMode SizeMode;

	public static float SizeMin = 5f;

	public static float SizeMax = 5f;

	public static float SizeMinX = 5f;

	public static float SizeMaxX = 5f;

	public static float SizeMinY = 5f;

	public static float SizeMaxY = 5f;

	public static float SizeMinZ = 5f;

	public static float SizeMaxZ = 5f;

	private const int SMOOTHING_ITERATIONS_DEFAULT = 1;

	private const int SIZE_DEFAULT = 5;

	private const int POSITION_DISTANCE_DEFAULT = 10;

	private const bool PIN_POSITION_DEFAULT = false;

	public static bool Texture1 = true;

	public static bool Texture2;

	public static bool MacroTexture;

	private static TerrainEditor _currentEditorTool;

	private static MeshBrowser _meshBrowser = new MeshBrowser(Application.streamingAssetsPath + "/Meshes/Terrain");

	private static bool _meshBrowserShowing;

	public static float LavaScaleX = 1f;

	public static float LavaScaleY = 1f;

	public static float LavaScaleZ = 1f;

	public static int RadiusRandomPercent = 25;

	public static Stack<TerrainEditorUndo> UndoStack = new Stack<TerrainEditorUndo>(1024);

	public static int Seed = 145399;

	public static Vector3 crackParam0 = new Vector3(0.055f, 30f, -10f);

	public static Vector3 crackParam1 = new Vector3(0.055f, 0.8f, 4f);

	private static string _scaleString = 1f.ToString(CultureInfo.CurrentCulture);

	private static Action DoubleClickHandler = MeshPathSelected;

	public static bool IsShowing { get; private set; }

	public static Mesh GetSelectedMesh => _meshBrowser.LoadedMesh;

	public static void Show(TerrainEditor terrainEditor)
	{
		IsShowing = true;
		_currentEditorTool = terrainEditor;
	}

	public static void Hide()
	{
		IsShowing = false;
		_currentEditorTool = null;
		CloseMeshBrowser();
	}

	public static void Reset()
	{
		DiggingMode = DiggingMode.None;
		PositionMode = PositionMode.Raycast;
		PinPosition = false;
		PositionDistance = 10f;
		SmoothingStrength = 2;
		Radius = 5;
		SmoothingIterations = 1;
		Size = 5f;
	}

	public static void Draw()
	{
		DrawMain();
		if (_meshBrowserShowing)
		{
			DrawMeshBrowser();
		}
	}

	private static void DrawMain()
	{
		if (EnsureToolIsSet())
		{
			ImGui.Begin("Terrain Tool", (ImGuiWindowFlags)18688);
			Vector2 size = new Vector2(200f, 600f);
			ImGui.SetWindowSize(size, ImGuiCond.Once);
			ImGui.SetWindowPos(new Vector2((float)Screen.width - size.x, 80f), ImGuiCond.Once);
			ImguiHelper.DrawCombo("Mode", ref DiggingMode, EnumCollections.DiggingModes);
			ImguiHelper.DrawCombo("Position", ref PositionMode, EnumCollections.PositionModes);
			if (PositionMode == PositionMode.Distance)
			{
				ImGui.SliderFloat("###PositionDistance", ref PositionDistance, 5f, 100f);
			}
			DiggingMode diggingMode = DiggingMode;
			if (diggingMode == DiggingMode.Stamp || diggingMode == DiggingMode.Paint)
			{
				Separator();
				ImGui.Checkbox("Texture1", ref Texture1);
				ImGui.Checkbox("Texture2", ref Texture2);
				ImGui.Checkbox("MacroTexture", ref MacroTexture);
				Separator();
			}
			ImGui.Checkbox("Pin", ref PinPosition);
			ImGui.Checkbox("Continuous", ref ContinuousPlacement);
			Separator();
			switch (DiggingMode)
			{
			case DiggingMode.Smooth:
				DrawSmoothMode();
				break;
			case DiggingMode.Stamp:
				DrawStampCutMode();
				break;
			case DiggingMode.Cut:
				DrawStampCutMode();
				break;
			case DiggingMode.Paint:
				DrawPaintMode();
				break;
			case DiggingMode.Lava:
				DrawLavaMode();
				break;
			case DiggingMode.Cracks:
				DrawCracksMode();
				break;
			}
			if (ImGui.Button("Undo") && UndoStack.TryPop(out var result))
			{
				result.Apply();
			}
			ImGui.End();
		}
	}

	private static void DrawCracksMode()
	{
		ImGui.SliderInt("###Radius", ref Radius, 1, 50);
		ImGui.InputInt("Seed###crackSeed", ref Seed);
		ImGui.InputFloat3("param0###crack0", ref crackParam0);
		ImGui.InputFloat3("param1###crack1", ref crackParam1);
	}

	private static void DrawPaintMode()
	{
		ImGui.Text("Radius");
		ImGui.SliderInt("###Radius", ref Radius, 1, 50);
		ImGui.Text("Randomize");
		ImGui.SliderInt("###RadiusRandom", ref RadiusRandomPercent, 0, 100);
	}

	private static void DrawSmoothMode()
	{
		ImGui.Text("Radius");
		ImGui.SliderInt("###Radius", ref Radius, 1, 20);
		ImGui.Text("Strength");
		ImGui.SliderInt("###Strength", ref SmoothingStrength, 1, 4);
	}

	private static void DrawLavaMode()
	{
		ImGui.Text("Scale");
		ImGui.SliderFloat("Z###LavaScaleX", ref LavaScaleX, 1f, 100f);
		ImGui.SliderFloat("Y###LavaScaleY", ref LavaScaleY, 1f, 100f);
		ImGui.SliderFloat("Z###LavaScaleZ", ref LavaScaleZ, 1f, 100f);
		ImGui.Separator();
		if (ImGui.Button("Reset Transform"))
		{
			_currentEditorTool.ResetMeshStampTransform();
		}
		ImGui.Separator();
		if (ImGui.Button("Save Lava Lakes"))
		{
			VoxelTerrain.Instance.SaveLavaLakes();
		}
	}

	private static void DrawStampCutMode()
	{
		ImGui.Text("Mesh");
		ImGui.Text(_meshBrowser.LoadedMesh?.name ?? "None Selected");
		ImGui.SameLine();
		if (ImGui.Button("L"))
		{
			OpenMeshBrowser();
		}
		Separator();
		ImGui.Text("Smoothing Steps");
		ImGui.SliderInt("###SmoothingIterations", ref SmoothingIterations, 1, 4);
		ImguiHelper.DrawCombo("Size", ref SizeMode, EnumCollections.SizeModes);
		switch (SizeMode)
		{
		case SizeMode.Constant:
			ImGui.SliderFloat("###ConstantSize", ref Size, 1f, 1000f);
			break;
		case SizeMode.Range:
			ImGui.SliderFloat("Min###SizeMin", ref SizeMin, 1f, 1000f);
			ImGui.SliderFloat("Max###SizeMax", ref SizeMax, 1f, 1000f);
			break;
		case SizeMode.PerAxis:
			ImGui.SliderFloat("Min X###SizeMinX", ref SizeMinX, 1f, 1000f);
			ImGui.SliderFloat("Max X###SizeMaxX", ref SizeMaxX, 1f, 1000f);
			ImGui.SliderFloat("Min Y###SizeMinY", ref SizeMinY, 1f, 1000f);
			ImGui.SliderFloat("Max Y###SizeMaxY", ref SizeMaxY, 1f, 1000f);
			ImGui.SliderFloat("Min Z###SizeMinZ", ref SizeMinZ, 1f, 1000f);
			ImGui.SliderFloat("Max Z###SizeMaxZ", ref SizeMaxZ, 1f, 1000f);
			break;
		}
		Separator();
		ImGui.Text("Random Rotation");
		ImGui.Checkbox("X", ref RandomRotX);
		ImGui.SameLine();
		ImGui.Checkbox("Y", ref RandomRotY);
		ImGui.SameLine();
		ImGui.Checkbox("Z", ref RandomRotZ);
		if (ImGui.Button("Reset Transform"))
		{
			_currentEditorTool.ResetMeshStampTransform();
		}
	}

	private static bool EnsureToolIsSet()
	{
		if ((object)_currentEditorTool == null || _currentEditorTool.IsBeingDestroyed)
		{
			Hide();
			return false;
		}
		return true;
	}

	private static void Separator()
	{
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
	}

	private static void MeshPathSelected()
	{
		CloseMeshBrowser();
	}

	private static void OpenMeshBrowser()
	{
		_meshBrowser.Clear();
		_meshBrowser.Init();
		_meshBrowserShowing = true;
		MeshBrowser meshBrowser = _meshBrowser;
		meshBrowser.OnDoubleClicked = (Action)Delegate.Combine(meshBrowser.OnDoubleClicked, DoubleClickHandler);
	}

	private static void CloseMeshBrowser()
	{
		_meshBrowser.Clear();
		_meshBrowserShowing = false;
		MeshBrowser meshBrowser = _meshBrowser;
		meshBrowser.OnDoubleClicked = (Action)Delegate.Remove(meshBrowser.OnDoubleClicked, DoubleClickHandler);
	}

	public static void Clear()
	{
		foreach (TerrainEditorUndo item in UndoStack)
		{
			item.Clear();
		}
		UndoStack.Clear();
	}

	private static void DrawMeshBrowser()
	{
		ImGui.Begin("Mesh Browser", ref _meshBrowserShowing, (ImGuiWindowFlags)18688);
		if (ImGui.IsWindowFocused() && KeyManager.GetButton(KeyCode.Escape))
		{
			CloseMeshBrowser();
			ImGui.End();
			return;
		}
		Vector2 size = new Vector2(1024f, 768f);
		ImGui.SetWindowSize(size, ImGuiCond.Once);
		ImGui.SetWindowPos(new Vector2((float)Screen.width - size.x, 80f), ImGuiCond.Once);
		ImGui.Text("Selected File: " + (string.IsNullOrEmpty(_meshBrowser.SelectedPath) ? "None" : _meshBrowser.SelectedPath));
		ImGui.Separator();
		ImGui.Columns(2);
		ImGui.SetColumnWidth(0, 128f);
		ImGui.SetColumnWidth(1, 128f);
		if (ImguiHelper.DrawTextInput("Scale", ref _scaleString, 5u, isError: false, canBeNull: true, ImGuiInputTextFlags.CharsDecimal, "Import scale") && float.TryParse(_scaleString, NumberStyles.Any, CultureInfo.CurrentCulture, out var result) && !Mathf.Approximately(_meshBrowser.Scale, result))
		{
			_meshBrowser.Scale = Mathf.Clamp(result, 0.01f, 1000f);
			_meshBrowser.Reload();
		}
		ImGui.Columns(1);
		ImGui.Separator();
		_meshBrowser.Draw();
		ImGui.End();
	}
}
