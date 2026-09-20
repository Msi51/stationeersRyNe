using System;
using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Chutes;

public class Silo : DeviceImportExport, IMemoryReadable, IMemory, IInstructable, ILogicTick, ILogicStack
{
	[Header("Silo")]
	private Queue<StoredThings> _storedItems = new Queue<StoredThings>();

	public Collider InfoPanel;

	private const int MAX_ITEMS = 600;

	private int _totalItemsCurrentlyStored;

	private LogicStack _stack = new LogicStack(600);

	private bool _stackDirty;

	private Dictionary<long, long> _changedIds = new Dictionary<long, long>();

	private bool _doneSpawning = true;

	private bool _doneSaving = true;

	private bool _dispense;

	private int _dispenseSlot = -1;

	private const float SPAWN_CHILD_INTERVAL = 0.2f;

	private StoredThings _spawningThings;

	private int _spawnChildIndex;

	private float _spawnChildTimer;

	private StoredThings _savingThings;

	private DynamicThing _savingRoot;

	private readonly Stack<DynamicThing> _saveStack = new Stack<DynamicThing>();

	private readonly List<DynamicThing> _saveDestroyList = new List<DynamicThing>();

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override int WreckageQuantity => 3;

	[ByteArraySync]
	public int TotalItemsCurrentlyStored
	{
		get
		{
			return _totalItemsCurrentlyStored;
		}
		set
		{
			_totalItemsCurrentlyStored = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return !IsFull;
			}
			return false;
		}
	}

	public bool IsFull => _storedItems.Count >= 600;

	public bool IsEmpty => _storedItems.Count == 0;

	public int SiloThingQuantity => _storedItems.Count;

	public override bool CanIceMelt => false;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override void Awake()
	{
		_storedItems = new Queue<StoredThings>(600);
		_stack = new LogicStack(600);
		base.Awake();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(TotalItemsCurrentlyStored);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			TotalItemsCurrentlyStored = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(TotalItemsCurrentlyStored);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		TotalItemsCurrentlyStored = reader.ReadInt32();
	}

	protected override CanConstructInfo CanDeconstruct()
	{
		if (_storedItems.Count == 0)
		{
			return base.CanDeconstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.StructureDeconstructionFailed.DisplayString);
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		TickSpawnExport(deltaTime);
		TickSaveImport();
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
		if (OnOff && Powered)
		{
			if (CanBeginImport && _doneSaving)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if (CanCompleteImport && ImportingThing == null && _doneSaving)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
			TryProcessImport();
		}
	}

	protected override void OnServerExportTick()
	{
		if (!OnOff || !Powered || !base.IsStructureCompleted)
		{
			return;
		}
		if (_dispense && IsEmpty)
		{
			_dispense = false;
		}
		if (_dispenseSlot >= 0 && _dispenseSlot >= _storedItems.Count)
		{
			_dispenseSlot = -1;
		}
		bool flag = !_doneSpawning || ExportingThing != null;
		if (IsOpen || _dispense || _dispenseSlot >= 0 || flag)
		{
			if (ExportingThing == null && base.IsExportOpen && _doneSpawning)
			{
				OnServer.Interact(base.InteractExport, 0);
			}
			if (IsNextExportReady && _doneSpawning && !IsEmpty)
			{
				BeginNextExport();
			}
			if (CanBeginExport && _doneSpawning)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
		}
	}

	private void BeginNextExport()
	{
		if (_dispenseSlot >= 0)
		{
			StoredThings storedThings = RemoveStoredAt(_dispenseSlot);
			_dispenseSlot = -1;
			if (storedThings != null)
			{
				BeginExport(storedThings);
			}
		}
		else
		{
			BeginExport(_storedItems.Dequeue());
			_dispense = false;
		}
	}

	private StoredThings RemoveStoredAt(int index)
	{
		if (index < 0 || index >= _storedItems.Count)
		{
			return null;
		}
		StoredThings result = null;
		int count = _storedItems.Count;
		for (int i = 0; i < count; i++)
		{
			StoredThings storedThings = _storedItems.Dequeue();
			if (i == index)
			{
				result = storedThings;
			}
			else
			{
				_storedItems.Enqueue(storedThings);
			}
		}
		return result;
	}

	private void BeginExport(StoredThings storedThings)
	{
		_changedIds.Clear();
		_doneSpawning = false;
		_stackDirty = true;
		SetDefaultSlotExport(storedThings.DynamicThing);
		DynamicThing storedThing = GetStoredThing(storedThings.DynamicThing);
		_spawningThings = storedThings;
		_spawnChildIndex = 0;
		_spawnChildTimer = 0f;
		if (storedThing is INutrition nutrition && nutrition is Item { CanDecay: not false } item && !(item is Seed))
		{
			storedThing.DamageState.Damage(ChangeDamageType.Increment, storedThing.DamageState.MaxDamage + 1f, DamageUpdateType.Decay);
		}
		UpdateTotalItemsCurrentlyStored();
	}

	private void TickSpawnExport(float deltaTime)
	{
		if (_spawningThings == null)
		{
			return;
		}
		_spawnChildTimer -= deltaTime;
		if (!(_spawnChildTimer > 0f))
		{
			if (_spawnChildIndex < _spawningThings.StoredChildren.Count)
			{
				GetStoredThing(_spawningThings.StoredChildren[_spawnChildIndex]);
				_spawnChildIndex++;
				_spawnChildTimer = 0.2f;
			}
			else
			{
				_spawningThings = null;
				_doneSpawning = true;
			}
		}
	}

	private DynamicThing GetStoredThing(DynamicThingSaveData saveData)
	{
		if (saveData == null)
		{
			return null;
		}
		if (_changedIds.TryGetValue(saveData.ParentReferenceId, out var value))
		{
			saveData.ParentReferenceId = value;
		}
		long referenceId = saveData.ReferenceId;
		saveData.ReferenceId = 0L;
		DynamicThing dynamicThing = XmlSaveLoad.Load<DynamicThing>(saveData);
		if (!dynamicThing)
		{
			return null;
		}
		if (dynamicThing.ReferenceId == 0L)
		{
			Referencable.RegisterNew(dynamicThing);
			if (NetworkManager.IsServer && NetworkBase.Clients.Count > 0)
			{
				Thing.NewToSend.Add(dynamicThing);
			}
		}
		_changedIds[referenceId] = dynamicThing.ReferenceId;
		return dynamicThing;
	}

	public void SetDefaultSlotExport(DynamicThingSaveData dynamicThingSaveData)
	{
		dynamicThingSaveData.WorldPosition = ExportSlot.Location.position;
		dynamicThingSaveData.WorldRotation = ExportSlot.Location.rotation;
		dynamicThingSaveData.ParentReferenceId = ExportSlot.Parent.ReferenceId;
		dynamicThingSaveData.ParentSlotId = ExportSlot.SlotIndex;
	}

	private void TryProcessImport()
	{
		if (ImportingThing != null && base.IsImportClosed && !IsFull && _doneSaving)
		{
			_doneSaving = false;
			SaveThings(ImportingThing);
		}
	}

	private void SaveThings(DynamicThing dynamicThing)
	{
		StoredThings savingThings = new StoredThings
		{
			StoredChildren = new List<DynamicThingSaveData>(),
			DynamicThing = ImportingThing.ToSiloData()
		};
		_savingThings = savingThings;
		_savingRoot = dynamicThing;
		_saveStack.Clear();
		_saveDestroyList.Clear();
		PushChildrenForSave(dynamicThing.Slots);
	}

	private void PushChildrenForSave(List<Slot> slots)
	{
		if (slots == null)
		{
			return;
		}
		for (int num = slots.Count - 1; num >= 0; num--)
		{
			DynamicThing occupant = slots[num].Occupant;
			if (occupant != null)
			{
				_saveStack.Push(occupant);
			}
		}
	}

	private void TickSaveImport()
	{
		if (_savingThings == null)
		{
			return;
		}
		if (_saveStack.Count > 0)
		{
			DynamicThing dynamicThing = _saveStack.Pop();
			if (dynamicThing != null && dynamicThing.gameObject != null)
			{
				_savingThings.StoredChildren.Add(dynamicThing.ToSiloData());
				UpdateTotalItemsCurrentlyStored();
				PushChildrenForSave(dynamicThing.Slots);
				_saveDestroyList.Add(dynamicThing);
			}
		}
		else if (_saveDestroyList.Count > 0)
		{
			int index = _saveDestroyList.Count - 1;
			DynamicThing thing = _saveDestroyList[index];
			_saveDestroyList.RemoveAt(index);
			DestroySavedThing(thing);
		}
		else
		{
			DestroySavedThing(_savingRoot);
			_savingRoot = null;
			_storedItems.Enqueue(_savingThings);
			_stackDirty = true;
			_savingThings = null;
			UpdateTotalItemsCurrentlyStored();
			_doneSaving = true;
		}
	}

	private void DestroySavedThing(DynamicThing thing)
	{
		if (!(thing == null) && !(thing.gameObject == null))
		{
			OnServer.Destroy(thing);
			if ((bool)thing.gameObject)
			{
				UnityEngine.Object.Destroy(thing.gameObject);
			}
		}
	}

	public void UpdateTotalItemsCurrentlyStored()
	{
		if (GameManager.RunSimulation)
		{
			TotalItemsCurrentlyStored = SiloThingQuantity;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == InfoPanel && InfoPanel != null)
		{
			result.Title = Localization.GetInterface("Contents");
			result.State = GameStrings.SiloItemCount.AsString(StringManager.Get(TotalItemsCurrentlyStored), StringManager.Get(600)) + "\n";
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SiloSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SiloSaveData siloSaveData)
		{
			_storedItems = new Queue<StoredThings>(siloSaveData.AllStoredItems);
			_stackDirty = true;
			UpdateTotalItemsCurrentlyStored();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		savedData.LogicStack = null;
		if (savedData is SiloSaveData siloSaveData)
		{
			siloSaveData.AllStoredItems = new List<StoredThings>(_storedItems);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => true, 
			LogicType.Dispense => true, 
			LogicType.DispenseSlot => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Dispense => true, 
			LogicType.DispenseSlot => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => SiloThingQuantity, 
			LogicType.Dispense => _dispense ? 1.0 : 0.0, 
			LogicType.DispenseSlot => _dispenseSlot, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Dispense:
			_dispense = value >= 1.0;
			break;
		case LogicType.DispenseSlot:
			_dispenseSlot = (int)value;
			break;
		default:
			base.SetLogicValue(logicType, value);
			break;
		}
	}

	public int GetStackSize()
	{
		return _stack?.Size ?? 0;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.SiloInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		if (EnumCollections.SiloInstructions[i] == SiloInstruction.SlotContents)
		{
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Quantity", 13, "UINT13"), new LogicStack.InstructionFormat("Prefab_Hash", typeof(uint)));
		}
		throw new NotImplementedException();
	}

	public void OnLogicTick()
	{
		if (GameManager.RunSimulation && _stackDirty)
		{
			_stackDirty = false;
			RebuildContentsStack();
		}
	}

	private void RebuildContentsStack()
	{
		if (_stack == null)
		{
			return;
		}
		_stack.Clear();
		int num = 0;
		foreach (StoredThings storedItem in _storedItems)
		{
			if (num >= _stack.Size)
			{
				break;
			}
			DynamicThingSaveData dynamicThingSaveData = storedItem?.DynamicThing;
			if (dynamicThingSaveData != null && !string.IsNullOrEmpty(dynamicThingSaveData.PrefabName))
			{
				int @int = Animator.StringToHash(dynamicThingSaveData.PrefabName);
				int num2 = ((!(dynamicThingSaveData is StackableSaveData stackableSaveData)) ? 1 : stackableSaveData.Quantity);
				if (num2 < 0)
				{
					num2 = 0;
				}
				if (num2 > 8191)
				{
					num2 = 8191;
				}
				_stack[num++] = LogicStack.PackOpcodeUInt13Int32(1, (ushort)num2, @int);
			}
		}
	}
}
