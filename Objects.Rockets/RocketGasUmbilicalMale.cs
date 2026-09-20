using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Rockets.Scanning;
using Objects.Structures;
using Trading;
using UnityEngine;
using Util;

namespace Objects.Rockets;

public class RocketGasUmbilicalMale : DeviceInput, IUmbilical, IRocketComponent, IReferencable, IEvaluable
{
	private RocketGasUmbilicalFemale _partnerUmbilical;

	[SerializeField]
	private UmbilicalAnimComponent umbilicalAnimComponent;

	[SerializeField]
	private AssignableMultiStateAnimComponent umbilicalModeAnimComponent;

	[SerializeField]
	private UmbilicalConnection umbilicalConnection;

	private CancellationTokenWrapper _activateCancellation = new CancellationTokenWrapper();

	private long _savedPartnerId;

	private bool InputValidWithAtmosphere
	{
		get
		{
			if (IsInputValid)
			{
				return ConnectedPipeNetwork.Atmosphere != null;
			}
			return false;
		}
	}

	private bool CanTransfer
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
			if ((object)_partnerUmbilical != null && _partnerUmbilical.InputValidWithAtmosphere)
			{
				if (_partnerUmbilical.RocketNetwork != null)
				{
					Rocket rocket = _partnerUmbilical.RocketNetwork.Rocket;
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

	private Atmosphere ConnectedAtmosphere => ConnectedPipeNetwork?.Atmosphere;

	public UmbilicalType UmbilicalType => UmbilicalType.Umbilical;

	public Vector3 FirstPartnerSearchPosition => umbilicalConnection.LocalGrid.ToVector3();

	protected override bool IsOperable
	{
		get
		{
			bool flag = IsInputValid && base.GridController.CanContainAtmos(base.WorldGrid) && PartnerValid;
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	public override string[] ModeStrings => RocketUmbilicalHelper.ModeStrings;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Umbilical;

	public RocketNetwork RocketNetwork { get; set; }

	public Thing AsThing => this;

	public Type PartnerType => typeof(RocketGasUmbilicalFemale);

	public int PartnerDistance { get; set; }

	public float GetActionProgress => 0f;

	public IRocketActionProgressableTarget CurrentTarget { get; private set; }

	public bool CanProgressAction(out RocketActionResult result)
	{
		return result = RocketActionResult.Failure(GameStrings.None);
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
	}

	public void ClearAction()
	{
	}

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
		return CanConstructInfo.InvalidPlacement(GameStrings.RocketTowerPlacementRule.AsString(DisplayName));
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (umbilicalAnimComponent != null)
		{
			umbilicalAnimComponent.UpdateExtendedPosition(_partnerUmbilical?.PartnerDistance ?? 0);
			umbilicalAnimComponent.RefreshState(skipAnimation);
		}
		if (umbilicalModeAnimComponent != null)
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

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((object)_partnerUmbilical != null)
		{
			_partnerUmbilical.PartnerRemoved();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOperable && CanTransfer && InputValidWithAtmosphere && PartnerValid)
		{
			AtmosphereHelper.Mix(ConnectedAtmosphere, _partnerUmbilical.ConnectedAtmosphere, AtmosphereHelper.MatterState.All);
		}
	}

	public void OnLaunch(bool immediate = false)
	{
		_partnerUmbilical.PartnerRemoved();
	}

	public void OnLanded(bool immediate = false)
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public void SetPartner(IUmbilical partner)
	{
		_partnerUmbilical = partner as RocketGasUmbilicalFemale;
		OnServer.Interact(base.InteractMode, RocketUmbilicalHelper.FindModePosition(this, _partnerUmbilical));
	}

	public bool IsCompatibleWith(IUmbilical other)
	{
		if (!(other is RocketGasUmbilicalFemale rocketGasUmbilicalFemale))
		{
			return false;
		}
		foreach (Connection accessOpenEnd in base.AccessOpenEnds)
		{
			foreach (Connection accessOpenEnd2 in rocketGasUmbilicalFemale.AccessOpenEnds)
			{
				if (accessOpenEnd.ConnectionType == accessOpenEnd2.ConnectionType)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void PartnerRemoved()
	{
		SetPartner(null);
	}

	public void RetractUmbilical()
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
		if (IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
	}

	public void ExtendUmbilical()
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
		if (!IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WritePackedId(writer, _partnerUmbilical);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Network.ReadPackedId(reader, out _savedPartnerId);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketGasUmbilicalMaleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketGasUmbilicalMaleSaveData rocketGasUmbilicalMaleSaveData)
		{
			rocketGasUmbilicalMaleSaveData.PartnerUmbilicalId = _partnerUmbilical?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketGasUmbilicalMaleSaveData rocketGasUmbilicalMaleSaveData)
		{
			_savedPartnerId = rocketGasUmbilicalMaleSaveData.PartnerUmbilicalId;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPartner(Thing.Find<RocketGasUmbilicalFemale>(_savedPartnerId));
		RefreshAnimState(skipAnimation: true);
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

	public string GetActionInfoText()
	{
		return string.Empty;
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		return null;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
		CurrentTarget = selectedTarget;
	}
}
