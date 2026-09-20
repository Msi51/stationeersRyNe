using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Rockets.Scanning;
using Objects.Structures;
using Trading;
using UnityEngine;
using Util;

namespace Objects.Rockets;

public class RocketCrewUmbilical : Device, IUmbilical, IRocketComponent, IReferencable, IEvaluable, ISmartRotatable
{
	[Header("Crew Umbilical")]
	[SerializeField]
	private UmbilicalConnection _umbilicalConnection;

	[SerializeField]
	private Transform _exitPoint;

	[SerializeField]
	private UmbilicalAnimComponent _umbilicalAnimComponent;

	[SerializeField]
	private UmbilicalEndAnimComponent _umbilicalEndAnimComponent;

	[SerializeField]
	private Transform _doorLocation;

	private long _umbilicalDoorId;

	[Header("Smart Rotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	private CrewModule _partnerUmbilical;

	private long _savedPartnerId;

	private readonly CancellationTokenWrapper _activateCancellation = new CancellationTokenWrapper();

	private static int UmbilicalDoorNameHash = Animator.StringToHash("StructureCrewUmbilicalDoor");

	public RocketCrewUmbilicalDoor UmbilicalDoor { get; set; }

	public Vector3 ExitPosition => _exitPoint.position;

	public bool CanUse
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

	public bool PartnerValid
	{
		get
		{
			if ((object)_partnerUmbilical != null)
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

	public Thing AsThing => this;

	public Type PartnerType => typeof(CrewModule);

	public int PartnerDistance { get; set; }

	public Vector3 FirstPartnerSearchPosition => _umbilicalConnection.LocalGrid.ToVector3();

	public UmbilicalType UmbilicalType => UmbilicalType.Umbilical;

	public float GetActionProgress => 0f;

	public IRocketActionProgressableTarget CurrentTarget => null;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RocketUmbilicalHelper.FindAndSetOtherUmbilicalLargeGrid(this);
		if (UmbilicalDoor == null)
		{
			UmbilicalDoor = Thing.Create<RocketCrewUmbilicalDoor>(UmbilicalDoorNameHash, _doorLocation.position, Quaternion.identity, 0L);
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((object)_partnerUmbilical != null)
		{
			_partnerUmbilical.PartnerRemoved();
		}
		OnServer.Destroy(UmbilicalDoor);
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
		if (_umbilicalAnimComponent != null)
		{
			CrewModule partnerUmbilical = _partnerUmbilical;
			float valueOrDefault = ((float?)(((object)partnerUmbilical != null) ? new int?(partnerUmbilical.PartnerDistance * 2) : ((int?)null)) + 1.1f).GetValueOrDefault();
			_umbilicalAnimComponent.UpdateExtendedPosition(valueOrDefault);
			_umbilicalAnimComponent.RefreshState(skipAnimation);
		}
		if (_umbilicalEndAnimComponent != null)
		{
			CrewModule partnerUmbilical2 = _partnerUmbilical;
			float valueOrDefault2 = ((float?)(((object)partnerUmbilical2 != null) ? new int?(partnerUmbilical2.PartnerDistance * 2) : ((int?)null)) + 1f).GetValueOrDefault();
			_umbilicalEndAnimComponent.UpdateExtendedPosition(valueOrDefault2, invert: true);
			_umbilicalEndAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.UmbilicalCategory);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 1)
		{
			_activateCancellation.CancelAndInitialize();
			WaitThenStop(_activateCancellation.Token).Forget();
		}
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Error && interactable.State == 1 && IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
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
		return interactable.Action switch
		{
			InteractableType.Activate => InteractWithActivate(interactable, interaction, doAction), 
			InteractableType.Button1 => InteractWithButton1(interactable, interaction, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance InteractWithActivate(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
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

	private DelayedActionInstance InteractWithButton1(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (Error != 0)
		{
			return delayedActionInstance.Fail(GameStrings.UmbilicalError);
		}
		if (!CanUse)
		{
			return delayedActionInstance.Fail();
		}
		if (!PartnerValid)
		{
			return delayedActionInstance.Fail();
		}
		Slot seatSlot = _partnerUmbilical.GetSeatSlot();
		if (seatSlot == null)
		{
			return delayedActionInstance.Fail(GameStrings.RocketNoAvailableSeat);
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, seatSlot);
		}
		return delayedActionInstance.Succeed();
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
		if (logicType == LogicType.Activate)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
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

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WritePackedId(writer, _partnerUmbilical);
		Network.WritePackedId(writer, UmbilicalDoor);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Network.ReadPackedId(reader, out _savedPartnerId);
		Network.ReadPackedId(reader, out _umbilicalDoorId);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketCrewUmbilicalSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is RocketCrewUmbilicalSaveData rocketCrewUmbilicalSaveData)
		{
			rocketCrewUmbilicalSaveData.PartnerUmbilicalId = _partnerUmbilical?.ReferenceId ?? 0;
			rocketCrewUmbilicalSaveData.UmbilicalDoorId = UmbilicalDoor?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is RocketCrewUmbilicalSaveData rocketCrewUmbilicalSaveData)
		{
			_savedPartnerId = rocketCrewUmbilicalSaveData.PartnerUmbilicalId;
			_umbilicalDoorId = rocketCrewUmbilicalSaveData.UmbilicalDoorId;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPartner(Thing.Find<CrewModule>(_savedPartnerId));
		UmbilicalDoor = Thing.Find<RocketCrewUmbilicalDoor>(_umbilicalDoorId);
		RefreshAnimState(skipAnimation: true);
	}

	public void PartnerRemoved()
	{
		SetPartner(null);
	}

	public void RetractUmbilical()
	{
	}

	public void ExtendUmbilical()
	{
	}

	public bool IsCompatibleWith(IUmbilical other)
	{
		return other is CrewModule;
	}

	public void SetPartner(IUmbilical partner)
	{
		_partnerUmbilical = partner as CrewModule;
		OnServer.Interact(base.InteractMode, RocketUmbilicalHelper.FindModePosition(this, _partnerUmbilical));
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		result = RocketActionResult.Failure(GameStrings.None);
		return result;
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
	}

	public void ClearAction()
	{
	}

	public string GetActionInfoText()
	{
		return "";
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		return null;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
	}
}
