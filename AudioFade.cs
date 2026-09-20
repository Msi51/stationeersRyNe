using Cysharp.Threading.Tasks;
using UnityEngine;

public static class AudioFade
{
	public static async UniTaskVoid FadeOut(AudioSource audioSource, float fadeTime)
	{
		float startVolume = audioSource.volume;
		while (audioSource.volume > 0f)
		{
			audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
			await UniTask.WaitForEndOfFrame();
		}
		audioSource.Stop();
		audioSource.volume = startVolume;
	}

	public static async UniTaskVoid FadeIn(AudioSource audioSource, float fadeTime, float targetVolume)
	{
		audioSource.Play();
		audioSource.volume = 0f;
		while (audioSource.volume < targetVolume)
		{
			audioSource.volume += Time.deltaTime / fadeTime;
			await UniTask.WaitForEndOfFrame();
		}
	}
}
