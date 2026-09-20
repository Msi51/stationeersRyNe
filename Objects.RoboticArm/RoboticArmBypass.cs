using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmBypass : RoboticArmRailDeviceBase, IRoboticArmBypass, IRoboticArmJunction, IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	[Space(15f)]
	[Header("Robotic Arm Bypass")]
	[SerializeField]
	private List<RailNode> _railNodes;

	[SerializeField]
	private Transform _pivot;

	[SerializeField]
	private Transform _bypassPoint;

	[SerializeField]
	private Collider _infoScreenCollider;

	protected override bool IsOperable
	{
		get
		{
			if (base.RoboticArmNetwork != null && Powered)
			{
				return OnOff;
			}
			return false;
		}
	}

	public List<RailNode> RailNodes => _railNodes;

	public int JunctionIndex { get; set; }

	public Vector3 Pivot { get; private set; }

	public Vector3 BypassPosition { get; private set; }

	public SmallGrid AsSmallGrid => this;

	public bool CanOpen
	{
		get
		{
			RoboticArmDock dock;
			if (Powered && OnOff && !IsOpen)
			{
				return !base.RoboticArmNetwork.ArmIsStationaryAtIndex(RailNodes[0].Index, out dock);
			}
			return false;
		}
	}

	public bool CanClose
	{
		get
		{
			if (Powered && OnOff)
			{
				return IsOpen;
			}
			return false;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmBypassSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	private string GetInfoPanelTooltip()
	{
		Tooltip.ToolTipStringBuilder.Clear();
		Tooltip.ToolTipStringBuilder.AppendLine(IsOpen ? "Currently Open" : "Currently Closed");
		return Tooltip.ToolTipStringBuilder.ToString();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (!string.IsNullOrEmpty(passiveTooltip.Title))
		{
			return passiveTooltip;
		}
		if (hitCollider != null && hitCollider == _infoScreenCollider)
		{
			passiveTooltip.Title = DisplayName;
			passiveTooltip.Extended = GetInfoPanelTooltip();
		}
		else
		{
			int value = base.RoboticArmNetwork?.GetOffsetJunctionIndex(JunctionIndex) ?? 0;
			passiveTooltip.Title = GameStrings.RoboticArmJunction.AsString(StringManager.Get(value));
		}
		return passiveTooltip;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		BypassPosition = _bypassPoint.position;
		Pivot = _pivot.position;
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
		SetOpen(1);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Open)
		{
			int openState = (int)Mathf.Clamp((float)value, 0f, 1f);
			base.RoboticArmNetwork.ArmIsStationaryAtIndex(RailNodes[0].Index, out var dock);
			if ((bool)dock)
			{
				dock.TrySetOpenState(openState);
			}
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
	}

	public void SetOpen(int state)
	{
		OnServer.Interact(base.InteractOpen, state);
	}
}
