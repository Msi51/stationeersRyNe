using System.Threading;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Util;

namespace UI;

public class PointOfInterestMessage : UserInterfaceBase
{
	[Header("Point of Interest Message")]
	[SerializeField]
	private TextMeshProUGUI _titleText;

	[SerializeField]
	private TextMeshProUGUI _descriptionText;

	[SerializeField]
	private CanvasGroup _canvasGroup;

	[SerializeField]
	private float _easeInTime;

	[SerializeField]
	private float _easeOutTime;

	private static PointOfInterestMessage _instance;

	private static CancellationTokenWrapper _showCancellation = new CancellationTokenWrapper();

	private void Awake()
	{
		Initialize();
	}

	public void Initialize()
	{
		_instance = this;
	}

	public static void ClearAll()
	{
		_showCancellation.Cancel();
		_instance._canvasGroup.alpha = 0f;
	}

	public static void Show(string title, string description, float time)
	{
		_showCancellation.CancelAndInitialize();
		ShowTask(title, description, time, _showCancellation.Token).Forget();
	}

	private static async UniTaskVoid ShowTask(string title, string description, float time, CancellationToken cancellationToken)
	{
		_instance._titleText.text = title;
		_instance._descriptionText.text = description;
		float t = 0f;
		while (!cancellationToken.IsCancellationRequested && t < _instance._easeInTime)
		{
			_instance._canvasGroup.alpha = EaseInTextAlpha(t);
			await UniTask.WaitForEndOfFrame();
			t += Time.deltaTime;
		}
		_instance._canvasGroup.alpha = 1f;
		await UniTask.Delay((int)(time * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		t = 0f;
		while (!cancellationToken.IsCancellationRequested && t < _instance._easeOutTime)
		{
			_instance._canvasGroup.alpha = EaseOutTextAlpha(t);
			await UniTask.WaitForEndOfFrame();
			t += Time.deltaTime;
		}
		_instance._canvasGroup.alpha = 0f;
	}

	private static float EaseInTextAlpha(float t)
	{
		return Mathf.Pow(SecondsToMultiplier(_instance._easeInTime) * t, 4f);
	}

	private static float EaseOutTextAlpha(float t)
	{
		return Mathf.Pow(1f - t * SecondsToMultiplier(_instance._easeOutTime), 2f);
	}

	private static float SecondsToMultiplier(float s)
	{
		return 1f / s;
	}
}
