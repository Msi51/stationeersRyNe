using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CharacterCustomisation;

public class CharacterRotator : MonoBehaviour
{
	[SerializeField]
	[ReadOnly]
	private Vector2 _clickPos;

	[SerializeField]
	[ReadOnly]
	private Vector3 _clickRot;

	[SerializeField]
	[ReadOnly]
	private bool _isOverUi;

	private void Update()
	{
		_isOverUi = (bool)EventSystem.current && EventSystem.current.IsPointerOverGameObject();
		if (!_isOverUi)
		{
			if (Input.GetMouseButtonDown(0) || KeyManager.GetMouseDown("Primary"))
			{
				_clickPos = Input.mousePosition;
				_clickRot = base.transform.rotation.eulerAngles;
			}
			if (Input.GetMouseButton(0) || KeyManager.GetMouse("Primary"))
			{
				Quaternion rotation = base.transform.rotation;
				rotation.eulerAngles = new Vector3(0f, _clickPos.x - Input.mousePosition.x, 0f) + _clickRot;
				base.transform.rotation = rotation;
			}
		}
	}
}
