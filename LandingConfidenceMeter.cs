using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class LandingConfidenceMeter : GameBase
{
	[SerializeField]
	private TextMeshProUGUI ConfidencePercent;

	[SerializeField]
	private RectTransform _progressBar;

	[SerializeField]
	private Image _bar;

	private Color AutolandAborted = new Color(1f, 0f, 0f, 0.45f);

	public CancellationTokenWrapper BarFlashCancellationToken = new CancellationTokenWrapper();

	public void Refresh(bool autolandEnabled, float confidenceRatio, string confidenceString)
	{
		ConfidencePercent.text = confidenceString;
		if (autolandEnabled && confidenceRatio <= 0f)
		{
			if (!BarFlashCancellationToken.Initialized)
			{
				BarFlashCancellationToken.Initialize();
				FlashBarRed(BarFlashCancellationToken.Token).Forget();
			}
		}
		else
		{
			BarFlashCancellationToken.Cancel();
			_progressBar.localScale = new Vector3(confidenceRatio, 1f, 1f);
			float r = Mathf.Lerp(1f, 0f, confidenceRatio);
			float g = Mathf.Lerp(0f, 1f, confidenceRatio);
			_bar.color = new Color(r, g, 0f, 0.45f);
		}
	}

	private async UniTaskVoid FlashBarRed(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			_progressBar.localScale = new Vector3(0f, 1f, 1f);
			await UniTask.Delay(200);
			_progressBar.localScale = new Vector3(1f, 1f, 1f);
			_bar.color = AutolandAborted;
			await UniTask.Delay(200);
		}
	}

	public void Clear()
	{
		BarFlashCancellationToken.Cancel();
	}
}
