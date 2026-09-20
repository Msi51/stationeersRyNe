using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmJunction : RoboticArmRailBase, IRoboticArmJunction, IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	[Space(15f)]
	[Header("Robotic Arm Junction")]
	[SerializeField]
	private List<RailNode> _railNodes;

	[SerializeField]
	private Transform _pivot;

	public List<RailNode> RailNodes => _railNodes;

	public int JunctionIndex { get; set; }

	public Vector3 Pivot => _pivot.position;

	public SmallGrid AsSmallGrid => this;

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmJunctionSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		_ = thingSaveData is RoboticArmJunctionSaveData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		_ = thingSaveData is RoboticArmJunctionSaveData;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		int value = base.RoboticArmNetwork?.GetOffsetJunctionIndex(JunctionIndex) ?? 0;
		string text = GameStrings.RoboticArmJunction.AsString(StringManager.Get(value));
		if (string.IsNullOrEmpty(passiveTooltip.Title))
		{
			passiveTooltip.Title = text;
		}
		else
		{
			Tooltip.ToolTipStringBuilder.Clear();
			Tooltip.ToolTipStringBuilder.AppendLine(text);
			Tooltip.ToolTipStringBuilder.Append(passiveTooltip.Extended);
			passiveTooltip.Extended = Tooltip.ToolTipStringBuilder.ToString();
		}
		return passiveTooltip;
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
