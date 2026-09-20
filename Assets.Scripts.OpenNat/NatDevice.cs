using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.OpenNat;

public abstract class NatDevice
{
	private readonly HashSet<Mapping> _openedMapping = new HashSet<Mapping>();

	protected DateTime LastSeen { get; private set; }

	internal void Touch()
	{
		LastSeen = DateTime.Now;
	}

	public abstract void CreatePortMapAsync(Mapping mapping);

	public abstract void DeletePortMapAsync(Mapping mapping);

	public abstract void GetAllMappingsAsync();

	public abstract void GetExternalIPAsync();

	public abstract Mapping GetSpecificMappingAsync(Protocol protocol, int port);

	protected void RegisterMapping(Mapping mapping)
	{
		_openedMapping.Remove(mapping);
		_openedMapping.Add(mapping);
	}

	protected void UnregisterMapping(Mapping mapping)
	{
		_openedMapping.RemoveWhere((Mapping x) => x.Equals(mapping));
	}

	internal void ReleaseMapping(IEnumerable<Mapping> mappings)
	{
		int num = mappings.ToArray().Length;
		Debug.Log($"{num} ports to close");
		for (int i = 0; i < num; i++)
		{
			Mapping mapping = _openedMapping.ElementAt(i);
			try
			{
				DeletePortMapAsync(mapping);
				Debug.Log(mapping?.ToString() + " port successfully closed");
			}
			catch (Exception)
			{
				Debug.Log(mapping?.ToString() + " port couldn't be close");
			}
		}
	}

	internal void ReleaseAll()
	{
		ReleaseMapping(_openedMapping);
	}

	internal void ReleaseSessionMappings()
	{
		IEnumerable<Mapping> mappings = _openedMapping.Where((Mapping m) => m.LifetimeType == MappingLifetime.Session);
		ReleaseMapping(mappings);
	}

	internal void RenewMappings()
	{
		Mapping[] array = _openedMapping.Where((Mapping x) => x.ShoundRenew()).ToArray();
		foreach (Mapping mapping in array)
		{
			RenewMapping(mapping);
		}
	}

	private void RenewMapping(Mapping mapping)
	{
		Mapping mapping2 = new Mapping(mapping);
		mapping2.Expiration = DateTime.UtcNow.AddSeconds(mapping.Lifetime);
		Debug.Log($"Renewing mapping {mapping2}");
		CreatePortMapAsync(mapping2);
	}
}
