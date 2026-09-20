using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Events;

public class MachineInputTrigger : MonoBehaviour
{
	public Device ParentMachine;

	private void OnTriggerEnter(Collider otherCollider)
	{
		DynamicThing componentInParent = otherCollider.GetComponentInParent<DynamicThing>();
		if ((bool)componentInParent && !componentInParent.IsEntity && !componentInParent.IsChild)
		{
			componentInParent.CurrentMachineInputTriggers.Add(this);
			ParentMachine.OnInputTriggerEnter(componentInParent, this);
		}
	}

	private void OnTriggerExit(Collider otherCollider)
	{
		DynamicThing componentInParent = otherCollider.GetComponentInParent<DynamicThing>();
		if ((bool)componentInParent)
		{
			componentInParent.CurrentMachineInputTriggers.Remove(this);
			ParentMachine.OnInputTriggerExit(componentInParent, this);
		}
	}
}
