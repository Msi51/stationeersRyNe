using System;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class PlantGeneticSplicer : Appliance, IGenetics, IPowered, IDensePoolable, IReferencable, IEvaluable
{
	[Header("Plant Genetic Splicer")]
	[SerializeField]
	private MaterialChanger _infoPanelMaterialChanger;

	[SerializeField]
	private MaterialChanger _leftArrowMaterialChanger;

	[SerializeField]
	private MaterialChanger _rightArrowMaterialChanger;

	[SerializeField]
	private MaterialChanger _activateButtonMaterialChanger;

	private Plant _sourcePlant;

	private Plant _targetPlant;

	private int _geneToSplice = 1;

	private float _spliceTimeRemaining;

	private int _plantGeneCount;

	private float _spliceTime = 1200f;

	private int _ticksUnPowered;

	private int _unPoweredTicksBeforeCancel = 3;

	private readonly int PoweredState = Animator.StringToHash("powered");

	private readonly int UnPoweredState = Animator.StringToHash("unpowered");

	private static readonly Vector3 InteractSoundLocalPos = new Vector3(-0.1f, 0.18f, 0.17f);

	[ByteArraySync]
	private int GeneToSplice
	{
		get
		{
			return _geneToSplice;
		}
		set
		{
			if (value != _geneToSplice)
			{
				_geneToSplice = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	[ByteArraySync]
	private float SpliceTimeRemaining
	{
		get
		{
			return _spliceTimeRemaining;
		}
		set
		{
			if (value != _spliceTimeRemaining)
			{
				_spliceTimeRemaining = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	private string GeneName(int index)
	{
		return GeneHelper.DisplayName((Gene)LoopGeneToSpliceIndex(index));
	}

	private int LoopGeneToSpliceIndex(int index)
	{
		if (index >= _plantGeneCount)
		{
			return 1;
		}
		if (index < 1)
		{
			return _plantGeneCount - 1;
		}
		return index;
	}

	public override void Awake()
	{
		base.Awake();
		ElectricityManager.Register(this);
		_plantGeneCount = Enum.GetValues(typeof(Gene)).Length;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.GeneticDevices);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => SwapGeneButton(-1, doAction), 
			InteractableType.Button2 => SwapGeneButton(1, doAction), 
			InteractableType.Activate => StartSpliceButton(doAction), 
			InteractableType.Button3 => InfoPanel(), 
			InteractableType.Slot1 => SlotInteraction(Slots[0], interactable, interaction, doAction), 
			InteractableType.Slot2 => SlotInteraction(Slots[1], interactable, interaction, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance SlotInteraction(Slot slot, Interactable interactable, Interaction interaction, bool doAction)
	{
		if (!(interaction.SourceSlot.Occupant is Plant plant))
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = slot.DisplayName
		};
		if (slot.Occupant != null)
		{
			if (slot.Occupant.PrefabHash != plant.PrefabHash)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.SlotFull);
				return delayedActionInstance.Fail();
			}
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (doAction)
		{
			plant.SplitStackIntoEmptySlot(slot, 1);
			return delayedActionInstance.Succeed();
		}
		delayedActionInstance.AppendStateMessage(GameStrings.SplicerAddOneToSlot, plant.DisplayName);
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance InfoPanel()
	{
		return new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = "Info"
		}.Succeed();
	}

	private DelayedActionInstance SwapGeneButton(int increment, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = GameStrings.SplicerChangeGene.DisplayString
		};
		if (!OnOff)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
			return delayedActionInstance.Fail();
		}
		if (!Powered)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoPower);
			return delayedActionInstance.Fail();
		}
		if (IsLocked)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceLocked);
			return delayedActionInstance.Fail();
		}
		if (!doAction)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerCurrentGene, GeneName(GeneToSplice));
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerNextGene, GeneName(GeneToSplice + increment));
			return delayedActionInstance.Succeed();
		}
		int clipsDataNameHash = ((increment > 0) ? Defines.Sounds.DirectionNextButton : Defines.Sounds.DirectionPreviousButton);
		PlayPooledAudioSound(clipsDataNameHash, InteractSoundLocalPos);
		if (GameManager.RunSimulation)
		{
			GeneToSplice = LoopGeneToSpliceIndex(GeneToSplice + increment);
		}
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance StartSpliceButton(bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = GameStrings.SplicerStartSplice.DisplayString
		};
		if (!OnOff)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
			return delayedActionInstance.Fail();
		}
		if (!Powered)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoPower);
			return delayedActionInstance.Fail();
		}
		if (!PlantSlotsValid())
		{
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerNeedSourceAndTargetPlant);
			return delayedActionInstance.Fail();
		}
		if (IsOpen)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerDeviceIsOpen);
			return delayedActionInstance.Fail();
		}
		if (IsLocked)
		{
			string arg = StringManager.Get(Mathf.RoundToInt((1f - SpliceTimeRemaining / _spliceTime) * 100f));
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerBusy, arg);
			return delayedActionInstance.Fail();
		}
		if (!doAction)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.SplicerPreviewSplice, GeneName(GeneToSplice), _sourcePlant.DisplayName, _targetPlant.DisplayName);
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			_ticksUnPowered = 0;
			SpliceTimeRemaining = _spliceTime;
			StartSplice().Forget();
		}
		return delayedActionInstance.Succeed();
	}

	private void SetLockState(int state)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Lock, state);
		}
	}

	private async UniTaskVoid StartSplice()
	{
		OnServer.Interact(base.InteractActivate, 1);
		SetLockState(1);
		while (Error == 0 && SpliceTimeRemaining > 0f && _ticksUnPowered < _unPoweredTicksBeforeCancel)
		{
			await UniTask.WaitForEndOfFrame();
			SpliceTimeRemaining -= Time.deltaTime;
		}
		SetLockState(0);
		OnServer.Interact(base.InteractActivate, 0);
		if (SpliceTimeRemaining > 0f)
		{
			SpliceTimeRemaining = 0f;
			return;
		}
		Gene geneToSplice = (Gene)GeneToSplice;
		float value = _sourcePlant.Genes.GetValue(geneToSplice);
		_targetPlant.Genes.SetGeneValue(geneToSplice, value);
		if (NetworkManager.IsServer)
		{
			_targetPlant.NetworkUpdateFlags |= 512;
		}
		PlayNetworkSound(Defines.Sounds.CompletedChime);
		OnServer.Destroy(_sourcePlant);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && SpliceTimeRemaining > 0f)
		{
			_ticksUnPowered = 0;
			StartSplice().Forget();
		}
	}

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Powered, receivingPower ? 1 : 0);
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (GameManager.RunSimulation && SpliceTimeRemaining > 0f)
		{
			if (!Powered)
			{
				_ticksUnPowered++;
			}
			else
			{
				_ticksUnPowered = 0;
			}
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ScaleToSlot();
			newChild.ThingTransformLocalPosition = Vector3.zero;
			newChild.ThingTransformLocalRotation = Quaternion.identity;
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckSlots();
		CheckErrorState();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckSlots();
		CheckErrorState();
	}

	private void CheckSlots()
	{
		_sourcePlant = Slots[0].Occupant as Plant;
		_targetPlant = Slots[1].Occupant as Plant;
	}

	private bool PlantSlotsValid()
	{
		if (_sourcePlant == null || _targetPlant == null)
		{
			return false;
		}
		if (_sourcePlant.PrefabHash == _targetPlant.PrefabHash)
		{
			return true;
		}
		if ((bool)_sourcePlant.SeedObject && _sourcePlant.SeedObject.PrefabHash == _targetPlant.PrefabHash)
		{
			return true;
		}
		if ((bool)_targetPlant.SeedObject && _sourcePlant.PrefabHash == _targetPlant.SeedObject.PrefabHash)
		{
			return true;
		}
		return false;
	}

	private void CheckErrorState()
	{
		if (GameManager.RunSimulation)
		{
			int num = (((!PlantSlotsValid() || IsOpen) && Powered && OnOff) ? 1 : 0);
			if (Error != num)
			{
				OnServer.Interact(base.InteractError, num);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		BaseInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			int id = ((Powered && OnOff) ? PoweredState : UnPoweredState);
			_infoPanelMaterialChanger.ChangeState(id);
			_leftArrowMaterialChanger.ChangeState(id);
			_rightArrowMaterialChanger.ChangeState(id);
			_activateButtonMaterialChanger.ChangeState(id);
		}
		action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered || action == InteractableType.Open)
		{
			CheckErrorState();
		}
		if (interactable.Action == InteractableType.Activate && Activate == 0)
		{
			StopSound(Defines.Sounds.Splice);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is PlantGeneticSplicerSaveData plantGeneticSplicerSaveData)
		{
			plantGeneticSplicerSaveData.GeneToSplice = GeneToSplice;
			plantGeneticSplicerSaveData.SpliceTimeRemaining = SpliceTimeRemaining;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		base.SerializeSave();
		ThingSaveData savedData = new PlantGeneticSplicerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is PlantGeneticSplicerSaveData plantGeneticSplicerSaveData)
		{
			SpliceTimeRemaining = plantGeneticSplicerSaveData.SpliceTimeRemaining;
			GeneToSplice = plantGeneticSplicerSaveData.GeneToSplice;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(GeneToSplice);
			writer.WriteSingle(SpliceTimeRemaining);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			GeneToSplice = reader.ReadInt32();
			SpliceTimeRemaining = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(GeneToSplice);
		writer.WriteSingle(SpliceTimeRemaining);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		GeneToSplice = reader.ReadInt32();
		SpliceTimeRemaining = reader.ReadSingle();
	}
}
