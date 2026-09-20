using Cysharp.Threading.Tasks;
using UnityEngine;

public class DisableEffectsDelay : MonoBehaviour
{
	public ParticleSystem Target;

	public float DelayTimer;

	private void OnEnable()
	{
		if (DelayTimer > 0f)
		{
			DelayDisable(DelayTimer).Forget();
		}
		else
		{
			DisableOnStop().Forget();
		}
	}

	private async UniTaskVoid DelayDisable(float delay)
	{
		await UniTask.Delay((int)(delay * 1000f));
		Target.gameObject.SetActive(value: false);
	}

	private async UniTaskVoid DisableOnStop()
	{
		while (!Target.isStopped)
		{
			await UniTask.NextFrame();
		}
		Target.gameObject.SetActive(value: false);
	}
}
