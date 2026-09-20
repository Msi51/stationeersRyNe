using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class SettingWheel : MonoBehaviour
{
	public DeviceAtmospherics Parent;

	[FormerlySerializedAs("Collider")]
	public Transform Wheel;

	public float WheelSpinSpeed = 10f;

	private Quaternion _wheelBaseRotation;

	public SettingWheelMode Mode;

	public static readonly int WheelTurnHash = Animator.StringToHash("WheelTurn");

	public static readonly int WheelTurnCloseHash = Animator.StringToHash("WheelTurnClose");

	public static readonly int WheelTurnSmallHash = Animator.StringToHash("WheelTurnSmall");

	private float _lastAngle;

	public float OutputSetting => Mode switch
	{
		SettingWheelMode.OutputSetting => Parent.OutputSetting, 
		SettingWheelMode.OutputSetting2 => (float)((ISetable2)Parent).Setting2, 
		_ => Parent.OutputSetting, 
	};

	public void Awake()
	{
		if (!Parent.IsCursor && (bool)Wheel)
		{
			_wheelBaseRotation = Wheel.localRotation;
			Wheel.localRotation = _wheelBaseRotation * Quaternion.AngleAxis(OutputSetting * 10f, Vector3.up);
		}
	}

	public async UniTaskVoid CheckWheel()
	{
		await UniTask.SwitchToMainThread();
		if ((bool)Wheel)
		{
			RotateWheel();
		}
	}

	public void OnDeserialize()
	{
		if ((bool)Wheel)
		{
			Wheel.localRotation = Quaternion.AngleAxis((0f - OutputSetting) * 10f, Vector3.up);
		}
	}

	public void SetLastAngle()
	{
		_lastAngle = OutputSetting * WheelSpinSpeed;
	}

	public void SetRotation()
	{
		SetLastAngle();
		if (!(Wheel == null))
		{
			Wheel.localRotation = _wheelBaseRotation;
			Wheel.Rotate(0f, _lastAngle, 0f, Space.Self);
		}
	}

	private void RotateWheel()
	{
		Quaternion quaternion = Quaternion.AngleAxis(OutputSetting * 10f, Vector3.up);
		Wheel.DOLocalRotateQuaternion(_wheelBaseRotation * quaternion, 0.5f);
	}

	public void ThreadedCheck()
	{
		CheckWheel().Forget();
	}

	public static void PlayWheelSound(Thing parentThing, Transform settingWheelTransform, bool increaseSetting, bool isAltInteraction, float outputSetting, float minSetting, float maxSetting, float settingIncrement, float altSettingIncrement)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (increaseSetting)
		{
			if (!(outputSetting >= maxSetting))
			{
				float num = outputSetting + (isAltInteraction ? altSettingIncrement : settingIncrement);
				Singleton<AudioManager>.Instance.PlayAudioClipsData(parentThing, (num >= maxSetting) ? WheelTurnCloseHash : (isAltInteraction ? WheelTurnSmallHash : WheelTurnHash), settingWheelTransform.localPosition);
			}
		}
		else if (!(outputSetting <= minSetting))
		{
			float num2 = outputSetting - (isAltInteraction ? altSettingIncrement : settingIncrement);
			Singleton<AudioManager>.Instance.PlayAudioClipsData(parentThing, (num2 <= minSetting) ? WheelTurnCloseHash : (isAltInteraction ? WheelTurnSmallHash : WheelTurnHash), settingWheelTransform.localPosition);
		}
	}
}
