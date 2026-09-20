using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class PictureFrameMessage : ProcessedMessage<PictureFrameMessage>
{
	public long PictureFrame;

	public int PictureIndex;

	public int InteractableType;

	public override void Process(long hostId)
	{
		PictureFrame pictureFrame = Thing.Find<PictureFrame>(PictureFrame);
		if ((bool)pictureFrame)
		{
			pictureFrame.HandlePictureFrame((InteractableType)InteractableType, "", PictureIndex);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		PictureFrame = reader.ReadInt64();
		PictureIndex = reader.ReadInt32();
		InteractableType = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(PictureFrame);
		writer.WriteInt32(PictureIndex);
		writer.WriteInt32(InteractableType);
	}
}
