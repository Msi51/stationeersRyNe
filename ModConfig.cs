using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

public class ModConfig
{
	[XmlElement("Core", typeof(CoreModData))]
	[XmlElement("Local", typeof(LocalModData))]
	[XmlElement("Workshop", typeof(WorkshopModData))]
	public List<ModData> Mods = new List<ModData>();

	public void CreateCoreMod()
	{
		bool flag = false;
		foreach (ModData mod in Mods)
		{
			if (mod is CoreModData)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Mods.Insert(0, new CoreModData());
		}
	}

	public IEnumerable<ModData> GetEnabledMods()
	{
		return Mods.Where((ModData x) => x.Enabled);
	}

	public void MoveModUp(ModData mod)
	{
		int num = Mods.IndexOf(mod);
		if (num != 0)
		{
			ModData value = Mods[num - 1];
			Mods[num - 1] = mod;
			Mods[num] = value;
		}
	}

	public void MoveModDown(ModData mod)
	{
		int num = Mods.IndexOf(mod);
		if (num != Mods.Count - 1)
		{
			ModData value = Mods[num + 1];
			Mods[num + 1] = mod;
			Mods[num] = value;
		}
	}

	public void MoveToBottom(ModData mod)
	{
		Mods.Remove(mod);
		Mods.Add(mod);
	}

	public void Cleanup()
	{
		bool flag = false;
		for (int num = Mods.Count - 1; num >= 0; num--)
		{
			ModData modData = Mods[num];
			if (!modData.GetAboutData().IsValid)
			{
				Mods.RemoveAt(num);
				continue;
			}
			if (modData is CoreModData)
			{
				if (flag)
				{
					Mods.RemoveAt(num);
					continue;
				}
				flag = true;
			}
			if (modData is LocalModData localModData && !Directory.Exists(localModData.DirectoryPath))
			{
				Mods.RemoveAt(num);
			}
		}
	}
}
