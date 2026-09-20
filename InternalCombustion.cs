using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InternalCombustion
{
	public IInternalCombustion Parent;

	public const float FULL_THROTTLE = 100f;

	public const float MAX_STRESS = 100f;

	public const float MAX_LEVER_ROTATION = -50f;

	private const float MINIMUM_SAFE_RPM_DELTA = 1f;

	private static readonly MoleQuantity MaxMolarInput = new MoleQuantity(0.009999999776482582);

	private static readonly PressurekPa RunningTargetPressure = new PressurekPa(4000.0);

	private bool _gainedStress;

	private MoleEnergy _normalCombustionEnergyCache = MoleEnergy.Zero;

	private PressurekPa _targetPressure = RunningTargetPressure;

	private float _rpm;

	private float _combustionLimiter;

	private float _throttle;

	private float _stress;

	public bool DidCombustionLastTick { get; private set; }

	public float Rpm
	{
		get
		{
			return _rpm;
		}
		set
		{
			value = Mathf.Max(0f, value);
			if (!RocketMath.Approximately(Rpm, value) && NetworkManager.IsServer)
			{
				Parent.NetworkUpdateFlags |= 16384;
			}
			_rpm = value;
		}
	}

	public float CombustionLimiter
	{
		get
		{
			return _combustionLimiter;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 100f);
			value = Mathf.Round(value / 10f) * 10f;
			float combustionLimiter = CombustionLimiter;
			_combustionLimiter = value;
			AnimateCombustionLimiter(combustionLimiter).Forget();
			if (NetworkManager.IsServer)
			{
				Parent.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public float Throttle
	{
		get
		{
			return _throttle;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 100f);
			value = Mathf.Round(value / 10f) * 10f;
			float throttle = Throttle;
			_throttle = value;
			AnimateThrottle(throttle).Forget();
			if (NetworkManager.IsServer)
			{
				Parent.NetworkUpdateFlags |= 4096;
			}
		}
	}

	public float Stress
	{
		get
		{
			return _stress;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 100f);
			if (!RocketMath.Approximately(Stress, value) && NetworkManager.IsServer)
			{
				Parent.NetworkUpdateFlags |= 32768;
			}
			_stress = value;
		}
	}

	public InternalCombustion(IInternalCombustion parent)
	{
		Parent = parent;
	}

	private async UniTaskVoid AnimateThrottle(float lastValue)
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		Parent.ThrottleLever.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0f, -50f, Throttle / 100f), Vector3.right);
		if (!RocketMath.Approximately(lastValue, Throttle))
		{
			Parent.GetAsThing.PlayPooledAudioSound((lastValue > Throttle) ? Defines.Sounds.ThrottleLeverDown : Defines.Sounds.ThrottleLeverUp, Parent.ThrottleLever.localPosition);
		}
	}

	private async UniTaskVoid AnimateCombustionLimiter(float lastValue)
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		Parent.CombustionLever.localRotation = Quaternion.AngleAxis(Mathf.Lerp(0f, -50f, CombustionLimiter / 100f), Vector3.right);
		if (!RocketMath.Approximately(lastValue, CombustionLimiter))
		{
			Parent.GetAsThing.PlayPooledAudioSound((lastValue > CombustionLimiter) ? Defines.Sounds.ThrottleLeverDown : Defines.Sounds.ThrottleLeverUp, Parent.CombustionLever.localPosition);
		}
	}

	public void HandleShutDown()
	{
		if (!_gainedStress)
		{
			Rpm -= 15f;
			_targetPressure = RocketMath.Lerp(PressurekPa.Zero, RunningTargetPressure, Mathf.Clamp01(Rpm / 1000f));
		}
	}

	public void ManualCombust()
	{
		_normalCombustionEnergyCache = Parent.InternalAtmosphere.CombustionEnergy;
		Parent.InternalAtmosphere.TryCombust(Mathf.Clamp(Mathf.Pow(CombustionLimiter / 10f, 2f) * 0.0075f, 0.001f, 1f), force: true);
	}

	public void SpeedTick()
	{
		_targetPressure = RunningTargetPressure;
		float num = 0f;
		MoleEnergy moleEnergy = new MoleEnergy(30.0);
		float num2 = Rpm;
		if (Parent.InternalAtmosphere.Temperature >= new TemperatureKelvin(573.15) && Parent.InternalAtmosphere.CombustionEnergy + _normalCombustionEnergyCache >= moleEnergy)
		{
			num = Mathf.Lerp(0f, 12f, Mathf.Clamp01(Parent.InternalAtmosphere.Temperature.ToFloat() / 5000f));
			MoleEnergy energy = moleEnergy * num;
			Parent.InternalAtmosphere.GasMixture.RemoveEnergy(energy);
			num2 += num;
			DidCombustionLastTick = true;
		}
		else
		{
			DidCombustionLastTick = false;
		}
		float num3 = 0.99f;
		float num4 = num2 * num3;
		float num5 = num2 - num4;
		if (Parent.GetAsThing.OnOff && Parent.GetAsThing.Powered)
		{
			Rpm = num4;
		}
		float num6 = Mathf.Abs(num - num5);
		float num7 = Mathf.Lerp(0.8f, 2f, Mathf.Clamp01((Rpm - 300f) / 1200f));
		bool flag = num6 > 1f / num7;
		if (!_gainedStress && (!Parent.GetAsThing.OnOff || !Parent.GetAsThing.Powered))
		{
			flag = false;
		}
		if (flag)
		{
			Stress = Mathf.Clamp(Stress + (num6 - 1f) * num7, 0f, 100f);
			_gainedStress = true;
		}
		else
		{
			Stress = Mathf.Clamp(Stress - 0.5f, 0f, 100f);
			_gainedStress = false;
		}
		if (Stress >= 100f)
		{
			Rpm -= 40f;
			if (Parent.GetAsThing.Error == 0)
			{
				OnServer.Interact(Parent.GetAsThing.InteractError, 1);
			}
		}
	}

	public void HandleGasOutput(PressurekPa pressurePerTick, PipeNetwork inputNetwork, PipeNetwork outputNetwork)
	{
		PressurekPa pressureGassesAndLiquids = Parent.InternalAtmosphere.PressureGassesAndLiquids;
		if (!(pressureGassesAndLiquids <= _targetPressure))
		{
			PressurekPa pressure = pressureGassesAndLiquids - _targetPressure;
			MoleQuantity transferMoles = RocketMath.Min(IdealGas.Quantity(pressurePerTick, Chemistry.PipeVolume, inputNetwork.Atmosphere.Temperature), IdealGas.Quantity(pressure, Parent.InternalAtmosphere.Volume, Parent.InternalAtmosphere.Temperature) * 0.1);
			GasMixture gasMixture = Parent.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
			outputNetwork.Atmosphere.Add(gasMixture);
		}
	}

	public void HandleGasInput(PipeNetwork inputNetwork)
	{
		Thing getAsThing = Parent.GetAsThing;
		if (getAsThing.OnOff && getAsThing.Powered && getAsThing.Error <= 0)
		{
			MoleQuantity transferMoles = MaxMolarInput * (Throttle / 100f);
			GasMixture gasMixture = inputNetwork.Atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
			Parent.InternalAtmosphere.Add(gasMixture);
		}
	}
}
