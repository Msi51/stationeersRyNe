using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ItemRemoteDetonator : PowerTool
{
	private const float EXPLOSION_DELAY = 0.15f;

	public float energyPerExplosive = 1000f;

	public int colourIndex = -1;

	public bool emissive;

	private readonly List<ItemExplosive> _linkedExplosives = new List<ItemExplosive>();

	[SerializeField]
	private MaterialChanger armedMaterialChanger;

	[SerializeField]
	private GameObject armedMesh;

	[SerializeField]
	private AssignableBinaryAnimComponent coverAnim;

	private static string formatString = "{1} {0}";

	private InteractableType _lastInteractableType;

	private string _lastInteractableString;

	private List<long> _savedIds = new List<long>();

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (ItemExplosive linkedExplosive in _linkedExplosives)
		{
			Network.WritePackedId(writer, linkedExplosive.ReferenceId);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_linkedExplosives.Clear();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			ItemExplosive itemExplosive = Thing.Find<ItemExplosive>(referenceId);
			if (itemExplosive != null)
			{
				_linkedExplosives.Add(itemExplosive);
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (!Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			return;
		}
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (ItemExplosive linkedExplosive in _linkedExplosives)
		{
			Network.WritePackedId(writer, linkedExplosive.ReferenceId);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (!Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			return;
		}
		_linkedExplosives.Clear();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			ItemExplosive itemExplosive = Thing.Find<ItemExplosive>(referenceId);
			if (itemExplosive != null)
			{
				_linkedExplosives.Add(itemExplosive);
			}
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (base.BeingDestroyed)
		{
			return;
		}
		if (coverAnim != null)
		{
			coverAnim.RefreshState(skipAnimation);
		}
		if (armedMesh != null)
		{
			armedMesh.SetActive(OnOff && Powered && Mode == 1);
		}
		if (OnOff && Powered)
		{
			if (Mode == 1)
			{
				armedMaterialChanger.ChangeState(Defines.Animator.Armed);
			}
			else
			{
				armedMaterialChanger.ChangeState(Defines.Animator.OnPowered);
			}
		}
		else
		{
			armedMaterialChanger.ChangeState(Defines.Animator.Off);
		}
	}

	public void RefreshMode()
	{
		int num = ((_linkedExplosives.Count > 0) ? 1 : 0);
		if (num != Mode)
		{
			OnServer.Interact(base.InteractMode, num);
		}
	}

	public void AddExplosive(ItemExplosive explosive)
	{
		if (!_linkedExplosives.Contains(explosive))
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
			PlaySound(Defines.Sounds.Label);
			_linkedExplosives.Add(explosive);
			if (!GameManager.RunSimulation)
			{
				NetworkMessages.ExplosiveLinkMessage explosiveLinkMessage = new NetworkMessages.ExplosiveLinkMessage();
				explosiveLinkMessage.ToLink = true;
				explosiveLinkMessage.ExplosiveId = explosive.netId;
				explosiveLinkMessage.DetonatorId = explosive.LinkedDevice.netId;
				explosiveLinkMessage.SendToServer();
			}
			RefreshMode();
			if (!IsLocked)
			{
				OnServer.Interact(base.InteractLock, 1);
			}
		}
	}

	public void RemoveExplosive(ItemExplosive explosive)
	{
		if (_linkedExplosives.Contains(explosive))
		{
			_linkedExplosives.Remove(explosive);
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
			PlaySound(Defines.Sounds.LabelCancel);
			if (!GameManager.RunSimulation)
			{
				NetworkMessages.ExplosiveLinkMessage explosiveLinkMessage = new NetworkMessages.ExplosiveLinkMessage();
				explosiveLinkMessage.ToLink = false;
				explosiveLinkMessage.ExplosiveId = explosive.netId;
				explosiveLinkMessage.DetonatorId = explosive.LinkedDevice.netId;
				explosiveLinkMessage.SendToServer();
			}
			RefreshMode();
		}
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		if (Activate == 0 && !IsLocked && Powered && OnOff && !(CursorManager.CursorThing is ItemExplosive))
		{
			Thing.Interact(base.InteractActivate, 1);
		}
	}

	public override bool PreventInteraction(out DelayedActionInstance failResult, Interactable interactable, Interaction interaction)
	{
		failResult = null;
		if (IsLocked && interactable.Action == InteractableType.OnOff)
		{
			return false;
		}
		return base.PreventInteraction(out failResult, interactable, interaction);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (Activate == 0)
			{
				OnServer.Interact(interactable, 1);
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
			if (!IsLocked)
			{
				TriggerExplosives();
				OnServer.Interact(base.InteractLock, 1);
				OnServer.Interact(base.InteractMode, 0);
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == _lastInteractableType)
		{
			return _lastInteractableString;
		}
		_lastInteractableType = interactable.Action;
		switch (interactable.Action)
		{
		case InteractableType.Lock:
			_lastInteractableString = (IsLocked ? ActionStrings.Unlock : ActionStrings.Lock);
			return _lastInteractableString;
		case InteractableType.Activate:
			_lastInteractableString = GameStrings.DetonateExplosives;
			return _lastInteractableString;
		default:
			_lastInteractableString = base.GetContextualName(interactable);
			return _lastInteractableString;
		}
	}

	public void TriggerExplosives()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		int count = _linkedExplosives.Count;
		while (count-- > 0)
		{
			ItemExplosive itemExplosive = _linkedExplosives[count];
			if (itemExplosive != null)
			{
				itemExplosive.TriggerExplosionCountdown(0.15f * (float)(count + 1));
			}
			base.Battery.PowerStored -= energyPerExplosive;
		}
		_linkedExplosives.Clear();
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			base.NetworkUpdateFlags |= 256;
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		for (int i = 0; i < _linkedExplosives.Count; i++)
		{
			if (_linkedExplosives[i] != null)
			{
				OnServer.SetCustomColor(_linkedExplosives[i], index);
			}
		}
		colourIndex = index;
		this.emissive = emissive;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		RemoteDetonatorSaveData remoteDetonatorSaveData = savedData as RemoteDetonatorSaveData;
		if (GameManager.GameState != GameState.None && remoteDetonatorSaveData != null)
		{
			_linkedExplosives.RemoveAll((ItemExplosive item) => item == null);
			remoteDetonatorSaveData.LinkedExplosives = new long[_linkedExplosives.Count];
			for (int num = 0; num < _linkedExplosives.Count; num++)
			{
				remoteDetonatorSaveData.LinkedExplosives[num] = _linkedExplosives[num].ReferenceId;
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RemoteDetonatorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RemoteDetonatorSaveData { LinkedExplosives: not null } remoteDetonatorSaveData)
		{
			_savedIds.Clear();
			long[] linkedExplosives = remoteDetonatorSaveData.LinkedExplosives;
			foreach (long item in linkedExplosives)
			{
				_savedIds.Add(item);
			}
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		foreach (long savedId in _savedIds)
		{
			ItemExplosive itemExplosive = Thing.Find<ItemExplosive>(savedId);
			if ((object)itemExplosive != null)
			{
				_linkedExplosives.Add(itemExplosive);
			}
		}
		RefreshMode();
		RefreshAnimState();
	}
}
