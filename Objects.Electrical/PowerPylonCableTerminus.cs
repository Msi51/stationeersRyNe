using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class PowerPylonCableTerminus : Cable, IPylonNodeOwner, IGridMergeable, ISmartRotatable
{
	[Header("Power Pylon Cable Terminus")]
	[SerializeField]
	private List<Transform> _nodeTransforms;

	private bool _needsPylonSync;

	public List<PylonNode> Nodes { get; set; } = new List<PylonNode>(1);

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

	CanConstructInfo IGridMergeable.CanReplace(MultiConstructor constructor, Item inactiveHandItem)
	{
		if (!(constructor is MultiMergeConstructor))
		{
			return CanConstructInfo.ValidPlacement;
		}
		return CanReplace(constructor, inactiveHandItem);
	}

	public override void Awake()
	{
		base.Awake();
		MaxVoltage = float.PositiveInfinity;
		BlockMergeWithOtherCables = true;
		IsStraight = false;
		StraightUnitLength = 0;
		CableType = Type.superHeavy;
		ConnectionType = SmartRotate.ConnectionType.Exhaustive;
		OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };
		for (byte b = 0; b < _nodeTransforms.Count; b++)
		{
			Nodes.Add(new PylonNode(b, this, _nodeTransforms[b], 1));
		}
		base.OnPowerNetworkChanged += MarkPylonSyncNeeded;
	}

	private void MarkPylonSyncNeeded()
	{
		_needsPylonSync = true;
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		if (!_needsPylonSync)
		{
			return;
		}
		_needsPylonSync = false;
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (base.CableNetwork == null)
		{
			PylonHelper.DebugLog($"Deferred pylon sync skipped: terminus #{base.ReferenceId} has null CableNetwork");
			return;
		}
		long referenceId = base.CableNetwork.ReferenceId;
		PylonHelper.DebugLog($"Deferred pylon sync: terminus #{base.ReferenceId} in network #{referenceId}");
		Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
		int count = 0;
		PylonHelper.AppendPylonCableNeighbors(this, buf, ref count);
		HashSet<long> hashSet = new HashSet<long> { referenceId };
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			Cable cable = buf[i].Get<Cable>();
			if ((object)cable != null)
			{
				CableNetwork cableNetwork = cable.CableNetwork;
				if (cableNetwork != null && hashSet.Add(cableNetwork.ReferenceId))
				{
					PylonHelper.DebugLog($"  cable #{cable.ReferenceId}: in #{cableNetwork.ReferenceId} — merging into #{referenceId}");
					base.CableNetwork.Merge(cableNetwork);
					num++;
				}
			}
		}
		PylonHelper.DebugLog($"Deferred pylon sync done: terminus #{base.ReferenceId}, {num} merges");
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

	public override void FillConnected<T>(Span<SmallCellRef> buf, ref int count)
	{
		int num = count;
		base.FillConnected<T>(buf, ref count);
		int num2 = count - num;
		if (typeof(T) != typeof(Cable))
		{
			PylonHelper.DebugLog($"FillConnected<non-Cable>: terminus #{base.ReferenceId} returned {num2} physical (T={typeof(T).Name})");
			return;
		}
		int num3 = count;
		PylonHelper.AppendPylonCableNeighbors(this, buf, ref count);
		int num4 = count - num3;
		PylonHelper.DebugLog($"FillConnected<Cable>: terminus #{base.ReferenceId} returned {num2} physical + {num4} pylon-reachable cables");
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PowerPylonCableTerminusSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
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
