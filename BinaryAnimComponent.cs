using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BinaryAnimComponent : InteractableAnimComponent
{
	private BinaryAnimState _currentState;

	private float t;

	protected virtual IKeyFrameCollection State0 { get; }

	protected virtual IKeyFrameCollection State1 { get; }

	public BinaryAnimState CurrentState
	{
		get
		{
			return _currentState;
		}
		private set
		{
			_currentState = value;
			t = value switch
			{
				BinaryAnimState.None => 0f, 
				BinaryAnimState.Off => 0f, 
				BinaryAnimState.On => 1f, 
				_ => t, 
			};
		}
	}

	protected override bool ShouldUpdateAnimation()
	{
		if (!parentThing)
		{
			return false;
		}
		int interactableState = InteractableState;
		if (interactableState == 0 || interactableState != 1)
		{
			BinaryAnimState currentState = CurrentState;
			if (currentState == BinaryAnimState.None || currentState == BinaryAnimState.OffToOn || currentState == BinaryAnimState.On)
			{
				return true;
			}
		}
		else
		{
			BinaryAnimState currentState = CurrentState;
			if (currentState == BinaryAnimState.None || currentState == BinaryAnimState.Off || currentState == BinaryAnimState.OnToOff)
			{
				return true;
			}
		}
		return false;
	}

	protected override void AnimateImmediate()
	{
		switch (InteractableState)
		{
		case 0:
			State0.Apply();
			CurrentState = BinaryAnimState.Off;
			break;
		case 1:
			State1.Apply();
			CurrentState = BinaryAnimState.On;
			break;
		}
	}

	protected override async UniTask Animate(CancellationToken token)
	{
		CurrentState = InteractableState switch
		{
			0 => BinaryAnimState.OnToOff, 
			1 => BinaryAnimState.OffToOn, 
			_ => CurrentState, 
		};
		OnAnimationStart();
		while (GameManager.GameState == GameState.Running)
		{
			BinaryAnimState currentState = CurrentState;
			if ((currentState != BinaryAnimState.OffToOn && currentState != BinaryAnimState.OnToOff) || !(parentThing != null) || parentThing.BeingDestroyed)
			{
				break;
			}
			switch (CurrentState)
			{
			case BinaryAnimState.OffToOn:
				t += Time.deltaTime / time;
				break;
			case BinaryAnimState.OnToOff:
				t -= Time.deltaTime / time;
				break;
			}
			t = Mathf.Clamp01(t);
			State1.Lerp(State0, t);
			CurrentState = InteractableState switch
			{
				0 => (t <= 0f) ? BinaryAnimState.Off : BinaryAnimState.OnToOff, 
				1 => (t >= 1f) ? BinaryAnimState.On : BinaryAnimState.OffToOn, 
				_ => CurrentState, 
			};
			await UniTask.WaitForEndOfFrame(token);
		}
		OnAnimationCompleted();
	}
}
