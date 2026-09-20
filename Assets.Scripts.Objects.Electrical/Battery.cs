using System;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Networks;
using Objects.Rockets;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Battery : ElectricalInputOutput, IRocketInternals, IRocketComponent, IChargable, IReferencable, IEvaluable, IRocketMassContributor
{
	public static string[] BatteryModeStrings = Enum.GetNames(typeof(BatteryCellState));

	private BatteryCellState _batteryState;

	[SerializeField]
	private MeshRenderer[] displayRenderers;

	[SerializeField]
	private Material[] displayModeMaterials;

	[Header("Battery")]
	public float PowerMaximum = 3600000f;

	public GameObject Screen;

	[SerializeField]
	private RocketInternalCellType _rocketInternalCellType;

	[SerializeField]
	private bool _strictlyInternal;

	private UniTask _flashingTask;

	private bool _powerUpdate;

	private float _powerStored;

	private Interactable _modeInteractable;

	private bool _destroyed;

	private const float LOSS_NORMAL = 10f;

	private const float LOSS_IN_COLD = 50f;

	private IRocketInternals _rocketInternalsImplementation;

	public float MassOnRocket;

	public override string[] ModeStrings => BatteryModeStrings;

	public RocketInternalCellType InternalCellType => _rocketInternalCellType;

	public bool StrictlyInternal => _strictlyInternal;

	public RocketNetwork RocketNetwork { get; set; }

	public float PowerStored
	{
		get
		{
			return _powerStored;
		}
		set
		{
			if (!float.IsNaN(value))
			{
				_powerStored = Mathf.Clamp(value, 0f, PowerMaximum);
				if (NetworkManager.IsServer && NetworkServer.HasClients())
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public float PowerRatio => PowerStored / PowerMaximum;

	public float PowerDelta => PowerStored - PowerMaximum;

	public bool IsEmpty => Mode == 0;

	public bool IsCharged => Mode == 6;

	public override float AvailablePower => PowerStored;

	public override bool Powered => _batteryState != BatteryCellState.Empty;

	public override int PoweredValue
	{
		get
		{
			if (!Powered)
			{
				return 0;
			}
			return 1;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted || IsBroken)
			{
				return false;
			}
			return base.IsOperable;
		}
	}

	protected override float EnergyToHeatRatio => 0f;

	public override bool DoSubmergableTick => true;

	public float MassContribution => MassOnRocket;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override StringBuilder GetExtendedText()
	{
		if (InputNetwork != OutputNetwork || InputNetwork == null)
		{
			return base.GetExtendedText();
		}
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.DeviceShortCircuited.AsColor("red"));
		return extendedText;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (!OnOff)
		{
			SetRenderersState(0, displayModeMaterials[Mode]);
			return;
		}
		switch (Mode)
		{
		case 0:
			SetRenderersState(0, displayModeMaterials[Mode]);
			break;
		case 1:
			if (_flashingTask.Status != UniTaskStatus.Pending)
			{
				_flashingTask = FlashingDisplay();
			}
			break;
		case 2:
			SetRenderersState(1, displayModeMaterials[Mode]);
			break;
		case 3:
			SetRenderersState(2, displayModeMaterials[Mode]);
			break;
		case 4:
			SetRenderersState(3, displayModeMaterials[Mode]);
			break;
		case 5:
			SetRenderersState(4, displayModeMaterials[Mode]);
			break;
		case 6:
			SetRenderersState(5, displayModeMaterials[Mode]);
			break;
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		RefreshAnimState();
		if (newState != previousState)
		{
			Achievements.AchievePowerOverwhelming(this);
		}
	}

	private void SetRenderersState(int numberToEnable, Material material)
	{
		if (displayRenderers == null)
		{
			return;
		}
		for (int num = displayRenderers.Length - 1; num >= 0; num--)
		{
			MeshRenderer meshRenderer = displayRenderers[num];
			if ((bool)meshRenderer)
			{
				bool flag = base.IsStructureCompleted && num < numberToEnable;
				meshRenderer.gameObject.SetActive(flag);
				if (flag)
				{
					meshRenderer.material = material;
				}
			}
		}
	}

	private async UniTask FlashingDisplay()
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (Mode == 1 && OnOff)
		{
			SetRenderersState(1, displayModeMaterials[Mode]);
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested || Mode != 1 || !OnOff)
			{
				break;
			}
			SetRenderersState(1, displayModeMaterials[0]);
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public float GetPowerMaximum()
	{
		return PowerMaximum;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Charge || logicType - 23 <= LogicType.Mode)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Charge => AvailablePower, 
			LogicType.Maximum => PowerMaximum, 
			LogicType.Ratio => PowerStored / PowerMaximum, 
			LogicType.PowerPotential => base.PotentialLoad, 
			LogicType.PowerActual => base.CurrentLoad, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Mode)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (!_destroyed)
		{
			float num = _powerStored / PowerMaximum;
			_batteryState = BatteryCellState.Empty;
			if (num >= 0.999f)
			{
				_batteryState = BatteryCellState.Full;
			}
			else if (num >= 0.75f)
			{
				_batteryState = BatteryCellState.High;
			}
			else if (num >= 0.5f)
			{
				_batteryState = BatteryCellState.Medium;
			}
			else if (num >= 0.25f)
			{
				_batteryState = BatteryCellState.Low;
			}
			else if (num >= 0.1f)
			{
				_batteryState = BatteryCellState.VeryLow;
			}
			else if (num > 0f)
			{
				_batteryState = BatteryCellState.Critical;
			}
			if (HasPowerState)
			{
				CheckPower();
			}
			if (!_powerUpdate && GameManager.RunSimulation && _batteryState != (BatteryCellState)Mode)
			{
				_powerUpdate = true;
				WaitThenUpdateChargeDisplay().Forget();
			}
		}
	}

	public override void CheckPower()
	{
		if (GameManager.RunSimulation && base.InteractPowered.State == 1 != Powered)
		{
			OnServer.Interact(base.InteractPowered, Powered ? 1 : 0);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.BatteryCategory);
	}

	public override void Awake()
	{
		base.Awake();
		_modeInteractable = Interactables.Find((Interactable i) => i.Action == InteractableType.Mode);
		if (GameManager.RunSimulation)
		{
			WaitThenUpdateChargeDisplay().Forget();
		}
	}

	private void CheckError()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0, skipAnimation: true);
			}
		}
	}

	private StringBuilder GetInfoBoxString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), ((BatteryCellState)Mode).GetName()));
		return stringBuilder;
	}

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetInfoBoxString().ToString());
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckError();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckError();
	}

	public override CanConstructInfo CanConstruct()
	{
		if (InternalCellType != RocketInternalCellType.None)
		{
			return base.CanConstruct();
		}
		if (!HasFrameBelow())
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	private async UniTaskVoid WaitCheckState()
	{
		if (!base.IsBeingDestroyed)
		{
			CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
			await UniTask.NextFrame(cancelToken);
			if (!cancelToken.IsCancellationRequested)
			{
				CheckError();
			}
		}
	}

	private async UniTaskVoid WaitThenUpdateChargeDisplay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (GameManager.GameState == GameState.Running && !base.IsBeingDestroyed && !cancelToken.IsCancellationRequested)
		{
			Mode = (int)_batteryState;
			OnServer.Interact(_modeInteractable, Mode);
			_powerUpdate = false;
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.OnOff && interactable.State == 1)
		{
			WaitCheckState().Forget();
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			_destroyed = true;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new BatterySaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is BatterySaveData batterySaveData)
		{
			PowerStored = batterySaveData.PowerStored;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is BatterySaveData batterySaveData)
		{
			if (float.IsNaN(PowerStored))
			{
				PowerStored = 0f;
			}
			batterySaveData.PowerStored = PowerStored;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(PowerStored);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			PowerStored = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(PowerStored);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		PowerStored = reader.ReadSingle();
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (Error != 1 && OnOff && cableNetwork == OutputNetwork && IsOperable)
		{
			PowerStored = Mathf.Clamp(PowerStored - powerUsed, 0f, PowerMaximum);
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (Error != 1 && OnOff && cableNetwork == InputNetwork && IsOperable)
		{
			PowerStored = Mathf.Clamp(powerAdded + PowerStored, 0f, PowerMaximum);
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || Error == 1 || cableNetwork != InputNetwork || !IsOperable)
		{
			return 0f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Clamp(PowerMaximum - PowerStored, 0f, PowerMaximum);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork || !IsOperable)
		{
			return 0f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Max(PowerStored, 0f);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (_rocketInternalCellType != RocketInternalCellType.None || PowerStored <= float.Epsilon)
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		if (!atmosphere.IsAboveArmstrong())
		{
			PowerStored -= 50f;
			return;
		}
		float num = 10f;
		if (atmosphere.Temperature < Chemistry.Temperature.ZeroDegrees)
		{
			num = Mathf.Max(50f * (TemperatureKelvin.One - atmosphere.Temperature / Chemistry.Temperature.ZeroDegrees).ToFloat() * atmosphere.HeatExchangeRatio(), num);
		}
		PowerStored -= num;
		atmosphere.GasMixture.AddEnergy(new MoleEnergy(num));
	}

	public override void ValidateOnLoad(int currentSaveVersion)
	{
		base.ValidateOnLoad(currentSaveVersion);
		if (currentSaveVersion <= 25553 && StrictlyInternal)
		{
			base.CurrentBuildStateIndex = BuildStates.Count - 1;
			UpdateStateVisualizer();
		}
	}
}
