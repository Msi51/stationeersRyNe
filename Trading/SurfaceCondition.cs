using System.Xml.Serialization;
using TerrainSystem;

namespace Trading;

public class SurfaceCondition : ConditionData
{
	[XmlAttribute("Value")]
	public bool Value = true;

	private string Name
	{
		get
		{
			if (!Value)
			{
				return "Is not on surface";
			}
			return "Is On Surface";
		}
	}

	public override string DebugName => Name;

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Value.GetHashCode()) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is VeinCluster veinCluster)
		{
			sbyte depth = VoxelTerrain.GetDepth(32);
			flag = (VoxelTerrain.GetReadonlyNodeTypeWorldSpace(veinCluster.WorldPosition, depth) & VoxelNodeType.Crust) != 0 == Value;
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}
}
