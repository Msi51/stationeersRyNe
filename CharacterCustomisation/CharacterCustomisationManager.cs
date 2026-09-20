using System;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CharacterCustomisation;

public class CharacterCustomisationManager : MonoBehaviour
{
	private const string SCENE_NAME = "CharacterCustomisation";

	[SerializeField]
	private CharacterCreationPanel _creationPanel;

	[SerializeField]
	private EventSystem _sceneEventSystem;

	public static bool IsSceneLoaded { get; private set; }

	public static event Action OnSceneLoaded;

	public static event Action OnSceneUnloaded;

	private void Awake()
	{
		if (NetworkManager.CurrentTransport == null)
		{
			NetworkManager.Init(TransportType.Rocket);
		}
		if (!EventSystem.current)
		{
			_sceneEventSystem.gameObject.SetActive(value: true);
		}
		_creationPanel.OnConfirmed += UnloadScene;
		_creationPanel.OnCancelled += UnloadScene;
	}

	public static async void LoadScene()
	{
		await SceneManager.LoadSceneAsync("CharacterCustomisation", LoadSceneMode.Additive);
		IsSceneLoaded = true;
		CharacterCustomisationManager.OnSceneLoaded?.Invoke();
	}

	public static async void UnloadScene()
	{
		if (IsSceneLoaded)
		{
			await SceneManager.UnloadSceneAsync("CharacterCustomisation", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
			IsSceneLoaded = false;
		}
		CharacterCustomisationManager.OnSceneUnloaded?.Invoke();
		await Resources.UnloadUnusedAssets();
		GC.Collect();
	}
}
