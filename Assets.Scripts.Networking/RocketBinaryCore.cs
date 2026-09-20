using System;
using System.Collections.Generic;

namespace Assets.Scripts.Networking;

public abstract class RocketBinaryCore : IDisposable
{
	public enum NetworkDataType
	{
		Bool,
		Byte,
		SByte,
		Int32,
		UInt32,
		Int16,
		UInt16,
		Int64,
		UInt64,
		Single,
		Double,
		Fp16,
		Quaternion,
		Vector3,
		String,
		Grid3,
		Colour,
		MessageType,
		UpdateType
	}

	public static Dictionary<NetworkDataType, string> NetworkDataTypeDict;

	protected bool _isLogging;

	protected int debuggedOperationIndex;

	private bool _previousState;

	public bool IsLogging => _isLogging;

	public virtual void StartLogging()
	{
		if (NetworkDataTypeDict == null)
		{
			NetworkDataTypeDict = new Dictionary<NetworkDataType, string>();
			NetworkDataType[] array = Enum.GetValues(typeof(NetworkDataType)) as NetworkDataType[];
			for (int i = 0; i < array.Length; i++)
			{
				NetworkDataType key = array[i];
				NetworkDataTypeDict.Add(key, key.ToString());
			}
		}
		_previousState = _isLogging;
		_isLogging = true;
	}

	public virtual void StopLogging()
	{
		_previousState = _isLogging;
		_isLogging = false;
	}

	public virtual void ReturnLoggingToPreviousState()
	{
	}

	public virtual void Close()
	{
	}

	public virtual void Dispose()
	{
	}
}
