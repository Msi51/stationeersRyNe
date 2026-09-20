using System.Threading;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects.Electrical;

public class LogicOnOffButton : MonoBehaviour
{
	[SerializeField]
	private Material powerOff;

	[SerializeField]
	private Material powerOn;

	[SerializeField]
	private Material errorEmissive;

	[SerializeField]
	private Material error;

	[SerializeField]
	private MeshRenderer buttonRenderer;

	[SerializeField]
	private LogicUnitBase parentLogicUnit;

	[SerializeField]
	private Collider buttonCollider;

	private UniTask _errorFlashTask;

	private CancellationTokenSource _errorFlashCancel;

	private static readonly Vector3 SoundOffset = new Vector3(0.13f, 0.14f, 0.12f);

	private LogicUnitButtonState _currentState;

	public void Awake()
	{
		if (parentLogicUnit == null)
		{
			parentLogicUnit = GetComponentInParent<LogicUnitBase>();
		}
	}

	public void RefreshState()
	{
		if (parentLogicUnit == null)
		{
			return;
		}
		LogicUnitButtonState logicUnitButtonState;
		if (parentLogicUnit.Powered)
		{
			logicUnitButtonState = LogicUnitButtonState.OnPowered;
			if (parentLogicUnit.Error == 1)
			{
				logicUnitButtonState = LogicUnitButtonState.Error;
			}
		}
		else
		{
			logicUnitButtonState = LogicUnitButtonState.Off;
		}
		if (logicUnitButtonState == _currentState)
		{
			return;
		}
		_currentState = logicUnitButtonState;
		switch (_currentState)
		{
		case LogicUnitButtonState.Off:
			if (parentLogicUnit.ShouldPlayLogicSound)
			{
				parentLogicUnit.PlayPooledAudioSound(Defines.Sounds.LogicOffBeep, SoundOffset);
			}
			buttonRenderer.material = powerOff;
			break;
		case LogicUnitButtonState.OnPowered:
			if (parentLogicUnit.ShouldPlayLogicSound)
			{
				parentLogicUnit.PlayPooledAudioSound(Defines.Sounds.LogicOnBeep, SoundOffset);
			}
			buttonRenderer.material = powerOn;
			break;
		case LogicUnitButtonState.Error:
			if (_errorFlashTask.Status != UniTaskStatus.Pending)
			{
				_errorFlashTask = ErrorFlashing();
			}
			break;
		}
	}

	public void PlayButtonInteractSound(bool on)
	{
		if (!(parentLogicUnit == null))
		{
			parentLogicUnit.PlayPooledAudioSound(on ? Defines.Sounds.LogicOn : Defines.Sounds.LogicOff, SoundOffset);
		}
	}

	private async UniTask ErrorFlashing()
	{
		while (parentLogicUnit != null && _currentState == LogicUnitButtonState.Error)
		{
			if (parentLogicUnit.ShouldPlayLogicSound)
			{
				parentLogicUnit.PlayPooledAudioSound(Defines.Sounds.LogicErrorBeep, SoundOffset);
			}
			buttonRenderer.material = errorEmissive;
			await UniTask.Delay(250);
			if (_currentState != LogicUnitButtonState.Error)
			{
				break;
			}
			buttonRenderer.material = error;
			await UniTask.Delay(250);
			if (_currentState != LogicUnitButtonState.Error)
			{
				break;
			}
		}
	}
}
