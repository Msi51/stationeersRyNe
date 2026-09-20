using UnityEngine;

namespace LeTai.Asset.TranslucentImage.Demo;

public class ChangeMaxUpdateRate : MonoBehaviour
{
	private TranslucentImageSource source;

	private void Awake()
	{
		source = GetComponent<TranslucentImageSource>();
	}

	public void SetUpdateRate(float value)
	{
		source.maxUpdateRate = value;
	}

	public float GetUpdateRate()
	{
		return source.maxUpdateRate;
	}

	private void Update()
	{
	}
}
