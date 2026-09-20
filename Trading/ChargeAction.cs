using System;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Trading;

public class ChargeAction : ActionData
{
	private const string STATE_ATTRIBUTE = "State";

	[XmlAttribute("State")]
	public BatteryCellState State = BatteryCellState.Full;

	private const string X_ELEMENT_NAME = "Charge";

	public override string XElementName => "Charge";

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Thing thing && thing is IChargable chargable)
		{
			IChargable.SetPower(chargable, State);
			return true;
		}
		return false;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		throw new NotImplementedException();
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.Append(GameStrings.TradeActionCharge.AsString(State.GetName().AsColor("yellow")));
	}

	public override int GetChecksum()
	{
		return (int)State * 1000;
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XDocumentHelper.SetAttribute(XDocumentHelper.MakeElement(actionElementName, ref parentElement), "State", State.GetXmlEnumAttributeValueFromEnum());
	}
}
