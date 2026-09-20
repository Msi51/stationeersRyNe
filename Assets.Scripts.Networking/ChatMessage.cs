using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class ChatMessage : ProcessedMessage<ChatMessage>
{
	public long HumanId { get; set; }

	public string DisplayName { get; set; }

	public string ChatText { get; set; }

	public override void Process(long hostId)
	{
		PrintToConsole();
		if (NetworkManager.IsServer)
		{
			NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
		}
		Human human = Thing.Find<Human>(HumanId);
		if ((bool)human && (bool)InventoryManager.Parent && InventoryManager.Parent.ReferenceId != HumanId)
		{
			human.SetChatText(ChatText);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanId = reader.ReadInt64();
		DisplayName = reader.ReadString();
		ChatText = reader.ReadString();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanId);
		writer.WriteString(DisplayName);
		writer.WriteString(ChatText);
	}

	public void PrintToConsole()
	{
		ConsoleWindow.Print(DisplayName + ": " + ChatText, ConsoleColor.Cyan, clearLine: false, aged: false, unformatted: true);
	}
}
