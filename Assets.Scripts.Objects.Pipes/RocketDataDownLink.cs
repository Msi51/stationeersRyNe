using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Networks;
using Objects.Rockets;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class RocketDataDownLink : RocketDataLink, ITransmitDataNetworkDevices, ILogicable, IReferencable, IEvaluable, IRocketInternals, IRocketComponent
{
	public static readonly List<ILogicable> AllITransmitDataNetworkDevices = new List<ILogicable>();

	public BoxCollider InfoBox;

	private List<long> _connectedDeviceIds = new List<long>();

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public override bool OnOff => true;

	public RocketNetwork RocketNetwork { get; set; }

	public List<IReceiveDataNetworkDevices> ConnectedDataNetReceivers { get; set; } = new List<IReceiveDataNetworkDevices>();

	public bool AtLeastOneReceiver
	{
		get
		{
			List<IReceiveDataNetworkDevices> connectedDataNetReceivers = ConnectedDataNetReceivers;
			if (connectedDataNetReceivers != null)
			{
				return connectedDataNetReceivers.Count > 0;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool atLeastOneReceiver = AtLeastOneReceiver;
			if (Error == 1)
			{
				if (!atLeastOneReceiver)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (atLeastOneReceiver)
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

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetInfoBoxString().ToString());
	}

	private StringBuilder GetInfoBoxString()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IsOperable)
		{
			if (base.DataCableNetwork != null)
			{
				foreach (Device dataDevice in base.DataCableNetwork.DataDeviceList)
				{
					if (!(dataDevice is RocketDataDownLink rocketDataDownLink))
					{
						continue;
					}
					if (!rocketDataDownLink.AtLeastOneReceiver)
					{
						extendedText.Append(GameStrings.DownlinkConnectedToNothing);
					}
					else
					{
						extendedText.Append(GameStrings.DownlinkConnectedTo);
						for (int i = 0; i < rocketDataDownLink.ConnectedDataNetReceivers.Count; i++)
						{
							IReceiveDataNetworkDevices receiveDataNetworkDevices = rocketDataDownLink.ConnectedDataNetReceivers[i];
							extendedText.Append(" " + receiveDataNetworkDevices.ToTooltip());
							if (i < rocketDataDownLink.ConnectedDataNetReceivers.Count - 1)
							{
								extendedText.Append(", ");
							}
						}
					}
					extendedText.AppendLine();
				}
			}
		}
		else if (OnOff && !Powered)
		{
			extendedText.AppendLine(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
		}
		return extendedText;
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public void RemoveReceiver(IReceiveDataNetworkDevices receiver)
	{
		if (ConnectedDataNetReceivers.Contains(receiver))
		{
			ConnectedDataNetReceivers.Remove(receiver);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public void AddReceiver(IReceiveDataNetworkDevices receiver)
	{
		if (!ConnectedDataNetReceivers.Contains(receiver))
		{
			ConnectedDataNetReceivers.Add(receiver);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public bool DataConnectionActive()
	{
		if (!AtLeastOneReceiver)
		{
			return false;
		}
		if (!base.IsStructureCompleted)
		{
			return false;
		}
		foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in ConnectedDataNetReceivers)
		{
			if (connectedDataNetReceiver.DataConnectionActive())
			{
				return true;
			}
		}
		return false;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		for (int num = ConnectedDataNetReceivers.Count - 1; num >= 0; num--)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices = ConnectedDataNetReceivers[num];
			receiveDataNetworkDevices?.DataCableNetwork?.DirtyDataDeviceList();
			receiveDataNetworkDevices?.DataCableNetwork?.RequestDeviceRefresh(this);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!AtLeastOneReceiver)
		{
			extendedText.AppendLine(GameStrings.NoDataLinkActive);
		}
		return extendedText;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (!Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			return;
		}
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in ConnectedDataNetReceivers)
		{
			Network.WritePackedId(writer, connectedDataNetReceiver?.ReferenceId ?? 0);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (!Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			return;
		}
		ConnectedDataNetReceivers.Clear();
		Network.ReadIndex<ushort>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			IReceiveDataNetworkDevices receiveDataNetworkDevices = Thing.Find<IReceiveDataNetworkDevices>(referenceId);
			if (receiveDataNetworkDevices != null)
			{
				ConnectedDataNetReceivers.Add(receiveDataNetworkDevices);
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		foreach (long connectedDeviceId in _connectedDeviceIds)
		{
			Network.WritePackedId(writer, connectedDeviceId);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_connectedDeviceIds.Clear();
		Network.ReadIndex<ushort>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			_connectedDeviceIds.Add(referenceId);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketDataDownLinkSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is RocketDataDownLinkSaveData rocketDataDownLinkSaveData))
		{
			return;
		}
		_connectedDeviceIds = new List<long>();
		foreach (long connectedId in rocketDataDownLinkSaveData.ConnectedIds)
		{
			_connectedDeviceIds.Add(connectedId);
		}
	}

	public override void ValidateOnLoad(int currentSaveVersion)
	{
		base.ValidateOnLoad(currentSaveVersion);
		if (currentSaveVersion <= 25553)
		{
			base.CurrentBuildStateIndex = BuildStates.Count - 1;
			UpdateStateVisualizer();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is RocketDataDownLinkSaveData rocketDataDownLinkSaveData))
		{
			return;
		}
		rocketDataDownLinkSaveData.ConnectedIds = new List<long>();
		foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in ConnectedDataNetReceivers)
		{
			rocketDataDownLinkSaveData.ConnectedIds.Add(connectedDataNetReceiver?.ReferenceId ?? 0);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		ConnectedDataNetReceivers = new List<IReceiveDataNetworkDevices>();
		foreach (long connectedDeviceId in _connectedDeviceIds)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices = Thing.Find<IReceiveDataNetworkDevices>(connectedDeviceId);
			if (receiveDataNetworkDevices != null)
			{
				ConnectedDataNetReceivers.Add(receiveDataNetworkDevices);
			}
		}
	}

	public override void OnNetworkChange()
	{
		base.OnNetworkChange();
		for (int num = ConnectedDataNetReceivers.Count - 1; num >= 0; num--)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices = ConnectedDataNetReceivers[num];
			receiveDataNetworkDevices?.DataCableNetwork?.DirtyDataDeviceList();
			receiveDataNetworkDevices?.DataCableNetwork?.RequestDeviceRefresh(this);
		}
	}

	private void DisconnectReceiver()
	{
		AllITransmitDataNetworkDevices.Remove(this);
		for (int num = ConnectedDataNetReceivers.Count - 1; num >= 0; num--)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices = ConnectedDataNetReceivers[num];
			if (receiveDataNetworkDevices != null)
			{
				if (receiveDataNetworkDevices.ConnectedDataNetTransmitter is RocketDataDownLink == (bool)this)
				{
					receiveDataNetworkDevices.ConnectedDataNetTransmitter = null;
				}
				receiveDataNetworkDevices.DataCableNetwork?.DirtyDataDeviceList();
				receiveDataNetworkDevices.DataCableNetwork?.RequestDeviceRefresh(this);
			}
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (!base.IsStructureCompleted)
		{
			DisconnectReceiver();
		}
		else if (!AllITransmitDataNetworkDevices.Contains(this))
		{
			AllITransmitDataNetworkDevices.Add(this);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (base.IsStructureCompleted)
		{
			AllITransmitDataNetworkDevices.Add(this);
		}
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
}
