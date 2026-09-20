using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public abstract class DataCollection : IChecksum
{
	protected const string ID_ATTRIBUTE = "Id";

	[XmlAttribute("Id")]
	public string Id;

	protected const string NAME_ELEMENT = "Name";

	[XmlElement("Name")]
	public LocalizedStringReference Name;

	private static Dictionary<int, DataCollection> _dataCollectionDictionary = new Dictionary<int, DataCollection>();

	[XmlIgnore]
	public int IdHash => Animator.StringToHash(Id);

	public virtual int GetChecksum()
	{
		return ((!string.IsNullOrEmpty(Id)) ? Animator.StringToHash(Id) : 0) * 41;
	}

	public static void Register(DataCollection dataCollection, ModAbout mod)
	{
		if (dataCollection == null || string.IsNullOrEmpty(dataCollection.Id))
		{
			return;
		}
		if (!_dataCollectionDictionary.TryAdd(dataCollection.IdHash, dataCollection))
		{
			if (mod != null)
			{
				ConsoleWindow.PrintAction($"replacing '{dataCollection.Id}' because of '{mod}'");
				_dataCollectionDictionary[dataCollection.IdHash] = dataCollection;
			}
			else
			{
				ConsoleWindow.PrintError($"Can't register {typeof(DataCollection)} as another data is registered with Id {dataCollection.Id}");
			}
		}
		dataCollection.OnRegistered();
	}

	protected virtual void OnRegistered()
	{
	}

	public abstract void Initialize(ModAbout mod);

	public virtual bool IsValid()
	{
		return true;
	}

	public static T Get<T>(int idHash) where T : DataCollection
	{
		if (!_dataCollectionDictionary.TryGetValue(idHash, out var value))
		{
			ConsoleWindow.PrintError($"error getting {typeof(T).Name} Id: {idHash}");
			return null;
		}
		if (value is T result)
		{
			return result;
		}
		return null;
	}

	public static bool TryGet<T>(string idString, out T data) where T : DataCollection
	{
		int key = Animator.StringToHash(idString);
		if (_dataCollectionDictionary.TryGetValue(key, out var value))
		{
			data = value as T;
			return true;
		}
		data = null;
		return false;
	}

	public static bool TryGet<T>(int id, out T data) where T : DataCollection
	{
		if (_dataCollectionDictionary.TryGetValue(id, out var value))
		{
			data = value as T;
			return true;
		}
		data = null;
		return false;
	}

	public static bool IsUniqueId(string id)
	{
		return !_dataCollectionDictionary.ContainsKey(Animator.StringToHash(id));
	}

	public static T Get<T>(string id) where T : DataCollection
	{
		return Get<T>(Animator.StringToHash(id));
	}
}
