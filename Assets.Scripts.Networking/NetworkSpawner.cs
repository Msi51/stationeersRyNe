using System.Collections.Generic;
using Assets.Scripts.Objects;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Networking;

public static class NetworkSpawner
{
	private class ThingSpawn
	{
		public ThingSpawnMessage Message;

		public Thing TheThing;
	}

	private static readonly List<ThingSpawn> _ThingsWaitingToBeSpawnedOnServerList = new List<ThingSpawn>();

	private static async UniTask<T> WaitForThingToSpawnOnServer<T>(int thingSpawnHashCode) where T : Thing
	{
		await UniTask.WaitUntil(delegate
		{
			ThingSpawn thingSpawn = _ThingsWaitingToBeSpawnedOnServerList.Find((ThingSpawn x) => x.Message.GetHashCode() == thingSpawnHashCode);
			return thingSpawn != null && thingSpawn.TheThing != null;
		});
		return _ThingsWaitingToBeSpawnedOnServerList.Find((ThingSpawn x) => x.Message.GetHashCode() == thingSpawnHashCode).TheThing as T;
	}

	public static void FindSpawnedItem(ThingSpawnMessage message)
	{
		Thing theThing = Thing.Find<Thing>(message.ReferenceId);
		_ThingsWaitingToBeSpawnedOnServerList.Find((ThingSpawn x) => x.Message.GetHashCode() == message.GetHashCode()).TheThing = theThing;
	}

	public static void SpawnOnServer(ThingSpawnMessage message)
	{
		Thing thing = OnServer.Create<Thing>(message.PrefabHash, message.Position, message.Rotation);
		message.ReferenceId = thing.ReferenceId;
		message.IsSpawned = true;
		NetworkServer.SendToClient(message, NetworkChannel.GeneralTraffic, message.ConnectionId);
	}
}
