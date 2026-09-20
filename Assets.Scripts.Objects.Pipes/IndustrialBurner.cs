using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Trading;
using UnityEngine;
using UnityEngine.Rendering;
using Util;

namespace Assets.Scripts.Objects.Pipes;

public class IndustrialBurner : FurnaceBase, IExtendableStructure, IReferencable, IEvaluable
{
	private const float HEAT_SINK_AREA = 100f;

	private const float HEAT_SINK_HEAT_CAPACITY = 50000f;

	private const double MIN_OXYGEN_PRESSURE = 20.0;

	public const double OXYGEN_TO_CARBON_RATIO = 100.0;

	private const float MAX_STRESS = 100f;

	private const double JOULES_PER_UNIT_CARBON = 2300000.0;

	private const double MAX_OPERATING_TEMP_C = 500.0;

	private const float MIN_STRESS_GAIN = 0.5f;

	private const float MAX_STRESS_GAIN = 2f;

	private const float MAX_STRESS_TEMP_OFFSET = 200f;

	private HeatSink _heatSink;

	[SerializeField]
	private List<StructureExtensionInfo> extensions;

	[SerializeField]
	private Collider temperatureGaugeCollider;

	[SerializeField]
	private Transform temperatureGaugeTransform;

	private static readonly Vector3 TemperatureGaugeRange = new Vector3(0f, 0.125f, 0f);

	[SerializeField]
	private Dial stressDial;

	[SerializeField]
	private Dial pressureDial;

	private MoleEnergy _energyTransfer;

	private TemperatureKelvin _machineTemperature;

	private float _stress;

	private bool _stressedToFailure;

	private bool _gainedStress;

	private static readonly TemperatureKelvin SafeTemperatureChange = TemperatureKelvin.One * 1.0;

	public static readonly TemperatureKelvin MaxTemperature = new TemperatureKelvin(773.15);

	private CancellationTokenWrapper BurnCancellation = new CancellationTokenWrapper();

	[SerializeField]
	private MaterialChanger indicatorIn;

	public List<StructureExtensionInfo> CompatibleExtensions => extensions;

	public List<IStructureExtension> Extensions { get; } = new List<IStructureExtension>(2);

	public IndustrialBurnerChimneyExtension ChimneyExtension { get; private set; }

	public IndustrialBurnerHeatExchangerExtension HeatExchangerExtension { get; private set; }

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
				base.NetworkUpdateFlags |= 512;
			}
			_stress = value;
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

	public float CarbonToPollutantRatio { get; set; }

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && FullyExtended && !StressedToFailure;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				OnServer.Interact(base.InteractError, 0);
				return true;
			}
			if (flag)
			{
				return true;
			}
			OnServer.Interact(base.InteractError, 1);
			return false;
		}
	}

	public bool FullyExtended
	{
		get
		{
			if (HeatExchangerExtension != null && HeatExchangerExtension.IsStructureCompleted && ChimneyExtension != null)
			{
				return ChimneyExtension.IsStructureCompleted;
			}
			return false;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (base.IsImportOpen)
			{
				return ImportingThing is GrindableOre;
			}
			return false;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == stressDial.Collider)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			if (StressedToFailure)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.StressedToFailure);
			}
			if (Stress < 50f)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + Stress.ToStringPercent("green"));
			}
			else if (Stress < 80f)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + Stress.ToStringPercent("yellow"));
			}
			else
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + Stress.ToStringPercent("red"));
			}
			result.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result;
		}
		if (hitCollider == pressureDial.Collider)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result2 = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, Tooltip.ToolTipStringBuilder);
			result2.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result2;
		}
		if (hitCollider == temperatureGaugeCollider)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result3 = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			StringManager.AddKeyValueLine(Tooltip.ToolTipStringBuilder, GameStrings.MachineTemperature, MachineTemperature.ToFloat().ToStringPrefix("K", "yellow"));
			Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.HeatExchangerEnergyTransfer.AsString(AtmosAnalyser.GetEnergyUnitString(EnergyTransfer.ToFloat())));
			Tooltip.ToolTipStringBuilder.Append(GameStrings.CarbonDioxideRatio.AsString(StringManager.Get(CarbonToPollutantRatio)));
			result3.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result3;
		}
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result4 = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			string value = ReagentMixture.ToString();
			Tooltip.ToolTipStringBuilder.AppendLine(value);
			result4.Title = Localization.GetInterface("Contents");
			result4.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result4;
		}
		return base.GetPassiveTooltip(hitCollider);
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
		if (!(extension is IndustrialBurnerChimneyExtension chimneyExtension))
		{
			if (!(extension is IndustrialBurnerHeatExchangerExtension heatExchangerExtension))
			{
				throw new NotImplementedException("extension");
			}
			HeatExchangerExtension = heatExchangerExtension;
		}
		else
		{
			ChimneyExtension = chimneyExtension;
		}
	}

	private void OnRemove(IStructureExtension extension)
	{
		if (!(extension is IndustrialBurnerChimneyExtension))
		{
			if (!(extension is IndustrialBurnerHeatExchangerExtension))
			{
				throw new NotImplementedException("extension");
			}
			HeatExchangerExtension = null;
		}
		else
		{
			ChimneyExtension = null;
		}
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		if (base.IsStructureCompleted && base.InternalAtmosphere != null)
		{
			double num = RocketMath.MapToScale(273.15, MaxTemperature.ToDouble(), 0.0, 1.0, MachineTemperature.ToDouble());
			Vector3 b = Vector3.Lerp(-TemperatureGaugeRange, TemperatureGaugeRange, (float)num);
			temperatureGaugeTransform.localPosition = Vector3.Lerp(temperatureGaugeTransform.localPosition, b, deltaTime);
			stressDial.UpdatePosition(Stress, deltaTime);
			pressureDial.UpdatePosition(base.InternalAtmosphere.PressureGasses.ToFloat(), deltaTime);
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override ShadowCastingMode GetShadowCastingMode()
	{
		return ShadowCastingMode.On;
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			stressDial.Init();
			pressureDial.Init();
			_heatSink = new HeatSink(new HeatCapacity(50000.0), Chemistry.Temperature.ZeroDegrees, 100f);
		}
	}

	public bool CanBurn()
	{
		return base.InternalAtmosphere.PartialPressureO2 > new PressurekPa(20.0);
	}

	public override void HandleGasInput()
	{
		if (!base.IsStructureCompleted || !FullyExtended)
		{
			return;
		}
		Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(ChimneyExtension.VentGrid, 0L);
		float num = RocketMath.MapToScaleClamp(PressurekPa.Zero.ToFloat(), Chemistry.Limits.MAXPressureGasPipe.ToFloat(), Chemistry.OneAtmosphere.ToFloat(), Chemistry.Limits.MAXPressureGasPipe.ToFloat() / 10f, base.InternalAtmosphere.PressureGasses.ToFloat());
		AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, outputAtmos, new PressurekPa(num), AtmosphereHelper.MatterState.Gas);
		if (!OnOff || !Powered || !IsOperable)
		{
			return;
		}
		if (InputNetwork != null)
		{
			AtmosphereHelper.MoveVolume(InputNetwork.Atmosphere, base.InternalAtmosphere, new VolumeLitres(base.OutputSetting), AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
		}
		if (CanBurn())
		{
			double num2 = 0.0;
			double num3 = 0.0;
			MoleQuantity quantity = base.InternalAtmosphere.GasMixture.Oxygen.Quantity;
			GasMixture gasMixture = GasMixtureHelper.Invalid;
			if ((double)ReagentMixture.Carbon > 0.0)
			{
				num2 = quantity.ToDouble() / 100.0;
				num2 = Math.Min(num2, ReagentMixture.Carbon.Quantity);
				gasMixture = base.InternalAtmosphere.Remove(new MoleQuantity(num2 * 100.0), Chemistry.GasType.Oxygen);
			}
			else if ((double)ReagentMixture.Hydrocarbon > 0.0)
			{
				num3 = quantity.ToDouble() / 100.0;
				num3 = Math.Min(num3, ReagentMixture.Hydrocarbon.Quantity);
				gasMixture = base.InternalAtmosphere.Remove(new MoleQuantity(num3 * 100.0), Chemistry.GasType.Oxygen);
			}
			if (gasMixture.IsValid)
			{
				float num4 = (CarbonToPollutantRatio = CarbonDioxideToPollutantRatio());
				base.InternalAtmosphere.Add(new Mole(Chemistry.GasType.CarbonDioxide, gasMixture.Oxygen.Quantity * num4, gasMixture.Oxygen.Energy * num4));
				base.InternalAtmosphere.Add(new Mole(Chemistry.GasType.Pollutant, gasMixture.Oxygen.Quantity * (1f - num4), gasMixture.Oxygen.Energy * (1f - num4)));
				double num6 = num2 + num3;
				base.InternalAtmosphere.GasMixture.AddEnergy(new MoleEnergy(num6 * 2300000.0));
			}
			BurnCancellation.CancelAndInitialize();
			BurnCarbonNextFrame(BurnCancellation.Token, num2, num3).Forget();
		}
	}

	public override void HandleGasOutput()
	{
		if (FullyExtended)
		{
			TemperatureKelvin temperature = _heatSink.Temperature;
			_heatSink.HeatExchange(base.InternalAtmosphere);
			if (OutputNetwork != null)
			{
				EnergyTransfer = _heatSink.HeatExchange(OutputNetwork.Atmosphere);
			}
			MachineTemperature = _heatSink.Temperature;
			HandleStress(MachineTemperature - temperature);
		}
	}

	private void HandleStress(TemperatureKelvin temperatureDelta)
	{
		float num = Mathf.Max(0f, (MachineTemperature - Chemistry.Temperature.ZeroDegrees).ToFloat());
		float num2 = (MaxTemperature - Chemistry.Temperature.ZeroDegrees).ToFloat();
		float t = Mathf.Clamp01(num / num2);
		float num3 = Mathf.Lerp(0.5f, 2f, t);
		bool flag = temperatureDelta > SafeTemperatureChange / num3 || MachineTemperature > MaxTemperature;
		if (!_gainedStress && (!OnOff || !Powered))
		{
			flag = false;
		}
		if (flag)
		{
			Stress = Mathf.Clamp(Stress + ((temperatureDelta - SafeTemperatureChange) * num3).ToFloat(), 0f, 100f);
			if (MachineTemperature > MaxTemperature)
			{
				Stress += RocketMath.MapToScale(MaxTemperature.ToFloat(), MaxTemperature.ToFloat() + 200f, 0.5f, 2f, MachineTemperature.ToFloat());
			}
			_gainedStress = true;
		}
		else
		{
			Stress = Mathf.Clamp(Stress - 0.5f, 0f, 100f);
			_gainedStress = false;
		}
		if (Stress > 99f && !StressedToFailure)
		{
			StressedToFailure = true;
		}
		if (StressedToFailure && Stress < 50f)
		{
			StressedToFailure = false;
		}
	}

	public float CarbonDioxideToPollutantRatio()
	{
		return RocketMath.MapToScaleClamp(273.15f, MaxTemperature.ToFloat(), 0f, 1f, MachineTemperature.ToFloat());
	}

	private async UniTaskVoid BurnCarbonNextFrame(CancellationToken token, double carbonUsed, double hydrocarbonUsed)
	{
		await UniTask.SwitchToMainThread(token);
		if (carbonUsed > 0.0)
		{
			ReagentMixture.Set(ReagentMixture.Carbon.ReagentId, ReagentMixture.Carbon.Quantity - carbonUsed);
		}
		else if (hydrocarbonUsed > 0.0)
		{
			ReagentMixture.Set(ReagentMixture.Hydrocarbon.ReagentId, ReagentMixture.Hydrocarbon.Quantity - hydrocarbonUsed);
		}
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (base.IsImportClosed)
		{
			if (ImportingThing != null)
			{
				Smelt(ImportingThing);
			}
			else
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered)
		{
			RefreshIndicators().Forget();
		}
	}

	private async UniTaskVoid RefreshIndicators()
	{
		if (!GameManager.IsBatchMode)
		{
			await UniTask.SwitchToMainThread();
			if (GameManager.GameState == GameState.Running && !base.BeingDestroyed)
			{
				indicatorIn.ChangeState((Powered && base.OutputSetting > 0f) ? Defines.Animator.OnPowered : Defines.Animator.Off);
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Button3:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.InputLitres, StringManager.Get((int)base.OutputSetting));
			delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.InputLitres, StringManager.Get((int)base.OutputSetting));
			delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			delayedActionInstance.AppendStateMessage(GameStrings.UseLabelerToSet);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnDestroy()
	{
		BurnCancellation.Cancel();
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

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(EnergyTransfer.ToFloat());
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteByte((byte)Stress);
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
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			Stress = (int)reader.ReadByte();
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
		writer.WriteByte((byte)Stress);
		writer.WriteBoolean(StressedToFailure);
		writer.WriteUInt16((ushort)MachineTemperature.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyTransfer = new MoleEnergy(reader.ReadSingle());
		Stress = (int)reader.ReadByte();
		StressedToFailure = reader.ReadBoolean();
		MachineTemperature = new TemperatureKelvin((int)reader.ReadUInt16());
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is IndustrialBurnerSaveData industrialBurnerSaveData)
		{
			industrialBurnerSaveData.Stress = Stress;
			industrialBurnerSaveData.MachineTemperature = MachineTemperature.ToDouble();
			industrialBurnerSaveData.StressedToFailure = StressedToFailure;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new IndustrialBurnerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is IndustrialBurnerSaveData industrialBurnerSaveData)
		{
			Stress = industrialBurnerSaveData.Stress;
			MachineTemperature = new TemperatureKelvin(industrialBurnerSaveData.MachineTemperature);
			StressedToFailure = industrialBurnerSaveData.StressedToFailure;
			_heatSink.SetTemperature(MachineTemperature);
		}
	}
}
