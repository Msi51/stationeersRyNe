using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PassthroughHeatExchanger : HeatExchangerBase
{
	[SerializeField]
	private Collider _infoPanel;

	private const float HEAT_EXCHANGE_AREA = 10f;

	private MoleQuantity _line1PassedMoles;

	private MoleQuantity _line2PassedMoles;

	private MoleEnergy _energyTransfer;

	private const int HEAT_EXCHANGE_STEPS = 4;

	private const int NUMBER_OF_PACKETS = 8;

	private static MoleEnergy _energyTransferAggregate;

	private static readonly GasMixture[] Input1Packets = new GasMixture[8];

	private static readonly GasMixture[] Input2Packets = new GasMixture[8];

	private const float STEP_AREA = 5f / 32f;

	private static int _windowEndInput1Pointer;

	private static int _windowStartInput2Pointer;

	private MoleQuantity Line1PassedMoles
	{
		get
		{
			return _line1PassedMoles;
		}
		set
		{
			if (!RocketMath.Approximately(value, _line1PassedMoles))
			{
				_line1PassedMoles = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	private MoleQuantity Line2PassedMoles
	{
		get
		{
			return _line2PassedMoles;
		}
		set
		{
			if (!RocketMath.Approximately(value, _line2PassedMoles))
			{
				_line2PassedMoles = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	private MoleEnergy EnergyTransfer
	{
		get
		{
			return _energyTransfer;
		}
		set
		{
			if (!RocketMath.Approximately(value, _energyTransfer))
			{
				_energyTransfer = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.IsInputValid && base.IsInput2Valid && base.IsOutputValid)
			{
				return base.IsOutput2Valid;
			}
			return false;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == null)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider != _infoPanel)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		result.Title = DisplayName;
		result.Extended = GetExtendedText().ToString();
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.HeatExchangerEnergyTransfer.AsString(AtmosAnalyser.GetEnergyUnitString(EnergyTransfer.ToFloat())));
		extendedText.AppendLine(GameStrings.PassedMolesInput1.AsString(StringManager.Get(Line1PassedMoles.ToFloat())));
		extendedText.AppendLine(GameStrings.PassedMolesInput2.AsString(StringManager.Get(Line2PassedMoles.ToFloat())));
		return extendedText;
	}

	public override void OnPreAtmosphere()
	{
		base.OnPreAtmosphere();
		if (!IsOperable)
		{
			Line1PassedMoles = MoleQuantity.Zero;
			Line2PassedMoles = MoleQuantity.Zero;
		}
		else
		{
			HeatExchange();
		}
	}

	private void HeatExchange()
	{
		GasMixture gasMixture = AtmosphereHelper.RemoveToEqualise(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, PressurekPa.MaxValue);
		GasMixture gasMixture2 = AtmosphereHelper.RemoveToEqualise(InputNetwork2.Atmosphere, OutputNetwork2.Atmosphere, PressurekPa.MaxValue);
		Line1PassedMoles = gasMixture.GetTotalMolesGassesAndLiquids;
		Line2PassedMoles = gasMixture2.GetTotalMolesGassesAndLiquids;
		if (gasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES * 8.0 || gasMixture2.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES * 8.0)
		{
			OutputNetwork.Atmosphere.Add(gasMixture);
			OutputNetwork2.Atmosphere.Add(gasMixture2);
			EnergyTransfer = MoleEnergy.Zero;
			return;
		}
		GasMixture value = new GasMixture(gasMixture);
		value.Scale(0.125);
		Array.Fill(Input1Packets, value);
		GasMixture value2 = new GasMixture(gasMixture2);
		value2.Scale(0.125);
		Array.Fill(Input2Packets, value2);
		float efficiencyRatio = HeatExchangerBase.HeatExchangeRatio(InputNetwork.Atmosphere, InputNetwork2.Atmosphere);
		_windowEndInput1Pointer = -1;
		_windowStartInput2Pointer = 8;
		_energyTransferAggregate = MoleEnergy.Zero;
		for (int i = 0; i < 11; i++)
		{
			_windowEndInput1Pointer++;
			TransferHeatBetweenPacketPairs(efficiencyRatio);
			_windowStartInput2Pointer--;
			TransferHeatBetweenPacketPairs(efficiencyRatio);
		}
		EnergyTransfer = _energyTransferAggregate;
		GasMixture gasMixture3 = AggregateMixes(Input1Packets);
		GasMixture gasMixture4 = AggregateMixes(Input2Packets);
		MoleQuantity getTotalMolesGassesAndLiquids = gasMixture.GetTotalMolesGassesAndLiquids;
		MoleQuantity getTotalMolesGassesAndLiquids2 = gasMixture3.GetTotalMolesGassesAndLiquids;
		MoleQuantity getTotalMolesGassesAndLiquids3 = gasMixture2.GetTotalMolesGassesAndLiquids;
		MoleQuantity getTotalMolesGassesAndLiquids4 = gasMixture4.GetTotalMolesGassesAndLiquids;
		MoleEnergy totalEnergy = gasMixture.TotalEnergy;
		MoleEnergy totalEnergy2 = gasMixture2.TotalEnergy;
		MoleEnergy totalEnergy3 = gasMixture3.TotalEnergy;
		MoleEnergy totalEnergy4 = gasMixture4.TotalEnergy;
		if (!RocketMath.Approximately(getTotalMolesGassesAndLiquids, getTotalMolesGassesAndLiquids2, 1.401298464324817E-45))
		{
			double num = (getTotalMolesGassesAndLiquids / getTotalMolesGassesAndLiquids2).ToDouble();
			if (num > 1.0 || num < 1.0)
			{
				gasMixture3.Scale((float)num);
			}
		}
		if (!RocketMath.Approximately(getTotalMolesGassesAndLiquids3, getTotalMolesGassesAndLiquids4, 1.401298464324817E-45))
		{
			double num2 = (getTotalMolesGassesAndLiquids3 / getTotalMolesGassesAndLiquids4).ToDouble();
			if (num2 > 1.0 || num2 < 1.0)
			{
				gasMixture4.Scale((float)num2);
			}
		}
		if (!RocketMath.Approximately(totalEnergy + totalEnergy2, totalEnergy3 + totalEnergy4, 0.009999999776482582))
		{
			double num3 = ((totalEnergy + totalEnergy2) / (totalEnergy3 + totalEnergy4)).ToDouble();
			if (num3 > 1.0 || num3 < 1.0)
			{
				gasMixture3.TotalEnergy *= num3;
				gasMixture4.TotalEnergy *= num3;
			}
		}
		OutputNetwork.Atmosphere.Add(gasMixture3);
		OutputNetwork2.Atmosphere.Add(gasMixture4);
	}

	private static void TransferHeatBetweenPacketPairs(float efficiencyRatio)
	{
		for (int i = 0; i < 4; i++)
		{
			int num = _windowEndInput1Pointer - 3 + i;
			int num2 = _windowStartInput2Pointer + i;
			if (num >= 0 && num < 8 && num2 >= 0 && num2 < 8)
			{
				TransferHeat(ref Input1Packets[num], ref Input2Packets[num2], 5f / 32f, efficiencyRatio);
			}
		}
	}

	private static void TransferHeat(ref GasMixture gasMix1, ref GasMixture gasMix2, float area, float efficiencyRatio)
	{
		double num = 100.0 * (double)area * (double)efficiencyRatio * (gasMix1.Temperature - gasMix2.Temperature).ToDouble();
		MoleEnergy moleEnergy = new MoleEnergy(num * (double)AtmosphericsManager.Instance.TickSpeedSeconds);
		_energyTransferAggregate += moleEnergy;
		gasMix1.TransferEnergyTo(ref gasMix2, moleEnergy);
	}

	private GasMixture AggregateMixes(GasMixture[] mixArray)
	{
		GasMixture result = GasMixtureHelper.Create();
		foreach (GasMixture newGasMix in mixArray)
		{
			result.Add(newGasMix);
		}
		return result;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(EnergyTransfer.ToFloat());
			writer.WriteSingle(Line1PassedMoles.ToFloat());
			writer.WriteSingle(Line2PassedMoles.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			EnergyTransfer = new MoleEnergy(reader.ReadSingle());
			Line1PassedMoles = new MoleQuantity(reader.ReadSingle());
			Line2PassedMoles = new MoleQuantity(reader.ReadSingle());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(EnergyTransfer.ToFloat());
		writer.WriteSingle(Line1PassedMoles.ToFloat());
		writer.WriteSingle(Line2PassedMoles.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyTransfer = new MoleEnergy(reader.ReadSingle());
		Line1PassedMoles = new MoleQuantity(reader.ReadSingle());
		Line2PassedMoles = new MoleQuantity(reader.ReadSingle());
	}
}
