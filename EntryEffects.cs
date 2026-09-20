using UnityEngine;

public class EntryEffects : MonoBehaviour
{
	[SerializeField]
	private Material _material;

	[SerializeField]
	private GameObject[] _linkedGameObjects;

	[SerializeField]
	private Light _light;

	[SerializeField]
	private Transform[] _thrusterEffects;

	private static readonly int INTENSITY = Shader.PropertyToID("_Intensity");

	private void ToggleLinkedGameObjects(bool state)
	{
		GameObject[] linkedGameObjects = _linkedGameObjects;
		for (int i = 0; i < linkedGameObjects.Length; i++)
		{
			linkedGameObjects[i].SetActive(state);
		}
	}

	private void SetLightIntensity(float intensity)
	{
		_light.intensity = intensity;
	}

	private void SetMaterialIntensity(float intensity)
	{
		_material.SetFloat(INTENSITY, intensity);
	}

	private void SetThrusterIntensity(float lerpFactor)
	{
		float num = EaseOutQuart(lerpFactor) * 0.7f;
		Transform[] thrusterEffects = _thrusterEffects;
		foreach (Transform obj in thrusterEffects)
		{
			float num2 = Random.Range(0.9f, 1.1f);
			obj.localScale = new Vector3(0.3f, num * num2, 0.6f);
		}
	}

	public void DisableEffects()
	{
		ToggleLinkedGameObjects(state: false);
	}

	public void EnableEffects()
	{
		ToggleLinkedGameObjects(state: true);
	}

	public void SetIntensity(float lerpFactor)
	{
		float num = Mathf.Pow(1f - lerpFactor, 4f);
		SetThrusterIntensity(lerpFactor);
		if (num <= 0f)
		{
			ToggleLinkedGameObjects(state: false);
		}
	}

	private static float EaseOutQuart(float x)
	{
		return 1f - (1f - x) * (1f - x) * (1f - x) * (1f - x);
	}
}
