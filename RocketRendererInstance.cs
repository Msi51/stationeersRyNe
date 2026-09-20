using Assets.Scripts;
using UnityEngine;

public class RocketRendererInstance : MonoBehaviour
{
	[ReadOnly]
	public Mesh mesh;

	[ReadOnly]
	private Material[] _materials;

	[ReadOnly]
	[Tooltip("Whether this instance has already been registered with the RocketRendererManager and will be drawn")]
	public bool Registered;

	public Transform Transform;

	public Material[] Materials
	{
		get
		{
			return _materials;
		}
		set
		{
			_materials = value;
			Refresh();
		}
	}

	private void Awake()
	{
		base.enabled = false;
		Transform = base.transform;
	}

	private void OnEnable()
	{
		RegisterToManager();
	}

	private void OnDestroy()
	{
		DeregisterFromManager();
	}

	private void OnDisable()
	{
		DeregisterFromManager();
	}

	public void RegisterToManager()
	{
		if (Registered)
		{
			DeregisterFromManager();
		}
		MeshFilter component = GetComponent<MeshFilter>();
		if (component != null && component.sharedMesh != null)
		{
			mesh = component.sharedMesh;
			RocketRendererManager.instance.Register(this);
			Registered = true;
		}
	}

	public void DeregisterFromManager()
	{
		if (Registered && RocketRendererManager.instance != null)
		{
			RocketRendererManager.instance.Deregister(this);
			Registered = false;
		}
	}

	public void Refresh()
	{
		if (base.isActiveAndEnabled)
		{
			DeregisterFromManager();
			RegisterToManager();
		}
	}
}
