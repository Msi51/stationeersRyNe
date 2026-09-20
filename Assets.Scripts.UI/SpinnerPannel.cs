using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class SpinnerPannel : GameBase
{
	public TextMeshProUGUI Title;

	public GameObject SpinnerIcon;

	public GameObject SpinnerWindow;

	public static SpinnerPannel Instance;

	private readonly int _uiMouseOverHash = Animator.StringToHash("SFX_UI_MouseOver");

	private bool _fadeOutWindow;

	private float _fadeOutDelay = 1f;

	private CanvasRenderer _canvasRenderer;

	public bool FadeOutWindow
	{
		get
		{
			return _fadeOutWindow;
		}
		set
		{
			_fadeOutWindow = value;
			FadeOut().Forget();
		}
	}

	private void Awake()
	{
		Instance = this;
		_canvasRenderer = SpinnerWindow.GetComponent<CanvasRenderer>();
		Close();
	}

	public void ShowSpinner(string title)
	{
		Title.text = title;
		_canvasRenderer.SetAlpha(1f);
		Title.canvasRenderer.SetAlpha(1f);
		SpinnerIcon.SetActive(value: true);
		SpinnerWindow.SetActive(value: true);
		UIAudioManager.Play(_uiMouseOverHash);
	}

	public void HideSpinner(string title, float delay)
	{
		Title.text = title;
		SpinnerIcon.SetActive(value: false);
		FadeOutWindow = true;
		_fadeOutDelay = delay;
	}

	public void Close()
	{
		_fadeOutWindow = false;
		SpinnerIcon.SetActive(value: false);
		SpinnerWindow.SetActive(value: false);
	}

	public async UniTaskVoid FadeOut()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		while (_fadeOutWindow)
		{
			if (_fadeOutDelay <= 0f)
			{
				if (_canvasRenderer.GetColor().a - Time.deltaTime > 0f)
				{
					_canvasRenderer.SetAlpha(_canvasRenderer.GetColor().a - Time.deltaTime * 2f);
					Title.canvasRenderer.SetAlpha(_canvasRenderer.GetColor().a - Time.deltaTime * 2f);
				}
				else
				{
					_fadeOutWindow = false;
					SpinnerWindow.SetActive(value: false);
				}
			}
			else
			{
				_fadeOutDelay -= Time.deltaTime;
			}
			await UniTask.NextFrame();
		}
	}
}
