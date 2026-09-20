using UnityEngine;

[ExecuteInEditMode]
public class Mask : MonoBehaviour
{
	[SerializeField]
	private bool[] layers = new bool[4];

	private Mesh mesh;

	private Bounds bounds;

	private Renderer source;

	private bool skinned;

	private MaterialPropertyBlock properties;

	private bool changed = true;

	public bool Enabled
	{
		get
		{
			if (source != null)
			{
				return source.enabled;
			}
			return false;
		}
	}

	public bool Layer1
	{
		get
		{
			return layers[0];
		}
		set
		{
			layers[0] = value;
			changed = true;
		}
	}

	public bool Layer2
	{
		get
		{
			return layers[1];
		}
		set
		{
			layers[1] = value;
			changed = true;
		}
	}

	public bool Layer3
	{
		get
		{
			return layers[2];
		}
		set
		{
			layers[2] = value;
			changed = true;
		}
	}

	public bool Layer4
	{
		get
		{
			return layers[3];
		}
		set
		{
			layers[3] = value;
			changed = true;
		}
	}

	public Mesh Mesh
	{
		get
		{
			if (skinned)
			{
				if (mesh == null)
				{
					mesh = new Mesh();
				}
				((SkinnedMeshRenderer)source).BakeMesh(mesh);
			}
			return mesh;
		}
	}

	public Bounds Bounds
	{
		get
		{
			if (!(source != null))
			{
				return default(Bounds);
			}
			return source.bounds;
		}
	}

	public MaterialPropertyBlock Properties
	{
		get
		{
			UpdateProperties();
			return properties;
		}
	}

	public void Update()
	{
		changed = true;
	}

	private void UpdateProperties()
	{
		if (properties == null)
		{
			properties = new MaterialPropertyBlock();
		}
		if (changed)
		{
			if (layers.Length < 4)
			{
				layers = new bool[4];
			}
			properties.Clear();
			properties.SetFloat("_Layer1", layers[0] ? 1 : 0);
			properties.SetFloat("_Layer2", layers[1] ? 1 : 0);
			properties.SetFloat("_Layer3", layers[2] ? 1 : 0);
			properties.SetFloat("_Layer4", layers[3] ? 1 : 0);
		}
		changed = false;
	}

	private void Start()
	{
		Initialize();
		Register();
	}

	private void OnEnable()
	{
		Initialize();
		Register();
	}

	private void OnDisable()
	{
		Deregister();
	}

	private void Initialize()
	{
		if (GetComponent<MeshRenderer>() != null)
		{
			mesh = GetComponent<MeshFilter>().sharedMesh;
			source = GetComponent<MeshRenderer>();
			skinned = false;
		}
		if (GetComponent<SkinnedMeshRenderer>() != null)
		{
			source = GetComponent<SkinnedMeshRenderer>();
			skinned = true;
		}
	}

	private void Register()
	{
		DynamicDecals.AddMask(this);
	}

	private void Deregister()
	{
		DynamicDecals.RemoveMask(this);
	}
}
