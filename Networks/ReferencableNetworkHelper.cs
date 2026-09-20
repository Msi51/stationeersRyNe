using System.Collections.Generic;
using Assets.Scripts.Util;

namespace Networks;

public static class ReferencableNetworkHelper
{
	public static readonly List<ReferencableNetwork> AllNetworks = new List<ReferencableNetwork>();

	public static void AssignReference(ReferencableNetwork network, long referenceId = 0L)
	{
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(network);
		}
		else
		{
			Referencable.RegisterAs(network, referenceId);
		}
	}

	public static long[] GetValidNetworkIds<T>(List<T> networks) where T : ReferencableNetwork
	{
		List<long> list = new List<long>(networks.Count);
		foreach (T network in networks)
		{
			if (network != null && network.IsNetworkValid())
			{
				list.Add(network.ReferenceId);
			}
		}
		return list.ToArray();
	}

	public static long[] GetValidNetworkIds<T>(DensePool<T> networks) where T : ReferencableNetwork, IDensePoolable
	{
		List<long> ids = new List<long>(networks.ActiveCount);
		networks.ForEach(delegate(T network)
		{
			if (network.IsNetworkValid())
			{
				ids.Add(network.ReferenceId);
			}
		});
		return ids.ToArray();
	}

	public static void ClearAll()
	{
		AllNetworks.Clear();
	}
}
