using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class IntegratedCircuitHeader : ProcessedMessage<IntegratedCircuitHeader>
{
	public long Id;

	public int SourceCodeLength;

	private float _receivedTime;

	public override void Process(long hostId)
	{
		ISourceCode iSourceCode = Referencable.Find<ISourceCode>(Id);
		WaitForFragments(iSourceCode).Forget();
	}

	private async UniTaskVoid WaitForFragments(ISourceCode iSourceCode)
	{
		if (iSourceCode != null && iSourceCode.SourceCodeCharArray == null)
		{
			iSourceCode.SourceCodeCharArray = new char[SourceCodeLength];
			iSourceCode.SourceCodeWritePointer = 0;
			_receivedTime = Time.time;
			await UniTask.WaitUntil(() => AllReceived(iSourceCode) || TimeOut());
			if (TimeOut())
			{
				iSourceCode.SourceCodeCharArray = null;
				iSourceCode.SendUpdate();
			}
			else if (GameManager.GameState != GameState.None && iSourceCode != null)
			{
				iSourceCode.SetSourceCode(new string(iSourceCode.SourceCodeCharArray));
				iSourceCode.SourceCodeCharArray = null;
				iSourceCode.SendUpdate();
			}
		}
	}

	private bool TimeOut()
	{
		return _receivedTime + 1f < Time.time;
	}

	private bool AllReceived(ISourceCode iSourceCode)
	{
		if (GameManager.GameState == GameState.None)
		{
			return true;
		}
		if (iSourceCode == null)
		{
			return true;
		}
		if (iSourceCode.SourceCodeWritePointer == SourceCodeLength)
		{
			return true;
		}
		return false;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Id = reader.ReadInt64();
		SourceCodeLength = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(Id);
		writer.WriteInt32(SourceCodeLength);
	}
}
