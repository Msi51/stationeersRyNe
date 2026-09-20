using System.Xml.Serialization;
using Assets.Scripts.Objects;
using TerrainSystem;
using UnityEngine;

namespace Trading;

public class RegionCondition : ConditionData
{
	[XmlAttribute("Id")]
	public string Id;

	[XmlIgnore]
	public int IdHash;

	public override string DebugName => "Region_" + Id;

	public override void Initialise()
	{
		base.Initialise();
		IdHash = Animator.StringToHash(Id);
	}

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Id.GetHashCode()) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is VeinCluster veinCluster)
		{
			if (RegionManager.EvaluateRegionsAtPosition(veinCluster.CenterPosition, IdHash))
			{
				flag = true;
			}
		}
		else if (t is Thing thing)
		{
			if (RegionManager.EvaluateRegionsAtPosition(thing.RootParent.Position, IdHash))
			{
				flag = true;
			}
		}
		else if (t is EvaluablePosition evaluablePosition && RegionManager.EvaluateRegionsAtPosition(evaluablePosition.Value, IdHash))
		{
			flag = true;
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}
}
