using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using DLC;
using Objects;
using Objects.Items;
using TerrainSystem;
using UnityEngine;
using Util;

public static class OnServer
{
	public static float _heightmapRespawnOffset = 1f;

	public static void Merge(IMergeable parent, IMergeable child)
	{
		if (parent != null && child != null && GameManager.RunSimulation)
		{
			parent.Merge(child);
		}
	}

	public static void Combine(Consumable parent, Consumable child)
	{
		if ((object)parent != null && (object)child != null)
		{
			parent.Combine(child);
		}
	}

	public static void MoveToSlotOrWorld(DynamicThing childThing, Slot slot)
	{
		if (slot != null && !slot.Occupant)
		{
			MoveToSlot(childThing, slot);
		}
		else
		{
			MoveToWorld(childThing);
		}
	}

	public static void MoveToSlot(DynamicThing childThing, Slot slot)
	{
		if ((object)childThing != null && !childThing.IsBeingDestroyed)
		{
			if (GameManager.RunSimulation)
			{
				childThing.MoveToSlot(slot, slot.Parent);
				return;
			}
			NetworkClient.SendToServer(new MoveToSlotMessage
			{
				ChildId = childThing.netId,
				ParentId = slot.Parent.netId,
				SlotId = slot.SlotIndex,
				Drag = false
			});
		}
	}

	public static void PublishDragHuman(Human childThing, Slot slot, Vector3 offset)
	{
		if ((object)childThing == null || childThing.IsBeingDestroyed)
		{
			return;
		}
		if (!NetworkManager.IsActive)
		{
			childThing.DragInSlot(slot, offset);
			return;
		}
		MoveToSlotMessage message = new MoveToSlotMessage
		{
			ChildId = childThing.netId,
			ParentId = slot.Parent.netId,
			SlotId = slot.SlotIndex,
			Offset = offset,
			Drag = true
		};
		if (GameManager.RunSimulation)
		{
			childThing.DragInSlot(slot, offset);
			NetworkServer.SendToClients(message, NetworkChannel.GeneralTraffic, -1L);
		}
		else
		{
			NetworkClient.SendToServer(message);
		}
	}

	public static void AddPrintingComponent(Thing obj)
	{
		if (NetworkManager.IsServer)
		{
			NetworkServer.SendToClients(new AddPrintingComponenetMessage
			{
				DynamicThingId = obj.netId
			}, NetworkChannel.GeneralTraffic, -1L);
		}
	}

	private static async UniTaskVoid MoveInWorldFromThread(DynamicThing dynamicThing, Vector3 worldPosition, bool cancelMomentum = false)
	{
		await UniTask.SwitchToMainThread();
		MoveInWorld(dynamicThing, worldPosition, cancelMomentum);
	}

	public static void MoveInWorld(DynamicThing dynamicThing, Vector3 worldPosition, bool cancelMomentum = false)
	{
		if (dynamicThing.BeingDestroyed)
		{
			return;
		}
		if (ThreadedManager.IsThread)
		{
			MoveInWorldFromThread(dynamicThing, worldPosition, cancelMomentum).Forget();
			return;
		}
		dynamicThing.ActiveRigidbody.MovePosition(worldPosition);
		if (cancelMomentum && !dynamicThing.ActiveRigidbody.isKinematic)
		{
			dynamicThing.ActiveRigidbody.velocity = Vector3.zero;
			dynamicThing.ActiveRigidbody.angularVelocity = Vector3.zero;
		}
	}

	public static async UniTaskVoid ResetObjectYPos(DynamicThing dynamicThing)
	{
		await UniTask.SwitchToMainThread();
		float x = dynamicThing.Position.x;
		float z = dynamicThing.Position.z;
		Vector3 safePoint = SpawnPoint.GetSafePoint(new Vector3(x, 0f, z));
		MoveInWorld(dynamicThing, safePoint, cancelMomentum: true);
	}

	public static void AttackWith(Thing attackParent, byte activeHandSlotId, byte offHandSlotId, long targetId, Vector3 attackPosition, float completedRatio, bool isDestroy, bool isCopy)
	{
		bool runSimulation = GameManager.RunSimulation;
		Slot activeHand = attackParent.Slots[activeHandSlotId];
		Thing thing = Thing.Find(targetId);
		if ((bool)thing)
		{
			Slot otherHand = attackParent.Slots[offHandSlotId];
			Attack attack = new Attack(activeHand, otherHand, attackPosition, thing, completedRatio, null, isDestroy, isCopy);
			if (thing.AttackWith(attack, runSimulation) != null && NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new AttackWithMessage
				{
					AttackParentId = attackParent.ReferenceId,
					TargetId = targetId,
					ActiveHandSlotId = activeHandSlotId,
					OffHandSlotId = offHandSlotId,
					AttackPosition = attackPosition,
					IsDestroy = isDestroy,
					IsCopy = isCopy,
					CompletionRatio = completedRatio
				});
			}
		}
	}

	public static void InteractWith(Interactable interactable, Interaction interaction)
	{
		if (!interactable.Parent.PreventInteraction(out var _, interactable, interaction))
		{
			interactable.InteractWith(interaction);
		}
	}

	public static void Interact(Thing thing, InteractableType interactableType, int state, bool skipAnimation = false)
	{
		if ((object)thing == null)
		{
			return;
		}
		Interactable interactable = null;
		foreach (Interactable interactable2 in thing.Interactables)
		{
			if (interactable2.Action == interactableType)
			{
				interactable = interactable2;
				break;
			}
		}
		if (interactable != null)
		{
			Interact(interactable, state, skipAnimation);
		}
	}

	public static void Interact(InteractionInstance interactionInstance)
	{
		Interact(interactionInstance.Thing, interactionInstance.Action, interactionInstance.State, interactionInstance.SkipAnimation);
	}

	public static void Interact(Interactable interactable, int state, bool skipAnimation = false)
	{
		if (!GameManager.RunSimulation || interactable == null)
		{
			return;
		}
		if (ThreadedManager.IsThread)
		{
			lock (Interactable.QueuedInteractions)
			{
				Interactable.QueuedInteractions.Enqueue(new InteractionInstance(interactable.Parent, interactable.Action, state, skipAnimation));
				return;
			}
		}
		if (GameManager.GameState != GameState.Running || (object)interactable.Parent == null)
		{
			return;
		}
		if (skipAnimation && interactable.State != state)
		{
			interactable.Interact(state);
		}
		else if ((bool)interactable.Parent && interactable.Parent.isActiveAndEnabled)
		{
			if (interactable.Parent.AllowInteraction)
			{
				interactable.Interact(state, skipAnimation);
			}
			else
			{
				interactable.InteractWhenReady(state, skipAnimation);
			}
		}
	}

	public static async UniTaskVoid PlayWorldAudioClipsData(int clipsDataNameHash, Vector3 WorldPosition, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		await UniTask.SwitchToMainThread();
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new PlayWorldAudioClipsDataMessage
			{
				ClipsDataNameHash = clipsDataNameHash,
				Position = WorldPosition,
				VolumeMultiplier = volumeMultiplier,
				PitchMultiplier = pitchMultiplier
			});
		}
	}

	public static void MoveToWorld(DynamicThing dynamicThing, float force = 0f)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new MoveToWorldMessage
			{
				ChildId = dynamicThing.ReferenceId,
				Force = force
			});
		}
		else
		{
			dynamicThing.MoveToWorld(force);
		}
	}

	public static void MoveToWorld(DynamicThing dynamicThing, Slot slot, float velocity)
	{
		if ((object)slot?.Location == null)
		{
			MoveToWorld(dynamicThing);
		}
		else
		{
			MoveToWorld(dynamicThing, slot.Location.position, slot.Location.rotation, slot.Location.forward * velocity, Vector3.zero);
		}
	}

	public static void MoveToWorld(DynamicThing dynamicThing, Vector3 position, Quaternion rotation)
	{
		MoveToWorld(dynamicThing, position, rotation, Vector3.zero, Vector3.zero);
	}

	public static void MoveToWorld(DynamicThing dynamicThing, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new MoveToWorldMessage
			{
				ChildId = dynamicThing.ReferenceId,
				Position = position,
				Rotation = rotation,
				Velocity = velocity,
				AngularVelocity = angularVelocity
			});
		}
		else
		{
			dynamicThing.MoveToWorld(position, rotation, velocity, angularVelocity);
		}
	}

	public static void SetCustomColor(Thing thing, int colorIndex)
	{
		if (!thing.HasColorState)
		{
			thing.SetCustomColor(colorIndex);
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new ThingColorMessage
				{
					ThingId = thing.netId,
					ColorIndex = thing.CustomColor.Index
				});
			}
		}
	}

	[Obsolete("This is an obsolete method, use Create<T> instead")]
	public static DynamicThing CreateOld(string prefabName, Vector3 position, Quaternion rotation)
	{
		return CreateOld(Prefab.Find<DynamicThing>(prefabName), position, rotation, 0uL);
	}

	[Obsolete("This is an obsolete method, use Create<T> instead")]
	public static DynamicThing CreateOld(string prefabName, Slot slot)
	{
		DynamicThing dynamicThing = CreateOld(Prefab.Find<DynamicThing>(prefabName), slot.Location.position, slot.Location.rotation, 0uL);
		MoveToSlotOrWorld(dynamicThing, slot);
		return dynamicThing;
	}

	public static T Create<T>(string prefabName, Slot slot) where T : Thing
	{
		return Create<T>(Prefab.Find(Animator.StringToHash(prefabName)), slot);
	}

	public static T Create<T>(Thing prefab, Slot slot) where T : Thing
	{
		T val = Create<T>(prefab, default(Vector3), default(Quaternion));
		if (val is DynamicThing dynamicThing)
		{
			bool num = !dynamicThing.Indestructable;
			if (num)
			{
				dynamicThing.Indestructable = true;
			}
			MoveToSlotOrWorld(dynamicThing, slot);
			if (num)
			{
				dynamicThing.Indestructable = false;
			}
		}
		return val;
	}

	public static T Create<T>(int prefabHash) where T : Thing
	{
		return Create<T>(Prefab.Find(prefabHash), Vector3.zero, Quaternion.identity);
	}

	public static T Create<T>(string prefabName) where T : Thing
	{
		return Create<T>(Animator.StringToHash(prefabName));
	}

	public static T Create<T>(string prefabName, Vector3 position, Quaternion rotation) where T : Thing
	{
		return Create<T>(Prefab.Find(Animator.StringToHash(prefabName)), position, rotation);
	}

	public static T Create<T>(int prefabHash, Vector3 position, Quaternion rotation) where T : Thing
	{
		return Create<T>(Prefab.Find(prefabHash), position, rotation);
	}

	public static T Create<T>(Thing prefab, Vector3 position, Quaternion rotation) where T : Thing
	{
		T val = Thing.Create<T>(prefab, position, rotation, 0L);
		if ((object)val != null)
		{
			T result = val;
			_ = val;
			return result;
		}
		throw new NullReferenceException();
	}

	public static Item CreateOrStack(Item prefab, int quantity, Vector3 position, Quaternion rotation, Slot preferredSlot = null)
	{
		if (!prefab || quantity <= 0)
		{
			return null;
		}
		Slot slot = null;
		if (preferredSlot != null)
		{
			if (!preferredSlot.Occupant)
			{
				slot = preferredSlot;
			}
			else if (prefab is Stackable targetStack)
			{
				Stackable stackable = preferredSlot.Occupant as Stackable;
				if ((bool)stackable && stackable.CanStack(targetStack))
				{
					int num = Mathf.Min(stackable.MaxQuantity - stackable.Quantity, quantity);
					if (num > 0)
					{
						stackable.AddQuantity(num);
						quantity -= num;
					}
				}
			}
		}
		if (quantity <= 0)
		{
			return null;
		}
		Item item = Create<Item>(prefab, position, rotation);
		if (!item)
		{
			return null;
		}
		if (slot != null)
		{
			MoveToSlot(item, slot);
		}
		Stackable stackable2 = item as Stackable;
		if ((bool)stackable2)
		{
			stackable2.SetQuantity(quantity);
		}
		Consumable consumable = item as Consumable;
		if ((bool)consumable)
		{
			consumable.Quantity = quantity;
		}
		return item;
	}

	[Obsolete("This is an obsolete method, use Create<T> instead")]
	public static DynamicThing CreateOld(DynamicThing prefab, Slot slot)
	{
		DynamicThing dynamicThing = CreateOld(prefab, slot.Location.position, slot.Location.rotation, 0uL);
		MoveToSlotOrWorld(dynamicThing, slot);
		return dynamicThing;
	}

	[Obsolete("This is an obsolete method, use Create<T> instead")]
	public static DynamicThing CreateOld(DynamicThing prefabDynamicThing, Vector3 position, Quaternion rotation, ulong ownerSteamId = 0uL, Rigidbody parentRigidbody = null)
	{
		DynamicThing dynamicThing = Thing.Create<DynamicThing>(prefabDynamicThing, position, rotation, 0L);
		dynamicThing.OwnerClientId = ownerSteamId;
		Entity entity = dynamicThing as Entity;
		if ((bool)entity)
		{
			entity.OnLifeCreated();
		}
		Container container = dynamicThing as Container;
		if ((bool)container)
		{
			container.InitContainer();
		}
		ItemContainer itemContainer = dynamicThing as ItemContainer;
		if ((bool)itemContainer)
		{
			itemContainer.InitContainer();
		}
		if ((bool)parentRigidbody && !dynamicThing.ActiveRigidbody.isKinematic)
		{
			dynamicThing.ActiveRigidbody.velocity = parentRigidbody.velocity;
			dynamicThing.ActiveRigidbody.angularVelocity = parentRigidbody.angularVelocity;
		}
		return dynamicThing;
	}

	public static void Destroy(Thing thing)
	{
		if (!GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			string text = (thing ? thing.DisplayName : "unknown");
			ConsoleWindow.PrintError("OnServer.Destroy called on client for " + text);
		}
		if ((bool)thing && (bool)thing.GameObject)
		{
			thing.GameObject.DestroyGameObject();
			thing.BeingDestroyed = true;
		}
	}

	public static void SendMetaData(Client client)
	{
		if (NetworkManager.IsServer)
		{
			NetworkServer.SendToClientReliable(new NetworkMessages.FromServer.GameMetaData
			{
				ConnectionId = client.connectionId,
				BytesToReceive = NetworkServer.PackagedJoinDataBytes
			}, NetworkChannel.GeneralTraffic, client).Forget();
		}
	}

	public static void SwapSlots(Slot slot1, Slot slot2)
	{
		Thing parent = slot1.Parent;
		SwapSlots(thingId2: slot2.Parent.ReferenceId, thingId1: parent.ReferenceId, slotId1: slot1.SlotIndex, slotId2: slot2.SlotIndex);
	}

	public static void SwapSlots(long thingId1, long thingId2, int slotId1, int slotId2)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SwapSlotsMessage
			{
				ThingId1 = thingId1,
				ThingId2 = thingId2,
				SlotId1 = slotId1,
				SlotId2 = slotId2
			});
		}
		else
		{
			SwapSlotsAsync(thingId1, thingId2, slotId1, slotId2).Forget();
		}
	}

	public static void SortContents(Thing thing)
	{
		if ((object)thing == null)
		{
			ConsoleWindow.PrintError("error cannot sort as thing not found");
			return;
		}
		List<Stackable> list = new List<Stackable>(thing.Slots.Count);
		foreach (Slot slot in thing.Slots)
		{
			if (slot.IsSortable && slot.Contains<Stackable>(out var occupant) && !occupant.IsStackFull && occupant.Quantity != 0)
			{
				list.Add(occupant);
			}
		}
		while (list.Count > 0)
		{
			int index = list.Count - 1;
			Stackable stackable = list[index];
			if (stackable.IsStackFull || stackable.Quantity == 0)
			{
				list.RemoveAt(index);
				continue;
			}
			List<Stackable> list2 = new List<Stackable>(list.Count);
			foreach (Stackable item2 in list)
			{
				if (!(item2 == stackable) && item2.IsColorCompatible(stackable) && stackable.CanStack(item2) && !item2.IsStackFull)
				{
					list2.Add(item2);
				}
			}
			while (list2.Count > 0 && !stackable.IsStackFull)
			{
				int index2 = list2.Count - 1;
				Stackable child = list2[index2];
				Thing.Merge(stackable, child);
				list2.RemoveAt(index2);
			}
			list.RemoveAt(index);
		}
		List<Item> list3 = new List<Item>(thing.Slots.Count);
		foreach (Slot slot2 in thing.Slots)
		{
			if (slot2.Contains<Item>(out var occupant2) && slot2.IsSortable)
			{
				list3.Add(occupant2);
			}
		}
		list3.Sort(Item.SmartSortItems);
		List<Item> list4 = new List<Item>(list3.Count);
		foreach (Item item3 in list3)
		{
			if (item3.ParentSlot != null)
			{
				if (!item3.Indestructable)
				{
					list4.Add(item3);
					item3.Indestructable = true;
				}
				MoveToWorld(item3);
			}
		}
		for (int i = 0; i < list3.Count; i++)
		{
			Item item = list3[i];
			foreach (Slot slot3 in thing.Slots)
			{
				if (slot3.IsEmpty() && Slot.AllowMove(item, slot3))
				{
					MoveToSlot(item, slot3);
					break;
				}
			}
		}
		foreach (Item item4 in list4)
		{
			item4.Indestructable = false;
		}
	}

	public static void SortContents(long thingId1)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SortContentsMessage
			{
				ThingId1 = thingId1
			});
			return;
		}
		Thing thing = Thing.Find<Thing>(thingId1);
		if ((object)thing == null)
		{
			ConsoleWindow.PrintError($"error cannot sort '{thingId1}' as thing not found");
		}
		else
		{
			SortContents(thing);
		}
	}

	private static async UniTaskVoid SwapSlotsAsync(long thingId1, long thingId2, int slotId1, int slotId2)
	{
		UniTask<(bool timedOut, Thing thing)> task = Thing.FindAsync<Thing>(thingId1);
		UniTask<(bool, Thing)> task2 = Thing.FindAsync<Thing>(thingId2);
		var (tuple2, tuple3) = await UniTask.WhenAll(task, task2);
		var (flag, thing) = tuple2;
		var (flag2, thing2) = tuple3;
		if (flag || flag2)
		{
			throw new TimeoutException();
		}
		Slot slot = ((slotId1 >= 0) ? thing.Slots[slotId1] : null);
		Slot slot2 = ((slotId2 >= 0) ? thing2.Slots[slotId2] : null);
		DynamicThing dynamicThing = (slot?.Occupant ? slot.Get() : (thing as Item));
		DynamicThing dynamicThing2 = (slot2?.Occupant ? slot2.Get() : (thing2 as Item));
		if ((object)dynamicThing == null || (object)dynamicThing2 == null)
		{
			return;
		}
		dynamicThing.DamageState.Defend = true;
		dynamicThing2.DamageState.Defend = true;
		if ((bool)dynamicThing.Joint)
		{
			MoveToWorld(dynamicThing);
			MoveToSlot(dynamicThing2, slot);
			dynamicThing.DamageState.Defend = false;
			dynamicThing2.DamageState.Defend = false;
			return;
		}
		if (slot != null && slot2 != null)
		{
			MoveToWorld(dynamicThing);
			MoveToSlot(dynamicThing2, slot);
			MoveToSlot(dynamicThing, slot2);
		}
		if (slot != null && slot2 == null)
		{
			MoveToWorld(dynamicThing);
			MoveToSlot(dynamicThing2, slot);
		}
		else if (slot2 != null && slot == null)
		{
			MoveToWorld(dynamicThing2);
			MoveToSlot(dynamicThing, slot2);
		}
		dynamicThing.DamageState.Defend = false;
		dynamicThing2.DamageState.Defend = false;
	}

	public static void SpawnSpawnData(long parentId, int spawndataIdHash)
	{
		if (NetworkManager.IsClient)
		{
			SpawnSpawnDataMessage spawnSpawnDataMessage = new SpawnSpawnDataMessage();
			spawnSpawnDataMessage.ParentId = parentId;
			spawnSpawnDataMessage.DataIdHash = spawndataIdHash;
			spawnSpawnDataMessage.SendToServer();
			return;
		}
		SpawnData spawnData = DataCollection.Get<SpawnData>(spawndataIdHash);
		if (spawnData != null)
		{
			Entity entity = Thing.Find<Entity>(parentId);
			if ((bool)entity)
			{
				Vector3 position = entity.RigidBody.worldCenterOfMass + entity.EntityForward * 1f;
				Quaternion rotation = Quaternion.AngleAxis(180f, entity.ThingTransform.up);
				spawnData.Execute(null, position, rotation);
			}
		}
	}

	public static void SpawnDynamicThingMaxStack(long parentId, string prefabName)
	{
		if (NetworkManager.IsClient)
		{
			SpawnDynamicThingMaxStackMessage spawnDynamicThingMaxStackMessage = new SpawnDynamicThingMaxStackMessage();
			spawnDynamicThingMaxStackMessage.ParentId = parentId;
			spawnDynamicThingMaxStackMessage.PrefabName = prefabName;
			spawnDynamicThingMaxStackMessage.SendToServer();
			return;
		}
		Entity entity = Thing.Find<Entity>(parentId);
		DynamicThing dynamicThing = Prefab.Find<DynamicThing>(prefabName);
		if (!dynamicThing || (object)entity == null)
		{
			ConsoleWindow.PrintError("error unable to find prefab type '" + prefabName + "'", suppressStacktrace: true);
		}
		else if (!SharedDLCManager.CheckSharedAccess(dynamicThing.DLCType))
		{
			ConsoleWindow.PrintError("error DLC not owned for " + prefabName, suppressStacktrace: true);
		}
		else
		{
			if (entity.tag == "NotSpawnable" || dynamicThing.tag == "NotSpawnable")
			{
				return;
			}
			Vector3 position = entity.RigidBody.worldCenterOfMass + entity.EntityForward * 1f;
			Quaternion rotation = Quaternion.AngleAxis(180f, entity.ThingTransform.up);
			DynamicThing dynamicThing2 = Create<DynamicThing>(dynamicThing, position, rotation);
			if (!(dynamicThing2 is Stackable stackable))
			{
				if (!(dynamicThing2 is BatteryCell batteryCell))
				{
					if (!(dynamicThing2 is Ingot ingot))
					{
						if (!(dynamicThing2 is CreditCard creditCard))
						{
							if (!(dynamicThing2 is DirtCanister dirtCanister))
							{
								if (!(dynamicThing2 is WaterBottle waterBottle))
								{
									if (dynamicThing2 is Entity entity2)
									{
										entity2.OnLifeCreated();
									}
								}
								else
								{
									waterBottle.AddLiquidToThing(waterBottle.MaxQuantity);
								}
							}
							else
							{
								dirtCanister.CurrentCollectedDirt = 8000f;
							}
						}
						else
						{
							creditCard.Currency = 8000f;
						}
					}
					else
					{
						ingot.Quantity = ingot.MaxQuantity;
					}
				}
				else
				{
					batteryCell.PowerStored = batteryCell.PowerMaximum;
				}
			}
			else
			{
				stackable.SetQuantity(stackable.MaxQuantity);
			}
		}
	}

	public static void PlaceVoxelAtWorldPosition(long toolReferenceId, Vector3 worldPosition, VoxelNodeType voxelType)
	{
		if (!GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(toolReferenceId, Animator.StringToHash("SpawnVoxel"), Vector3.left);
		}
		if (GameManager.RunSimulation)
		{
			VoxelTerrain.SetDensityWorldSpace(worldPosition, 1f, RoomChangeSource.VoxelAdd, dirtyLods: true, setNodeType: true, voxelType);
			VoxelTool voxelTool = Thing.Find<VoxelTool>(toolReferenceId);
			if ((bool)voxelTool)
			{
				voxelTool.RemoveDirtFromDirtBag();
			}
		}
		else if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new AsteroidVoxelPaintMessage
			{
				VoxelWorldPosition = worldPosition,
				Type = voxelType,
				Density = 1,
				ToolId = toolReferenceId
			});
		}
	}

	public static void MineAsteroid(long parentBrainNetId, Vector3 worldVoxelPosition, float amount, long toolId)
	{
		Vector3Int vector3Int = worldVoxelPosition.FloorToInt();
		float densityWorldSpace = VoxelTerrain.GetDensityWorldSpace(vector3Int);
		if (densityWorldSpace == 0f)
		{
			return;
		}
		float num = Mathf.Clamp(densityWorldSpace - amount, 0f, 1f);
		Human human = Thing.Find<Human>(parentBrainNetId);
		IMiningTool miningTool = Thing.Find<IMiningTool>(toolId);
		for (int i = 0; i < Vein.DirectionLookup.Length; i++)
		{
			Vector3Int vector3Int2 = vector3Int + Vein.DirectionLookup[i];
			float densityWorldSpace2 = VoxelTerrain.GetDensityWorldSpace(vector3Int2);
			if (densityWorldSpace2 == 0f)
			{
				continue;
			}
			float density = densityWorldSpace2;
			Vein veinAtPosition = Vein.GetVeinAtPosition(vector3Int2);
			if (i == 0)
			{
				miningTool.OnMinedVoxel(amount);
				VoxelTerrain.SetDensityWorldSpace(vector3Int2, num);
				density = num;
			}
			else if (veinAtPosition == null && miningTool.CursorVoxelMode == CursorVoxelMode.Flatten && densityWorldSpace2 < 0.49305883f && densityWorldSpace2 > num)
			{
				VoxelTerrain.SetDensityWorldSpace(vector3Int2, num);
				density = num;
			}
			else if (veinAtPosition == null && densityWorldSpace2 > 0f && miningTool.CursorVoxelMode == CursorVoxelMode.Default)
			{
				float num2 = Mathf.Exp(-1.5f * (vector3Int2 - vector3Int).magnitude) * amount;
				num2 *= Mathf.Clamp01(Vector3.Dot((vector3Int2 + VoxelConstants.TerrainMeshOffset - human.Position).normalized, (human.AimIkTarget - human.Position).normalized));
				num2 *= Mathf.Clamp01(Vector3.Distance(vector3Int2 + VoxelConstants.TerrainMeshOffset, human.Position));
				density = densityWorldSpace2 - num2;
				VoxelTerrain.SetDensityWorldSpace(vector3Int2, density);
			}
			if (veinAtPosition != null && Vein.ShouldReleaseMinables(VoxelTerrain.DensityToByte(density)))
			{
				Vector3 dropPosition = (worldVoxelPosition - human.CenterPosition) * 0.5f + human.CenterPosition;
				if (veinAtPosition.TryMineServer(vector3Int2, out Ore createdOre, dropPosition))
				{
					miningTool.OnMinedOre(createdOre);
					Thing.OnMinedOreInvoke(createdOre);
					Thing.OnMinedOreAmountInvoke(createdOre, createdOre.Quantity);
				}
			}
		}
		IMiningTool.MineEffect(worldVoxelPosition, MinableType.Stone, densityWorldSpace);
	}

	public static void SetVoxel(Vector3 worldVoxelPosition, byte density, MinableType type = MinableType.None, bool mined = false)
	{
		throw new NotImplementedException();
	}

	public static void UseComputer(int commandId, long motherboardId, long referenceId, int referenceInt, bool broadcastToClients, string text = "")
	{
		if (GameManager.RunSimulation && Thing.TryFind(motherboardId, out Motherboard thing))
		{
			Thing reference = Thing.Find(referenceId);
			thing.MotherboardCommand(commandId, reference, referenceInt, text);
			thing.NetworkUpdateFlags |= 256;
			if (broadcastToClients && NetworkManager.IsServer && NetworkBase.Clients.Count > 0)
			{
				Motherboard.NewCommands.Add(new MotherboardCommand(commandId, motherboardId, referenceId, referenceInt, text));
			}
		}
	}

	public static void SetCustomName(Thing thing, string customName)
	{
		SetCustomName(thing.ReferenceId, customName);
	}

	public static void SetCustomName(long targetId, string customName)
	{
		Thing.RenameThing(targetId, customName);
	}

	public static void SetRecipe(long targetId, int recipeIndex)
	{
		if (Thing.TryFind(targetId, out SimpleFabricatorBase thing))
		{
			thing.CurrentIndex = recipeIndex;
		}
	}

	public static void PublishCustomName(Thing thing, string newName)
	{
		thing.CustomName = newName;
	}

	public static void SendJetPackState(long jetpackRefId, int directionInt)
	{
		Jetpack jetpack = Thing.Find<Jetpack>(jetpackRefId);
		if (NetworkManager.IsServer)
		{
			jetpack.NetworkUpdateFlags |= 512;
			jetpack.CurrentEmission = directionInt;
		}
		if (jetpack.AnyEmissions)
		{
			if (jetpack.Activate == 0)
			{
				Interact(jetpack.InteractActivate, 1);
			}
		}
		else if (jetpack.Activate == 1)
		{
			Interact(jetpack.InteractActivate, 0);
		}
		if (jetpack.StabilizeEmission)
		{
			if (jetpack.Importing == 0)
			{
				Interact(jetpack.InteractImport, 1);
			}
		}
		else if (jetpack.Importing == 1)
		{
			Interact(jetpack.InteractImport, 0);
		}
		if (NetworkManager.IsClient)
		{
			NetworkMessages.JetpackStateMessage jetpackStateMessage = new NetworkMessages.JetpackStateMessage();
			jetpackStateMessage.DirectionInt = directionInt;
			jetpackStateMessage.JetpackId = jetpackRefId;
			jetpackStateMessage.SendToServer();
		}
	}

	public static void UseJetpack(long jetpackId, int directionInt)
	{
		if (Thing.TryFind(jetpackId, out Jetpack thing))
		{
			SendJetPackState(thing.ReferenceId, directionInt);
		}
	}

	public static void SetEntityState(long entityId, EntityState state)
	{
		if (GameManager.RunSimulation && Thing.TryFind(entityId, out Entity thing))
		{
			thing.State = state;
		}
		if (NetworkManager.IsClient)
		{
			EntityStateMessage entityStateMessage = new EntityStateMessage();
			entityStateMessage.ThingId = entityId;
			entityStateMessage.EntityState = state;
			entityStateMessage.SendToServer();
		}
	}

	public static void UseItemPrimary(Thing thing, int slotId, Vector3 targetLocation, Quaternion targetRotation, ulong steamId, ICreativeSpawnable spawnPrefab)
	{
		Item item = thing.Slots[slotId].Get<Item>();
		if (item is AuthoringTool)
		{
			item = Prefab.Find<Constructor>(spawnPrefab.SpawnId);
		}
		item?.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode: false);
	}

	public static void UseItemPrimaryAuthoring(Thing thing, int slotId, Vector3 targetLocation, Quaternion targetRotation, ulong steamId, ICreativeSpawnable spawnPrefab)
	{
		Item item = thing.Slots[slotId].Get<Item>();
		if (item is AuthoringTool)
		{
			item = Prefab.Find<Constructor>(spawnPrefab.SpawnId);
		}
		item?.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode: true);
	}

	public static void UseMultiConstructor(Thing thing, int activeHandSlotId, int inactiveHandSlotId, Vector3 targetLocation, Quaternion targetRotation, int optionIndex, bool authoringMode, ulong steamId, ICreativeSpawnable spawnPrefab)
	{
		MultiConstructor multiConstructor = thing.Slots[activeHandSlotId].Get<MultiConstructor>();
		Item offhandItem = thing.Slots[inactiveHandSlotId].Get<Item>();
		if (!multiConstructor)
		{
			multiConstructor = Prefab.Find<MultiConstructor>(spawnPrefab.SpawnId);
			if ((object)multiConstructor == null)
			{
				return;
			}
		}
		multiConstructor.Construct(targetLocation.ToGridPosition(), targetRotation, optionIndex, offhandItem, authoringMode, steamId);
	}

	public static void UseItemSecondary(Thing parent, int slotId, float completedRatio)
	{
		Item item = parent.Slots[slotId].Get<Item>();
		if ((bool)item)
		{
			item.OnUseSecondary(doAction: true, completedRatio);
		}
	}

	public static void WeldEffect(long itemId, Vector3 position, bool isEnabled)
	{
		Item item = Thing.Find<Item>(itemId);
		if (!item.RootParentHuman.HasAuthority && item is IWelder welder)
		{
			if (isEnabled)
			{
				welder.SetSparks(position);
			}
			else
			{
				welder.ResetSparks();
			}
			if (!GameManager.RunSimulation)
			{
				NetworkClient.SendToServer(new WeldingEffect
				{
					ItemId = itemId,
					Position = CursorManager.CursorHit.point,
					IsEnabled = true
				});
			}
		}
	}

	public static void RelinquishBrain(Brain playerBrain)
	{
		Brain.PlayerBrains.Remove(playerBrain.ClientId);
		playerBrain.ClientControl = false;
		playerBrain.ClientId = 0uL;
	}
}
