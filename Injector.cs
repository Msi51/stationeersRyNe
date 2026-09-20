using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Trading;

public class Injector : Item, ILogicable, IReferencable, IEvaluable
{
	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Activate && value >= 1.0)
		{
			Slot parentSlot = base.ParentSlot;
			if (parentSlot != null && parentSlot.Type == Slot.Class.AutoInjector && RootParent is Human useOnThing && OnUseItem(1f, useOnThing))
			{
				return;
			}
		}
		base.SetLogicValue(logicType, value);
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction);
		}
		if (!(RootParent as Human))
		{
			return new DelayedActionInstance
			{
				Duration = float.MaxValue,
				ActionMessage = ActionStrings.ConsumeFail
			};
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = ActionStrings.Consume
		};
		if (!doAction)
		{
			return result;
		}
		if (actionCompletedRatio >= 1f)
		{
			OnUseItem(1f, RootParent);
		}
		return result;
	}

	public override bool OnUseItem(float quantity, Thing useOnThing)
	{
		if (!GameManager.RunSimulation)
		{
			return true;
		}
		AudioEvent.Create(useOnThing, Defines.Sounds.InjectorUse);
		DestroyItem();
		return true;
	}
}
