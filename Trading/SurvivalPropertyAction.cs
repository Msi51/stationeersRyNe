using System;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;

namespace Trading;

public class SurvivalPropertyAction : ActionData
{
	[XmlAttribute("Type")]
	public EntitySurvivalProperty SurvivalProperty;

	[XmlAttribute("Percent")]
	public float PercentValue = float.NaN;

	private const string X_ELEMENT_NAME = "SurvivalProperty";

	public override string XElementName => "SurvivalProperty";

	public override int GetChecksum()
	{
		return (((int)SurvivalProperty * 41) ^ PercentValue.GetHashCode()) * 41;
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Entity entity)
		{
			float num = PercentValue / 100f;
			if (SurvivalProperty != EntitySurvivalProperty.None)
			{
				switch (SurvivalProperty)
				{
				case EntitySurvivalProperty.Nutrition:
					entity.Nutrition = num * entity.BaseNutritionStorage;
					break;
				case EntitySurvivalProperty.Hydration:
					entity.Hydration = num * 5f;
					break;
				case EntitySurvivalProperty.Mood:
					entity.Mood = num * 1f;
					break;
				case EntitySurvivalProperty.Hygiene:
					entity.Hygiene = num;
					break;
				case EntitySurvivalProperty.FoodQuality:
					entity.FoodQuality = num;
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
				return true;
			}
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
		stringBuilder.Append(EnumCollections.EntitySurvivalProperty.GetName(SurvivalProperty));
		stringBuilder.Append(" ");
		stringBuilder.Append(PercentValue.ToStringPercent());
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
		XDocumentHelper.SetAttribute(element, "Type", SurvivalProperty.GetXmlEnumAttributeValueFromEnum());
		XDocumentHelper.SetAttribute(element, "Percent", PercentValue.ToString(CultureInfo.InvariantCulture));
	}
}
