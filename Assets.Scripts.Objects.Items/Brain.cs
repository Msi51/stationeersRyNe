using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using CharacterCustomisation;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Brain : Organ, ITrackable
{
	public static readonly Dictionary<ulong, Brain> PlayerBrains = new Dictionary<ulong, Brain>();

	[ReadOnly]
	public ulong ClientId;

	[ReadOnly]
	public string SteamName;

	[ReadOnly]
	public float StartPlayTime;

	private static readonly float FadeInDuration = 6f;

	private static readonly float FadeInDelay = 1.5f;

	private float _animationTime;

	private bool _sendAnimation;

	public bool LocalControl;

	private bool _clientControl;

	private static readonly float _resetPlayerConsciousStateStun = 0f;

	private static readonly float _unconsciousStateStun = 200f;

	private bool _positionSent;

	private Vector3 _lastPosition;

	private Quaternion _lastRotation;

	private const float DAMAGE_TICK_NO_OXYGEN = 0.2f;

	private const float STUN_TICK_NO_OXYGEN = 3f;

	public const float STUN_TICK_NITRUS_OXIDE = 2f;

	private const float UNCONSCIOUS_DAMAGE_REDUCTION = 0.5f;

	private bool hasControl;

	public Human ParentHuman => ParentEntity as Human;

	public bool IsOnline => Client.Find(ClientId) != null;

	public PlayerCosmetics HumanCosmetics
	{
		get
		{
			if (!ParentHuman)
			{
				return null;
			}
			return ParentHuman.CosmeticData;
		}
	}

	public override string TrackableName
	{
		get
		{
			if ((bool)ParentBodyBag)
			{
				string arg = ((!string.IsNullOrEmpty(ParentBodyBag.PlayersDisplayName)) ? ParentBodyBag.PlayersDisplayName : GameStrings.UnidentifiedPlayer.DisplayString);
				return GameStrings.TrackablePlayerBodyBag.AsString(arg);
			}
			if ((bool)ParentHuman)
			{
				return ParentHuman.DisplayName;
			}
			return base.TrackableName;
		}
	}

	public override bool HasAuthority => LocalControl;

	[ByteArraySync]
	public bool ClientControl
	{
		get
		{
			return _clientControl;
		}
		set
		{
			if (_clientControl != value)
			{
				_clientControl = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
			}
		}
	}

	public override string ToConsoleString()
	{
		return string.Format("client:{0} '{1}' {2}", ClientId, SteamName, IsOnline ? "online" : "offline");
	}

	public override void Awake()
	{
		base.Awake();
		ITrackable.Trackables.Add(this);
	}

	public override void Start()
	{
		base.Start();
		TradeValue = UnityEngine.Random.Range(100, 101);
	}

	public void TakeControlInBodyBag()
	{
		if (!GameManager.IsBatchMode)
		{
			if ((bool)ParentBodyBag)
			{
				InitializeCamera(ParentBodyBag);
			}
			XmlSaveLoad.IsReadyToPlayWorldAudio = true;
			InventoryManager.ParentBrain = this;
			ListenerEffectManager.WorldVolumeMultiplier = 1f;
			LocalControl = true;
			if (ClientId != 0)
			{
				PlayerBrains[ClientId] = this;
			}
		}
	}

	public void TakeControl(bool setPhysics = true)
	{
		if (hasControl)
		{
			return;
		}
		hasControl = true;
		if (GameManager.IsBatchMode)
		{
			return;
		}
		XmlSaveLoad.IsReadyToPlayWorldAudio = true;
		InventoryManager.ParentBrain = this;
		ListenerEffectManager.WorldVolumeMultiplier = 1f;
		LocalControl = true;
		ParentEntity?.RefreshClothing();
		if ((bool)ParentEntity && !(this != ParentEntity.OrganBrain))
		{
			InventoryManager.Parent = ParentEntity;
			if (ClientId != 0)
			{
				PlayerBrains[ClientId] = this;
			}
			ParentEntity.TakeControl(setPhysics);
			InitializeCamera(ParentHuman);
			if (HasAuthority && ParentEntity.ParentSlot?.Parent is IExitable exitable)
			{
				ParentEntity.HasAcquiredControlInExitable(exitable);
			}
		}
	}

	private void InitializeCamera(Thing parent)
	{
		if (!CameraController.Instance)
		{
			CameraController.OnInitialize = (Action)Delegate.Combine(CameraController.OnInitialize, (Action)delegate
			{
				CameraController.Instance.enabled = true;
				CameraController.Instance.InitializeCamera(parent);
			});
		}
		else
		{
			CameraController.Instance.enabled = true;
			CameraController.Instance.InitializeCamera(parent);
		}
	}

	public void RelinquishControl()
	{
		LocalControl = false;
		ParentEntity?.ReleaseControl();
		PlayerBrains.Remove(ClientId);
		if (NetworkManager.IsClient)
		{
			NetworkClient.ReturnCharacter();
		}
		InventoryManager.ParentBrain = null;
		InventoryManager.Parent = null;
		ClientId = 0uL;
	}

	public override void InitializeDamageState()
	{
		DamageState = new OrganicDamageState(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new BrainSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is BrainSaveData brainSaveData)
		{
			ClientId = brainSaveData.ClientSteamId;
		}
		if (ClientId != 0)
		{
			PlayerBrains[ClientId] = this;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		BrainSaveData brainSaveData = savedData as BrainSaveData;
		if (GameManager.GameState != GameState.None && brainSaveData != null)
		{
			brainSaveData.ClientSteamId = ClientId;
			brainSaveData.identity = HumanCosmetics;
		}
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (HasAuthority && !ParentEntity)
			{
				CameraController.CurrentCamera.transform.position = Vector3.Lerp(CameraController.CameraOrigin, base.Position, Time.deltaTime * 100f);
				CameraController.CurrentCamera.transform.rotation = Quaternion.Lerp(CameraController.CurrentCamera.transform.rotation, ThingTransform.rotation, Time.deltaTime * 30f);
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteBoolean(ClientControl);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			ClientControl = reader.ReadBoolean();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt64(ClientId);
		writer.WriteString(SteamName);
		writer.WriteBoolean(ClientControl);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ClientId = reader.ReadUInt64();
		SteamName = reader.ReadString();
		ClientControl = reader.ReadBoolean();
		RegisterBrain(ClientId);
	}

	public override void OnLifeTick()
	{
		base.OnLifeTick();
		if (!ParentEntity)
		{
			return;
		}
		bool flag = ParentEntity is Npc;
		float num = ((flag || IsOnline) ? 0.2f : (0.2f * Mathf.Clamp(DifficultySetting.Current.OfflineMetabolism, 0.1f, 1f)));
		if (!flag && ((object)ParentHuman == null || ParentHuman.IsSleeping))
		{
			num *= 0.5f;
		}
		float num2 = ((flag || IsOnline) ? 3f : (3f * Mathf.Clamp(DifficultySetting.Current.OfflineMetabolism, 0.1f, 1f)));
		if ((object)ParentHuman != null && ParentHuman.IsArtificial)
		{
			if ((object)ParentHuman.RobotBattery == null || ParentHuman.RobotBattery.IsEmpty)
			{
				DamageState.Damage(ChangeDamageType.Increment, num2, DamageUpdateType.Stun);
			}
			else if (DamageState.Stun > 0f)
			{
				EntityState state = ParentHuman.State;
				if ((state == EntityState.Alive || state == EntityState.Unconscious) && !ParentHuman.IsSleeping)
				{
					DamageState.Damage(ChangeDamageType.Decrement, num2 * 1f, DamageUpdateType.Stun);
				}
			}
			return;
		}
		bool flag2 = (object)InventoryManager.ParentHuman != null && (object)ParentHuman != null && InventoryManager.ParentHuman == ParentHuman;
		if (!ParentHuman)
		{
			return;
		}
		ParentHuman.Oxygenation -= 0.0015840002f * ((IsOnline || flag2 || flag) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
		if (ParentHuman.Oxygenation <= 0f)
		{
			DamageState.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Oxygen);
			DamageState.Damage(ChangeDamageType.Increment, num2, DamageUpdateType.Stun);
			return;
		}
		float oxygen = DamageState.Oxygen;
		if (oxygen > 0f && oxygen < 50f)
		{
			DamageState.Damage(ChangeDamageType.Decrement, num, DamageUpdateType.Oxygen);
		}
		bool flag3 = (object)ParentHuman != null && ParentHuman.IsSleeping;
		bool flag4 = (object)ParentHuman != null && ParentHuman.GForce > 4f;
		if (DamageState.Stun > 0f)
		{
			EntityState state = ParentEntity.State;
			if ((state == EntityState.Alive || state == EntityState.Unconscious) && !flag3 && !flag4)
			{
				DamageState.Damage(ChangeDamageType.Decrement, num2, DamageUpdateType.Stun);
			}
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		if ((bool)ParentBodyBag)
		{
			hasControl = false;
		}
		base.OnEnterInventory(parent);
		if ((bool)ParentEntity && !(this != ParentEntity.OrganBrain) && !GameManager.IsBatchMode && ClientId == NetworkManager.LocalClientId && !hasControl)
		{
			if (!GameManager.RunSimulation)
			{
				NetworkClient.TakeControlOfExistingBrain(this);
			}
			else
			{
				TakeControl(setPhysics: false);
			}
		}
	}

	public static void GetValidatedBrain(ulong steamId, out Brain playerBrain)
	{
		PlayerBrains.TryGetValue(steamId, out playerBrain);
		if ((object)playerBrain != null && (object)playerBrain.ParentHuman != null && playerBrain.ParentHuman.State == EntityState.Decay)
		{
			playerBrain.ClientId = 0uL;
			if (GameManager.RunSimulation)
			{
				OnServer.Destroy(playerBrain);
			}
			playerBrain = null;
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		base.name = "Brain_" + DisplayName;
		CustomName = DisplayName + "'s Brain";
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ITrackable.Trackables.Remove(this);
		if (ClientId != 0)
		{
			PlayerBrains.Remove(ClientId);
		}
	}

	public void RegisterBrain(ulong steamId)
	{
		ClientId = steamId;
		PlayerBrains[steamId] = this;
	}
}
