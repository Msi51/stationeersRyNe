using Assets.Scripts;
using Assets.Scripts.Networking;
using Cysharp.Threading.Tasks;
using UnityEngine;

public readonly struct ExplosionEvent(Vector3 position, float radius) : ISyncListable
{
	public readonly Vector3 Position = position;

	public readonly float Radius = radius;

	public static SyncList<ExplosionEvent> NewEvents = new SyncList<ExplosionEvent>(DeserializeEvent);

	public const float DISTANCE_SQUARED = 400f;

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteVector3(Position);
		writer.WriteSingle(Radius);
	}

	private static void DeserializeEvent(RocketBinaryReader reader)
	{
		Vector3 position = reader.ReadVector3();
		float radius = reader.ReadSingle();
		new ExplosionEvent(position, radius).Queue().Forget();
	}

	private async UniTaskVoid Queue()
	{
		await UniTask.NextFrame();
		EffectManager.CreateExplosionEffect(Position, Radius);
	}
}
