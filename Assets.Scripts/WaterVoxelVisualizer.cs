using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts;

public class WaterVoxelVisualizer : MonoBehaviour
{
	public static bool Disabled = true;

	public float IsoLevel;

	public int ComputeShaderExtents;

	public int CellExtents;

	public int VisualizerExtents;

	public Material Material;

	public ComputeShader MarchingCubesCs;

	public ComputeShader DensityCs;

	public ComputeShader CountVertsCs;

	public Transform Transform;

	public AtmosphericsController AtmosController;

	public GridController GridController;

	private float[] _connectivityDataArray = new float[2048];

	private RenderTexture _densityTexture;

	private Texture3D _waterLevelTexture;

	private int _kernelMc;

	private int _kernelDc;

	private int _kernelCv;

	private ComputeBuffer _connectivityBuffer;

	private ComputeBuffer _appendVertexBuffer;

	private ComputeBuffer _argBuffer;

	private CommandBuffer _renderMarchingCubeBuffer;

	private const int ConnectivityBufferSize = 512;

	private const int ItemsInConnectivityElement = 4;

	private const int ConnectivityStride = 16;

	private Vector3 _playerGridPosition;

	private Vector3 _playerGridPositionDelta;

	public bool CamerasEnabled;

	public bool UseGPUVertCount = true;

	private Color[] _c;

	private static readonly float _minimumWaterLevelToDisplay = 0.001f;

	private static readonly float _visualizerOffset = 0.125f;

	private Vector3 _atmosPos;

	private List<Grid3> _openNeighbors = new List<Grid3>();

	private Atmosphere _atmos;

	private static readonly int Model = Shader.PropertyToID("model");

	private void Awake()
	{
		if (Disabled)
		{
			base.gameObject.SetActive(value: false);
		}
		if (!GameManager.IsBatchMode)
		{
			Material = Object.Instantiate(Material);
			MarchingCubesCs = Object.Instantiate(MarchingCubesCs);
			DensityCs = Object.Instantiate(DensityCs);
			_kernelMc = MarchingCubesCs.FindKernel("MarchingCubes");
			_kernelDc = DensityCs.FindKernel("CSMain");
			_kernelDc = CountVertsCs.FindKernel("CountVerts");
			_c = new Color[VisualizerExtents * VisualizerExtents * VisualizerExtents];
			int num = CellExtents * VisualizerExtents;
			_densityTexture = new RenderTexture(0, 0, 0)
			{
				format = RenderTextureFormat.RFloat,
				dimension = TextureDimension.Tex3D,
				width = num,
				height = num,
				volumeDepth = num,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
			_densityTexture.Create();
			_connectivityBuffer = new ComputeBuffer(512, 16);
			_waterLevelTexture = new Texture3D(VisualizerExtents, VisualizerExtents, VisualizerExtents, TextureFormat.RFloat, mipChain: false);
			_appendVertexBuffer = new ComputeBuffer((num - 1) * (num - 1) * (num - 1) * 5, 96, ComputeBufferType.Append);
			_renderMarchingCubeBuffer = new CommandBuffer();
			_argBuffer = new ComputeBuffer(4, 4, ComputeBufferType.DrawIndirect);
			int[] data = new int[4] { 0, 1, 0, 0 };
			_argBuffer.SetData(data);
			Material.SetPass(0);
			Material.SetBuffer("triangles", _appendVertexBuffer);
			Material.SetMatrix("model", Transform.localToWorldMatrix);
			_renderMarchingCubeBuffer.DrawProceduralIndirect(Matrix4x4.identity, Material, -1, MeshTopology.Triangles, _argBuffer);
			AddToCameras();
			if (IsoLevel < 0f || IsoLevel > 1f)
			{
				IsoLevel = 0.5f;
			}
			MarchingCubesCs.SetFloat("_gridSize", 4f);
			MarchingCubesCs.SetFloat("_isoLevel", IsoLevel);
			MarchingCubesCs.SetBuffer(_kernelMc, "triangleRW", _appendVertexBuffer);
			DensityCs.SetBuffer(_kernelDc, "_connectivityData", _connectivityBuffer);
			DensityCs.SetTexture(_kernelDc, "_waterHeights", _waterLevelTexture);
			DensityCs.SetTexture(_kernelDc, "_densityTexture", _densityTexture);
			MarchingCubesCs.SetTexture(_kernelMc, "_densityTexture", _densityTexture);
			CountVertsCs.SetBuffer(_kernelCv, "argBuffer", _argBuffer);
		}
	}

	private void AddToCameras()
	{
		if (!CamerasEnabled)
		{
			Camera[] allCameras = Camera.allCameras;
			for (int i = 0; i < allCameras.Length; i++)
			{
				allCameras[i].AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderMarchingCubeBuffer);
			}
			CamerasEnabled = true;
		}
	}

	private void RemoveFromCameras()
	{
		if (CamerasEnabled)
		{
			Camera[] allCameras = Camera.allCameras;
			for (int i = 0; i < allCameras.Length; i++)
			{
				allCameras[i].RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _renderMarchingCubeBuffer);
			}
			CamerasEnabled = false;
		}
	}

	private void OnDisable()
	{
		RemoveFromCameras();
	}

	private void Update()
	{
		if (Disabled)
		{
			RemoveFromCameras();
			return;
		}
		AddToCameras();
		Material.SetMatrix(Model, Transform.localToWorldMatrix);
	}

	public bool UpdateWaterVisualizer()
	{
		if (_appendVertexBuffer == null || Disabled)
		{
			return false;
		}
		DensityCs.SetTexture(_kernelDc, "_waterHeights", _waterLevelTexture);
		DensityCs.Dispatch(_kernelDc, ComputeShaderExtents, ComputeShaderExtents, ComputeShaderExtents);
		_appendVertexBuffer.SetCounterValue(0u);
		MarchingCubesCs.Dispatch(_kernelMc, ComputeShaderExtents, ComputeShaderExtents, ComputeShaderExtents);
		if (UseGPUVertCount)
		{
			int[] array = new int[4] { 0, 1, 0, 0 };
			_argBuffer.SetData(array);
			ComputeBuffer.CopyCount(_appendVertexBuffer, _argBuffer, 0);
			_argBuffer.GetData(array);
			array[0] *= 3;
			_argBuffer.SetData(array);
		}
		else
		{
			ComputeBuffer.CopyCount(_appendVertexBuffer, _argBuffer, 0);
			CountVertsCs.Dispatch(_kernelCv, 1, 1, 1);
		}
		return true;
	}

	private void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			if (_connectivityBuffer != null)
			{
				_connectivityBuffer.Release();
			}
			if (_renderMarchingCubeBuffer != null)
			{
				_renderMarchingCubeBuffer.Release();
			}
			if (_appendVertexBuffer != null)
			{
				_appendVertexBuffer.Release();
			}
			if (_argBuffer != null)
			{
				_argBuffer.Release();
			}
			_connectivityBuffer = null;
			_renderMarchingCubeBuffer = null;
			_appendVertexBuffer = null;
			_argBuffer = null;
		}
	}
}
