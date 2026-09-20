using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Objects;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class PlantStabilizer : Appliance, IGenetics
{
	[Header("Plant Stabilizer")]
	[SerializeField]
	private SwitchMode _switchMode;

	[SerializeField]
	private Collider _infoPanel;

	private float _processTimeStabilise = 5f;

	private float _processTimeDestabilise = 12f;

	private float _stabilizeAmount = 0.5f;

	private float _destabilizeMainAmount = 0.5f;

	private float _destabilizeSubAmount = 0.1f;

	private int _currentGeneIndex;

	private bool _cancelProgress;

	private float _progressPercentage;

	private MaterialChanger[] _materialChanges;

	[SerializeField]
	private MaterialChanger[] _materialChangerArrows;

	private readonly int PoweredState = Animator.StringToHash("powered");

	private readonly int UnPoweredState = Animator.StringToHash("unpowered");

	private bool IsStabilize => Mode == 0;

	private bool IsDestabilize => Mode == 1;

	private Plant CurrentPlant => Slots[0].Occupant as Plant;

	private Gene CurrentGene => GeneCollection.Genes[_currentGeneIndex];

	[ByteArraySync]
	private float ProgressPercentage
	{
		get
		{
			return _progressPercentage;
		}
		set
		{
			if (value != _progressPercentage)
			{
				_progressPercentage = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public override string[] ModeStrings => new string[2]
	{
		GameStrings.PlantStabilizerStabilizeMode.AsString(),
		GameStrings.PlantStabilizerDestabilizeMode.AsString()
	};

	public override void Awake()
	{
		base.Awake();
		_materialChanges = GetComponentsInChildren<MaterialChanger>();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.GeneticDevices);
	}

	private void SetEmissiveMaterials()
	{
		int num = ((OnOff && Powered) ? PoweredState : UnPoweredState);
		MaterialChanger[] materialChanges = _materialChanges;
		for (int i = 0; i < materialChanges.Length; i++)
		{
			materialChanges[i].ChangeState(num);
		}
		materialChanges = _materialChangerArrows;
		for (int i = 0; i < materialChanges.Length; i++)
		{
			materialChanges[i].ChangeState(IsDestabilize ? num : UnPoweredState);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && Activate == 1)
		{
			StartProcessAnimation().Forget();
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if ((bool)_switchMode)
		{
			_switchMode.RefreshState(skipAnimation);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckErrorState();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckErrorState();
	}

	private void CheckErrorState()
	{
		if (GameManager.RunSimulation)
		{
			int num = (((IsOpen || !CurrentPlant) && Powered && OnOff) ? 1 : 0);
			if (Error != num)
			{
				OnServer.Interact(base.InteractError, num);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		switch (interactable.Action)
		{
		case InteractableType.Activate:
			if (GameManager.RunSimulation && interactable.State == 1 && (bool)CurrentPlant)
			{
				_cancelProgress = false;
				StartProcessAnimation().Forget();
			}
			break;
		case InteractableType.OnOff:
		case InteractableType.Powered:
			if (interactable.State == 0)
			{
				CancelProcess();
			}
			CheckErrorState();
			SetEmissiveMaterials();
			break;
		case InteractableType.Mode:
			SetEmissiveMaterials();
			break;
		case InteractableType.Open:
			if (interactable.State == 1)
			{
				CancelProcess();
			}
			CheckErrorState();
			break;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (!_infoPanel || hitCollider != _infoPanel)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		PassiveTooltip result = passiveTooltip;
		if (!OnOff)
		{
			result.State = GameStrings.DeviceNotOn.DisplayString;
			return result;
		}
		if (!Powered)
		{
			result.State = GameStrings.DeviceNoPower.DisplayString;
			return result;
		}
		StringBuilder stringBuilder = new StringBuilder();
		Gene gene = Gene.None;
		if (IsDestabilize)
		{
			gene = GeneCollection.Genes[_currentGeneIndex];
			stringBuilder.AppendLine(GameStrings.GeneCurrentSelected.AsString(gene.ToGameString().AsString()));
		}
		if ((bool)CurrentPlant)
		{
			stringBuilder.AppendLine(string.Empty);
			foreach (KeyValuePair<Gene, GeneWrapper> item in CurrentPlant.Genes.Lookup)
			{
				if (item.Key == gene)
				{
					stringBuilder.AppendLine($"<color=yellow>{item.Key.ToGameString()}: {StringManager.Get(item.Value.Stability)}</color>");
				}
				else
				{
					stringBuilder.AppendLine($"{item.Key.ToGameString()}: {StringManager.Get(item.Value.Stability)}");
				}
			}
		}
		result.State = stringBuilder.ToString();
		return result;
	}

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Powered, receivingPower ? 1 : 0);
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

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		return interactable.Action switch
		{
			InteractableType.Activate => HandleActivate(doAction, result), 
			InteractableType.Mode => HandleMode(doAction, result), 
			InteractableType.Slot1 => InteractWithSlot1(doAction, interactable, interaction, result), 
			InteractableType.Button1 => InteractHandleButton1(doAction, result), 
			InteractableType.Button2 => InteractHandleButton2(doAction, result), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance ChangeGene(bool doAction, DelayedActionInstance result, int geneIndex)
	{
		Gene gene = GeneCollection.Genes[geneIndex];
		result.ActionMessage = gene.ToGameString().AsString();
		if (!OnOff)
		{
			result.AppendStateMessage(GameStrings.DeviceNotOn);
			return result.Fail();
		}
		if (!Powered)
		{
			result.AppendStateMessage(GameStrings.DeviceNoPower);
			return result.Fail();
		}
		if (IsStabilize)
		{
			result.AppendStateMessage(GameStrings.GeneDestabilisingOnly);
			return result.Fail();
		}
		if (IsLocked)
		{
			result.AppendStateMessage(GameStrings.DeviceLocked);
			return result.Fail();
		}
		if (doAction)
		{
			SetGeneIndex(geneIndex);
		}
		return result.Succeed();
	}

	private DelayedActionInstance InteractHandleButton1(bool doAction, DelayedActionInstance result)
	{
		int num = _currentGeneIndex - 1;
		if (num < 0)
		{
			num = GeneCollection.Genes.Length - 1;
		}
		return ChangeGene(doAction, result, num);
	}

	private DelayedActionInstance InteractHandleButton2(bool doAction, DelayedActionInstance result)
	{
		int geneIndex = (_currentGeneIndex + 1) % GeneCollection.Genes.Length;
		return ChangeGene(doAction, result, geneIndex);
	}

	private DelayedActionInstance HandleActivate(bool doAction, DelayedActionInstance result)
	{
		result.ActionMessage = (IsStabilize ? GameStrings.PlantStabilizerStartStabilizing.AsString() : GameStrings.PlantStabilizerStartDestabilizing.AsString());
		if (!CurrentPlant)
		{
			result.AppendStateMessage(GameStrings.PlantStabilizerNoPlantInSlot);
			return result.Fail();
		}
		if (IsOpen)
		{
			result.AppendStateMessage(GameStrings.DeviceIsOpen);
			return result.Fail();
		}
		if (!OnOff)
		{
			result.AppendStateMessage(GameStrings.DeviceNotOn);
			return result.Fail();
		}
		if (!Powered)
		{
			result.AppendStateMessage(GameStrings.DeviceNoPower);
			return result.Fail();
		}
		if (doAction && GameManager.RunSimulation)
		{
			HandleActivate();
		}
		if (Activate == 1)
		{
			Assets.Scripts.Localization2.GameString gameString = (IsStabilize ? GameStrings.PlantStabilizerStabilizingPlant : GameStrings.PlantStabilizerDestabilizingPlant);
			int value = Mathf.RoundToInt(ProgressPercentage * 100f);
			result.AppendStateMessage(GameStrings.DeviceLocked);
			result.AppendStateMessage(gameString, CurrentPlant.ToTooltip(), StringManager.Get(value));
			return result.Fail();
		}
		if (IsStabilize)
		{
			result.AppendStateMessage(GameStrings.PlantStabilizerStabilizePlant, CurrentPlant.ToTooltip());
		}
		else
		{
			result.AppendStateMessage(GameStrings.PlantStabilizerDestabilizePlant, CurrentGene.ToGameString().AsString(), CurrentPlant.ToTooltip());
		}
		return result.Succeed();
	}

	private DelayedActionInstance HandleMode(bool doAction, DelayedActionInstance result)
	{
		result.ActionMessage = ((Mode == 1) ? GameStrings.PlantStabilizerToggleModeToStabilize.AsString() : GameStrings.PlantStabilizerToggleModeToDestabilize.AsString());
		if (IsLocked)
		{
			result.AppendStateMessage(GameStrings.DeviceLocked);
			return result.Fail();
		}
		if (!OnOff)
		{
			result.AppendStateMessage(GameStrings.DeviceNotOn);
			return result.Fail();
		}
		if (!Powered)
		{
			result.AppendStateMessage(GameStrings.DeviceNoPower);
			return result.Fail();
		}
		if (doAction)
		{
			OnServer.Interact(base.InteractMode, (Mode != 1) ? 1 : 0);
		}
		return result.Succeed();
	}

	private DelayedActionInstance InteractWithSlot1(bool doAction, Interactable interactable, Interaction interaction, DelayedActionInstance result)
	{
		if (!(interaction.SourceSlot.Occupant is Plant plant))
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		if ((bool)Slots[0].Occupant)
		{
			if (Slots[0].Occupant.PrefabHash != plant.PrefabHash)
			{
				result.AppendStateMessage(GameStrings.SlotFull);
				return result.Fail();
			}
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (doAction)
		{
			plant.SplitStackIntoEmptySlot(Slots[0], 1);
		}
		return result.Succeed();
	}

	private void SetGeneIndex(int index)
	{
		_currentGeneIndex = index;
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 512;
		}
	}

	private void HandleActivate()
	{
		int num = ((OnOff && !IsOpen && (bool)CurrentPlant) ? 1 : 0);
		if (Activate != num)
		{
			OnServer.Interact(base.InteractActivate, num);
			OnServer.Interact(base.InteractLock, num);
		}
	}

	private async UniTaskVoid StartProcessAnimation()
	{
		float t = 0f;
		float targetTime = (IsStabilize ? _processTimeStabilise : _processTimeDestabilise);
		while (t < targetTime && !_cancelProgress && (bool)CurrentPlant)
		{
			t += Time.deltaTime;
			ProgressPercentage = t / targetTime;
			await UniTask.WaitForEndOfFrame();
		}
		if (!_cancelProgress && (bool)CurrentPlant)
		{
			if (IsStabilize)
			{
				Stabilise();
			}
			else if (IsDestabilize)
			{
				Destabilise(GeneCollection.Genes[_currentGeneIndex]);
			}
			CancelProcess();
		}
	}

	private void Stabilise()
	{
		foreach (GeneWrapper value in CurrentPlant.Genes.Lookup.Values)
		{
			value.Stabilise(_stabilizeAmount);
		}
		if (NetworkManager.IsServer)
		{
			CurrentPlant.NetworkUpdateFlags |= 512;
		}
	}

	private void Destabilise(Gene geneDestabilise)
	{
		foreach (GeneWrapper value in CurrentPlant.Genes.Lookup.Values)
		{
			float amount = ((value.Gene == geneDestabilise) ? _destabilizeMainAmount : _destabilizeSubAmount);
			value.Destabilise(amount);
		}
		if (NetworkManager.IsServer)
		{
			CurrentPlant.NetworkUpdateFlags |= 512;
		}
	}

	private void CancelProcess()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractActivate, 0);
			OnServer.Interact(base.InteractLock, 0);
			_cancelProgress = true;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(_currentGeneIndex);
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(ProgressPercentage);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			_currentGeneIndex = reader.ReadInt32();
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			ProgressPercentage = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(_currentGeneIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_currentGeneIndex = reader.ReadInt32();
	}
}
