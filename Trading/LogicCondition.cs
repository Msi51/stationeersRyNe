using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class LogicCondition : ConditionComparable
{
	[XmlAttribute("Type")]
	public LogicType LogicType;

	[XmlAttribute("Value")]
	public double Value;

	public override string DebugName => $"{LogicType} {Value}";

	public override int GetChecksum()
	{
		return (int)(((((uint)base.GetChecksum() ^ (uint)LogicType) * 41) ^ (uint)(int)(Value * 1000.0)) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Thing thing && thing is ILogicable logicable && logicable.CanLogicRead(LogicType))
		{
			double logicValue = logicable.GetLogicValue(LogicType);
			flag = Compare((float)logicValue, (float)Value);
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(LogicType.GetName()).Append(" ").Append(StringManager.Get(Value));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
