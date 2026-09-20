using UnityEngine;

namespace Assets.Scripts.Objects.Electrical.Helper;

public class GrowLightEvent : MonoBehaviour
{
	public GrowLight Parent;

	private void OnTriggerEnter(Collider other)
	{
		Parent.HandleEnterTrigger(other);
	}

	private void OnTriggerExit(Collider other)
	{
		Parent.HandleExitTrigger(other);
	}
}
