using System;
using Assets.Scripts.Util;
using UI;

namespace Assets.Scripts.Networking;

public class AnnounceMessage : ProcessedMessage<AnnounceMessage>
{
	public string AnnounceText { get; set; }

	public override void Process(long hostId)
	{
		if (!NetworkManager.IsServer)
		{
			Display(AnnounceText);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		AnnounceText = reader.ReadString();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteString(AnnounceText);
	}

	public static void Display(string text)
	{
		ConsoleWindow.Print("Announcement: " + text, ConsoleColor.Yellow, clearLine: false, aged: false, unformatted: true);
		if (!GameManager.IsBatchMode && (bool)Singleton<ConfirmationPanel>.Instance)
		{
			Singleton<ConfirmationPanel>.Instance.ShowWithRawMessage("ServerAnnouncement", text, "ButtonOk");
		}
	}
}
