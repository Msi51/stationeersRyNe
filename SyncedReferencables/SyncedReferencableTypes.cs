using System;
using System.Collections.Generic;
using Objects.Electrical;

namespace SyncedReferencables;

public static class SyncedReferencableTypes
{
	private static readonly Dictionary<int, (Type saveDataType, Func<SyncedReferencable> factory)> _lookup = new Dictionary<int, (Type, Func<SyncedReferencable>)> { 
	{
		PylonConnection.TypeId,
		(typeof(PylonConnectionSaveData), () => new PylonConnection())
	} };

	public static SyncedReferencable Create(int typeId)
	{
		if (_lookup.TryGetValue(typeId, out (Type, Func<SyncedReferencable>) value))
		{
			return value.Item2();
		}
		throw new Exception($"Type {typeId} not found!");
	}

	public static void AddSaveDataTypes(List<Type> extraTypes)
	{
		foreach (var value in _lookup.Values)
		{
			Type item = value.saveDataType;
			if (!extraTypes.Contains(item))
			{
				extraTypes.Add(item);
			}
		}
	}
}
