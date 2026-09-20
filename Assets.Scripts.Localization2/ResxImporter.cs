using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;

namespace Assets.Scripts.Localization2;

public class ResxImporter
{
	public static Dictionary<string, string> LoadDictionaryFromFile(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		using FileStream stream = new FileStream(path, FileMode.Open);
		return new Dictionary<string, string>(EnumerateStream(stream));
	}

	private static IEnumerable<KeyValuePair<string, string>> EnumerateStream(Stream stream)
	{
		using ResXResourceReader reader = new ResXResourceReader(stream);
		foreach (DictionaryEntry item in reader)
		{
			yield return new KeyValuePair<string, string>(item.Key.ToString(), item.Value.ToString());
		}
	}
}
