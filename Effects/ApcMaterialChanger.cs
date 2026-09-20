using System.Threading.Tasks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Effects;

public class ApcMaterialChanger : MaterialChanger
{
	[SerializeField]
	public Thing parent;

	private Task _errorAnimTask;

	private Task _chargingAnimTask;

	private Task _dischargingAnimTask;

	private const int APC_FLASH_DELAY = 500;

	private bool RunErrorState
	{
		get
		{
			if (parent.Error == 1 && parent.OnOff)
			{
				return parent.Powered;
			}
			return false;
		}
	}

	private bool RunChargingState
	{
		get
		{
			if (parent.Mode == 3 && parent.Error == 0)
			{
				return parent.OnOff;
			}
			return false;
		}
	}

	private bool RunDischargingState
	{
		get
		{
			if (parent.Mode == 2 && parent.Error == 0)
			{
				return parent.OnOff;
			}
			return false;
		}
	}

	public void RefreshState()
	{
		if (!parent)
		{
			return;
		}
		if (RunErrorState)
		{
			if (_errorAnimTask == null || _errorAnimTask.IsCompleted)
			{
				parent.PlayPooledAudioSound(Defines.Sounds.Error, parent.SoundPosition.localPosition);
				_errorAnimTask = ErrorAnim().AsTask();
			}
		}
		else if (parent.Error == 0 && parent.OnOff)
		{
			switch ((PowerMode)parent.Mode)
			{
			case PowerMode.Idle:
				ChangeState(Defines.Animator.Idle);
				break;
			case PowerMode.Charged:
				ChangeState(Defines.Animator.Charged);
				parent.PlayPooledAudioSound(Defines.Sounds.ApcCharged, parent.SoundPosition.localPosition);
				break;
			case PowerMode.Charging:
				if (_chargingAnimTask == null || _chargingAnimTask.IsCompleted)
				{
					_chargingAnimTask = ChargingAnim().AsTask();
				}
				break;
			case PowerMode.Discharging:
				if (_dischargingAnimTask == null || _dischargingAnimTask.IsCompleted)
				{
					_dischargingAnimTask = DischargingAnim().AsTask();
				}
				break;
			case PowerMode.Discharged:
				ChangeState(Defines.Animator.Discharged);
				parent.PlayPooledAudioSound(Defines.Sounds.ApcDischarged, parent.SoundPosition.localPosition);
				break;
			}
		}
		else
		{
			ChangeState(Defines.Animator.Idle);
		}
	}

	private async UniTask ErrorAnim()
	{
		while (RunErrorState)
		{
			ChangeState(Defines.Animator.Error0);
			await UniTask.Delay(250);
			if (!RunErrorState)
			{
				break;
			}
			ChangeState(Defines.Animator.Error1);
			await UniTask.Delay(250);
		}
		_errorAnimTask = null;
	}

	private async UniTask ChargingAnim()
	{
		while (RunChargingState)
		{
			ChangeState(Defines.Animator.Charging);
			await UniTask.Delay(500);
			if (!RunChargingState)
			{
				break;
			}
			ChangeState(Defines.Animator.Charged);
			parent.PlayPooledAudioSound(Defines.Sounds.ApcCharging, parent.SoundPosition.localPosition);
			await UniTask.Delay(500);
		}
		_errorAnimTask = null;
	}

	private async UniTask DischargingAnim()
	{
		while (RunDischargingState)
		{
			ChangeState(Defines.Animator.Charging);
			await UniTask.Delay(500);
			if (!RunDischargingState)
			{
				break;
			}
			ChangeState(Defines.Animator.Discharged);
			parent.PlayPooledAudioSound(Defines.Sounds.ApcDischarging, parent.SoundPosition.localPosition);
			await UniTask.Delay(500);
		}
		_errorAnimTask = null;
	}
}
