using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Electrical;

public class VendingMachine : DeviceImportExport, ITradableInventory, IReferencable, IEvaluable, ITrading
{
	public const int StartIndex = 2;

	public const int StorageSlots = 100;

	public Transform PreviewTransform;

	private int _currentIndex = 2;

	private int _dispenseSlot = -1;

	public readonly CancellationTokenWrapper RequestTask = new CancellationTokenWrapper();

	public int RequestedHash;

	private int _filledSlots;

	public static readonly int HasContentsState = Animator.StringToHash("HasContents");

	private static readonly int VendButtonHash = Animator.StringToHash("VendButton");

	private static readonly int SelectButtonHash = Animator.StringToHash("SelectButton");

	private static readonly int VendButtonEnabledHash = Animator.StringToHash("VendButtonEnabled");

	private static readonly int VendButtonDisabledHash = Animator.StringToHash("VendButtonDisabled");

	public Renderer Screen;

	private GameAudioSource _vendButtonAudio;

	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			BaseAnimator.SetBool(HasContentsState, value: false);
			_currentIndex = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			if (!CurrentSlot.IsInteractable && (bool)CurrentSlot.Occupant)
			{
				StartCoroutine(WaitThenCheck());
			}
		}
	}

	public override int PoweredValue
	{
		get
		{
			return base.PoweredValue;
		}
		set
		{
			base.PoweredValue = value;
			PreviousContentsShow();
		}
	}

	public bool HasSomething => _filledSlots > 0;

	public override bool CanIceMelt => false;

	public Slot CurrentSlot => Slots[CurrentIndex];

	public bool IsSlotTradable(Slot slot)
	{
		if (!slot.IsInteractable)
		{
			return slot.Parent == this;
		}
		return false;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(CurrentIndex);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentIndex = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(CurrentIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentIndex = reader.ReadInt32();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new VendingMachineSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is VendingMachineSaveData vendingMachineSaveData)
		{
			CurrentIndex = vendingMachineSaveData.CurrentIndex;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is VendingMachineSaveData vendingMachineSaveData)
		{
			vendingMachineSaveData.CurrentIndex = CurrentIndex;
		}
	}

	private int SetRequestFromHash(int hash)
	{
		int num = Slots.FindIndex(2, (Slot slot) => (bool)slot.Occupant && slot.Occupant.PrefabHash == hash);
		if (num < 0)
		{
			return -1;
		}
		RequestedHash = hash;
		return num;
	}

	private async UniTask SetRequestFromHashTask(CancellationToken cancellationToken, int hash)
	{
		await UniTask.SwitchToMainThread(cancellationToken);
		int index = SetRequestFromHash(hash);
		if (index >= 2)
		{
			while (!Powered || IsLocked)
			{
				await UniTask.WaitForEndOfFrame(cancellationToken);
			}
			OnServer.Interact(base.InteractLock, 1);
			CurrentIndex = index;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
			OnServer.Interact(base.InteractExport, 1);
			RequestedHash = 0;
			OnServer.Interact(base.InteractLock, 0);
		}
		RequestTask.Cancel();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		RequestTask.Cancel();
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.RequestHash || logicType == LogicType.DispenseSlot)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.RequestHash:
			if (!RequestTask.Initialized)
			{
				RequestTask.Initialize();
				SetRequestFromHashTask(RequestTask.Token, (int)value).Forget();
			}
			break;
		case LogicType.DispenseSlot:
			_dispenseSlot = (int)value;
			break;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Ratio:
		case LogicType.Quantity:
		case LogicType.RequestHash:
		case LogicType.TargetSlotIndex:
		case LogicType.TargetPrefabHash:
		case LogicType.DispenseSlot:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	private void CalculateFilledSlots()
	{
		int num = 0;
		for (int i = 2; i < Slots.Count; i++)
		{
			if ((bool)Slots[i].Occupant)
			{
				num++;
			}
		}
		_filledSlots = num;
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.TargetSlotIndex:
			return CurrentIndex;
		case LogicType.TargetPrefabHash:
		{
			if (CurrentIndex == -1)
			{
				return 0.0;
			}
			if (CurrentIndex >= Slots.Count)
			{
				return 0.0;
			}
			DynamicThing occupant;
			return Slots[CurrentIndex].Contains<DynamicThing>(out occupant) ? occupant.PrefabHash : 0;
		}
		case LogicType.RequestHash:
			return RequestedHash;
		case LogicType.Quantity:
			return _filledSlots;
		case LogicType.Ratio:
			return (float)_filledSlots / 100f;
		case LogicType.DispenseSlot:
			return _dispenseSlot;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	private IEnumerator WaitThenCheck()
	{
		yield return Yielders.WaitForSeconds(0.2f);
		BaseAnimator.SetBool(HasContentsState, CurrentSlot.Occupant);
	}

	public void PreviousContentsClear()
	{
		Screen.material.mainTexture = null;
		Screen.material.SetTexture("_EmissionMap", null);
	}

	public void PreviousContentsShow()
	{
		Texture2D texture2D = (((bool)CurrentSlot.Occupant && Powered && OnOff) ? CurrentSlot.Occupant.GetThumbnail().texture : null);
		Screen.material.mainTexture = texture2D;
		Screen.material.color = (((object)texture2D == null) ? Color.clear : Color.white);
		Screen.material.SetTexture("_EmissionMap", texture2D);
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		TryDispenseSlot();
	}

	private void TryDispenseSlot()
	{
		if (_dispenseSlot >= 0)
		{
			if (_dispenseSlot < 2 || _dispenseSlot >= Slots.Count || !Slots[_dispenseSlot].Occupant)
			{
				_dispenseSlot = -1;
			}
			else if (OnOff && Powered && IsNextExportReady)
			{
				CurrentIndex = _dispenseSlot;
				OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
				OnServer.Interact(base.InteractExport, 1);
				_dispenseSlot = -1;
			}
		}
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable)
		{
			BaseAnimator.SetBool(HasContentsState, value: false);
			StartCoroutine(WaitThenCheck());
		}
		CalculateFilledSlots();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		if (previousChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable)
		{
			BaseAnimator.SetBool(HasContentsState, value: false);
		}
		base.OnChildExitInventory(previousChild);
		if (!base.BeingDestroyed)
		{
			BaseAnimator.SetBool(HasContentsState, value: false);
			StartCoroutine(WaitThenCheck());
			CalculateFilledSlots();
			if (GameManager.RunSimulation)
			{
				TryProcessImport();
			}
		}
	}

	public Slot SamplePlanBackward()
	{
		int num = CurrentIndex;
		bool flag = true;
		int num2 = 0;
		while (flag)
		{
			num--;
			if (num < 0)
			{
				num = Slots.Count - 1;
			}
			Slot slot = Slots[num];
			if (!slot.IsInteractable && (bool)slot.Occupant)
			{
				flag = false;
			}
			if (num2 > Slots.Count)
			{
				num = CurrentIndex;
				flag = false;
			}
			num2++;
		}
		return Slots[num];
	}

	public Slot SamplePlanForward()
	{
		int num = CurrentIndex;
		bool flag = true;
		int num2 = 0;
		while (flag)
		{
			num++;
			if (num >= Slots.Count)
			{
				num = 0;
			}
			Slot slot = Slots[num];
			if (!slot.IsInteractable && (bool)slot.Occupant)
			{
				flag = false;
			}
			if (num2 > Slots.Count)
			{
				num = CurrentIndex;
				flag = false;
			}
			num2++;
		}
		return Slots[num];
	}

	public void PlanForward()
	{
		CurrentIndex = SamplePlanForward().SlotIndex;
	}

	public void PlanBackward()
	{
		CurrentIndex = SamplePlanBackward().SlotIndex;
	}

	protected override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation)
		{
			TryProcessImport();
			if (GameManager.GameState == GameState.Running && !CurrentSlot.Occupant)
			{
				PlanForward();
			}
		}
	}

	protected virtual void TryProcessImport()
	{
		if (ImportingThing != null)
		{
			for (int i = 0; i < Slots.Count; i++)
			{
				Slot slot = Slots[i];
				if (!slot.IsInteractable)
				{
					if (!(slot.Occupant != null))
					{
						OnServer.MoveToSlot(ImportingThing, slot);
						break;
					}
					if (i == Slots.Count - 1)
					{
						PlayNetworkSound(FabricatorBase.ImportErrorHash);
					}
				}
			}
		}
		OnServer.Interact(base.InteractImport, 0);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			if (!CurrentSlot.Occupant)
			{
				return ActionStrings.Vend;
			}
			return ActionStrings.Vend + " " + CurrentSlot.Occupant.DisplayName + " " + CurrentSlot.Occupant.GetQuantityText();
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			if (!CurrentSlot.Occupant)
			{
				return CurrentSlot.DisplayName;
			}
			return CurrentSlot.Occupant.DisplayName + " " + CurrentSlot.Occupant.GetQuantityText();
		}
		return base.GetContextualName(interactable);
	}

	public override void OnExportClosingComplete()
	{
		base.OnExportClosingComplete();
		PlanForward();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate || interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!IsAuthorized(interaction.SourceThing))
			{
				return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
			}
			switch (interactable.Action)
			{
			case InteractableType.Activate:
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (Exporting != 0)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceLocked);
				}
				if (!CurrentSlot.Occupant || !HasSomething)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNothingSelectedToDispense);
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlaySound(VendButtonHash);
				if (GameManager.RunSimulation)
				{
					OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
					OnServer.Interact(base.InteractExport, 1);
				}
				return delayedActionInstance.Succeed();
			case InteractableType.Button1:
			{
				if (!HasSomething)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Down;
					return delayedActionInstance.Fail(GameStrings.DeviceNothingToChangeTo);
				}
				if (!OnOff)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Off;
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Powered;
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				Slot slot2 = SamplePlanBackward();
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, slot2.Occupant ? slot2.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlaySound(SelectButtonHash);
				if (GameManager.RunSimulation)
				{
					PlanBackward();
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!HasSomething)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Up;
					return delayedActionInstance.Fail(GameStrings.DeviceNothingToChangeTo);
				}
				if (!OnOff)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Off;
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Powered;
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				Slot slot = SamplePlanForward();
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, slot.Occupant ? slot.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlaySound(SelectButtonHash);
				if (GameManager.RunSimulation)
				{
					PlanForward();
				}
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public List<DynamicThing> GetContents()
	{
		List<DynamicThing> list = new List<DynamicThing>();
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && IsSlotTradable(slot))
			{
				list.Add(slot.Occupant);
			}
		}
		return list;
	}

	public List<Slot> GetSlots()
	{
		return Slots;
	}

	public Dictionary<int, Slot> GetOccupiedSlots()
	{
		Dictionary<int, Slot> dictionary = new Dictionary<int, Slot>();
		for (int i = 2; i < Slots.Count; i++)
		{
			if ((bool)Slots[i].Occupant)
			{
				dictionary.Add(i, Slots[i]);
			}
		}
		return dictionary;
	}

	public void PlayVendButtonEnabledSound()
	{
		if (_vendButtonAudio == null)
		{
			_vendButtonAudio = GetAudioSource(GetAudioEvent(VendButtonHash).Channel);
		}
		if (Powered && (!_vendButtonAudio.isPlaying || _vendButtonAudio.CurrentClips?.NameHash != VendButtonHash))
		{
			PlaySound(VendButtonEnabledHash);
		}
	}

	public void PlayVendButtonDisabledSound()
	{
		if (_vendButtonAudio == null)
		{
			_vendButtonAudio = GetAudioSource(GetAudioEvent(VendButtonHash).Channel);
		}
		if (Powered && (!_vendButtonAudio.isPlaying || _vendButtonAudio.CurrentClips?.NameHash != VendButtonHash))
		{
			PlaySound(VendButtonDisabledHash);
		}
	}
}
