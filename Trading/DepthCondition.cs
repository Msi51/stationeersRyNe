using System.Xml.Serialization;
using Assets.Scripts.Objects;
using TerrainSystem;

namespace Trading;

public class DepthCondition : ConditionComparable
{
	private const string DEPTH_NAME = "Depth";

	[XmlAttribute("Depth")]
	public float Depth;

	public override string DebugName => "Depth";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)(Depth * 1000f)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is VeinCluster veinCluster)
		{
			flag = Compare(veinCluster.CenterPosition.y, Depth);
		}
		else if (t is Thing thing)
		{
			flag = Compare(thing.Position.y, Depth);
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}
}
