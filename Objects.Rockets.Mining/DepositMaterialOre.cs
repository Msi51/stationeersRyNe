using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace Objects.Rockets.Mining;

public class DepositMaterialOre
{
	public float Weight;

	public Thing OrePrefab;

	public DepositMaterialOre(DepositMaterialOreData data)
	{
		Weight = data.Weight;
		OrePrefab = Prefab.Find(data.PrefabName);
	}

	public DepositMaterialOre(RocketBinaryReader reader)
	{
		Weight = reader.ReadSingle();
		int prefabHash = reader.ReadInt32();
		OrePrefab = Prefab.Find(prefabHash);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteSingle(Weight);
		writer.WriteInt32(OrePrefab.PrefabHash);
	}
}
