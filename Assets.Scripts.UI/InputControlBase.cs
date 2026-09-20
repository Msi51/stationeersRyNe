using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class InputControlBase : InputWindowBase
{
	public delegate void InputAxisEvent(ControllerAxis result);

	public delegate void InputEvent(KeyCode result);

	public TextMeshProUGUI TitleText;

	public TextMeshProUGUI StatusText;

	public TextMeshProUGUI AssignmentText;

	public Animator AssignmentAnimator;

	public static bool IsRecording;

	public virtual void ButtonInputCancel()
	{
		IsRecording = false;
		AssignmentAnimator.SetBool("Input", value: false);
		StopAllCoroutines();
	}

	public virtual void ButtonInputSubmit()
	{
		IsRecording = false;
		AssignmentAnimator.SetBool("Input", value: false);
		StopAllCoroutines();
	}
}
