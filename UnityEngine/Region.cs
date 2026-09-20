using System.Xml.Serialization;
using Assets.Scripts;
using Trading;

namespace UnityEngine;

public class Region : DataCollection, IEvaluable
{
	[XmlAttribute("R")]
	public int R;

	[XmlAttribute("G")]
	public int G;

	[XmlAttribute("B")]
	public int B;

	[XmlElement("Name")]
	public new LocalizedStringReference Name;

	public override void Initialize(ModAbout mod)
	{
		DataCollection.Register(this, mod);
	}
}
