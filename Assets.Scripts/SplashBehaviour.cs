using System.Collections;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts;

public class SplashBehaviour : MonoBehaviour
{
	[SerializeField]
	private float _splashTime = 3f;

	private static float progress;

	public static bool IsActive { get; private set; }

	private void Awake()
	{
		Application.targetFrameRate = 60;
		StartCoroutine(AwakeCoroutine());
	}

	private void OnDestroy()
	{
		IsActive = false;
	}

	private IEnumerator AwakeCoroutine()
	{
		yield return new WaitForSeconds(_splashTime);
		AsyncOperation op = SceneManager.LoadSceneAsync("Base");
		while (!op.isDone)
		{
			IsActive = true;
			progress = op.progress;
			yield return null;
		}
		IsActive = false;
	}

	public static void Draw()
	{
		ImGuiLoadingScreen.DrawSplashLoading(progress);
	}
}
