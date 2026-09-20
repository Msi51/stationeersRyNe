using Assets.Scripts.Genetics;
using Assets.Scripts.Networking;
using UnityEngine.Networking;

public struct RequirementWrapper : IRocketReaderWriter
{
	public float BaseValue;

	public float MinValue;

	public float CurrentValue;

	public float MaxValue;

	public RequirementWrapper(PlantStat stat)
	{
		BaseValue = stat.Base;
		MinValue = stat.Min;
		MaxValue = stat.Max;
		CurrentValue = stat;
	}

	public RequirementWrapper(float baseValue, float minValue, float maxValue, float currentValue)
	{
		BaseValue = baseValue;
		MinValue = minValue;
		MaxValue = maxValue;
		CurrentValue = currentValue;
	}

	public static RequirementWrapper Create(RocketBinaryReader reader)
	{
		RequirementWrapper result = default(RequirementWrapper);
		result.Read(reader);
		return result;
	}

	public void Read(RocketBinaryReader reader)
	{
		BaseValue = reader.ReadSingle();
		MinValue = reader.ReadSingle();
		CurrentValue = reader.ReadSingle();
		MaxValue = reader.ReadSingle();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteSingle(BaseValue);
		writer.WriteSingle(MinValue);
		writer.WriteSingle(CurrentValue);
		writer.WriteSingle(MaxValue);
	}
}
