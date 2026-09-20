using System.Threading;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects;

public class SwitchOnOff : DevicePart
{
	[SerializeField]
	protected Transform switchTransform;

	[SerializeField]
	protected Vector3 offPosition = new Vector3(-13.5f, 0f, 0f);

	[SerializeField]
	protected Vector3 onPosition = new Vector3(13.5f, 0f, 0f);

	[SerializeField]
	protected Material off;

	[SerializeField]
	protected Material on;

	[SerializeField]
	protected Material onPowered;

	[SerializeField]
	protected Material[] error;

	[SerializeField]
	protected MeshRenderer switchRenderer;

	[SerializeField]
	private Collider interactionTrigger;

	protected SwitchColorState _currentColorState;

	protected bool IsOn;

	private CancellationTokenSource _errorStateCancellationTokenSource;

	private static readonly float AudibleSqrDistance = 121f;

	public Collider InteractOnOffCollider => interactionTrigger;

	public override void RefreshState(bool skipAnim = false)
	{
		if (!(parentThing == null))
		{
			RefreshColorState(skipAnim);
			RefreshPositionState(skipAnim);
		}
	}

	protected virtual void RefreshPositionState(bool skipAnim)
	{
		if (parentThing.OnOff != IsOn)
		{
			IsOn = parentThing.OnOff;
			switchTransform.localRotation = Quaternion.Euler(IsOn ? onPosition : offPosition);
			if (IsAudible(parentThing) && !skipAnim)
			{
				parentThing.PlayPooledAudioSound(IsOn ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, Transform.localPosition);
			}
		}
	}

	protected virtual void RefreshColorState(bool skipAnim)
	{
		SwitchColorState switchColorState = SwitchColorState.Off;
		if (parentThing.OnOff)
		{
			switchColorState = ((parentThing.Powered || !parentThing.HasPowerState) ? SwitchColorState.OnPowered : SwitchColorState.On);
		}
		if ((parentThing.Error != 0 && parentThing.Powered) || (parentThing.Error != 0 && !parentThing.HasPowerState))
		{
			switchColorState = SwitchColorState.Error;
		}
		if (switchColorState != _currentColorState)
		{
			_currentColorState = switchColorState;
			CancelErrorAnimation();
			switch (_currentColorState)
			{
			case SwitchColorState.Off:
				switchRenderer.material = off;
				break;
			case SwitchColorState.On:
				switchRenderer.material = on;
				break;
			case SwitchColorState.OnPowered:
				switchRenderer.material = onPowered;
				break;
			case SwitchColorState.Error:
				_errorStateCancellationTokenSource = new CancellationTokenSource();
				ErrorAnimation(_errorStateCancellationTokenSource.Token).Forget();
				break;
			}
		}
	}

	private void CancelErrorAnimation()
	{
		if (_errorStateCancellationTokenSource != null)
		{
			_errorStateCancellationTokenSource.Cancel();
			_errorStateCancellationTokenSource.Dispose();
			_errorStateCancellationTokenSource = null;
		}
	}

	private async UniTaskVoid ErrorAnimation(CancellationToken cancellationToken)
	{
		if (IsAudible(parentThing))
		{
			parentThing.PlayPooledAudioSound(Defines.Sounds.Error, Transform.localPosition);
		}
		while (parentThing != null && _currentColorState == SwitchColorState.Error && !cancellationToken.IsCancellationRequested)
		{
			if (parentThing != null && !parentThing.IsBeingDestroyed && switchRenderer != null)
			{
				switchRenderer.material = error[0];
			}
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			if (parentThing != null && !parentThing.IsBeingDestroyed && switchRenderer != null)
			{
				switchRenderer.material = error[1];
			}
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		}
		CancelErrorAnimation();
	}

	protected static bool IsAudible(Thing parentThing)
	{
		if (parentThing == null || InventoryManager.Parent == null)
		{
			return false;
		}
		return Vector3.SqrMagnitude(parentThing.Position - InventoryManager.ParentPosition) < AudibleSqrDistance;
	}

	private void OnDisable()
	{
		CancelErrorAnimation();
	}
}
