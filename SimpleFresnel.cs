using UnityEngine;

public class SimpleFresnel : MonoBehaviour
{
	public float fresnelFactor = 1f;

	public bool realTimeUpdate = true;

	private void Start()
	{
		setUniforms();
	}

	private void Update()
	{
		if (realTimeUpdate)
		{
			setUniforms();
		}
	}

	private void setUniforms()
	{
		base.transform.GetComponent<Renderer>().material.SetFloat("fresnelFactor", fresnelFactor);
	}
}
