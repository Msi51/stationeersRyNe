using Assets.Scripts.Util;
using UnityEngine;

namespace DefaultNamespace;

public class ValveLeverAnimationComponent : AssignableBinaryAnimComponent
{
	[SerializeField]
	private Vector3 soundPosition = Vector3.zero;

	protected override Vector3 SoundPosition => soundPosition;

	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.PipeValveOnHash : Defines.Sounds.PipeValveOffHash);
	}
}
