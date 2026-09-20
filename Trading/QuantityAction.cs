using System;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;

namespace Trading;

public class QuantityAction : ActionData
{
	private const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public float Value;

	private const string X_ELEMENT_NAME = "Quantity";

	public override string XElementName => "Quantity";

	public float GetSafeValue(IQuantity iQuantity)
	{
		return Mathf.Min(Value, iQuantity.GetMaxQuantity);
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Thing thing && thing is IQuantity quantity)
		{
			quantity.SetQuantity(GetSafeValue(quantity));
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
		if (prefab is Stackable stackable)
		{
			stringBuilder.AppendLine(GameStrings.ItemInSlotStack.AsString(stackable.ToTooltip(), StringManager.Get(Value)));
			return;
		}
		if (prefab is Consumable consumable)
		{
			stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(consumable.ToTooltip(), StringManager.Get(Value)));
			return;
		}
		stringBuilder.Append(GameStrings.Quantity);
		stringBuilder.Append(" ");
		stringBuilder.AppendLine(StringManager.Get(Value).AsColor("yellow"));
	}

	public override int GetChecksum()
	{
		return (int)(Value * 1000f);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XDocumentHelper.SetAttribute(XDocumentHelper.MakeElement(actionElementName, ref parentElement), "Value", Value.ToString(CultureInfo.InvariantCulture));
	}
}
