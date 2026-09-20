using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Trading;

namespace Assets.Scripts;

public class StructureSpawnData : ThingSpawnData
{
	public StructureSpawnData()
	{
	}

	public StructureSpawnData(Structure structure)
		: base(structure)
	{
		SpawnPositionData = new SpawnPositionData
		{
			SpawnPositionRule = SpawnPositionRule.Explicit,
			Offset = new Vector3Reference(structure.Position),
			Rotation = new Vector3Reference(structure.Rotation.eulerAngles.Round())
		};
		if (structure.CurrentBuildStateIndex > 0)
		{
			Actions.Add(new BuildStateAction
			{
				Index = structure.CurrentBuildStateIndex
			});
		}
	}
}
