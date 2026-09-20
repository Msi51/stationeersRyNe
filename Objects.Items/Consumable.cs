using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;

namespace Objects.Items;

public class Consumable : Item, IQuantity, ITradable, IEvaluable, IReferencable
{
	public float MaxQuantity = 1f;

	public float UseAmount = 0.1f;

	[Header("Consumable")]
	[SerializeField]
	private float quantity = 1f;

	public bool AllowSplitting = true;

	public bool DestroyAtZero = true;

	public float Quantity
	{
		get
		{
			return quantity;
		}
		set
		{
			quantity = Mathf.Clamp(value, 0f, MaxQuantity);
			if (base.ParentSlot != null && base.ParentSlot.Display != null)
			{
				base.ParentSlot.RefreshQuantity();
			}
			OnQuantityChanged();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
			if (GameManager.RunSimulation && quantity <= 0f && DestroyAtZero)
			{
				DestroyItemAtZero();
			}
		}
	}

	public bool IsEmpty => quantity <= float.Epsilon;

	public float GetMaxQuantity => MaxQuantity;

	public new virtual float GetTradableQuantity => 1f;

	public virtual float GetRatioQuantity => Quantity / MaxQuantity;

	public override float QuantityPerUse => UseAmount;

	public override float GetQuantity => Quantity;

	public float RemainingRatio => Quantity / MaxQuantity;

	public override bool HasRoom => Quantity < MaxQuantity;

	public bool IsStackFull => Quantity >= MaxQuantity;

	public float GetUseAmount()
	{
		return UseAmount;
	}

	public virtual void OnQuantityChanged()
	{
	}

	public override object GetModXmlType()
	{
		return new ConsumableModData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is ConsumableModData consumableModData)
		{
			if (!float.IsNaN(consumableModData.MaxQuantity))
			{
				MaxQuantity = consumableModData.MaxQuantity;
			}
			if (!float.IsNaN(consumableModData.UseAmount))
			{
				UseAmount = consumableModData.UseAmount;
			}
			if (!string.IsNullOrEmpty(consumableModData.AllowSplitting))
			{
				AllowSplitting = bool.Parse(consumableModData.AllowSplitting);
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteSingle(Quantity);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Quantity = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Quantity);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Quantity = reader.ReadSingle();
	}

	public override void SetQuantity(float value)
	{
		Quantity = value;
	}

	public override float CalculateUniqueRatioIdentifier()
	{
		return Quantity / MaxQuantity;
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (GameManager.RunSimulation && Quantity <= float.Epsilon)
		{
			DestroyItem();
			return;
		}
		reagentMixture.Add(CreatedReagentMixture);
		Quantity -= 1f;
		if (localAtmosphere.IsGlobalAtmosphere)
		{
			AtmosphericEventInstance.CloneGlobalRemoveEnergy(base.WorldGrid, IdealGas.Energy(CreatedReagentMixture.HeatCapacity, base.FlashPointTemperature));
		}
		else
		{
			AtmosphericEventInstance.CreateRemoveEnergy(localAtmosphere, IdealGas.Energy(CreatedReagentMixture.HeatCapacity, base.FlashPointTemperature));
		}
	}

	public override void Recycle()
	{
		if (GameManager.RunSimulation && Quantity <= 0f)
		{
			DestroyItem();
		}
		else
		{
			Quantity -= 1f;
		}
	}

	public override bool OnUseItem(float useQuantity, Thing useOnThing)
	{
		float num = useQuantity;
		useQuantity = Mathf.Min(useQuantity, Quantity);
		Quantity -= useQuantity;
		return base.OnUseItem(num, useOnThing);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Other);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ConsumableSaveData consumableSaveData)
		{
			consumableSaveData.Quantity = Quantity;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ConsumableSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is ConsumableSaveData consumableSaveData)
		{
			Quantity = consumableSaveData.Quantity;
		}
	}

	public void Combine(Consumable consumable)
	{
		float num = Mathf.Min(consumable.Quantity + Quantity, MaxQuantity) - Quantity;
		consumable.Quantity -= num;
		Quantity += num;
	}

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)(RemainingRatio * 100f), Unit.Percent);
	}
}
