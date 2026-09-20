using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Trading;

namespace Assets.Scripts.Objects.Electrical;

public interface ISourceCode : IReferencable, IEvaluable
{
	private static readonly int MaxArrayLength;

	char[] SourceCodeCharArray { get; set; }

	int SourceCodeWritePointer { get; set; }

	void SendUpdate();

	void SetSourceCode(string sourceCode);

	AsciiString GetSourceCode();

	static void SendSourceCodeToServer(AsciiString sourceCode, long referenceId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		byte[] bytes = sourceCode.GetBytes();
		List<byte[]> list = new List<byte[]>();
		byte[] array = Array.Empty<byte>();
		for (int i = 0; i < bytes.Length; i++)
		{
			int num = i % MaxArrayLength;
			if (num == 0)
			{
				array = new byte[Math.Min(MaxArrayLength, sourceCode.Length - list.Count * MaxArrayLength)];
				list.Add(array);
			}
			array[num] = bytes[i];
		}
		int num2 = 0;
		foreach (byte[] item in list)
		{
			num2 += item.Length;
		}
		IntegratedCircuitHeader integratedCircuitHeader = new IntegratedCircuitHeader();
		integratedCircuitHeader.Id = referenceId;
		integratedCircuitHeader.SourceCodeLength = num2;
		integratedCircuitHeader.SendToServer();
		for (int j = 0; j < list.Count; j++)
		{
			IntegratedCircuitUpdate integratedCircuitUpdate = new IntegratedCircuitUpdate();
			integratedCircuitUpdate.Id = referenceId;
			integratedCircuitUpdate.SourceCodeFragment = list[j];
			integratedCircuitUpdate.FragmentStartIndex = j;
			integratedCircuitUpdate.SendToServer();
		}
	}

	static ISourceCode()
	{
		MaxArrayLength = 512;
	}
}
