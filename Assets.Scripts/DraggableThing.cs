using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class DraggableThing : DynamicThing, IDraggable, IReferencable, IEvaluable
{
	public const float RENDER_DISTANCE = 60f;

	public static int AnchoredState = Animator.StringToHash("Anchored");

	public InteractableType DraggingAction = InteractableType.Button1;

	private static readonly int GrabDraggableThingHash = Animator.StringToHash("GrabDraggableThing");

	private static readonly Vector3 GrabSoundOffset = new Vector3(0f, 0.5f, 0.5f);

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(60f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void Awake()
	{
		base.Awake();
		DynamicThing.DynamicObjects.Add(this);
		if (!IsCursor && this is ICircuitHolder iCircuitHolder)
		{
			CircuitHolders.Register(iCircuitHolder);
		}
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_circuitHolderPool.CanAddToPool(densePool))
		{
			return _circuitHolderPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_circuitHolderPool.OnRemovedFrom(densePool);
	}

	public void OnJointBreak(float breakForce)
	{
		if (!GameManager.RunSimulation)
		{
			OnServer.MoveToWorld(this, base.ParentSlot, breakForce);
			return;
		}
		Transform transform = base.ParentSlot?.Location;
		Vector3 worldPosition = (transform ? transform.position : base.Position);
		Quaternion quaternion = (transform ? transform.rotation : Rotation);
		Vector3 velocity = (transform ? (transform.forward * breakForce) : Vector3.zero);
		MoveToWorld(worldPosition, quaternion, velocity, Vector3.zero);
		NetworkServer.SendToClients(new MoveToWorldMessage
		{
			ChildId = base.ReferenceId,
			IsPrecisionPlacement = true,
			Position = worldPosition,
			Rotation = quaternion,
			Velocity = velocity,
			AngularVelocity = Vector3.zero
		}, NetworkChannel.GeneralTraffic, -1L);
	}

	protected override void PhysicsOnRender(bool isRendered)
	{
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			DynamicThing.DynamicObjects.Remove(this);
			if (!IsCursor && this is ICircuitHolder iCircuitHolder)
			{
				CircuitHolders.Deregister(iCircuitHolder);
			}
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ActionStrings.Rename
		};
		Labeller labeller = sourceItem as Labeller;
		if ((bool)labeller)
		{
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action != DraggingAction || base.IsChild)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (Vector3.Distance(interaction.SourceThing.Position, base.Position) > 3f)
		{
			return delayedActionInstance.Fail(GameStrings.ThingToFarAwayForDrag, ToTooltip());
		}
		if ((bool)interaction.SourceSlot.Occupant)
		{
			return delayedActionInstance.Fail(GameStrings.ThingCanNotBeDraggedWithSomethingIn, interaction.SourceSlot.ToTooltip());
		}
		if ((bool)base.Joint)
		{
			delayedActionInstance.Fail(GameStrings.ThingAlreadyDraggedBy, ToTooltip(), base.ParentSlot?.Parent.ToTooltip());
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		if (!GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(interaction.SourceThing, GrabDraggableThingHash, GrabSoundOffset);
		}
		Vector3 offset = (interactable.FakeCollider ? interactable.FakeCollider.transform.localPosition : interactable.Collider.transform.localPosition);
		DragInSlot(interaction.SourceSlot, offset);
		return delayedActionInstance.Succeed();
	}

	public override void Delete(Thing sourceItem)
	{
		if ((bool)(sourceItem as AuthoringTool))
		{
			base.Delete(sourceItem);
		}
		if (Slots.Count > 0)
		{
			foreach (Slot slot in Slots)
			{
				if ((bool)slot.Occupant && GameManager.RunSimulation)
				{
					OnServer.MoveToWorld(slot.Occupant);
				}
			}
		}
		if (GameManager.RunSimulation)
		{
			if (ThreadedManager.IsThread)
			{
				DestroyFromThread().Forget();
			}
			else
			{
				OnServer.Destroy(this);
			}
		}
		else
		{
			DestroyThingRequest destroyThingRequest = new DestroyThingRequest();
			destroyThingRequest.ThingId = base.netId;
			destroyThingRequest.SourceItemId = sourceItem.NetworkId;
			destroyThingRequest.SendToServer();
		}
	}

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		if (!destinationSlot.AllowDragging || destinationSlot.IsLocked)
		{
			return CanEnterResult.Fail(GameStrings.SlotDoesNotAllowDragging);
		}
		return base.CanEnter(destinationSlot);
	}
}
