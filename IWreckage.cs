using UnityEngine;

public interface IWreckage
{
	Wreckage[] WreckagePrefabs { get; }

	WreckageSize WreckageSize { get; }

	int WreckageQuantity { get; }

	string DisplayName { get; }

	int CustomColorIndex { get; }

	bool HasSpawnedWreckage { get; set; }

	Vector3 Position { get; }

	Bounds GetLocalBounds { get; }

	string CustomName { get; }

	void SpawnWreckage();

	int GetPrefabHash();
}
