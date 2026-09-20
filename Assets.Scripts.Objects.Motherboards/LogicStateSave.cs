using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Motherboards;

[XmlRoot]
public class LogicStateSave
{
	[XmlElement]
	public string DisplayName;

	[XmlArray("Conditions")]
	[XmlArrayItem("Condition")]
	public List<LogicConditionSave> Conditions = new List<LogicConditionSave>();

	[XmlArray("Actions")]
	[XmlArrayItem("Action")]
	public List<LogicActionSave> Actions = new List<LogicActionSave>();

	[XmlElement]
	public bool IsTriggered;

	[XmlElement]
	public int NextLogicIndex;

	[XmlElement]
	public int FalseLogicIndex;

	public LogicStateSave()
	{
	}

	public LogicStateSave(LogicState logicState)
	{
		NextLogicIndex = logicState.NextState.Index;
		FalseLogicIndex = logicState.FalseState.Index;
		DisplayName = logicState.DisplayName;
		IsTriggered = logicState.IsTriggered;
		foreach (LogicCondition condition in logicState.Conditions)
		{
			Conditions.Add(new LogicConditionSave(condition));
		}
		foreach (LogicAction action in logicState.Actions)
		{
			Actions.Add(new LogicActionSave(action));
		}
	}
}
