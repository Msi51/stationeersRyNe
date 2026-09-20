using Assets.Scripts.Inventory;
using UnityEngine;

namespace Assets.Scripts.Effects;

public class ClampToCamera : MonoBehaviour
{
	[Tooltip("Off set from the main camera")]
	public Vector3 OffSetFromCamera;

	private Transform _transform;

	private Transform _cameraTransform;

	private void Start()
	{
		_transform = GetComponent<Transform>();
		_cameraTransform = Camera.main.transform;
	}

	private void Update()
	{
		if (!(InventoryManager.Parent == null) && !WorldManager.IsGamePaused)
		{
			bool flag = Vector3.Dot(InventoryManager.Parent.ActiveRigidbody.velocity, WorldManager.WindVector) < 0f;
			_transform.position = _cameraTransform.position + OffSetFromCamera + (flag ? InventoryManager.Parent.ActiveRigidbody.velocity : Vector3.zero);
		}
	}
}
