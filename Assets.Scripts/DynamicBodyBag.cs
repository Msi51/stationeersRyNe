using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using CharacterCustomisation;
using TerrainSystem.Lods;
using UnityEngine;

namespace Assets.Scripts;

public class DynamicBodyBag : DraggableThing
{
	public static readonly List<DynamicBodyBag> AllBodyBags = new List<DynamicBodyBag>();

	private string _playersDisplayName;

	public EntityDamageState BodyDamageState;

	public PlayerCosmetics BodyCosmeticData;

	private static float _stunUpdateSpeed = 5f;

	public override bool ShouldRender => BrainSlot.Get<Brain>()?.LocalControl ?? false;

	public override LodInfo LodInfo
	{
		get
		{
			if (!ShouldRender)
			{
				return LodManager.ThingLodInfo;
			}
			return LodManager.PlayerLodInfo;
		}
	}

	public Slot BrainSlot => Slots[0];

	public Slot LungsSlot => Slots[1];

	public Slot StomachSlot => Slots[2];

	private Brain Brain => BrainSlot?.Get<Brain>();

	public bool PlayerIsOnline
	{
		get
		{
			if ((bool)Brain)
			{
				return Brain.IsOnline;
			}
			return false;
		}
	}

	public bool PlayerHasRespawned
	{
		get
		{
			if ((bool)Brain)
			{
				return Brain.ClientId == 0;
			}
			return true;
		}
	}

	public string PlayersDisplayName
	{
		get
		{
			return _playersDisplayName;
		}
		set
		{
			if (!(_playersDisplayName == value))
			{
				_playersDisplayName = value;
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (BodyDamageState == null)
		{
			BodyDamageState = new EntityDamageState(this);
		}
		if (BodyCosmeticData == null)
		{
			BodyCosmeticData = new PlayerCosmetics();
		}
		if (!AllBodyBags.Contains(this))
		{
			AllBodyBags.Add(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AllBodyBags.Remove(this);
	}

	public void OnCameraUpdate(CameraController cameraController)
	{
		float b = 0.5f;
		float b2 = 1f;
		float b3 = 0f;
		float b4 = 0f;
		cameraController.CameraVignette.intensity = Mathf.Lerp(cameraController.CameraVignette.intensity, b, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraVignette.blur = Mathf.Lerp(cameraController.CameraVignette.blur, b2, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraColorControl.Saturation = Mathf.Lerp(cameraController.CameraColorControl.Saturation, b3, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraColorControl.Brightness = Mathf.Lerp(cameraController.CameraColorControl.Brightness, b4, Time.deltaTime * _stunUpdateSpeed);
	}

	public override ThingSaveData SerializeSave()
	{
		base.SerializeSave();
		ThingSaveData savedData = new ThingDynamicBodyBagSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (GameManager.GameState != GameState.None && savedData is ThingDynamicBodyBagSaveData thingDynamicBodyBagSaveData)
		{
			thingDynamicBodyBagSaveData.BodyDamageState = new DamageUpdate(BodyDamageState);
			thingDynamicBodyBagSaveData.BodyCosmeticData = new PlayerCosmetics();
			if (BodyCosmeticData != null)
			{
				thingDynamicBodyBagSaveData.BodyCosmeticData.Copy(BodyCosmeticData);
			}
			thingDynamicBodyBagSaveData.PlayerDisplayName = PlayersDisplayName;
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		string arg = ((!string.IsNullOrEmpty(PlayersDisplayName)) ? PlayersDisplayName : GameStrings.UnidentifiedPlayer.DisplayString);
		if (PlayerHasRespawned)
		{
			extendedText.AppendLine(GameStrings.PlayerHasAlreadyRespawned.AsString(arg));
			return extendedText;
		}
		extendedText.AppendLine(GameStrings.PlayerBodyBagDead.AsString(arg));
		extendedText.AppendLine(GameStrings.PlayerReviveInCryotube.AsString(arg));
		if (PlayerIsOnline)
		{
			extendedText.AppendLine(GameStrings.PlayerIsOnline.AsString(arg));
		}
		else
		{
			extendedText.AppendLine(GameStrings.PlayerIsOffline.AsString(arg));
		}
		return extendedText;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is ThingDynamicBodyBagSaveData thingDynamicBodyBagSaveData)
		{
			if (BodyDamageState == null)
			{
				BodyDamageState = new EntityDamageState(this);
			}
			if (BodyCosmeticData == null)
			{
				BodyCosmeticData = new PlayerCosmetics();
			}
			BodyDamageState.Copy(thingDynamicBodyBagSaveData.BodyDamageState);
			if (thingDynamicBodyBagSaveData.BodyCosmeticData != null)
			{
				BodyCosmeticData = new PlayerCosmetics();
				BodyCosmeticData.Copy(thingDynamicBodyBagSaveData.BodyCosmeticData);
			}
			_playersDisplayName = thingDynamicBodyBagSaveData.PlayerDisplayName;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteString(PlayersDisplayName ?? string.Empty);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			PlayersDisplayName = reader.ReadString();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteString(PlayersDisplayName ?? string.Empty);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_playersDisplayName = reader.ReadString();
	}
}
