using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.UI;
using ch.sycoforge.Flares;
using Cysharp.Threading.Tasks;
using TMPro;
using UI;
using UI.LoadGame;
using UnityEngine;
using UnityEngine.UI;

public class NewWorldMenu : MainMenuPage
{
	[Space(10f)]
	[Header("New World Menu")]
	public TMP_Dropdown WorldPresetDropdown;

	[SerializeField]
	private Button _newGameButton;

	public WorldDescription WorldDescriptionWindow;

	public StartConditionMenu StartConditionMenu;

	public WorldPresetItem WorldPresetPrefab;

	[SerializeField]
	private RectTransform _contentRectTransform;

	public LayoutGroup WorldPresetGroup;

	public static WorldPresetItem SelectedWorld;

	public static StartLocationItem SelectedStartLocation;

	private bool resettingValues;

	public bool InGameTravel;

	public static NewWorldMenu Instance;

	public List<WorldPresetItem> WorldPresetItems = new List<WorldPresetItem>();

	public static string WorldHashes;

	public static void SelectFirstPreset()
	{
		SelectedStartLocation = null;
		foreach (WorldPresetItem worldPresetItem in Instance.WorldPresetItems)
		{
			if ((object)worldPresetItem != null && !worldPresetItem.WorldSetting.IsHidden)
			{
				Instance?.SelectNewWorld(worldPresetItem);
				break;
			}
		}
	}

	private async UniTaskVoid WaitThenRebuildLayout()
	{
		await UniTask.Yield();
		LayoutRebuilder.MarkLayoutForRebuild(_contentRectTransform);
		foreach (WorldPresetItem worldPresetItem in WorldPresetItems)
		{
			LayoutRebuilder.MarkLayoutForRebuild(worldPresetItem._bottomPanelRectTransform);
		}
	}

	private static WorldSetting Find(string worldName)
	{
		foreach (WorldPresetItem worldPresetItem in Instance.WorldPresetItems)
		{
			if (worldPresetItem.WorldSetting.Id == worldName)
			{
				return worldPresetItem.WorldSetting;
			}
		}
		return null;
	}

	private void Awake()
	{
		Instance = this;
		if ((bool)_newGameButton)
		{
			_newGameButton.onClick.AddListener(NewWorldButton);
		}
		PopulateWorldList();
	}

	public override void OnManagerStart()
	{
		base.OnManagerStart();
		Instance = this;
	}

	public override void OnPagePopped()
	{
		ClearPreviewScenes();
	}

	public override void OnPageShown()
	{
		if (SelectedWorld == null)
		{
			SelectFirstPreset();
		}
		WaitThenRebuildLayout().Forget();
	}

	public static void PopulateWorldHashes()
	{
		WorldHashes = "";
		foreach (WorldSetting item in WorldSetting.AllWorldSettings.Where((WorldSetting preset) => !preset.IsDeprecated))
		{
			WorldHashes = $"{WorldHashes}\n{item.Id}:{{POS:300}}<link=Clipboard>{{COLORYELLOW:{Animator.StringToHash(item.Id)}}}</link>";
		}
	}

	private void PopulateWorldList()
	{
		SkyBoxController.ClearPlanets();
		List<WorldSetting> list = new List<WorldSetting>(WorldSetting.AllWorldSettings);
		list.Sort((WorldSetting a, WorldSetting b) => a.Data.Priority.CompareTo(b.Data.Priority));
		foreach (WorldSetting item in list)
		{
			if (!item.IsTutorial && !item.IsHidden)
			{
				WorldPresetItem worldPresetItem = UnityEngine.Object.Instantiate(WorldPresetPrefab, WorldPresetGroup.transform);
				worldPresetItem.name = "World" + item.Id;
				worldPresetItem.Assign(item, this);
				worldPresetItem.Expand(expand: false, force: true);
				WorldPresetItems.Add(worldPresetItem);
				if (item.IsDeprecated)
				{
					worldPresetItem.SetVisible(isVisble: false);
				}
				if (!InGameTravel && !GameManager.IsBatchMode)
				{
					PlanetScene planetScene = PlanetSceneManager.Instance.Create(item);
					worldPresetItem.PlanetScene = planetScene;
				}
			}
		}
	}

	public static void ClearPreviewScenes()
	{
		if (GameManager.IsBatchMode || !Instance)
		{
			return;
		}
		foreach (WorldPresetItem worldPresetItem in Instance.WorldPresetItems)
		{
			if ((bool)worldPresetItem.PlanetScene)
			{
				worldPresetItem.PlanetScene.SetVisible(isVisble: false);
			}
			worldPresetItem.OnPointerExit(null);
		}
		if (SelectedWorld != null)
		{
			SelectedWorld.Expand(expand: false);
			SelectedWorld = null;
		}
	}

	public virtual void SelectNewWorld(WorldPresetItem worldItem)
	{
		if (SelectedWorld != null && SelectedWorld != worldItem)
		{
			if ((bool)SelectedWorld.PlanetScene)
			{
				SelectedWorld.PlanetScene.SetVisible(isVisble: false);
			}
			SelectedWorld.OnPointerExit(null);
		}
		SelectedWorld = worldItem;
		if ((bool)SelectedWorld.PlanetScene)
		{
			SelectedWorld.PlanetScene.SetVisible(isVisble: true);
		}
		ImGuiLoadingScreen.WorldName = worldItem.WorldSetting.Id;
		if (!GameManager.IsBatchMode)
		{
			PreviewScene previewScene = ((worldItem.PlanetScene != null) ? worldItem.PlanetScene.PreviewScene : null);
			EasyFlares easyFlares = previewScene?.SunEasyFlares;
			if ((bool)easyFlares)
			{
				easyFlares.Opacity = previewScene.LensFlareIntensity;
			}
		}
		WorldSetting world = Find(worldItem.WorldSetting.Id);
		WorldDescriptionWindow.SetWorld(world);
		StartConditionMenu.SetWorld(world);
		WorldPresetItemClicked(worldItem);
		if (SelectedStartLocation == null)
		{
			using (List<StartLocationItem>.Enumerator enumerator = worldItem.StartLocationItems.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					StartLocationItem current = enumerator.Current;
					StartLocationClicked(current);
				}
				return;
			}
		}
		StartLocationClicked(SelectedStartLocation);
	}

	public void OnValueChanged(Transform target)
	{
		if (!resettingValues)
		{
			WorldPresetDropdown.value = 0;
		}
	}

	public void SliderRound(GameObject target)
	{
		target.transform.GetComponentInChildren<Slider>().value = (float)Math.Round((decimal)target.transform.GetComponentInChildren<Slider>().value, 1);
	}

	public void NewWorldButton()
	{
		if ((bool)SelectedWorld && SelectedWorld.WorldSetting != null)
		{
			MainMenu.Instance.PageManager.EnableMainMenuPage("WorldConfiguration");
		}
	}

	public void StartLocationClicked(StartLocationItem startLocationItem)
	{
		foreach (StartLocationItem startLocationItem2 in startLocationItem.WorldPresetItem.StartLocationItems)
		{
			startLocationItem2.Select(startLocationItem == startLocationItem2);
		}
		CalculateSize();
		SelectedStartLocation = startLocationItem;
	}

	private void WorldPresetItemClicked(WorldPresetItem worldPresetItem)
	{
		if (worldPresetItem.Expanded)
		{
			worldPresetItem.Expand(expand: false);
		}
		else
		{
			foreach (WorldPresetItem worldPresetItem2 in WorldPresetItems)
			{
				worldPresetItem2.Expand(worldPresetItem == worldPresetItem2);
			}
		}
		CalculateSize();
	}

	public static float CalculateVerticalExtraSize(VerticalLayoutGroup layoutGroup, int itemCount)
	{
		float num = (float)(itemCount - 1) * layoutGroup.spacing;
		int num2 = ((itemCount > 0) ? layoutGroup.padding.vertical : 0);
		return num + (float)num2;
	}

	private void CalculateSize()
	{
		float num = 0f;
		foreach (WorldPresetItem worldPresetItem in WorldPresetItems)
		{
			num += worldPresetItem.CalculateSize();
		}
		num += CalculateVerticalExtraSize((VerticalLayoutGroup)WorldPresetGroup, WorldPresetItems.Count);
		_contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		LayoutRebuilder.MarkLayoutForRebuild(_contentRectTransform);
	}
}
