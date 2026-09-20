using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Weapons;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public abstract class NetworkMessages
{
	public class Handshake : ProcessedMessage<Handshake>
	{
		public long ConnectionId { get; set; }

		public string Username { get; set; }

		public ulong ClientId { get; set; }

		public HandshakeType HandshakeState { get; set; }

		public string LoadingState { get; set; }

		public byte LoadingProgress { get; set; }

		public string Message { get; set; }

		public override void Process(long hostId)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.Handshake(this);
			}
			else
			{
				NetworkServer.Handshake(this);
			}
		}

		public override void Deserialize(RocketBinaryReader reader)
		{
			ConnectionId = reader.ReadInt64();
			Username = reader.ReadString();
			ClientId = reader.ReadUInt64();
			HandshakeState = (HandshakeType)reader.ReadByte();
			LoadingState = reader.ReadString();
			LoadingProgress = reader.ReadByte();
			Message = reader.ReadString();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(ConnectionId);
			writer.WriteString(Username);
			writer.WriteUInt64(ClientId);
			writer.WriteByte((byte)HandshakeState);
			writer.WriteString(LoadingState);
			writer.WriteByte(LoadingProgress);
			writer.WriteString(Message);
		}

		public static byte ProgressToByte(float progressF)
		{
			return (byte)Math.Round(progressF * 100f);
		}

		public float ProgressByteToFloat()
		{
			return (float)(int)LoadingProgress / 100f;
		}
	}

	public class PhysicsUpdateMessage : MessageBase<PhysicsUpdateMessage>
	{
		public long OwnerConnectionId = -1L;

		public long DynamicThingId;

		public Vector3 WorldPosition;

		public Quaternion WorldRotation;

		public Vector3 AngularVelocity;

		public Vector3 Velocity;

		public Vector3 ChildPosition;

		public float VelocityMagnitude;

		public Vector3 LocalPosition;

		public bool UseLocal;

		public bool ForceUpdate;

		public long MothershipId;

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			DynamicThingId = reader.ReadInt32();
			WorldPosition = reader.ReadVector3();
			WorldRotation = reader.ReadQuaternion();
			AngularVelocity = reader.ReadVector3();
			Velocity = reader.ReadVector3();
			ChildPosition = reader.ReadVector3();
			VelocityMagnitude = reader.ReadUInt32();
			LocalPosition = reader.ReadVector3();
			UseLocal = reader.ReadBoolean();
			ForceUpdate = reader.ReadBoolean();
			MothershipId = reader.ReadUInt32();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteInt32((int)DynamicThingId);
			writer.WriteVector3(WorldPosition);
			writer.WriteQuaternion(WorldRotation);
			writer.WriteVector3(AngularVelocity);
			writer.WriteVector3(Velocity);
			writer.WriteVector3(ChildPosition);
			writer.WriteSingle(VelocityMagnitude);
			writer.WriteVector3(LocalPosition);
			writer.WriteBoolean(UseLocal);
			writer.WriteBoolean(ForceUpdate);
			writer.WriteInt32((int)MothershipId);
		}
	}

	public class CreateThing : MessageBase<CreateThing>
	{
		public long OwnerConnectionId = -1L;

		public int prefabHash;

		public Vector3 worldPosition;

		public Quaternion worldRotation;

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			prefabHash = reader.ReadInt32();
			worldPosition = reader.ReadVector3();
			worldRotation = reader.ReadQuaternion();
			Thing thing = Prefab.Find(prefabHash);
			if (!(thing == null))
			{
				Thing.Create<Thing>(thing, worldPosition, worldRotation, 0L);
				Debug.LogError("Spawned Item using new network");
			}
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteInt32(prefabHash);
			writer.WriteVector3(worldPosition);
			writer.WriteQuaternion(worldRotation);
		}
	}

	public class VerifyPlayer : ProcessedMessage<VerifyPlayer>
	{
		public long OwnerConnectionId = -1L;

		public ulong ClientId;

		public string Name;

		public string Password;

		public string Version;

		public ConnectionMethod ClientConnectionMethod = ConnectionMethod.None;

		public override void Process(long hostId)
		{
			ConsoleWindow.Print($"Process verify player {ClientId}");
			NetworkServer.VerifyConnection(hostId, this);
		}

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			ClientId = reader.ReadUInt64();
			Name = reader.ReadString();
			Password = reader.ReadString();
			Version = reader.ReadString();
			ClientConnectionMethod = (ConnectionMethod)reader.ReadByte();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteUInt64(ClientId);
			writer.WriteString(Name);
			writer.WriteString(Password);
			writer.WriteString(Version);
			writer.WriteByte((byte)ClientConnectionMethod);
		}
	}

	public class VerifyPlayerRequest : ProcessedMessage<VerifyPlayerRequest>
	{
		public long OwnerConnectionId { get; set; } = -1L;

		public long ClientConnectionID { get; set; }

		public bool PasswordRequired { get; set; }

		public ConnectionMethod ClientConnectionMethod { get; set; } = ConnectionMethod.None;

		public override void Process(long hostId)
		{
			if (!NetworkManager.IsClient)
			{
				return;
			}
			ConsoleWindow.Print($"id received by server {NetworkManager.LocalClientId}");
			NetworkClient.StopConnectionTimer();
			VerifyPlayer msg = new VerifyPlayer
			{
				OwnerConnectionId = ClientConnectionID,
				ClientConnectionMethod = ClientConnectionMethod,
				ClientId = NetworkManager.LocalClientId,
				Name = NetworkManager.Username,
				Version = GameManager.GetGameVersion()
			};
			if (PasswordRequired)
			{
				PasswordWindow.PromptPassword(delegate(string pw)
				{
					msg.Password = pw;
					NetworkClient.SendToServer(msg);
				}, NetworkClient.Cancel);
			}
			else
			{
				ConsoleWindow.Print("Client sending player response with connection method: " + msg.ClientConnectionMethod);
				NetworkClient.SendToServer(msg);
			}
		}

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			ClientConnectionID = reader.ReadInt64();
			PasswordRequired = reader.ReadBoolean();
			ClientConnectionMethod = (ConnectionMethod)reader.ReadByte();
			ConsoleWindow.Print("VerifyPlayer - Deserialising connection method: " + ClientConnectionMethod);
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteInt64(ClientConnectionID);
			writer.WriteBoolean(PasswordRequired);
			writer.WriteByte((byte)ClientConnectionMethod);
			ConsoleWindow.Print("VerifyPlayer - Serialising connection method: " + ClientConnectionMethod);
		}
	}

	public class DisconnectClient : MessageBase<DisconnectClient>
	{
		public long OwnerConnectionId = -1L;

		public string Reason;

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			Reason = reader.ReadString();
			if (NetworkManager.IsClient)
			{
				NetworkClient.Disconnect().Forget();
			}
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteString(Reason);
		}
	}

	public class Interact : MessageBase<Interact>
	{
		private Interaction _interaction;

		private Interactable _interactable;

		public long OwnerConnectionId = -1L;

		public long DestinationId;

		public long SourceId;

		public int SourceSlotId;

		public int InteractionId;

		public int State;

		public bool Force;

		public bool AltKey;

		public override void Deserialize(RocketBinaryReader reader)
		{
			OwnerConnectionId = reader.ReadInt64();
			DestinationId = reader.ReadInt32();
			SourceId = reader.ReadInt32();
			SourceSlotId = reader.ReadInt32();
			InteractionId = reader.ReadInt32();
			State = reader.ReadInt32();
			Force = reader.ReadBoolean();
			AltKey = reader.ReadBoolean();
			Processmsg(MessageBase<Interact>.Singleton);
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, OwnerConnectionId);
			}
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(OwnerConnectionId);
			writer.WriteInt32((int)DestinationId);
			writer.WriteInt32((int)SourceId);
			writer.WriteInt32(SourceSlotId);
			writer.WriteInt32(InteractionId);
			writer.WriteInt32(State);
			writer.WriteBoolean(Force);
			writer.WriteBoolean(AltKey);
		}

		public async UniTaskVoid InteractionWait(Thing thing)
		{
			float framesAttempted = 0f;
			while (!_interactable.Animator.isInitialized && !thing.AllowInteraction)
			{
				float num = framesAttempted + 1f;
				framesAttempted = num;
				if (num > 1000f)
				{
					Debug.LogError("Timed out waiting for " + thing.DisplayName + " to be ready to apply interaction", thing);
					return;
				}
				await UniTask.NextFrame();
			}
			if (!thing.PreventInteraction(out var _, _interactable, _interaction))
			{
				thing.InteractWith(_interactable, _interaction);
			}
		}

		public void Processmsg(Interact updateData)
		{
			Thing thing = Thing.Find(DestinationId);
			Thing thing2 = Thing.Find(SourceId);
			if (thing == null || thing2 == null)
			{
				return;
			}
			_interactable = thing.Interactables[InteractionId];
			_interaction = new Interaction(thing2, thing2.Slots[SourceSlotId], thing, AltKey);
			Thing.DelayedActionInstance failResult;
			if (Force && (bool)_interactable.Animator)
			{
				if (_interactable.State != State)
				{
					InteractionWait(thing).Forget();
				}
			}
			else if (!thing.PreventInteraction(out failResult, _interactable, _interaction))
			{
				thing.InteractWith(_interactable, _interaction);
				_interactable.UpdateDisplay();
			}
		}
	}

	public class ThingUpdate : MessageBase<ThingUpdate>
	{
		public uint ReferenceId;

		public Vector3 WorldPosition;

		public Vector3 WorldRotation;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ReferenceId = reader.ReadUInt32();
			WorldPosition = reader.ReadVector3();
			WorldRotation = reader.ReadVector3();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt32(ReferenceId);
			writer.WriteVector3(WorldPosition);
			writer.WriteVector3(WorldRotation);
		}
	}

	public abstract class FromServer
	{
		public class GameMetaData : MessageBase<GameMetaData>
		{
			public long ConnectionId;

			public int CurrentDateInt;

			public uint BytesToReceive;

			public override void Serialize(RocketBinaryWriter writer)
			{
				writer.WriteInt64(ConnectionId);
				writer.WriteInt32(CurrentDateInt);
				writer.WriteUInt32(BytesToReceive);
				writer.WriteUInt32(NetworkServer.ReferencableCount);
			}

			public override void Deserialize(RocketBinaryReader reader)
			{
				NetworkClient.ConnectionId = (ConnectionId = reader.ReadInt64());
				GameManager.GameState = GameState.Joining;
				CurrentDateInt = reader.ReadInt32();
				BytesToReceive = reader.ReadUInt32();
				ConsoleWindow.Print($"BytesToReceive: {BytesToReceive}");
				NetworkClient.ThingCount = reader.ReadUInt32();
				ConsoleWindow.Print($"ThingCount: {NetworkClient.ThingCount}");
				NetworkClient.JoinPackageTotalBytes = BytesToReceive;
				NetworkClient.JoinPackageBytes = new byte[BytesToReceive];
				NetworkClient.JoinBytesUntilUpdate = Mathf.RoundToInt((float)BytesToReceive / 100f);
			}
		}
	}

	public class ExplodeMessage : MessageBase<ExplodeMessage>
	{
		public long ThingId;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ThingId = reader.ReadInt64();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(ThingId);
		}
	}

	public class ExplosionMessage : MessageBase<ExplosionMessage>
	{
		public Vector3 Position;

		public float ExplosionForce;

		public float ExplosionRadius;

		public override void Deserialize(RocketBinaryReader reader)
		{
			Position = reader.ReadVector3();
			ExplosionForce = reader.ReadSingle();
			ExplosionRadius = reader.ReadSingle();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteVector3(Position);
			writer.WriteSingle(ExplosionForce);
			writer.WriteSingle(ExplosionRadius);
		}
	}

	public class GameStateMessage : MessageBase<GameStateMessage>
	{
		public int GameState;

		public float ServerTime;

		public override void Deserialize(RocketBinaryReader reader)
		{
			GameState = reader.ReadInt32();
			ServerTime = reader.ReadSingle();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt32(GameState);
			writer.WriteSingle(ServerTime);
		}
	}

	public class ClientConnectionMessage : MessageBase<ClientConnectionMessage>
	{
		public ulong SteamId;

		public string SteamName;

		public string Password;

		public bool ValidMissionServer;

		public uint MissionServerIp;

		public ushort MissionServerGamePort;

		public short PlayerControllerId;

		public override void Deserialize(RocketBinaryReader reader)
		{
			SteamId = reader.ReadUInt64();
			SteamName = reader.ReadString();
			Password = reader.ReadString();
			ValidMissionServer = reader.ReadBoolean();
			MissionServerIp = reader.ReadUInt32();
			MissionServerGamePort = reader.ReadUInt16();
			PlayerControllerId = reader.ReadInt16();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(SteamId);
			writer.WriteString(SteamName);
			writer.WriteString(Password);
			writer.WriteBoolean(ValidMissionServer);
			writer.WriteUInt32(MissionServerIp);
			writer.WriteUInt16(MissionServerGamePort);
			writer.WriteInt16(PlayerControllerId);
		}
	}

	public class CollisionMessage : MessageBase<CollisionMessage>
	{
		public long ThingId;

		public long OtherThingId;

		public Vector3 ContactPoint;

		public Vector3 RelativeVelocity;

		public float ImpulseMagnitude;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ThingId = reader.ReadInt64();
			OtherThingId = reader.ReadInt64();
			ContactPoint = reader.ReadVector3();
			RelativeVelocity = reader.ReadVector3();
			ImpulseMagnitude = reader.ReadSingle();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(ThingId);
			writer.WriteInt64(OtherThingId);
			writer.WriteVector3(ContactPoint);
			writer.WriteVector3(RelativeVelocity);
			writer.WriteSingle(ImpulseMagnitude);
		}
	}

	public class GenerateRoomMessage : MessageBase<GenerateRoomMessage>
	{
		public Vector3[] Grids;

		public override void Deserialize(RocketBinaryReader reader)
		{
			short num = reader.ReadInt16();
			Grids = new Vector3[num];
			for (int i = 0; i < num; i++)
			{
				Grids[i] = reader.ReadVector3();
			}
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt16((short)Grids.Length);
			Vector3[] grids = Grids;
			foreach (Vector3 value in grids)
			{
				writer.WriteVector3(value);
			}
		}
	}

	public class JetpackStateMessage : ProcessedMessage<JetpackStateMessage>
	{
		public long JetpackId;

		public int DirectionInt;

		public override void Process(long hostId)
		{
			base.Process(hostId);
			OnServer.SendJetPackState(JetpackId, DirectionInt);
		}

		public override void Deserialize(RocketBinaryReader reader)
		{
			JetpackId = reader.ReadInt64();
			DirectionInt = reader.ReadInt32();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(JetpackId);
			writer.WriteInt32(DirectionInt);
		}
	}

	public class InteractionSyncMessage : MessageBase<InteractionSyncMessage>
	{
		public long ThingId;

		public int InteractionIndex;

		public int State;

		public bool SkipAnimation;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ThingId = reader.ReadInt64();
			InteractionIndex = reader.ReadInt32();
			State = reader.ReadInt32();
			SkipAnimation = reader.ReadBoolean();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt64(ThingId);
			writer.WriteInt32(InteractionIndex);
			writer.WriteInt32(State);
			writer.WriteBoolean(SkipAnimation);
		}
	}

	public class ResetManagerMessage : MessageBase<ResetManagerMessage>
	{
		public int ManagerType;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ManagerType = reader.ReadInt32();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt32(ManagerType);
		}
	}

	public class GasMixtureMessage : MessageBase<GasMixtureMessage>
	{
		public Vector3 Position;

		public Vector3 Direction;

		public float Oxygen;

		public float Nitrogen;

		public float CarbonDioxide;

		public float Propane;

		public float Chlorine;

		public float Water;

		public float NitrousOxide;

		public float TotalEnergy;

		public long ThingId;

		public float CleanBurnRate;

		public float HeatEnergyRate;

		public override void Deserialize(RocketBinaryReader reader)
		{
			Position = reader.ReadVector3();
			Direction = reader.ReadVector3();
			Oxygen = reader.ReadSingle();
			Nitrogen = reader.ReadSingle();
			CarbonDioxide = reader.ReadSingle();
			Propane = reader.ReadSingle();
			Chlorine = reader.ReadSingle();
			Water = reader.ReadSingle();
			NitrousOxide = reader.ReadSingle();
			TotalEnergy = reader.ReadSingle();
			ThingId = reader.ReadInt64();
			CleanBurnRate = reader.ReadSingle();
			HeatEnergyRate = reader.ReadSingle();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteVector3(Position);
			writer.WriteVector3(Direction);
			writer.WriteSingle(Oxygen);
			writer.WriteSingle(Nitrogen);
			writer.WriteSingle(CarbonDioxide);
			writer.WriteSingle(Propane);
			writer.WriteSingle(Chlorine);
			writer.WriteSingle(Water);
			writer.WriteSingle(NitrousOxide);
			writer.WriteSingle(TotalEnergy);
			writer.WriteInt64(ThingId);
			writer.WriteSingle(CleanBurnRate);
			writer.WriteSingle(HeatEnergyRate);
		}
	}

	public class AtmosphereDestroyMessage : MessageBase<AtmosphereDestroyMessage>
	{
		public WorldGrid WorldGrid;

		public long MothershipId;

		public override void Deserialize(RocketBinaryReader reader)
		{
			WorldGrid = reader.ReadWorldGrid();
			MothershipId = reader.ReadInt64();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteWorldGrid(WorldGrid);
			writer.WriteInt64(MothershipId);
		}
	}

	public class PlayerConnectionMessage : MessageBase<PlayerConnectionMessage>
	{
		public ulong SteamId;

		public long BrainNetId;

		public string PlayerName;

		public float StartPlayTime;

		public GameState State;

		public override void Deserialize(RocketBinaryReader reader)
		{
			SteamId = reader.ReadUInt64();
			BrainNetId = reader.ReadInt64();
			PlayerName = reader.ReadString();
			StartPlayTime = reader.ReadSingle();
			State = (GameState)reader.ReadByte();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(SteamId);
			writer.WriteInt64(BrainNetId);
			writer.WriteString(PlayerName);
			writer.WriteSingle(StartPlayTime);
			writer.WriteByte((byte)State);
		}
	}

	public class SteamServerIdMessage : MessageBase<SteamServerIdMessage>
	{
		public ulong ServerSteamId;

		public ulong HostSteamId;

		public bool DedicatedServer;

		public uint ServerPublicIp;

		public string WorldType;

		public int BedrockLevel;

		public int LavaLevel;

		public bool HasLava;

		public int Seed;

		public SpawnGas[] SpawnGases;

		public string AsteroidMaterial;

		public string SkyboxMaterial;

		public float Temperature;

		public GameMode ServerGameMode;

		public float SunOrbitPeriodMultiplier;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ServerSteamId = reader.ReadUInt64();
			HostSteamId = reader.ReadUInt64();
			DedicatedServer = reader.ReadBoolean();
			ServerPublicIp = reader.ReadUInt32();
			WorldType = reader.ReadString();
			BedrockLevel = reader.ReadInt32();
			LavaLevel = reader.ReadInt32();
			HasLava = reader.ReadBoolean();
			Seed = reader.ReadInt32();
			short num = reader.ReadInt16();
			SpawnGases = new SpawnGas[num];
			for (int i = 0; i < num; i++)
			{
				SpawnGases[i] = new SpawnGas();
				SpawnGases[i].Deserialize(reader);
			}
			AsteroidMaterial = reader.ReadString();
			SkyboxMaterial = reader.ReadString();
			Temperature = reader.ReadSingle();
			ServerGameMode = (GameMode)reader.ReadByte();
			SunOrbitPeriodMultiplier = reader.ReadSingle();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(ServerSteamId);
			writer.WriteUInt64(HostSteamId);
			writer.WriteBoolean(DedicatedServer);
			writer.WriteUInt32(ServerPublicIp);
			writer.WriteString(WorldType);
			writer.WriteInt32(BedrockLevel);
			writer.WriteInt32(LavaLevel);
			writer.WriteBoolean(HasLava);
			writer.WriteInt32(Seed);
			writer.WriteInt16((short)SpawnGases.Length);
			SpawnGas[] spawnGases = SpawnGases;
			for (int i = 0; i < spawnGases.Length; i++)
			{
				spawnGases[i].Serialize(writer);
			}
			writer.WriteString(AsteroidMaterial);
			writer.WriteString(SkyboxMaterial);
			writer.WriteSingle(Temperature);
			writer.WriteByte((byte)ServerGameMode);
			writer.WriteSingle(SunOrbitPeriodMultiplier);
		}
	}

	public class ValidateAuthBlobMessage : MessageBase<ValidateAuthBlobMessage>
	{
		public byte[] AuthBlob = new byte[1024];

		public override void Deserialize(RocketBinaryReader reader)
		{
			short num = reader.ReadInt16();
			AuthBlob = new byte[num];
			for (int i = 0; i < num; i++)
			{
				AuthBlob[i] = reader.ReadByte();
			}
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt16((short)AuthBlob.Length);
			byte[] authBlob = AuthBlob;
			foreach (byte value in authBlob)
			{
				writer.WriteByte(value);
			}
		}
	}

	public class SpawnIncidentResultFromServer : MessageBase<SpawnIncidentResultFromServer>
	{
		public int MessageHash;

		public override void Deserialize(RocketBinaryReader reader)
		{
			MessageHash = reader.ReadInt32();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteInt32(MessageHash);
		}
	}

	public class PlayerInfoMessage : MessageBase<PlayerInfoMessage>
	{
		public ulong ClientId;

		public int Score;

		public float StartPlayTime;

		public int Ping;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ClientId = reader.ReadUInt64();
			Score = reader.ReadInt32();
			StartPlayTime = reader.ReadSingle();
			Ping = reader.ReadInt32();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(ClientId);
			writer.WriteInt32(Score);
			writer.WriteSingle(StartPlayTime);
			writer.WriteInt32(Ping);
		}
	}

	public enum KickCode : byte
	{
		Normal,
		WrongPassword,
		ExceedMaxPlayer,
		Duplicated,
		Custom
	}

	public class KickPlayerMessage : MessageBase<KickPlayerMessage>
	{
		public KickCode Status;

		public string Reason;

		public override void Deserialize(RocketBinaryReader reader)
		{
			Status = (KickCode)reader.ReadByte();
			Reason = reader.ReadString();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteByte((byte)Status);
			writer.WriteString(Reason);
		}
	}

	public class BanPlayerMessage : MessageBase<BanPlayerMessage>
	{
		public string RemainingTime;

		public string Reason;

		public override void Deserialize(RocketBinaryReader reader)
		{
			RemainingTime = reader.ReadString();
			Reason = reader.ReadString();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteString(RemainingTime);
			writer.WriteString(Reason);
		}
	}

	public class ClientUpdatePlayerDataMessage : MessageBase<ClientUpdatePlayerDataMessage>
	{
		public ulong ClientId;

		public uint ServerIp;

		public ushort ServerPort;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ClientId = reader.ReadUInt64();
			ServerIp = reader.ReadUInt32();
			ServerPort = reader.ReadUInt16();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(ClientId);
			writer.WriteUInt32(ServerIp);
			writer.WriteUInt16(ServerPort);
		}
	}

	public class ServerNoticeMessage : MessageBase<ServerNoticeMessage>
	{
		public string NoticeMessage;

		public override void Deserialize(RocketBinaryReader reader)
		{
			NoticeMessage = reader.ReadString();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteString(NoticeMessage);
		}
	}

	public class TrackerMessage : MessageBase<TrackerMessage>
	{
		public override void Deserialize(RocketBinaryReader reader)
		{
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
		}
	}

	public class ExplosiveLinkMessage : ProcessedMessage<ExplosiveLinkMessage>
	{
		public long ExplosiveId;

		public long DetonatorId;

		public bool ToLink;

		public override void Process(long hostId)
		{
			base.Process(hostId);
			ItemRemoteDetonator itemRemoteDetonator = Referencable.Find<ItemRemoteDetonator>(DetonatorId);
			ItemExplosive itemExplosive = Referencable.Find<ItemExplosive>(ExplosiveId);
			if ((object)itemRemoteDetonator != null && (object)itemExplosive != null)
			{
				if (ToLink)
				{
					itemExplosive.Link(itemRemoteDetonator);
				}
				else
				{
					itemExplosive.Unlink();
				}
				itemRemoteDetonator.RefreshMode();
			}
		}

		public override void Deserialize(RocketBinaryReader reader)
		{
			Network.ReadPackedId(reader, out ExplosiveId);
			Network.ReadPackedId(reader, out DetonatorId);
			ToLink = reader.ReadBoolean();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			Network.WritePackedId(writer, ExplosiveId);
			Network.WritePackedId(writer, DetonatorId);
			writer.WriteBoolean(ToLink);
		}
	}

	public class DisconnectP2PMessage : MessageBase<DisconnectP2PMessage>
	{
		public ulong ClientId;

		public override void Deserialize(RocketBinaryReader reader)
		{
			ClientId = reader.ReadUInt64();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt64(ClientId);
		}
	}

	public class UpdatePauseMessage : MessageBase<UpdatePauseMessage>
	{
		public bool Pause;

		public static event Action<bool> OnPauseChanged;

		public override void Deserialize(RocketBinaryReader reader)
		{
			Pause = reader.ReadBoolean();
			UpdatePauseMessage.OnPauseChanged?.Invoke(Pause);
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteBoolean(Pause);
		}
	}

	public class PreLoadingMessage : MessageBase<PreLoadingMessage>
	{
		public bool IsWorldOriginRebased;

		public Vector3 OriginPosition;

		public override void Deserialize(RocketBinaryReader reader)
		{
			IsWorldOriginRebased = reader.ReadBoolean();
			OriginPosition = reader.ReadVector3();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteBoolean(IsWorldOriginRebased);
			writer.WriteVector3(OriginPosition);
		}
	}

	public class TravelWorldMessage : MessageBase<TravelWorldMessage>
	{
		public string WorldName;

		public string MapName;

		public override void Deserialize(RocketBinaryReader reader)
		{
			WorldName = reader.ReadString();
			MapName = reader.ReadString();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteString(WorldName);
			writer.WriteString(MapName);
		}
	}

	public class FireProjectileMessage : ProcessedMessage<FireProjectileMessage>
	{
		public long LauncherReferenceId;

		public Vector3 Origin;

		public Vector3 Velocity;

		public override void Deserialize(RocketBinaryReader reader)
		{
			Network.ReadPackedId(reader, out LauncherReferenceId);
			Velocity = reader.ReadVector3();
			Origin = reader.ReadVector3();
		}

		public override void Serialize(RocketBinaryWriter writer)
		{
			Network.WritePackedId(writer, LauncherReferenceId);
			writer.WriteVector3(Velocity);
			writer.WriteVector3(Origin);
		}

		public override void Process(long hostId)
		{
			base.Process(hostId);
			Referencable.Find<ProjectileLauncher>(LauncherReferenceId)?.Fire(Origin, Velocity);
		}
	}
}
