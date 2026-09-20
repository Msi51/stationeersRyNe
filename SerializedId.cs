using System;
using System.Xml.Serialization;
using UnityEngine;

public class SerializedId : IGameData
{
	[XmlAttribute]
	public string Id = string.Empty;

	private int _hash;

	public int Hash => _hash;

	public SerializedId()
	{
	}

	public SerializedId(string id)
	{
		Id = id;
		CheckHash();
	}

	public static implicit operator SerializedId(string id)
	{
		return new SerializedId(id);
	}

	public static SerializedId Create(IReferencable sourceReferenceId)
	{
		return new SerializedId(sourceReferenceId);
	}

	private SerializedId(IReferencable sourceReferenceId)
	{
		Id = (sourceReferenceId?.ReferenceId ?? 0).ToString();
	}

	internal void CheckHash()
	{
		if (_hash == 0)
		{
			if (Id.Contains(" "))
			{
				throw new Exception("Id '" + Id + "' contains spaces, which is not allowed for a serialized Id");
			}
			_hash = Animator.StringToHash(Id);
		}
	}

	public virtual void OnDataLoad()
	{
		CheckHash();
	}

	public static implicit operator string(SerializedId serializedData)
	{
		return serializedData.Id;
	}
}
