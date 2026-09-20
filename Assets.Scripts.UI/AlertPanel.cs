using System;
using System.Collections;
using Assets.Scripts.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class AlertPanel : MonoBehaviour
{
	[Tooltip("Alert texture displayed when Alert message is triggered")]
	public Texture AlertTexture;

	[Tooltip("Loading texture displayed when Alert Loading message is triggered")]
	public Texture LoadingTexture;

	[Tooltip("Component the textures will be applied to")]
	public RawImage AlertImage;

	[ReadOnly]
	public TextMeshProUGUI Message;

	public Image Black;

	public Image Blocker;

	public TextMeshProUGUI AlertTitleText;

	public static AlertPanel Instance;

	public GameObject AlertWindow;

	private Coroutine _timerCoroutine;

	public static Thing.Event OnAlertClose;

	private void Awake()
	{
		Instance = this;
		AlertWindow = base.transform.GetChild(0).gameObject;
		DisableAlertPanel();
	}

	private void SwitchAlertType(AlertState alertType)
	{
		switch (alertType)
		{
		case AlertState.Alert:
			AlertImage.texture = AlertTexture;
			AlertTitleText.text = AlertStrings.TitleAttention;
			break;
		case AlertState.Loading:
			AlertImage.texture = LoadingTexture;
			AlertTitleText.text = AlertStrings.TitlePleaseWait;
			break;
		}
	}

	public void ShowAlert(string message, AlertState alertType, float timer = 0f)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		Message.text = message;
		Black.enabled = Math.Abs(timer) < 0.1f;
		AlertWindow.SetActive(value: true);
		SwitchAlertType(alertType);
		base.transform.GetChild(0).Find("Close").gameObject.SetActive(Math.Abs(timer) < 0.1f && alertType != AlertState.Loading);
		Blocker.enabled = true;
		if (timer > 0f)
		{
			if (_timerCoroutine != null)
			{
				StopCoroutine(_timerCoroutine);
				_timerCoroutine = null;
			}
			_timerCoroutine = StartCoroutine(SetDisable(timer));
		}
	}

	public void DisableAlertPanel()
	{
		AlertWindow.SetActive(value: false);
		Black.gameObject.SetActive(value: false);
		Blocker.enabled = false;
		if (OnAlertClose != null)
		{
			OnAlertClose();
		}
	}

	private IEnumerator SetDisable(float timer)
	{
		yield return Yielders.WaitForSeconds(timer);
		DisableAlertPanel();
		_timerCoroutine = null;
	}
}
