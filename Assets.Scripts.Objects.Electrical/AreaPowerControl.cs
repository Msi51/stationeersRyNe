using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Effects;
using JetBrains.Annotations;
using Sound;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class AreaPowerControl : ElectricalInputOutput
{
	public enum AreaPowerControlButton
	{
		Off,
		Auto,
		On
	}

	[SerializeField]
	private ApcMaterialChanger _chargingLedMaterialChanger;

	[SerializeField]
	private ApcOnOffAnimComponent _onOffAnimComponent;

	[SerializeField]
	private ApcOpenAnimComponent _openAnimComponent;

	private const float AUDIO_DISTANCE_SQUARED = 25f;

	[SerializeField]
	private Transform _soundPosition;

	private PooledAudioSource _poweredSound;

	private static readonly int ApcPoweredChargingHash = Animator.StringToHash("ApcPoweredCharging");

	private static readonly int ApcPoweredDisChargingHash = Animator.StringToHash("ApcPoweredDisCharging");

	[Header("Area Power Control")]
	[Tooltip("How many watts are used to charge the battery?")]
	public float BatteryChargeRate = 1000f;

	private static EnumCollection<AreaPowerControlButton, int> _areaPowerControlButtons = new EnumCollection<AreaPowerControlButton, int>();

	private float _lastBatteryPower;

	private float _powerProvided;

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	public Slot BatterySlot => Slots[0];

	public override int WreckageQuantity => 0;

	public override float AudioDistanceSquared => 25f;

	public override Transform SoundPosition => _soundPosition;

	public float MaximumPower
	{
		get
		{
			if (!Battery)
			{
				return InputNetwork?.PotentialLoad ?? 0f;
			}
			return Battery.PowerMaximum + (InputNetwork?.PotentialLoad ?? 0f);
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsOperable || InputNetwork == null || !InputNetwork.IsNetworkValid())
			{
				if (OutputNetwork != null)
				{
					return OutputNetwork.IsNetworkValid();
				}
				return false;
			}
			return true;
		}
	}

	public override string[] ModeStrings => EnumCollections.PowerModes.Names;

	protected override float EnergyToHeatRatio => 0f;

	public bool NoPower
	{
		get
		{
			if (!Battery || Battery.IsEmpty)
			{
				if (InputNetwork != null)
				{
					return InputNetwork.PotentialLoad <= 0f;
				}
				return true;
			}
			return false;
		}
	}

	public override float AvailablePower
	{
		get
		{
			float num = InputNetwork?.PotentialLoad ?? 0f;
			if ((bool)Battery && !Battery.IsEmpty)
			{
				num += Battery.PowerStored;
			}
			return num;
		}
	}

	public override bool DoSubmergableTick => true;

	protected override bool CanShortOut
	{
		get
		{
			if (OnOff)
			{
				return !NoPower;
			}
			return false;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(25f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_chargingLedMaterialChanger?.RefreshState();
		_onOffAnimComponent?.RefreshState(skipAnimation);
		_openAnimComponent?.RefreshState(skipAnimation);
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!IsOccluded && OnOff && Powered && Mode != 0)
		{
			int num;
			switch (Mode)
			{
			case 3:
			case 4:
				num = ApcPoweredChargingHash;
				break;
			case 1:
			case 2:
				num = ApcPoweredDisChargingHash;
				break;
			default:
				num = 0;
				break;
			}
			int num2 = num;
			if (!_poweredSound)
			{
				_poweredSound = PlayPooledAudioSound(num2, SoundPosition.localPosition);
			}
			else
			{
				GameAudioSource gameAudioSource = _poweredSound.GameAudioSource;
				if (gameAudioSource == null || gameAudioSource.CurrentClips?.NameHash != num2)
				{
					_poweredSound.Stop();
					_poweredSound = PlayPooledAudioSound(num2, SoundPosition.localPosition);
				}
			}
		}
		else
		{
			_poweredSound?.Stop();
		}
		if ((bool)_poweredSound && !_poweredSound.IsActive)
		{
			_poweredSound = null;
		}
	}

	public override void OnDestroy()
	{
		_poweredSound?.Stop();
		base.OnDestroy();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.BatteryCategory);
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
			LogicType.Maximum => Battery ? Battery.PowerMaximum : 0f, 
			LogicType.Ratio => Battery ? (Battery.PowerStored / Battery.PowerMaximum) : 0f, 
			LogicType.PowerPotential => base.PotentialLoad, 
			LogicType.PowerActual => base.CurrentLoad, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override CanConstructInfo CanConstruct()
	{
		CanMountResult canMountResult = CanMountOnWall();
		if (canMountResult.result == WallMountResult.Valid)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		string arg = Prefab.Find<Crowbar>(Defines.Prefabs.ItemCrowbar)?.DisplayName ?? string.Empty;
		if (IsOpen)
		{
			extendedText.AppendLine(GameStrings.ToolRequiredToClose.AsString(arg));
		}
		else
		{
			extendedText.AppendLine(GameStrings.ToolRequiredToOpen.AsString(arg));
		}
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider != null)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		return passiveTooltip;
	}

	private string GetNextName(Interactable interactable)
	{
		return _areaPowerControlButtons.GetNameFromValue(NextButtonState(interactable));
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GameStrings.PowerControlSetTo.AsString("Lights", GetNextName(interactable)), 
			InteractableType.Button2 => GameStrings.PowerControlSetTo.AsString("Utility", GetNextName(interactable)), 
			InteractableType.Button3 => GameStrings.PowerControlSetTo.AsString("General", GetNextName(interactable)), 
			_ => base.GetContextualName(interactable), 
		};
	}

	private void AdvanceButtonState(Interactable interactable)
	{
		OnServer.Interact(interactable, NextButtonState(interactable));
	}

	private int NextButtonState(Interactable interactable)
	{
		int num = interactable.State + 1;
		if (num > 2)
		{
			num = 0;
		}
		return num;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		Crowbar crowbar = attack.SourceItem as Crowbar;
		if ((bool)crowbar)
		{
			if (!base.AllowInteraction)
			{
				return null;
			}
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = (IsOpen ? ActionStrings.Close : ActionStrings.Open)
			};
			if (IsLocked)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.ApcUnableToMoveLocked);
				return delayedActionInstance;
			}
			if (!crowbar.IsOperable)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.ApcUnableToMoveTool);
				return delayedActionInstance;
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
			}
		}
		return base.AttackWith(attack, doAction);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		switch (interactable.Action)
		{
		case InteractableType.Button1:
		case InteractableType.Button2:
		case InteractableType.Button3:
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			AdvanceButtonState(interactable);
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.OnOff:
			if (doAction && GameManager.RunSimulation)
			{
				CheckPower();
			}
			break;
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running && interactable.Action == InteractableType.OnOff && OnOff)
		{
			CheckConnections();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CheckConnections();
	}

	protected override void CheckConnections()
	{
		base.CheckConnections();
		if (GameManager.RunSimulation && !IsOperable && Error == 0)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && IsOperable && Error == 1)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild as BatteryCell != null)
		{
			_lastBatteryPower = Battery.PowerStored;
			CheckPower();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild == Battery)
		{
			_lastBatteryPower = 0f;
			CheckPower();
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		PowerMode powerMode = PowerMode.Idle;
		if ((bool)Battery && Battery.IsCharged)
		{
			powerMode = PowerMode.Charged;
		}
		else if ((bool)Battery && Battery.PowerStored > _lastBatteryPower && !Battery.IsEmpty)
		{
			powerMode = PowerMode.Charging;
		}
		else if ((bool)Battery && Battery.PowerStored < _lastBatteryPower && !Battery.IsEmpty)
		{
			powerMode = PowerMode.Discharging;
		}
		else if (((bool)Battery && !Battery.IsEmpty) || Powered)
		{
			powerMode = PowerMode.Discharged;
		}
		int num = (int)powerMode;
		if (num != Mode)
		{
			OnServer.Interact(base.InteractMode, num);
		}
		if ((bool)Battery)
		{
			_lastBatteryPower = Battery.PowerStored;
		}
	}

	public override void CheckPower()
	{
		if (GameManager.RunSimulation && NoPower && Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
	}

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		if (InputNetwork == cableNetwork)
		{
			return true;
		}
		return false;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (cableNetwork == OutputNetwork)
		{
			if (_powerProvided > 0f && (bool)Battery && !Battery.IsEmpty)
			{
				float num = Mathf.Min(Battery.PowerStored, _powerProvided);
				Battery.PowerStored -= num;
				_powerProvided -= num;
			}
			_powerProvided += powerUsed;
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if ((InputNetwork == null || cableNetwork == InputNetwork) && InputNetwork != null)
		{
			_powerProvided -= powerAdded;
			if (_powerProvided < 0f && (bool)Battery && !Battery.IsCharged)
			{
				float num = Mathf.Min(Battery.PowerDelta, BatteryChargeRate, powerAdded);
				Battery.PowerStored += num;
				_powerProvided += num;
			}
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		float num = 0f;
		if (OnOff && OutputNetwork != null)
		{
			num = Mathf.Max(_powerProvided, UsedPower);
		}
		if ((bool)Battery && !Battery.IsCharged)
		{
			num += Mathf.Min(BatteryChargeRate, Battery.PowerDelta);
		}
		return num;
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return AvailablePower;
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (IsOpen)
		{
			SetContentsVisibility(isVisible: true);
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (!IsOpen)
		{
			SetContentsVisibility(isVisible: false);
		}
	}

	public void SetContentsVisibility(bool isVisible)
	{
		InteractableType action = BatterySlot.Action;
		foreach (Interactable interactable in Interactables)
		{
			if ((interactable.Action == action || interactable.Action == InteractableType.Slot1 || interactable.Action == InteractableType.OnOff) && (bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		if ((bool)Battery)
		{
			Battery.SetVisibility(isVisible);
		}
	}
}
