using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Nitrolyzer : DeviceInputOutputCircuit, IThermal
{
	[SerializeField]
	private float volume = 10f;

	public static MoleEnergy ActiveUsedPower = new MoleEnergy(6000.0);

	public static float IdleUsedPower = 50f;

	public static float MinimumReactionRatio = 0.01f;

	public static MoleQuantity MolesPerTick = new MoleQuantity(1.5);

	public static float MaximumEfficiency = 0.3f;

	private float _efficiencyCached;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public bool CanReact
	{
		get
		{
			if (base.InternalAtmosphere != null && base.InternalAtmosphere.GasMixture.GetTotalMolesGasses > MoleQuantity.Zero && base.InternalAtmosphere.GasMixture.GetGasTypeRatio(Chemistry.GasType.Nitrogen) > MinimumReactionRatio)
			{
				return base.InternalAtmosphere.GasMixture.GetGasTypeRatio(Chemistry.GasType.Oxygen) > MinimumReactionRatio;
			}
			return false;
		}
	}

	private float _reactionEfficiencyCached
	{
		get
		{
			return _efficiencyCached;
		}
		set
		{
			if (!RocketMath.Approximately(value, _efficiencyCached, 0.01f))
			{
				_efficiencyCached = value;
				if (NetworkManager.IsServer && NetworkServer.HasClients())
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public override bool HasReadableAtmosphere => true;

	protected override bool IsOperable
	{
		get
		{
			bool num = !base.IsInputValid && !base.IsInput2Valid;
			bool flag = !base.IsOutputValid;
			bool flag2 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag3 = !num && !flag && !flag2;
			if (Error == 1)
			{
				if (!flag3)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag3)
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

	public float ReactionEfficiency()
	{
		if (!CanReact)
		{
			return 0f;
		}
		float gasTypeRatio = base.InternalAtmosphere.GasMixture.GetGasTypeRatio(Chemistry.GasType.Oxygen);
		float gasTypeRatio2 = base.InternalAtmosphere.GasMixture.GetGasTypeRatio(Chemistry.GasType.Nitrogen);
		float num = ((gasTypeRatio <= gasTypeRatio2) ? (gasTypeRatio / gasTypeRatio2) : (gasTypeRatio2 / gasTypeRatio));
		return (gasTypeRatio2 + gasTypeRatio) * num * MaximumEfficiency;
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		UsedPower = IdleUsedPower;
		if (base.IsOutputValid)
		{
			OutputNetwork.Atmosphere.Add(base.InternalAtmosphere.GasMixture);
			base.InternalAtmosphere.GasMixture.Reset();
		}
		if (!OnOff || !Powered || Error > 0 || Mode == 0 || !IsOperable)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			_reactionEfficiencyCached = 0f;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		bool num = InputNetwork?.Atmosphere != null;
		bool flag = InputNetwork2?.Atmosphere != null;
		MoleQuantity transferMoles = ((num && flag) ? (MolesPerTick * 0.5) : MolesPerTick);
		if (num)
		{
			base.InternalAtmosphere.Add(InputNetwork.Atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All));
		}
		if (flag)
		{
			base.InternalAtmosphere.Add(InputNetwork2.Atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All));
		}
		float num2 = (_reactionEfficiencyCached = ReactionEfficiency());
		base.ProcessedMoles = MolesPerTick;
		if (CanReact)
		{
			UsedPower = ActiveUsedPower.ToFloat();
			if (Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			GasMixture gasMixture = GasMixtureHelper.Create();
			gasMixture.Add(new Mole(Chemistry.GasType.Oxygen, base.InternalAtmosphere.GasMixture.Oxygen.Quantity * num2, base.InternalAtmosphere.GasMixture.Oxygen.Energy * num2));
			gasMixture.Add(new Mole(Chemistry.GasType.Nitrogen, base.InternalAtmosphere.GasMixture.Nitrogen.Quantity * num2, base.InternalAtmosphere.GasMixture.Nitrogen.Energy * num2));
			base.InternalAtmosphere.Remove(gasMixture, AtmosphereHelper.MatterState.All);
			float num4 = MaximumEfficiency - num2;
			base.InternalAtmosphere.Add(new Mole(Chemistry.GasType.NitrousOxide, gasMixture.GetTotalMolesGasses * 0.5, gasMixture.TotalEnergy * 0.5 + ActiveUsedPower * num4));
		}
		else if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid && !base.IsInput2Valid;
		bool flag2 = !base.IsOutputValid;
		bool flag3 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2 || flag3))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2 && !flag3) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder infoPanelOperationText = base.GetInfoPanelOperationText();
		float num = Mathf.Round(_reactionEfficiencyCached / MaximumEfficiency * 100f) * 100f;
		infoPanelOperationText.AppendLine(GameStrings.ConversionEfficiency.AsString(num.ToStringPercent((num > 90f) ? "green" : "yellow")));
		return infoPanelOperationText;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(_efficiencyCached);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			_efficiencyCached = reader.ReadSingle();
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new NitrolyzerSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}
}
