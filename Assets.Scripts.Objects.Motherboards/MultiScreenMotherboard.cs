using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Motherboard;

namespace Assets.Scripts.Objects.Motherboards;

public class MultiScreenMotherboard : Motherboard
{
	private int _currentTab = 1;

	[ByteArraySync]
	public int CurrentTab
	{
		get
		{
			return _currentTab;
		}
		set
		{
			_currentTab = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			RefreshScreen();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(CurrentTab);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentTab = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(CurrentTab);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentTab = reader.ReadInt32();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is MultiMotherboardSaveData multiMotherboardSaveData)
		{
			multiMotherboardSaveData.CurrentTab = CurrentTab;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new MultiMotherboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is MultiMotherboardSaveData multiMotherboardSaveData)
		{
			CurrentTab = multiMotherboardSaveData.CurrentTab;
		}
	}

	public void OnButtonRenameDevice(Device sorter)
	{
		if (InputWindow.ShowInputPanel(ScreenDropdownBase.EnterNewValue, sorter.DisplayName, this, 32))
		{
			InputWindow.OnSubmit += delegate(string input, string input2)
			{
				RenameDevice(input, sorter);
			};
		}
	}

	private static void RenameDevice(string value, Thing device)
	{
		if ((bool)device)
		{
			if (GameManager.RunSimulation)
			{
				Thing.RenameThing(device.ReferenceId, value);
			}
			else
			{
				NetworkClient.RenameThing(device.ReferenceId, value);
			}
		}
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		Screens[0].SetActive(CurrentTab == 0);
		Screens[1].SetActive(CurrentTab == 1);
		Screens[2].SetActive(CurrentTab == 2);
	}

	public override void SetMode(bool isNormal)
	{
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		if (command == 2 && GameManager.RunSimulation && (bool)reference)
		{
			OnServer.Interact(reference, InteractableType.Activate, (reference.Activate != 1) ? 1 : 0);
		}
	}

	public void ButtonTabDevices()
	{
		CurrentTab = 1;
	}

	public void ButtonTabSettings()
	{
		CurrentTab = 2;
	}
}
