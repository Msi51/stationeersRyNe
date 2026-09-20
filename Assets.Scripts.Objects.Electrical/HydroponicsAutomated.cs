using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class HydroponicsAutomated : DeviceInputOutputImportExport, IGrower, IReferencable, IEvaluable, ISmartRotatable, IPrefabHash, IThermal, ILightActivated, IDensePoolable
{
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume;

	public Collider InfoBox;

	private static readonly int HarvestButtonHash = Animator.StringToHash("HarvestButton");

	private static readonly int AutomatedHydroponicsHarvestHash = Animator.StringToHash("AutomatedHydroponicsHarvest");

	private static readonly int AutomatedHydroponicsPlantHash = Animator.StringToHash("AutomatedHydroponicsPlant");

	private static int _canPlant = Animator.StringToHash("CanPlant");

	private static int _canHarvest = Animator.StringToHash("CanHarvest");

	private static readonly Vector3 HarvestPosition = new Vector3(0.25f, 0.4f, -0.3f);

	private string _newLine = "\n";

	public virtual int CurrentHash
	{
		get
		{
			if (!(Plant != null))
			{
				return 0;
			}
			return Plant.PrefabHash;
		}
		set
		{
		}
	}

	public List<GrowLight> LinkedGrowLights { get; } = new List<GrowLight>();

	public virtual bool IsLitByGrowLight
	{
		get
		{
			foreach (GrowLight linkedGrowLight in LinkedGrowLights)
			{
				if (!(linkedGrowLight == null) && linkedGrowLight.OnOff && linkedGrowLight.Powered)
				{
					return true;
				}
			}
			return false;
		}
	}

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override bool HasReadableAtmosphere => true;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsInput2Valid;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
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

	public override Slot ImportSlot => Slots[2];

	public override Slot ExportSlot => Slots[1];

	public Slot PlantedSlot => Slots[0];

	public Slot FertilizerSlot => Slots[3];

	public Atmosphere BreathingAtmosphere => base.InternalAtmosphere;

	public float CurrentLightExposure
	{
		get
		{
			float num = 0f;
			if (Powered)
			{
				num += 0.8f;
			}
			if (HasLight)
			{
				num += OrbitalSimulation.EarthSolarRatio;
			}
			return num;
		}
	}

	public Plant Plant => PlantedSlot.Occupant as Plant;

	public Fertiliser Fertiliser => FertilizerSlot.Occupant as Fertiliser;

	public Atmosphere WaterAtmosphere => InputNetwork2?.Atmosphere;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.Running && GameManager.RunSimulation)
		{
			GameManager.OnGameStartedOnce += delegate
			{
				if (Activate == 1 || Activate == 2)
				{
					OnResetArm();
				}
			};
		}
		if (GameManager.GameState != GameState.None && !(Volume <= VolumeLitres.Zero))
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (InputNetwork != null && InputNetwork.Atmosphere != null)
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, InputNetwork.Atmosphere, AtmosphereHelper.MatterState.Gas);
		}
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid;
		bool flag2 = !base.IsInput2Valid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		switch (logicSlotType)
		{
		case LogicSlotType.Efficiency:
		case LogicSlotType.Health:
		case LogicSlotType.Growth:
		case LogicSlotType.Mature:
		case LogicSlotType.Seeding:
		case LogicSlotType.MaturityRatio:
		case LogicSlotType.SeedingRatio:
			return slotId == PlantedSlot.SlotIndex;
		default:
			return base.CanLogicRead(logicSlotType, slotId);
		}
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (slotId != PlantedSlot.SlotIndex)
		{
			return base.GetLogicValue(logicSlotType, slotId);
		}
		switch (logicSlotType)
		{
		case LogicSlotType.Growth:
			return Plant ? ((float)Plant.Stage) : (-1f);
		case LogicSlotType.Health:
			return Plant ? Plant.DamageState.TotalRatioClampedUndamaged : (-1f);
		case LogicSlotType.Efficiency:
			return Plant ? Plant.lifeRequirements.GrowthEfficiency() : (-1f);
		case LogicSlotType.Mature:
			return (!Plant) ? (-1f) : (Plant.IsMature ? 1f : 0f);
		case LogicSlotType.Seeding:
			return (!Plant) ? (-1f) : (Plant.IsSeeding ? 1f : 0f);
		case LogicSlotType.MaturityRatio:
			if (!Plant)
			{
				return -1.0;
			}
			return Plant.MaturityRatio;
		case LogicSlotType.SeedingRatio:
			if (!Plant)
			{
				return -1.0;
			}
			return Plant.SeedingRatio;
		default:
			return base.GetLogicValue(logicSlotType, slotId);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Activate:
			return false;
		case LogicType.Plant:
		case LogicType.Harvest:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Plant:
			PlantAction(0uL);
			break;
		case LogicType.Harvest:
			if (value <= 0.0)
			{
				return;
			}
			if (Plant != null && Exporting == 0 && ExportingThing == null && Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 2);
			}
			break;
		}
		base.SetLogicValue(logicType, value);
	}

	public void PlantAction(ulong steamId = 0uL)
	{
		if (GameManager.IsThread)
		{
			PlantActionFromThread(steamId).Forget();
		}
		else
		{
			DelayedPlantAction(steamId).Forget();
		}
	}

	public async UniTaskVoid PlantActionFromThread(ulong steamId = 0uL)
	{
		await UniTask.SwitchToMainThread();
		PlantAction(steamId);
	}

	public async UniTaskVoid DelayedPlantAction(ulong steamId = 0uL)
	{
		if (Activate > 0)
		{
			return;
		}
		await UniTask.NextFrame();
		if (ImportingThing is Plant plant)
		{
			GeneCollection genes = ((GameManager.RunSimulation && plant != null) ? GeneCollection.Copy(plant.Genes) : null);
			if (!plant || string.IsNullOrEmpty(plant.CustomName))
			{
				_ = string.Empty;
			}
			else
			{
				_ = plant.CustomName;
			}
			if (plant.Quantity >= 1 && plant.OnUseItem(1f, this))
			{
				((!(plant is Seed seed)) ? OnServer.Create<Plant>(plant.SourcePrefab, PlantedSlot) : OnServer.Create<Plant>(seed.PlantType, PlantedSlot))?.ApplySeedTraits(genes);
				OnServer.Interact(base.InteractActivate, 1);
			}
			else
			{
				OnServer.MoveToSlot(plant, PlantedSlot);
			}
		}
		else if (ImportingThing != null)
		{
			OnServer.MoveToSlot(ImportingThing, ExportSlot);
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant is Plant plant)
			{
				plant.PlanterName = CustomName;
			}
		}
	}

	public void RenderButtonState()
	{
		BaseAnimator.SetBool(_canPlant, Activate == 0 && Plant == null && (bool)ImportingThing);
		BaseAnimator.SetBool(_canHarvest, Activate == 0 && Plant != null && ExportingThing == null && Exporting == 0);
	}

	public void OnHarvested()
	{
		if (!GameManager.IsBatchMode && (bool)Plant)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(this, AutomatedHydroponicsHarvestHash, HarvestPosition);
		}
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			_ = (bool)Plant;
		}
	}

	public void OnPlanted()
	{
		if ((bool)Plant)
		{
			if (!GameManager.IsBatchMode)
			{
				Singleton<AudioManager>.Instance.PlayAudioClipsData(this, AutomatedHydroponicsPlantHash, HarvestPosition);
			}
			Plant.Planted(this);
		}
	}

	public void OnResetArm()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (OnOff && Powered)
		{
			TryCollect();
			if (CanBeginImport)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if (CanCompleteImport && ImportingThing == null)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	protected override void OnServerExportTick(float deltaTime)
	{
		if (OnOff && Powered && CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (OnOff && Powered && Plant != null && Activate == 0 && Plant.ParentTray == null)
		{
			Plant.Planted(this);
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
		case InteractableType.Button1:
			delayedActionInstance.ActionMessage = ActionStrings.Plant;
			if (doAction)
			{
				PlaySound(HarvestButtonHash);
			}
			if (!ImportingThing || (bool)Plant || Activate > 0)
			{
				return delayedActionInstance.Fail();
			}
			if (!doAction || !GameManager.RunSimulation)
			{
				return delayedActionInstance.Succeed();
			}
			PlantAction(interaction.SourceThing.OwnerClientId);
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance.ActionMessage = ((Plant == null || !Plant.IsMature || !Plant.IsSeeding) ? ActionStrings.Clear : ActionStrings.Harvest);
			if (doAction)
			{
				PlaySound(HarvestButtonHash);
			}
			if (Plant == null || Exporting > 0 || Activate > 0)
			{
				return delayedActionInstance.Fail();
			}
			if (!doAction || !GameManager.RunSimulation)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(base.InteractActivate, 2);
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Slot1:
			return HydroponicsUtils.HandlePlantInteraction(this, interactable, interaction, doAction);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RenderButtonState();
		if (GameManager.RunSimulation && newChild.ParentSlot == ExportSlot)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
		else if (GameManager.GameState != GameState.Running && (bool)Plant && newChild == Plant)
		{
			Plant.Planted(this);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		RenderButtonState();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		RenderButtonState();
	}

	public override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		RenderButtonState();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		if (InputConnection != null && InputConnection.Collider != null && hitCollider == InputConnection.Collider)
		{
			result.Title = Localization.GetInterface("WorldAtmosphere");
			return result;
		}
		if (InputConnection2 != null && InputConnection2.Collider != null && hitCollider == InputConnection2.Collider)
		{
			result.Title = Localization.GetInterface("Liquid");
			return result;
		}
		if (InfoBox != null && hitCollider == InfoBox)
		{
			if (!Plant)
			{
				result.Title = Localization.GetInterface("Contents");
				result.State = Localization.GetInterface("Empty");
				return result;
			}
			Tooltip.ToolTipStringBuilder.Clear();
			Tooltip.ToolTipStringBuilder.Append(string.Format(Localization.GetInterface("Stage1Contains"), Plant.ToTooltip(), Plant.Stage, "<color=yellow>", "</color>"));
			result.Title = DisplayName;
			result.State = Tooltip.ToolTipStringBuilder.ToString();
			result.Title = Localization.GetInterface("Contents");
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None && Volume > VolumeLitres.Zero)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public PlantToFertiliserSlotMapping PlantToFertiliserSlotMapping(InteractableType interactableType)
	{
		if (interactableType == InteractableType.Slot1)
		{
			return new PlantToFertiliserSlotMapping(GetSlot(InteractableType.Slot1), GetSlot(InteractableType.Slot4));
		}
		throw new Exception("No slot mapping found");
	}

	SmartRotate.ConnectionType ISmartRotatable.GetConnectionType()
	{
		return GetConnectionType();
	}

	void ISmartRotatable.SetOpenEndsPermutation(int[] permutation)
	{
		SetOpenEndsPermutation(permutation);
	}

	int[] ISmartRotatable.GetOpenEndsPermutation()
	{
		return GetOpenEndsPermutation();
	}

	void ISmartRotatable.SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		SetConnectionType(connectionType);
	}
}
