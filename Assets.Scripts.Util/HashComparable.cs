using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Util;

public class HashComparable<T>
{
	public int CurrentDataHash;

	public string CompressedData;

	private T _data;

	public T GetData()
	{
		return _data;
	}

	public HashComparable()
	{
	}

	public HashComparable(ref T data)
	{
		_data = data;
	}

	public bool IsDirty(int hash)
	{
		if (CurrentDataHash != 0)
		{
			return CurrentDataHash != hash;
		}
		return true;
	}

	public bool UpdateData(ref string compressedData)
	{
		ClearData();
		string s = StringCompressor.DecompressString(compressedData);
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		StringReader stringReader = new StringReader(s);
		object obj = xmlSerializer.Deserialize(stringReader);
		if (obj is T)
		{
			_data = (T)obj;
			CurrentDataHash = Animator.StringToHash(stringReader.ToString());
			CompressedData = compressedData;
			return true;
		}
		return false;
	}

	public void SetData(T data)
	{
		_data = data;
		GenerateHash();
	}

	public void ClearData()
	{
		CompressedData = null;
		CurrentDataHash = 0;
	}

	private void GenerateHash()
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		StringWriter stringWriter = new StringWriter();
		xmlSerializer.Serialize(stringWriter, _data);
		CurrentDataHash = Animator.StringToHash(stringWriter.ToString());
		CompressedData = StringCompressor.CompressString(stringWriter.ToString());
		stringWriter.Close();
	}
}
