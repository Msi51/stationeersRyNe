using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Objects.Rockets;
using Objects.Rockets.Log.RocketEvents;
using Objects.Rockets.Scanning;
using TerrainSystem;
using UnityEngine;

public class DeliveryPayload : RocketPayload, ITrackable
{
	[SerializeField]
	private GameObject openObject;

	private static readonly int DeploySound = Animator.StringToHash("PayloadDeploy");

	private static readonly int DeploySoundTail = Animator.StringToHash("PayloadDeployTail");

	public new const float RENDER_DISTANCE = 100f;

	private const float DEPLOY_HEIGHT = 200f;

	public override float LavaDamage => 0f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override RocketActionResult CanDeploy()
	{
		NodeType? nodeType = (base.ParentSlot?.Parent as RocketPayloadBay)?.RocketNetwork?.Rocket?.CurrentNode?.NodeType;
		if (!nodeType.HasValue || nodeType != NodeType.Entry)
		{
			return RocketActionResult.Failure(GameStrings.RocketDeployFailNotInLowOrbit, this);
		}
		return RocketActionResult.Success;
	}

	public override void OnDeploy()
	{
		RocketPayloadBay rocketPayloadBay = base.ParentSlot?.Parent as RocketPayloadBay;
		if ((bool)CanDeploy() && rocketPayloadBay?.RocketNetwork?.Rocket != null)
		{
			double logicValue = rocketPayloadBay.GetLogicValue(LogicType.PositionX);
			double logicValue2 = rocketPayloadBay.GetLogicValue(LogicType.PositionZ);
			Vector3 vector = SpawnPoint.GetSafePoint(new Vector3((float)logicValue, 0f, (float)logicValue2)) + Vector3.up * 200f;
			rocketPayloadBay.RocketNetwork.Rocket.Report(new RocketDeployEvent(rocketPayloadBay.RocketNetwork.Rocket, this, (float)logicValue, (float)logicValue2));
			OnServer.MoveToWorld(this, vector, Quaternion.Euler(180f, 0f, 0f));
			AudioEvent.Schedule(this, DeploySound).Forget();
			AudioEvent.Schedule(this, DeploySoundTail).Forget();
			OnServer.Interact(base.InteractActivate, 1);
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ThingTransformLocalRotation = Quaternion.Euler(newChild.ChildSlotOffset);
			newChild.ScaleToSlot(0.9f);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.ThingTransform.localScale = Vector3.one;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open && (bool)openObject)
		{
			openObject.SetActive(!IsOpen);
		}
		if (interactable.Action == InteractableType.Activate && interactable.State == 1 && !ITrackable.Trackables.Contains(this))
		{
			ITrackable.Trackables.Add(this);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (base.InteractActivate.State == 1 && !ITrackable.Trackables.Contains(this))
		{
			ITrackable.Trackables.Add(this);
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if (GameManager.GameState != GameState.None)
		{
			ITrackable.Trackables.Remove(this);
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if ((bool)base.Joint)
		{
			return base.AttackWith(attack, doAction);
		}
		if (attack.SourceItem is IWelder)
		{
			DelayedActionInstance result = new DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = ((base.ParentSlot != null) ? GameStrings.ActionWeld : GameStrings.ActionUnweld)
			};
			if (!doAction || !GameManager.RunSimulation)
			{
				return result;
			}
			OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
			return result;
		}
		return base.AttackWith(attack, doAction);
	}

	public override bool ShouldResetPosition()
	{
		if (base.Position == Vector3.zero || base.ParentSlot != null)
		{
			return false;
		}
		if (!(base.Position.y < 0f))
		{
			return VoxelTerrain.GetDensityWorldSpace(CenterPosition) > 0.99f;
		}
		return true;
	}
}
