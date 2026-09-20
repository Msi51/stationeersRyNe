using System;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Objects.Items;

namespace Trading;

public class PercentAction : ActionData
{
	private const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public float Value;

	private const string X_ELEMENT_NAME = "Percent";

	public override string XElementName => "Percent";

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Consumable consumable)
		{
			consumable.SetQuantity(Value * consumable.MaxQuantity);
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
		stringBuilder.Append(GameStrings.Quantity);
		stringBuilder.Append(" ");
		stringBuilder.AppendLine(Value.ToStringPercent("yellow"));
	}

	public override int GetChecksum()
	{
		return (int)(Value * 1000f);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XDocumentHelper.SetAttribute(XDocumentHelper.MakeElement(actionElementName, ref parentElement), "Value", Value.ToString("F4"));
	}
}
