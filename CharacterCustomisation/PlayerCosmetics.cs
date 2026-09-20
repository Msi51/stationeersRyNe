using System;
using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace CharacterCustomisation;

[Serializable]
public class PlayerCosmetics : IRocketReaderWriter
{
	[XmlElement("Species")]
	public SpeciesClass SpeciesClassDeprecated;

	[FormerlySerializedAs("Species")]
	public SpeciesClass SpeciesClass = SpeciesClass.Human;

	public Gender Gender;

	public string KitId;

	public KitMetaData MetaData;

	public static event Action<PlayerCosmetics> onCosmeticsChanged;

	public void Copy(PlayerCosmetics toCopy)
	{
		SpeciesClass = toCopy.SpeciesClass;
		Gender = toCopy.Gender;
		KitId = toCopy.KitId;
		MetaData = new KitMetaData();
		if (toCopy.MetaData != null)
		{
			MetaData.Copy(toCopy.MetaData);
		}
	}

	public void Save(int slot)
	{
		SpeciesClassDeprecated = SpeciesClass.None;
		this.SaveXml(GetFilePath(slot));
		PlayerCosmetics.onCosmeticsChanged?.Invoke(this);
	}

	public static PlayerCosmetics Load(int slot)
	{
		string filePath = GetFilePath(slot);
		PlayerCosmetics playerCosmetics = (File.Exists(filePath) ? XmlSerialization.Deserialize<PlayerCosmetics>(filePath) : null);
		if (playerCosmetics != null && playerCosmetics.SpeciesClassDeprecated != SpeciesClass.None)
		{
			playerCosmetics.SpeciesClass = playerCosmetics.SpeciesClassDeprecated;
		}
		return playerCosmetics;
	}

	private static string GetFilePath(int slot)
	{
		return $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\My Games\\Stationeers\\PlayerCosmetics_{slot}.xml";
	}

	public override string ToString()
	{
		return JObject.FromObject(this).ToString();
	}

	public void Read(RocketBinaryReader reader)
	{
		SpeciesClass = (SpeciesClass)reader.ReadByte();
		Gender = (Gender)reader.ReadByte();
		KitId = reader.ReadString();
		if (MetaData == null)
		{
			MetaData = new KitMetaData();
		}
		MetaData.Deserialize(reader);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)SpeciesClass);
		writer.WriteByte((byte)Gender);
		writer.WriteString(KitId ?? string.Empty);
		if (MetaData == null)
		{
			MetaData = KitMetaData.DefaultKit;
		}
		MetaData.Serialize(writer);
	}

	public CharacterKit FindIn(CharacterKit[] characterKits)
	{
		for (int i = 0; i < characterKits.Length; i++)
		{
			CharacterKit characterKit = characterKits[i];
			if ((object)characterKit == null)
			{
				throw new NullReferenceException($"character kit {i} is null");
			}
			if (characterKit.Id == KitId)
			{
				return characterKit;
			}
		}
		return null;
	}
}
