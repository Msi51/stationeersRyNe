using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class MotionSensor : Sensor, IDoorControl
{
	public List<DynamicThing> TriggeredDynamicThings = new List<DynamicThing>();

	public override bool IsTriggered => TriggeredDynamicThings.Count > 0;

	public override void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard && circuitboard.ParentComputer.AsDevice().Powered)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		base.GridController.SetWatchGrid(base.WorldGrid, this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		base.GridController.RemoveWatchGrid(base.WorldGrid, this);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return TriggeredDynamicThings.Count;
		}
		return base.GetLogicValue(logicType);
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (Activate == 1)
		{
			int count = TriggeredDynamicThings.Count;
			while (count-- > 0)
			{
				DynamicThing dynamicThing = TriggeredDynamicThings[count];
				if (!dynamicThing)
				{
					TriggeredDynamicThings.RemoveAt(count);
				}
				else if (dynamicThing.WorldGrid != base.WorldGrid)
				{
					TriggeredDynamicThings.RemoveAt(count);
				}
			}
		}
		if (Activate == 1 != IsTriggered)
		{
			OnServer.Interact(base.InteractActivate, IsTriggered ? 1 : 0);
		}
	}

	public override void OnGridEvent(GridEvent gridEvent)
	{
		base.OnGridEvent(gridEvent);
		if (!GameManager.RunSimulation)
		{
			return;
		}
		switch (gridEvent.Type)
		{
		case GridEvent.GridEventType.Enter:
			if (TriggeredDynamicThings.Contains(gridEvent.DynamicThing))
			{
				return;
			}
			TriggeredDynamicThings.Add(gridEvent.DynamicThing);
			break;
		case GridEvent.GridEventType.Leave:
			TriggeredDynamicThings.Remove(gridEvent.DynamicThing);
			break;
		}
		ActivateSensor();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if (!BaseAnimator && !(MaterialChanger == null))
		{
			MaterialChanger.ChangeState((Activate == 1) ? Defines.Animator.On : Defines.Animator.Off);
		}
	}
}
