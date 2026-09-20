using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Rockets.Mining;

public class DepositMaterialGas
{
	public float Weight;

	public float Temperature;

	public Thing MixPrefab;

	public List<SpawnGas> SpawnGasses = new List<SpawnGas>();

	private DepositMaterialGasData _data;

	public void AppendToString(ref StringBuilder sb, bool asAtmosphere)
	{
		if (sb == null)
		{
			return;
		}
		if (asAtmosphere)
		{
			AtmosphericsManager.DisplayBasicAtmosphere(_data.GetTemperature(), _data.GetPressure(), sb, Pipe.ContentType.All, indent: false, includeCelsius: false);
			sb.AppendLine();
		}
		foreach (SpawnGas spawnGass in SpawnGasses)
		{
			sb.AppendLine(spawnGass.ToString());
		}
	}

	public GasMixture AsGasMixture()
	{
		GasMixture result = GasMixtureHelper.Create();
		foreach (SpawnGas spawnGass in SpawnGasses)
		{
			result.Add(new Mole(spawnGass.Type, spawnGass.GetQuantity(), MoleEnergy.Zero));
		}
		if (result.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero)
		{
			result.TotalEnergy = IdealGas.Energy(result.HeatCapacity, new TemperatureKelvin(Temperature));
		}
		return result;
	}

	public DepositMaterialGas()
	{
	}

	public DepositMaterialGas(DepositMaterialGasData data)
	{
		Weight = data.Weight;
		Temperature = data.Temperature.ToFloat();
		MixPrefab = Prefab.Find("ItemSpaceIce");
		_data = data;
		data.Populate(SpawnGasses);
	}

	public DepositMaterialGas(RocketBinaryReader reader)
	{
		float weight = reader.ReadSingle();
		float temperature = reader.ReadSingle();
		int prefabHash = reader.ReadInt32();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			SpawnGasses.Add(new SpawnGas(reader));
		}
		Thing mixPrefab = Prefab.Find(prefabHash);
		MixPrefab = mixPrefab;
		Weight = weight;
		Temperature = temperature;
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteSingle(Weight);
		writer.WriteSingle(Temperature);
		writer.WriteInt32(MixPrefab.PrefabHash);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (SpawnGas spawnGass in SpawnGasses)
		{
			spawnGass.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public void AddToTree(TreeString parentString)
	{
		TreeString myParent = TreeString.Node(MixPrefab.DisplayName, parentString);
		for (int i = 0; i < SpawnGasses.Count; i++)
		{
			SpawnGas spawnGas = SpawnGasses[i];
			TreeString.Node($"{spawnGas.Name} x {spawnGas.Quantity}", myParent);
		}
	}
}
