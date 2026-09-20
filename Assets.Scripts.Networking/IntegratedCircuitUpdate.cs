using System;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class IntegratedCircuitUpdate : ProcessedMessage<IntegratedCircuitUpdate>
{
	public long Id;

	public byte[] SourceCodeFragment;

	public int FragmentStartIndex;

	public override void Process(long hostId)
	{
		ISourceCode sourceCode = Referencable.Find<ISourceCode>(Id);
		Array.Copy(SourceCodeFragment, 0, sourceCode.SourceCodeCharArray, sourceCode.SourceCodeWritePointer, SourceCodeFragment.Length);
		sourceCode.SourceCodeWritePointer += SourceCodeFragment.Length;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out Id);
		ushort num = reader.ReadUInt16();
		SourceCodeFragment = new byte[num];
		reader.ReadBytes(SourceCodeFragment, num);
		FragmentStartIndex = reader.ReadUInt16();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, Id);
		writer.WriteUInt16((ushort)SourceCodeFragment.Length);
		writer.WriteBytes(SourceCodeFragment, SourceCodeFragment.Length);
		writer.WriteUInt16((ushort)FragmentStartIndex);
	}
}
