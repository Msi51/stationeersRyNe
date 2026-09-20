using UnityEngine;

namespace Assets.Features.AtmosphericScattering.Code;

[ExecuteInEditMode]
public class AtmosphericScatteringSun : MonoBehaviour
{
	public static AtmosphericScatteringSun Instance;

	public Transform Transform { get; private set; }

	public Light Light { get; private set; }

	public void Register()
	{
		if ((bool)Instance && Instance != this)
		{
			Instance.Deregister();
		}
		Transform = base.transform;
		Light = GetComponent<Light>();
		Instance = this;
	}

	public void Deregister()
	{
		if ((bool)Light)
		{
			Light.RemoveAllCommandBuffers();
		}
		Instance = null;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Deregister();
		}
	}
}
