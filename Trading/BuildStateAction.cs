using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;

namespace Trading;

public class BuildStateAction : ActionData
{
	private const string INDEX_ATTRIBUTE = "Index";

	[XmlAttribute("Index")]
	public int Index;

	private const string X_ELEMENT_NAME = "BuildState";

	public override string XElementName => "BuildState";

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Structure structure)
		{
			structure.UpdateBuildStateAndVisualizer(Index);
			return true;
		}
		return false;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		throw new NotImplementedException();
	}

	public override int GetChecksum()
	{
		return Index * 1000 * 41;
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XDocumentHelper.SetAttribute(XDocumentHelper.MakeElement(actionElementName, ref parentElement), "Index", Index.ToString());
	}
}
