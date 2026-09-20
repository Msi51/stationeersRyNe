using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class RoboticArmDoor : Device, ISmartRotatable
{
	[SerializeField]
	private GenericAssignableAnimComponent _doorAnimComponent;

	public List<Collider> DoorColliders = new List<Collider>();

	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	public override bool OnOff => true;

	public override bool CanAirPass
	{
		get
		{
			if (!IsOpen || base.NeverAirPass)
			{
				return base.CanAirPass;
			}
			return true;
		}
	}

	public override bool CanLightPass
	{
		get
		{
			if (!IsOpen)
			{
				return base.CanLightPass;
			}
			return true;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (_doorAnimComponent != null)
		{
			_doorAnimComponent.RefreshState(skipAnimation: true);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_doorAnimComponent != null)
		{
			_doorAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.DoorCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.DoorCategory;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new RoboticArmDoorSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		_ = thingSaveData is RoboticArmDoorSaveData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		_ = thingSaveData is RoboticArmDoorSaveData;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!base.AllowInteraction || !base.IsStructureCompleted)
		{
			return base.AttackWith(attack, doAction);
		}
		Crowbar crowbar = attack.SourceItem as Crowbar;
		if ((bool)crowbar && (attack.TargetCollider == null || DoorColliders.Contains(attack.TargetCollider) || attack.TargetCollider.transform == ThingTransform))
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = crowbar.DoorForceOpenDuration,
				ActionMessage = (IsOpen ? ActionStrings.ForceClose : ActionStrings.ForceOpen)
			};
			if (attack.TargetCollider != null && attack.TargetCollider.transform != ThingTransform)
			{
				delayedActionInstance.Selection = GetSelection(attack.TargetCollider);
			}
			if (IsLocked)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenLocked);
				return delayedActionInstance;
			}
			if (!Powered)
			{
				if (doAction && GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
				}
				return delayedActionInstance;
			}
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenPowered);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public Structure IsSideBlocked(bool allStructual = false)
	{
		Grid3 grid = new Grid3(base.ThingTransformPosition);
		foreach (Structure faceStructure in base.GridController.GetFaceStructures(grid))
		{
			Grid3 grid2 = new Grid3(faceStructure.ThingTransformPosition);
			if (allStructual && faceStructure.StructureCollisionType == CollisionType.BlockGrid)
			{
				return faceStructure;
			}
			if (!(grid != grid2) && (faceStructure.IsDoor || allStructual))
			{
				return faceStructure;
			}
		}
		return null;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			base.GridController.UpdateAirState(this);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		if ((bool)IsSideBlocked(allStructual: true))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.DisplayString);
		}
		return base.CanConstruct();
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
