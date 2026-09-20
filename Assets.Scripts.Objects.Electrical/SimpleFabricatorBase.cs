using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Effects;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DLC;
using Objects;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class SimpleFabricatorBase : FabricatorBase, IPrefabHash, IRequireReagent, IConsumesAllIngots, IResourceConsumer, IReferencable, IEvaluable, IMemoryReadable, IMemory, IMemoryWritable, IInstructable, ILogicTick, ILogicStack, ISubmergeable
{
	private readonly LogicStack _stack = new LogicStack(64);

	private FabricatorCreateStackAddress _createInstruction;

	private readonly StackPointerStackAddress _stackPointer = new StackPointerStackAddress(63, 0.0);

	private const int INVALID_SKIP = int.MinValue;

	private const int INVALID_WAIT = -1;

	private const int STACK_TRANSACTION_COUNT = 53;

	private const int STACK_SIZE = 64;

	private const int STACK_TRANSACTION_BEGIN = 0;

	private const int STACK_TRANSACTION_END = 53;

	private const int STACK_REQUEST_BEGIN = 54;

	private const int STACK_REQUEST_END = 62;

	private const int STACK_POINTER_ADDRESS = 63;

	public PrintingEffect PrintingEffect;

	private int _currentIndex;

	public MeshRenderer IconMaterial;

	private float _powerUsedDuringTick;

	private byte _processing;

	public bool QuickFabricate;

	public float QuickFabTime;

	private UniTask _productionTask;

	private Recipe _currentRecipe;

	private DynamicThing _currentResult;

	private DynamicThing _nextResult;

	private float _waitTime;

	private float _waitForClearExit;

	private float _timeSinceIgnite;

	private int _makingIndex;

	public static readonly int Previous4Hash = Animator.StringToHash("Previous4");

	public static readonly int Previous4BeepHash = Animator.StringToHash("Previous4Beep");

	public static readonly int Next4Hash = Animator.StringToHash("Next4");

	public static readonly int Next4BeepHash = Animator.StringToHash("Next4Beep");

	public static readonly int ActivateOnHash = Animator.StringToHash("ActivateOn");

	public static readonly int ActivateOffHash = Animator.StringToHash("ActivateOff");

	public static readonly int ActivateOffBeepHash = Animator.StringToHash("ActivateOffBeep");

	public static readonly int ActivateOnBeepHash = Animator.StringToHash("ActivateOnBeep");

	public static readonly int Search4Hash = Animator.StringToHash("Search4");

	public static readonly int Search4BeepHash = Animator.StringToHash("Search4Beep");

	public static readonly int Select4BeepHash = Animator.StringToHash("Select4Beep");

	public static readonly int ErrorBeepHash = Animator.StringToHash("ErrorBeep");

	public static readonly int Button5Hash = Animator.StringToHash("Button5");

	protected static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

	public MachineTier CurrentTier => base.CurrentBuildState.ManufactureDat.MachinesTier;

	public virtual Dictionary<DynamicThing, Recipe> Recipes => null;

	public virtual Dictionary<MachineTier, List<DynamicThing>> DynamicThings => null;

	public virtual List<DynamicThing> ValidDynamicThings => null;

	public virtual Dictionary<int, int> PrefabTypeLookup => null;

	[ByteArraySync]
	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			_currentIndex = value;
			InitializePrintingEffect();
			this.OnRecipeChanged?.Invoke();
			ToggleIcon();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public bool IsReagentUser => true;

	public Recipe RequiredReagents => CurrentRecipe.GetMissingReagents(ReagentMixture);

	public Recipe CurrentRecipe => GetRecipe();

	public DynamicThing CurrentProduct => GetProduct((Activate == 1) ? _makingIndex : CurrentIndex);

	public int CurrentHash
	{
		get
		{
			if (!CurrentProduct)
			{
				return 0;
			}
			return CurrentProduct.PrefabHash;
		}
		set
		{
			SetRecipeFromHash(value);
		}
	}

	public byte Processing
	{
		get
		{
			return _processing;
		}
		set
		{
			if (value != Processing)
			{
				_processing = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public float ManufactureTimeMultiplier => base.CurrentBuildState.ManufactureDat.BuildTimeMultiplier;

	public bool IsValid => !base.IsBeingDestroyed;

	public bool DoSubmergableTick => true;

	public event Event OnRecipeChanged;

	public int GetStackSize()
	{
		return 64;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public void WriteMemory(int address, double value)
	{
		_stack[address] = value;
	}

	public void ClearMemory()
	{
		_stack.Clear();
		_createInstruction = null;
		_stackPointer.Set(0);
		_stackPointer.Write(_stack);
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.PrinterInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		return EnumCollections.PrinterInstructions[i] switch
		{
			PrinterInstruction.DeviceSetLock => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Lock_State", typeof(bool))), 
			PrinterInstruction.StackPointer => LogicStack.FormatInstruction(63, LogicStack.OpCode, new LogicStack.InstructionFormat("Index", typeof(ushort))), 
			PrinterInstruction.ExecuteRecipe => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Quantity", typeof(byte)), new LogicStack.InstructionFormat("Prefab_Hash", typeof(int))), 
			PrinterInstruction.WaitUntilNextValid => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode), 
			PrinterInstruction.JumpIfNextInvalid => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Stack_Address", typeof(ushort))), 
			PrinterInstruction.EjectAllReagents => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode), 
			PrinterInstruction.EjectReagent => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Reagent_Hash", typeof(int))), 
			PrinterInstruction.MissingRecipeReagent => LogicStack.FormatInstruction(54, 62, LogicStack.OpCode, new LogicStack.InstructionFormat("Quantity_Ceil", typeof(byte)), new LogicStack.InstructionFormat("Reagent_Hash", typeof(uint))), 
			PrinterInstruction.JumpToAddress => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Stack_Address", typeof(ushort))), 
			_ => throw new NotImplementedException(), 
		};
	}

	private void ClearAndAdvance()
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		WriteMemory(_createInstruction.StackIndex, 0.0);
		_createInstruction = null;
		AdvanceStack();
	}

	private void SkipAndAdvance()
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		_createInstruction = null;
		AdvanceStack();
	}

	private void SkipAndJump(ushort address)
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		_createInstruction = null;
		_stackPointer.Set(address);
		_stackPointer.Write(_stack);
	}

	public void OnLogicTick()
	{
		if (!GameManager.RunSimulation || !OnOff || !Powered || !base.IsStructureCompleted || IsBusy())
		{
			return;
		}
		if (_createInstruction != null)
		{
			_createInstruction.Update();
			if (_createInstruction.Quantity == 0)
			{
				ClearAndAdvance();
				return;
			}
			if (Activate == 0)
			{
				if (!ReagentMixture.Contains(CurrentRecipe))
				{
					if (_createInstruction.InvalidHandler == int.MinValue)
					{
						SkipAndAdvance();
					}
					else if (_createInstruction.InvalidHandler >= 0)
					{
						SkipAndJump((ushort)_createInstruction.InvalidHandler);
					}
					else
					{
						CurrentRecipe.GetMissingReagents(ReagentMixture).PopulateInto(9, _stack, 54, 62, _createInstruction.Quantity);
					}
				}
				else
				{
					_stack.Clear(54, 62);
					OnServer.Interact(base.InteractActivate, 1);
				}
				return;
			}
		}
		int num = 0;
		int invalidHandler = int.MinValue;
		while (num < 53)
		{
			num++;
			StackAddress input = new StackAddress(_stackPointer, ReadMemory(_stackPointer));
			if (input.Opcode == 0)
			{
				AdvanceStack();
				continue;
			}
			switch ((PrinterInstruction)input.Opcode)
			{
			case PrinterInstruction.JumpToAddress:
			{
				(byte, ushort) tuple3 = LogicStack.UnpackUInt16(input.IntegerValue);
				_stackPointer.Set(ClampAddress(tuple3.Item2));
				_stackPointer.Write(_stack);
				return;
			}
			case PrinterInstruction.DeviceSetLock:
			{
				(byte, bool) tuple = LogicStack.UnpackBool(input.IntegerValue);
				OnServer.Interact(base.InteractLock, tuple.Item2 ? 1 : 0);
				AdvanceStack();
				WriteMemory(input.StackIndex, 0.0);
				return;
			}
			case PrinterInstruction.WaitUntilNextValid:
				invalidHandler = -1;
				AdvanceStack();
				continue;
			case PrinterInstruction.JumpIfNextInvalid:
				invalidHandler = ClampAddress(LogicStack.UnpackUInt16(input.IntegerValue).Item2);
				AdvanceStack();
				continue;
			case PrinterInstruction.ExecuteRecipe:
			{
				(byte, byte, int) tuple2 = LogicStack.UnpackByteInt32(input.IntegerValue);
				int item = tuple2.Item3;
				if (tuple2.Item2 > 0)
				{
					SetRecipeHashSafe(item, start: true);
					_createInstruction = new FabricatorCreateStackAddress(input, invalidHandler);
					return;
				}
				break;
			}
			case PrinterInstruction.EjectReagent:
			{
				Reagent reagent = Reagent.Find(LogicStack.UnpackInt32(input.IntegerValue).Item2);
				if (reagent != null && ReagentMixture.Contains(reagent))
				{
					if (IsNextExportReady && Activate == 0)
					{
						Drop(reagent).Forget();
					}
				}
				else
				{
					WriteMemory(input.StackIndex, 0.0);
					AdvanceStack();
				}
				return;
			}
			case PrinterInstruction.EjectAllReagents:
				if (!IsOpen)
				{
					OnServer.Interact(base.InteractOpen, 1);
				}
				if (!ReagentMixture.IsNotEmpty())
				{
					OnServer.Interact(base.InteractOpen, 0);
					WriteMemory(input.StackIndex, 0.0);
					AdvanceStack();
				}
				return;
			}
			invalidHandler = int.MinValue;
			AdvanceStack();
		}
		ReadMemory(_stackPointer);
		_ = 0.0;
	}

	private ushort ClampAddress(ushort address)
	{
		return (ushort)Mathf.Clamp(address, 0, 53);
	}

	private void AdvanceStack()
	{
		ushort index = _stackPointer.Index;
		index++;
		if (index > 53)
		{
			index = 0;
		}
		_stackPointer.Set(index);
		_stackPointer.Write(_stack);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None && !IsCursor)
		{
			CurrentJob = new FabricatorJob();
			if ((bool)IconMaterial)
			{
				ToggleIcon();
				OnDeviceConnectToNetworkEvent = (Event)Delegate.Combine(OnDeviceConnectToNetworkEvent, new Event(ToggleIcon));
			}
		}
	}

	public virtual void GetValidDynamicThings(ref List<DynamicThing> list)
	{
		for (int i = 0; i < 4 && i <= (int)CurrentTier; i++)
		{
			if (DynamicThings.ContainsKey((MachineTier)i))
			{
				list.AddRange(DynamicThings[(MachineTier)i]);
			}
		}
		if ((bool)IconMaterial)
		{
			IconMaterial.transform.gameObject.SetActive(CurrentTier == MachineTier.TierTwo);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Fabricators);
	}

	public int GetPrefabHashFromReagentHash(int reagentHash)
	{
		Reagent reagentType = Reagent.Find(reagentHash);
		foreach (Ingot allIngotPrefab in Ingot.AllIngotPrefabs)
		{
			if (allIngotPrefab.CreatedReagentMixture.Contains(reagentType))
			{
				return allIngotPrefab.PrefabHash;
			}
		}
		return 0;
	}

	private void InitializePrintingEffect()
	{
		if ((bool)PrintingEffect)
		{
			PrintingEffect.EffectMaterial.color = Color.white;
			PrintingEffect.SetBlueprint(CurrentProduct);
		}
	}

	public DynamicThing GetProduct(int index)
	{
		List<DynamicThing> validDynamicThings = ValidDynamicThings;
		if (validDynamicThings == null || validDynamicThings.Count == 0)
		{
			return null;
		}
		if (index >= validDynamicThings.Count || index < 0)
		{
			index = 0;
			RefreshDynamicThings();
		}
		return validDynamicThings[index];
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SimpleFabricatorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SimpleFabricatorSaveData simpleFabricatorSaveData)
		{
			CurrentIndex = simpleFabricatorSaveData.CurrentIndex;
			CurrentJob = simpleFabricatorSaveData.FabricatorJob;
		}
		if (CurrentIndex >= ValidDynamicThings.Count)
		{
			CurrentIndex = 0;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SimpleFabricatorSaveData simpleFabricatorSaveData)
		{
			simpleFabricatorSaveData.FabricatorJob = CurrentJob;
			simpleFabricatorSaveData.CurrentIndex = CurrentIndex;
		}
	}

	public override void UpdateStateVisualizer(bool visualOnly = false)
	{
		base.UpdateStateVisualizer(visualOnly);
		ToggleIcon();
	}

	protected override void UpdateSoundsOnBuildState(bool construct)
	{
		if (OnOff)
		{
			OnServer.Interact(this, InteractableType.OnOff, 0);
		}
		base.UpdateSoundsOnBuildState(construct);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		InitializePrintingEffect();
	}

	public void ToggleIcon()
	{
		if (!IsCursor)
		{
			AwaitToggleIcon().Forget();
		}
	}

	private async UniTaskVoid AwaitToggleIcon()
	{
		await UniTask.SwitchToMainThread();
		SetIcon();
	}

	protected virtual void SetIcon()
	{
		if ((bool)IconMaterial && !base.BeingDestroyed && base.CurrentBuildStateIndex >= 0)
		{
			bool active = BuildStates[base.CurrentBuildStateIndex].ManufactureDat.MachinesTier == MachineTier.TierTwo && OnOff;
			IconMaterial.transform.gameObject.SetActive(active);
			if (ValidDynamicThings != null && CurrentIndex < ValidDynamicThings.Count && CurrentIndex >= 0)
			{
				Material material = IconMaterial.material;
				material.mainTexture = ValidDynamicThings[CurrentIndex].Thumbnail.texture;
				IconMaterial.material.SetTexture(EmissionMap, material.mainTexture);
			}
		}
	}

	public override void UpdateEachFrame()
	{
		if (!GameManager.IsBatchMode && !WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (OnOff && Powered && Activate == 1 && !IsOccluded && (bool)PrintingEffect)
			{
				PrintingEffect.RefreshMaterialSettings();
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!GameManager.IsBatchMode && interactable == base.InteractActivate && (bool)PrintingEffect)
		{
			if (interactable.State == 1)
			{
				PrintingEffect.gameObject.SetActive(value: true);
				PrintingEffect.SetBlueprint(CurrentProduct);
			}
			else
			{
				PrintingEffect.SetBlueprint(null);
				PrintingEffect.gameObject.SetActive(value: false);
			}
		}
	}

	private void SetRecipeFromHash(int hash)
	{
		int num = -1;
		for (int i = 0; i < ValidDynamicThings.Count; i++)
		{
			if (ValidDynamicThings[i].PrefabHash == hash)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			if (_createInstruction != null)
			{
				_createInstruction = null;
				AdvanceStack();
			}
		}
		else
		{
			CurrentIndex = num;
		}
	}

	private async UniTaskVoid SetRecipeFromHashFromThread(int hash, bool start = false)
	{
		await UniTask.SwitchToMainThread();
		SetRecipeFromHash(hash);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.RecipeHash)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.RecipeHash)
		{
			SetRecipeHashSafe((int)value);
		}
	}

	private void SetRecipeHashSafe(int value, bool start = false)
	{
		if (CurrentHash != value || start)
		{
			if (GameManager.IsThread)
			{
				SetRecipeFromHashFromThread(value, start).Forget();
			}
			else
			{
				SetRecipeFromHash(value);
			}
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.RecipeHash => true, 
			LogicType.CompletionRatio => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.RecipeHash => CurrentHash, 
			LogicType.CompletionRatio => Mathf.Clamp01((float)(int)Processing / 100f), 
			_ => base.GetLogicValue(logicType), 
		};
	}

	protected override void OnServerExportTick()
	{
		if (!OnOff || !Powered || !base.IsStructureCompleted)
		{
			return;
		}
		if (IsOpen && IsNextExportReady && ReagentMixture.TotalReagents > 0.0 && Activate == 0)
		{
			Item item = DropReagent(ExportSlot);
			if (item != null)
			{
				OnServer.MoveToSlot(item, ExportSlot);
			}
		}
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	private async UniTaskVoid Drop(Reagent reagentType)
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (!base.IsBeingDestroyed && IsNextExportReady && Activate == 0 && ReagentMixture.Contains(reagentType))
		{
			Item item = DropReagent(reagentType, ExportSlot);
			if ((object)item != null)
			{
				OnServer.MoveToSlot(item, ExportSlot);
			}
		}
	}

	public void Exported()
	{
		if (_createInstruction != null && Activate == 1 && GameManager.RunSimulation)
		{
			_createInstruction.Decrement();
			_createInstruction.Write(_stack);
			if (_createInstruction.Quantity == 0)
			{
				WriteMemory(_createInstruction.StackIndex, 0.0);
				_createInstruction = null;
				OnServer.Interact(base.InteractActivate, 0);
				AdvanceStack();
			}
		}
	}

	private bool IsBusy()
	{
		if (Activate != 1 && (!IsOpen || !ReagentMixture.IsNotEmpty()))
		{
			return ExportSlot.IsNotEmpty();
		}
		return true;
	}

	public void OnBeginSmelt()
	{
		if (GameManager.RunSimulation && OnOff && Powered && !ExportSlot.Occupant && _productionTask.Status != UniTaskStatus.Pending && base.IsStructureCompleted)
		{
			_productionTask = WaitThenMake();
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if ((object)base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
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

	public virtual int CompletedProductionSoundHash()
	{
		return 0;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte(Processing);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteInt32(_makingIndex);
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(CurrentIndex);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Processing = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			_makingIndex = reader.ReadInt32();
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentIndex = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(Processing);
		writer.WriteInt32(CurrentIndex);
		writer.WriteInt32(_makingIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Processing = reader.ReadByte();
		CurrentIndex = reader.ReadInt32();
		_makingIndex = reader.ReadInt32();
	}

	private async UniTask WaitThenMake()
	{
		_currentRecipe = GetRecipe();
		_currentResult = GetProduct(CurrentIndex);
		_makingIndex = CurrentIndex;
		base.NetworkUpdateFlags |= 16384;
		_nextResult = _currentResult;
		_waitTime = (QuickFabricate ? QuickFabTime : (_currentRecipe.Time * ManufactureTimeMultiplier));
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		_timeSinceIgnite = 0f;
		while ((bool)_currentResult && _currentResult.PrefabHash == _nextResult.PrefabHash && OnOff && Powered && base.CurrentBuildState.CanManufacture && Error == 0)
		{
			float complete = 0f;
			_waitForClearExit = 0f;
			while (base.IsExportChuteBlocked)
			{
				_waitForClearExit += Time.deltaTime;
				if (_waitForClearExit > 2f)
				{
					OnServer.Interact(base.InteractActivate, 0);
					break;
				}
				await UniTask.NextFrame(cancelToken);
				if (cancelToken.IsCancellationRequested)
				{
					return;
				}
			}
			while (complete < _waitTime && OnOff && Powered && Activate == 1)
			{
				complete += Time.deltaTime;
				Processing = (byte)(Mathf.Clamp01(complete / _waitTime) * 100f);
				_powerUsedDuringTick += _currentRecipe.Energy * base.CurrentBuildState.ManufactureDat.EnergyCostMultiplier * (Time.deltaTime / _waitTime);
				_timeSinceIgnite += Time.deltaTime;
				if (_timeSinceIgnite >= 1f)
				{
					base.AtmosphericsController.IgniteAtmosphere(base.WorldGrid, new MoleEnergy(_powerUsedDuringTick * 0.25f));
					_timeSinceIgnite = 0f;
				}
				await UniTask.NextFrame(cancelToken);
				if (cancelToken.IsCancellationRequested)
				{
					return;
				}
			}
			if (Activate == 0)
			{
				break;
			}
			if (complete >= _waitTime && OnOff && Powered && ReagentMixture.Contains(_currentRecipe))
			{
				ReagentMixture.Subtract(_currentRecipe, base.CurrentBuildState.ManufactureDat.MaterialCostMultiplier);
				SpawnCreatedItems(_currentResult, base.CurrentBuildState.ManufactureDat.ItemSpawnMultiplier);
				Achievements.Achieve121Gigawatts(_currentResult);
				if (CompletedProductionSoundHash() != 0)
				{
					PlayNetworkSound(CompletedProductionSoundHash());
				}
			}
			if (!ReagentMixture.Contains(_currentRecipe))
			{
				break;
			}
			while (OnOff && Powered && !IsNextExportReady)
			{
				await UniTask.NextFrame(cancelToken);
			}
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			_nextResult = GetProduct(CurrentIndex);
		}
		OnServer.Interact(base.InteractActivate, 0);
	}

	public void SpawnCreatedItems(Thing thing, int num)
	{
		for (int i = 0; i < num; i++)
		{
			DynamicThing dynamicThing = Thing.Create<DynamicThing>(_currentResult, ExportSlot.Location.position, ExportSlot.Location.rotation, 0L);
			dynamicThing.name = _currentResult.name;
			OnServer.MoveToSlot(dynamicThing, ExportSlot);
			DynamicThing.ItemManufactured(dynamicThing, num);
			Exported();
			if ((bool)(dynamicThing as Stackable))
			{
				break;
			}
		}
	}

	public void ResetWaitTime()
	{
		_waitTime = _currentRecipe.Time * ManufactureTimeMultiplier;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable == null)
		{
			return " Null Interactable";
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (GetProduct(CurrentIndex) == null)
			{
				RefreshDynamicThings();
			}
			if (GetProduct(CurrentIndex) == null)
			{
				return ActionStrings.Clear + " " + GameStrings.FabricatorUknownRecipe;
			}
			if (Activate != 0)
			{
				return ActionStrings.Clear + " " + CurrentProduct.DisplayName;
			}
			return ActionStrings.Build + " " + GetProduct(CurrentIndex).DisplayName;
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DynamicThing product = GetProduct(CurrentIndex);
			if (product == null)
			{
				return GameStrings.FabricatorUknownRecipe;
			}
			return product.DisplayName;
		}
		if (interactable.Action == InteractableType.Button3)
		{
			return GameStrings.FabricatorFindRecipe;
		}
		return base.GetContextualName(interactable);
	}

	public Recipe GetRecipe(int index)
	{
		Recipe value = default(Recipe);
		DynamicThing product = GetProduct(index);
		if (product == null)
		{
			Recipes.TryGetValue(GetProduct(0), out value);
			return value;
		}
		Recipes.TryGetValue(product, out value);
		return value;
	}

	public Recipe GetRecipe()
	{
		return GetRecipe(CurrentIndex);
	}

	public Recipe GetRecipeSafe(DynamicThing prefab)
	{
		List<DynamicThing> validDynamicThings = ValidDynamicThings;
		if (validDynamicThings == null || validDynamicThings.Count == 0)
		{
			return Recipe.INVALID;
		}
		for (int i = 0; i < validDynamicThings.Count; i++)
		{
			if (validDynamicThings[i].PrefabHash == prefab.PrefabHash)
			{
				return GetRecipe(i);
			}
		}
		return Recipe.INVALID;
	}

	public Recipe GetRecipeSafe(int index)
	{
		try
		{
			return GetRecipe(index);
		}
		catch (Exception)
		{
			return Recipe.INVALID;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		InteractableType action = interactable.Action;
		if (action == InteractableType.Activate || action == InteractableType.Button1 || action == InteractableType.Button2 || action == InteractableType.Button3)
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
			Recipe recipe;
			try
			{
				recipe = GetRecipe();
			}
			catch (Exception)
			{
				return delayedActionInstance.Fail(GameStrings.ThingClearUnknownRecipe);
			}
			if (interactable.Action == InteractableType.Activate)
			{
				if (!OnOff)
				{
					if (doAction)
					{
						PlaySound(IsTierTwo() ? Button5Hash : ActivateOnHash);
					}
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					if (doAction)
					{
						PlaySound(IsTierTwo() ? Button5Hash : ActivateOnHash);
					}
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				DynamicThing product = GetProduct(CurrentIndex);
				if ((object)product != null && !SharedDLCManager.CheckSharedAccess(product.DLCType))
				{
					return delayedActionInstance.Fail(GameStrings.RequireDlcToFabricate);
				}
				string comparisonResult = recipe.GetComparisonResult(ReagentMixture);
				if (comparisonResult != string.Empty)
				{
					delayedActionInstance.ExtendedMessage = comparisonResult;
				}
				if (Activate == 1)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.ThingCreateThing, ((object)CurrentProduct != null) ? CurrentProduct.ToTooltip() : "???", StringManager.Get(Processing));
				}
				if (!ReagentMixture.Contains(recipe) && Activate == 0)
				{
					if (doAction)
					{
						PlaySound(IsTierTwo() ? Button5Hash : ActivateOnHash);
						PlaySound(ErrorBeepHash);
					}
					return delayedActionInstance.Fail(GameStrings.ThingCanNotManufacture, ((object)CurrentProduct != null) ? CurrentProduct.ToTooltip() : "???");
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				if (Activate == 1)
				{
					PlaySound(IsTierTwo() ? Button5Hash : ActivateOffHash);
				}
				else
				{
					PlaySound(IsTierTwo() ? Button5Hash : ActivateOnHash);
				}
				PlaySound((Activate == 1) ? ActivateOffBeepHash : ActivateOnBeepHash);
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractActivate, (Activate != 1) ? 1 : 0);
				}
				return delayedActionInstance.Succeed();
			}
			if (interactable.Action == InteractableType.Button1)
			{
				if (doAction)
				{
					PlaySound(IsTierTwo() ? Button5Hash : Previous4Hash);
				}
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!ReagentMixture.Contains(recipe))
				{
					delayedActionInstance.ExtendedMessage = GameStrings.ThingNotEnoughReagents.DisplayString;
				}
				int num = RocketMath.Wrap(CurrentIndex - 1, 0, ValidDynamicThings.Count - 1);
				DynamicThing dynamicThing = ValidDynamicThings[num];
				delayedActionInstance.AppendStateMessage(GameStrings.ThingCycleTo, dynamicThing.ToTooltip());
				delayedActionInstance.ActionMessage = Localization.GetThingName(dynamicThing.PrefabName);
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlaySound(Previous4BeepHash);
				if (GameManager.RunSimulation)
				{
					CurrentIndex = num;
				}
				return delayedActionInstance.Succeed();
			}
			if (interactable.Action == InteractableType.Button2)
			{
				if (doAction)
				{
					PlaySound(IsTierTwo() ? Button5Hash : Next4Hash);
				}
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!ReagentMixture.Contains(recipe))
				{
					delayedActionInstance.ExtendedMessage = GameStrings.ThingNotEnoughReagents.DisplayString;
				}
				int num2 = RocketMath.Wrap(CurrentIndex + 1, 0, ValidDynamicThings.Count - 1);
				DynamicThing dynamicThing2 = ValidDynamicThings[num2];
				delayedActionInstance.AppendStateMessage(GameStrings.ThingCycleTo, dynamicThing2.ToTooltip());
				delayedActionInstance.ActionMessage = Localization.GetThingName(dynamicThing2.PrefabName);
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlaySound(Next4BeepHash);
				if (GameManager.RunSimulation)
				{
					CurrentIndex = num2;
				}
				return delayedActionInstance.Succeed();
			}
			if (interactable.Action == InteractableType.Button3)
			{
				if (doAction)
				{
					PlaySound(IsTierTwo() ? Button5Hash : Search4Hash);
				}
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.ThingSelectRecipeFromList);
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				Human human = interaction.SourceThing as Human;
				PlaySound(Search4BeepHash);
				if (human != null && human.State == EntityState.Alive && human.OrganBrain != null && human.OrganBrain.LocalControl && InputPrefabs.ShowInputPanel(GameStrings.InputPanelSelectRecipe, null, DynamicThings, this))
				{
					InputPrefabs.OnSubmit += InputFinished;
					StartCoroutine(WaitForDropdown());
				}
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public bool IsTierTwo()
	{
		if (base.CurrentBuildState != null)
		{
			return CurrentTier == MachineTier.TierTwo;
		}
		return false;
	}

	public void InputFinished(DynamicThing prefab)
	{
		int num = ValidDynamicThings.IndexOf(prefab);
		if (num >= 0)
		{
			if (GameManager.RunSimulation)
			{
				OnServer.SetRecipe(base.ReferenceId, num);
			}
			if (NetworkManager.IsClient)
			{
				NetworkClient.SetRecipe(base.ReferenceId, num);
			}
			ToggleIcon();
			PlaySound(Select4BeepHash);
		}
	}

	private IEnumerator WaitForDropdown()
	{
		while (InputPrefabs.InputState == InputPanelState.Waiting)
		{
			if (!Powered)
			{
				InputWindowBase.Cancel();
			}
			yield return null;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		InitializePrintingEffect();
	}

	public virtual List<Item> GetResourcesUsed()
	{
		return new List<Item>(Ingot.AllIngotPrefabs);
	}

	public virtual bool CanProcess(Recipe recipe)
	{
		foreach (Ingot allIngotPrefab in Ingot.AllIngotPrefabs)
		{
			if (allIngotPrefab.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanProcess(Reagent reagentType)
	{
		foreach (Ingot allIngotPrefab in Ingot.AllIngotPrefabs)
		{
			if (allIngotPrefab.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	public void OnSubmergeableTick()
	{
		if (_inputPowerConnection != null)
		{
			bool flag = AtmosphereHelper.IsSubmerged(_inputPowerConnection.LocalGrid.ToVector3());
			if (Error == 0 && flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error != 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}
}
