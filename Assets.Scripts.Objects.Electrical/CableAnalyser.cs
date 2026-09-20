using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class CableAnalyser : DeviceCableMounted, IRocketInternals, IRocketComponent
{
	public Collider InfoCollider;

	public float CurrentLoad
	{
		get
		{
			if (base.CableNetwork == null)
			{
				return 0f;
			}
			return base.CableNetwork.CurrentLoad;
		}
	}

	public float RequiredLoad
	{
		get
		{
			if (base.CableNetwork == null)
			{
				return 0f;
			}
			return base.CableNetwork.RequiredLoad;
		}
	}

	public float PotentialLoad
	{
		get
		{
			if (base.CableNetwork == null)
			{
				return 0f;
			}
			return base.CableNetwork.PotentialLoad;
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider != InfoCollider)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		if (base.CableNetwork == null)
		{
			Tooltip.ToolTipStringBuilder.AppendColorText("red", GameStrings.CableAnalyserNoCableNetworkFound);
		}
		else
		{
			Tooltip.ToolTipStringBuilder.Append(GameStrings.CableAnalyserActual.AsString(CurrentLoad.ToStringPrefix("W", "yellow")));
			Tooltip.ToolTipStringBuilder.AppendLine();
			Tooltip.ToolTipStringBuilder.Append(GameStrings.CableAnalyserRequired.AsString(RequiredLoad.ToStringPrefix("W", "yellow")));
			Tooltip.ToolTipStringBuilder.AppendLine();
			Tooltip.ToolTipStringBuilder.Append(GameStrings.CableAnalyserPotential.AsString(PotentialLoad.ToStringPrefix("W", "yellow")));
		}
		result.Title = DisplayName;
		result.Extended = Tooltip.ToolTipStringBuilder.ToString();
		return result;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 25 <= LogicType.Power || logicType == LogicType.PowerRequired)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PowerRequired => RequiredLoad, 
			LogicType.PowerActual => CurrentLoad, 
			LogicType.PowerPotential => PotentialLoad, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
