using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public class LiquidBallSpawnCollection
{
	private Atmosphere _owner;

	private List<LiquidBallSpawnInfo> _spawnInfo = new List<LiquidBallSpawnInfo>();

	public List<LiquidBallSpawnInfo> SpawnInfo => _spawnInfo;

	public LiquidBallSpawnCollection(Atmosphere atmosphere)
	{
		_owner = atmosphere;
	}

	public void Clear()
	{
		_spawnInfo.Clear();
	}

	public void Add(GasMixture gasMix, Atmosphere neighbour, Atmosphere current)
	{
		Vector3 normalized = (neighbour.WorldPosition - _owner.WorldPosition).normalized;
		Vector3 position = (neighbour.WorldPosition + _owner.WorldPosition) / 2f;
		_spawnInfo.Add(new LiquidBallSpawnInfo
		{
			Origin = current,
			GasMix = gasMix,
			Position = position,
			Direction = normalized
		});
	}
}
