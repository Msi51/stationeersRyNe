using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts;

[Serializable]
public class RotatableBehaviour
{
	private IRotatable _parentRotatable;

	private double _targetVertical;

	private double _targetHorizontal;

	public float MaxAudibleSquareDistance = 25f;

	public UniTask _currentTask;

	private bool _playMovingHorizontalSound;

	private bool _playMovingVerticalSound;

	public double TargetVertical
	{
		get
		{
			return _targetVertical;
		}
		set
		{
			if (double.IsNaN(value))
			{
				return;
			}
			SyncTargetVertical(value);
			if (NetworkManager.IsServer)
			{
				Thing getAsThing = _parentRotatable.GetAsThing;
				if ((object)getAsThing != null)
				{
					getAsThing.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public double TargetHorizontal
	{
		get
		{
			return _targetHorizontal;
		}
		set
		{
			if (double.IsNaN(value))
			{
				return;
			}
			SyncTargetHorizontal(value);
			if (NetworkManager.IsServer)
			{
				Thing getAsThing = _parentRotatable.GetAsThing;
				if ((object)getAsThing != null)
				{
					getAsThing.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	private Thing Parent => _parentRotatable?.GetAsThing;

	public bool IsHorizontal
	{
		get
		{
			if (_parentRotatable != null)
			{
				return Math.Abs(_parentRotatable.Horizontal - TargetHorizontal) < (double)_parentRotatable.RotationTolerance;
			}
			return true;
		}
	}

	public bool IsVertical
	{
		get
		{
			if (_parentRotatable != null)
			{
				return Math.Abs(_parentRotatable.Vertical - TargetVertical) < (double)_parentRotatable.RotationTolerance;
			}
			return true;
		}
	}

	public float MovementSpeedHorizontal => (float)(180.0 / _parentRotatable.MaximumHorizontal) * _parentRotatable.MovementSpeedHorizontal * Time.deltaTime;

	public float MovementSpeedVertical => (float)(180.0 / _parentRotatable.MaximumVertical) * _parentRotatable.MovementSpeedVertical * Time.deltaTime;

	public bool IsMoving => _currentTask.Status == UniTaskStatus.Pending;

	public bool PlayMovingHorizontalSound
	{
		get
		{
			return _playMovingHorizontalSound;
		}
		set
		{
			if (Parent != null && value != _playMovingHorizontalSound)
			{
				if (value)
				{
					Parent.PlaySound(Defines.Sounds.MovingSoundHorizontalStartHash);
					Parent.PlaySound(Defines.Sounds.MovingSoundHorizontalHash);
				}
				else
				{
					Parent.StopSound(Defines.Sounds.MovingSoundHorizontalHash);
					Parent.PlaySound(Defines.Sounds.MovingSoundHorizontalFinishHash);
				}
			}
			_playMovingHorizontalSound = value;
		}
	}

	public bool PlayMovingVerticalSound
	{
		get
		{
			return _playMovingVerticalSound;
		}
		set
		{
			if (Parent != null && value != _playMovingVerticalSound)
			{
				if (value)
				{
					Parent.PlaySound(Defines.Sounds.MovingSoundVerticalStartHash);
					Parent.PlaySound(Defines.Sounds.MovingSoundVerticalHash);
				}
				else
				{
					Parent.StopSound(Defines.Sounds.MovingSoundVerticalHash);
					Parent.PlaySound(Defines.Sounds.MovingSoundVerticalFinishHash);
				}
			}
			_playMovingVerticalSound = value;
		}
	}

	public RotatableBehaviour(IRotatable parentRotatable)
	{
		_parentRotatable = parentRotatable;
	}

	public void MoveToTarget()
	{
		if (_currentTask.Status != UniTaskStatus.Pending)
		{
			_currentTask = DoMoveTask();
		}
	}

	public void SyncTargetVertical(double f)
	{
		_targetVertical = f;
		MoveToTarget();
		_parentRotatable.UpdateAnimator().Forget();
	}

	public void SyncTargetHorizontal(double f)
	{
		_targetHorizontal = f;
		MoveToTarget();
		_parentRotatable.UpdateAnimator().Forget();
	}

	public void OnClientStart()
	{
		_parentRotatable.UpdateAnimator();
		_parentRotatable.Horizontal = TargetHorizontal;
		_parentRotatable.Vertical = TargetVertical;
	}

	private async UniTask DoMoveTask()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if ((object)Parent == null || Parent.IsCursor || Parent.IsBroken || !_parentRotatable.CanRotate())
		{
			return;
		}
		_parentRotatable.UpdateAnimator();
		if (GameManager.GameState != GameState.Running)
		{
			_parentRotatable.Horizontal = TargetHorizontal;
			_parentRotatable.Vertical = TargetVertical;
			return;
		}
		while ((object)Parent != null)
		{
			await UniTask.NextFrame();
			if ((object)InventoryManager.ParentHuman != null && Vector3.SqrMagnitude(InventoryManager.ParentHuman.Position - Parent.Position) < MaxAudibleSquareDistance)
			{
				PlayMovingHorizontalSound = !IsHorizontal;
				PlayMovingVerticalSound = !IsVertical;
			}
			if (!IsHorizontal)
			{
				if ((_parentRotatable.Horizontal - TargetHorizontal >= 0.0 && _parentRotatable.Horizontal - TargetHorizontal < 0.5) || (_parentRotatable.Horizontal - TargetHorizontal < 0.0 && TargetHorizontal - _parentRotatable.Horizontal >= 0.5))
				{
					double num = RocketMath.ModuloCorrect(_parentRotatable.Horizontal - (double)MovementSpeedHorizontal, 1.0);
					if (num < TargetHorizontal && (_parentRotatable.Horizontal >= TargetHorizontal || _parentRotatable.Horizontal - (double)MovementSpeedHorizontal < 0.0))
					{
						_parentRotatable.Horizontal = TargetHorizontal;
					}
					else
					{
						_parentRotatable.Horizontal = num;
					}
				}
				else
				{
					double num2 = RocketMath.ModuloCorrect(_parentRotatable.Horizontal + (double)MovementSpeedHorizontal, 1.0);
					if (num2 > TargetHorizontal && (_parentRotatable.Horizontal <= TargetHorizontal || _parentRotatable.Horizontal + (double)MovementSpeedHorizontal > 1.0))
					{
						_parentRotatable.Horizontal = TargetHorizontal;
					}
					else
					{
						_parentRotatable.Horizontal = num2;
					}
				}
			}
			if (!IsVertical)
			{
				if (TargetVertical > _parentRotatable.Vertical)
				{
					_parentRotatable.Vertical += MovementSpeedVertical;
					if (_parentRotatable.Vertical > TargetVertical)
					{
						_parentRotatable.Vertical = TargetVertical;
					}
				}
				else if (TargetVertical < _parentRotatable.Vertical)
				{
					_parentRotatable.Vertical -= MovementSpeedVertical;
					if (_parentRotatable.Vertical < TargetVertical)
					{
						_parentRotatable.Vertical = TargetVertical;
					}
				}
			}
			if (_parentRotatable.Vertical > 1.0)
			{
				_parentRotatable.Vertical -= 1.0;
			}
			if ((IsHorizontal && IsVertical) || Parent.IsBroken || !_parentRotatable.CanRotate())
			{
				_parentRotatable.RunAfterAnimation();
				PlayMovingHorizontalSound = false;
				PlayMovingVerticalSound = false;
				break;
			}
		}
	}

	public static double RoundRatioToNearestDegree(double ratioValue, double maxValue)
	{
		double num = Math.Round(ratioValue * maxValue);
		RocketMath.ModuloCorrect(num, maxValue);
		return num / maxValue;
	}
}
