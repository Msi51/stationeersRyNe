using System;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;

namespace Trading;

public class SourceCodeAction : ActionData
{
	private const string TEXT_ELEMENT = "Text";

	[XmlElement("Text")]
	public string Text = string.Empty;

	public int Size;

	public string SizeText = "0 bytes";

	public int TextChecksum = 1;

	private const string X_ELEMENT_NAME = "SourceCode";

	public override string XElementName => "SourceCode";

	public override void Initialize()
	{
		base.Initialize();
		Size = Text.Length;
		SizeText = StringGenerator.GetString(Size, Unit.ProgrammableChip);
		TextChecksum = Text.GetHashCode();
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (!(t is ISourceCode sourceCode))
		{
			return false;
		}
		sourceCode.SetSourceCode(Text);
		return true;
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
		stringBuilder.Append(GameStrings.TradeActionData.AsString(SizeText.AsColor("yellow")));
	}

	public override int GetChecksum()
	{
		return TextChecksum;
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XDocumentHelper.MakeElement(actionElementName, ref parentElement).SetValue(Text);
	}
}
