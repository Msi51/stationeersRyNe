using Cysharp.Threading.Tasks;
using DG.Tweening;
using Objects.Electrical;
using Trading.Waypoints;
using UnityEngine;

namespace Trading;

public class TraderShuttleAnimationHandler : MonoBehaviour
{
	[SerializeField]
	private Animator _animator;

	private bool _isWaitingForAnimation;

	private float _moveDuration = 0.05f;

	private readonly int StateParameterHash = Animator.StringToHash("State");

	public TraderShuttle TraderShuttle { get; set; }

	public bool WaitingForAnimation => _isWaitingForAnimation;

	public async UniTask DepartInvalidOnPad()
	{
		if (_isWaitingForAnimation)
		{
			await WaitForAnimation();
		}
		if (!TraderShuttle)
		{
			return;
		}
		await LiftUp();
		if ((bool)TraderShuttle)
		{
			await RotateTowards(TraderShuttle.Transform.forward * -1f);
			if ((bool)TraderShuttle)
			{
				await LeaveAtmosphere();
			}
		}
	}

	public async UniTask DepartInvalidOnArrival()
	{
		if (_isWaitingForAnimation)
		{
			await WaitForAnimation();
		}
		if ((bool)TraderShuttle)
		{
			await RotateTowards(TraderShuttle.Transform.forward * -1f);
			if ((bool)TraderShuttle)
			{
				await LeaveAtmosphere();
			}
		}
	}

	public async UniTask DepartInvalidOnDepart()
	{
		if (_isWaitingForAnimation)
		{
			await WaitForAnimation();
		}
		if ((bool)TraderShuttle)
		{
			await LeaveAtmosphere();
		}
	}

	public void SetShuttlePositionAndRotation(Vector3 position, Vector3 rotation)
	{
		TraderShuttle.Transform.position = position;
		TraderShuttle.TargetPosition = position;
		Quaternion quaternion = Quaternion.Euler(rotation);
		TraderShuttle.Transform.rotation = quaternion;
		TraderShuttle.TargetRotation = quaternion;
	}

	public void LandImmediate(IWaypoint waypoint)
	{
		SetAnimationState(ShuttleAnimationState.CloseDoors);
	}

	public async UniTask EnterAtmosphere(IWaypoint waypoint)
	{
		SetAnimationState(ShuttleAnimationState.Approach);
		await WaitForAnimation();
	}

	private void EnterAtmosphereFinished()
	{
		_isWaitingForAnimation = false;
		SetAnimationState(ShuttleAnimationState.Idle);
	}

	public async UniTask RotateTowards(Vector3 forward)
	{
		Vector3 vector = new Vector3(0f, Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y, 0f);
		Quaternion b = Quaternion.Euler(vector);
		float duration = Quaternion.Angle(TraderShuttle.Transform.rotation, b) / 90f;
		await DOTween.To(() => TraderShuttle.TargetRotation, delegate(Quaternion value)
		{
			TraderShuttle.TargetRotation = value;
		}, vector, duration).SetEase(Ease.InOutSine).AsyncWaitForCompletion()
			.AsUniTask();
	}

	public async UniTask MoveTo(IWaypoint toWaypoint, IWaypoint fromWaypoint, float moveTimeMultiplier = 1f)
	{
		Vector3 vector = TraderShuttle.TargetPosition - toWaypoint.WaypointPosition;
		float distance = vector.magnitude;
		if (Mathf.Approximately(distance, 0f))
		{
			return;
		}
		if (!Mathf.Approximately(vector.x, 0f) || !Mathf.Approximately(vector.z, 0f))
		{
			await RotateTowards(-vector);
		}
		if ((bool)TraderShuttle)
		{
			int num;
			if (toWaypoint is LandingPadTile || toWaypoint is LandingPadCenter)
			{
				IWaypoint nextWaypoint = toWaypoint.NextWaypoint;
				num = ((nextWaypoint is LandingPadCenter || nextWaypoint is LandingPadTaxiTile || nextWaypoint is LandingPadTaxiThreshold) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			float num2 = ((num != 0) ? 4f : 2f);
			float num3 = TraderShuttle?.LandingPadCenter?.CurrentTradingContact?.MovementSpeedMultiplier ?? 1f;
			float duration = _moveDuration * distance * num2 * moveTimeMultiplier / num3;
			await DOTween.To(() => TraderShuttle.TargetPosition, delegate(Vector3 value)
			{
				TraderShuttle.TargetPosition = value;
			}, toWaypoint.WaypointPosition, duration).SetEase(Ease.Linear).AsyncWaitForCompletion()
				.AsUniTask();
		}
	}

	public async UniTask TouchDown()
	{
		SetAnimationState(ShuttleAnimationState.Land);
		await WaitForAnimation();
	}

	private void TouchDownFinished()
	{
		_isWaitingForAnimation = false;
	}

	public async UniTask OpenDoors()
	{
		SetAnimationState(ShuttleAnimationState.OpenDoors);
		await WaitForAnimation();
	}

	public async UniTask CloseDoors()
	{
		if (GetAnimationState() != ShuttleAnimationState.CloseDoors)
		{
			SetAnimationState(ShuttleAnimationState.CloseDoors);
			await WaitForAnimation();
		}
	}

	private void OpenDoorsFinished()
	{
		_isWaitingForAnimation = false;
	}

	private void CloseDoorsFinished()
	{
		_isWaitingForAnimation = false;
	}

	public async UniTask LiftUp()
	{
		if (GetAnimationState() == ShuttleAnimationState.OpenDoors)
		{
			SetAnimationState(ShuttleAnimationState.CloseDoors);
			await WaitForAnimation();
		}
		if ((bool)TraderShuttle)
		{
			SetAnimationState(ShuttleAnimationState.LiftUp);
			await WaitForAnimation();
		}
	}

	private void LiftUpFinished()
	{
		_isWaitingForAnimation = false;
		SetAnimationState(ShuttleAnimationState.Idle);
	}

	public async UniTask LeaveAtmosphere()
	{
		SetAnimationState(ShuttleAnimationState.Depart);
		await WaitForAnimation();
	}

	private void LeaveAtmosphereFinished()
	{
		_isWaitingForAnimation = false;
		TraderShuttle.AudioHandler.StopAllSound();
	}

	public void SetAnimationState(ShuttleAnimationState state)
	{
		_animator.SetInteger(StateParameterHash, (int)state);
	}

	public ShuttleAnimationState GetAnimationState()
	{
		return (ShuttleAnimationState)_animator.GetInteger(StateParameterHash);
	}

	private async UniTask WaitForAnimation()
	{
		_isWaitingForAnimation = true;
		while ((bool)TraderShuttle && _isWaitingForAnimation)
		{
			await UniTask.WaitForEndOfFrame();
		}
	}
}
