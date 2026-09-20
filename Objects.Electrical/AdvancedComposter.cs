using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Electrical;

public class AdvancedComposter : DeviceInputOutputImportExport
{
	public static readonly MoleQuantity MolesDrainedPerProcessedItem = new MoleQuantity(20.0);

	private int _unprocessedAmount;

	public Collider InfoPanel;

	[FormerlySerializedAs("Fertiliser")]
	public Fertiliser FertiliserPrefab;

	[FormerlySerializedAs("ExspelledGas")]
	public List<SpawnGas> ExpelledGas;

	private float _powerUsedDuringTick;

	public const float PROGRESS_REQUIRED_SECONDS = 1.5f;

	private float _currentProgress;

	public int DecayFoodQuantity;

	public int NormalFoodQuantity;

	public int BiomassQuantity;

	private static readonly int _itemsToCreateFertilizer = 3;

	public const float PROCESS_TIME_SECONDS = 60f;

	private float _totalFertilizerProcessedSeconds;

	private static readonly int ActivateButtonHash = Animator.StringToHash("ActivateButton");

	private const float PROCESSING_POWER = 100f;

	[ByteArraySync]
	public int UnprocessedAmount
	{
		get
		{
			return _unprocessedAmount;
		}
		set
		{
			if (NetworkManager.IsServer && value != _unprocessedAmount)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_unprocessedAmount = value;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool isInputValid = base.IsInputValid;
			if (Error == 1)
			{
				if (!isInputValid)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (isInputValid)
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

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return ImportingThing is ICompostable;
			}
			return false;
		}
	}

	private bool IsProcessedReady
	{
		get
		{
			if (ImportingThing is ICompostable)
			{
				return _currentProgress >= 1.5f;
			}
			return false;
		}
	}

	private bool IsProcessedFinished => (object)ImportingThing == null;

	private bool CanProcess => ImportingThing is ICompostable;

	public bool CanDoProcessing
	{
		get
		{
			if (UnprocessedAmount >= _itemsToCreateFertilizer && IsOperable && Activate == 1 && OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	protected override void CheckConnections()
	{
		base.CheckConnections();
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
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

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void OnPowerTick()
	{
		_ = base.IsStructureCompleted;
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (!OnOff || !Powered)
		{
			OnServer.Interact(base.InteractImport, 0);
			_currentProgress = 0f;
			return;
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (base.IsImportClosed && IsProcessedReady)
		{
			CollectResource(ImportingThing as ICompostable);
			_currentProgress = 0f;
		}
		if (base.IsImportClosed && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
			_currentProgress = 0f;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		base.ActivateButton.MaterialChanger.ChangeState((Activate == 1 && OnOff && Powered) ? Defines.Animator.OnPowered : Defines.Animator.Off);
	}

	public void CollectResource(ICompostable input)
	{
		Stackable stackable = ImportingThing as Stackable;
		if (!(stackable != null) || (Powered && OnOff && !(ImportingThing == null) && stackable.OnUseItem(1f, stackable)))
		{
			AddRatio(input.CompostType);
			UnprocessedAmount++;
			if (!stackable)
			{
				UnityEngine.Object.Destroy(ImportingThing);
			}
		}
	}

	private void AddRatio(CompostType type)
	{
		switch (type)
		{
		case CompostType.GrowthSpeed:
			DecayFoodQuantity++;
			break;
		case CompostType.HarvestQuantity:
			NormalFoodQuantity++;
			break;
		case CompostType.GrowthCycles:
			BiomassQuantity++;
			break;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (ImportingThing is ICompostable)
		{
			if (Mode != 1)
			{
				OnServer.Interact(base.InteractMode, 1);
			}
			_currentProgress += deltaTime;
		}
		else if (Mode == 1)
		{
			OnServer.Interact(base.InteractMode, 0);
		}
		if (!CanDoProcessing)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return;
		}
		_totalFertilizerProcessedSeconds += deltaTime;
		if (_totalFertilizerProcessedSeconds >= 60f)
		{
			_totalFertilizerProcessedSeconds = 0f;
			UnprocessedAmount -= _itemsToCreateFertilizer;
			CreateOutput();
		}
	}

	private void CreateOutput()
	{
		float num = DecayFoodQuantity + NormalFoodQuantity + BiomassQuantity;
		float num2 = (float)DecayFoodQuantity / num;
		float num3 = (float)NormalFoodQuantity / num;
		float num4 = (float)BiomassQuantity / num;
		RemoveQuantity(ref DecayFoodQuantity, num2);
		RemoveQuantity(ref NormalFoodQuantity, num3);
		RemoveQuantity(ref BiomassQuantity, num4);
		Fertiliser fertiliser = OnServer.Create<Fertiliser>(FertiliserPrefab, base.Position, Rotation);
		fertiliser.GrowthSpeed += num2 * 0.25f;
		fertiliser.HarvestBoost += num3 * 1.5f;
		fertiliser.Cycles += num4 * 5f;
		OnServer.MoveToSlot(fertiliser, ExportSlot);
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

	protected override void OnServerExportTick(float deltaTime)
	{
		if (OnOff && Powered && base.IsStructureCompleted)
		{
			if (ExportingThing == null && base.IsExportOpen)
			{
				OnServer.Interact(base.InteractExport, 0);
			}
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == InfoPanel && InfoPanel != null)
		{
			result.Title = Localization.GetInterface("Contents");
			int value = 0;
			if (ImportingThing != null)
			{
				value = ((!(ImportingThing is Stackable stackable)) ? 1 : stackable.Quantity);
			}
			string text = string.Empty;
			if (Activate == 1)
			{
				text = GameStrings.AdvancedComposterCurrentlyProcessing;
			}
			result.State += text;
			ref string state = ref result.State;
			state = state + "\n" + GameStrings.AdvancedComposterItemsLeftToGrind.AsString(StringManager.Get(value)) + "\n " + GameStrings.AdvancedComposterItemsLeftToProcess.AsString(StringManager.Get(UnprocessedAmount)) + "\n";
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			if (doAction)
			{
				PlaySound(ActivateButtonHash);
			}
			if (UnprocessedAmount < _itemsToCreateFertilizer)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.ComposterNotEnoughProcessItems);
			}
			if (ConnectedPipeNetworks.Count == 0 || ConnectedPipeNetworks[0] == null || ConnectedPipeNetworks[0].Atmosphere.GasMixture.Water.Quantity < MolesDrainedPerProcessedItem)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.ComposterNotEnoughWater);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			if (Activate == 0)
			{
				if (UnprocessedAmount >= _itemsToCreateFertilizer)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
			}
			else
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private bool AtmosphericProcessing()
	{
		MoleQuantity moleQuantity = MolesDrainedPerProcessedItem / 60.0 * GameManager.GameTickSpeedSeconds;
		Mole mole = ConnectedPipeNetworks[0].Atmosphere.GasMixture.Water.Remove(moleQuantity);
		_powerUsedDuringTick += 100f;
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		GasMixture gasMixture = GasMixtureHelper.Create();
		foreach (SpawnGas expelledGa in ExpelledGas)
		{
			MoleQuantity quantity = expelledGa.GetQuantity() / 60.0 * GameManager.GameTickSpeedSeconds;
			MoleEnergy energy = expelledGa.GetEnergy() / 60.0 * GameManager.GameTickSpeedSeconds;
			gasMixture.Add(new Mole(expelledGa.Type, quantity, energy));
		}
		atmosphere.Add(gasMixture);
		return mole.Quantity >= moleQuantity;
	}

	public override void OnAtmosphericTick()
	{
		if (Activate != 0 && OnOff && Powered && IsOperable && !AtmosphericProcessing())
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return UnprocessedAmount;
		}
		return base.GetLogicValue(logicType);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AdvancedComposterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is AdvancedComposterSaveData advancedComposterSaveData)
		{
			UnprocessedAmount = advancedComposterSaveData.UnprocessedItems;
			BiomassQuantity = advancedComposterSaveData.SavedBiomassQuantity;
			DecayFoodQuantity = advancedComposterSaveData.SavedDecayFoodQuantity;
			NormalFoodQuantity = advancedComposterSaveData.SavedNormalFoodQuantity;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is AdvancedComposterSaveData advancedComposterSaveData)
		{
			advancedComposterSaveData.UnprocessedItems = UnprocessedAmount;
			advancedComposterSaveData.SavedBiomassQuantity = BiomassQuantity;
			advancedComposterSaveData.SavedDecayFoodQuantity = DecayFoodQuantity;
			advancedComposterSaveData.SavedNormalFoodQuantity = NormalFoodQuantity;
		}
	}
}
