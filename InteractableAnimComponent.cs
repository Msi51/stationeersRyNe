using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Cysharp.Threading.Tasks;
using Sound;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public abstract class InteractableAnimComponent : GameBase
{
	[SerializeField]
	protected Thing parentThing;

	[FormerlySerializedAs("animSpeed")]
	[SerializeField]
	protected float time = 1f;

	[SerializeField]
	protected Collider interactableCollider;

	private bool _initialised;

	public int DebugState = -1;

	private readonly CancellationTokenWrapper _animationCancellation = new CancellationTokenWrapper();

	protected PooledAudioSource _assignedAudio;

	public Collider InteractableCollider => interactableCollider;

	protected virtual float AudibleSqrDistance => 400f;

	protected virtual Vector3 SoundPosition => Transform.position - parentThing.Position;

	public abstract InteractableType AssignedAction { get; }

	protected abstract int InteractableState { get; }

	private void Awake()
	{
		Init();
	}

	protected virtual void Init()
	{
		if (!_initialised)
		{
			_initialised = true;
			if (!parentThing)
			{
				parentThing = GetComponentInParent<Thing>();
			}
		}
	}

	public void SetParentThing(Thing parent)
	{
		parentThing = parent;
	}

	public void RefreshState(bool skipAnimation = false)
	{
		Init();
		if (!parentThing)
		{
			return;
		}
		if (time == 0f)
		{
			skipAnimation = true;
		}
		DebugState = InteractableState;
		if (skipAnimation)
		{
			_animationCancellation.Cancel();
			OnAnimationStart();
			AnimateImmediate();
			OnAnimationCompleted();
		}
		else if (ShouldUpdateAnimation())
		{
			if (!GameManager.IsBatchMode)
			{
				TriggerAudio();
			}
			_animationCancellation.Cancel();
			_animationCancellation.Initialize();
			Animate(_animationCancellation.Token).Forget();
		}
	}

	protected abstract void AnimateImmediate();

	protected abstract bool ShouldUpdateAnimation();

	protected abstract UniTask Animate(CancellationToken animationCancellationToken);

	protected virtual void OnAnimationStart()
	{
	}

	protected virtual void OnAnimationCompleted()
	{
	}

	protected virtual void TriggerAudio()
	{
	}

	protected virtual bool IsAudible()
	{
		if (GameManager.IsBatchMode || !parentThing || !InventoryManager.Parent)
		{
			return false;
		}
		return Vector3.SqrMagnitude(parentThing.Position - InventoryManager.ParentPosition) < AudibleSqrDistance;
	}

	protected void PlaySound(int soundHash)
	{
		if ((bool)_assignedAudio && parentThing.PooledAudioSources.Contains(_assignedAudio))
		{
			_assignedAudio.Stop();
		}
		_assignedAudio = null;
		if (IsAudible())
		{
			_assignedAudio = parentThing.PlayPooledAudioSound(soundHash, SoundPosition);
		}
	}
}
