using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Rockets;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class RocketDataUpLink : RocketDataLink, IReceiveDataNetworkDevices, ILogicable, IReferencable, IEvaluable
{
	private ITransmitDataNetworkDevices _connectedDataNetTransmitter;

	public BoxCollider InfoBox;

	private long _connectedDeviceId;

	public ITransmitDataNetworkDevices ConnectedDataNetTransmitter
	{
		get
		{
			return _connectedDataNetTransmitter;
		}
		set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
			_connectedDataNetTransmitter?.RemoveReceiver(this);
			_connectedDataNetTransmitter = value;
			_connectedDataNetTransmitter?.AddReceiver(this);
			base.DataCableNetwork?.DirtyDataDeviceList();
			base.DataCableNetwork?.RequestDeviceRefresh(this);
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted || IsBroken)
			{
				return false;
			}
			bool flag = ConnectedDataNetTransmitter != null;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	private StringBuilder GetInfoBoxString()
	{
		StringBuilder extendedText = GetExtendedText();
		if (OnOff && !Powered)
		{
			extendedText.AppendLine(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
			return extendedText;
		}
		if (!IsOperable)
		{
			return extendedText;
		}
		extendedText.AppendLine((ConnectedDataNetTransmitter != null) ? GameStrings.UplinkConnectedTo.AsString(ConnectedDataNetTransmitter.ToTooltip()) : GameStrings.UplinkNotConnected.AsColor("red"));
		if (ConnectedDataNetTransmitter?.DataCableNetwork?.DataDeviceList != null)
		{
			foreach (Device dataDevice in ConnectedDataNetTransmitter.DataCableNetwork.DataDeviceList)
			{
				if (dataDevice is RocketAvionicsDevice rocketAvionicsDevice)
				{
					extendedText.AppendLine(GameStrings.UplinkConnectedToRocket.AsString(rocketAvionicsDevice.Rocket?.ToTooltip() ?? "Unknown?", rocketAvionicsDevice.ToTooltip()));
				}
			}
		}
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoBox)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			result.Extended = GetInfoBoxString().ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			return (ConnectedDataNetTransmitter is Device device) ? device.DisplayName : InterfaceStrings.LogicNoDevice;
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action != InteractableType.Button1 && action != InteractableType.Button2)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!(interaction.SourceSlot.Occupant is Screwdriver))
		{
			return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
		}
		if (interactable.Action == InteractableType.Button1)
		{
			ITransmitDataNetworkDevices nextValidReadable = Logicable.GetNextValidReadable(this, ConnectedDataNetTransmitter, RocketDataDownLink.AllITransmitDataNetworkDevices, interaction.AltKey);
			if (nextValidReadable == null)
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextValidReadable.ToTooltip());
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				AudioEvent.Create(this, Defines.Sounds.ScrewdriverSound);
			}
			if (GameManager.RunSimulation)
			{
				ConnectedDataNetTransmitter = nextValidReadable;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public bool GetIsOperable()
	{
		return IsOperable;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (!HasFrameBelow())
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		base.DataCableNetwork?.DirtyDataDeviceList();
		base.DataCableNetwork?.RequestDeviceRefresh(this);
	}

	public bool DataConnectionActive()
	{
		if (Powered && OnOff && IsOperable)
		{
			return ConnectedDataNetTransmitter?.DataCableNetwork != null;
		}
		return false;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(ConnectedDataNetTransmitter?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			ConnectedDataNetTransmitter = Thing.Find<ITransmitDataNetworkDevices>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(ConnectedDataNetTransmitter?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_connectedDeviceId = reader.ReadInt64();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketDataLinkSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketDataLinkSaveData rocketDataLinkSaveData)
		{
			_connectedDeviceId = rocketDataLinkSaveData.ConnectedId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketDataLinkSaveData rocketDataLinkSaveData)
		{
			rocketDataLinkSaveData.ConnectedId = ConnectedDataNetTransmitter?.ReferenceId ?? 0;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		ConnectedDataNetTransmitter = Thing.Find<ITransmitDataNetworkDevices>(_connectedDeviceId);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		DisconnectReceiver();
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		DisconnectReceiver();
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (newState < previousState)
		{
			DisconnectReceiver();
		}
	}

	private void DisconnectReceiver()
	{
		if (ConnectedDataNetTransmitter != null)
		{
			ConnectedDataNetTransmitter.RemoveReceiver(this);
			ConnectedDataNetTransmitter = null;
		}
	}
}
