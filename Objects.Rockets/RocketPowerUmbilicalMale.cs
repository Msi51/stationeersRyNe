using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Objects.Structures;
using UnityEngine;
using Util;

namespace Objects.Rockets;

public class RocketPowerUmbilicalMale : RocketPowerUmbilical
{
	[SerializeField]
	private UmbilicalAnimComponent umbilicalAnimComponent;

	[SerializeField]
	private AssignableMultiStateAnimComponent umbilicalModeAnimComponent;

	[SerializeField]
	private UmbilicalConnection umbilicalConnection;

	private BatteryCellState _batteryState;

	private CancellationTokenWrapper _activateCancellation = new CancellationTokenWrapper();

	public override UmbilicalType UmbilicalType => UmbilicalType.Umbilical;

	public override Vector3 FirstPartnerSearchPosition => umbilicalConnection.LocalGrid.ToVector3();

	public new bool CanTransfer
	{
		get
		{
			if (Powered && OnOff && Error == 0 && IsOpen)
			{
				return !IsBroken;
			}
			return false;
		}
	}

	private bool PartnerValid
	{
		get
		{
			if ((object)base.PartnerUmbilical != null && base.PartnerUmbilical.CanTransfer)
			{
				if (base.PartnerUmbilical.RocketNetwork != null)
				{
					Rocket rocket = base.PartnerUmbilical.RocketNetwork.Rocket;
					if (rocket == null)
					{
						return false;
					}
					return rocket.RocketState == RocketState.OnLaunchMount;
				}
				return true;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (PartnerValid)
			{
				return InputNetwork != null;
			}
			return false;
		}
	}

	public override string[] ModeStrings => RocketUmbilicalHelper.ModeStrings;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Umbilical;

	public new Thing AsThing => this;

	public Type PartnerType => typeof(RocketPowerUmbilicalFemale);

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.UmbilicalCategory);
	}

	public override CanConstructInfo CanConstruct()
	{
		List<Structure> list = base.GridController?.GetCell(new WorldGrid(base.Position))?.AllStructures;
		if (list != null)
		{
			foreach (Structure item in list)
			{
				if (item is RocketTower)
				{
					return base.CanConstruct();
				}
			}
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.RocketTowerPlacementRule.AsString(ToTooltip()));
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if ((bool)umbilicalAnimComponent)
		{
			umbilicalAnimComponent.UpdateExtendedPosition(base.PartnerUmbilical?.PartnerDistance ?? 0);
			umbilicalAnimComponent.RefreshState(skipAnimation);
		}
		if ((bool)umbilicalModeAnimComponent)
		{
			umbilicalModeAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 1)
		{
			_activateCancellation.Cancel();
			_activateCancellation.Initialize();
			WaitThenStop(_activateCancellation.Token).Forget();
		}
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Error && interactable.State == 1 && IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
		if (interactable.Action != InteractableType.Mode && GameManager.RunSimulation)
		{
			RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
		}
	}

	private async UniTaskVoid WaitThenStop(CancellationToken token)
	{
		await UniTask.Delay(550, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Activate)
		{
			if (!Powered)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (Activate == 1)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyInUse);
			}
			if (Error != 0)
			{
				return delayedActionInstance.Fail(GameStrings.UmbilicalError);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(base.InteractActivate, 1);
			OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Activate)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Activate => false, 
			LogicType.Mode => false, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (logicType == LogicType.Open)
		{
			if (OnOff && Error == 0)
			{
				OnServer.Interact(base.InteractOpen, (int)Mathf.Clamp((float)value, 0f, 1f));
			}
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (CanTransfer && PartnerValid)
		{
			MovePowerToUmbilical();
		}
	}

	private void CheckError()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (!IsOperable)
		{
			if (Error == 0)
			{
				base.LastPowerAdded = 0f;
				base.LastPowerRemoved = 0f;
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
		}
		else if (Error == 1)
		{
			OnServer.Interact(base.InteractError, 0, skipAnimation: true);
		}
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

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			if ((bool)base.PartnerUmbilical)
			{
				base.PartnerUmbilical.PartnerRemoved();
			}
			base.PartnerUmbilical = null;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketPowerUmbilicalMaleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketPowerUmbilicalMaleSaveData rocketPowerUmbilicalMaleSaveData)
		{
			base.PowerStored = rocketPowerUmbilicalMaleSaveData.PowerStored;
			_savedPartnerId = rocketPowerUmbilicalMaleSaveData.PartnerUmbilicalId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketPowerUmbilicalMaleSaveData rocketPowerUmbilicalMaleSaveData)
		{
			if (float.IsNaN(base.PowerStored))
			{
				base.PowerStored = 0f;
			}
			rocketPowerUmbilicalMaleSaveData.PowerStored = base.PowerStored;
			rocketPowerUmbilicalMaleSaveData.PartnerUmbilicalId = base.PartnerUmbilical?.ReferenceId ?? 0;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPartner(Thing.Find<RocketPowerUmbilicalFemale>(_savedPartnerId));
		RefreshAnimState(skipAnimation: true);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((bool)base.PartnerUmbilical)
		{
			base.PartnerUmbilical.PartnerRemoved();
		}
		base.PartnerUmbilical = null;
		RetractUmbilical();
	}

	private void MovePowerToUmbilical()
	{
		float num = Mathf.Min(Mathf.Clamp(base.PartnerUmbilical.PowerMaximum - base.PartnerUmbilical.PowerStored, 0f, PowerMaximum), base.PowerStored);
		base.PartnerUmbilical.ReceivePower(null, num);
		base.PowerStored -= num;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (!OnOff)
		{
			base.LastPowerRemoved = 0f;
			return;
		}
		base.LastPowerRemoved = powerUsed;
		base.PowerStored = Mathf.Clamp(base.PowerStored - powerUsed, 0f, PowerMaximum);
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (Error == 1 || !OnOff)
		{
			base.LastPowerAdded = 0f;
			return;
		}
		base.LastPowerAdded = powerAdded;
		base.PowerStored = Mathf.Clamp(powerAdded + base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		if (Error == 1 && OnOff)
		{
			return UsedPower;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return UsedPower + Mathf.Clamp(PowerMaximum - base.PowerStored, 0f, PowerMaximum);
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
		return Mathf.Max(base.PowerStored, 0f);
	}

	public void OnLaunch(bool immediate = false)
	{
		base.PartnerUmbilical.PartnerRemoved();
		base.PartnerUmbilical = null;
	}

	public void OnLanded(bool immediate = false)
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void SetPartner(IUmbilical partner)
	{
		base.PartnerUmbilical = partner as RocketPowerUmbilicalFemale;
		OnServer.Interact(base.InteractMode, RocketUmbilicalHelper.FindModePosition(this, base.PartnerUmbilical));
		CheckError();
	}

	public override bool IsCompatibleWith(IUmbilical other)
	{
		return other is RocketPowerUmbilicalFemale;
	}

	public override void PartnerRemoved()
	{
		SetPartner(null);
	}

	public new void RetractUmbilical()
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
		if (IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
	}

	public override void ExtendUmbilical()
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
		if (!IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action != InteractableType.Activate)
		{
			return base.GetContextualName(interactable);
		}
		if (!IsOpen)
		{
			return ActionStrings.Extend;
		}
		return ActionStrings.Retract;
	}
}
