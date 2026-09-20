using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using ch.sycoforge.Flares;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class TutorialScenariosMenu : MainMenuPage
{
	private static readonly List<WorldPresetItemTutorial> WorldPresetItems = new List<WorldPresetItemTutorial>();

	[Header("Tutorial Scenarios Menu")]
	public LayoutGroup WorldPresetGroup;

	public WorldPresetItemTutorial WorldPresetPrefab;

	public Button StartGameButton;

	public WorldDescription WorldDescriptionWindow;

	public WorldPresetItemTutorial selectedWorld;

	public static TutorialScenariosMenu Instance;

	private bool PresetsAreLoaded => WorldPresetItems.Count > 0;

	private void Awake()
	{
		Instance = this;
		if ((bool)StartGameButton)
		{
			StartGameButton.onClick.AddListener(StartClicked);
		}
		PopulateWorldList();
	}

	private void OnEnable()
	{
		if ((object)selectedWorld == null && PresetsAreLoaded)
		{
			SelectFirstPreset();
		}
		if ((bool)selectedWorld)
		{
			SelectNewWorld(selectedWorld);
		}
		else
		{
			SetBlank();
		}
	}

	public override void OnPagePopped()
	{
		ClearPreviewScenes();
	}

	private void ClearPreviewScenes()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (WorldPresetItemTutorial worldPresetItem in WorldPresetItems)
		{
			if ((bool)worldPresetItem.PlanetScene)
			{
				worldPresetItem.PlanetScene.SetVisible(isVisble: false);
			}
			worldPresetItem.OnPointerExit(null);
		}
		selectedWorld = null;
	}

	private void PopulateWorldList()
	{
		SkyBoxController.ClearPlanets();
		foreach (WorldSetting allWorldSetting in WorldSetting.AllWorldSettings)
		{
			if (allWorldSetting.IsTutorial)
			{
				WorldPresetItemTutorial worldPresetItemTutorial = Object.Instantiate(WorldPresetPrefab, WorldPresetGroup.transform);
				worldPresetItemTutorial.name = allWorldSetting.Id;
				worldPresetItemTutorial.Assign(allWorldSetting, this);
				WorldPresetItems.Add(worldPresetItemTutorial);
				if (!GameManager.IsBatchMode)
				{
					PlanetScene planetScene = PlanetSceneManager.Instance.Create(allWorldSetting);
					worldPresetItemTutorial.PlanetScene = planetScene;
				}
			}
		}
		if (PresetsAreLoaded)
		{
			SelectFirstPreset();
		}
	}

	private void SelectFirstPreset()
	{
		foreach (WorldPresetItemTutorial worldPresetItem in WorldPresetItems)
		{
			if ((object)worldPresetItem != null && !worldPresetItem.WorldSetting.IsHidden)
			{
				SelectNewWorld(worldPresetItem);
				break;
			}
		}
	}

	public void SelectNewWorld(WorldPresetItemTutorial item)
	{
		if (selectedWorld != null && selectedWorld != item)
		{
			if ((bool)selectedWorld.PlanetScene)
			{
				selectedWorld.PlanetScene.SetVisible(isVisble: false);
			}
			selectedWorld.OnPointerExit(null);
		}
		selectedWorld = item;
		if ((bool)selectedWorld.PlanetScene)
		{
			selectedWorld.PlanetScene.SetVisible(isVisble: true);
		}
		ImGuiLoadingScreen.WorldName = item.WorldSetting.Id;
		if (!GameManager.IsBatchMode)
		{
			PreviewScene previewScene = (((object)item.PlanetScene == null) ? null : item.PlanetScene.PreviewScene);
			EasyFlares easyFlares = previewScene?.SunEasyFlares;
			if ((bool)easyFlares)
			{
				easyFlares.Opacity = previewScene.LensFlareIntensity;
			}
		}
		WorldSetting tutorial = Find(item.WorldSetting.Id);
		WorldDescriptionWindow.SetTutorial(tutorial);
	}

	private void SetBlank()
	{
		WorldDescriptionWindow.SetBlank();
	}

	private WorldSetting Find(string worldName)
	{
		foreach (WorldPresetItemTutorial worldPresetItem in WorldPresetItems)
		{
			if (worldPresetItem.WorldSetting.Id == worldName)
			{
				return worldPresetItem.WorldSetting;
			}
		}
		return null;
	}

	private void StartClicked()
	{
		if ((object)selectedWorld != null)
		{
			selectedWorld.PlanetScene.SetVisible(isVisble: false);
			WorldSetting worldSetting = selectedWorld.WorldSetting;
			if (worldSetting.IsTutorial)
			{
				LoadTutorial(worldSetting).Forget();
			}
		}
	}

	private void PopulateStartConditionData(WorldSetting worldSetting)
	{
		if (worldSetting?.Data?.StartConditionDatas == null)
		{
			return;
		}
		foreach (StartConditionData startConditionData in worldSetting.Data.StartConditionDatas)
		{
			if (startConditionData.IsDefault)
			{
				worldSetting.StartConditionData = DataCollection.Get<StartConditionData>(startConditionData.IdHash);
				break;
			}
		}
	}

	private async UniTaskVoid LoadTutorial(WorldSetting worldSetting)
	{
		XmlSaveLoad.ClearAll();
		PopulateStartConditionData(worldSetting);
		WorldSetting.SetCurrent(worldSetting);
		DifficultySetting.SetCurrent(DifficultySetting.Find("Normal"));
		GameManager.IsTutorial = (GameManager.IsNewTutorial = true);
		XmlSaveLoad.WorldIsReadOnly = true;
		await World.StartNewWorld(worldSetting.Id);
	}
}
