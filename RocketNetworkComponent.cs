using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using UnityEngine;

public class RocketNetworkComponent : MonoBehaviour
{
	public void CmdSpawnDynamicThingMaxStack(DynamicThing prefabDynamicThing, Vector3 position, Quaternion rotation, ulong ownerSteamId = 0uL, Rigidbody parentRigidbody = null)
	{
		if (NetworkManager.IsActive)
		{
			NetworkMessages.CreateThing createThing = new NetworkMessages.CreateThing
			{
				prefabHash = prefabDynamicThing.PrefabHash,
				worldPosition = position,
				worldRotation = rotation
			};
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(createThing, NetworkChannel.GeneralTraffic, -1L);
				Thing.Create<Thing>(createThing.prefabHash, position, rotation, 0L);
			}
		}
	}

	public void InteractWith(int interactableId, long targetId, long sourceId, int slotId, bool altKey)
	{
		if (NetworkManager.IsActive)
		{
			Interactable interactable = Thing.Find(targetId).Interactables[interactableId];
			NetworkMessages.Interact interact = new NetworkMessages.Interact
			{
				DestinationId = targetId,
				InteractionId = interactableId,
				SourceId = sourceId,
				SourceSlotId = slotId,
				State = interactable.State,
				AltKey = altKey
			};
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(interact);
			}
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(interact, NetworkChannel.GeneralTraffic, -1L);
				interact.Processmsg(interact);
			}
		}
	}
}
