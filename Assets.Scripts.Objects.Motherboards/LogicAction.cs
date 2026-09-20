using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Motherboard;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

[Serializable]
public class LogicAction : LogicBase
{
	[NonSerialized]
	public LogicState ParentState;

	[Tooltip("Script Component that contains the visual elements displaying the logic")]
	public ScreenAction ScreenAction;

	[Tooltip("The class of variable or attribute that is part of this logic")]
	public LogicType Type;

	public double Value;

	public bool IsDisconnected;

	public int Index => ParentState.Actions.FindIndex((LogicAction action) => action == this);

	public void PopulateDevices(ref List<Dropdown.OptionData> optionData)
	{
		if (Device == null)
		{
			IsDisconnected = false;
			ScreenAction.Device.options = optionData;
			ScreenAction.Device.SetValueWithoutNotify(0);
			Device = ParentState.ParentMotherboard.GetListDevice(0);
			ScreenAction.Device.interactable = true;
			return;
		}
		int num = ParentState.ParentMotherboard.DisplayedDevices.FindIndex((Device d) => d == Device);
		if (num < 0)
		{
			IsDisconnected = true;
			ScreenAction.Device.options = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData("?" + Device.DisplayName + "?")
			};
			ScreenAction.Device.interactable = false;
		}
		else
		{
			IsDisconnected = false;
			ScreenAction.Device.options = optionData;
			ScreenAction.Device.SetValueWithoutNotify((num >= 0) ? num : 0);
			ScreenAction.Device.interactable = true;
		}
	}

	public override void Read(RocketBinaryReader reader)
	{
		base.Read(reader);
		Network.ReadLogicValue(reader, out Type, out Value);
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		Network.WriteLogicValue(writer, Type, Value);
	}
}
