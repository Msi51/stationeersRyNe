using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Steamworks;
using UnityEngine;

namespace Networking.Servers;

public class PlayerCookie
{
	[XmlRoot(ElementName = "PlayerCookie")]
	public class PlayerCookieSaveDataV1
	{
		public ushort Version { get; set; }

		public ulong ClientId { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }
	}

	[XmlRoot("PlayerCookie")]
	public class PlayerCookieSaveDataV2
	{
		[XmlAttribute]
		public ushort Version;

		[XmlAttribute]
		public ulong ClientId;

		[XmlAttribute]
		public string Username;

		[XmlElement]
		public bool DismissedMajorUpdatePopup;

		[XmlElement]
		public bool DismissedOldSavePopup;

		[XmlElement("World")]
		public List<WorldPrefs> Worlds = new List<WorldPrefs>(1024);
	}

	private const ushort CURRENT_COOKIE_VERSION = 2;

	private const string RECRUIT = "Recruit";

	private readonly Dictionary<string, WorldPrefs> _uniqueWorlds = new Dictionary<string, WorldPrefs>();

	private static string PATH_VERSIONED => string.Format("{0}/{1}-v{2}.xml", Application.persistentDataPath, "PlayerCookie", (ushort)2);

	private static string PATH_NONVERSIONED => Application.persistentDataPath + "/PlayerCookie.xml";

	public ushort Version { get; set; }

	public ulong ClientId { get; set; }

	public string Username { get; set; }

	public bool DismissedMajorUpdatePopup { get; private set; }

	public bool DismissedOldSavePopup { get; private set; }

	public static PlayerCookie Current => NetworkManager.Cookie;

	private static bool HasVersionedCookie()
	{
		return File.Exists(PATH_VERSIONED);
	}

	private PlayerCookie()
	{
	}

	private PlayerCookie(PlayerCookieSaveDataV2 data)
	{
		foreach (WorldPrefs world in data.Worlds)
		{
			_uniqueWorlds.TryAdd(world.Id, world);
		}
		Username = data.Username;
		Version = data.Version;
		ClientId = data.ClientId;
		DismissedMajorUpdatePopup = data.DismissedMajorUpdatePopup;
		DismissedOldSavePopup = data.DismissedOldSavePopup;
	}

	private PlayerCookie(PlayerCookieSaveDataV1 data)
	{
		Username = data.Username;
		Version = data.Version;
		ClientId = data.ClientId;
	}

	public static PlayerCookie Load()
	{
		PlayerCookie playerCookie = null;
		if (HasVersionedCookie())
		{
			PlayerCookieSaveDataV2 playerCookieSaveDataV = XmlSerialization.LoadOrNull<PlayerCookieSaveDataV2>(PATH_VERSIONED);
			if (playerCookieSaveDataV != null)
			{
				playerCookie = new PlayerCookie(playerCookieSaveDataV);
			}
		}
		else
		{
			PlayerCookieSaveDataV1 playerCookieSaveDataV2 = XmlSerialization.LoadOrNull<PlayerCookieSaveDataV1>(PATH_NONVERSIONED);
			if (playerCookieSaveDataV2 != null)
			{
				playerCookie = new PlayerCookie(playerCookieSaveDataV2);
			}
		}
		if (playerCookie?.Username == "Recruit")
		{
			playerCookie.Username = (SteamClient.IsValid ? SteamClient.Name : "Recruit");
			playerCookie.Save();
		}
		if (playerCookie != null && playerCookie.Version == 2 && playerCookie.ClientId != 0L)
		{
			return playerCookie;
		}
		PlayerCookie playerCookie2 = CreateNewCookie();
		playerCookie2.Save();
		return playerCookie2;
	}

	public void Save()
	{
		PlayerCookieSaveDataV2 playerCookieSaveDataV = new PlayerCookieSaveDataV2();
		foreach (KeyValuePair<string, WorldPrefs> uniqueWorld in _uniqueWorlds)
		{
			playerCookieSaveDataV.Worlds.Add(uniqueWorld.Value);
		}
		playerCookieSaveDataV.Username = Username;
		playerCookieSaveDataV.Version = Version;
		playerCookieSaveDataV.ClientId = ClientId;
		playerCookieSaveDataV.DismissedMajorUpdatePopup = DismissedMajorUpdatePopup;
		playerCookieSaveDataV.DismissedOldSavePopup = DismissedOldSavePopup;
		playerCookieSaveDataV.SaveXml(PATH_VERSIONED);
	}

	private static PlayerCookie CreateNewCookie()
	{
		return new PlayerCookie
		{
			Version = 2,
			ClientId = (SteamClient.IsValid ? ((ulong)SteamClient.SteamId) : NetworkManager.GenerateUniqueClientId()),
			Username = (SteamClient.IsValid ? SteamClient.Name : "Recruit")
		};
	}

	public UserInterfaceSaveData GetWorldPrefsInterfaceData()
	{
		if (World.CurrentId == null)
		{
			return null;
		}
		if (!_uniqueWorlds.TryGetValue(World.CurrentId, out var value))
		{
			return null;
		}
		return (UserInterfaceSaveData)(value?.Interface);
	}

	public void SetWorldPrefsInterfaceData(UserInterfaceSaveData data)
	{
		if (!string.IsNullOrWhiteSpace(World.CurrentId))
		{
			if (_uniqueWorlds.TryGetValue(World.CurrentId, out var value))
			{
				value.Interface = (WorldPrefs.UI)data;
				return;
			}
			_uniqueWorlds[World.CurrentId] = new WorldPrefs
			{
				Id = World.CurrentId,
				Interface = (WorldPrefs.UI)data
			};
		}
	}

	public List<int> GetDiscoveredPois()
	{
		if (!_uniqueWorlds.TryGetValue(World.CurrentId, out var value))
		{
			return new List<int>();
		}
		return value.DiscoveredPois;
	}

	public void SetDiscoveredPois(List<int> poiKeyHashes)
	{
		if (!string.IsNullOrWhiteSpace(World.CurrentId) && _uniqueWorlds.TryGetValue(World.CurrentId, out var value))
		{
			value.DiscoveredPois = poiKeyHashes;
		}
	}

	public void DismissMajorUpdatePopup()
	{
		DismissedMajorUpdatePopup = true;
	}

	public void DismissOldSavePopup()
	{
		DismissedOldSavePopup = true;
	}

	public override string ToString()
	{
		return $"ClientId: {ClientId}, Username: {Username}, Version: {Version}";
	}
}
