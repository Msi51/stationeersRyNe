using System;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Trading;

public class LogicValueAction : ActionData
{
	private const string TYPE_ATTRIBUTE = "Type";

	[XmlAttribute("Type")]
	public LogicType LogicType;

	private const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public double Value;

	private const string X_ELEMENT_NAME = "Logic";

	public override string XElementName => "Logic";

	public LogicValueAction()
	{
	}

	public LogicValueAction(LogicType logicType, double logicValue)
	{
		LogicType = logicType;
		Value = logicValue;
	}

	public override int GetChecksum()
	{
		return (((int)LogicType * 41) ^ (int)(Value * 1000.0)) * 41;
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
		XDocumentHelper.SetAttribute(element, "Type", LogicType.ToString());
		XDocumentHelper.SetAttribute(element, "Value", Value.ToString(CultureInfo.InvariantCulture));
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Thing thing && thing is ILogicable logicable)
		{
			if (logicable.CanLogicWrite(LogicType))
			{
				logicable.SetLogicValue(LogicType, Value);
			}
			return true;
		}
		return false;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		throw new NotImplementedException();
	}
}
