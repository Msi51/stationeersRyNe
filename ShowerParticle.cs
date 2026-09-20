using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShowerParticle : MonoBehaviour
{
	public ParticleSystem showerParticle;

	private void EmitShowerParticles(Vector3 position, int amount)
	{
		base.transform.position = position;
		showerParticle.Emit(amount);
	}

	public async UniTaskVoid EmitShowerParticles(Vector3 position, int amount, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			EmitShowerParticles(position, amount);
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		}
	}
}
