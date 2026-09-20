using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Reagents;
using Trading;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Pipes;

public class ArcFurnace : DeviceImportExport, IResourceConsumer, IReferencable, IEvaluable
{
	public static IQuantityRecipeComparable RecipeComparable = new IQuantityRecipeComparable("ArcFurnace");

	public Ore SlagPrefab;

	private UniTask _smeltingTask;

	[SerializeField]
	private MeshRenderer _glow;

	private Color _emissionA = new Color(2f, 0.86897f, 0f);

	private Color _emissionB = new Color(2.066f, 0.62187f, 0f);

	private float _startupCurrent;

	private Recipe _currentRecipe;

	private ReagentMixture _ratioMix;

	private IQuantity _smelterResult;

	public static float PowerReleaseScale = 20f;

	private float _powerUsedDuringTick;

	private readonly CancellationTokenWrapper _cancellation = new CancellationTokenWrapper();

	private static readonly int EmissionColor1 = Shader.PropertyToID("_EmissionColor");

	public bool IsIdle
	{
		get
		{
			if (Activate == 0 && ImportingThing != null)
			{
				return IsNextExportReady;
			}
			return false;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return Activate == 1;
			}
			return false;
		}
	}

	public override bool CanCompleteImport
	{
		get
		{
			if (base.CanCompleteImport)
			{
				return _smeltingTask.Status != UniTaskStatus.Pending;
			}
			return false;
		}
	}

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override bool HasReadableReagentMixture => true;

	public override bool CanBeginExport
	{
		get
		{
			if (base.CanBeginExport)
			{
				return Activate == 0;
			}
			return false;
		}
	}

	public int CurrentStackSize
	{
		get
		{
			if (!(ImportSlot.Occupant is Stackable stackable))
			{
				return 0;
			}
			return stackable.Quantity;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override void UpdateEachFrame()
	{
		PulseGlowMaterial();
	}

	private void PulseGlowMaterial()
	{
		if (Activate != 0 || _startupCurrent != 0f)
		{
			float t = Mathf.PingPong(Time.time * 2f, 1f);
			_startupCurrent = Mathf.MoveTowards(_startupCurrent, Activate, Time.deltaTime * 0.5f);
			Color value = Color.Lerp(_emissionA, _emissionB, t) * _startupCurrent;
			_glow.material.SetColor(EmissionColor1, value);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Idle => true, 
			LogicType.RecipeHash => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Idle => (!base.IsDeviceActive) ? 1 : 0, 
			LogicType.RecipeHash => _smelterResult?.GetPrefabHash() ?? 0, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
	}

	private IQuantity GetSmelterResult(ReagentMixture reagentMix)
	{
		if (reagentMix.TotalReagents <= 0.0)
		{
			return null;
		}
		_ratioMix = reagentMix.GetRatioMixture();
		Recipe recipe = new Recipe(_ratioMix, 5273.15f, 101.325f);
		RecipeComparable.Recipes.TryGetValue(recipe, out var value);
		if (value != null)
		{
			foreach (Recipe key in RecipeComparable.Recipes.Keys)
			{
				if (key.Equals(recipe))
				{
					_currentRecipe = key;
					break;
				}
			}
		}
		else
		{
			_currentRecipe = default(Recipe);
		}
		return value;
	}

	public Item CreateOutput(IQuantity orePrefab, int quantity)
	{
		Item item = Thing.Create<Item>((Item)orePrefab, base.ExportConnection.Transform.position, base.ExportConnection.Transform.rotation, 0L);
		if (item is IQuantity quantity2)
		{
			quantity2.SetQuantity(quantity);
		}
		return item;
	}

	private bool DropIngots()
	{
		IQuantity smelterResult = GetSmelterResult(ReagentMixture);
		if (smelterResult != null)
		{
			int num = (int)Mathf.Clamp((int)(ReagentMixture.TotalReagents / _ratioMix.TotalReagents), 0f, smelterResult.GetMaxQuantity);
			Item childThing = CreateOutput(smelterResult, num);
			ReagentMixture.Subtract(_ratioMix * num);
			OnServer.MoveToSlot(childThing, ExportSlot);
			return true;
		}
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			return false;
		}
		_ratioMix = ReagentMixture.GetRatioMixture();
		ReagentMixture reagentMixture = new ReagentMixture(_ratioMix) * Math.Min(ReagentMixture.TotalReagents, SlagPrefab.MaxQuantity);
		ReagentMixture.Subtract(reagentMixture);
		OnServer.MoveToSlot(Ore.CreateOreType(SlagPrefab, base.ExportConnection, _ratioMix, (int)Math.Round(reagentMixture.TotalReagents)), ExportSlot);
		if (ReagentMixture.TotalReagents < 1.0)
		{
			ReagentMixture.Clear();
		}
		return true;
	}

	protected override void OnServerImportTick()
	{
		if (base.IsStructureCompleted)
		{
			if (ReagentMixture.TotalReagents > 0.0 && ReagentMixture.TotalReagents < 0.1)
			{
				ReagentMixture.Clear();
			}
			if (ExportingThing == null && IsNextImportReady)
			{
				TryChuteImport();
			}
			if (IsNextExportReady && Activate == 0)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
			if (!OnOff || (!Powered && _smeltingTask.Status == UniTaskStatus.Pending))
			{
				_cancellation.Cancel();
				OnServer.Interact(base.InteractActivate, 0);
			}
			else if (OnOff && Powered && !base.IsImportOpening && _smeltingTask.Status != UniTaskStatus.Pending)
			{
				_cancellation.CancelAndInitialize();
				_smeltingTask = WaitThenSmelt(_cancellation.Token);
			}
			if (IsNextExportReady && ReagentMixture.TotalReagents > 0.0 && Activate == 0)
			{
				DropIngots();
			}
		}
	}

	protected override void OnServerExportTick()
	{
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
		base.OnServerExportTick();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation && newChild is Item item && item.ParentSlot == ImportSlot)
		{
			_smelterResult = GetSmelterResult(item.CreatedReagentMixture);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GameManager.RunSimulation && Activate != 1)
		{
			_smelterResult = ((ImportSlot.Occupant is Item item) ? GetSmelterResult(item.CreatedReagentMixture) : null);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 0)
		{
			_cancellation.Cancel();
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
			if (Activate == 1)
			{
				if ((object)ImportSlot.Occupant != null)
				{
					int value = 0;
					if (ImportSlot.Occupant is Stackable stackable)
					{
						value = stackable.Quantity;
					}
					else if (ImportSlot.Occupant is Consumable consumable)
					{
						value = (int)consumable.Quantity;
					}
					delayedActionInstance.AppendStateMessage(GameStrings.FurnaceCurrentlySmelting, ImportSlot.Occupant.ToTooltip(), StringGenerator.GetString(value, Unit.g));
					if (doAction)
					{
						OnServer.Interact(base.InteractActivate, 0);
						return delayedActionInstance.Succeed();
					}
					return delayedActionInstance.Succeed();
				}
				if ((object)ExportSlot.Occupant != null)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceAlreadyExporting, ExportSlot.Occupant.ToTooltip());
				}
				return delayedActionInstance.Fail();
			}
			if ((object)ImportSlot.Occupant == null)
			{
				if ((object)ExportSlot.Occupant != null)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceAlreadyExporting, ExportSlot.Occupant.ToTooltip());
				}
				return delayedActionInstance.Fail(GameStrings.FurnaceNothingToSmelt);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(base.InteractActivate, 1);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return _powerUsedDuringTick;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (!base.RequiresFrame || HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	private async UniTask WaitThenSmelt(CancellationToken cancellationToken)
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		while (GameManager.GameState != GameState.Running && !cancellationToken.IsCancellationRequested)
		{
			await UniTask.NextFrame(cancellationToken);
		}
		while ((object)ImportingThing != null)
		{
			if ((base.PowerCableNetwork != null && base.PowerCableNetwork.EstimatedRemainingLoad < _currentRecipe.Energy) || !Powered)
			{
				OnServer.Interact(base.InteractOnOff, 0);
				break;
			}
			if (Activate == 0)
			{
				break;
			}
			if (base.IsImportOpen)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			await UniTask.Delay(Mathf.RoundToInt(_currentRecipe.Time * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			_powerUsedDuringTick += _currentRecipe.Energy;
			AtmosphericEventInstance.CloneGlobal(base.WorldGrid, new MoleEnergy(_currentRecipe.Energy * PowerReleaseScale), spark: true);
			if ((object)ImportingThing == null)
			{
				_cancellation.Cancel();
				break;
			}
			ImportingThing.Smelt(base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid), ReagentMixture);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
		}
		_smelterResult = null;
		OnServer.Interact(base.InteractActivate, 0);
	}

	public List<Item> GetResourcesUsed()
	{
		List<Ore> allOrePrefabs = Ore.AllOrePrefabs;
		List<Item> list = new List<Item>(allOrePrefabs.Count);
		foreach (Ore item2 in allOrePrefabs)
		{
			Item item = Prefab.Find<Item>(item2.GetPrefabHash());
			if (item == null || list.Contains(item))
			{
				continue;
			}
			foreach (KeyValuePair<Recipe, IQuantity> allRecipe in RecipeComparable.AllRecipes)
			{
				if (item.CreatedReagentMixture.ContainsSome(allRecipe.Key))
				{
					list.Add(item);
					break;
				}
			}
		}
		return list;
	}

	public virtual bool CanProcess(Recipe recipe)
	{
		foreach (Ore allOrePrefab in Ore.AllOrePrefabs)
		{
			if (allOrePrefab.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanProcess(Reagent reagentType)
	{
		foreach (Ore allOrePrefab in Ore.AllOrePrefabs)
		{
			if (allOrePrefab.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}
}
