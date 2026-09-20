using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace CharacterCustomisation;

public class KitMetaData : MessageBase<KitMetaData>
{
	public string Body;

	public string Head;

	public string Hair;

	public string Eyes;

	public string FacialHair;

	public string SkinColour;

	public string EyeColour;

	public string[] HairColours;

	public static KitMetaData DefaultKit
	{
		get
		{
			KitMetaData kitMetaData = new KitMetaData();
			kitMetaData.Body = string.Empty;
			kitMetaData.Head = string.Empty;
			kitMetaData.Hair = string.Empty;
			kitMetaData.Eyes = string.Empty;
			kitMetaData.FacialHair = string.Empty;
			kitMetaData.SkinColour = string.Empty;
			kitMetaData.EyeColour = string.Empty;
			kitMetaData.HairColours = new string[2]
			{
				string.Empty,
				string.Empty
			};
			return kitMetaData;
		}
	}

	public void Copy(KitMetaData toCopy)
	{
		Body = toCopy.Body;
		Head = toCopy.Head;
		Hair = toCopy.Hair;
		Eyes = toCopy.Eyes;
		FacialHair = toCopy.FacialHair;
		SkinColour = toCopy.SkinColour;
		EyeColour = toCopy.EyeColour;
		if (toCopy.HairColours != null)
		{
			HairColours = new string[toCopy.HairColours.Length];
			for (int i = 0; i < toCopy.HairColours.Length; i++)
			{
				HairColours[i] = toCopy.HairColours[i];
			}
		}
	}

	public bool IsValid()
	{
		if (GetAllHashes().All((string hash) => hash != null))
		{
			return HairColours != null;
		}
		return false;
	}

	private IEnumerable<string> GetAllHashes()
	{
		yield return Body;
		yield return Head;
		yield return Hair;
		yield return Eyes;
		yield return FacialHair;
		yield return SkinColour;
		yield return EyeColour;
		if (HairColours != null)
		{
			string[] hairColours = HairColours;
			for (int i = 0; i < hairColours.Length; i++)
			{
				yield return hairColours[i];
			}
		}
	}

	public override string ToString()
	{
		return JsonUtility.ToJson(this, prettyPrint: true);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Body = reader.ReadString();
		Head = reader.ReadString();
		Hair = reader.ReadString();
		Eyes = reader.ReadString();
		FacialHair = reader.ReadString();
		SkinColour = reader.ReadString();
		EyeColour = reader.ReadString();
		byte b = reader.ReadByte();
		HairColours = new string[b];
		for (int i = 0; i < b; i++)
		{
			HairColours[i] = reader.ReadString();
		}
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteString(Body);
		writer.WriteString(Head);
		writer.WriteString(Hair);
		writer.WriteString(Eyes);
		writer.WriteString(FacialHair);
		writer.WriteString(SkinColour);
		writer.WriteString(EyeColour);
		writer.WriteByte((byte)HairColours.Length);
		string[] hairColours = HairColours;
		foreach (string value in hairColours)
		{
			writer.WriteString(value);
		}
	}
}
