using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace TerrainSystem;

public class TransformGizmo : GameBase
{
	[SerializeField]
	private Transform _root;

	[SerializeField]
	private List<TransformGizmoManipulator> _manipulators;

	[SerializeField]
	private LayerMask _transformGizmoLayerMask;

	private Vector3 _dragStartPositionWorldSpace;

	private Vector3 _dragPositionOffset;

	private TransformGizmoManipulator _currentManipulator;

	private Dictionary<Collider, TransformGizmoManipulator> _manipulatorLookup = new Dictionary<Collider, TransformGizmoManipulator>();

	public Transform Target { get; set; }

	private void Awake()
	{
		foreach (TransformGizmoManipulator manipulator in _manipulators)
		{
			_manipulatorLookup.Add(manipulator.Collider, manipulator);
			manipulator.Initialise();
		}
	}

	public void SetScale(float scale)
	{
		_root.localScale = Vector3.one * scale;
	}

	public void CheckInteraction()
	{
		if (!InputMouse.IsMouseControl)
		{
			return;
		}
		Vector3 mousePosition = Input.mousePosition;
		Ray ray = CameraController.CurrentCamera.ScreenPointToRay(mousePosition);
		if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out var hitInfo, float.PositiveInfinity, _transformGizmoLayerMask))
		{
			_currentManipulator = _manipulatorLookup.GetValueOrDefault(hitInfo.collider);
			if ((object)_currentManipulator != null)
			{
				_dragStartPositionWorldSpace = Transform.position;
				_currentManipulator.StartManipulate(Target, ray, _dragStartPositionWorldSpace);
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			_dragStartPositionWorldSpace = Vector3.zero;
			_currentManipulator = null;
		}
		if ((object)_currentManipulator != null)
		{
			_currentManipulator.Manipulate(Target, ray, _dragStartPositionWorldSpace);
		}
	}
}
