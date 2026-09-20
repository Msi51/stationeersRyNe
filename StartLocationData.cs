using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

public class StartLocationData : DataCollection
{
	[XmlElement("Position")]
	public Vector2Reference Position;

	[XmlElement("SpawnRadius")]
	public FloatReference SpawnRadius;

	public const string DEFAULT_START_LOCATION_ID = "DefaultStartLocation";

	private const float DEFAULT_SPAWN_RADIUS = 30f;

	protected const string DESCRIPTION_ELEMENT = "Description";

	[XmlElement("Description")]
	public LocalizedStringReference Description;

	public float GetSpawnRadius()
	{
		FloatReference spawnRadius = SpawnRadius;
		if (spawnRadius == null)
		{
			return 30f;
		}
		return spawnRadius;
	}

	public virtual Vector3 WorldPosition()
	{
		return new Vector3(Position.x, 0f, Position.y);
	}

	public Vector3 GetSafePositionInRadius()
	{
		float spawnRadius = GetSpawnRadius();
		Vector3 position = WorldPosition();
		Vector3 offset = Random.insideUnitSphere * spawnRadius;
		return SpawnPoint.GetSafePoint(position, offset, avoidStructure: true) + Vector3.up * 6f;
	}

	public override bool IsValid()
	{
		return Position != null;
	}

	public override void Initialize(ModAbout mod)
	{
		if (IsValid())
		{
			DataCollection.Register(this, mod);
		}
	}

	public virtual StartLocationData Get()
	{
		return DataCollection.Get<StartLocationData>(Id);
	}
}
