using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Pipes;

public class HeatSink
{
	private MoleEnergy _energy;

	private readonly HeatCapacity _heatCapacity;

	private readonly float _heatExchangeArea;

	public HeatCapacity HeatCapacity => _heatCapacity;

	public MoleEnergy Energy => _energy;

	public TemperatureKelvin Temperature => IdealGas.Temperature(_energy, _heatCapacity);

	public HeatSink(HeatCapacity heatCapacity, TemperatureKelvin temperature, float heatExchangeArea)
	{
		_heatCapacity = heatCapacity;
		_energy = IdealGas.Energy(heatCapacity, temperature);
		_heatExchangeArea = heatExchangeArea;
	}

	public MoleEnergy HeatExchange(Atmosphere other, float ratio = 1f)
	{
		float num = other.HeatExchangeRatio();
		double value = 100.0 * (double)_heatExchangeArea * (other.Temperature - Temperature).ToDouble() * (double)GameManager.GameTickSpeedSeconds * (double)ratio * (double)num;
		MoleEnergy signedEnergyToTransfer = new MoleEnergy(value);
		return other.GasMixture.TransferEnergyTo(this, signedEnergyToTransfer);
	}

	public MoleEnergy TransferEnergyTo(ref GasMixture targetGasMix, MoleEnergy signedEnergyToTransfer)
	{
		if (!targetGasMix.IsValid)
		{
			return MoleEnergy.Zero;
		}
		HeatCapacity heatCapacity = targetGasMix.HeatCapacity + HeatCapacity;
		MoleEnergy moleEnergy = targetGasMix.TotalEnergy + Energy;
		double num = moleEnergy.ToDouble() * (targetGasMix.HeatCapacity / heatCapacity).ToDouble();
		double value = targetGasMix.TotalEnergy.ToDouble() - num;
		double num2 = moleEnergy.ToDouble() * (HeatCapacity / heatCapacity).ToDouble();
		double value2 = Energy.ToDouble() - num2;
		if (Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value) || Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value2))
		{
			signedEnergyToTransfer = new MoleEnergy((double)Math.Sign(signedEnergyToTransfer.ToDouble()) * Math.Min(Math.Abs(value), Math.Abs(value2)));
		}
		if (signedEnergyToTransfer < MoleEnergy.Zero)
		{
			return targetGasMix.TransferEnergyTo(this, -signedEnergyToTransfer);
		}
		MoleEnergy moleEnergy2 = RemoveEnergy(signedEnergyToTransfer);
		if (moleEnergy2 > MoleEnergy.Zero)
		{
			targetGasMix.AddEnergy(moleEnergy2);
		}
		return moleEnergy2;
	}

	public void SetTemperature(TemperatureKelvin t)
	{
		_energy = IdealGas.Energy(HeatCapacity, t);
	}

	public void AddEnergy(MoleEnergy energy)
	{
		if (!energy.IsDenormalOrZero() && !energy.IsNaN())
		{
			_energy += energy;
		}
	}

	public MoleEnergy RemoveEnergy(MoleEnergy energy)
	{
		if (energy <= MoleEnergy.Zero || energy.IsNaN())
		{
			return MoleEnergy.Zero;
		}
		MoleEnergy moleEnergy = RocketMath.Min(Energy, energy);
		_energy -= moleEnergy;
		return moleEnergy;
	}
}
