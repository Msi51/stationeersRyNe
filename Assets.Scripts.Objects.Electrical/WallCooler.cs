using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class WallCooler : SmallDeviceOutput, ISmartRotatable
{
	[Header("Wall Cooler")]
	public float HeatTransferJoulesPerTick = 1000f;

	[Tooltip("How efficient the cooler will be depending on the temperature difference between input and output")]
	public AnimationCurve TemperatureDeltaEfficiency;

	private float _powerUsedDuringTick;

	private Atmosphere _environment;

	protected override bool IsOperable
	{
		get
		{
			if (Error == 1)
			{
				if (!HasPipeNetwork || !base.HasOpenGrid || !IsEnvironmentOkay || !IsPipeEnvironmentOkay)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (HasPipeNetwork && base.HasOpenGrid && IsEnvironmentOkay && IsPipeEnvironmentOkay)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public bool IsEnvironmentOkay
	{
		get
		{
			if (_environment != null)
			{
				return _environment.IsAboveArmstrong();
			}
			return false;
		}
	}

	public bool IsPipeEnvironmentOkay
	{
		get
		{
			if (base.IsOutputValid && ConnectedPipeNetwork.Atmosphere != null)
			{
				return ConnectedPipeNetwork.Atmosphere.IsAboveArmstrong();
			}
			return false;
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered)
		{
			return;
		}
		_environment = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		if (IsOperable)
		{
			TemperatureKelvin temperatureKelvin = _environment.GasMixture.Temperature - ConnectedPipeNetwork.Atmosphere.Temperature;
			float num = TemperatureDeltaEfficiency.Evaluate(temperatureKelvin.ToFloat());
			MoleEnergy moleEnergy = _environment.GasMixture.RemoveEnergy(new MoleEnergy(HeatTransferJoulesPerTick * num));
			if (!(moleEnergy <= MoleEnergy.Zero))
			{
				_powerUsedDuringTick += moleEnergy.ToFloat();
				ConnectedPipeNetwork.Atmosphere.GasMixture.AddEnergy(moleEnergy);
			}
		}
	}
}
