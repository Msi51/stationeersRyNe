using System;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

[Serializable]
public class Knob
{
	public GameObject KnobGameObject;

	private Transform _knobTransform;

	private float _knobRotation;

	private Quaternion _knobBaseRotation;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum = 240f;

	[SerializeField]
	private Vector3 rotationAxis = Vector3.forward;

	private Thing _parentThing;

	[SerializeField]
	private bool _invertRotation;

	public void Initialize(Thing parentThing)
	{
		_knobTransform = KnobGameObject.transform;
		_knobBaseRotation = _knobTransform.localRotation;
		_parentThing = parentThing;
	}

	public void PlayKnobSound(int setting, bool increase, int maxSetting)
	{
		if (!GameManager.IsBatchMode)
		{
			int num = setting;
			num = ((!increase) ? (num - 1) : (num + 1));
			if (num >= 0 && num <= maxSetting)
			{
				Singleton<AudioManager>.Instance.PlayAudioClipsData(_parentThing, Defines.Sounds.DialTurnHash, _knobTransform);
			}
		}
	}

	public async UniTaskVoid SetKnob(int value, int maxValue, int minValue = 0)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (!_knobTransform.GetCancellationTokenOnDestroy().IsCancellationRequested && _knobTransform != null && _parentThing != null)
		{
			int num = maxValue - minValue;
			if (num == 0)
			{
				num = 1;
			}
			_knobRotation = Mathf.Lerp(NeedleMinimum, NeedleMaximum, (float)(value - minValue) / (float)num);
			_knobTransform.localRotation = _knobBaseRotation;
			Vector3 eulers = rotationAxis * (_invertRotation ? (0f - _knobRotation) : _knobRotation);
			_knobTransform.Rotate(eulers, Space.Self);
		}
	}
}
