using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class RagdollPart
{
	public string PartName;

	public Transform Transform;

	public GameObject GameObject;

	public Collider Collider;

	public Rigidbody Rigidbody;

	public Entity Parent;

	public Joint Joint;

	public RagdollPartType Type;

	public RagdollPart()
	{
	}

	public RagdollPart(RagdollPart part)
	{
		PartName = part.Type.ToString();
		Transform = part.Transform;
		Collider = Transform.GetComponent<Collider>();
		Rigidbody = Transform.GetComponent<Rigidbody>();
		Joint = Transform.GetComponent<Joint>();
		Parent = Transform.root.GetComponent<Entity>();
		GameObject = Transform.gameObject;
		Type = part.Type;
	}

	public void SetCarried(bool carried, Vector3 direction)
	{
		if ((bool)GameObject)
		{
			GameObject.layer = (carried ? Layers.IgnoreRaycast : Layers.PlayerRagdoll);
		}
		RagdollPartType type = Type;
		if ((type == RagdollPartType.Spine || type == RagdollPartType.Head) && !carried)
		{
			Rigidbody.AddForce(direction * 2f, ForceMode.Impulse);
		}
	}
}
