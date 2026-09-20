using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class PowerPylon : SmallGrid, ISmartRotatable, IPylonNodeOwner
{
	[Header("Power Pylon")]
	[SerializeField]
	private List<Transform> _nodeTransforms;

	[Header("Smart Rotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	public List<PylonNode> Nodes { get; set; } = new List<PylonNode>(2);

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

	protected override float GetRenderMaxDistanceSquared()
	{
		return 10000f;
	}

	public override void Awake()
	{
		base.Awake();
		for (byte b = 0; b < _nodeTransforms.Count; b++)
		{
			Nodes.Add(new PylonNode(b, this, _nodeTransforms[b], 2));
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
		ThingSaveData savedData = new PowerPylonSaveData();
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

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
