using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Objects.Structures;

public class StructurePoweredShower : StructureShower
{
	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsStructureCompleted && OnOff && Powered && Error == 0 && base.IsInputValid && base.IsOutputValid && !base.WaterTooCold && !base.WaterTooHot && !base.WaterPolluted && HasEnoughWater(StructureShower.MolesUsedPerTick);
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	protected override DelayedActionInstance HandleOpen(DelayedActionInstance result, Interaction interaction, bool doAction)
	{
		if (!Powered || !OnOff)
		{
			result.AppendStateMessage(GameStrings.DeviceOffOrUnpowered.AsString(DisplayName));
		}
		return base.HandleOpen(result, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}
}
