using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;

public class MeshLibrary : ManagerBase
{
	public static int DEFAULT_SPHERE = Animator.StringToHash("Sphere01");

	public static MeshLibrary Instance;

	public List<Mesh> RegisteredMeshes = new List<Mesh>();

	private static Dictionary<int, Mesh> _meshLookup = new Dictionary<int, Mesh>();

	public override void ManagerStart()
	{
		base.ManagerStart();
		Instance = this;
		foreach (Mesh registeredMesh in RegisteredMeshes)
		{
			_meshLookup.Add(Animator.StringToHash(registeredMesh.name), registeredMesh);
		}
	}

	public static Mesh Find(string meshName)
	{
		return Find(Animator.StringToHash(meshName));
	}

	public static Mesh Find(int stringToHash)
	{
		_meshLookup.TryGetValue(stringToHash, out var value);
		return value;
	}
}
