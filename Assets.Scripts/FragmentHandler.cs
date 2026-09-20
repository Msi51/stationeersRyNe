using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DLC;
using LZ4;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Log.RocketEvents;
using SyncedReferencables;
using TerrainSystem;
using Weather;
using WorldLogSystem;

namespace Assets.Scripts;

public static class FragmentHandler
{
	public const int MAX_FRAGMENTS_PER_FRAME = 50;

	private const bool NETWORK_COMPRESSION = true;

	public const uint PHYSICS_HEARTBEAT_TICKS = 10u;

	public static readonly FragmentStream State = new FragmentStream(NetworkChannel.StateTick, "StateTick");

	public static readonly FragmentStream Physics = new FragmentStream(NetworkChannel.PhysicsTick, "PhysicsTick");

	private static readonly List<(string name, int bytes)> _sectionBytes = new List<(string, int)>(48);

	private static int _sectionMark;

	private static bool _readingOnThread;

	public static uint NetworkTick { get; private set; }

	public static uint CurrentApplyTick { get; private set; }

	public static int LastTickCompressedBytes => State.LastCompressedBytes;

	public static int LastPhysicsCompressedBytes => Physics.LastCompressedBytes;

	public static long PhysicsRecordsApplied { get; private set; }

	public static long PhysicsRecordsSkippedUnknown { get; private set; }

	public static long PhysicsRecordsSkippedStale { get; private set; }

	public static int LastPhysicsRecordCount { get; private set; }

	public static int LastPhysicsSuppressedCount { get; private set; }

	public static IReadOnlyList<(string name, int bytes)> LastSectionBytes => _sectionBytes;

	public static void Initialize()
	{
	}

	public static void Reset()
	{
		State.Reset();
		Physics.Reset();
	}

	public static async UniTask Send(bool logging = false)
	{
		uint tick = ++NetworkTick;
		await State.Send(tick, WriteStateImmediate, logging);
		await Physics.Send(tick, WritePhysicsBatch, logging);
	}

	private static void Mark(RocketBinaryWriter writer, string name)
	{
		_sectionBytes.Add((name, writer.Position - _sectionMark));
		_sectionMark = writer.Position;
	}

	private static void WriteStateImmediate(RocketBinaryWriter writer, uint tick)
	{
		_sectionBytes.Clear();
		_sectionMark = writer.Position;
		GameManager.SerializeGameTime(writer);
		Mark(writer, "GameTime");
		Client.SerializeDeltaState(writer);
		Mark(writer, "Client");
		OrbitalSimulation.SerializeDeltaState(writer);
		Mark(writer, "OrbitalSimulation");
		TerraForming.Serialize(writer);
		Mark(writer, "TerraForming");
		VoxelTerrain.VoxelChangeEvents.Serialize(writer);
		Mark(writer, "VoxelChangeEvents");
		ExplosionEvent.NewEvents.Serialize(writer);
		Mark(writer, "ExplosionEvents");
		StructureNetwork.NewToSend.Serialize(writer);
		CableNetwork.NewToSend.Serialize(writer);
		Mark(writer, "NewNetworks");
		TraderContact.SerializeDeleted(writer);
		TraderContact.SerializeNew(writer);
		TraderContact.SerializeDirty(writer);
		Mark(writer, "TraderContacts");
		SpaceMapNode.NewToSend.Serialize(writer);
		SpaceMap.SerializeDeltaState(writer);
		SpaceMapNode.DestroyToSend.Serialize(writer);
		Mark(writer, "SpaceMap");
		RocketMotherboard.PinDeviceEvents.Serialize(writer);
		RocketMotherboard.PinLogicValueEvents.Serialize(writer);
		Mark(writer, "MotherboardPins");
		Motherboard.SerializeNewMotherboards(writer);
		RocketEvent.NewToSend.Serialize(writer);
		Mark(writer, "Motherboards+RocketEvents");
		Thing.NewToSend.Serialize(writer);
		Mark(writer, "ThingNew");
		Thing.SerializeDeltaState(writer);
		Mark(writer, "ThingDelta");
		Thing.DestroyToSend.Serialize(writer);
		Mark(writer, "ThingDestroy");
		SyncedReferencableManager.SerializeNew(writer);
		SyncedReferencableManager.SerializeDeltaState(writer);
		SyncedReferencableManager.SerializeDestroy(writer);
		Mark(writer, "SyncedReferencables");
		AudioEvent.NewEvents.Serialize(writer);
		Mark(writer, "AudioEvents");
		RebuildReferencableNetworkEvent.NewEvents.Serialize(writer);
		RebuildCableNetworkEvent.NewEvents.Serialize(writer);
		Mark(writer, "NetworkRebuilds");
		StructureNetwork.SerializeDeltaState(writer);
		Mark(writer, "StructureNetworkDelta");
		ElectricityManager.SerialiseDeltaState(writer);
		Mark(writer, "Electricity");
		NetworkManager.SerialisePlayerList(writer);
		Mark(writer, "PlayerList");
		WeatherManager.SerialiseDeltaState(writer);
		Mark(writer, "Weather");
		Rocket.SerializeNew(writer);
		Rocket.SerializeDeltaState(writer);
		Rocket.SerializeRemoved(writer);
		Mark(writer, "Rockets");
		WorldLog.SerializeNew(writer);
		WorldObjectiveState.UpdatedObjectives.Serialize(writer);
		Mark(writer, "WorldLog+Objectives");
		SharedDLCManager.SerializeDeltaState(writer);
		Achievements.AchievedEvents.Serialize(writer);
		Mark(writer, "DLC+Achievements");
		writer.WriteBoolean(AtmosphericsManager.SendToClients);
		if (AtmosphericsManager.SendToClients)
		{
			AtmosphericsManager.SendToClients = false;
			AtmosphericsManager.SerialiseDeltaState(writer);
		}
		Mark(writer, "Atmospherics");
	}

	private static void ReadStateImmediate(RocketBinaryReader reader)
	{
		GameManager.DeserializeGameTime(reader);
		Client.DeserializeDeltaState(reader);
		OrbitalSimulation.DeserializeDeltaState(reader);
		TerraForming.Deserialize(reader);
		VoxelTerrain.VoxelChangeEvents.Deserialize(reader);
		ExplosionEvent.NewEvents.Deserialize(reader);
		StructureNetwork.NewToSend.Deserialize(reader);
		CableNetwork.NewToSend.Deserialize(reader);
		TraderContact.DeserializeDeleted(reader);
		TraderContact.DeserializeNew(reader);
		TraderContact.DeserializeDirty(reader);
		SpaceMapNode.NewToSend.Deserialize(reader);
		SpaceMap.DeserializeDeltaState(reader);
		SpaceMapNode.DestroyToSend.Deserialize(reader);
		RocketMotherboard.PinDeviceEvents.Deserialize(reader);
		RocketMotherboard.PinLogicValueEvents.Deserialize(reader);
		Motherboard.DeserializeNewMotherboards(reader);
		RocketEvent.NewToSend.Deserialize(reader);
		Thing.NewToSend.Deserialize(reader);
		Thing.DeserializeDeltaState(reader);
		Thing.DestroyToSend.Deserialize(reader);
		SyncedReferencableManager.DeserializeNew(reader);
		SyncedReferencableManager.DeserializeDeltaState(reader);
		SyncedReferencableManager.DeserializeDestroy(reader);
		AudioEvent.NewEvents.Deserialize(reader);
		RebuildReferencableNetworkEvent.NewEvents.Deserialize(reader);
		RebuildCableNetworkEvent.NewEvents.Deserialize(reader);
		StructureNetwork.DeserializeDeltaState(reader);
		ElectricityManager.DeserializeDeltaState(reader);
		NetworkManager.DeserialisePlayerList(reader);
		WeatherManager.DeserialiseDeltaState(reader);
		Rocket.DeserializeNew(reader);
		Rocket.DeserializeDeltaState(reader);
		Rocket.DeSerializeRemoved(reader);
		WorldLog.DeserializeNew(reader);
		WorldObjectiveState.UpdatedObjectives.Deserialize(reader);
		SharedDLCManager.DeserializeDeltaState(reader);
		Achievements.AchievedEvents.Deserialize(reader);
		if (reader.ReadBoolean())
		{
			while (_readingOnThread)
			{
				Thread.Sleep(1);
			}
			DeserialiseOnThread(reader).Forget();
		}
	}

	public static async UniTaskVoid DeserialiseOnThread(RocketBinaryReader reader)
	{
		_readingOnThread = true;
		await UniTask.SwitchToThreadPool();
		try
		{
			AtmosphericsManager.DeserialiseDeltaState(reader);
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
			reader.Close();
			_readingOnThread = false;
		}
		finally
		{
			reader.Close();
			_readingOnThread = false;
		}
	}

	private static void WritePhysicsBatch(RocketBinaryWriter writer, uint tick)
	{
		int num = 0;
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current is DynamicThing dynamicThing && (bool)dynamicThing && dynamicThing.ReferenceId != 0L && !dynamicThing.IsBeingDestroyed)
			{
				DynamicThingPosition record;
				switch (dynamicThing.BuildPhysicsBatchRecord(tick, out record))
				{
				case PhysicsBatchResult.Write:
					record.Write(writer);
					count++;
					break;
				case PhysicsBatchResult.Suppressed:
					num++;
					break;
				}
			}
		}
		LastPhysicsRecordCount = (int)count;
		LastPhysicsSuppressedCount = num;
		if (count == 0)
		{
			writer.Reset();
		}
		else
		{
			Network.WriteIndex(writer, count, bufferIndex);
		}
	}

	public static void ReceiveState(byte[] bytes, int size)
	{
		State.Receive(bytes, size, ApplyState);
	}

	public static void ReceivePhysics(byte[] bytes, int size)
	{
		Physics.Receive(bytes, size, ApplyPhysicsBatch);
	}

	private static void ApplyState(byte[] buffer, uint tick)
	{
		int count = BitConverter.ToInt32(buffer, 0);
		RocketBinaryReader reader = new RocketBinaryReader(new MemoryStream(LZ4Codec.Unwrap(buffer), 0, count));
		CurrentApplyTick = tick;
		ReadStateImmediate(reader);
	}

	private static void ApplyPhysicsBatch(byte[] buffer, uint tick)
	{
		int count = BitConverter.ToInt32(buffer, 0);
		using MemoryStream stream = new MemoryStream(LZ4Codec.Unwrap(buffer), 0, count);
		RocketBinaryReader rocketBinaryReader = new RocketBinaryReader(stream);
		try
		{
			Network.ReadIndex<uint>(rocketBinaryReader, out var value);
			for (int i = 0; i < value; i++)
			{
				DynamicThingPosition updateData = default(DynamicThingPosition);
				updateData.Read(rocketBinaryReader);
				DynamicThing dynamicThing = Referencable.Find<DynamicThing>(updateData.DynamicThingId);
				if ((object)dynamicThing == null || dynamicThing.IsBeingDestroyed)
				{
					PhysicsRecordsSkippedUnknown++;
					continue;
				}
				if (tick < dynamicThing.LastPhysicsTick)
				{
					PhysicsRecordsSkippedStale++;
					continue;
				}
				dynamicThing.LastPhysicsTick = tick;
				dynamicThing.ProcessPhysicsUpdate(updateData);
				PhysicsRecordsApplied++;
			}
		}
		finally
		{
			rocketBinaryReader.Close();
		}
	}
}
