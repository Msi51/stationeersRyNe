using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Objects.Motherboards;

[Serializable]
public class LogicState : IRocketReaderWriter
{
	public string DisplayName;

	public List<LogicCondition> Conditions = new List<LogicCondition>();

	public List<LogicAction> Actions = new List<LogicAction>();

	public LogicState NextState;

	public LogicState FalseState;

	[Tooltip("Script Component that contains the visual elements displaying the logic")]
	public ScreenState ScreenState;

	public LogicMotherboard ParentMotherboard;

	public bool Enabled;

	private sbyte _deleteType = -1;

	private int _deleteIndex = -1;

	private bool _isTriggered;

	public int Index => ParentMotherboard.LogicStates.IndexOf(this);

	public bool IsTriggered
	{
		get
		{
			return _isTriggered;
		}
		set
		{
			if (Enabled && value != _isTriggered)
			{
				_isTriggered = value;
				UnityMainThreadDispatcher.Instance().Enqueue(_isTriggered ? OnTriggered() : OnReset());
			}
		}
	}

	public void AddCondition(LogicCondition newCondition)
	{
		lock (Conditions)
		{
			Conditions.Add(newCondition);
		}
	}

	public void RemoveCondition(int index)
	{
		lock (Conditions)
		{
			if (index >= 0 && index < Conditions.Count)
			{
				LogicCondition logicCondition = Conditions[index];
				Conditions.RemoveAt(logicCondition.Index);
				UnityEngine.Object.Destroy(logicCondition.ScreenCondition.gameObject);
				_deleteType = 0;
				_deleteIndex = index;
			}
		}
	}

	public void RemoveAction(int index)
	{
		if (index >= 0 && index < Actions.Count)
		{
			LogicAction logicAction = Actions[index];
			Actions.RemoveAt(logicAction.Index);
			UnityEngine.Object.Destroy(logicAction.ScreenAction.gameObject);
			_deleteType = 1;
			_deleteIndex = index;
		}
	}

	public void Load(LogicStateSave logicStateSave)
	{
		DisplayName = logicStateSave.DisplayName;
		_isTriggered = logicStateSave.IsTriggered;
		ScreenState.Title.text = DisplayName;
		ScreenState.TriggeredImage.enabled = _isTriggered;
		for (int i = 0; i < logicStateSave.Conditions.Count; i++)
		{
			LogicConditionSave logicConditionSave = logicStateSave.Conditions[i];
			LogicCondition logicCondition = ParentMotherboard.CreateNewCondition(this);
			logicCondition.IsTrue = logicConditionSave.IsTrue;
			logicCondition.ScreenCondition.IsTrue = logicConditionSave.IsTrue;
			logicCondition.ScreenCondition.IsTrueImage.enabled = logicConditionSave.IsTrue;
			logicCondition.IsDisconnected = logicConditionSave.IsDisconnected;
		}
		for (int j = 0; j < logicStateSave.Actions.Count; j++)
		{
			ParentMotherboard.CreateNewAction(this);
		}
	}

	public IEnumerator OnReset()
	{
		if ((bool)ScreenState && (bool)ScreenState.TriggeredImage)
		{
			ScreenState.TriggeredImage.enabled = false;
		}
		yield break;
	}

	public IEnumerator OnTriggered()
	{
		ScreenState.TriggeredImage.enabled = IsTriggered;
		if (GameManager.RunSimulation)
		{
			ParentMotherboard.IsPaused = true;
			if ((IsTriggered ? NextState : FalseState) != this)
			{
				ParentMotherboard.SetCurrentLogicState(IsTriggered ? NextState : FalseState);
			}
			if (IsTriggered)
			{
				foreach (LogicAction action in Actions)
				{
					if (!action.IsDisconnected)
					{
						if (!(action.Device as LogicMemory))
						{
							action.Device.SetLogicValue(action.Type, (int)action.Value);
						}
						else
						{
							action.Device.SetLogicValue(action.Type, (float)Math.Round(action.Value, 1));
						}
					}
				}
			}
		}
		yield return new WaitForSecondsRealtime(0.5f);
		if (NextState != this)
		{
			IsTriggered = false;
			ParentMotherboard.SendCurrentLogicState(this);
		}
		if ((bool)ParentMotherboard && GameManager.RunSimulation)
		{
			ParentMotherboard.IsPaused = false;
		}
	}

	public void Refresh()
	{
		ScreenState.TriggeredImage.enabled = IsTriggered;
		ScreenState.BackgroundImage.color = ((ParentMotherboard.CurrentLogicState == this) ? Color.yellow : Color.gray);
		foreach (LogicCondition condition in Conditions)
		{
			condition.ScreenCondition.RefreshAll();
		}
		foreach (LogicAction action in Actions)
		{
			action.ScreenAction.RefreshAll();
		}
	}

	public void OnDeviceListUpdated()
	{
		foreach (LogicCondition condition in Conditions)
		{
			condition.PopulateDevices(ref ParentMotherboard.DeviceOptionList);
		}
		foreach (LogicAction action in Actions)
		{
			action.PopulateDevices(ref ParentMotherboard.DeviceOptionList);
		}
	}

	public void Read(RocketBinaryReader reader)
	{
		long referenceId = reader.ReadInt64();
		ParentMotherboard = Thing.Find<LogicMotherboard>(referenceId);
		DisplayName = reader.ReadString();
		_deleteType = reader.ReadSByte();
		_deleteIndex = reader.ReadInt32();
		switch (_deleteType)
		{
		case 0:
			RemoveCondition(_deleteIndex);
			break;
		case 1:
			RemoveAction(_deleteIndex);
			break;
		}
		lock (Conditions)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				if (i < Conditions.Count)
				{
					Conditions[i].Read(reader);
				}
				else
				{
					ParentMotherboard.CreateNewCondition(this).Read(reader);
				}
			}
		}
		lock (Actions)
		{
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				if (j < Actions.Count)
				{
					Actions[j].Read(reader);
				}
				else
				{
					ParentMotherboard.CreateNewAction(this).Read(reader);
				}
			}
		}
		ScreenCondition[] conditions = ScreenState.GetConditions();
		int num3 = reader.ReadInt32();
		for (int k = 0; k < num3; k++)
		{
			int valueWithoutNotify = reader.ReadInt32();
			int valueWithoutNotify2 = reader.ReadInt32();
			int valueWithoutNotify3 = reader.ReadInt32();
			int valueWithoutNotify4 = reader.ReadInt32();
			conditions[k].Device.SetValueWithoutNotify(valueWithoutNotify);
			conditions[k].Operator.SetValueWithoutNotify(valueWithoutNotify2);
			conditions[k].Type.SetValueWithoutNotify(valueWithoutNotify3);
			conditions[k].Value.SetValueWithoutNotify(valueWithoutNotify4);
		}
		ScreenAction[] actions = ScreenState.GetActions();
		int num4 = reader.ReadInt32();
		for (int l = 0; l < num4; l++)
		{
			int valueWithoutNotify5 = reader.ReadInt32();
			int valueWithoutNotify6 = reader.ReadInt32();
			int valueWithoutNotify7 = reader.ReadInt32();
			actions[l].Device.SetValueWithoutNotify(valueWithoutNotify5);
			actions[l].Type.SetValueWithoutNotify(valueWithoutNotify6);
			actions[l].Value.SetValueWithoutNotify(valueWithoutNotify7);
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentMotherboard.ReferenceId);
		writer.WriteString(DisplayName);
		writer.WriteSByte(_deleteType);
		writer.WriteInt32(_deleteIndex);
		lock (Conditions)
		{
			writer.WriteInt32(Conditions.Count);
			foreach (LogicCondition condition in Conditions)
			{
				condition.Write(writer);
			}
		}
		lock (Actions)
		{
			writer.WriteInt32(Actions.Count);
			foreach (LogicAction action in Actions)
			{
				action.Write(writer);
			}
		}
		ScreenCondition[] conditions = ScreenState.GetConditions();
		int value = conditions.Length;
		writer.WriteInt32(value);
		ScreenCondition[] array = conditions;
		foreach (ScreenCondition screenCondition in array)
		{
			writer.WriteInt32(screenCondition.Device.value);
			writer.WriteInt32(screenCondition.Operator.value);
			writer.WriteInt32(screenCondition.Type.value);
			writer.WriteInt32(screenCondition.Value.value);
		}
		ScreenAction[] actions = ScreenState.GetActions();
		int value2 = actions.Length;
		writer.WriteInt32(value2);
		ScreenAction[] array2 = actions;
		foreach (ScreenAction screenAction in array2)
		{
			writer.WriteInt32(screenAction.Device.value);
			writer.WriteInt32(screenAction.Type.value);
			writer.WriteInt32(screenAction.Value.value);
		}
		_deleteType = -1;
		_deleteIndex = -1;
	}
}
