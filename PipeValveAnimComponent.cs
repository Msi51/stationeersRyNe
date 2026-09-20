using Assets.Scripts.Util;
using UnityEngine;

public class PipeValveAnimComponent : OnOffAnimationComponent
{
	private static readonly Vector3 _soundPosition = new Vector3(0f, 0.25f, 0f);

	protected override Vector3 SoundPosition => _soundPosition;

	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.PipeValveOnHash : Defines.Sounds.PipeValveOffHash);
	}
}
