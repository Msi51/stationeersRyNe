using System;
using System.Threading;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

public class InstancedIndirectDrawCall
{
	public struct MeshPerInstanceDatum
	{
		public Matrix4x4 ObjectToWorldMatrix;

		public Matrix4x4 WorldToObjectMatrix;

		public static readonly int Size = 128;
	}

	private static readonly int InstanceDataHash = Shader.PropertyToID("_InstanceData");

	private ComputeBuffer _instanceDataBuffer;

	private ComputeBuffer _argsBuffer;

	private Material _materialPrefab;

	private Mesh _baseMesh;

	public Material MaterialInstance;

	private MeshPerInstanceDatum[] _instanceData;

	private bool _isDirty;

	private bool _isInstanceDataBufferUpToDate = true;

	public readonly uint[] Args = new uint[5];

	public Bounds Bounds;

	private bool _isMainThreadInitialized;

	private int _submeshIndex;

	public ComputeBuffer InstanceDataBuffer
	{
		get
		{
			if (!_isInstanceDataBufferUpToDate)
			{
				FlushInstanceDataBuffer();
			}
			return _instanceDataBuffer;
		}
	}

	public ComputeBuffer ArgsBuffer
	{
		get
		{
			if (_isDirty)
			{
				FlushArgBuffer();
			}
			if (!_isInstanceDataBufferUpToDate)
			{
				FlushInstanceDataBuffer();
			}
			return _argsBuffer;
		}
	}

	public Material MaterialPrefab => _materialPrefab;

	public Mesh BaseMesh => _baseMesh;

	public int InstanceCount { get; private set; }

	public InstancedIndirectDrawCall(Mesh baseMesh, Material materialPrefab, int submeshIndex, int size = 1023)
	{
		_instanceData = new MeshPerInstanceDatum[size];
		Initialize(baseMesh, materialPrefab, submeshIndex, size);
		_isMainThreadInitialized = false;
	}

	private void Initialize(Mesh baseMesh, Material materialPrefab, int submeshIndex, int size)
	{
		_baseMesh = baseMesh;
		_materialPrefab = materialPrefab;
		if (_instanceData.Length < size)
		{
			_instanceData = new MeshPerInstanceDatum[size];
		}
		_submeshIndex = submeshIndex;
	}

	private void MainThreadInitialize(int subMeshIndex)
	{
		MaterialInstance = new Material(_materialPrefab);
		Args[0] = _baseMesh.GetIndexCount(subMeshIndex);
		Args[1] = 0u;
		Args[2] = _baseMesh.GetIndexStart(subMeshIndex);
		Args[3] = _baseMesh.GetBaseVertex(subMeshIndex);
		if (_argsBuffer == null)
		{
			_argsBuffer = new ComputeBuffer(1, 20, ComputeBufferType.DrawIndirect);
		}
		_isMainThreadInitialized = true;
	}

	public void Clear()
	{
		_isDirty = true;
		_isInstanceDataBufferUpToDate = false;
		InstanceCount = 0;
	}

	public void AddInstance(Matrix4x4 objectToWorld)
	{
		_isDirty = true;
		_isInstanceDataBufferUpToDate = false;
		Matrix4x4 inverse = objectToWorld.inverse;
		MeshPerInstanceDatum meshPerInstanceDatum = new MeshPerInstanceDatum
		{
			ObjectToWorldMatrix = objectToWorld,
			WorldToObjectMatrix = inverse
		};
		lock (_instanceData)
		{
			if (_instanceData.Length == InstanceCount)
			{
				MeshPerInstanceDatum[] array = new MeshPerInstanceDatum[InstanceCount + 1];
				Array.Copy(_instanceData, array, _instanceData.Length);
				_instanceData = array;
			}
			_instanceData[InstanceCount] = meshPerInstanceDatum;
			InstanceCount++;
		}
		Vector3 vector = objectToWorld.GetColumn(3);
		Bounds.Encapsulate(vector + Vector3.one);
		Bounds.Encapsulate(vector - Vector3.one);
	}

	public void FlushInstanceDataBuffer()
	{
		lock (_instanceData)
		{
			if (_instanceDataBuffer == null || _instanceDataBuffer.count < InstanceCount)
			{
				_instanceDataBuffer?.Release();
				_instanceDataBuffer = new ComputeBuffer(Mathf.Max(1, InstanceCount), MeshPerInstanceDatum.Size);
			}
			_instanceDataBuffer.SetData(_instanceData, 0, 0, InstanceCount);
			_isInstanceDataBufferUpToDate = true;
		}
		MaterialInstance.SetBuffer(InstanceDataHash, _instanceDataBuffer);
		FlushArgBuffer();
	}

	public void FlushArgBuffer()
	{
		Args[1] = (uint)InstanceCount;
		_argsBuffer.SetData(Args);
		_isDirty = false;
	}

	public void Draw(Bounds bounds, ShadowCastingMode shadowMode = ShadowCastingMode.Off, bool receiveShadows = true)
	{
		if (Thread.CurrentThread.ManagedThreadId == GameManager.MainThreadId && InstanceCount > 0)
		{
			if (!_isMainThreadInitialized)
			{
				MainThreadInitialize(_submeshIndex);
			}
			Graphics.DrawMeshInstancedIndirect(BaseMesh, 0, MaterialInstance, bounds, ArgsBuffer, 0, null, shadowMode, receiveShadows);
		}
	}

	public bool CanDraw(Material material, Mesh mesh)
	{
		if (material == MaterialPrefab)
		{
			return mesh == BaseMesh;
		}
		return false;
	}

	public void OnDestroy()
	{
		ComputeBuffer instanceDataBuffer = _instanceDataBuffer;
		ComputeBuffer argsBuffer = _argsBuffer;
		instanceDataBuffer?.Release();
		argsBuffer?.Release();
		_instanceDataBuffer = null;
		_argsBuffer = null;
	}

	public void RemoveInstance(Matrix4x4 objectToWorld)
	{
		bool flag = false;
		lock (_instanceData)
		{
			for (int i = 0; i < _instanceData.Length; i++)
			{
				if (!flag && _instanceData[i].ObjectToWorldMatrix == objectToWorld)
				{
					flag = true;
				}
				if (flag && i + 1 < _instanceData.Length - 1)
				{
					_instanceData[i] = _instanceData[i + 1];
				}
			}
		}
		if (flag)
		{
			InstanceCount--;
			_isDirty = true;
			_isInstanceDataBufferUpToDate = false;
		}
	}
}
