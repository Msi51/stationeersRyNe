using System.Threading;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Util;

namespace UI;

public class AlertMessage : UserInterfaceBase
{
	[Header("Alert Message")]
	[SerializeField]
	private TextMeshProUGUI _messageText;

	[SerializeField]
	private CanvasGroup _canvasGroup;

	[SerializeField]
	private float _fadeSpeed;

	private static AlertMessage _instance;

	private static CancellationTokenWrapper _showCancellation = new CancellationTokenWrapper();

	private void Awake()
	{
		_instance = this;
	}

	public static void ClearAll()
	{
		_showCancellation.Cancel();
		_instance._canvasGroup.alpha = 0f;
	}

	public static void Show(string text, float time)
	{
		_showCancellation.CancelAndInitialize();
		ShowTask(text, time, _showCancellation.Token).Forget();
	}

	private static async UniTaskVoid ShowTask(string text, float time, CancellationToken cancellationToken)
	{
		_instance._messageText.text = text;
		float fadeIn = 0f;
		while (!cancellationToken.IsCancellationRequested && fadeIn < 1f)
		{
			_instance._canvasGroup.alpha = fadeIn * fadeIn;
			await UniTask.WaitForEndOfFrame();
			fadeIn += Time.deltaTime * _instance._fadeSpeed;
		}
		_instance._canvasGroup.alpha = 1f;
		await UniTask.Delay((int)(time * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		float fadeOut = 0f;
		while (!cancellationToken.IsCancellationRequested && fadeOut < 1f)
		{
			_instance._canvasGroup.alpha = (1f - fadeOut) * (1f - fadeOut);
			await UniTask.WaitForEndOfFrame();
			fadeOut += Time.deltaTime * _instance._fadeSpeed;
		}
		_instance._canvasGroup.alpha = 0f;
	}
}
