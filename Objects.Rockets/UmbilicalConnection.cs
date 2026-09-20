using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects.Rockets;

public class UmbilicalConnection : MonoBehaviour
{
	public Transform Transform;

	public SmallGrid Parent;

	public Grid3 LocalGrid;

	[HideInInspector]
	public bool IsValid;

	private bool _isInitialized;

	private void Awake()
	{
		Validate();
		SetGrids();
	}

	public void Validate()
	{
		if (Parent == null)
		{
			Parent = GetComponentInParent<SmallGrid>();
		}
		Transform = base.gameObject.transform;
		IsValid = Transform != null && Parent != null;
	}

	public void SetGrids()
	{
		if (!Parent.IsCursor && IsValid)
		{
			_isInitialized = true;
			Vector3 position = Transform.position;
			LocalGrid = Parent.GridController.WorldToLocalGrid(position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		}
	}
}
