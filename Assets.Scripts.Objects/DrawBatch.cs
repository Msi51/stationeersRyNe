using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects;

public class DrawBatch : IPoolable<DrawBatch>
{
	public Mesh Mesh;

	public Material Material;

	private ShadowCastingMode _shadowMode;

	private int _layer;

	private readonly Matrix4x4[] _matrices = new Matrix4x4[1023];

	private int _count;

	private static readonly ObjectPool<DrawBatch> _pool = new ObjectPool<DrawBatch>("DrawBatch");

	public int PoolId { get; set; }

	public string DebugName { get; set; }

	public bool IsVisible { get; }

	public bool IsActive { get; set; }

	public static ObjectPool<DrawBatch> BatchPool => _pool;

	public ObjectPool<DrawBatch> Pool { get; set; } = _pool;

	public bool IsFull => _count >= 1023;

	public void ReturnToPool()
	{
		_count = 0;
		Mesh = null;
		Material = null;
		_pool.Return(this);
	}

	public void SetVisible(bool isVisible)
	{
	}

	public static DrawBatch Get()
	{
		return _pool.Get() ?? _pool.PopulateOnce();
	}

	public void Add(Matrix4x4 matrix)
	{
		_matrices[_count] = matrix;
		_count++;
	}

	public static void Initialize()
	{
		_pool.Initialize(100);
		_pool.PopulateAll();
	}

	public void Render()
	{
		if ((object)Mesh != null)
		{
			Graphics.DrawMeshInstanced(Mesh, 0, Material, _matrices, _count, null, _shadowMode, receiveShadows: true, _layer, null, LightProbeUsage.Off, null);
		}
	}

	public static DrawBatch Make(IBatchRendered rendered)
	{
		DrawBatch drawBatch = Get();
		drawBatch.Mesh = rendered.GetMesh();
		drawBatch.Material = rendered.GetMaterial();
		drawBatch._shadowMode = rendered.GetShadowMode();
		return drawBatch;
	}

	public static DrawBatch Make(ThingRenderer rendered)
	{
		DrawBatch drawBatch = Get();
		drawBatch.Mesh = rendered.SharedMesh;
		drawBatch.Material = GameManager.GetTextureArrayColorMaterial();
		drawBatch._shadowMode = rendered.ShadowCastingMode;
		return drawBatch;
	}
}
