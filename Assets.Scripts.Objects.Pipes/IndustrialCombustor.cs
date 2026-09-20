using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class IndustrialCombustor : DeviceInputOutputCircuit, IExtendableStructure, IReferencable, IEvaluable
{
	private const float HEAT_SINK_AREA = 3f;

	private const float HEAT_SINK_HEAT_CAPACITY = 100000f;

	private const double VOLUME = 8000.0;

	private static VolumeLitres Volume = new VolumeLitres(8000.0);

	private HeatSink _heatSink;

	public const float COMBUSTION_RATIO = 0.95f;

	private const double MAX_OPERATING_TEMP_C = 1000.0;

	private bool _gainedStress;

	public static readonly TemperatureKelvin MaxTemperature = new TemperatureKelvin(1273.15);

	[SerializeField]
	private List<StructureExtensionInfo> extensions;

	private MoleEnergy _energyTransfer;

	private TemperatureKelvin _machineTemperature;

	private bool _stressedToFailure;

	public override bool HasReadableAtmosphere => true;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted || !FullyExtended)
			{
				return false;
			}
			bool flag = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = base.IsInputValid && base.IsInput2Valid && base.IsOutputValid && !flag && !StressedToFailure;
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

	public bool FullyExtended
	{
		get
		{
			if (ChamberExtension != null && ChamberExtension.IsStructureCompleted && ChimneyExtension != null)
			{
				return ChimneyExtension.IsStructureCompleted;
			}
			return false;
		}
	}

	public List<StructureExtensionInfo> CompatibleExtensions => extensions;

	public List<IStructureExtension> Extensions { get; } = new List<IStructureExtension>(2);

	public IndustrialCombustorChamberExtension ChamberExtension { get; private set; }

	public IndustrialCombustorChimneyExtension ChimneyExtension { get; private set; }

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

	private TemperatureKelvin MachineTemperature
	{
		get
		{
			return _machineTemperature;
		}
		set
		{
			if (!RocketMath.Approximately(value, _machineTemperature))
			{
				_machineTemperature = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public bool StressedToFailure
	{
		get
		{
			return _stressedToFailure;
		}
		set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
			_stressedToFailure = value;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == _infoScreen.InfoTrigger)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			if (StressedToFailure)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.StressedToFailure);
			}
			StringManager.AddKeyValueLine(Tooltip.ToolTipStringBuilder, GameStrings.MachineTemperature, MachineTemperature.ToFloat().ToStringPrefix("K", "yellow"));
			Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.HeatExchangerEnergyTransfer.AsString(AtmosAnalyser.GetEnergyUnitString(EnergyTransfer.ToFloat())));
			result.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			_heatSink = new HeatSink(new HeatCapacity(100000.0), Chemistry.Temperature.ZeroDegrees, 3f);
		}
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid;
		bool flag2 = !base.IsInput2Valid;
		bool flag3 = !base.IsOutputValid;
		bool flag4 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2 || flag3 || flag4))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2 && !flag3 && !flag4) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override bool HasFrameBelow(float upOffset = 0f)
	{
		return base.HasFrameBelow((0f - GridSize) / 2f + upOffset);
	}

	public override void OnAtmosphericTick()
	{
		if (!FullyExtended)
		{
			return;
		}
		Atmosphere outputAtmos = AtmosphericsController.World.CloneGlobalAtmosphere(ChimneyExtension.VentGrid, 0L);
		AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, outputAtmos, PressurekPa.MaxValue, AtmosphereHelper.MatterState.Gas);
		_heatSink.HeatExchange(base.InternalAtmosphere);
		if (OutputNetwork != null)
		{
			EnergyTransfer = _heatSink.HeatExchange(OutputNetwork.Atmosphere, 10f);
		}
		MachineTemperature = _heatSink.Temperature;
		StressedToFailure = MachineTemperature > MaxTemperature;
		if (!OnOff || !Powered || !IsOperable)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
		}
		else if (Mode == 0)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.ProcessedMoles = MoleQuantity.Zero;
		}
		else if (_hasInputGas())
		{
			float denominator = 0.8f;
			MoleQuantity transferMoles;
			GasMixture gasMixture = AtmosphereHelper.TakeNormalisedGasPressureScaled(InputNetwork.Atmosphere, base.PressurePerTick, InputNetwork.Atmosphere.PressureGasses - base.InternalAtmosphere.PressureGasses, out transferMoles, AtmosphereHelper.MatterState.All, denominator);
			MoleQuantity transferMoles2;
			GasMixture gasMixture2 = AtmosphereHelper.TakeNormalisedGasPressureScaled(InputNetwork2.Atmosphere, base.PressurePerTick, InputNetwork2.Atmosphere.PressureGasses - base.InternalAtmosphere.PressureGasses, out transferMoles2, AtmosphereHelper.MatterState.All, denominator);
			if (gasMixture.IsValid)
			{
				if (Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
				base.InternalAtmosphere.Add(gasMixture);
			}
			if (gasMixture2.IsValid)
			{
				if (Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
				base.InternalAtmosphere.Add(gasMixture2);
			}
			if (!gasMixture.IsValid && !gasMixture2.IsValid && Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			base.InternalAtmosphere.TryCombust(0.949999988079071, force: true);
			base.InternalAtmosphere.StateChange();
			base.ProcessedMoles = transferMoles + transferMoles2;
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

	private bool _hasInputGas()
	{
		if (InputNetwork?.Atmosphere == null || !(InputNetwork.Atmosphere.TotalMoles >= AtmosphereHelper.MinimumMolesForProcessing))
		{
			if (InputNetwork2.Atmosphere != null)
			{
				return InputNetwork2.Atmosphere.TotalMoles >= AtmosphereHelper.MinimumMolesForProcessing;
			}
			return false;
		}
		return true;
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		if (GameManager.GameState != GameState.None)
		{
			for (int num = Extensions.Count - 1; num >= 0; num--)
			{
				IStructureExtension structureExtension = Extensions[num];
				if (structureExtension == null)
				{
					Extensions.RemoveAt(num);
				}
				else
				{
					RemoveExtension(structureExtension);
				}
			}
		}
		base.OnDestroy();
	}

	public bool CanExtend(IStructureExtension extension)
	{
		foreach (IStructureExtension extension2 in Extensions)
		{
			if (extension2.SourcePrefab.PrefabHash == extension.SourcePrefab.PrefabHash)
			{
				return false;
			}
		}
		foreach (StructureExtensionInfo compatibleExtension in CompatibleExtensions)
		{
			if (compatibleExtension.Prefab.PrefabHash == extension.SourcePrefab.PrefabHash && RocketMath.Approximately(extension.Position, compatibleExtension.ExtensionPosition.position) && RocketMath.Approximately(extension.ThingTransformRotation.eulerAngles, Rotation.eulerAngles, 0.1f))
			{
				return true;
			}
		}
		return false;
	}

	public void Extend(IStructureExtension extension)
	{
		if (CanExtend(extension))
		{
			Extensions.Add(extension);
			extension.ExtendableParent = this;
			OnExtend(extension);
		}
	}

	public void RemoveExtension(IStructureExtension extension)
	{
		OnRemove(extension);
		if (Extensions.Contains(extension))
		{
			Extensions.Remove(extension);
		}
		extension.ExtendableParent = null;
	}

	private void OnExtend(IStructureExtension extension)
	{
		if (!(extension is IndustrialCombustorChamberExtension chamberExtension))
		{
			if (!(extension is IndustrialCombustorChimneyExtension chimneyExtension))
			{
				throw new NotImplementedException("extension");
			}
			ChimneyExtension = chimneyExtension;
		}
		else
		{
			ChamberExtension = chamberExtension;
		}
	}

	private void OnRemove(IStructureExtension extension)
	{
		if (!(extension is IndustrialCombustorChamberExtension))
		{
			if (!(extension is IndustrialCombustorChimneyExtension))
			{
				throw new NotImplementedException("extension");
			}
			ChimneyExtension = null;
		}
		else
		{
			ChamberExtension = null;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(EnergyTransfer.ToFloat());
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteBoolean(StressedToFailure);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)MachineTemperature.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			EnergyTransfer = new MoleEnergy(reader.ReadSingle());
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			StressedToFailure = reader.ReadBoolean();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			MachineTemperature = new TemperatureKelvin((int)reader.ReadUInt16());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(EnergyTransfer.ToFloat());
		writer.WriteBoolean(StressedToFailure);
		writer.WriteUInt16((ushort)MachineTemperature.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyTransfer = new MoleEnergy(reader.ReadSingle());
		StressedToFailure = reader.ReadBoolean();
		MachineTemperature = new TemperatureKelvin((int)reader.ReadUInt16());
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new IndustrialCombustorSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is IndustrialCombustorSaveData industrialCombustorSaveData)
		{
			MachineTemperature = new TemperatureKelvin(industrialCombustorSaveData.MachineTemperature);
			StressedToFailure = industrialCombustorSaveData.StressedToFailure;
			_heatSink.SetTemperature(MachineTemperature);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is IndustrialCombustorSaveData industrialCombustorSaveData)
		{
			industrialCombustorSaveData.MachineTemperature = MachineTemperature.ToDouble();
			industrialCombustorSaveData.StressedToFailure = StressedToFailure;
		}
	}
}
