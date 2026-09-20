using UnityEngine;

namespace Assets.Scripts.Objects.Electrical.Helper;

public class TriggerPlateEvent : MonoBehaviour
{
	public TriggerPlate Parent;

	private void OnTriggerEnter(Collider other)
	{
		Parent.HandleEnterTrigger(other);
	}

	private void OnTriggerStay(Collider other)
	{
		Parent.HandleEnterTrigger(other);
	}

	private void OnTriggerExit(Collider other)
	{
		Parent.HandleExitTrigger(other);
	}
}
