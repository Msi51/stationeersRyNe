using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class Interactable : SlotDisplayBase
{
	public static int ColorState = Animator.StringToHash("Color");

	public static int OpenState = Animator.StringToHash("Open");

	public static int CloseState = Animator.StringToHash("Close");

	public static int OnState = Animator.StringToHash("On");

	public static int OffState = Animator.StringToHash("Off");

	public static int LockState = Animator.StringToHash("Lock");

	public static int ModeState = Animator.StringToHash("Mode");

	public static int ExportState = Animator.StringToHash("Export");

	public static int Export2State = Animator.StringToHash("Export2");

	public static int ImportState = Animator.StringToHash("Import");

	public static int Import2State = Animator.StringToHash("Import2");

	public static int ImportEnteredState = Animator.StringToHash("ImportEntered");

	public static int Import2EnteredState = Animator.StringToHash("Import2Entered");

	public static int ImportExitedState = Animator.StringToHash("ImportExited");

	public static int Import2ExitedState = Animator.StringToHash("Import2Exited");

	public static int OnOffState = Animator.StringToHash("OnOff");

	public static int ActivateState = Animator.StringToHash("Activate");

	public static int PoweredState = Animator.StringToHash("Powered");

	public static int ErrorState = Animator.StringToHash("Error");

	public static int Button1State = Animator.StringToHash("Button1");

	public static int Button2State = Animator.StringToHash("Button2");

	public static int Button3State = Animator.StringToHash("Button3");

	public static int AccessState = Animator.StringToHash("Access");

	public static int CreditState = Animator.StringToHash("CreditCard");

	public string StringKey;

	[ReadOnly]
	public int StringHash;

	public Thing Parent;

	public Collider Collider;

	[ReadOnly]
	public int ParentInteractableIndex = -1;

	[Tooltip("This is a fake collider used for faking the position of the collider used for dragging in different positions")]
	public Collider FakeCollider;

	[ReadOnly]
	public Bounds Bounds;

	public InteractableType Action;

	[Tooltip("The animator found on the parent object. Typically used by interactables.")]
	public Animator Animator;

	public bool JoinInProgressSync;

	[Tooltip("The layer that the interactable is on. Usually 0 (base)")]
	public int Layer;

	[Tooltip("Can the interaction occur via context menu")]
	public bool CanKeyInteract;

	[Tooltip("The key mapping for this action")]
	public string KeyMap;

	[NonSerialized]
	[ReadOnly]
	public Slot Slot;

	[ReadOnly]
	[Tooltip("Sound Effects for the different states")]
	public List<GameAudioEvent> AssociatedAudioEvents = new List<GameAudioEvent>();

	[ReadOnly]
	public string ActionName;

	private bool _hasAnimator;

	private int _propertyId = -1;

	private int _index = -1;

	private bool _updateSoundEffects;

	private static UniqueQueue<Interactable> _scheduledSoundEvents = new UniqueQueue<Interactable>();

	private static string _import = "Import{0}";

	private static string _import2 = "Import2{0}";

	private static string _export = "Export{0}";

	private static string _export2 = "Export2{0}";

	private static string _mode = "Mode{0}";

	private static string _activate = "Activate{0}";

	[Tooltip("Set the default state for the interactable here if not caching values on Animator. In most cases this should be 0, with only some exceptions (i.e. jetpack stabilizer).")]
	[SerializeField]
	private int _state;

	private bool _isDirty;

	[NonSerialized]
	public Bounds OriginalBounds;

	public static Queue<InteractionInstance> QueuedInteractions = new Queue<InteractionInstance>();

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(StringKey))
			{
				return Localization.GetName(this);
			}
			return EnumCollections.InteractableTypes.GetName(Action);
		}
	}

	public Interactable ParentInteractable
	{
		get
		{
			if (ParentInteractableIndex == -1)
			{
				return null;
			}
			return Parent.Interactables[ParentInteractableIndex];
		}
	}

	public int InteractableId
	{
		get
		{
			if (_index <= 0)
			{
				_index = Parent.Interactables.FindIndex((Interactable i) => i == this);
			}
			return _index;
		}
	}

	public int PropertyId
	{
		get
		{
			_propertyId = Animator.StringToHash(Action.ToString());
			return _propertyId;
		}
	}

	public string ContextualName => Parent.GetContextualName(this);

	public int State
	{
		get
		{
			if (JoinInProgressSync)
			{
				if (_hasAnimator && (bool)Animator)
				{
					if (!Animator.isInitialized)
					{
						return _state;
					}
					return Animator.GetInteger(PropertyId);
				}
				return ParentInteractable?.State ?? _state;
			}
			return 0;
		}
		set
		{
			if (!Animator && ParentInteractable != null)
			{
				ParentInteractable.State = value;
				return;
			}
			int state = _state;
			_state = value;
			Parent.OnInteractableStateChanged(this, _state, state);
			if (Settings.SoundOn && AssociatedAudioEvents.Count > 0)
			{
				lock (_scheduledSoundEvents)
				{
					_scheduledSoundEvents.Enqueue(this);
				}
			}
			Parent.OnInteractableUpdated(this);
			if (NetworkManager.IsServer)
			{
				Parent.NetworkUpdateFlags |= 2;
				IsDirty = true;
			}
		}
	}

	public bool IsDirty
	{
		get
		{
			return _isDirty;
		}
		set
		{
			_isDirty = value;
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}

	public string ToTooltip()
	{
		return $"<color=yellow>{DisplayName}</color>";
	}

	public string ToContextualTooltip()
	{
		return $"<color=yellow>{ContextualName}</color>";
	}

	public SelectionInstance GetSelection()
	{
		SelectionInstance obj = new SelectionInstance
		{
			Position = Parent.ThingTransformPosition,
			Rotation = Parent.ThingTransform.rotation,
			ParentThingRefernceId = Parent.ReferenceId,
			InteractableId = InteractableId
		};
		Bounds bounds = Bounds;
		Vector3 min = bounds.min;
		Transform transform = Collider.transform;
		Vector3 lossyScale = transform.lossyScale;
		min.x = lossyScale.x * min.x;
		min.y = lossyScale.y * min.y;
		min.z = lossyScale.z * min.z;
		Vector3 max = bounds.max;
		max.x = lossyScale.x * max.x;
		max.y = lossyScale.y * max.y;
		max.z = lossyScale.z * max.z;
		bounds.SetMinMax(min, max);
		obj.Position = transform.position;
		obj.Rotation = transform.rotation;
		bounds.min -= Vector3.one * 0.01f;
		bounds.max += Vector3.one * 0.01f;
		obj.Bounds = bounds;
		return obj;
	}

	public static void PlayScheduledSounds()
	{
		lock (_scheduledSoundEvents)
		{
			while (_scheduledSoundEvents.Count > 0)
			{
				Interactable interactable = _scheduledSoundEvents.Dequeue();
				if (interactable?.Parent != null)
				{
					interactable.PlayInteractableSounds(interactable.Parent.SuppressSound);
				}
			}
		}
	}

	private void PlayInteractableSounds(bool suppressSound)
	{
		bool flag = GameManager.GameState != GameState.Running;
		foreach (GameAudioEvent associatedAudioEvent in AssociatedAudioEvents)
		{
			if (associatedAudioEvent.IsValid)
			{
				if ((!flag || associatedAudioEvent.ClipsData.IsLooping) && (!(Parent.SuppressSound || suppressSound) || associatedAudioEvent.ClipsData.IsLooping))
				{
					associatedAudioEvent.Trigger(1f, 1f, flag);
				}
			}
			else if (associatedAudioEvent.ClipsData.IsLooping || associatedAudioEvent.StopIfInvalid)
			{
				associatedAudioEvent.Stop();
			}
		}
		_updateSoundEffects = false;
	}

	public void Initialize()
	{
		_hasAnimator = Animator != null;
		int num = Parent.Slots.FindIndex((Slot s) => s.Action == Action);
		if (num >= 0)
		{
			Slot = Parent.Slots[num];
			Slot.Interactable = this;
		}
		else
		{
			Slot = null;
		}
	}

	public void Play(int stateHash)
	{
		if (!(Parent == null) && Parent.ReferenceId != 0L && Animator.HasState(Layer, stateHash))
		{
			Animator.Play(stateHash, Layer, 1f);
		}
	}

	public void SetState()
	{
		if ((bool)Animator && JoinInProgressSync)
		{
			switch (Action)
			{
			case InteractableType.Import:
				Play(Animator.StringToHash(string.Format(_import, Parent.Importing)));
				break;
			case InteractableType.Import2:
				Play(Animator.StringToHash(string.Format(_import2, Parent.Importing2)));
				break;
			case InteractableType.Export:
				Play(Animator.StringToHash(string.Format(_export, Parent.Exporting)));
				break;
			case InteractableType.Export2:
				Play(Animator.StringToHash(string.Format(_export2, Parent.Exporting)));
				break;
			case InteractableType.Open:
				Play(Parent.IsOpen ? OpenState : CloseState);
				break;
			case InteractableType.OnOff:
			{
				int stateHash = ((!Animator.HasParameter(PoweredState)) ? (Parent.OnOff ? OnState : OffState) : ((Parent.OnOff && Parent.Powered) ? OnState : OffState));
				Play(stateHash);
				break;
			}
			case InteractableType.Mode:
				Play(Animator.StringToHash(string.Format(_mode, Parent.Mode)));
				break;
			case InteractableType.Activate:
				Play(Animator.StringToHash(string.Format(_activate, Parent.Activate)));
				break;
			}
		}
	}

	public void UpdateDisplay()
	{
		if (Display == null || !Display.DisplayImage || !Display.SlotText)
		{
			return;
		}
		if ((bool)InventoryManager.Parent)
		{
			Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, Parent, altKey: false);
			if (!Parent.PreventInteraction(out var failResult, this, interaction))
			{
				failResult = Parent.InteractWith(this, interaction, doAction: false);
			}
			Display.SlotText.text = ContextualName;
			Display.IsDisabled = failResult.IsDisabled;
		}
		else
		{
			if (!Parent.PreventInteraction(out var failResult2, this, default(Interaction)))
			{
				failResult2 = Parent.InteractWith(this, default(Interaction), doAction: false);
			}
			Display.SlotText.text = ContextualName;
			Display.IsDisabled = failResult2.IsDisabled;
		}
	}

	public void CacheBounds()
	{
		Bounds = default(Bounds);
		BoxCollider boxCollider = Collider as BoxCollider;
		if ((bool)boxCollider)
		{
			Bounds = new Bounds(boxCollider.center, boxCollider.size);
		}
		SphereCollider sphereCollider = Collider as SphereCollider;
		if ((bool)sphereCollider)
		{
			Bounds = new Bounds(sphereCollider.center, Vector3.one * sphereCollider.radius * 2f);
		}
		CapsuleCollider capsuleCollider = Collider as CapsuleCollider;
		if ((bool)capsuleCollider)
		{
			if (capsuleCollider.direction == 0)
			{
				Bounds = new Bounds(capsuleCollider.center, new Vector3(capsuleCollider.height, capsuleCollider.radius * 2f, capsuleCollider.radius * 2f));
			}
			else if (capsuleCollider.direction == 1)
			{
				Bounds = new Bounds(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2f, capsuleCollider.height, capsuleCollider.radius * 2f));
			}
			else
			{
				Bounds = new Bounds(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2f, capsuleCollider.radius * 2f, capsuleCollider.height));
			}
		}
	}

	public void SetBounds(Thing thing)
	{
		BoxCollider boxCollider = Collider as BoxCollider;
		if (!(boxCollider == null))
		{
			boxCollider.size = thing.Bounds.size;
			boxCollider.center = new Vector3(0f, thing.Bounds.extents.y, 0f) + thing.Bounds.center;
			Bounds = thing.Bounds;
			Bounds.center = boxCollider.center;
			thing.ThingTransformLocalPosition = boxCollider.center;
		}
	}

	public void ResetBounds()
	{
		Bounds = OriginalBounds;
		BoxCollider boxCollider = Collider as BoxCollider;
		if (!(boxCollider == null))
		{
			boxCollider.size = Bounds.size;
			boxCollider.center = Bounds.center;
		}
	}

	public void PlayerInteractWith(Slot sourceSlot)
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, sourceSlot, Parent, altKey: false);
		if (GameManager.RunSimulation)
		{
			OnServer.InteractWith(this, interaction);
		}
		if (NetworkManager.IsClient)
		{
			NetworkClient.InteractWith(this, interaction);
		}
	}

	public void PlayerInteractWith()
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, Parent, altKey: false);
		if (GameManager.RunSimulation)
		{
			OnServer.InteractWith(this, interaction);
		}
		if (NetworkManager.IsClient)
		{
			NetworkClient.InteractWith(this, interaction);
		}
	}

	public async UniTaskVoid WaitThenResetInteractableState(int waitMs = 1000)
	{
		await UniTask.Delay(waitMs, ignoreTimeScale: false, PlayerLoopTiming.Update, Parent.GetCancellationTokenOnDestroy());
		OnServer.Interact(Parent, Action, 0);
	}

	public bool IsValidToSend()
	{
		return JoinInProgressSync;
	}

	public void InteractWith(Interaction interaction, bool doAction = true)
	{
		Parent.InteractWith(this, interaction, doAction);
	}

	public void Interact(int state, bool skipAnimation = true)
	{
		State = state;
		if (skipAnimation)
		{
			SetState();
		}
		Parent.OnFinishedInteractionSync(this);
	}

	private async UniTaskVoid WaitThenInteract(int state, bool skipAnimation)
	{
		int frame = 0;
		CancellationToken cancelToken = Parent.GetCancellationTokenOnDestroy();
		while (!Parent.AllowInteraction && frame < 60)
		{
			await UniTask.NextFrame(cancelToken);
			frame++;
		}
		if (!cancelToken.IsCancellationRequested)
		{
			State = state;
			if (skipAnimation)
			{
				SetState();
			}
			Parent.OnFinishedInteractionSync(this);
		}
	}

	public void InteractWhenReady(int state, bool skipAnimation)
	{
		WaitThenInteract(state, skipAnimation).Forget();
	}

	public static void DoQueuedInteractions()
	{
		lock (QueuedInteractions)
		{
			while (QueuedInteractions.Count > 0)
			{
				OnServer.Interact(QueuedInteractions.Dequeue());
			}
		}
	}

	public static void SetFromThread(Interactable interactable, int state)
	{
		interactable.DoSetFromThread(state).Forget();
	}

	private async UniTaskVoid DoSetFromThread(int state)
	{
		await UniTask.SwitchToMainThread();
		State = state;
	}
}
