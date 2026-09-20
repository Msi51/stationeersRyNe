using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class H2CombustorMachine : DeviceInputOutputCircuit
{
	[SerializeField]
	private float volume = 1000f;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override bool HasReadableAtmosphere => true;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = (base.IsInputValid || base.IsInput2Valid) && base.IsOutputValid && !flag;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
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

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered || !IsOperable)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, OutputNetwork.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
		if (Mode == 0)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
		}
		else if (_hasInputGas())
		{
			MoleQuantity transferMoles;
			GasMixture gasMixture = AtmosphereHelper.TakeNormalisedGasPressureScaled(InputNetwork.Atmosphere, base.PressurePerTick, InputNetwork.Atmosphere.PressureGasses - base.InternalAtmosphere.PressureGasses, out transferMoles);
			if (gasMixture.IsValid)
			{
				if (Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
				base.InternalAtmosphere.Add(gasMixture);
			}
			else if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.InternalAtmosphere.TryCombust(0.99, force: true);
			base.ProcessedMoles = transferMoles;
		}
		else
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
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

	private bool _hasInputGas()
	{
		if (InputNetwork?.Atmosphere != null)
		{
			return InputNetwork.Atmosphere.TotalMoles >= AtmosphereHelper.MinimumMolesForProcessing;
		}
		return false;
	}
}
