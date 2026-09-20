using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class LogicMotherboard : Motherboard, IRocketReaderWriter
{
	[Header("Logic Controller")]
	public List<LogicState> LogicStates = new List<LogicState>();

	public VerticalLayoutGroup StateGroup;

	public Button ScreenButtonNewState;

	[NonSerialized]
	public LogicState CurrentLogicState;

	[Header("Screen Prefabs")]
	public GameObject StateIconPrefab;

	public ScreenState StatePrefab;

	public ScreenCondition ConditionPrefab;

	public ScreenAction ActionPrefab;

	public bool IsPaused;

	public List<Device> DisplayedDevices = new List<Device>();

	public List<Dropdown.OptionData> DeviceOptionList = new List<Dropdown.OptionData>();

	private sbyte _deleteCategory = -1;

	private int _deleteIndex = -1;

	private int _logicStateIndex = -1;

	public override bool IsOperable => true;

	public Device GetListDevice(int index)
	{
		if (index <= DisplayedDevices.Count - 1)
		{
			return DisplayedDevices[index];
		}
		return null;
	}

	private void RebuildLogicList()
	{
		List<Dropdown.OptionData> options = LogicStates.Select((LogicState x) => new Dropdown.OptionData(x.DisplayName)).ToList();
		foreach (LogicState logicState in LogicStates)
		{
			logicState.ScreenState.NextStateDropdown.options = options;
			logicState.ScreenState.FalseStateDropdown.options = options;
			int num = LogicStates.FindIndex((LogicState s) => s == logicState.NextState);
			int num2 = LogicStates.FindIndex((LogicState f) => f == logicState.FalseState);
			logicState.ScreenState.NextStateDropdown.value = ((num < 0) ? logicState.Index : num);
			logicState.ScreenState.FalseStateDropdown.value = ((num2 < 0) ? logicState.Index : num2);
			logicState.Refresh();
		}
	}

	private void RebuildDeviceList()
	{
		DeviceOptionList.Clear();
		if (ParentComputer == null || !ParentComputer.DataCable || ParentComputer.DataCable.CableNetwork == null)
		{
			DisplayedDevices.Clear();
		}
		else
		{
			DisplayedDevices = new List<Device>(ParentComputer.DataCable.CableNetwork.DataDeviceList);
			for (int i = 0; i < DisplayedDevices.Count; i++)
			{
				DeviceOptionList.Add(new Dropdown.OptionData(DisplayedDevices[i].DisplayName));
			}
		}
		foreach (LogicState logicState in LogicStates)
		{
			logicState.OnDeviceListUpdated();
		}
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (GameManager.GameState != GameState.Running || ParentComputer == null || !ParentComputer.AsThing().OnOff || !ParentComputer.AsThing().Powered)
		{
			return;
		}
		int count = LogicStates.Count;
		while (count-- > 0)
		{
			LogicState logicState = LogicStates[count];
			lock (logicState.Conditions)
			{
				int count2 = logicState.Conditions.Count;
				while (count2-- > 0)
				{
					LogicCondition logicCondition = logicState.Conditions[count2];
					if (!logicCondition.IsDisconnected)
					{
						logicCondition.Assess();
					}
				}
			}
		}
		if (CurrentLogicState != null && CurrentLogicState.Enabled && !IsPaused)
		{
			CurrentLogicState.IsTriggered = CurrentLogicState.Conditions.Count >= 0 && CurrentLogicState.Conditions.FindIndex((LogicCondition c) => !c.IsTrue || c.IsDisconnected) == -1;
			if (!CurrentLogicState.IsTriggered && CurrentLogicState.FalseState != CurrentLogicState)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(CurrentLogicState.OnTriggered());
			}
		}
	}

	public void RemoveCondition(int logicStateIndex, int conditionIndex)
	{
		ConsoleWindow.Print($"Condition Removed: {logicStateIndex} | {conditionIndex}");
		LogicStates[logicStateIndex].RemoveCondition(conditionIndex);
	}

	public void RemoveAction(int logicStateIndex, int actionIndex)
	{
		ConsoleWindow.Print($"Action Removed: {logicStateIndex} | {actionIndex}");
		LogicStates[logicStateIndex].RemoveAction(actionIndex);
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (GameManager.GameState != GameState.Running || ParentComputer == null || ParentComputer.AsThing().IsOccluded || !ParentComputer.AsThing().OnOff || !ParentComputer.AsThing().Powered)
		{
			return;
		}
		foreach (LogicState logicState in LogicStates)
		{
			foreach (LogicCondition condition in logicState.Conditions)
			{
				condition.ScreenCondition.SetConditionState();
			}
		}
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		if (GameManager.GameState == GameState.Running)
		{
			RebuildDeviceList();
		}
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		if (GameManager.GameState == GameState.Running)
		{
			if (CurrentLogicState != null && CurrentLogicState.Enabled)
			{
				CurrentLogicState.IsTriggered = false;
			}
			RebuildDeviceList();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is LogicMotherboardSaveData logicMotherboardSaveData))
		{
			return;
		}
		foreach (LogicState logicState in LogicStates)
		{
			logicMotherboardSaveData.LogicStates.Add(new LogicStateSave(logicState));
		}
		if (LogicStates.Count > 0)
		{
			logicMotherboardSaveData.CurrentLogicIndex = CurrentLogicState?.Index ?? (-1);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicMotherboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (!(saveData is LogicMotherboardSaveData logicMotherboardSaveData))
		{
			return;
		}
		foreach (LogicStateSave logicState2 in logicMotherboardSaveData.LogicStates)
		{
			CreateNewState().Load(logicState2);
		}
		if (LogicStates.Count > 0 && logicMotherboardSaveData.CurrentLogicIndex >= 0)
		{
			SetCurrentLogicState(LogicStates[logicMotherboardSaveData.CurrentLogicIndex]);
		}
		for (int i = 0; i < LogicStates.Count; i++)
		{
			LogicState logicState = LogicStates[i];
			logicState.NextState = LogicStates[logicMotherboardSaveData.LogicStates[i].NextLogicIndex];
			logicState.FalseState = LogicStates[logicMotherboardSaveData.LogicStates[i].FalseLogicIndex];
		}
		StartCoroutine(DeserializeNextFrame(logicMotherboardSaveData));
	}

	public void Read(RocketBinaryReader reader)
	{
		int num = reader.ReadInt32();
		int logicStateIndex = reader.ReadInt32();
		sbyte b = reader.ReadSByte();
		int num2 = reader.ReadInt32();
		switch (b)
		{
		case 0:
			RemoveLogicState(num2);
			break;
		case 1:
			RemoveCondition(logicStateIndex, num2);
			break;
		case 2:
			RemoveAction(logicStateIndex, num2);
			break;
		}
		int num3 = reader.ReadInt32();
		for (int i = 0; i < num3; i++)
		{
			string displayName = reader.ReadString();
			int nextStateIndex = reader.ReadInt32();
			int falseStateIndex = reader.ReadInt32();
			AddOrUpdateState(i, displayName, nextStateIndex, falseStateIndex);
			LogicStates[i].Read(reader);
		}
		CurrentLogicState = ((num != -1) ? LogicStates[num] : null);
		RebuildLogicList();
		foreach (LogicState logicState in LogicStates)
		{
			logicState.OnDeviceListUpdated();
			logicState.Refresh();
		}
		RefreshScreen();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteInt32(CurrentLogicState?.Index ?? (-1));
		writer.WriteInt32(_logicStateIndex);
		writer.WriteSByte(_deleteCategory);
		writer.WriteInt32(_deleteIndex);
		int count = LogicStates.Count;
		writer.WriteInt32(count);
		foreach (LogicState logicState in LogicStates)
		{
			writer.WriteString(logicState.DisplayName);
			writer.WriteInt32(logicState.NextState.Index);
			writer.WriteInt32(logicState.FalseState.Index);
			logicState.Write(writer);
		}
		_deleteCategory = -1;
		_deleteIndex = -1;
		_logicStateIndex = -1;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Write(writer);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Read(reader);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Write(writer);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Read(reader);
		}
	}

	public void AddOrUpdateState(int index, string displayName, int nextStateIndex, int falseStateIndex)
	{
		LogicState logicState = ((index < LogicStates.Count) ? LogicStates[index] : CreateNewState());
		LogicState nextState = ((nextStateIndex < LogicStates.Count) ? LogicStates[nextStateIndex] : logicState);
		LogicState falseState = ((falseStateIndex < LogicStates.Count) ? LogicStates[falseStateIndex] : logicState);
		logicState.DisplayName = displayName;
		logicState.ScreenState.Title.text = displayName;
		logicState.NextState = nextState;
		logicState.FalseState = falseState;
		RebuildLogicList();
	}

	public void ProcessRefresh(int currentStateIndex)
	{
		if (currentStateIndex >= LogicStates.Count)
		{
			return;
		}
		SetCurrentLogicState(LogicStates[currentStateIndex]);
		foreach (LogicState logicState in LogicStates)
		{
			logicState.Refresh();
		}
	}

	public void AddOrNewCondition(int logicStateIndex, int conditionIndex, Thing device, LogicType logicType, ConditionOperation conditionOperation, double value)
	{
		LogicState logicState = LogicStates[logicStateIndex];
		LogicCondition logicCondition = ((conditionIndex < logicState.Conditions.Count) ? logicState.Conditions[conditionIndex] : CreateNewCondition(logicState));
		logicCondition.Device = device as Device;
		logicCondition.Type = logicType;
		logicCondition.Operation = conditionOperation;
		logicCondition.Value = value;
		if (GameManager.GameState == GameState.Running)
		{
			logicCondition.PopulateDevices(ref DeviceOptionList);
			logicCondition.ScreenCondition.RefreshAll();
		}
	}

	public void AddOrNewAction(int logicStateIndex, int actionIndex, Thing device, LogicType logicType, double value)
	{
		if (LogicStates.Count > logicStateIndex)
		{
			LogicState logicState = LogicStates[logicStateIndex];
			LogicAction logicAction = ((actionIndex < logicState.Actions.Count) ? logicState.Actions[actionIndex] : CreateNewAction(logicState));
			logicAction.Device = device as Device;
			logicAction.Type = logicType;
			logicAction.Value = value;
			if (GameManager.GameState == GameState.Running)
			{
				logicAction.PopulateDevices(ref DeviceOptionList);
				logicAction.ScreenAction.RefreshAll();
			}
		}
	}

	public void SendCurrentLogicState(LogicState logicState, LogicCondition logicCondition = null, LogicAction logicAction = null)
	{
		ConsoleWindow.Print($"LogicMotherboard UpdateType: {logicState}");
		if (GameManager.RunSimulation)
		{
			base.NetworkUpdateFlags |= 512;
		}
		if (NetworkManager.IsClient)
		{
			if (logicCondition != null)
			{
				NetworkClient.SendToServer(new LogicConditionMessage
				{
					DeviceId = logicCondition.Device.ReferenceId,
					LogicType = logicCondition.Type,
					ConditionOperation = logicCondition.Operation,
					Value = logicCondition.Value,
					MotherboardId = base.ReferenceId,
					LogicStateIndex = logicCondition.ParentState.Index,
					IsDeleting = (_deleteCategory == 1),
					Index = logicCondition.Index
				});
			}
			else if (logicAction != null)
			{
				NetworkClient.SendToServer(new LogicActionMessage
				{
					DeviceId = logicAction.Device.ReferenceId,
					LogicType = logicAction.Type,
					Value = logicAction.Value,
					MotherboardId = base.ReferenceId,
					LogicStateIndex = logicAction.ParentState.Index,
					IsDeleting = (_deleteCategory == 2),
					Index = logicAction.Index
				});
			}
			else
			{
				NetworkClient.SendToServer(new LogicStateMessage
				{
					DisplayName = logicState.DisplayName,
					MotherboardId = base.ReferenceId,
					IsDeleting = (_deleteCategory == 0),
					Index = logicState.Index,
					NextState = logicState.NextState.Index,
					FalseState = logicState.FalseState.Index
				});
			}
			_deleteCategory = -1;
			_deleteIndex = -1;
			_logicStateIndex = -1;
		}
	}

	private IEnumerator DeserializeNextFrame(LogicMotherboardSaveData saveData)
	{
		yield return Yielders.EndOfFrame;
		while (GameManager.GameState != GameState.Running)
		{
			yield return Yielders.EndOfFrame;
		}
		for (int i = 0; i < saveData.LogicStates.Count; i++)
		{
			LogicStateSave logicStateSave = saveData.LogicStates[i];
			for (int j = 0; j < logicStateSave.Conditions.Count; j++)
			{
				LogicCondition logicCondition = LogicStates[i].Conditions[j];
				LogicConditionSave logicConditionSave = logicStateSave.Conditions[j];
				logicCondition.Device = Referencable.Find<Device>(logicConditionSave.DeviceReferenceId);
				logicCondition.Operation = logicConditionSave.Operation;
				logicCondition.Type = logicConditionSave.Type;
				logicCondition.Value = logicConditionSave.Value;
				logicCondition.Assess();
				logicCondition.ScreenCondition.SetConditionState();
				logicCondition.ScreenCondition.RefreshAll();
			}
			for (int k = 0; k < logicStateSave.Actions.Count; k++)
			{
				LogicAction logicAction = LogicStates[i].Actions[k];
				LogicActionSave logicActionSave = logicStateSave.Actions[k];
				logicAction.Device = Referencable.Find<Device>(logicActionSave.DeviceReferenceId);
				logicAction.Type = logicActionSave.Type;
				logicAction.Value = logicActionSave.Value;
				logicAction.ScreenAction.RefreshAll();
			}
		}
		yield return new WaitForSecondsRealtime(1f);
		RebuildLogicList();
		foreach (LogicState logicState in LogicStates)
		{
			logicState.Enabled = true;
			logicState.Refresh();
		}
	}

	public void ButtonNewState()
	{
		CreateNewState();
	}

	private void OnButtonRenameState(LogicState logicState)
	{
		if (InputWindow.ShowInputPanel(string.Format(InterfaceStrings.RenameThing, logicState.DisplayName), logicState.DisplayName, this, 32))
		{
			InputWindow.OnSubmit += delegate(string input, string input2)
			{
				RenameLogicState(input, logicState);
			};
		}
	}

	private void RenameLogicState(string value, LogicState logicState)
	{
		if (logicState != null)
		{
			logicState.DisplayName = value;
			logicState.ScreenState.Title.text = value;
			RebuildLogicList();
			SendCurrentLogicState(logicState);
		}
	}

	private void OnButtonSetCurrentState(LogicState logicState)
	{
		SetCurrentLogicState(logicState);
		SendCurrentLogicState(logicState);
	}

	public void SetCurrentLogicState(LogicState logicState)
	{
		if (logicState == CurrentLogicState)
		{
			return;
		}
		LogicState currentLogicState = CurrentLogicState;
		if (logicState != null)
		{
			logicState.IsTriggered = false;
		}
		CurrentLogicState = logicState;
		if (currentLogicState != null)
		{
			currentLogicState.IsTriggered = false;
		}
		foreach (LogicState logicState2 in LogicStates)
		{
			logicState2.Refresh();
		}
	}

	private void OnButtonNextStateChanged(LogicState logicState, int value)
	{
		LogicState nextState = ((value < LogicStates.Count) ? LogicStates[value] : logicState);
		logicState.NextState = nextState;
		SendCurrentLogicState(logicState);
	}

	private void OnButtonFalseStateChanged(LogicState logicState, int value)
	{
		LogicState falseState = ((value < LogicStates.Count) ? LogicStates[value] : logicState);
		logicState.FalseState = falseState;
		SendCurrentLogicState(logicState);
	}

	public void OnButtonDeleteState(LogicState logicState)
	{
		_deleteCategory = 0;
		_deleteIndex = logicState.Index;
		_logicStateIndex = logicState.Index;
		SendCurrentLogicState(logicState);
		RemoveLogicState(logicState.Index);
	}

	private void OnButtonDeleteCondition(LogicCondition condition)
	{
		_deleteCategory = 1;
		_deleteIndex = condition.Index;
		_logicStateIndex = condition.ParentState.Index;
		SendCurrentLogicState(condition.ParentState, condition);
		RemoveCondition(condition.ParentState.Index, condition.Index);
	}

	private void OnButtonDeleteAction(LogicAction action)
	{
		_deleteCategory = 2;
		_deleteIndex = action.Index;
		_logicStateIndex = action.ParentState.Index;
		SendCurrentLogicState(action.ParentState, null, action);
		RemoveAction(action.ParentState.Index, action.Index);
	}

	private void OnActionDropdownTypeChanged(LogicAction action, int value)
	{
		action.Type = (LogicType)Enum.Parse(typeof(LogicType), action.ScreenAction.Type.options[action.ScreenAction.Type.value].text);
		action.ScreenAction.OnTypeChanged();
		SendCurrentLogicState(action.ParentState, null, action);
	}

	private void OnActionDropdownValueChanged(LogicAction action, int value)
	{
		switch (action.Type)
		{
		case LogicType.Color:
			action.Value = GameManager.LogicColorIndexFromDropdown(action.ScreenAction.Value.value);
			break;
		case LogicType.Power:
		case LogicType.Open:
		case LogicType.Mode:
		case LogicType.Activate:
		case LogicType.Lock:
			action.Value = action.ScreenAction.Value.value;
			break;
		default:
			if (value == 0)
			{
				return;
			}
			action.ScreenAction.Value.SetValueWithoutNotify(0);
			if (InputWindow.ShowInputPanel(ScreenDropdownBase.EnterNewValue, action.Value.ToString(CultureInfo.InvariantCulture), this, 16))
			{
				InputWindow.OnSubmit += delegate(string input, string input2)
				{
					SetLogicActionValue(input, action);
				};
			}
			break;
		case LogicType.None:
			break;
		}
		SendCurrentLogicState(action.ParentState, null, action);
	}

	private void SetLogicActionValue(string value, LogicAction action)
	{
		if (action != null)
		{
			float.TryParse(value, out var result);
			action.Value = result;
			action.ScreenAction.Value.captionText.text = result.ToString(CultureInfo.InvariantCulture);
			SendCurrentLogicState(action.ParentState, null, action);
		}
	}

	private void OnActionDropdownDeviceChanged(LogicAction action, int value)
	{
		if (value < DisplayedDevices.Count)
		{
			action.Device = GetListDevice(value);
			if (action.ScreenAction.Device.options.Count >= DisplayedDevices.Count)
			{
				action.PopulateDevices(ref DeviceOptionList);
			}
		}
		action.ScreenAction.OnDeviceChanged();
		SendCurrentLogicState(action.ParentState, null, action);
	}

	private void OnConditionDropdownDeviceChanged(LogicCondition condition, int value)
	{
		if (value < DisplayedDevices.Count)
		{
			condition.Device = GetListDevice(value);
			if (condition.ScreenCondition.Device.options.Count > DisplayedDevices.Count)
			{
				condition.PopulateDevices(ref DeviceOptionList);
			}
		}
		condition.ScreenCondition.OnDeviceChanged();
		SendCurrentLogicState(condition.ParentState, condition);
	}

	private void OnConditionDropdownTypeChanged(LogicCondition condition, int value)
	{
		condition.Type = (LogicType)Enum.Parse(typeof(LogicType), condition.ScreenCondition.Type.options[value].text);
		condition.ScreenCondition.OnTypeChanged();
		SendCurrentLogicState(condition.ParentState, condition);
	}

	private void OnConditionDropdownOperatorChanged(LogicCondition condition, int value)
	{
		condition.Operation = condition.ScreenCondition.DisplayedOperators[value];
		condition.ScreenCondition.OnOperatorChanged();
		SendCurrentLogicState(condition.ParentState, condition);
	}

	private void OnConditionDropdownValueChanged(LogicCondition condition, int value)
	{
		switch (condition.Type)
		{
		case LogicType.Color:
			condition.Value = GameManager.LogicColorIndexFromDropdown(condition.ScreenCondition.Value.value);
			break;
		case LogicType.Power:
		case LogicType.Open:
		case LogicType.Mode:
		case LogicType.Activate:
		case LogicType.Lock:
			condition.Value = condition.ScreenCondition.Value.value;
			break;
		default:
			if (value == 0)
			{
				return;
			}
			condition.ScreenCondition.Value.SetValueWithoutNotify(0);
			if (InputWindow.ShowInputPanel(ScreenDropdownBase.EnterNewValue, condition.Value.ToString(CultureInfo.InvariantCulture), this, 16, TMP_InputField.ContentType.DecimalNumber))
			{
				InputWindow.OnSubmit += delegate(string input, string input2)
				{
					SetLogicConditionValue(input, condition);
				};
			}
			break;
		case LogicType.None:
			break;
		}
		SendCurrentLogicState(condition.ParentState, condition);
	}

	private void SetLogicConditionValue(string value, LogicCondition condition)
	{
		if (condition != null)
		{
			float.TryParse(value, out var result);
			condition.Value = result;
			condition.ScreenCondition.Value.captionText.text = result.ToString(CultureInfo.InvariantCulture);
			SendCurrentLogicState(condition.ParentState, condition);
		}
	}

	private void OnButtonNewCondition(LogicState state)
	{
		LogicCondition logicCondition = CreateNewCondition(state);
		logicCondition.PopulateDevices(ref DeviceOptionList);
		logicCondition.ScreenCondition.RefreshAll();
		SendCurrentLogicState(state, logicCondition);
	}

	private void OnButtonNewAction(LogicState state)
	{
		LogicAction logicAction = CreateNewAction(state);
		logicAction.PopulateDevices(ref DeviceOptionList);
		logicAction.ScreenAction.RefreshAll();
		SendCurrentLogicState(state, null, logicAction);
	}

	private LogicState CreateNewState()
	{
		ScreenState screenState = UnityEngine.Object.Instantiate(StatePrefab, StateGroup.transform);
		LogicState newState = new LogicState
		{
			DisplayName = $"State {LogicStates.Count + 1}",
			ParentMotherboard = this,
			ScreenState = screenState
		};
		LogicStates.Add(newState);
		screenState.ButtonDelete.onClick.AddListener(delegate
		{
			OnButtonDeleteState(newState);
		});
		screenState.ButtonNewCondition.onClick.AddListener(delegate
		{
			OnButtonNewCondition(newState);
		});
		screenState.ButtonNewAction.onClick.AddListener(delegate
		{
			OnButtonNewAction(newState);
		});
		screenState.NextStateDropdown.onValueChanged.AddListener(delegate(int p)
		{
			OnButtonNextStateChanged(newState, p);
		});
		screenState.FalseStateDropdown.onValueChanged.AddListener(delegate(int p)
		{
			OnButtonFalseStateChanged(newState, p);
		});
		screenState.ButtonRename.onClick.AddListener(delegate
		{
			OnButtonRenameState(newState);
		});
		screenState.ButtonPlay.onClick.AddListener(delegate
		{
			OnButtonSetCurrentState(newState);
		});
		ScreenButtonNewState.transform.SetAsLastSibling();
		newState.ScreenState.Title.text = newState.DisplayName;
		newState.NextState = newState;
		newState.FalseState = newState;
		newState.Enabled = true;
		SetCurrentLogicState(newState);
		SendCurrentLogicState(newState);
		RebuildLogicList();
		newState.Refresh();
		return newState;
	}

	public LogicCondition CreateNewCondition(LogicState state)
	{
		ScreenCondition screenCondition = UnityEngine.Object.Instantiate(ConditionPrefab, state.ScreenState.ConditionGrid.transform);
		LogicCondition newCondition = new LogicCondition
		{
			ParentState = state,
			ScreenCondition = screenCondition
		};
		int count = state.Conditions.Count;
		screenCondition.Initialize(newCondition, count);
		state.AddCondition(newCondition);
		screenCondition.ButtonDelete.onClick.AddListener(delegate
		{
			OnButtonDeleteCondition(newCondition);
		});
		screenCondition.Value.onValueChanged.AddListener(delegate(int p)
		{
			OnConditionDropdownValueChanged(newCondition, p);
		});
		screenCondition.Device.onValueChanged.AddListener(delegate(int p)
		{
			OnConditionDropdownDeviceChanged(newCondition, p);
		});
		screenCondition.Type.onValueChanged.AddListener(delegate(int p)
		{
			OnConditionDropdownTypeChanged(newCondition, p);
		});
		screenCondition.Operator.onValueChanged.AddListener(delegate(int p)
		{
			OnConditionDropdownOperatorChanged(newCondition, p);
		});
		newCondition.PopulateDevices(ref DeviceOptionList);
		newCondition.ScreenCondition.RefreshAll();
		return newCondition;
	}

	public LogicAction CreateNewAction(LogicState state)
	{
		ScreenAction screenAction = UnityEngine.Object.Instantiate(ActionPrefab, state.ScreenState.ActionGrid.transform);
		LogicAction newAction = new LogicAction
		{
			ParentState = state,
			ScreenAction = screenAction
		};
		int count = state.Actions.Count;
		screenAction.Initialize(newAction, count);
		state.Actions.Add(newAction);
		screenAction.ButtonDelete.onClick.AddListener(delegate
		{
			OnButtonDeleteAction(newAction);
		});
		screenAction.Device.onValueChanged.AddListener(delegate(int p)
		{
			OnActionDropdownDeviceChanged(newAction, p);
		});
		screenAction.Type.onValueChanged.AddListener(delegate(int p)
		{
			OnActionDropdownTypeChanged(newAction, p);
		});
		screenAction.Value.onValueChanged.AddListener(delegate(int p)
		{
			OnActionDropdownValueChanged(newAction, p);
		});
		return newAction;
	}

	public void RemoveLogicState(int index)
	{
		if (index >= 0 && index < LogicStates.Count)
		{
			LogicState logicState = LogicStates[index];
			if (logicState == CurrentLogicState)
			{
				LogicState currentLogicState = ((logicState.NextState != null && logicState.NextState != logicState) ? logicState.NextState : null);
				SetCurrentLogicState(currentLogicState);
			}
			LogicStates.RemoveAt(index);
			UnityEngine.Object.Destroy(logicState.ScreenState.gameObject);
			if (LogicStates.Count > 0 && CurrentLogicState == null)
			{
				SetCurrentLogicState(LogicStates[0]);
			}
			RebuildLogicList();
		}
	}
}
