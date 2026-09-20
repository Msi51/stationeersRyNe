using Objects.RoboticArm;

namespace Objects;

public class SwitchLarreMode : SwitchMode
{
	private RoboticArmDock RoboticArmParent => parentThing as RoboticArmDock;

	protected override void RefreshColorState(bool skipAnim)
	{
		bool flag = RoboticArmParent.Powered && RoboticArmParent.ArmState == ArmState.Down;
		SwitchColorState switchColorState = ((parentThing.Mode != 1) ? ((!flag) ? SwitchColorState.Off : SwitchColorState.OffPowered) : (flag ? SwitchColorState.OnPowered : SwitchColorState.On));
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
