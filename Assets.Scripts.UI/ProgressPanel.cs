using System;
using Assets.Scripts.Networking.Transports;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ProgressPanel : MonoBehaviour
{
	public TextMeshProUGUI Title;

	public Slider ProgressBar;

	public TextMeshProUGUI BarCaptionText;

	public GameObject CancelButton;

	public Image Black;

	public Image Blocker;

	public TextMeshProUGUI ProgressBarTitleText;

	public static ProgressPanel Instance;

	public GameObject ProgressBarWindow;

	private void Awake()
	{
		Instance = this;
		SteamTransport.WorkshopProgress.ProgressEvent = (Action<bool, float>)Delegate.Combine(SteamTransport.WorkshopProgress.ProgressEvent, new Action<bool, float>(UpdateProgressBar));
	}

	private void OnDestroy()
	{
		SteamTransport.WorkshopProgress.ProgressEvent = (Action<bool, float>)Delegate.Remove(SteamTransport.WorkshopProgress.ProgressEvent, new Action<bool, float>(UpdateProgressBar));
	}

	private void ShowProgressBar(string title)
	{
		Title.text = title;
		ProgressBarWindow.SetActive(value: true);
		Blocker.enabled = true;
		CancelButton.SetActive(value: false);
	}

	public void ShowProgressBar(string title, UnityAction CancelButtonAction)
	{
		Title.text = title;
		ProgressBarWindow.SetActive(value: true);
		Blocker.enabled = true;
		CancelButton.SetActive(value: true);
		CancelButton.GetComponent<Button>().onClick.RemoveAllListeners();
		CancelButton.GetComponent<Button>().onClick.AddListener(CancelButtonAction);
	}

	private void UpdateProgressBar(bool isDeleting, float value)
	{
		ProgressBar.value = value;
		if (isDeleting)
		{
			if (value < 0.3f)
			{
				UpdateProgressBarCaption("Unsubscribing");
			}
			else if (value < 0.5f)
			{
				UpdateProgressBarCaption("Deleting files");
			}
			else if (value < 0.9f)
			{
				UpdateProgressBarCaption("Cleaning up");
			}
		}
		else if (value < 0.1f)
		{
			UpdateProgressBarCaption("Processing configuration data");
		}
		else if (value < 0.3f)
		{
			UpdateProgressBarCaption("Reading and processing content files");
		}
		else if (value < 0.5f)
		{
			UpdateProgressBarCaption("Uploading content changes to Steam");
		}
		else if (value < 0.7f)
		{
			UpdateProgressBarCaption("Uploading new preview file image");
		}
		else if (value < 0.9f)
		{
			UpdateProgressBarCaption("Committing all changes");
		}
	}

	public void UpdateProgressBarCaption(string message)
	{
		BarCaptionText.text = message;
	}

	private void HideProgressBar()
	{
		ProgressBarWindow.SetActive(value: false);
		Blocker.enabled = false;
	}

	private async void HideProgress(string Message, float Timeout = 0.5f)
	{
		UpdateProgressBarCaption(Message);
		await UniTask.Delay(Mathf.RoundToInt(Timeout * 1000f), DelayType.UnscaledDeltaTime);
		Instance.HideProgressBar();
	}

	public static void ShowProgressBar(bool isDelete = false)
	{
		if (!Instance)
		{
			Debug.LogError("ProgressPanel Instance is null");
		}
		else
		{
			Instance.ShowProgressBar(isDelete ? "Deleting..." : "Uploading...");
		}
	}

	public static void ShowProgressSuccessOrFailure(bool success, bool isDelete = false)
	{
		if (!Instance)
		{
			Debug.LogError("ProgressPanel Instance is null");
		}
		else if (success)
		{
			Instance.HideProgress((isDelete ? "Deletion" : "Upload") + " Complete!");
		}
		else
		{
			Instance.HideProgress((isDelete ? "Deletion" : "Upload") + " Failed.", 1f);
		}
	}
}
