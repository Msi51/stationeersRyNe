using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class WallHeater : SmallDevice, ISmartRotatable
{
	[SerializeField]
	[FormerlySerializedAs("HeatTransferJoulesPerTick")]
	private float heatTransferJoulesPerTick = 1000f;

	private float _powerUsedDuringTick;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public const float MAX_TEMPERATURE = 2500f;

	public static readonly TemperatureKelvin MAXTemperature = new TemperatureKelvin(2500.0);

	public MoleEnergy HeatTransferJoulesPerTick => new MoleEnergy(heatTransferJoulesPerTick);

	protected override bool IsOperable
	{
		get
		{
			if (Error == 1)
			{
				if (!base.HasOpenGrid)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (base.HasOpenGrid)
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

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if (base.SwitchOnOff != null)
		{
			base.SwitchOnOff.RefreshState(skipAnimation);
		}
		if (MaterialChanger != null)
		{
			MaterialChanger.ChangeState((OnOff && Powered && Error == 0) ? Defines.Animator.OnPowered : Defines.Animator.Off);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AirContitioningAtmos);
	}

	public override void OnAtmosphericTick()
	{
		if (OnOff && Powered && IsOperable && base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid).IsAboveArmstrong())
		{
			Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			if (atmosphere != null && atmosphere.Temperature < MAXTemperature)
			{
				atmosphere.GasMixture.AddEnergy(HeatTransferJoulesPerTick);
				atmosphere.Sparked = true;
				_powerUsedDuringTick = HeatTransferJoulesPerTick.ToFloat();
			}
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

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
