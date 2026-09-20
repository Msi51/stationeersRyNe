using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class CreditCard : CharacterItem, IMergeable, IReferencable, IEvaluable, IQuantity, ITradable
{
	private float _currency;

	private bool _isStackFull;

	public const int MAX_CURRENCY = 100000;

	[ByteArraySync]
	public float Currency
	{
		get
		{
			return _currency;
		}
		set
		{
			if (!RocketMath.Approximately(value, _currency))
			{
				_currency = value;
				base.ParentSlot?.RefreshSlotDisplay();
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public float GetMaxQuantity => 100000f;

	public float GetRatioQuantity => Currency / 100000f;

	public override float GetQuantity => Currency;

	public bool IsStackFull => Currency >= 100000f;

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(Currency);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Currency = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Currency);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Currency = reader.ReadSingle();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), GetQuantityText()));
		return extendedText;
	}

	public override void SetQuantity(float value)
	{
		Currency = value;
	}

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)Currency, Unit.Credits);
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is ThingCreditCardSaveData thingCreditCardSaveData)
		{
			Currency = thingCreditCardSaveData.Currency;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		base.SerializeSave();
		ThingSaveData savedData = new ThingCreditCardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ThingCreditCardSaveData thingCreditCardSaveData)
		{
			thingCreditCardSaveData.Currency = Currency;
		}
	}

	public bool CanStack(IMergeable targetStack)
	{
		if (!(targetStack is CreditCard))
		{
			return false;
		}
		return true;
	}

	public void Merge(IMergeable mergeable)
	{
		if (mergeable is CreditCard creditCard)
		{
			float num = Mathf.Min(creditCard.Currency + Currency, 100000f) - Currency;
			creditCard.Currency -= num;
			Currency += num;
			if (creditCard.Currency == 0f && GameManager.RunSimulation)
			{
				OnServer.Destroy(creditCard);
			}
		}
	}
}
