using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class SolarControl : Circuitboard
{
	public enum SolarControlCommands
	{
		IncreaseHorizontal = 1001,
		DecreaseHorizontal,
		IncreaseVertical,
		DecreaseVertical
	}

	private float _targetHorizontal;

	private float _targetVertical;

	public static readonly float MaxExtension = 1f;

	public static readonly int ExtensionIncrementLarge = 5;

	public static readonly int ExtensionIncrementSmall = 1;

	public static readonly float SmoothDisplayRate = 2f;

	public Text TargetHorizontalText;

	public Text TargetVerticalText;

	public Text InfoConnectedText;

	public Text InfoGeneratedText;

	public HashSet<SolarPanel> SolarPanels = new HashSet<SolarPanel>();

	private float _currentDisplayedLoad;

	[ByteArraySync]
	public float TargetHorizontal
	{
		get
		{
			return _targetHorizontal;
		}
		set
		{
			_targetHorizontal = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			UpdateConnectedSolars();
		}
	}

	[ByteArraySync]
	public float TargetVertical
	{
		get
		{
			return _targetVertical;
		}
		set
		{
			_targetVertical = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			UpdateConnectedSolars();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(TargetHorizontal);
			writer.WriteSingle(TargetVertical);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			TargetHorizontal = reader.ReadSingle();
			TargetVertical = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(TargetHorizontal);
		writer.WriteSingle(TargetVertical);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		TargetHorizontal = reader.ReadSingle();
		TargetVertical = reader.ReadSingle();
	}

	public override bool CanDeviceLink(Device device)
	{
		if (!(typeof(SolarPanel) == device.GetType()))
		{
			return device.GetType().IsSubclassOf(typeof(SolarPanel));
		}
		return true;
	}

	public void ButtonIncreaseHorizontal()
	{
		Motherboard.UseComputer(1001, base.netId, base.netId, KeyManager.GetButton(KeyMap.QuantityModifier) ? ExtensionIncrementSmall : ExtensionIncrementLarge, sendToAll: false);
	}

	public void ButtonDecreaseHorizontal()
	{
		Motherboard.UseComputer(1002, base.netId, base.netId, KeyManager.GetButton(KeyMap.QuantityModifier) ? ExtensionIncrementSmall : ExtensionIncrementLarge, sendToAll: false);
	}

	public void ButtonIncreaseVertical()
	{
		Motherboard.UseComputer(1003, base.netId, base.netId, KeyManager.GetButton(KeyMap.QuantityModifier) ? ExtensionIncrementSmall : ExtensionIncrementLarge, sendToAll: false);
	}

	public void ButtonDecreaseVertical()
	{
		Motherboard.UseComputer(1004, base.netId, base.netId, KeyManager.GetButton(KeyMap.QuantityModifier) ? ExtensionIncrementSmall : ExtensionIncrementLarge, sendToAll: false);
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (ParentComputer != null && !IsOccluded)
			{
				UpdateScreen();
			}
		}
	}

	public void UpdateConnectedSolars()
	{
		if (ParentComputer == null || !ParentComputer.AsDevice() || ParentComputer.AsDevice().ConnectedCableNetworks == null)
		{
			return;
		}
		foreach (SolarPanel solarPanel in SolarPanels)
		{
			if ((bool)solarPanel)
			{
				solarPanel.OrientatePanel(this);
			}
		}
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		SolarPanels.Clear();
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			SolarPanel solarPanel = linkedDevice as SolarPanel;
			if ((bool)solarPanel && IsDeviceConnected(solarPanel))
			{
				SolarPanels.Add(solarPanel);
				solarPanel.OrientatePanel(this);
			}
		}
	}

	public override void OnDeviceListChanged(Device device)
	{
		base.OnDeviceListChanged(device);
		SolarPanel solarPanel = device as SolarPanel;
		if ((bool)solarPanel && IsDeviceConnected(solarPanel))
		{
			SolarPanels.Add(solarPanel);
			solarPanel.OrientatePanel(this);
		}
		else if ((bool)solarPanel)
		{
			SolarPanels.Remove(solarPanel);
		}
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		base.MotherboardCommand(command, reference, referenceInt, text);
		switch ((SolarControlCommands)command)
		{
		case SolarControlCommands.IncreaseHorizontal:
			TargetHorizontal += (float)referenceInt / 100f;
			TargetHorizontal = Mathf.Clamp(TargetHorizontal, 0f, MaxExtension);
			break;
		case SolarControlCommands.DecreaseHorizontal:
			TargetHorizontal -= (float)referenceInt / 100f;
			TargetHorizontal = Mathf.Clamp(TargetHorizontal, 0f, MaxExtension);
			break;
		case SolarControlCommands.IncreaseVertical:
			TargetVertical += (float)referenceInt / 100f;
			TargetVertical = Mathf.Clamp(TargetVertical, 0f, MaxExtension);
			break;
		case SolarControlCommands.DecreaseVertical:
			TargetVertical -= (float)referenceInt / 100f;
			TargetVertical = Mathf.Clamp(TargetVertical, 0f, MaxExtension);
			break;
		}
	}

	private void UpdateScreen()
	{
		float num = 0f;
		foreach (SolarPanel solarPanel in SolarPanels)
		{
			num += solarPanel.GenerationRate;
		}
		_currentDisplayedLoad = Mathf.Lerp(_currentDisplayedLoad, num, Time.deltaTime * SmoothDisplayRate);
		TargetHorizontalText.text = StringManager.Get(Mathf.Round(TargetHorizontal * 100f)) + "%";
		TargetVerticalText.text = StringManager.Get(Mathf.Round(TargetVertical * 100f)) + "%";
		InfoConnectedText.text = GameStrings.SolarControlPanelsCount.AsString(StringManager.Get(SolarPanels.Count));
		InfoGeneratedText.text = _currentDisplayedLoad.ToString("F0") + " W";
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SolarControlSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		SolarControlSaveData solarControlSaveData = savedData as SolarControlSaveData;
		TargetHorizontal = solarControlSaveData.TargetHorizontal;
		TargetVertical = solarControlSaveData.TargetVertical;
		UpdateScreen();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		SolarControlSaveData obj = savedData as SolarControlSaveData;
		obj.TargetHorizontal = TargetHorizontal;
		obj.TargetVertical = TargetVertical;
	}
}
