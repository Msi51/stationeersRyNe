using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Trading;

public class GeneAction : ActionData
{
	private const string ID_ATTRIBUTE = "Id";

	[XmlAttribute("Id")]
	public Gene Gene;

	private const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public float Value;

	private const string X_ELEMENT_NAME = "Gene";

	public override string XElementName => "Gene";

	public override int GetChecksum()
	{
		return (int)(((uint)Gene ^ (uint)(int)(Value * 1000f)) * 41);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
		XDocumentHelper.SetAttribute(element, "Id", Gene.GetXmlEnumAttributeValueFromEnum());
		XDocumentHelper.SetAttribute(element, "Value", Value.ToString("F4"));
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Stackable stackable)
		{
			return stackable.SetGene(Gene, Value);
		}
		return false;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		return false;
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.Gene.DisplayString + " " + GeneHelper.DisplayName(Gene) + " " + StringManager.Get(Value));
	}
}
