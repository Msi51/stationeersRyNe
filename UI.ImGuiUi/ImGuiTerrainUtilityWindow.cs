using System;
using System.Globalization;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using TerrainSystem;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;
using Util;

namespace UI.ImGuiUi;

public class ImGuiTerrainUtilityWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	public static ImGuiTerrainUtilityWindow Window = new ImGuiTerrainUtilityWindow();

	private static ImGuiDirectoryBrowser _saveLocationBrowser = new ImGuiDirectoryBrowser(Application.streamingAssetsPath + "/Worlds");

	private static ImGuiFileBrowserWindow _fileBrowser = new ImGuiFileBrowserWindow(ImGuiFileBrowserSettings.Folders(SteamTransport.WorkshopType.Mod.GetLocalDirInfo().ToString()));

	private static ImGuiFileBrowserWindow _heightmapBrowser = new ImGuiFileBrowserWindow(ImGuiFileBrowserSettings.AnyType(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)), "Choose a heightmap");

	private static ImGuiFileBrowserWindow _crustTypeBrowser = new ImGuiFileBrowserWindow(ImGuiFileBrowserSettings.PngOnly(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)), "Choose a crust type map");

	private static ImGuiFileBrowserWindow _crackBrowser = new ImGuiFileBrowserWindow(ImGuiFileBrowserSettings.PngOnly(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)), "Choose a crack map");

	private static ImGuiFileBrowserWindow _lavaHeightBrowser = new ImGuiFileBrowserWindow(ImGuiFileBrowserSettings.PngOnly(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)), "Choose a lava height map");

	private static bool _isLiveImportCurrent;

	private static Texture2D _heightMapTexture;

	private static Texture2D _lavaHeightMapTexture;

	private static Texture2D _crustTypeMapTexture;

	private static Texture2D _crackMapTexture;

	public static int CrackDepth = 16;

	private static string _workshopItemTitle = "Title";

	private static string _workshopItemDescription = "Description";

	private static float _publishProgress;

	private static bool _publishing;

	private static string _saveResult = "";

	private static string _newFolderName;

	public static VoxelOctree ImportedData;

	private static VoxelNodeType _fillType = VoxelNodeType.Dirt;

	private static string _minHeightString = 8f.ToString(CultureInfo.CurrentCulture);

	private static string _maxHeightString = 128f.ToString(CultureInfo.CurrentCulture);

	private static string _minLavaHeightString = 8f.ToString(CultureInfo.CurrentCulture);

	private static string _maxLavaHeightString = 128f.ToString(CultureInfo.CurrentCulture);

	private static string _blurRadiusString = 1.ToString(CultureInfo.CurrentCulture);

	private static string _terrainSizeString = 4096.ToString(CultureInfo.CurrentCulture);

	public static bool IsGenerating => VoxelTerrainHeightmapImporter.CurrentTask != VoxelTerrainHeightmapImporter.HeightmapImportTask.None;

	public ImGuiTerrainUtilityWindow()
		: base("Terrain Tools", new Vector2(1024f, 768f))
	{
	}

	public override void OnOpen()
	{
		_saveLocationBrowser.Init();
		SteamTransport.WorkshopProgress.ProgressEvent = (Action<bool, float>)Delegate.Combine(SteamTransport.WorkshopProgress.ProgressEvent, new Action<bool, float>(UpdateProgressBar));
		ImGuiFileBrowserWindow heightmapBrowser = _heightmapBrowser;
		heightmapBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Combine(heightmapBrowser.OnFileDoubleClicked, new Action<string>(HeightMapSelected));
		ImGuiFileBrowserWindow heightmapBrowser2 = _heightmapBrowser;
		heightmapBrowser2.OnButtonClicked = (Action<string>)Delegate.Combine(heightmapBrowser2.OnButtonClicked, new Action<string>(HeightMapSelected));
		ImGuiFileBrowserWindow crustTypeBrowser = _crustTypeBrowser;
		crustTypeBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Combine(crustTypeBrowser.OnFileDoubleClicked, new Action<string>(CrustTypeMapSelected));
		ImGuiFileBrowserWindow crustTypeBrowser2 = _crustTypeBrowser;
		crustTypeBrowser2.OnButtonClicked = (Action<string>)Delegate.Combine(crustTypeBrowser2.OnButtonClicked, new Action<string>(CrustTypeMapSelected));
		ImGuiFileBrowserWindow crackBrowser = _crackBrowser;
		crackBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Combine(crackBrowser.OnFileDoubleClicked, new Action<string>(CrackMapSelected));
		ImGuiFileBrowserWindow crackBrowser2 = _crackBrowser;
		crackBrowser2.OnButtonClicked = (Action<string>)Delegate.Combine(crackBrowser2.OnButtonClicked, new Action<string>(CrackMapSelected));
		ImGuiFileBrowserWindow lavaHeightBrowser = _lavaHeightBrowser;
		lavaHeightBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Combine(lavaHeightBrowser.OnFileDoubleClicked, new Action<string>(LavaHeightMapSelected));
		ImGuiFileBrowserWindow lavaHeightBrowser2 = _lavaHeightBrowser;
		lavaHeightBrowser2.OnButtonClicked = (Action<string>)Delegate.Combine(lavaHeightBrowser2.OnButtonClicked, new Action<string>(LavaHeightMapSelected));
	}

	public override void OnClose()
	{
		ImGuiFileBrowserWindow heightmapBrowser = _heightmapBrowser;
		heightmapBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Remove(heightmapBrowser.OnFileDoubleClicked, new Action<string>(HeightMapSelected));
		ImGuiFileBrowserWindow heightmapBrowser2 = _heightmapBrowser;
		heightmapBrowser2.OnButtonClicked = (Action<string>)Delegate.Remove(heightmapBrowser2.OnButtonClicked, new Action<string>(HeightMapSelected));
		ImGuiFileBrowserWindow crustTypeBrowser = _crustTypeBrowser;
		crustTypeBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Remove(crustTypeBrowser.OnFileDoubleClicked, new Action<string>(CrustTypeMapSelected));
		ImGuiFileBrowserWindow crustTypeBrowser2 = _crustTypeBrowser;
		crustTypeBrowser2.OnButtonClicked = (Action<string>)Delegate.Remove(crustTypeBrowser2.OnButtonClicked, new Action<string>(CrustTypeMapSelected));
		ImGuiFileBrowserWindow crackBrowser = _crackBrowser;
		crackBrowser.OnFileDoubleClicked = (Action<string>)Delegate.Remove(crackBrowser.OnFileDoubleClicked, new Action<string>(CrackMapSelected));
		ImGuiFileBrowserWindow crackBrowser2 = _crackBrowser;
		crackBrowser2.OnButtonClicked = (Action<string>)Delegate.Remove(crackBrowser2.OnButtonClicked, new Action<string>(CrackMapSelected));
		_saveLocationBrowser.Clear();
		SteamTransport.WorkshopProgress.ProgressEvent = (Action<bool, float>)Delegate.Remove(SteamTransport.WorkshopProgress.ProgressEvent, new Action<bool, float>(UpdateProgressBar));
		_isLiveImportCurrent = false;
		if (GameManager.GameState == GameState.Paused)
		{
			WorldManager.SetGamePause(pauseGame: false);
		}
		_heightMapTexture = null;
		_crustTypeMapTexture = null;
		_fileBrowser.Clear();
		_fileBrowser.CloseWindow();
		_heightmapBrowser.Clear();
		_heightmapBrowser.CloseWindow();
		_crustTypeBrowser.Clear();
		_crustTypeBrowser.CloseWindow();
		ClearData();
	}

	public override void DrawContent()
	{
		if (ImGui.BeginTabBar("###TabBar", (ImGuiTabBarFlags)40))
		{
			GameState gameState = GameManager.GameState;
			ImGui.BeginDisabled(gameState == GameState.Running || gameState == GameState.Paused);
			if (ImGui.BeginTabItem("Import###ImportTab"))
			{
				DrawImportTab();
				ImGui.EndTabItem();
			}
			ImGui.EndDisabled();
			gameState = GameManager.GameState;
			ImGui.BeginDisabled(gameState != GameState.Running && gameState != GameState.Paused);
			if (ImGui.BeginTabItem("Import Live###ImportLiveTab"))
			{
				DrawImportLiveTab();
				ImGui.EndTabItem();
			}
			ImGui.EndDisabled();
			if (GameManager.GameState == GameState.None)
			{
				ImGui.BeginDisabled(ImportedData == null || IsGenerating);
			}
			else
			{
				ImGui.BeginDisabled(!_isLiveImportCurrent);
			}
			if (ImGui.BeginTabItem("Save###SaveTab"))
			{
				DrawSaveTab();
				ImGui.EndTabItem();
			}
			ImGui.EndDisabled();
			if (ImGui.BeginTabItem("Workshop###WorkshopTab"))
			{
				DrawWorkshopTab();
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
	}

	private static void HeightMapSelected(string path)
	{
		try
		{
			if (int.TryParse(_terrainSizeString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result) && LoadTextureFromPath.LoadRaw(path, TextureFormat.RFloat, result, result, out var texture))
			{
				_heightMapTexture = texture;
			}
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception);
		}
	}

	private static void LavaHeightMapSelected(string path)
	{
		try
		{
			if (LoadTextureFromPath.Load(path, out var texture))
			{
				_lavaHeightMapTexture = texture;
			}
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception);
		}
	}

	private static void CrustTypeMapSelected(string path)
	{
		try
		{
			if (LoadTextureFromPath.Load(path, out var texture))
			{
				_crustTypeMapTexture = texture;
			}
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception);
		}
	}

	private static void CrackMapSelected(string path)
	{
		try
		{
			if (LoadTextureFromPath.Load(path, out var texture))
			{
				_crackMapTexture = texture;
			}
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception);
		}
	}

	private static void DrawWorkshopTab()
	{
		ImGui.Text("Help?");
		if (ImGui.IsItemHovered())
		{
			ImguiHelper.ShowTooltip("Select a root mod folder from the file browser.\nFolder structure should be as follows:\nModName\n- About\n-- About.xml\n- GameData\n-- WorldName\n--- WorldName.xml (this is the worldsettings file)\n--- Terrain\n---- Terrain0.dat (generated from heightmap)\n--- Textures\n---- SomeTexture.png (reference these in the worldsettings)");
		}
		ImGui.Separator();
		if (ImGui.Button("Pick###PickFolderButton") && !_fileBrowser.IsShowing)
		{
			ImGuiWindowManager.Open(_fileBrowser);
		}
		ImGui.SameLine();
		ImGui.Text("Selected Folder: ");
		ImGui.SameLine();
		ImGui.Text(_fileBrowser.PathSelected ? _fileBrowser.SelectedPath : "None");
		ImGui.Separator();
		ImGui.Text("Title");
		ImGui.PushItemWidth(-1f);
		ImGui.InputText("###TitleText", ref _workshopItemTitle, 50u);
		ImGui.PopItemWidth();
		ImGui.Text("Description");
		ImGui.InputTextMultiline("###DescriptionText", ref _workshopItemDescription, 200u, new Vector2(ImGui.GetContentRegionAvail().x, 200f));
		ImGui.BeginDisabled(_publishing);
		if (ImGui.Button("Publish"))
		{
			string text = _fileBrowser.SelectedPath + "/About/About.xml";
			if (GetModAbout(text, out var modAbout))
			{
				PublishTask(new SteamTransport.WorkShopItemDetail
				{
					Title = _workshopItemTitle,
					Path = _fileBrowser.SelectedPath,
					PreviewPath = null,
					Description = _workshopItemDescription,
					PublishedFileId = modAbout.WorkshopHandle,
					Type = SteamTransport.WorkshopType.Mod
				}, text).Forget();
			}
		}
		ImGui.EndDisabled();
		if (_publishing)
		{
			ImGui.SameLine();
			ImGui.ProgressBar(_publishProgress, new Vector2(ImGui.GetContentRegionAvail().x, 0f));
		}
	}

	private static async UniTaskVoid PublishTask(SteamTransport.WorkShopItemDetail itemDetail, string aboutPath)
	{
		_publishing = true;
		_publishProgress = 0f;
		var (flag, num, _) = await SteamTransport.Workshop_PublishItemAsync(itemDetail);
		if (flag)
		{
			itemDetail.PublishedFileId = num;
			UpdateAboutData(aboutPath, num);
		}
		_publishProgress = 0f;
		_publishing = false;
	}

	private static void UpdateProgressBar(bool isDeleting, float value)
	{
		_publishProgress = value;
	}

	private static bool GetModAbout(string path, out ModAbout modAbout)
	{
		if (File.Exists(path))
		{
			ModAbout modAbout2 = XmlSerialization.Deserialize<ModAbout>(path, "ModMetadata");
			if (modAbout2 != null)
			{
				modAbout = modAbout2;
				return true;
			}
		}
		modAbout = null;
		return false;
	}

	private static void UpdateAboutData(string aboutPath, ulong fileId)
	{
		if (GetModAbout(aboutPath, out var modAbout))
		{
			modAbout.WorkshopHandle = fileId;
			modAbout.SaveXml(aboutPath);
		}
	}

	private static void DrawSaveTab()
	{
		ImGui.Text("Target Folder: ");
		ImGui.SameLine();
		ImGui.Text(_saveLocationBrowser.SelectedPath);
		if (!string.IsNullOrEmpty(_saveResult))
		{
			ImGui.Separator();
			ImGui.TextColored(ImGuiColor.Float4.Green, _saveResult);
			ImGui.Separator();
		}
		if (ImGui.Button("Save Terrain Data"))
		{
			VoxelOctree octree = ((GameManager.GameState == GameState.None) ? ImportedData : VoxelTerrain.Octree);
			VoxelTerrainHeightmapImporter.SaveTerrain(_saveLocationBrowser.SelectedPath, octree, out var result);
			_saveResult = result;
			_isLiveImportCurrent = false;
			if (GameManager.GameState == GameState.Paused)
			{
				WorldManager.SetGamePause(pauseGame: false);
			}
		}
		ImGui.Separator();
		ImGui.Columns(2);
		if (ImGui.Button("+###AddFolder") && !string.IsNullOrEmpty(_newFolderName) && !Directory.Exists(_saveLocationBrowser.SelectedPath + "\\" + _newFolderName))
		{
			string selectedPath = _saveLocationBrowser.SelectedPath;
			string path = _saveLocationBrowser.SelectedPath + "\\" + _newFolderName;
			Directory.CreateDirectory(_saveLocationBrowser.SelectedPath + "\\" + _newFolderName);
			if (!_saveLocationBrowser.TrySetCurrentPath(path))
			{
				_saveLocationBrowser.TrySetCurrentPath(selectedPath);
			}
		}
		ImGui.SameLine();
		ImguiHelper.DrawTextInput("", ref _newFolderName, 128u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "New folder name");
		ImGui.Columns(1);
		ImGui.Separator();
		ImGui.Text("Select Target Folder");
		_saveLocationBrowser.Draw();
	}

	private static void DrawImportLiveTab()
	{
		ImGui.BeginDisabled(_isLiveImportCurrent);
		if (ImGui.Button("Import"))
		{
			WorldManager.SetGamePause(pauseGame: true);
			VoxelTerrain.Octree.CopyAll();
			_isLiveImportCurrent = true;
		}
		ImGui.EndDisabled();
		ImGui.BeginDisabled(!_isLiveImportCurrent);
		if (ImGui.Button("Clear and Unpause"))
		{
			_isLiveImportCurrent = false;
			WorldManager.SetGamePause(pauseGame: false);
		}
		ImGui.EndDisabled();
	}

	private static void DrawImportTab()
	{
		ImguiHelper.DrawTextInput("Terrain Size", ref _terrainSizeString, 5u, isError: false, canBeNull: false, ImGuiInputTextFlags.None, "Size of the terrain");
		if (ImGui.Button("Choose Heightmap###ChooseHeightmapButton") && !_heightmapBrowser.IsShowing)
		{
			ImGuiWindowManager.Open(_heightmapBrowser);
		}
		ImGui.SameLine();
		ImGui.Text(_heightmapBrowser.PathSelected ? _heightmapBrowser.SelectedPath : "None Selected");
		if ((bool)_heightMapTexture)
		{
			DrawTexturePreview(_heightMapTexture);
		}
		ImGui.Separator();
		if (ImGui.Button("Choose Crust Type###ChooseCrustTypeButton") && !_crustTypeBrowser.IsShowing)
		{
			ImGuiWindowManager.Open(_crustTypeBrowser);
		}
		ImGui.SameLine();
		ImGui.Text(_crustTypeBrowser.PathSelected ? _crustTypeBrowser.SelectedPath : "None Selected");
		if ((bool)_crustTypeMapTexture)
		{
			DrawTexturePreview(_crustTypeMapTexture);
		}
		ImGui.Separator();
		if (ImGui.Button("Choose Cracks###ChooseCrackButton") && !_crackBrowser.IsShowing)
		{
			ImGuiWindowManager.Open(_crackBrowser);
		}
		ImGui.SameLine();
		ImGui.Text(_crackBrowser.PathSelected ? _crackBrowser.SelectedPath : "None Selected");
		if ((bool)_crackMapTexture)
		{
			DrawTexturePreview(_crackMapTexture);
		}
		ImGui.Separator();
		ImGui.Separator();
		if (ImGui.Button("Choose Lava Height###ChooseLavaHeightTypeButton") && !_lavaHeightBrowser.IsShowing)
		{
			ImGuiWindowManager.Open(_lavaHeightBrowser);
		}
		ImGui.SameLine();
		ImGui.Text(_lavaHeightBrowser.PathSelected ? _lavaHeightBrowser.SelectedPath : "None Selected");
		if ((bool)_lavaHeightMapTexture)
		{
			DrawTexturePreview(_lavaHeightMapTexture);
		}
		ImGui.Columns(2);
		ImGui.SetColumnWidth(0, 128f);
		ImGui.SetColumnWidth(1, 128f);
		ImguiHelper.DrawTextInput("Min Height", ref _minHeightString, 4u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "Terrain height when pixel is fully black");
		ImguiHelper.DrawTextInput("Max Height", ref _maxHeightString, 4u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "Terrain height when pixel is fully white");
		ImguiHelper.DrawCombo("Fill Type", ref _fillType, EnumCollections.VoxelNodeTypes);
		if ((bool)_lavaHeightMapTexture)
		{
			ImguiHelper.DrawTextInput("Min Lava Height", ref _minLavaHeightString, 4u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "Terrain height when pixel is fully black");
			ImguiHelper.DrawTextInput("Max Lava Height", ref _maxLavaHeightString, 4u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "Terrain height when pixel is fully white");
		}
		ImGui.Columns(1);
		ImGui.Separator();
		ImguiHelper.DrawTextInput("Blur Radius", ref _blurRadiusString, 4u, isError: false, canBeNull: true, ImGuiInputTextFlags.None, "Terrain height when pixel is fully black");
		ImGui.Separator();
		if ((bool)_crackMapTexture)
		{
			ImGui.InputInt("CrackDepth", ref CrackDepth);
			ImGui.InputInt("Seed", ref VoxelTerrainHeightmapImporter.CrackSeed);
			ImGui.Text("Parameter0");
			VoxelTerrainHeightmapImporter.CrackParam0.Draw(0);
			ImGui.Text("Parameter1");
			VoxelTerrainHeightmapImporter.CrackParam1.Draw(1);
			ImGui.Separator();
		}
		ImGui.BeginDisabled(ImportedData != null || _heightMapTexture == null);
		if (ImGui.Button("Import Voxel Data"))
		{
			ClearData();
			VoxelTerrainHeightmapImporter.Init();
			ImportedData = new VoxelOctree(_heightMapTexture.width);
			ImportedData.PrepareOctree();
			float value = 8f;
			float a = 128f;
			int value2 = 8;
			int a2 = 128;
			if (float.TryParse(_minHeightString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result))
			{
				value = result;
			}
			if (float.TryParse(_maxHeightString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result2))
			{
				a = result2;
			}
			if (int.TryParse(_blurRadiusString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result3))
			{
				VoxelTerrainHeightmapImporter.BlurRadius = result3;
			}
			if (int.TryParse(_minLavaHeightString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result4))
			{
				value2 = result4;
			}
			if (int.TryParse(_maxLavaHeightString, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result5))
			{
				a2 = result5;
			}
			a = Mathf.Min(a, 1023f);
			value = Mathf.Clamp(value, 2f, a - 1f);
			a = Mathf.Clamp(a, value + 1f, 1023f);
			a2 = Mathf.Min(a2, 1023);
			value2 = Mathf.Clamp(value2, 2, a2 - 1);
			a2 = Mathf.Clamp(a2, value2 + 1, 1023);
			VoxelTerrainHeightmapImporter.MINTerrainHeight = value;
			VoxelTerrainHeightmapImporter.MAXTerrainHeight = a;
			LavaData lavaData = null;
			if ((bool)_lavaHeightMapTexture)
			{
				lavaData = new LavaData(_lavaHeightMapTexture, value2, a2);
			}
			VoxelTerrainHeightmapImporter.GenerateTask(_heightMapTexture, _crustTypeMapTexture, _crackMapTexture, lavaData, ImportedData, _fillType).Forget();
		}
		ImGui.EndDisabled();
		if (IsGenerating)
		{
			ImGui.Text("CurrentTask: ");
			ImGui.SameLine();
			ImGui.Text(VoxelTerrainHeightmapImporter.CurrentTask.ToString());
			ImGui.Text("Progress: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(VoxelTerrainHeightmapImporter.GetProgress()));
			ImGui.SameLine();
			ImGui.Text("%");
		}
		ImGui.BeginDisabled(ImportedData == null || IsGenerating);
		if (ImGui.Button("Clear"))
		{
			ClearData();
		}
		ImGui.EndDisabled();
	}

	private static void DrawTexturePreview(Texture2D texture)
	{
		ImGui.Image((IntPtr)GetTextureId(texture), new Vector2(300f, 300f));
		ImGui.Text("X: ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(texture.width));
		ImGui.SameLine();
		ImGui.Text(" Y: ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(texture.height));
	}

	private static void OnSelectTexture()
	{
		ClearData();
	}

	private static void ClearData()
	{
		ImportedData?.Clear();
		ImportedData = null;
		_isLiveImportCurrent = false;
		if (GameManager.GameState == GameState.Paused)
		{
			WorldManager.SetGamePause(pauseGame: false);
		}
	}

	private static int GetTextureId(Texture texture)
	{
		return ImGuiManager.igTextureManager.GetTextureId(texture);
	}
}
