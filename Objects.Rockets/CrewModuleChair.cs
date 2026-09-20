using System;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public class CrewModuleChair : Device, IExitable, IRocketInterior, IRocketInternals, IRocketComponent, ISmartRotatable
{
	[SerializeField]
	private Transform _exitTransform;

	[SerializeField]
	private Transform _cameraPoint;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[NonSerialized]
	private CrewModule _crewModule;

	public bool FreeLook => true;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public CrewModule CrewModule => _crewModule;

	public RocketNetwork RocketNetwork { get; set; }

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Slot1)
		{
			return InteractWithSlot1(interactable, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private DelayedActionInstance InteractWithSlot1(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 2f,
			ActionMessage = ActionStrings.GetIn
		};
		DynamicThing occupant = interaction.SourceSlot.Occupant;
		if ((bool)interactable.Slot.Occupant && !CanExitSeat())
		{
			return delayedActionInstance.Fail(GameStrings.RocketCannotExitSeatInFlight);
		}
		if ((bool)interactable.Slot.Occupant || (bool)occupant)
		{
			return HandleSwitch(interaction, interactable.Slot.SlotIndex, delayedActionInstance, doAction);
		}
		if (GameManager.RunSimulation && doAction)
		{
			OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, interactable.Slot);
		}
		return delayedActionInstance.Succeed();
	}

	public bool CanExitSeat()
	{
		Rocket rocket = RocketNetwork?.Rocket;
		if (rocket != null)
		{
			RocketState rocketState = rocket.RocketState;
			return rocketState == RocketState.None || rocketState == RocketState.OnLaunchMount;
		}
		return true;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation && newChild is Entity)
		{
			RocketNetwork?.Rocket?.EnforceMannedTargetRestriction();
		}
		if (InventoryManager.ParentHuman != newChild)
		{
			GameManager.SetSteamRichPresence("rocket", RocketNetwork?.Rocket?.DisplayName ?? "Unknown");
		}
	}

	private void ShowPopupWhenArriveInOrbit()
	{
		if (RocketNetwork?.Rocket?.CurrentNode?.Owner is LaunchMount { IsOrbital: not false } launchMount && RocketNetwork.Rocket.CanPopUpPoiText)
		{
			RocketNetwork.Rocket.CanPopUpPoiText = false;
			PointOfInterestManager.ShowPopup(launchMount.DisplayName, GameStrings.RocketLowOrbit.DisplayString, UIAudioManager.PointOfInterestSpace);
		}
	}

	public void Exit(Human human)
	{
		human.MoveToWorld(GetExitPosition(null), Quaternion.identity, Vector3.zero, Vector3.zero);
		ShowPopupWhenArriveInOrbit();
	}

	public Transform GetCameraPoint(Entity entity)
	{
		return _cameraPoint;
	}

	public Vector3 GetExitPosition(Entity entity)
	{
		return _exitTransform.position;
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
		Rocket rocket = RocketNetwork?.Rocket;
		if (rocket == null)
		{
			return;
		}
		NodeType? nodeType = rocket.CurrentNode?.NodeType;
		if (!nodeType.HasValue || nodeType != NodeType.LowOrbitLaunchPad)
		{
			return;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Contains<Human>(out var occupant) && occupant == InventoryManager.ParentHuman)
			{
				Achievements.Achieve(Achievements.Kind.AchievementMajorTomToGroundControl);
			}
		}
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

	public void OpenSeatScreen()
	{
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			if (!(@internal is RocketDataDownLink rocketDataDownLink))
			{
				continue;
			}
			foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in rocketDataDownLink.ConnectedDataNetReceivers)
			{
				foreach (Device device in connectedDataNetReceiver.DataCableNetwork.DeviceList)
				{
					if (device is Computer { CurrentMotherboard: RocketMotherboard currentMotherboard })
					{
						currentMotherboard.ToggleUI();
						return;
					}
				}
			}
		}
	}
}
