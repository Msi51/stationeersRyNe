using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DynamicComposter : DraggableThing, IUnfastenable, IReferencable, IEvaluable
{
	[Header("Portable Generator")]
	[ReadOnly]
	[Tooltip("PowerConnector that I am connected too")]
	public PowerConnector PowerConnector;

	[ReadOnly]
	[Tooltip("Gas Canister that is currently inserted")]
	public GasCanister GasCanister;

	[ReadOnly]
	[Tooltip("BatteryCell that is currently inserted")]
	public BatteryCell BatteryCell;

	[ReadOnly]
	[Tooltip("Set to true when the generator is connected to the power connector")]
	public bool Connected;

	public Collider InfoPanel;

	public static readonly MoleQuantity MolesDrainedPerProcessedItem = new MoleQuantity(20.0);

	public float PowerUseMult = 1f;

	public static readonly float HeatTransferJoulesPerProcessedItem = 100000f;

	public DynamicThing ImportingThing;

	private int _unprocessedAmount;

	public Fertiliser Fertiliser;

	public List<SpawnGas> ExspelledGas;

	private float _powerUsedDuringTick;

	private float _powerGenerated;

	private int LastFertilizerProcessedSeconds;

	private static readonly Vector3 ExportSoundPosition = new Vector3(0f, 0.5f, -0.2f);

	private static readonly int ComposterExportHash = Animator.StringToHash("ComposterExport");

	private bool _isGrinding;

	public int DecayFoodQuantity;

	public int NormalFoodQuantity;

	public int BiomassQuantity;

	private int _itemsLeftToProcess = 3;

	private bool _isProcessing;

	public static readonly int ProcessTimeSeconds = 120;

	private int TotalFertilizerProcessedSeconds;

	public Slot ImportSlot => Slots[0];

	public Slot ExportSlot => Slots[1];

	[ByteArraySync]
	public int UnprocessedAmount
	{
		get
		{
			return _unprocessedAmount;
		}
		set
		{
			_unprocessedAmount = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public float PowerGenerated
	{
		get
		{
			if (!OnOff || !Powered)
			{
				return 0f;
			}
			return _powerGenerated;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		base.ActivateButton.MaterialChanger.ChangeState((Activate == 1 && OnOff && Powered) ? Defines.Animator.OnPowered : Defines.Animator.Off);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(UnprocessedAmount);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			UnprocessedAmount = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(UnprocessedAmount);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		UnprocessedAmount = reader.ReadInt32();
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.RunSimulation)
		{
			return;
		}
		ImportingThing = ImportSlot.Occupant;
		UsePower();
		if (OnOff && Powered)
		{
			TryProcessImport();
			if (_isProcessing)
			{
				DoProcessing();
			}
		}
	}

	private void DoProcessing()
	{
		int totalFertilizerProcessedSeconds = TotalFertilizerProcessedSeconds;
		float num = (float)(totalFertilizerProcessedSeconds - LastFertilizerProcessedSeconds) / (float)ProcessTimeSeconds;
		LastFertilizerProcessedSeconds = totalFertilizerProcessedSeconds;
		if (GasCanister != null && GasCanister.InternalAtmosphere.GasMixture.Water.Quantity > MoleQuantity.Zero)
		{
			MoleQuantity quantity = MolesDrainedPerProcessedItem * num;
			MoleEnergy energy = IdealGas.Energy(GasCanister.InternalAtmosphere.Temperature, Mole.SpecificHeat(Chemistry.GasType.Water), quantity);
			AtmosphericEventInstance.CreateRemove(GasCanister.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.Water, quantity, energy)));
			_powerUsedDuringTick += MolesDrainedPerProcessedItem.ToFloat() * num * PowerUseMult;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		foreach (SpawnGas exspelledGa in ExspelledGas)
		{
			MoleQuantity quantity2 = new MoleQuantity(exspelledGa.Quantity * num);
			gasMixture.Add(new Mole(exspelledGa.Type, new MoleQuantity(exspelledGa.Quantity * num), IdealGas.Energy(new TemperatureKelvin(exspelledGa.Kelvin), Mole.SpecificHeat(exspelledGa.Type), quantity2)));
		}
		_powerUsedDuringTick += HeatTransferJoulesPerProcessedItem * num;
		AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, gasMixture);
		if (Connected)
		{
			_powerGenerated = _powerUsedDuringTick;
		}
	}

	public void UsePower()
	{
		if ((bool)BatteryCell && OnOff)
		{
			if (!Powered && !BatteryCell.IsEmpty)
			{
				OnServer.Interact(base.InteractPowered, 1);
			}
			else if (Powered && BatteryCell.IsEmpty)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
		}
		else if (Powered || !OnOff)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
		if ((_isGrinding || _isProcessing) && (bool)BatteryCell)
		{
			BatteryCell.PowerStored -= PowerUseMult;
		}
	}

	private void ExportStoredThing()
	{
		int num = DecayFoodQuantity + NormalFoodQuantity + BiomassQuantity;
		int num2 = DecayFoodQuantity / num;
		int num3 = NormalFoodQuantity / num;
		int num4 = BiomassQuantity / num;
		RemoveQuantity(ref DecayFoodQuantity, num2);
		RemoveQuantity(ref NormalFoodQuantity, num3);
		RemoveQuantity(ref BiomassQuantity, num4);
		if (OnServer.CreateOld(Fertiliser, base.Position, Rotation, 0uL) is Fertiliser fertiliser)
		{
			fertiliser.GrowthSpeed += (float)num2 * 0.25f;
			fertiliser.HarvestBoost += (float)num3 * 1.5f;
			fertiliser.Cycles += num4 * 5;
			PlayNetworkSound(ComposterExportHash);
			if (ExportSlot?.Occupant is Fertiliser fertiliser2)
			{
				fertiliser2.Merge(fertiliser);
			}
			OnServer.MoveToSlot(fertiliser, ExportSlot);
		}
	}

	public void RemoveQuantity(ref int quantity, float ratio)
	{
		if ((double)ratio > 0.5)
		{
			quantity -= 2;
		}
		else
		{
			quantity--;
		}
		quantity = Math.Max(0, quantity);
	}

	private void TryProcessImport()
	{
		if (ImportingThing == null || _isGrinding)
		{
			return;
		}
		DynamicThing importingThing = ImportingThing;
		int type;
		if (!(importingThing is DecayedFood))
		{
			if (!(importingThing is INutrition))
			{
				if (!(importingThing is OrganicMaterial) && !(importingThing is Hay))
				{
					return;
				}
				type = 2;
			}
			else
			{
				type = 1;
			}
		}
		else
		{
			type = 0;
		}
		_isGrinding = true;
		OnServer.Interact(base.InteractMode, 1);
		DoProcessingBlades(type).Forget();
	}

	private async UniTaskVoid DoProcessingBlades(int type)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		bool conditionsNotMeet = false;
		DynamicThing importingThing = ImportingThing;
		if (importingThing is Stackable { Quantity: var stackSize } stackable)
		{
			for (int i = 0; i < stackSize; i++)
			{
				await UniTask.Delay(5000, ignoreTimeScale: false, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
				if (!Powered || !OnOff || ImportingThing == null || BatteryCell == null)
				{
					conditionsNotMeet = true;
					break;
				}
				stackable.OnUseItem(1f, stackable);
				AddRatio(type);
				UnprocessedAmount++;
			}
		}
		else
		{
			await UniTask.Delay(5000, ignoreTimeScale: false, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
			AddRatio(type);
			UnprocessedAmount++;
		}
		if (!conditionsNotMeet && ImportingThing != null && !ImportingThing.IsBeingDestroyed)
		{
			OnServer.Destroy(ImportingThing);
		}
		_isGrinding = false;
		OnServer.Interact(base.InteractMode, 0);
	}

	public void AddRatio(int type)
	{
		switch (type)
		{
		case 0:
			DecayFoodQuantity++;
			break;
		case 1:
			NormalFoodQuantity++;
			break;
		case 2:
			BiomassQuantity++;
			break;
		}
	}

	private async UniTaskVoid DoFertilizationProcessing()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		bool runOutOFWater = false;
		while (UnprocessedAmount >= _itemsLeftToProcess && base.InteractActivate.State == 1 && base.InteractOnOff.State == 1 && base.InteractPowered.State == 1 && !runOutOFWater)
		{
			for (int i = 0; i < ProcessTimeSeconds; i++)
			{
				if (base.InteractPowered.State == 0)
				{
					break;
				}
				if (base.InteractOnOff.State == 0)
				{
					break;
				}
				if (GasCanister == null)
				{
					break;
				}
				if (BatteryCell == null)
				{
					break;
				}
				if (GasCanister.InternalAtmosphere.GasMixture.Water.Quantity <= MoleQuantity.Zero)
				{
					runOutOFWater = true;
					break;
				}
				TotalFertilizerProcessedSeconds++;
				await UniTask.Delay(1000, ignoreTimeScale: false, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
			}
			if (GasCanister == null || BatteryCell == null)
			{
				break;
			}
			if (base.InteractPowered.State == 1 && base.InteractOnOff.State == 1 && !runOutOFWater)
			{
				UnprocessedAmount -= _itemsLeftToProcess;
				ExportStoredThing();
			}
		}
		OnServer.Interact(base.InteractActivate, 0);
		_isProcessing = false;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		GasCanister gasCanister = newChild as GasCanister;
		if (gasCanister != null)
		{
			GasCanister = gasCanister;
			if (base.InteractActivate.State == 1 && !_isProcessing && UnprocessedAmount >= _itemsLeftToProcess)
			{
				_isProcessing = true;
				DoFertilizationProcessing().Forget();
			}
		}
		BatteryCell batteryCell = newChild as BatteryCell;
		if (batteryCell != null)
		{
			BatteryCell = batteryCell;
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		PowerConnector powerConnector = parent as PowerConnector;
		if (powerConnector != null)
		{
			PowerConnector = powerConnector;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: true);
			}
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		if (PowerConnector == parent)
		{
			PowerConnector = null;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: false);
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GasCanister == previousChild)
		{
			GasCanister = null;
		}
		if (BatteryCell == previousChild)
		{
			BatteryCell = null;
		}
	}

	public override bool MoveToWorld(float force = 0f)
	{
		bool result = base.MoveToWorld(force);
		if (GameManager.RunSimulation && base.Room != null)
		{
			RigidBody.AddForce(Vector3.up * 3f);
			RigidBody.AddTorque(UnityEngine.Random.insideUnitCircle);
		}
		return result;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				ActionMessage = interactable.ContextualName
			};
			if (UnprocessedAmount < _itemsLeftToProcess)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.ComposterNotEnoughProcessItems);
				delayedActionInstance.AppendStateMessage(GameStrings.ComposterItemsLeftToProcess, StringManager.Get(UnprocessedAmount));
				return delayedActionInstance.Fail();
			}
			if (!GasCanister || GasCanister.InternalAtmosphere.GasMixture.Water.Quantity < MolesDrainedPerProcessedItem)
			{
				return delayedActionInstance.Fail(GameStrings.ComposterNotEnoughWater);
			}
			if (!doAction)
			{
				delayedActionInstance.AppendStateMessage((Activate == 1) ? GameStrings.ComposterCurrentlyProcessing : GameStrings.ComposterCurrentlyNotProcessing);
				delayedActionInstance.AppendStateMessage(GameStrings.ComposterItemsLeftToProcess, StringManager.Get(UnprocessedAmount));
				return delayedActionInstance.Succeed();
			}
			if (!GameManager.RunSimulation)
			{
				return base.InteractWith(interactable, interaction, doAction);
			}
			if (base.InteractActivate.State == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
				if (!_isProcessing && UnprocessedAmount >= _itemsLeftToProcess)
				{
					_isProcessing = true;
					DoFertilizationProcessing().Forget();
				}
			}
			else
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = Localization.GetInterface("TankPressure");
			result.State = (GasCanister ? string.Format("{0}", GasCanister.InternalAtmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", "yellow")) : "Empty");
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (base.Joint != null || !(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		PowerConnector powerConnector = SmallCell.Get<PowerConnector>(CenterPosition);
		if ((object)powerConnector == null)
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = (base.IsChild ? ActionStrings.Disconnect : ActionStrings.Connect)
		};
		if (!doAction)
		{
			return result;
		}
		if (GameManager.RunSimulation)
		{
			if (base.IsChild)
			{
				OnServer.MoveToWorld(this);
			}
			else
			{
				OnServer.MoveToSlot(this, powerConnector.ConnectedSlot);
			}
		}
		return result;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DynamicComposterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DynamicComposterSaveData dynamicComposterSaveData)
		{
			UnprocessedAmount = dynamicComposterSaveData.UnprocessedItems;
			BiomassQuantity = dynamicComposterSaveData.SavedBiomassQuantity;
			DecayFoodQuantity = dynamicComposterSaveData.SavedDecayFoodQuantity;
			NormalFoodQuantity = dynamicComposterSaveData.SavedNormalFoodQuantity;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DynamicComposterSaveData dynamicComposterSaveData)
		{
			dynamicComposterSaveData.UnprocessedItems = UnprocessedAmount;
			dynamicComposterSaveData.SavedBiomassQuantity = BiomassQuantity;
			dynamicComposterSaveData.SavedDecayFoodQuantity = DecayFoodQuantity;
			dynamicComposterSaveData.SavedNormalFoodQuantity = NormalFoodQuantity;
		}
	}
}
