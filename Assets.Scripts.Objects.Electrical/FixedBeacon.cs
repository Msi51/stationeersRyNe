using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Structures;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class FixedBeacon : Diode, ITrackable
{
	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (GameManager.GameState != GameState.None)
		{
			ITrackable.Trackables.Add(this);
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if (GameManager.GameState != GameState.None)
		{
			ITrackable.Trackables.Remove(this);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PositionX => true, 
			LogicType.PositionY => true, 
			LogicType.PositionZ => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PositionX => Mathf.RoundToInt(base.Position.x), 
			LogicType.PositionY => Mathf.RoundToInt(base.Position.y), 
			LogicType.PositionZ => Mathf.RoundToInt(base.Position.z), 
			_ => base.GetLogicValue(logicType), 
		};
	}
}
