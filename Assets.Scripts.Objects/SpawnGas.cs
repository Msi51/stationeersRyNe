using System;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using UnityEngine.Networking;

namespace Assets.Scripts.Objects;

[Serializable]
[XmlRoot]
public class SpawnGas : MessageBase<SpawnGas>
{
	public Chemistry.GasType Type;

	public float Quantity;

	public float Kelvin = 293.15f;

	public string Name => EnumCollections.GasTypes.GetName(Type);

	public bool IsValid
	{
		get
		{
			if (Type != Chemistry.GasType.Undefined)
			{
				return Quantity > 0f;
			}
			return false;
		}
	}

	public SpawnGas(RocketBinaryReader reader)
	{
		Type = (Chemistry.GasType)reader.ReadUInt32();
		Quantity = reader.ReadSingle();
		Kelvin = reader.ReadSingle();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteUInt32((uint)Type);
		writer.WriteSingle(Quantity);
		writer.WriteSingle(Kelvin);
	}

	public SpawnGas()
	{
	}

	public SpawnGas(SpawnGas copy)
	{
		Quantity = copy.Quantity;
		Type = copy.Type;
		Kelvin = copy.Kelvin;
	}

	public SpawnGas(Chemistry.GasType type, float quantity, float kelvin = 293.15f)
	{
		Type = type;
		Quantity = quantity;
		Kelvin = kelvin;
	}

	public SpawnGas(Chemistry.GasType type, float quantity, TemperatureKelvin kelvin)
	{
		Type = type;
		Quantity = quantity;
		Kelvin = kelvin.ToFloat();
	}

	public override string ToString()
	{
		return $"<color=yellow>{Quantity} mol</color> x <link=Gas{Type}><color=#44AD83>{Name}</color></link>";
	}

	public StationFoundInInsert GetSpecificSpawnGasDat(Chemistry.GasType type, string prefabName)
	{
		return new StationFoundInInsert
		{
			NameOfThing = Localization.ParseHelpText("{THING:" + prefabName + "}"),
			QuantityOfThing = Quantity.ToString()
		};
	}

	public StationFoundInInsert ToStationpediaInsert()
	{
		return new StationFoundInInsert
		{
			NameOfThing = Name,
			QuantityOfThing = Quantity.ToString()
		};
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Type = (Chemistry.GasType)reader.ReadInt32();
		Quantity = reader.ReadSingle();
		Kelvin = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteUInt32((uint)Type);
		writer.WriteSingle(Quantity);
		writer.WriteSingle(Kelvin);
	}

	public MoleEnergy GetEnergy()
	{
		return IdealGas.Energy(new TemperatureKelvin(Kelvin), Mole.SpecificHeat(Type), new MoleQuantity(Quantity));
	}

	public MoleQuantity GetQuantity()
	{
		return new MoleQuantity(Quantity);
	}
}
