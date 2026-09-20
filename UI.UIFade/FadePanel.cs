using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.UIFade;

public class FadePanel : MonoBehaviour
{
	[SerializeField]
	private RawImage _fadeImage;

	private float _current;

	private readonly CancellationTokenWrapper _fadeCancellation = new CancellationTokenWrapper();

	private static FadePanel _instance;

	public static FadeState State { get; private set; }

	public static void ToBlackInstant()
	{
		if ((bool)_instance && !GameManager.IsBatchMode)
		{
			_instance.SetBlackInstant();
		}
	}

	public static void ToTransparentInstant()
	{
		if ((bool)_instance && !GameManager.IsBatchMode)
		{
			_instance.SetTransparentInstant();
		}
	}

	public static void ToBlack(float duration, float startDelay = 0f)
	{
		if ((bool)_instance && !GameManager.IsBatchMode)
		{
			_instance.SetFadeToBlack(duration, startDelay);
		}
	}

	public static void ToTransparent(float duration, float startDelay = 0f)
	{
		if ((bool)_instance && !GameManager.IsBatchMode)
		{
			_instance.SetFadeToTransparent(duration, startDelay);
		}
	}

	private void SetBlackInstant()
	{
		_fadeCancellation.Cancel();
		State = FadeState.Black;
		_current = 1f;
		SetEasedVolume(_current);
		SetAlpha(_current);
	}

	private void SetTransparentInstant()
	{
		_fadeCancellation.Cancel();
		State = FadeState.Transparent;
		_current = 0f;
		SetEasedVolume(_current);
		SetAlpha(_current);
	}

	private void SetFadeToBlack(float duration, float startDelay)
	{
		_fadeCancellation.CancelAndInitialize();
		Fade(duration, startDelay, 1f, FadeState.Black, _fadeCancellation.Token).Forget();
	}

	private void SetFadeToTransparent(float duration, float startDelay)
	{
		_fadeCancellation.CancelAndInitialize();
		Fade(duration, startDelay, 0f, FadeState.Transparent, _fadeCancellation.Token).Forget();
	}

	private async UniTask Fade(float duration, float startDelay, float target, FadeState targetState, CancellationToken cancellationToken)
	{
		SetEasedVolume(_current);
		State = FadeState.WaitingForDelay;
		await UniTask.Delay(DelayMs(startDelay), ignoreTimeScale: true, PlayerLoopTiming.Update, cancellationToken);
		State = FadeState.Fading;
		while (!cancellationToken.IsCancellationRequested && !Mathf.Approximately(_current, target))
		{
			float maxDelta = Mathf.Min(Time.unscaledDeltaTime, 0.02f) / duration;
			_current = Mathf.MoveTowards(_current, target, maxDelta);
			SetEasedAlpha(_current);
			SetEasedVolume(_current);
			await UniTask.Yield(cancellationToken);
		}
		_current = target;
		State = targetState;
		SetEasedVolume(_current);
		SetAlpha(_current);
		_fadeCancellation.Cancel();
	}

	private int DelayMs(float seconds)
	{
		return (int)(seconds * 1000f);
	}

	private float GetEasedVolume(float value)
	{
		return DOVirtual.EasedValue(RocketMath.MapToScale(0f, 100f, Settings.MinChannelVolume, 0f, Settings.CurrentData.MasterVolume), -100f, value, Ease.InCubic);
	}

	private void SetEasedVolume(float t)
	{
		float easedVolume = GetEasedVolume(t);
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("MasterVolume", easedVolume);
	}

	private void SetAlpha(float alpha)
	{
		_fadeImage.color = _fadeImage.color.SetAlpha(alpha);
	}

	private void SetEasedAlpha(float t)
	{
		float alpha = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutCubic);
		SetAlpha(alpha);
	}

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
		}
	}
}
