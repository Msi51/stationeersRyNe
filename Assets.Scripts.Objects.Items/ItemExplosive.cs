using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Items;

public class ItemExplosive : Stackable, IExplosive, IGenerateMinables
{
	[SerializeField]
	private MaterialChanger armedMaterialChanger;

	[SerializeField]
	private GameObject armedText;

	[SerializeField]
	private GenericAssignableAnimComponent activateLever;

	private ItemRemoteDetonator _linkedDevice;

	public new const float RENDER_DISTANCE = 30f;

	public new const float SHADOW_DISTANCE = 6f;

	private long _savedLinkedId;

	public float ExplosionRadius = 5.3f;

	public float ExplosionForce = 2000f;

	public bool CanRemoteDetonator;

	public float ExplosiveCountdownTime = 10f;

	private CancellationTokenWrapper countDownCancellation = new CancellationTokenWrapper();

	private const float EXPLOSION_CHAIN_DELAY = 0.03f;

	public const float MINING_CHARGE_MAX_DAMAGE = 2000f;

	public ItemRemoteDetonator LinkedDevice
	{
		get
		{
			return _linkedDevice;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
			_linkedDevice = value;
			RefreshAnimState();
		}
	}

	public Vector3Int MinablesGenerationRange => GameConstants.MINABLES_GENERATION_RANGE_EXPLOSIVE;

	public Vector3 PreviousMinableRequestPosition { get; set; }

	public bool ShouldGenerate => base.ParentSlot == null;

	public Vector3 GeneratePosition => base.Position;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(30f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(6f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (activateLever != null)
		{
			activateLever.RefreshState(skipAnimation);
		}
		if (armedMaterialChanger != null)
		{
			armedMaterialChanger.ChangeState((LinkedDevice != null) ? Defines.Animator.Armed : Defines.Animator.Disarmed);
		}
		if (armedText != null)
		{
			armedText.SetActive(LinkedDevice != null);
		}
	}

	public void Link(ItemRemoteDetonator detonator)
	{
		Unlink();
		LinkedDevice = detonator;
		detonator.AddExplosive(this);
	}

	public void Unlink()
	{
		if (LinkedDevice != null)
		{
			LinkedDevice.RemoveExplosive(this);
		}
		LinkedDevice = null;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt64(LinkedDevice?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			LinkedDevice = Thing.Find<ItemRemoteDetonator>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(LinkedDevice?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_savedLinkedId = reader.ReadInt64();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		LinkedDevice = Thing.Find<ItemRemoteDetonator>(_savedLinkedId);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!CanRemoteDetonator)
		{
			return base.AttackWith(attack, doAction);
		}
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		ItemRemoteDetonator itemRemoteDetonator = sourceItem as ItemRemoteDetonator;
		if ((bool)itemRemoteDetonator)
		{
			DelayedActionInstance delayedActionInstance = null;
			if (!LinkedDevice)
			{
				delayedActionInstance = new DelayedActionInstance
				{
					Duration = 0.4f,
					ActionMessage = ActionStrings.Link
				};
				if (!itemRemoteDetonator.OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!itemRemoteDetonator.IsOperable)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!doAction)
				{
					return delayedActionInstance;
				}
				Link(itemRemoteDetonator);
			}
			else
			{
				if (LinkedDevice != itemRemoteDetonator)
				{
					delayedActionInstance = new DelayedActionInstance
					{
						ActionMessage = ActionStrings.Error,
						ExtendedMessage = GameStrings.UnableToLinkExplosive
					};
					if (!itemRemoteDetonator.OnOff)
					{
						return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
					}
					if (!itemRemoteDetonator.IsOperable)
					{
						return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
					}
					return delayedActionInstance;
				}
				delayedActionInstance = new DelayedActionInstance
				{
					Duration = 0.4f,
					ActionMessage = ActionStrings.UnLink
				};
				if (!itemRemoteDetonator.OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!itemRemoteDetonator.IsOperable)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!doAction)
				{
					return delayedActionInstance;
				}
				Unlink();
			}
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return (Activate == 1) ? GameStrings.DisarmExplosive : GameStrings.ArmExplosive;
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Activate || CanRemoteDetonator)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			ActionMessage = interactable.ContextualName
		};
		if (!doAction)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.ActivateExplosive, StringManager.Get(ExplosiveCountdownTime));
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractActivate, (Activate != 1) ? 1 : 0);
		}
		return delayedActionInstance.Succeed();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate)
		{
			if (Activate == 1)
			{
				TriggerExplosionCountdown(ExplosiveCountdownTime);
			}
			else
			{
				CancelCountdown();
			}
		}
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		string actionMessage = base.InteractActivate.ContextualName;
		ItemRemoteDetonator itemRemoteDetonator = null;
		if (CanRemoteDetonator)
		{
			actionMessage = GameStrings.DropExplosive;
			if (RootParent is Human human)
			{
				if (human.LeftHandSlot.Contains<ItemRemoteDetonator>())
				{
					itemRemoteDetonator = human.LeftHandSlot.Get<ItemRemoteDetonator>();
				}
				if (human.RightHandSlot.Contains<ItemRemoteDetonator>())
				{
					itemRemoteDetonator = human.RightHandSlot.Get<ItemRemoteDetonator>();
				}
			}
			if (itemRemoteDetonator != null && itemRemoteDetonator.IsOperable && itemRemoteDetonator.OnOff && itemRemoteDetonator.Powered)
			{
				actionMessage = GameStrings.LinkAndDropExplosive;
			}
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = actionMessage
		};
		if (!doAction || actionCompletedRatio < 1f)
		{
			return result;
		}
		ItemExplosive itemExplosive = this;
		if (base.Quantity > 1)
		{
			itemExplosive = OnServer.Create<ItemExplosive>(base.SourcePrefab, base.Position, Rotation);
			OnServer.MoveToWorld(itemExplosive, 0.5f);
			DecrementQuantity();
		}
		else
		{
			OnServer.MoveToWorld(itemExplosive, 0.5f);
		}
		if (!CanRemoteDetonator)
		{
			OnServer.Interact(itemExplosive.InteractActivate, 1);
		}
		else if (itemRemoteDetonator != null && itemRemoteDetonator.IsOperable && itemRemoteDetonator.OnOff && itemRemoteDetonator.Powered)
		{
			itemExplosive.Link(itemRemoteDetonator);
		}
		return result;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public void TriggerExplosionCountdown(float delay)
	{
		countDownCancellation.CancelAndInitialize();
		ExplosionCountdown(delay, countDownCancellation.Token).Forget();
	}

	private async UniTaskVoid ExplosionCountdown(float delay, CancellationToken token)
	{
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Register(this);
			float countDownTimer = delay;
			while (countDownTimer > 0f && GameManager.GameState != GameState.None)
			{
				await UniTask.NextFrame(token);
				countDownTimer -= Time.deltaTime;
			}
			if (!base.BeingDestroyed && !token.IsCancellationRequested)
			{
				global::Explosion.Explode(ExplosionForce, base.transform.position, ExplosionRadius, 2000f, mineTerrain: true);
				CancelCountdown();
				OnServer.Destroy(this);
			}
		}
	}

	public override void Explosion(Vector3 position, float force = 0f)
	{
		TriggerExplosionCountdown(0.03f);
	}

	public void CancelCountdown()
	{
		Unlink();
		countDownCancellation.Cancel();
	}

	public override void OnDestroy()
	{
		AtmosphericsManager.Instance.Deregister(this);
		if (countDownCancellation.Initialized)
		{
			CancelCountdown();
			if (GameManager.GameState == GameState.Running)
			{
				global::Explosion.Explode(ExplosionForce, base.transform.position, ExplosionRadius, 2000f, mineTerrain: true);
			}
		}
		base.OnDestroy();
	}

	protected override void OnMergeStack(Stackable oldStack, float delta)
	{
		base.OnMergeStack(oldStack, delta);
		if (oldStack is ItemExplosive itemExplosive)
		{
			if (oldStack.Quantity > 0)
			{
				OnServer.Interact(oldStack.InteractActivate, 0);
			}
			else
			{
				itemExplosive.CancelCountdown();
			}
		}
		OnServer.Interact(base.InteractActivate, 0);
	}
}
