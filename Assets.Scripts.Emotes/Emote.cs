using System.Collections.Generic;
using Assets.Scripts.Inventory;

namespace Assets.Scripts.Emotes;

public static class Emote
{
	private static readonly Dictionary<string, EmoteData> _emotes;

	static Emote()
	{
		_emotes = new Dictionary<string, EmoteData>();
		Init();
	}

	private static void Init()
	{
		_emotes.Add("salute", new EmoteData(1, 2000, EmoteType.Body));
		_emotes.Add("wave", new EmoteData(2, 1000, EmoteType.Body));
		_emotes.Add("sit", new EmoteData(3, 5000, EmoteType.Body));
	}

	public static bool Trigger(string emoteName)
	{
		if (!_emotes.TryGetValue(emoteName.ToLower(), out var value))
		{
			return false;
		}
		InventoryManager.ParentHuman?.EmoteController?.OnEmote(value);
		return true;
	}
}
