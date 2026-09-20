using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class StandaloneInputModuleOverride : StandaloneInputModule
{
	public Image GameCursor;

	public Canvas GameCanvas;

	private Vector2 _mLastMousePosition;

	private Vector2 _mMousePosition;

	private readonly MouseState m_MouseState = new MouseState();

	public static StandaloneInputModuleOverride Instance;

	protected override void Start()
	{
		base.Start();
		Instance = this;
	}

	public static void UpdatePosition()
	{
		Instance.ActivateModule();
		Instance.Process();
	}

	protected override void ProcessMove(PointerEventData pointerEvent)
	{
		CursorLockMode lockState = Cursor.lockState;
		Cursor.lockState = CursorLockMode.None;
		base.ProcessMove(pointerEvent);
		Cursor.lockState = lockState;
	}

	protected override void ProcessDrag(PointerEventData pointerEvent)
	{
		CursorLockMode lockState = Cursor.lockState;
		Cursor.lockState = CursorLockMode.None;
		base.ProcessDrag(pointerEvent);
		Cursor.lockState = lockState;
	}

	protected override MouseState GetMousePointerEventData(int id)
	{
		CursorLockMode lockState = Cursor.lockState;
		Cursor.lockState = CursorLockMode.None;
		MouseState mousePointerEventData = base.GetMousePointerEventData(id);
		Cursor.lockState = lockState;
		return mousePointerEventData;
	}
}
