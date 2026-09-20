using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DynamicDecals
{
	internal class CameraData
	{
		public RenderingPath path;

		public CommandBuffer maskBuffer;

		public CommandBuffer projectionBuffer;

		public CullingGroup maskCulling;

		public CullingGroup projectionCulling;

		public bool sceneCamera;

		public bool previewCamera;

		public CameraData(Camera Camera, RenderingPath Path, CommandBuffer MaskBuffer, CommandBuffer ProjectionBuffer, CullingGroup MaskCulling, CullingGroup ProjectionCulling)
		{
			path = Path;
			maskBuffer = MaskBuffer;
			projectionBuffer = ProjectionBuffer;
			maskCulling = MaskCulling;
			projectionCulling = ProjectionCulling;
			sceneCamera = Camera.name.Equals("SceneCamera");
			previewCamera = Camera.name.Equals("Preview Camera");
		}
	}

	private static DynamicDecals system;

	private DynamicDecalSettings settings;

	private bool sceneFocus;

	private bool cameraClipping;

	private Material forwardBlit;

	private Material deferredBlit;

	private Material mask;

	private Material metallic;

	private Material metallicCutout;

	private Material specular;

	private Material specularCutout;

	private Material unlit;

	private Material unlitCutout;

	private Material roughness;

	private Material roughnessCutout;

	private Material normal;

	private Material normalCutout;

	private Material pulse;

	private Material pulseCutout;

	private Material eraser;

	private Material eraserCutout;

	private Material eraserGrab;

	private Mesh cube;

	private bool sort;

	private List<Projection> projections;

	private BoundingSphere[] projectionSpheres;

	private int staticCount;

	private List<Mask> masks;

	private BoundingSphere[] maskSpheres;

	internal static Dictionary<Camera, CameraData> cameraData = new Dictionary<Camera, CameraData>();

	private bool initialized;

	private bool updated;

	private static RenderTargetIdentifier[] one = new RenderTargetIdentifier[1];

	private static RenderTargetIdentifier[] two = new RenderTargetIdentifier[2];

	private static RenderTargetIdentifier[] three = new RenderTargetIdentifier[3];

	private static RenderTargetIdentifier[] four = new RenderTargetIdentifier[4];

	private static DynamicDecals System
	{
		get
		{
			if (system == null)
			{
				system = new DynamicDecals();
			}
			return system;
		}
	}

	public static DynamicDecalSettings Settings
	{
		get
		{
			if (System.settings == null)
			{
				System.settings = Resources.Load<DynamicDecalSettings>("Settings");
			}
			if (System.settings == null)
			{
				System.settings = new DynamicDecalSettings();
			}
			return System.settings;
		}
	}

	public static RenderingPath RenderingPath { get; private set; }

	private static CameraEvent ForwardMaskEvent => CameraEvent.AfterDepthNormalsTexture;

	private static DepthTextureMode ForwardDepthTextureMode => DepthTextureMode.DepthNormals;

	private static CameraEvent DeferredMaskEvent => CameraEvent.BeforeReflections;

	private static CameraEvent DeferredProjectionEvent => CameraEvent.BeforeReflections;

	private static DepthTextureMode DeferredDepthTextureMode => DepthTextureMode.DepthNormals;

	public static Material Mat_ForwardBlit
	{
		get
		{
			if (System.forwardBlit == null)
			{
				System.forwardBlit = new Material(Shader.Find("Decal/ForwardBlit"));
			}
			return System.forwardBlit;
		}
	}

	public static Material Mat_DeferredBlit
	{
		get
		{
			if (System.deferredBlit == null)
			{
				System.deferredBlit = new Material(Shader.Find("Decal/DeferredBlit"));
			}
			return System.deferredBlit;
		}
	}

	public static Material Mat_Mask
	{
		get
		{
			if (System.mask == null)
			{
				System.mask = new Material(Shader.Find("Decal/Mask"));
			}
			return System.mask;
		}
	}

	public static Material Mat_Decal_Metallic
	{
		get
		{
			if (System.metallic == null)
			{
				System.metallic = new Material(Shader.Find("Decal/Metallic"));
				System.metallic.DisableKeyword("_AlphaTest");
				System.metallic.EnableKeyword("_Blend");
			}
			return System.metallic;
		}
	}

	public static Material Mat_Decal_MetallicCutout
	{
		get
		{
			if (System.metallicCutout == null)
			{
				System.metallicCutout = new Material(Shader.Find("Decal/Metallic"));
				System.metallicCutout.EnableKeyword("_AlphaTest");
				System.metallicCutout.DisableKeyword("_Blend");
			}
			return System.metallicCutout;
		}
	}

	public static Material Mat_Decal_Specular
	{
		get
		{
			if (System.specular == null)
			{
				System.specular = new Material(Shader.Find("Decal/Specular"));
				System.specular.DisableKeyword("_AlphaTest");
				System.specular.EnableKeyword("_Blend");
			}
			return System.specular;
		}
	}

	public static Material Mat_Decal_SpecularCutout
	{
		get
		{
			if (System.specularCutout == null)
			{
				System.specularCutout = new Material(Shader.Find("Decal/Specular"));
				System.specularCutout.EnableKeyword("_AlphaTest");
				System.specularCutout.DisableKeyword("_Blend");
			}
			return System.specularCutout;
		}
	}

	public static Material Mat_Decal_Unlit
	{
		get
		{
			if (System.unlit == null)
			{
				System.unlit = new Material(Shader.Find("Decal/Unlit"));
				System.unlit.DisableKeyword("_AlphaTest");
				System.unlit.EnableKeyword("_Blend");
			}
			return System.unlit;
		}
	}

	public static Material Mat_Decal_UnlitCutout
	{
		get
		{
			if (System.unlitCutout == null)
			{
				System.unlitCutout = new Material(Shader.Find("Decal/Unlit"));
				System.unlitCutout.EnableKeyword("_AlphaTest");
				System.unlitCutout.DisableKeyword("_Blend");
			}
			return System.unlitCutout;
		}
	}

	public static Material Mat_Decal_Roughness
	{
		get
		{
			if (System.roughness == null)
			{
				System.roughness = new Material(Shader.Find("Decal/Roughness"));
				System.roughness.DisableKeyword("_AlphaTest");
				System.roughness.EnableKeyword("_Blend");
			}
			return System.roughness;
		}
	}

	public static Material Mat_Decal_RoughnessCutout
	{
		get
		{
			if (System.roughnessCutout == null)
			{
				System.roughnessCutout = new Material(Shader.Find("Decal/Roughness"));
				System.roughnessCutout.EnableKeyword("_AlphaTest");
				System.roughnessCutout.DisableKeyword("_Blend");
			}
			return System.roughnessCutout;
		}
	}

	public static Material Mat_Decal_Normal
	{
		get
		{
			if (System.normal == null)
			{
				System.normal = new Material(Shader.Find("Decal/Normal"));
				System.normal.DisableKeyword("_AlphaTest");
				System.normal.EnableKeyword("_Blend");
			}
			return System.normal;
		}
	}

	public static Material Mat_Decal_NormalCutout
	{
		get
		{
			if (System.normalCutout == null)
			{
				System.normalCutout = new Material(Shader.Find("Decal/Normal"));
				System.normalCutout.EnableKeyword("_AlphaTest");
				System.normalCutout.DisableKeyword("_Blend");
			}
			return System.normalCutout;
		}
	}

	public static Material Mat_Pulse
	{
		get
		{
			if (System.pulse == null)
			{
				System.pulse = new Material(Shader.Find("Decal/Pulse"));
				System.pulse.DisableKeyword("_AlphaTest");
				System.pulse.EnableKeyword("_Blend");
			}
			return System.pulse;
		}
	}

	public static Material Mat_PulseCutout
	{
		get
		{
			if (System.pulseCutout == null)
			{
				System.pulseCutout = new Material(Shader.Find("Decal/Pulse"));
				System.pulseCutout.EnableKeyword("_AlphaTest");
				System.pulseCutout.DisableKeyword("_Blend");
			}
			return System.pulseCutout;
		}
	}

	public static Material Mat_Eraser
	{
		get
		{
			if (System.eraser == null)
			{
				System.eraser = new Material(Shader.Find("Decal/Eraser"));
				System.eraser.DisableKeyword("_AlphaTest");
				System.eraser.EnableKeyword("_Blend");
			}
			return System.eraser;
		}
	}

	public static Material Mat_EraserCutout
	{
		get
		{
			if (System.eraserCutout == null)
			{
				System.eraserCutout = new Material(Shader.Find("Decal/Eraser"));
				System.eraserCutout.EnableKeyword("_AlphaTest");
				System.eraserCutout.DisableKeyword("_Blend");
			}
			return System.eraserCutout;
		}
	}

	public static Material Mat_EraserGrab
	{
		get
		{
			if (System.eraserGrab == null)
			{
				System.eraserGrab = new Material(Shader.Find("Decal/EraserGrab"));
			}
			return System.eraserGrab;
		}
	}

	public static Mesh Cube
	{
		get
		{
			if (System.cube == null)
			{
				GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
				System.cube = gameObject.GetComponent<MeshFilter>().sharedMesh;
				UnityEngine.Object.DestroyImmediate(gameObject);
			}
			return System.cube;
		}
	}

	public static bool StaticPass
	{
		get
		{
			if (System.staticCount <= 0)
			{
				return false;
			}
			return true;
		}
	}

	~DynamicDecals()
	{
		if (system.initialized)
		{
			Debug.LogWarning("System shutting down via Deconstructor. Forcing Uninitialization");
		}
		Uninitialize(Forced: true);
	}

	private static void CheckRenderingPath()
	{
		Camera camera = null;
		if (Camera.main != null)
		{
			camera = Camera.main;
		}
		else if (Camera.current != null)
		{
			camera = Camera.current;
		}
		if (!(camera != null))
		{
			return;
		}
		switch (Settings.systemRenderingPath)
		{
		case SystemRenderingPath.Forward:
			RenderingPath = RenderingPath.Forward;
			break;
		case SystemRenderingPath.Deferred:
			RenderingPath = RenderingPath.DeferredShading;
			break;
		case SystemRenderingPath.Auto:
			if (camera.actualRenderingPath == RenderingPath.Forward)
			{
				RenderingPath = RenderingPath.Forward;
			}
			else if (camera.actualRenderingPath == RenderingPath.DeferredShading)
			{
				RenderingPath = RenderingPath.DeferredShading;
			}
			else
			{
				Debug.LogWarning("Current Rendering Path not supported! Please use either Forward or Deferred");
			}
			break;
		}
	}

	private static void UpdateRenderingPath(Camera Camera, CameraData Data)
	{
		if (Data.path != RenderingPath)
		{
			switch (Data.path)
			{
			case RenderingPath.Forward:
				Camera.RemoveCommandBuffer(ForwardMaskEvent, Data.maskBuffer);
				break;
			case RenderingPath.DeferredShading:
				Camera.RemoveCommandBuffer(DeferredMaskEvent, Data.maskBuffer);
				Camera.RemoveCommandBuffer(DeferredProjectionEvent, Data.projectionBuffer);
				break;
			}
			Data.path = RenderingPath;
			switch (Data.path)
			{
			case RenderingPath.Forward:
				SetForward(Camera, Data.maskBuffer, Data.projectionBuffer);
				break;
			case RenderingPath.DeferredShading:
				SetDeferred(Camera, Data.maskBuffer, Data.projectionBuffer);
				break;
			}
		}
		else
		{
			switch (Data.path)
			{
			case RenderingPath.Forward:
				UpdateForward(Camera);
				break;
			case RenderingPath.DeferredShading:
				UpdateDeferred(Camera);
				break;
			}
		}
	}

	private static void LockClippingPlanes()
	{
	}

	public static void AddProjection(Projection Projection)
	{
		Initialize();
		if (System.projections == null)
		{
			System.projections = new List<Projection>();
		}
		if (system.projections.Contains(Projection))
		{
			return;
		}
		if (system.projections.Count == 0)
		{
			system.projections.Add(Projection);
		}
		else
		{
			for (int i = 0; i < system.projections.Count; i++)
			{
				if (Projection.Priority < system.projections[i].Priority)
				{
					system.projections.Insert(i, Projection);
					break;
				}
				if (Projection.Priority == system.projections[i].Priority && Projection.timeID < system.projections[i].timeID)
				{
					system.projections.Insert(i, Projection);
					break;
				}
				if (i == system.projections.Count - 1)
				{
					system.projections.Add(Projection);
					break;
				}
			}
		}
		if (Projection.GetType() == typeof(Eraser))
		{
			system.staticCount++;
		}
	}

	public static void RemoveProjection(Projection Projection)
	{
		if (system.projections.Remove(Projection) && Projection.GetType() == typeof(Eraser))
		{
			system.staticCount = Mathf.Clamp(system.staticCount - 1, 0, 10000000);
		}
		Uninitialize();
	}

	public static void Sort()
	{
		system.sort = true;
	}

	private static void ReorderProjections()
	{
		if (!System.sort || RenderingPath != RenderingPath.DeferredShading)
		{
			return;
		}
		System.projections.Sort(delegate(Projection x, Projection y)
		{
			if (x.Priority > y.Priority)
			{
				return 1;
			}
			if (x.Priority < y.Priority)
			{
				return -1;
			}
			if (x.timeID > y.timeID)
			{
				return 1;
			}
			return (x.timeID < y.timeID) ? (-1) : 0;
		});
		System.sort = false;
	}

	private static void UpdateProjections()
	{
		for (int i = 0; i < system.projections.Count; i++)
		{
			system.projections[i].UpdateProjection();
		}
	}

	public static void AddMask(Mask Mask)
	{
		if (System.masks == null)
		{
			System.masks = new List<Mask>();
		}
		if (!system.masks.Contains(Mask))
		{
			system.masks.Add(Mask);
		}
	}

	public static void RemoveMask(Mask Mask)
	{
		if (System.masks == null)
		{
			System.masks = new List<Mask>();
		}
		system.masks.Remove(Mask);
	}

	internal static CameraData GetData(Camera Camera)
	{
		if (!cameraData.TryGetValue(Camera, out var value))
		{
			RenderingPath actualRenderingPath = Camera.actualRenderingPath;
			CommandBuffer commandBuffer = new CommandBuffer();
			commandBuffer.name = "Dynamic Decals - Masking";
			CommandBuffer commandBuffer2 = new CommandBuffer();
			commandBuffer2.name = "Dynamic Decals - Projection";
			switch (actualRenderingPath)
			{
			case RenderingPath.Forward:
				SetForward(Camera, commandBuffer, commandBuffer2);
				break;
			case RenderingPath.DeferredShading:
				SetDeferred(Camera, commandBuffer, commandBuffer2);
				break;
			}
			CullingGroup cullingGroup = new CullingGroup();
			CullingGroup cullingGroup2 = new CullingGroup();
			cullingGroup.targetCamera = Camera;
			cullingGroup2.targetCamera = Camera;
			value = new CameraData(Camera, actualRenderingPath, commandBuffer, commandBuffer2, cullingGroup, cullingGroup2);
			cameraData[Camera] = value;
		}
		return value;
	}

	private static void SetForward(Camera Camera, CommandBuffer MaskBuffer, CommandBuffer ProjectionBuffer)
	{
		Camera.AddCommandBuffer(ForwardMaskEvent, MaskBuffer);
		UpdateForward(Camera);
	}

	private static void UpdateForward(Camera Camera)
	{
		Camera.depthTextureMode = ForwardDepthTextureMode;
	}

	private static void SetDeferred(Camera Camera, CommandBuffer MaskBuffer, CommandBuffer ProjectionBuffer)
	{
		Camera.AddCommandBuffer(DeferredMaskEvent, MaskBuffer);
		Camera.AddCommandBuffer(DeferredProjectionEvent, ProjectionBuffer);
		UpdateDeferred(Camera);
	}

	private static void UpdateDeferred(Camera Camera)
	{
		Camera.depthTextureMode = DeferredDepthTextureMode;
	}

	public static void Initialize()
	{
		if (!System.initialized)
		{
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(CullProjections));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPreRender, new Camera.CameraCallback(RenderProjections));
			System.initialized = true;
		}
	}

	public static void Uninitialize(bool Forced = false)
	{
		if (!Forced && (!System.initialized || system.projections.Count != 0))
		{
			return;
		}
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(CullProjections));
		Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPreRender, new Camera.CameraCallback(RenderProjections));
		foreach (KeyValuePair<Camera, CameraData> cameraDatum in cameraData)
		{
			if (cameraDatum.Key != null)
			{
				switch (cameraDatum.Value.path)
				{
				case RenderingPath.Forward:
					cameraDatum.Key.RemoveCommandBuffer(ForwardMaskEvent, cameraDatum.Value.maskBuffer);
					break;
				case RenderingPath.DeferredShading:
					cameraDatum.Key.RemoveCommandBuffer(DeferredMaskEvent, cameraDatum.Value.maskBuffer);
					cameraDatum.Key.RemoveCommandBuffer(DeferredProjectionEvent, cameraDatum.Value.projectionBuffer);
					break;
				}
			}
			if (cameraDatum.Value.maskCulling != null)
			{
				cameraDatum.Value.maskCulling.Dispose();
				cameraDatum.Value.maskCulling = null;
			}
			if (cameraDatum.Value.projectionCulling != null)
			{
				cameraDatum.Value.projectionCulling.Dispose();
				cameraDatum.Value.maskCulling = null;
			}
		}
		cameraData.Clear();
		System.initialized = false;
	}

	private static void CullProjections(Camera Camera)
	{
		if (System.initialized && !System.updated)
		{
			CheckRenderingPath();
			ReorderProjections();
			UpdateProjections();
			ProjectionPool.Update(Time.deltaTime);
			RequestCullUpdate();
			System.updated = true;
		}
		CameraData data = GetData(Camera);
		if (data != null)
		{
			if (system.masks != null && system.masks.Count > 0)
			{
				data.maskCulling.SetBoundingSpheres(system.maskSpheres);
			}
			if (RenderingPath == RenderingPath.DeferredShading && system.projections != null && system.projections.Count > 0)
			{
				data.projectionCulling.SetBoundingSpheres(system.projectionSpheres);
			}
		}
	}

	private static void RenderProjections(Camera Camera)
	{
		System.updated = false;
		CameraData data = GetData(Camera);
		if (data != null)
		{
			if (data.sceneCamera && Camera.farClipPlane > 1000f)
			{
				LockClippingPlanes();
			}
			if (!system.sceneFocus && data.sceneCamera && Camera.farClipPlane > 100000f)
			{
				Debug.LogWarning("Scene Camera focused on a large object - Projections in the scene view may apear strange if at all. To fix this, simply focus on an object with a reasonable scale. (Select then F key). This occurs as Unity sets its far clipping plane absurdly high when focusing large objects, which decreases the depthBuffer accuracy.");
				system.sceneFocus = true;
			}
			if (!system.cameraClipping && !data.sceneCamera && !data.previewCamera && Camera.farClipPlane > 1000000f)
			{
				Debug.LogWarning("Cameras far clipping plane is too high to maintain an accurate Depth Buffer - Projections may appear choppy or not at all. You'll also have a host of other issues, z-fighting among your objects etc.");
				system.cameraClipping = true;
			}
			UpdateRenderingPath(Camera, data);
			UpdateMaskBuffer(Camera, data);
			UpdateProjectionBuffer(Camera, data);
		}
	}

	internal static void RequestCullUpdate()
	{
		if (system.masks != null && system.masks.Count > 0)
		{
			if (system.maskSpheres == null || system.maskSpheres.Length < system.masks.Count)
			{
				system.maskSpheres = new BoundingSphere[system.masks.Count * 2];
			}
			for (int i = 0; i < system.masks.Count; i++)
			{
				if (system.masks[i].Enabled)
				{
					Bounds bounds = system.masks[i].Bounds;
					system.maskSpheres[i].position = bounds.center;
					system.maskSpheres[i].radius = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 1.5f;
				}
			}
		}
		if (RenderingPath == RenderingPath.DeferredShading && system.projections != null && system.projections.Count > 0)
		{
			if (system.projectionSpheres == null || system.projectionSpheres.Length < system.projections.Count)
			{
				system.projectionSpheres = new BoundingSphere[system.projections.Count * 2];
			}
			for (int j = 0; j < system.projections.Count; j++)
			{
				Transform transform = system.projections[j].transform;
				Vector3 lossyScale = transform.lossyScale;
				system.projectionSpheres[j].position = transform.position;
				system.projectionSpheres[j].radius = Mathf.Max(lossyScale.x, Mathf.Max(lossyScale.y, lossyScale.z));
			}
		}
	}

	private static void UpdateMaskBuffer(Camera Camera, CameraData Data)
	{
		Data.maskBuffer.Clear();
		switch (Camera.actualRenderingPath)
		{
		case RenderingPath.Forward:
			DrawMasksForward(Camera, Data.maskBuffer, Data.maskCulling);
			break;
		case RenderingPath.DeferredShading:
			DrawMasksDeferrred(Camera, Data.maskBuffer, Data.maskCulling);
			break;
		}
	}

	private static void DrawMasksForward(Camera Camera, CommandBuffer Buffer, CullingGroup MasksCullingGroup)
	{
		int num = Shader.PropertyToID("_MaskBuffer");
		Buffer.GetTemporaryRT(num, -1, -1, 8);
		Buffer.SetRenderTarget(num, BuiltinRenderTextureType.CurrentActive);
		Buffer.DrawMesh(Cube, Camera.transform.localToWorldMatrix, Mat_Mask, 0, 0);
		List<Mask> list = system.masks;
		if (list != null && list.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				try
				{
					if (MasksCullingGroup.IsVisible(i))
					{
						DrawMask(Camera, Buffer, list[i]);
					}
				}
				catch (IndexOutOfRangeException)
				{
					DrawMask(Camera, Buffer, list[i]);
				}
			}
		}
		Buffer.ReleaseTemporaryRT(num);
	}

	private static void DrawMasksDeferrred(Camera Camera, CommandBuffer Buffer, CullingGroup MasksCullingGroup)
	{
		int num = Shader.PropertyToID("_MaskBuffer");
		Buffer.GetTemporaryRT(num, -1, -1);
		Buffer.SetRenderTarget(num, BuiltinRenderTextureType.CameraTarget);
		Buffer.DrawMesh(Cube, Camera.transform.localToWorldMatrix, Mat_Mask, 0, 0);
		List<Mask> list = system.masks;
		if (list != null && list.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				try
				{
					if (MasksCullingGroup.IsVisible(i))
					{
						DrawMask(Camera, Buffer, list[i]);
					}
				}
				catch (IndexOutOfRangeException)
				{
					DrawMask(Camera, Buffer, list[i]);
				}
			}
		}
		Buffer.ReleaseTemporaryRT(num);
	}

	private static void DrawMask(Camera Camera, CommandBuffer Buffer, Mask Mask)
	{
		if (Mask.Enabled)
		{
			for (int i = 0; i < Mask.Mesh.subMeshCount; i++)
			{
				Buffer.DrawMesh(Mask.Mesh, Mask.transform.localToWorldMatrix, Mat_Mask, i, 0, Mask.Properties);
			}
		}
	}

	private static void UpdateProjectionBuffer(Camera Camera, CameraData Data)
	{
		Data.projectionBuffer.Clear();
		List<Projection> list = system.projections;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		RenderingPath path = Data.path;
		if (path == RenderingPath.Forward || path != RenderingPath.DeferredShading)
		{
			return;
		}
		if (StaticPass)
		{
			MultiChannelFullScreenBlit(Camera, Data.projectionBuffer);
		}
		else
		{
			StaticNormalFullScreenBlit(Camera, Data.projectionBuffer);
		}
		for (int i = 0; i < list.Count; i++)
		{
			try
			{
				if (Data.projectionCulling.IsVisible(i))
				{
					DrawDeferredProjection(Camera, Data.projectionBuffer, list[i]);
					list[i].SetVisibility(Visible: true);
				}
				else
				{
					list[i].SetVisibility(Visible: false);
				}
			}
			catch (IndexOutOfRangeException)
			{
				DrawDeferredProjection(Camera, Data.projectionBuffer, list[i]);
			}
		}
	}

	private static void DrawDeferredProjection(Camera Camera, CommandBuffer Buffer, Projection Projection)
	{
		if (Projection.isActiveAndEnabled && Projection.RenderMaterial != null && Projection.DeferredBuffers != null && Projection.DeferredBuffers.Length != 0)
		{
			if (Projection.DeferredPrePass)
			{
				MultiChannelBlit(Camera, Buffer, Projection);
			}
			if (Camera.allowHDR)
			{
				Buffer.SetRenderTarget(Projection.DeferredHDRTargets, BuiltinRenderTextureType.CameraTarget);
			}
			else
			{
				Buffer.SetRenderTarget(Projection.DeferredTargets, BuiltinRenderTextureType.CameraTarget);
			}
			Buffer.DrawMesh(Cube, Projection.RenderMatrix, Projection.RenderMaterial, 0, Projection.DeferredPass, Projection.MaterialProperties);
		}
	}

	public static RenderTargetIdentifier[] PassesToTargets(bool[] Channels, bool HDR)
	{
		int num = 0;
		if (Channels[0])
		{
			num += 2;
		}
		if (Channels[1])
		{
			num++;
		}
		if (Channels[2])
		{
			num++;
		}
		RenderTargetIdentifier[] array = null;
		switch (num)
		{
		case 0:
			return null;
		case 1:
			array = one;
			break;
		case 2:
			array = two;
			break;
		case 3:
			array = three;
			break;
		case 4:
			array = four;
			break;
		}
		num = 0;
		if (Channels[0])
		{
			array[num] = BuiltinRenderTextureType.GBuffer0;
			num++;
		}
		if (Channels[1])
		{
			array[num] = BuiltinRenderTextureType.GBuffer1;
			num++;
		}
		if (Channels[2])
		{
			array[num] = BuiltinRenderTextureType.GBuffer2;
			num++;
		}
		if (Channels[0])
		{
			if (HDR)
			{
				array[num] = BuiltinRenderTextureType.CameraTarget;
				num++;
			}
			else
			{
				array[num] = BuiltinRenderTextureType.GBuffer3;
				num++;
			}
		}
		return array;
	}

	private static void MultiChannelBlit(Camera Camera, CommandBuffer Buffer, Projection Projection)
	{
		int num = Shader.PropertyToID("_DynAlbedo");
		int num2 = Shader.PropertyToID("_DynAmbient");
		int num3 = Shader.PropertyToID("_DynGloss");
		int num4 = Shader.PropertyToID("_DynNormal");
		if (Projection.DeferredBuffers[0])
		{
			Buffer.GetTemporaryRT(num, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
			if (Camera.allowHDR)
			{
				Buffer.GetTemporaryRT(num2, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
			}
			else
			{
				Buffer.GetTemporaryRT(num2, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
			}
		}
		if (Projection.DeferredBuffers[1])
		{
			Buffer.GetTemporaryRT(num3, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
		}
		if (Projection.DeferredBuffers[2])
		{
			Buffer.GetTemporaryRT(num4, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
		}
		if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[1] && Projection.DeferredBuffers[2])
		{
			four[0] = num;
			four[1] = num3;
			four[2] = num4;
			four[3] = num2;
			DrawPrePass(Camera, Buffer, four, 6, Projection);
		}
		else if (Projection.DeferredBuffers[1] && Projection.DeferredBuffers[2])
		{
			two[0] = num3;
			two[1] = num4;
			DrawPrePass(Camera, Buffer, two, 5, Projection);
		}
		else if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[2])
		{
			three[0] = num;
			three[1] = num4;
			three[2] = num2;
			DrawPrePass(Camera, Buffer, three, 4, Projection);
		}
		else if (Projection.DeferredBuffers[0] && Projection.DeferredBuffers[1])
		{
			three[0] = num;
			three[1] = num3;
			three[2] = num2;
			DrawPrePass(Camera, Buffer, three, 3, Projection);
		}
		else if (Projection.DeferredBuffers[2])
		{
			one[0] = num4;
			DrawPrePass(Camera, Buffer, one, 2, Projection);
		}
		else if (Projection.DeferredBuffers[1])
		{
			one[0] = num3;
			DrawPrePass(Camera, Buffer, one, 1, Projection);
		}
		else
		{
			two[0] = num;
			two[1] = num2;
			DrawPrePass(Camera, Buffer, two, 0, Projection);
		}
		if (Projection.DeferredBuffers[0])
		{
			Buffer.ReleaseTemporaryRT(num);
			Buffer.ReleaseTemporaryRT(num2);
		}
		if (Projection.DeferredBuffers[1])
		{
			Buffer.ReleaseTemporaryRT(num3);
		}
		if (Projection.DeferredBuffers[2])
		{
			Buffer.ReleaseTemporaryRT(num4);
		}
	}

	private static void DrawPrePass(Camera Camera, CommandBuffer Buffer, RenderTargetIdentifier[] Buffers, int Pass, Projection Projection)
	{
		if (Buffers.Length != 0)
		{
			Buffer.SetRenderTarget(Buffers, BuiltinRenderTextureType.CameraTarget);
			Buffer.DrawMesh(Cube, Projection.RenderMatrix, Mat_DeferredBlit, 0, Pass);
		}
	}

	private static void StaticNormalFullScreenBlit(Camera Camera, CommandBuffer Buffer)
	{
		int num = Shader.PropertyToID("_StcNormal");
		Buffer.GetTemporaryRT(num, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
		Buffer.SetRenderTarget(num, BuiltinRenderTextureType.CameraTarget);
		Buffer.DrawMesh(Cube, Camera.transform.localToWorldMatrix, Mat_DeferredBlit, 0, 2);
		Buffer.ReleaseTemporaryRT(num);
	}

	private static void MultiChannelFullScreenBlit(Camera Camera, CommandBuffer Buffer)
	{
		int num = Shader.PropertyToID("_StcAlbedo");
		int num2 = Shader.PropertyToID("_StcGloss");
		int num3 = Shader.PropertyToID("_StcNormal");
		int num4 = Shader.PropertyToID("_StcAmbient");
		Buffer.GetTemporaryRT(num, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
		Buffer.GetTemporaryRT(num2, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
		Buffer.GetTemporaryRT(num3, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
		if (Camera.allowHDR)
		{
			Buffer.GetTemporaryRT(num4, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
		}
		else
		{
			Buffer.GetTemporaryRT(num4, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
		}
		four[0] = num;
		four[1] = num2;
		four[2] = num3;
		four[3] = num4;
		FullPrePass(Camera, Buffer, four);
		Buffer.ReleaseTemporaryRT(num);
		Buffer.ReleaseTemporaryRT(num2);
		Buffer.ReleaseTemporaryRT(num3);
		Buffer.ReleaseTemporaryRT(num4);
	}

	private static void FullPrePass(Camera Camera, CommandBuffer Buffer, RenderTargetIdentifier[] Buffers)
	{
		Buffer.SetRenderTarget(Buffers, BuiltinRenderTextureType.CameraTarget);
		Buffer.DrawMesh(Cube, Camera.transform.localToWorldMatrix, Mat_DeferredBlit, 0, 6);
	}
}
