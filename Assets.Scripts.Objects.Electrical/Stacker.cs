using System;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Stacker : SlotHandlerBase, ISetable, ILogicable, IReferencable, IEvaluable
{
	private static readonly int StackHash = Animator.StringToHash("Stack");

	private double _setting = 1.0;

	[Header("Stacker")]
	public Transform Wheel;

	public float WheelSpinSpeed = 10f;

	private float MaxWheelSetting = 500f;

	[ReadOnly]
	public DynamicThing ProcessingThing;

	[ReadOnly]
	public Stackable ProcessingStackable;

	[ReadOnly]
	public Consumable ProcessingConsumable;

	private Quaternion _wheelBaseRotation;

	private float _lastAngle;

	private UniTask _rotatingWheel;

	[ByteArraySync]
	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if (!(value < 1.0))
			{
				_setting = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				if (GameManager.GameState == GameState.Running)
				{
					CheckWheel();
				}
				_ = GameManager.RunSimulation;
			}
		}
	}

	public int SettingInt => (int)(Setting + 1E-06);

	public Slot ProcessingSlot => Slots[2];

	public override bool IsNextImportReady
	{
		get
		{
			if (base.IsNextImportReady)
			{
				return ExportingThing == null;
			}
			return false;
		}
	}

	public override bool CanIceMelt => false;

	public override void Awake()
	{
		base.Awake();
		_wheelBaseRotation = Wheel.localRotation;
		Setting = 1.0;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteDouble(Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadDouble();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadDouble();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StackerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StackerSaveData stackerSaveData)
		{
			if (double.IsNaN(stackerSaveData.Setting))
			{
				Setting = 1.0;
			}
			else
			{
				Setting = stackerSaveData.Setting;
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StackerSaveData stackerSaveData)
		{
			stackerSaveData.Setting = Setting;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GameStrings.GlobalIncrease.DisplayString, 
			InteractableType.Button2 => GameStrings.GlobalDecrease.DisplayString, 
			InteractableType.Activate => GameStrings.ClearStacker.DisplayString, 
			_ => base.GetContextualName(interactable), 
		};
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
		if ((bool)labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			delayedActionInstance.ActionMessage = ActionStrings.Set;
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			labeller.Set(this);
			return delayedActionInstance.Succeed();
		}
		switch (interactable.Action)
		{
		case InteractableType.Activate:
		{
			DelayedActionInstance delayedActionInstance3 = new DelayedActionInstance(0f);
			delayedActionInstance3.ActionMessage = GameStrings.ClearStacker.DisplayString;
			if (!ProcessingStackable)
			{
				if ((bool)ProcessingConsumable)
				{
					_ = ProcessingConsumable.Quantity;
				}
			}
			else
			{
				_ = ProcessingStackable.Quantity;
			}
			if (IsLocked)
			{
				delayedActionInstance3.AppendStateMessage(GameStrings.DeviceLocked);
				return delayedActionInstance3.Fail();
			}
			if (base.SlotHandlerMode == SlotHandlerMode.Logic)
			{
				delayedActionInstance3.AppendStateMessage(GameStrings.StackerWillForceChangeFromLogicToAutomatic);
			}
			if (ProcessingSlot.Occupant != null)
			{
				delayedActionInstance3.AppendStateMessage(GameStrings.StackerContainsQuantity, OccupantQuantity(), ProcessingSlot.Occupant.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance3;
				}
				OnServer.Interact(interactable, (Activate != 1) ? 1 : 0);
				return delayedActionInstance3.Succeed();
			}
			delayedActionInstance3.AppendStateMessage(GameStrings.StackerNoContentsToClear);
			return delayedActionInstance3.Fail();
		}
		case InteractableType.Button1:
			if (IsLocked)
			{
				return GetLockedText(interactable);
			}
			if (!doAction)
			{
				DelayedActionInstance delayedActionInstance4 = new DelayedActionInstance();
				delayedActionInstance4.ActionMessage = GameStrings.GlobalIncrease.AsString();
				delayedActionInstance4.AppendStateMessage(GameStrings.GlobalStackSize, StringManager.Get(Setting));
				delayedActionInstance4.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				return delayedActionInstance4.Succeed();
			}
			SettingWheel.PlayWheelSound(this, Wheel, increaseSetting: true, interaction.AltKey, Convert.ToSingle(Setting), 1f, MaxWheelSetting, 10f, 1f);
			if (GameManager.RunSimulation)
			{
				Setting = (int)Math.Min(Setting + (double)(interaction.AltKey ? 1f : 10f), MaxWheelSetting);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			if (IsLocked)
			{
				return GetLockedText(interactable);
			}
			if (!doAction)
			{
				DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance();
				delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
				delayedActionInstance2.AppendStateMessage(GameStrings.GlobalStackSize, StringManager.Get(Setting));
				delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, Wheel, increaseSetting: false, interaction.AltKey, Convert.ToSingle(Setting), 1f, MaxWheelSetting, 10f, 1f);
			if (GameManager.RunSimulation)
			{
				Setting = (int)Math.Max(Setting - (double)(interaction.AltKey ? 1f : 10f), 1.0);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	private string OccupantQuantity()
	{
		if (ProcessingSlot.Occupant is Stackable stackable)
		{
			return StringManager.Get(stackable.Quantity);
		}
		if (ProcessingSlot.Occupant is Ingot ingot)
		{
			return ingot.GetQuantityText();
		}
		return StringManager.Get(1);
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		_lastAngle = (float)Setting;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		_lastAngle = (float)Setting * WheelSpinSpeed;
		Wheel.localRotation = _wheelBaseRotation;
		Wheel.Rotate(0f, _lastAngle, 0f, Space.Self);
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (CanCompleteImport && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
		TryMoveToProcess();
	}

	protected override void OnServerExportTick()
	{
		TryClear();
		TryExportStack();
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	public DelayedActionInstance GetLockedText(Interactable interactable)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
		delayedActionInstance.Duration = 0f;
		delayedActionInstance.ActionMessage = interactable.ContextualName;
		delayedActionInstance.AppendStateMessage(GameStrings.DeviceLocked);
		return delayedActionInstance.Fail();
	}

	public void CheckWheel()
	{
		if ((bool)Wheel && _rotatingWheel.Status != UniTaskStatus.Pending)
		{
			_rotatingWheel = RotateWheel();
		}
	}

	private async UniTask RotateWheel()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (!base.BeingDestroyed && Math.Abs((double)_lastAngle - Setting) > 0.10000000149011612)
		{
			_lastAngle = ((Setting < (double)MaxWheelSetting) ? Mathf.Lerp(_lastAngle, (float)Setting * WheelSpinSpeed, 2f * Time.deltaTime) : Mathf.Lerp(_lastAngle, MaxWheelSetting * WheelSpinSpeed, 2f * Time.deltaTime));
			Wheel.localRotation = _wheelBaseRotation;
			Wheel.Rotate(0f, _lastAngle, 0f, Space.Self);
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private void TryClear()
	{
		if (OnOff && Powered && IsNextExportReady && Activate == 1 && ProcessingSlot.Occupant != null && ExportingThing == null)
		{
			OnServer.MoveToSlot(ProcessingSlot.Occupant, ExportSlot);
		}
	}

	private bool TryMoveToProcess()
	{
		if (!OnOff || !Powered || !base.IsImportClosed)
		{
			return false;
		}
		if (ImportingThing != null && ProcessingThing == null)
		{
			OnServer.MoveToSlot(ImportingThing, ProcessingSlot);
			return true;
		}
		if (ProcessingThing == null)
		{
			return false;
		}
		if (ImportingThing is Stackable stackable && stackable.PrefabHash == ProcessingThing.PrefabHash && ProcessingThing is Stackable { IsStackFull: false } stackable2)
		{
			OnServer.Merge(stackable2, stackable);
			PlayNetworkSound(StackHash);
			return true;
		}
		if (ImportingThing is Consumable consumable && consumable.PrefabHash == ProcessingThing.PrefabHash && ProcessingThing is Consumable { IsStackFull: false } consumable2)
		{
			OnServer.Combine(consumable2, consumable);
			PlayNetworkSound(StackHash);
			return true;
		}
		return false;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return passiveTooltip.Populate(openEnd);
			}
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	private void TryExportStack()
	{
		if (!OnOff || !Powered || !IsNextExportReady || (base.SlotHandlerMode == SlotHandlerMode.Logic && CurrentOutput < 0))
		{
			return;
		}
		if (ProcessingThing is Stackable stackable && (ImportingThing == null || ImportingThing.PrefabName == ProcessingThing.PrefabName || stackable.Quantity >= SettingInt))
		{
			ProcessingStackable = stackable;
			int num = Mathf.Min(stackable.MaxQuantity, SettingInt);
			if (stackable.Quantity >= num && ExportingThing == null)
			{
				if (stackable.Quantity == num)
				{
					OnServer.MoveToSlot(ProcessingThing, ExportSlot);
				}
				else
				{
					stackable.SplitStack(num, ExportSlot);
				}
			}
			if (base.SlotHandlerMode == SlotHandlerMode.Logic)
			{
				CurrentOutput = -1;
			}
		}
		else if (ProcessingThing is Consumable { AllowSplitting: not false } consumable && (ImportingThing == null || ImportingThing.PrefabName == ProcessingThing.PrefabName || consumable.Quantity >= (float)Setting))
		{
			ProcessingConsumable = consumable;
			int num2 = (int)Mathf.Min(consumable.MaxQuantity, (float)Setting);
			if (consumable.Quantity >= (float)num2 && ExportingThing == null)
			{
				if (Math.Abs(consumable.Quantity - (float)num2) < 0.01f)
				{
					OnServer.MoveToSlot(ProcessingThing, ExportSlot);
				}
				else
				{
					Consumable consumable2 = OnServer.Create<Consumable>(consumable.SourcePrefab, ExportSlot);
					consumable2.ThingTransform.localScale = Vector3.one;
					consumable2.Quantity = num2;
					consumable.Quantity -= num2;
				}
			}
			if (base.SlotHandlerMode == SlotHandlerMode.Logic)
			{
				CurrentOutput = -1;
			}
		}
		else if (ProcessingThing != null)
		{
			OnServer.MoveToSlot(ProcessingThing, ExportSlot);
			if (base.SlotHandlerMode == SlotHandlerMode.Logic)
			{
				CurrentOutput = -1;
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation && newChild.ParentSlot == ProcessingSlot)
		{
			ProcessingThing = newChild;
			OnServer.Interact(this, InteractableType.Activate, 0);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GameManager.RunSimulation && ProcessingThing == previousChild)
		{
			ProcessingThing = null;
			ProcessingStackable = null;
			ProcessingConsumable = null;
			OnServer.Interact(this, InteractableType.Activate, 1);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		Achievements.AchieveTidy(this);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Setting)
		{
			Setting = (float)Math.Max(value, 1.0);
		}
		base.SetLogicValue(logicType, value);
	}
}
