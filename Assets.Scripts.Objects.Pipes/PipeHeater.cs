using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Pipes;

public class PipeHeater : DevicePipeMounted, IRocketInternals, IRocketComponent
{
	public float HeatTransferJoulesPerTick = 1000f;

	private float _powerUsedDuringTick;

	private Atmosphere _worldAtmosphere;

	public new RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public new bool StrictlyInternal => false;

	public new RocketNetwork RocketNetwork { get; set; }

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocalGrid(base.ThingTransformPosition + ThingTransform.up * 0.5f);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && base.NetworkAtmosphere != null && base.NetworkAtmosphere.IsValid() && base.NetworkAtmosphere != null && base.NetworkAtmosphere.IsAboveArmstrong() && base.NetworkAtmosphere.Temperature < WallHeater.MAXTemperature)
		{
			base.NetworkAtmosphere.GasMixture.AddEnergy(new MoleEnergy(HeatTransferJoulesPerTick));
			_powerUsedDuringTick = HeatTransferJoulesPerTick;
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

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public new void OnLaunch(bool immediate = false)
	{
	}

	public new void OnLanded(bool immediate = false)
	{
	}
}
