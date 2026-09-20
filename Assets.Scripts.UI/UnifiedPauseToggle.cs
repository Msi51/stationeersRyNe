using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class UnifiedPauseToggle : ImageToggle
{
	private bool _isOn;

	[SerializeField]
	private Button button;

	public void OnClick()
	{
		WorldManager.SetGamePause(!_isOn);
	}

	private void OnPause(bool paused)
	{
		_isOn = paused;
		SetImage(_isOn ? 1 : 0);
		button.image = ImageComponent;
	}

	public void Awake()
	{
		WorldManager.OnPaused += OnPause;
	}
}
