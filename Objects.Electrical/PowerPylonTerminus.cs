using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public abstract class PowerPylonTerminus : ElectricalInputOutput, IPylonNodeOwner
{
	[Header("Power Pylon Node")]
	public float PowerMaximum = 10000f;

	[SerializeField]
	private List<Transform> _nodeTransforms;

	private float _powerStored;

	private float _lastPowerStored;

	private int _lastPowerTickRemoved = -1;

	private float _lastPowerRemoved;

	private int _lastPowerTickAdded = -1;

	private float _lastPowerAdded;

	public List<PylonNode> Nodes { get; set; } = new List<PylonNode>(1);

	public bool CanTransfer => true;

	public float PowerStored
	{
		get
		{
			return _powerStored;
		}
		set
		{
			if (!float.IsNaN(value))
			{
				_powerStored = Mathf.Clamp(value, 0f, PowerMaximum);
			}
		}
	}

	public override float AvailablePower => PowerStored;

	public float LastPowerRemoved
	{
		get
		{
			if (GameManager.RunSimulation && ElectricityManager.Instance.TotalTickCount > _lastPowerTickRemoved + 1)
			{
				_lastPowerRemoved = 0f;
			}
			return _lastPowerRemoved;
		}
		set
		{
			if (_lastPowerTickRemoved != ElectricityManager.Instance.TotalTickCount)
			{
				_lastPowerTickRemoved = ElectricityManager.Instance.TotalTickCount;
				_lastPowerRemoved = value / 1000f;
			}
			else
			{
				_lastPowerRemoved += value / 1000f;
			}
		}
	}

	protected float LastPowerAdded
	{
		get
		{
			if (GameManager.RunSimulation && ElectricityManager.Instance.TotalTickCount > _lastPowerTickAdded + 1)
			{
				_lastPowerAdded = 0f;
			}
			return _lastPowerAdded;
		}
		set
		{
			if (_lastPowerTickAdded != ElectricityManager.Instance.TotalTickCount)
			{
				_lastPowerTickAdded = ElectricityManager.Instance.TotalTickCount;
				_lastPowerAdded = value / 1000f;
			}
			else
			{
				_lastPowerAdded += value / 1000f;
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	private bool IsDestroyedByDamage
	{
		get
		{
			if (DamageState != null)
			{
				return DamageState.Total >= DamageState.MaxDamage;
			}
			return false;
		}
	}

	public override void Awake()
	{
		base.Awake();
		for (byte b = 0; b < _nodeTransforms.Count; b++)
		{
			Nodes.Add(new PylonNode(b, this, _nodeTransforms[b], 1));
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		PylonHelper.AllPylonNodeOwners.Add(this);
	}

	public override void AttackWithCompleteLocal(Attack attack)
	{
		PylonHelper.AttackWithCompleteLocal(this, attack);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!base.IsStructureCompleted)
		{
			return base.AttackWith(attack, doAction);
		}
		return PylonHelper.AttackWith(this, attack, doAction) ?? base.AttackWith(attack, doAction);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(LastPowerAdded);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			LastPowerAdded = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(LastPowerAdded);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LastPowerAdded = reader.ReadSingle();
	}

	protected void CheckError()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (!IsOperable)
		{
			if (Error == 0)
			{
				LastPowerAdded = 0f;
				LastPowerRemoved = 0f;
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
		}
		else if (Error == 1)
		{
			OnServer.Interact(base.InteractError, 0, skipAnimation: true);
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		if (newState < previousState)
		{
			DisconnectAllNodes(!IsDestroyedByDamage);
		}
		base.OnBuildStateUpdated(newState, previousState);
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			DisconnectAllNodes(refundCables: false);
			base.OnDestroy();
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		PylonHelper.AllPylonNodeOwners.Remove(this);
	}

	private void DisconnectAllNodes(bool refundCables)
	{
		foreach (PylonNode node in Nodes)
		{
			PylonHelper.DisconnectAll(node, refundCables ? new Vector3?(base.ThingTransformPosition) : ((Vector3?)null));
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PowerPylonTerminusSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is PowerPylonTerminusSaveData powerPylonTerminusSaveData)
		{
			PowerStored = powerPylonTerminusSaveData.PowerStored;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is PowerPylonTerminusSaveData powerPylonTerminusSaveData)
		{
			if (float.IsNaN(PowerStored))
			{
				PowerStored = 0f;
			}
			powerPylonTerminusSaveData.PowerStored = PowerStored;
		}
	}

	public long GetRefId()
	{
		return base.ReferenceId;
	}

	public new Structure AsStructure()
	{
		return this;
	}
}
