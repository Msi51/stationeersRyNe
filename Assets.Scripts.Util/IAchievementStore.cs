using System.Collections.Generic;

namespace Assets.Scripts.Util;

public interface IAchievementStore
{
	void Achieve(Achievements.Kind kind);

	void Add(Achievements.Stat stat, int value);

	void Add(Achievements.Stat stat, float value);

	void Clear(Achievements.Kind kind);

	void PopulateCache(Dictionary<Achievements.Kind, bool> cache);

	void PopulateCache(Dictionary<Achievements.Stat, bool> cache);
}
