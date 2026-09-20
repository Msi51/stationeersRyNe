using System;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects.Chutes;

public class Harvester : DeviceImportExport
{
	[ReadOnly]
	private DynamicThing _occupant;

	private static readonly string _harvestClipName = "Harvest";

	private static readonly string _plantClipName = "Plant";

	private bool _readyForPlantExporting;

	private bool _isHarvesting;

	private bool _isPlanting;

	private static readonly string[] BatteryStateStrings = Enum.GetNames(typeof(FaceState));

	private bool _isWaitingToTryHarvest;

	private bool _isWaitingToTryPlant;

	private int _smallGridSize = 5;

	private int _nSmallGridsBelow = 4;

	private Slot GetRobotHandSlot => Slots[2];

	public override string[] ModeStrings => BatteryStateStrings;

	private IHarvestable HydroponicTray { get; set; }

	private Plant Plant
	{
		get
		{
			if (!IsTray)
			{
				return null;
			}
			return HydroponicTray.GetPlant;
		}
	}

	private bool IsTray => HydroponicTray?.GetThing;

	private ArmControl CurrentState => (ArmControl)Activate;

	private Plant ImportPlant => ImportingThing as Plant;

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 68 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Plant:
			if (value <= 0.0)
			{
				return;
			}
			TryPlantSeed();
			break;
		case LogicType.Harvest:
			if (value <= 0.0)
			{
				return;
			}
			TryHarvestPlant();
			break;
		}
		base.SetLogicValue(logicType, value);
	}

	private void CheckForTray(bool force = false)
	{
		if ((!IsTray || HydroponicTray.IsBeingDestroyed || force) && GetBelowStructure() is IHarvestable hydroponicTray)
		{
			HydroponicTray = hydroponicTray;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (GameManager.RunSimulation && OnOff && Powered)
		{
			CheckForTray();
			SetFace();
		}
	}

	protected override void OnServerImportTick()
	{
		if (GameManager.RunSimulation && OnOff && Powered)
		{
			TryChuteImport();
		}
	}

	protected override void OnServerExportTick()
	{
	}

	private void SetFace()
	{
		if (Plant != null && OnOff && Powered)
		{
			if (Plant.IsDead)
			{
				OnServer.Interact(base.InteractMode, 3);
			}
			else if (!Plant.PlantStatus.CanHeal(Plant) || Plant.PlantStatus.GetCurrentState(PlantStatusType.UnDesiredGas) || Plant.PlantStatus.GetCurrentState(PlantStatusType.Dehydrated))
			{
				OnServer.Interact(base.InteractMode, 2);
			}
			else
			{
				OnServer.Interact(base.InteractMode, 1);
			}
		}
		else
		{
			OnServer.Interact(base.InteractMode, 0);
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
			if ((bool)Plant)
			{
				if (Plant.IsMature)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Harvest + " " + Plant.DisplayName;
				}
				else if (Plant.IsSeeding)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Harvest + " " + Plant.DisplayName + " " + Localization.GetInterface("Seedsstring");
				}
				else
				{
					delayedActionInstance.ActionMessage = ActionStrings.Clear + " " + Plant.DisplayName;
				}
				if (CurrentState != ArmControl.Idle)
				{
					return delayedActionInstance.Fail(GameStrings.HarvesterBusyDoing, StringManager.Get(CurrentState));
				}
				if (IsLocked)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceLocked);
				}
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (doAction)
				{
					TryHarvestPlant();
				}
				return delayedActionInstance.Succeed();
			}
			delayedActionInstance.ActionMessage = ActionStrings.Plant;
			if (CurrentState != ArmControl.Idle)
			{
				return delayedActionInstance.Fail(GameStrings.HarvesterBusyDoing, StringManager.Get(CurrentState));
			}
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!Powered)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if ((bool)ImportPlant)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Plant + " " + ImportPlant.DisplayName;
			}
			if (doAction)
			{
				TryPlantSeed();
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public void FixAnimations()
	{
		if (Activate == 0 && IsTray && GetRobotHandSlot.Occupant != null)
		{
			if (Plant != null)
			{
				OnServer.Interact(base.InteractActivate, 2);
			}
			else
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
		}
	}

	private async UniTaskVoid WaitTryHarvestPlant()
	{
		if (!_isWaitingToTryHarvest)
		{
			_isWaitingToTryHarvest = true;
			await UniTask.SwitchToMainThread();
			TryHarvestPlant();
			_isWaitingToTryHarvest = false;
		}
	}

	private bool TryHarvestPlant()
	{
		if (!GameManager.RunSimulation)
		{
			return false;
		}
		if (!Powered || !OnOff)
		{
			return false;
		}
		if (GameManager.IsThread)
		{
			WaitTryHarvestPlant().Forget();
			return true;
		}
		if (CurrentState == ArmControl.Idle || (!_isPlanting && !_isHarvesting))
		{
			OnServer.Interact(base.InteractActivate, 2);
			return true;
		}
		return false;
	}

	private async UniTaskVoid WaitTryPlantSeed()
	{
		if (!_isWaitingToTryPlant)
		{
			_isWaitingToTryPlant = true;
			await UniTask.SwitchToMainThread();
			TryPlantSeed();
			_isWaitingToTryPlant = false;
		}
	}

	private bool TryPlantSeed()
	{
		if (!GameManager.RunSimulation)
		{
			return false;
		}
		if (!Powered || !OnOff)
		{
			return false;
		}
		if (GameManager.IsThread)
		{
			WaitTryPlantSeed().Forget();
			return true;
		}
		if (CurrentState == ArmControl.Idle || (!_isHarvesting && !_isPlanting))
		{
			GeneCollection genes = ((GameManager.RunSimulation && ImportPlant != null) ? GeneCollection.Copy(ImportPlant.Genes) : null);
			if (!ImportPlant || string.IsNullOrEmpty(ImportPlant.CustomName))
			{
				_ = string.Empty;
			}
			else
			{
				_ = ImportPlant.CustomName;
			}
			Plant importPlant = ImportPlant;
			if ((object)importPlant != null && importPlant.OnUseItem(1f, ImportPlant))
			{
				Plant plant = ((!(ImportPlant is Seed seed)) ? OnServer.Create<Plant>(ImportPlant.SourcePrefab, GetRobotHandSlot) : OnServer.Create<Plant>(seed.PlantType, GetRobotHandSlot));
				if ((bool)plant)
				{
					plant.ApplySeedTraits(genes);
				}
			}
			OnServer.Interact(base.InteractActivate, 1);
			return true;
		}
		return false;
	}

	public override bool TryChuteImport()
	{
		base.TryChuteImport();
		return false;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation && newChild.ParentSlot == ExportSlot)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	private Structure GetStructure(Grid3 gridPos)
	{
		Grid3 grid = base.GridController.WorldToLocalGrid(base.Position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		SmallCell smallCell = base.GridController.GetSmallCell(grid + gridPos);
		if (smallCell == null)
		{
			return null;
		}
		if ((bool)smallCell.Pipe)
		{
			return smallCell.Pipe;
		}
		if ((bool)smallCell.Device)
		{
			return smallCell.Device;
		}
		return null;
	}

	private Structure GetBelowStructure()
	{
		return GetStructure(new Grid3(0f, -(_smallGridSize * _nSmallGridsBelow), 0f));
	}

	private CanConstructInfo IsPlaceable()
	{
		Structure belowStructure = GetBelowStructure();
		if (!belowStructure || !(belowStructure is IHarvestable))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresHydroponicsTray.DisplayString);
		}
		return CanConstructInfo.ValidPlacement;
	}

	public override CanConstructInfo CanConstruct()
	{
		CanConstructInfo result = IsPlaceable();
		if (!result.CanConstruct)
		{
			return result;
		}
		return base.CanConstruct();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		CheckForTray(force: true);
	}

	public void OnArmHarvestPlant()
	{
		if (GameManager.RunSimulation && (bool)Plant)
		{
			if (Plant.IsMature || Plant.IsSeeding)
			{
				Plant.Harvest(this, GetRobotHandSlot, Plant.IsSeeding && Plant.SeedQuantity > 0);
			}
			else if (GameManager.RunSimulation)
			{
				OnServer.Destroy(Plant);
			}
		}
	}

	public void OnHandReturned()
	{
		if (GameManager.RunSimulation && (bool)GetRobotHandSlot.Occupant)
		{
			OnServer.MoveToSlot(GetRobotHandSlot.Occupant, ExportSlot);
		}
	}

	public void OnStartHarvesting()
	{
		_isHarvesting = true;
		if (GameManager.RunSimulation && (bool)GetRobotHandSlot.Occupant)
		{
			OnServer.MoveToWorld(GetRobotHandSlot.Occupant);
		}
	}

	public void OnEndHarvesting()
	{
		_isHarvesting = false;
		_isPlanting = false;
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractActivate, 0);
			if ((bool)GetRobotHandSlot.Occupant)
			{
				OnServer.MoveToWorld(GetRobotHandSlot.Occupant);
			}
		}
	}

	public void OnArmGatherPlant()
	{
	}

	public void OnArmPlant()
	{
		if (GameManager.RunSimulation && GetRobotHandSlot.Occupant is Plant plant)
		{
			plant.SetQuantity(1);
			if (!IsTray || HydroponicTray.IsBeingDestroyed)
			{
				OnServer.MoveToWorld(plant);
				return;
			}
			plant.PlanterName = (HydroponicTray as IGrower)?.CustomName;
			OnServer.MoveToSlot(plant, HydroponicTray.InputSlot);
		}
	}

	public void OnStartPlanting()
	{
		_isPlanting = true;
		_ = GameManager.RunSimulation;
	}

	public void OnEndPlanting()
	{
		_isPlanting = false;
		_isHarvesting = false;
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractActivate, 0);
			if ((bool)GetRobotHandSlot.Occupant)
			{
				OnServer.MoveToWorld(GetRobotHandSlot.Occupant);
			}
		}
	}
}
