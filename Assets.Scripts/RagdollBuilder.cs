using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts;

[Serializable]
internal class RagdollBuilder
{
	private class BoneInfo
	{
		public string Name;

		public Transform Anchor;

		public CharacterJoint Joint;

		public BoneInfo Parent;

		public float MinLimit;

		public float MaxLimit;

		public float SwingLimit;

		public float SwingLimit2;

		public Vector3 Axis;

		public Vector3 NormalAxis;

		public float RadiusScale;

		public Type ColliderType;

		public readonly ArrayList Children = new ArrayList();

		public float Density;

		public float SummedMass;
	}

	public Transform Root;

	public float TotalMass = 20f;

	public float Strength;

	private Vector3 _right = Vector3.right;

	private Vector3 _up = Vector3.up;

	private Vector3 _forward = Vector3.forward;

	private List<Transform> TList = new List<Transform>();

	private Vector3 _worldRight = Vector3.right;

	private Vector3 _worldUp = Vector3.up;

	private Vector3 _worldForward = Vector3.forward;

	public bool FlipForward;

	private ArrayList _bones;

	private BoneInfo _rootBone;

	public List<RagdollPart> RagdollParts = new List<RagdollPart>();

	private static float _scaleBreast = 0.9f;

	private static float _scaleFoot = 0.4f;

	public Transform LeftHips => GetPartTransform(RagdollPartType.LeftHips);

	public Transform LeftKnee => GetPartTransform(RagdollPartType.LeftKnee);

	public Transform LeftFoot => GetPartTransform(RagdollPartType.LeftFoot);

	public Transform RightHips => GetPartTransform(RagdollPartType.RightHips);

	public Transform RightKnee => GetPartTransform(RagdollPartType.RightKnee);

	public Transform RightFoot => GetPartTransform(RagdollPartType.RightFoot);

	public Transform LeftArm => GetPartTransform(RagdollPartType.LeftArm);

	public Transform LeftElbow => GetPartTransform(RagdollPartType.LeftElbow);

	public Transform RightArm => GetPartTransform(RagdollPartType.RightArm);

	public Transform RightElbow => GetPartTransform(RagdollPartType.RightElbow);

	public Transform MiddleSpine => GetPartTransform(RagdollPartType.Pelvis);

	public Transform Head => GetPartTransform(RagdollPartType.Head);

	public Transform LeftHand => GetPartTransform(RagdollPartType.LeftHand);

	public Transform RightHand => GetPartTransform(RagdollPartType.RightHand);

	private void DecomposeVector(out Vector3 normalCompo, out Vector3 tangentCompo, Vector3 outwardDir, Vector3 outwardNormal)
	{
		outwardNormal = outwardNormal.normalized;
		normalCompo = outwardNormal * Vector3.Dot(outwardDir, outwardNormal);
		tangentCompo = outwardDir - normalCompo;
	}

	private void CalculateAxes()
	{
		_up = CalculateDirectionAxis(Root.InverseTransformPoint(Head.position));
		DecomposeVector(out var _, out var tangentCompo, Root.InverseTransformPoint(RightElbow.position), _up);
		_right = CalculateDirectionAxis(tangentCompo);
		_forward = Vector3.Cross(_right, _up);
		if (FlipForward)
		{
			_forward = -_forward;
		}
	}

	public void PrepareBones()
	{
		_worldRight = Root.TransformDirection(_right);
		_worldUp = Root.TransformDirection(_up);
		_worldForward = Root.TransformDirection(_forward);
		_bones = new ArrayList();
		_rootBone = new BoneInfo
		{
			Name = "Pelvis",
			Anchor = MiddleSpine,
			Parent = null,
			Density = 2.5f
		};
		_bones.Add(_rootBone);
		AddMirroredJoint("Hips", LeftHips, RightHips, "Pelvis", Vector3.forward, Vector3.up, -10f, 10f, 10f, 14f, typeof(CapsuleCollider), 0.3f, 1.5f);
		AddMirroredJoint("Knee", LeftKnee, RightKnee, "Hips", Vector3.right, Vector3.up, -5f, 5f, 0f, 14f, typeof(CapsuleCollider), 0.25f, 1.5f);
		AddMirroredJoint("Foot", LeftFoot, RightFoot, "Knee", Vector3.right, Vector3.up, -5f, 5f, 16f, 11f, typeof(BoxCollider), 0.3f, 1.5f);
		AddMirroredJoint("Arm", LeftArm, RightArm, "Pelvis", Vector3.right, Vector3.forward, -20f, 25f, 30f, 15f, typeof(CapsuleCollider), 0.25f, 1f);
		AddMirroredJoint("Elbow", LeftElbow, RightElbow, "Arm", Vector3.right, Vector3.forward, -30f, 20f, 15f, 11f, typeof(CapsuleCollider), 0.2f, 1f);
		AddMirroredJoint("Hand", LeftHand, RightHand, "Elbow", Vector3.right, Vector3.forward, -20f, 20f, 30f, 25f, typeof(CapsuleCollider), 0.2f, 1f);
		AddJoint("Head", Head, "Pelvis", Vector3.up, Vector3.right, 0f, 0f, 35f, 25f, null, 5f, 1f);
	}

	public List<Transform> GetAllTransformsRecursive(Transform t)
	{
		if ((bool)t)
		{
			TList.Add(t);
			foreach (Transform item in t)
			{
				if (!(item == null))
				{
					GetAllTransformsRecursive(item);
				}
			}
		}
		return TList;
	}

	private Transform GetPartTransform(RagdollPartType part)
	{
		int num = RagdollParts.FindIndex((RagdollPart p) => p.Type == part);
		if (num >= 0)
		{
			return RagdollParts[num].Transform;
		}
		return null;
	}

	public void OnWizardCreate(Entity parent, float mass)
	{
		Root = (parent.transform.Find("root") ? parent.transform.Find("root") : parent.transform.Find("Root"));
		foreach (Transform item in GetAllTransformsRecursive(Root))
		{
			RagdollPart ragdollPart = new RagdollPart
			{
				Transform = item
			};
			if (item.gameObject.name.ToLower().Contains("thigh_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftHips;
			}
			if (item.gameObject.name.ToLower().Contains("calf_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftKnee;
			}
			if (item.gameObject.name.ToLower().Contains("foot_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftFoot;
			}
			if (item.gameObject.name.ToLower().Contains("thigh_r"))
			{
				ragdollPart.Type = RagdollPartType.RightHips;
			}
			if (item.gameObject.name.ToLower().Contains("calf_r"))
			{
				ragdollPart.Type = RagdollPartType.RightKnee;
			}
			if (item.gameObject.name.ToLower().Contains("foot_r"))
			{
				ragdollPart.Type = RagdollPartType.RightFoot;
			}
			if (item.gameObject.name.ToLower().Contains("upperarm_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftArm;
			}
			if (item.gameObject.name.ToLower().Contains("lowerarm_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftElbow;
			}
			if (item.gameObject.name.ToLower().Contains("upperarm_r"))
			{
				ragdollPart.Type = RagdollPartType.RightArm;
			}
			if (item.gameObject.name.ToLower().Contains("lowerarm_r"))
			{
				ragdollPart.Type = RagdollPartType.RightElbow;
			}
			if (item.gameObject.name.ToLower().Contains("pelvis"))
			{
				ragdollPart.Type = RagdollPartType.Pelvis;
			}
			if (item.gameObject.name.ToLower().Contains("head"))
			{
				ragdollPart.Type = RagdollPartType.Head;
			}
			if (item.gameObject.name.ToLower().Contains("hand_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftHand;
			}
			if (item.gameObject.name.ToLower().Contains("hand_r"))
			{
				ragdollPart.Type = RagdollPartType.RightHand;
			}
			if (item.gameObject.name.ToLower().Contains("spine_01"))
			{
				ragdollPart.Type = RagdollPartType.Spine;
			}
			if (ragdollPart.Type != RagdollPartType.None)
			{
				RagdollParts.Add(ragdollPart);
			}
		}
		TotalMass = mass;
		PrepareBones();
		BuildCapsules();
		AddBreastColliders();
		AddHeadCollider();
		AddFootColliders();
		BuildBodies();
		BuildJoints();
		CalculateMass();
		CalculateSpringDampers();
		foreach (RagdollPart ragdollPart3 in RagdollParts)
		{
			RagdollPart ragdollPart2 = new RagdollPart(ragdollPart3);
			if (ragdollPart2.Rigidbody != null)
			{
				ragdollPart2.Rigidbody.angularDrag = 0.1f;
				ragdollPart2.Rigidbody.drag = 0.2f;
			}
			parent.RagdollParts.Add(ragdollPart2);
		}
	}

	public void OnWizardCreate(DynamicSkeleton parent, float mass)
	{
		Root = (parent.transform.Find("root") ? parent.transform.Find("root") : parent.transform.Find("Root"));
		foreach (Transform item in GetAllTransformsRecursive(Root))
		{
			RagdollPart ragdollPart = new RagdollPart
			{
				Transform = item
			};
			if (item.gameObject.name.ToLower().Contains("thigh_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftHips;
			}
			if (item.gameObject.name.ToLower().Contains("calf_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftKnee;
			}
			if (item.gameObject.name.ToLower().Contains("foot_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftFoot;
			}
			if (item.gameObject.name.ToLower().Contains("thigh_r"))
			{
				ragdollPart.Type = RagdollPartType.RightHips;
			}
			if (item.gameObject.name.ToLower().Contains("calf_r"))
			{
				ragdollPart.Type = RagdollPartType.RightKnee;
			}
			if (item.gameObject.name.ToLower().Contains("foot_r"))
			{
				ragdollPart.Type = RagdollPartType.RightFoot;
			}
			if (item.gameObject.name.ToLower().Contains("upperarm_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftArm;
			}
			if (item.gameObject.name.ToLower().Contains("lowerarm_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftElbow;
			}
			if (item.gameObject.name.ToLower().Contains("upperarm_r"))
			{
				ragdollPart.Type = RagdollPartType.RightArm;
			}
			if (item.gameObject.name.ToLower().Contains("lowerarm_r"))
			{
				ragdollPart.Type = RagdollPartType.RightElbow;
			}
			if (item.gameObject.name.ToLower().Contains("pelvis"))
			{
				ragdollPart.Type = RagdollPartType.Pelvis;
			}
			if (item.gameObject.name.ToLower().Contains("head"))
			{
				ragdollPart.Type = RagdollPartType.Head;
			}
			if (item.gameObject.name.ToLower().Contains("hand_l"))
			{
				ragdollPart.Type = RagdollPartType.LeftHand;
			}
			if (item.gameObject.name.ToLower().Contains("hand_r"))
			{
				ragdollPart.Type = RagdollPartType.RightHand;
			}
			if (item.gameObject.name.ToLower().Contains("spine_01"))
			{
				ragdollPart.Type = RagdollPartType.Spine;
			}
			if (ragdollPart.Type != RagdollPartType.None)
			{
				RagdollParts.Add(ragdollPart);
			}
		}
		TotalMass = mass;
		PrepareBones();
		BuildCapsules();
		AddBreastColliders();
		AddHeadCollider();
		AddFootColliders();
		BuildBodies();
		BuildJoints();
		CalculateMass();
		CalculateSpringDampers();
		foreach (RagdollPart ragdollPart3 in RagdollParts)
		{
			RagdollPart ragdollPart2 = new RagdollPart(ragdollPart3);
			if (ragdollPart2.Rigidbody != null)
			{
				ragdollPart2.Rigidbody.angularDrag = 0.1f;
				ragdollPart2.Rigidbody.drag = 0.2f;
			}
			parent.RagdollParts.Add(ragdollPart2);
		}
	}

	private BoneInfo FindBone(string name)
	{
		foreach (BoneInfo bone in _bones)
		{
			if (bone.Name == name)
			{
				return bone;
			}
		}
		return null;
	}

	private void AddMirroredJoint(string name, Transform leftAnchor, Transform rightAnchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swingLimit, float swingLimit2, Type colliderType, float radiusScale, float density)
	{
		AddJoint("Left " + name, leftAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swingLimit, swingLimit2, colliderType, radiusScale, density);
		AddJoint("Right " + name, rightAnchor, parent, -worldTwistAxis, -worldSwingAxis, minLimit, maxLimit, swingLimit, swingLimit2, colliderType, radiusScale, density);
	}

	private void AddJoint(string name, Transform anchor, string parent, Vector3 swingAxis, Vector3 axis, float minLimit, float maxLimit, float swingLimit, float swing2Limit, Type colliderType, float radiusScale, float density)
	{
		BoneInfo boneInfo = new BoneInfo();
		boneInfo.Name = name;
		boneInfo.Anchor = anchor;
		boneInfo.Axis = swingAxis;
		boneInfo.NormalAxis = axis;
		boneInfo.MinLimit = minLimit;
		boneInfo.MaxLimit = maxLimit;
		boneInfo.SwingLimit = swingLimit;
		boneInfo.SwingLimit2 = swing2Limit;
		boneInfo.Density = density;
		boneInfo.ColliderType = colliderType;
		boneInfo.RadiusScale = radiusScale;
		if (FindBone(parent) != null)
		{
			boneInfo.Parent = FindBone(parent);
		}
		else if (name.StartsWith("Left"))
		{
			boneInfo.Parent = FindBone("Left " + parent);
		}
		else if (name.StartsWith("Right"))
		{
			boneInfo.Parent = FindBone("Right " + parent);
		}
		boneInfo.Parent.Children.Add(boneInfo);
		_bones.Add(boneInfo);
	}

	private void BuildCapsules()
	{
		foreach (BoneInfo bone in _bones)
		{
			if (bone.ColliderType != typeof(CapsuleCollider))
			{
				continue;
			}
			int direction;
			float distance;
			if (bone.Children.Count == 1)
			{
				Vector3 position = ((BoneInfo)bone.Children[0]).Anchor.position;
				CalculateDirection(bone.Anchor.InverseTransformPoint(position), out direction, out distance);
			}
			else
			{
				Vector3 position2 = bone.Anchor.position - bone.Parent.Anchor.position + bone.Anchor.position;
				CalculateDirection(bone.Anchor.InverseTransformPoint(position2), out direction, out distance);
				if (bone.Anchor.GetComponentsInChildren(typeof(Transform)).Length > 1)
				{
					Bounds bounds = default(Bounds);
					Component[] componentsInChildren = bone.Anchor.GetComponentsInChildren(typeof(Transform));
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						Transform transform = (Transform)componentsInChildren[i];
						bounds.Encapsulate(bone.Anchor.InverseTransformPoint(transform.position));
					}
					distance = ((!(distance > 0f)) ? bounds.min[direction] : bounds.max[direction]);
				}
				else
				{
					Debug.Log(bone.Name);
				}
			}
			CapsuleCollider capsuleCollider = bone.Anchor.gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.direction = direction;
			Vector3 zero = Vector3.zero;
			zero[direction] = distance * 0.5f;
			capsuleCollider.center = zero;
			capsuleCollider.height = Mathf.Abs(distance);
			capsuleCollider.radius = Mathf.Abs(distance * bone.RadiusScale);
		}
	}

	private void BuildBodies()
	{
		foreach (BoneInfo bone in _bones)
		{
			bone.Anchor.gameObject.AddComponent<Rigidbody>();
			bone.Anchor.GetComponent<Rigidbody>().mass = bone.Density;
		}
	}

	private void BuildJoints()
	{
		foreach (BoneInfo bone in _bones)
		{
			if (bone.Parent != null && !(bone.Anchor.gameObject.GetComponent<CharacterJoint>() != null))
			{
				CharacterJoint characterJoint = (bone.Joint = bone.Anchor.gameObject.AddComponent<CharacterJoint>());
				characterJoint.axis = bone.NormalAxis;
				characterJoint.swingAxis = bone.Axis;
				characterJoint.anchor = Vector3.zero;
				characterJoint.connectedBody = bone.Parent.Anchor.GetComponent<Rigidbody>();
				characterJoint.enableCollision = true;
				SoftJointLimitSpring softJointLimitSpring = new SoftJointLimitSpring
				{
					spring = 50f,
					damper = 100f
				};
				characterJoint.swingLimitSpring = softJointLimitSpring;
				characterJoint.twistLimitSpring = softJointLimitSpring;
				SoftJointLimit softJointLimit = new SoftJointLimit
				{
					limit = bone.MinLimit
				};
				characterJoint.lowTwistLimit = softJointLimit;
				softJointLimit.limit = bone.MaxLimit;
				characterJoint.highTwistLimit = softJointLimit;
				softJointLimit.limit = bone.SwingLimit;
				characterJoint.swing1Limit = softJointLimit;
				softJointLimit.limit = bone.SwingLimit2;
				characterJoint.swing2Limit = softJointLimit;
			}
		}
	}

	private void CalculateMassRecurse(BoneInfo bone)
	{
		float num = bone.Anchor.GetComponent<Rigidbody>().mass;
		foreach (BoneInfo child in bone.Children)
		{
			CalculateMassRecurse(child);
			num += child.SummedMass;
		}
		bone.SummedMass = num;
	}

	private void CalculateMass()
	{
		CalculateMassRecurse(_rootBone);
		float num = TotalMass / _rootBone.SummedMass;
		foreach (BoneInfo bone in _bones)
		{
			bone.Anchor.GetComponent<Rigidbody>().mass *= num;
		}
		CalculateMassRecurse(_rootBone);
	}

	private JointDrive CalculateSpringDamper(float frequency, float damping, float mass)
	{
		return new JointDrive
		{
			positionSpring = 9f * frequency * frequency * mass,
			positionDamper = 4.5f * frequency * damping * mass
		};
	}

	private void CalculateSpringDampers()
	{
		foreach (BoneInfo bone in _bones)
		{
			_ = bone;
		}
	}

	private void CalculateDirection(Vector3 point, out int direction, out float distance)
	{
		direction = 0;
		if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
		{
			direction = 1;
		}
		if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
		{
			direction = 2;
		}
		distance = point[direction];
	}

	private Vector3 CalculateDirectionAxis(Vector3 point)
	{
		int direction = 0;
		CalculateDirection(point, out direction, out var distance);
		Vector3 zero = Vector3.zero;
		if (distance > 0f)
		{
			zero[direction] = 1f;
		}
		else
		{
			zero[direction] = -1f;
		}
		return zero;
	}

	private int SmallestComponent(Vector3 point)
	{
		int num = 0;
		if (Mathf.Abs(point[1]) < Mathf.Abs(point[0]))
		{
			num = 1;
		}
		if (Mathf.Abs(point[2]) < Mathf.Abs(point[num]))
		{
			num = 2;
		}
		return num;
	}

	private int LargestComponent(Vector3 point)
	{
		int num = 0;
		if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
		{
			num = 1;
		}
		if (Mathf.Abs(point[2]) > Mathf.Abs(point[num]))
		{
			num = 2;
		}
		return num;
	}

	private int SecondLargestComponent(Vector3 point)
	{
		int num = SmallestComponent(point);
		int num2 = LargestComponent(point);
		if (num < num2)
		{
			int num3 = num2;
			num2 = num;
			num = num3;
		}
		if (num == 0 && num2 == 1)
		{
			return 2;
		}
		if (num == 0 && num2 == 2)
		{
			return 1;
		}
		return 0;
	}

	private Bounds Clip(Bounds bounds, Transform relativeTo, Transform clipTransform, bool below)
	{
		int index = LargestComponent(bounds.size);
		if (Vector3.Dot(_worldUp, relativeTo.TransformPoint(bounds.max)) > Vector3.Dot(_worldUp, relativeTo.TransformPoint(bounds.min)) == below)
		{
			Vector3 min = bounds.min;
			min[index] = relativeTo.InverseTransformPoint(clipTransform.position)[index];
			bounds.min = min;
		}
		else
		{
			Vector3 max = bounds.max;
			max[index] = relativeTo.InverseTransformPoint(clipTransform.position)[index];
			bounds.max = max;
		}
		return bounds;
	}

	private Bounds GetBreastBounds(Transform relativeTo)
	{
		Bounds result = default(Bounds);
		result.Encapsulate(relativeTo.InverseTransformPoint(LeftHips.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(RightHips.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(LeftArm.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(RightArm.position));
		Vector3 size = result.size;
		size[SmallestComponent(result.size)] = size[LargestComponent(result.size)] / 2f;
		result.size = size;
		return result;
	}

	private void AddBreastColliders()
	{
		Bounds bounds = default(Bounds);
		bounds.Encapsulate(MiddleSpine.InverseTransformPoint(Head.position));
		bounds.Encapsulate(MiddleSpine.InverseTransformPoint(LeftHips.position));
		bounds.Encapsulate(MiddleSpine.InverseTransformPoint(RightHips.position));
		bounds.Encapsulate(MiddleSpine.InverseTransformPoint(LeftArm.position));
		bounds.Encapsulate(MiddleSpine.InverseTransformPoint(RightArm.position));
		bounds = new Bounds(bounds.center * _scaleBreast, bounds.size * _scaleBreast);
		Vector3 size = bounds.size;
		size[SmallestComponent(bounds.size)] = size[LargestComponent(bounds.size)] / 2f;
		BoxCollider boxCollider = MiddleSpine.gameObject.AddComponent<BoxCollider>();
		boxCollider.center = bounds.center;
		boxCollider.size = size;
	}

	private void AddHeadCollider()
	{
		float num = Vector3.Distance(LeftArm.transform.position, RightArm.transform.position);
		num /= 2f;
		SphereCollider sphereCollider = Head.gameObject.AddComponent<SphereCollider>();
		sphereCollider.radius = num;
		Vector3 zero = Vector3.zero;
		CalculateDirection(Head.InverseTransformPoint(Root.position), out var direction, out var distance);
		if (distance > 0f)
		{
			zero[direction] = 0f - num;
		}
		else
		{
			zero[direction] = num;
		}
		sphereCollider.center = zero;
	}

	private void AddFootColliders()
	{
		AddFootCollider(LeftFoot);
		AddFootCollider(RightFoot);
	}

	private void AddFootCollider(Transform parentFoot)
	{
		Transform child = parentFoot.GetChild(0);
		Bounds bounds = default(Bounds);
		bounds.Encapsulate(parentFoot.InverseTransformPoint(child.position + Vector3.right * 0.1f + Vector3.back * 0.2f + Vector3.down * 0.1f));
		bounds.Encapsulate(parentFoot.InverseTransformPoint(child.position + Vector3.left * 0.1f + Vector3.forward * 0.3f + Vector3.down * 0.1f));
		bounds = new Bounds(bounds.center * _scaleFoot, bounds.size * _scaleFoot);
		Vector3 size = bounds.size;
		size[SmallestComponent(bounds.size)] = size[LargestComponent(bounds.size)] / 2f;
		BoxCollider boxCollider = parentFoot.gameObject.AddComponent<BoxCollider>();
		boxCollider.center = bounds.center;
		boxCollider.size = size;
	}
}
