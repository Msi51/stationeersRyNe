using Assets.Scripts.GridSystem;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts;

public class MenuCutscene : MonoBehaviour
{
	public Material SkyboxMaterial;

	public Light Sun;

	public static float FieldOfView = 39f;

	public static Vector3 Position = new Vector3(-0.084f, 0.19f, 0.85f);

	public Quaternion Rotation = Quaternion.Euler(-4.455f, -201.55f, 1.039f);

	public GameObject[] DisableOnMenuLite;

	public GameObject[] EnableOnMenuLite;

	[SerializeField]
	private PlayerCosmeticsBehaviour _characterMannequin;

	[SerializeField]
	private KitItem _basicSuitKitItem;

	public void MenuLite(bool menuLite)
	{
		if (GameManager.GameState == GameState.None)
		{
			GameObject[] disableOnMenuLite = DisableOnMenuLite;
			for (int i = 0; i < disableOnMenuLite.Length; i++)
			{
				disableOnMenuLite[i].SetActive(!menuLite);
			}
			disableOnMenuLite = EnableOnMenuLite;
			for (int i = 0; i < disableOnMenuLite.Length; i++)
			{
				disableOnMenuLite[i].SetActive(menuLite);
			}
		}
	}

	public void SetPosition()
	{
		if (!GameManager.IsBatchMode)
		{
			RenderSettings.skybox = SkyboxMaterial;
			RenderSettings.sun = Sun;
			RenderSettings.ambientMode = AmbientMode.Trilight;
			Camera currentCamera = CameraController.CurrentCamera;
			currentCamera.fieldOfView = FieldOfView;
			currentCamera.transform.SetPositionAndRotation(Position, Rotation);
			SkyBoxController.SetPosition(Position);
			SkyBoxController.ApplyDefaults();
		}
	}

	private async UniTaskVoid WaitSetPosition()
	{
		for (int i = 0; i < 3; i++)
		{
			await UniTask.WaitForEndOfFrame();
			SetPosition();
		}
	}

	private void OnEnable()
	{
		SkyBoxController.ClearPlanets();
		WaitSetPosition().Forget();
		InventoryWindowManager._inventoryFinishedLoading = false;
		foreach (InputWindowBase inputWindow in Singleton<GameManager>.Instance.InputWindows)
		{
			if ((bool)inputWindow)
			{
				inputWindow.SetActive(active: false);
			}
			else
			{
				Debug.LogError($"Window is null. {inputWindow}");
			}
		}
	}

	private void Start()
	{
		_characterMannequin.ApplyClothing(_basicSuitKitItem);
		_characterMannequin.ApplyArmor(null);
	}
}
