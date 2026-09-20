using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmRail : RoboticArmRailBase, IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	[Space(15f)]
	[Header("Robotic Arm Rail")]
	[SerializeField]
	private List<RailNode> _railNodes;

	[SerializeField]
	private Transform _pivot;

	public List<RailNode> RailNodes => _railNodes;

	public Vector3 Pivot => _pivot.position;

	public SmallGrid AsSmallGrid => this;

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmRailSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		_ = thingSaveData is RoboticArmRailSaveData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		_ = thingSaveData is RoboticArmRailSaveData;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		foreach (RailNode railNode in _railNodes)
		{
			railNode.Init();
		}
	}

	public Connection OtherEnd(Connection end)
	{
		if (end != OpenEnds[0])
		{
			return OpenEnds[0];
		}
		return OpenEnds[1];
	}

	public void RailNetworkUpdated()
	{
	}
}
