using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Scripts.Inventory;
using Assets.Scripts.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

[Serializable]
public class StatusUpdate : IComparer<StatusUpdate>
{
	public string DisplayName;

	public StatusUpdateType Type;

	public AudioClip AudioAlert;

	public bool PlayRepeaterAudio = true;

	public Image Image;

	public Sprite Icon;

	public UniTask UpdateTask;

	public CancellationTokenSource CancelTokenSource;

	[ReadOnly]
	public AlertItem Display;

	[ReadOnly]
	public string Key;

	public StatusIcon ConnectedIcon;

	private static Dictionary<LanguageCode, AudioClip> AudioClipsByLanguage = new Dictionary<LanguageCode, AudioClip>();

	private static Dictionary<LanguageCode, string> LanguageCodeToString = new Dictionary<LanguageCode, string>();

	public AudioClip[] CollectedFiles;

	public bool _isEnabled = true;

	public bool _lastStaticState;

	public bool _lastFlashState;

	public bool UsesDedicatedDisplay;

	private readonly int _uiNotifyWarningHash = Animator.StringToHash("SFX_UI_Notify_WARNING");

	private readonly int _uiNotifyNoticeHash = Animator.StringToHash("SFX_UI_Notify_NOTICE");

	private readonly int _uiNotifyCriticalHash = Animator.StringToHash("SFX_UI_Notify_CRITICAL");

	private readonly int _uiNotifyCriticalRepeaterHash = Animator.StringToHash("SFX_UI_Notify_CRITICAL_Repeater");

	public string StringKey { get; private set; }

	public int StringHash { get; private set; }

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
			if ((bool)Display)
			{
				Display.Redraw();
			}
		}
	}

	public static void RefreshStatusUpdateVoice()
	{
		foreach (StatusUpdate allStatusUpdate in StatusUpdates.AllStatusUpdates)
		{
			allStatusUpdate.SetStatusUpdateVoiceByLanguage();
		}
	}

	public void SetStatusUpdateVoiceByLanguage()
	{
		AudioClipsByLanguage.Clear();
		LanguageCodeToString.Clear();
		LanguageCodeToString.Add(LanguageCode.EN, "ENGLISH");
		LanguageCodeToString.Add(LanguageCode.DE, "GERMAN");
		LanguageCodeToString.Add(LanguageCode.RU, "Russian");
		LanguageCodeToString.Add(LanguageCode.ZH, "Chinese");
		foreach (LanguageCode key in LanguageCodeToString.Keys)
		{
			AudioClip[] source = Resources.LoadAll<AudioClip>("Voice/" + LanguageCodeToString[key]);
			CollectedFiles = Array.FindAll(source.ToArray(), (AudioClip file) => file.name.ToLower().Contains(DisplayName.ToLower()));
			if (CollectedFiles.Length != 0)
			{
				AudioClipsByLanguage.Add(key, CollectedFiles[0]);
			}
		}
		if (!AudioClipsByLanguage.TryGetValue(Settings.CurrentData.VoiceLanguageCode, out AudioAlert))
		{
			AudioClipsByLanguage.TryGetValue(LanguageCode.EN, out AudioAlert);
		}
	}

	public void Reset()
	{
		CancelTokenSource?.Cancel();
		CancelTokenSource = null;
		Image.gameObject.SetActive(value: false);
		_lastFlashState = false;
		_lastStaticState = false;
		StatusUpdates.firstTime = true;
	}

	public void Register(Func<string> toTooltip)
	{
		if (!StatusUpdates.AllStatusUpdates.Contains(this))
		{
			StatusUpdates.AllStatusUpdates.Add(this);
			StringKey = $"Status{Type}{DisplayName}";
			StringHash = Animator.StringToHash(StringKey);
			ConnectedIcon.ToTooltip = toTooltip;
		}
	}

	public void RunFlashState(Func<bool> myState, Func<bool> otherState)
	{
		bool flag = myState();
		if (flag != _lastFlashState)
		{
			_lastFlashState = flag;
			if (UpdateTask.Status != UniTaskStatus.Pending && myState())
			{
				UpdateTask = FlashState(myState, otherState);
			}
		}
	}

	public void RunStaticState(bool myState, bool otherState)
	{
		if ((object)InventoryManager.ParentHuman == null || myState == _lastStaticState)
		{
			return;
		}
		_lastStaticState = myState;
		if (myState && !otherState && (!Image.gameObject.activeSelf || Image.sprite != Icon))
		{
			Image.sprite = Icon;
			if (AudioAlert != null && IsEnabled && Settings.NotificationsOn)
			{
				if (Type == StatusUpdateType.Warning && !StatusUpdates.firstTime)
				{
					UIAudioManager.Play(_uiNotifyWarningHash);
				}
				if (Type == StatusUpdateType.Notice && !StatusUpdates.firstTime)
				{
					UIAudioManager.Play(_uiNotifyNoticeHash);
				}
				SpeakDelay(500).Forget();
			}
		}
		if (Image.sprite == null)
		{
			Image.sprite = Icon;
		}
		if (!UsesDedicatedDisplay)
		{
			Image.gameObject.SetActive(myState || otherState);
		}
	}

	public async UniTaskVoid SpeakDelay(int time)
	{
		await UniTask.Delay(time);
		StatusUpdates.PlaySound(StatusUpdates.Instance.Audio, AudioAlert);
	}

	public void Speak(bool force = false)
	{
		StatusUpdates.PlaySound(StatusUpdates.Instance.Audio, AudioAlert, force);
	}

	public void RunStaticState(bool myState)
	{
		if (myState == _lastStaticState)
		{
			return;
		}
		_lastStaticState = myState;
		if (myState && (!Image.gameObject.activeSelf || Image.sprite != Icon))
		{
			if (AudioAlert != null && IsEnabled && Settings.NotificationsOn)
			{
				StatusUpdates.PlaySound(StatusUpdates.Instance.Audio, AudioAlert);
			}
			Image.sprite = Icon;
			if (!StatusUpdates.firstTime)
			{
				UIAudioManager.Play(_uiNotifyNoticeHash);
			}
		}
		if (!UsesDedicatedDisplay)
		{
			Image.gameObject.SetActive(myState);
		}
	}

	public async UniTask FlashState(Func<bool> criticalState, Func<bool> warningState)
	{
		CancelTokenSource = new CancellationTokenSource();
		if (AudioAlert != null && IsEnabled && Settings.NotificationsOn)
		{
			if (!StatusUpdates.firstTime)
			{
				UIAudioManager.Play(_uiNotifyCriticalHash);
			}
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
			StatusUpdates.PlaySound(StatusUpdates.Instance.Audio, AudioAlert);
		}
		Image.sprite = Icon;
		Image.transform.SetAsLastSibling();
		Image.gameObject.SetActive(value: true);
		while (criticalState() && !CancelTokenSource.IsCancellationRequested)
		{
			if (PlayRepeaterAudio)
			{
				UIAudioManager.Play(_uiNotifyCriticalRepeaterHash);
			}
			Image.sprite = Icon;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
			if (warningState())
			{
				break;
			}
			Image.sprite = StatusUpdates.Instance.IconBlank;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
		}
		if (!warningState())
		{
			Image.gameObject.SetActive(value: false);
		}
	}

	public async UniTask FlashState(Func<bool> criticalState)
	{
		CancelTokenSource = new CancellationTokenSource();
		if (AudioAlert != null && IsEnabled && Settings.NotificationsOn)
		{
			if (!StatusUpdates.firstTime)
			{
				UIAudioManager.Play(_uiNotifyCriticalHash);
			}
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
			StatusUpdates.PlaySound(StatusUpdates.Instance.Audio, AudioAlert);
		}
		Image.sprite = Icon;
		Image.transform.SetAsLastSibling();
		Image.gameObject.SetActive(value: true);
		while (criticalState() && !CancelTokenSource.IsCancellationRequested)
		{
			if (PlayRepeaterAudio)
			{
				UIAudioManager.Play(_uiNotifyCriticalRepeaterHash);
			}
			Image.sprite = Icon;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
			Image.sprite = StatusUpdates.Instance.IconBlank;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, CancelTokenSource.Token);
		}
		Image.gameObject.SetActive(value: false);
	}

	public static int Compare(StatusUpdate x, StatusUpdate y)
	{
		if (x.Type > y.Type)
		{
			return -1;
		}
		if (x.Type < y.Type)
		{
			return 1;
		}
		return string.Compare(x.DisplayName, y.DisplayName, StringComparison.Ordinal);
	}

	int IComparer<StatusUpdate>.Compare(StatusUpdate x, StatusUpdate y)
	{
		return Compare(x, y);
	}

	public string GetDisplayName()
	{
		return Localization.GetInterface(StringHash);
	}
}
