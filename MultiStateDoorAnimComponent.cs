using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MultiStateDoorAnimComponent : MultiStateAnimComponent
{
	private enum Phase
	{
		None,
		Opening,
		Closing,
		Open,
		Closed
	}

	private Interactable _interactable;

	private Phase _phase;

	private float _playT;

	public virtual int OpenSound => 0;

	public virtual int CloseSound => 0;

	public virtual int OpenNoPowerSound => 0;

	public virtual int CloseNoPowerSound => 0;

	public override InteractableType AssignedAction => InteractableType.Open;

	protected override int InteractableState
	{
		get
		{
			if (!(parentThing != null) || _interactable == null)
			{
				return 0;
			}
			return _interactable.State;
		}
	}

	private bool WantOpen => InteractableState == 1;

	protected override Vector3 SoundPosition => parentThing?.SoundPosition?.localPosition ?? Vector3.zero;

	protected override void Init()
	{
		base.Init();
		if (_interactable == null && parentThing != null)
		{
			_interactable = parentThing.GetInteractable(InteractableType.Open);
		}
	}

	protected override bool ShouldUpdateAnimation()
	{
		if (!parentThing)
		{
			return false;
		}
		if (_phase == Phase.None)
		{
			return true;
		}
		Phase phase;
		if (!WantOpen)
		{
			phase = _phase;
			return phase == Phase.Opening || phase == Phase.Open;
		}
		phase = _phase;
		return phase == Phase.Closing || phase == Phase.Closed;
	}

	protected override void AnimateImmediate()
	{
		bool wantOpen = WantOpen;
		_playT = (wantOpen ? base.TimelineLength : 0f);
		SampleAtTime(_playT);
		_phase = (wantOpen ? Phase.Open : Phase.Closed);
	}

	protected override async UniTask Animate(CancellationToken token)
	{
		OnAnimationStart();
		float length = base.TimelineLength;
		while (GameManager.GameState == GameState.Running && parentThing != null && !parentThing.BeingDestroyed)
		{
			bool wantOpen = WantOpen;
			_phase = (wantOpen ? Phase.Opening : Phase.Closing);
			_playT += (wantOpen ? 1f : (-1f)) * Time.deltaTime;
			_playT = Mathf.Clamp(_playT, 0f, length);
			SampleAtTime(_playT);
			if (wantOpen && _playT >= length)
			{
				_phase = Phase.Open;
				break;
			}
			if (!wantOpen && _playT <= 0f)
			{
				_phase = Phase.Closed;
				break;
			}
			await UniTask.WaitForEndOfFrame(token);
		}
		OnAnimationCompleted();
	}

	protected override void TriggerAudio()
	{
		if (parentThing.Powered)
		{
			PlaySound(WantOpen ? OpenSound : CloseSound);
		}
		else
		{
			PlaySound(WantOpen ? OpenNoPowerSound : CloseNoPowerSound);
		}
	}

	protected override void OnAnimationStart()
	{
		parentThing.OnAnimationStart();
	}

	protected override void OnAnimationCompleted()
	{
		parentThing.OnAnimationStop();
	}
}
