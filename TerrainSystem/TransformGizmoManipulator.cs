using UnityEngine;

namespace TerrainSystem;

public abstract class TransformGizmoManipulator : GameBase
{
	public MeshRenderer MeshRenderer;

	public Collider Collider;

	public Color Color;

	public Vector3 Axis;

	public void Initialise()
	{
		MeshRenderer.material.SetColor("_Color", Color);
	}

	public abstract void StartManipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace);

	public abstract void Manipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace);
}
