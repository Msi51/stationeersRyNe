using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using TerrainSystem;
using TerrainSystem.Lods;
using UI;
using UI.UIFade;
using UnityEngine;

namespace Assets.Scripts.Objects;

public static class World
{
	public static string CurrentId;

	private static CancellationTokenSource _cancellation;

	public static void Cancel()
	{
		_cancellation?.Cancel();
	}

	public static async UniTask StartNewWorld(string worldName)
	{
		Assets.Scripts.UI.MainMenu.Instance.SetActive(active: false);
		ImGuiLoadingScreen.SetActive(active: true);
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenInitializing.DisplayString);
		HelperHintsTextController.RefreshDisplayState(isLoadingWorld: true);
		_cancellation = new CancellationTokenSource();
		bool isSuccessful = true;
		try
		{
			await NewAsync(worldName, _cancellation.Token);
			KeyManager.ResetKeyStateToDefault();
			ConsoleWindow.Print("World " + worldName + " created");
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			isSuccessful = false;
		}
		if (!isSuccessful)
		{
			Singleton<ConfirmationPanel>.Instance.Show("FailedToLoadGameMessageTitle", "FailedToLoadGameMessageText", "ButtonExit", delegate
			{
				GameManager.LeaveGame();
				FadePanel.ToTransparentInstant();
			});
		}
	}

	private static async UniTask NewAsync(string worldName, CancellationToken cancellationToken = default(CancellationToken))
	{
		WorldManager.SetGamePause(pauseGame: true);
		FadePanel.ToBlack(0.5f);
		await UniTask.Delay(500, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);
		Initialize(worldName, newWorld: true, "Creating New World");
		TraderContact.InitializeAll();
		HelperHintsManager.Initialize();
		if (WorldSetting.Current.Data.TerrainSettings == null)
		{
			throw new NullReferenceException("No terrainSettings data found");
		}
		await VoxelTerrain.LoadTerrain(WorldSetting.Current.Data.TerrainSettings, newGame: true);
		RegionManager.LoadRegionSets(WorldSetting.Current.Data);
		if (!GameManager.IsBatchMode)
		{
			CreateCharacterAndTakeControl();
		}
		await WorldManager.SpawnOnNewWorld();
		AtmosphericsManager.HandleMainThreadRegistrations();
		await VoxelTerrain.InitialiseMinablesOnLoad();
		await LodManager.InitialiseLodsOnLoad();
		InventoryManager.ParentHuman?.SetPhysicsOnControl();
		GameManager.OnReadyToPlay();
		if (Settings.CurrentData.AutoSave && !GameManager.IsTutorial && !XmlSaveLoad.WorldIsReadOnly)
		{
			StationAutoSave.ResetAutoSave();
		}
		if (!GameManager.IsBatchMode)
		{
			GameManager.OnGameStartedOnce += delegate
			{
				Stationpedia.Instance.ButtonHome();
			};
		}
		WorldManager.StartWorld();
		await GameManager.StartGame();
		ImGuiLoadingScreen.SetActive(active: false);
	}

	public static void OnLoadingFinished(XmlSaveLoad.WorldData worldData)
	{
		HelperHintsManager.Initialize();
		WorldManager.StartWorld();
		GameManager.StartGame().Forget();
		if (!GameManager.IsBatchMode)
		{
			ImGuiLoadingScreen.SetActive(active: false);
		}
		OrbitalSimulation.DeserializeSave(worldData);
	}

	public static DynamicThing HandlePlayerControl()
	{
		foreach (KeyValuePair<ulong, Brain> playerBrain in Brain.PlayerBrains)
		{
			if (playerBrain.Key != NetworkManager.LocalClientId)
			{
				continue;
			}
			Brain value = playerBrain.Value;
			if ((bool)value)
			{
				if ((bool)(value.ParentSlot.Parent as Entity))
				{
					value.TakeControl();
				}
				else
				{
					value.TakeControlInBodyBag();
				}
				return value.ParentSlot.Parent.AsDynamicThing;
			}
		}
		return null;
	}

	public static Human CreateCharacterAndTakeControl()
	{
		ulong localClientId = NetworkManager.LocalClientId;
		string username = NetworkManager.Username;
		PlayerCosmetics cosmetics = PlayerCosmetics.Load(Singleton<GameManager>.Instance.CustomCosmeticsSlot) ?? new PlayerCosmetics();
		SerializedClientInfo value;
		bool flag = GameManager.ClientInfo.TryGetValue(localClientId, out value);
		StartLocationData startLocation = (flag ? DataCollection.Get<StartLocationData>(value.StartLocationHash) : null);
		ISpawnPoint spawnPoint = (flag ? Referencable.Find<ISpawnPoint>(value.SpawnPointReference) : null);
		Human human = Human.CreateCharacter(localClientId, username, cosmetics, flag, startLocation, spawnPoint);
		human.OrganBrain.TakeControl(setPhysics: false);
		return human;
	}

	private static void Initialize(string worldName, bool newWorld, string loadingScreenMessage)
	{
		if (!GameManager.IsBatchMode)
		{
			XmlSaveLoad.UpdateLoadingScreen(loadingScreenMessage, 0f).Forget();
		}
		Thing.ClearAll();
		GameManager.GameState = GameState.Joining;
		if (newWorld)
		{
			CurrentId = GenerateWorldId();
			GridController.InitializeWorldController();
			WorldManager.GenerateNewSeed();
		}
		WorldManager.Instance.InitializeWorldEnvironment();
		SpaceMap.BuildSpaceMap(WorldSetting.Current.SpaceMapData);
	}

	public static void PopulateEmptyId()
	{
		CurrentId = (string.IsNullOrWhiteSpace(CurrentId) ? GenerateWorldId() : CurrentId);
	}

	public static string GenerateWorldId()
	{
		return Guid.NewGuid().ToString();
	}
}
