using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects;

public class SwitchMode : SwitchOnOff
{
	[SerializeField]
	protected Material offPowered;

	public InteractableType State = InteractableType.Mode;

	protected override void RefreshPositionState(bool skipAnim)
	{
		int num = (IsOn ? 1 : 0);
		int num2 = ((parentThing.GetInteractable(State).State == 1) ? 1 : 0);
		if (num2 != num)
		{
			IsOn = num2 == 1;
			switchTransform.localRotation = Quaternion.Euler(IsOn ? onPosition : offPosition);
			if (SwitchOnOff.IsAudible(parentThing) && !skipAnim)
			{
				parentThing.PlayPooledAudioSound(IsOn ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, Transform.localPosition);
			}
		}
	}

	protected override void RefreshColorState(bool skipAnim)
	{
		SwitchColorState switchColorState = ((parentThing.GetInteractable(State).State != 1) ? ((!parentThing.Powered) ? SwitchColorState.Off : SwitchColorState.OffPowered) : (parentThing.Powered ? SwitchColorState.OnPowered : SwitchColorState.On));
		if (switchColorState != _currentColorState)
		{
			_currentColorState = switchColorState;
			switch (_currentColorState)
			{
			case SwitchColorState.Off:
				switchRenderer.material = off;
				break;
			case SwitchColorState.On:
				switchRenderer.material = on;
				break;
			case SwitchColorState.OnPowered:
				switchRenderer.material = onPowered;
				break;
			case SwitchColorState.OffPowered:
				switchRenderer.material = offPowered;
				break;
			case SwitchColorState.Error:
				break;
			}
		}
	}
}
