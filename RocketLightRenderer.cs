using System;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

public class RocketLightRenderer : MonoBehaviour
{
	public struct ShadowInstance
	{
		public ComputeBuffer visibleInstancesBuffer;

		public ComputeBuffer renderArgsBuffer;

		public ComputeBuffer planesBuffer;

		public CommandBuffer commandBuffer;

		public Material materialInstance;
	}

	public Light TargetLight;

	public ComputeShader CullingComputeShader;

	public ComputeShader ExtractFrustumComputeShader;

	private int extractFrustumKernelIndex;

	private int cullingKernelIndex;

	[ReadOnly]
	[Tooltip("Number of draw calls this LightRenderer will emit per frame. (Number of InstancedIndirectDrawCalls * number of shadow map passes)")]
	public int instanceCount;

	private Dictionary<Tuple<InstancedIndirectDrawCall, ShadowMapPass>, ShadowInstance> drawCallLookup = new Dictionary<Tuple<InstancedIndirectDrawCall, ShadowMapPass>, ShadowInstance>();

	private static Dictionary<InstancedIndirectDrawCall, ComputeBuffer> VisibleInstanceBufferCache = new Dictionary<InstancedIndirectDrawCall, ComputeBuffer>();

	private static readonly ShadowMapPass[] PointLightPasses = new ShadowMapPass[6]
	{
		ShadowMapPass.PointlightNegativeX,
		ShadowMapPass.PointlightNegativeY,
		ShadowMapPass.PointlightNegativeZ,
		ShadowMapPass.PointlightPositiveX,
		ShadowMapPass.PointlightPositiveY,
		ShadowMapPass.PointlightPositiveZ
	};

	private static readonly ShadowMapPass[] DirectionalPasses = new ShadowMapPass[4]
	{
		ShadowMapPass.DirectionalCascade0,
		ShadowMapPass.DirectionalCascade1,
		ShadowMapPass.DirectionalCascade2,
		ShadowMapPass.DirectionalCascade3
	};

	public static readonly int CullingPlanesPropertyID = Shader.PropertyToID("CullingPlanes");

	public static readonly int AllInstancesPropertyID = Shader.PropertyToID("AllInstances");

	public static readonly int VisibleInstancesPropertyID = Shader.PropertyToID("VisibleInstances");

	public static readonly int InputInstanceCountPropertyID = Shader.PropertyToID("InputInstanceCount");

	public static float Sqrt2 = 1.4142135f;

	public static float OneOverSqrt2 = 0.70710677f;

	public void OnEnable()
	{
		TargetLight = GetComponentInChildren<Light>();
		CullingComputeShader = RocketRendererManager.instance.CullingComputeShader;
		cullingKernelIndex = CullingComputeShader.FindKernel("CSMain");
		ExtractFrustumComputeShader = RocketRendererManager.instance.ExtractFrustumShader;
		extractFrustumKernelIndex = ExtractFrustumComputeShader.FindKernel("CSMain");
		ShadowMapPass[] array = TargetLight.type switch
		{
			LightType.Directional => DirectionalPasses, 
			LightType.Point => PointLightPasses, 
			_ => new ShadowMapPass[1] { ShadowMapPass.All }, 
		};
		foreach (InstancedIndirectDrawCall allManagedDrawCall in RocketRendererManager.instance.AllManagedDrawCalls)
		{
			if (allManagedDrawCall.InstanceCount <= 0)
			{
				continue;
			}
			ShadowMapPass[] array2 = array;
			foreach (ShadowMapPass shadowMapPass in array2)
			{
				if (!drawCallLookup.TryGetValue(new Tuple<InstancedIndirectDrawCall, ShadowMapPass>(allManagedDrawCall, shadowMapPass), out var value))
				{
					value = CreateShadowInstance(allManagedDrawCall, shadowMapPass);
					instanceCount++;
					drawCallLookup[new Tuple<InstancedIndirectDrawCall, ShadowMapPass>(allManagedDrawCall, shadowMapPass)] = value;
				}
				TargetLight.AddCommandBuffer(LightEvent.AfterShadowMapPass, value.commandBuffer, shadowMapPass);
			}
		}
	}

	public void Start()
	{
		RocketRendererManager.instance.RegisterLight(this);
	}

	public void OnDestroy()
	{
		RocketRendererManager.instance.DeregisterLight(this);
	}

	public void OnDisable()
	{
		foreach (KeyValuePair<Tuple<InstancedIndirectDrawCall, ShadowMapPass>, ShadowInstance> item in drawCallLookup)
		{
			TargetLight.RemoveCommandBuffer(LightEvent.AfterShadowMapPass, item.Value.commandBuffer);
			ReleaseShadowInstance(item.Value);
		}
		drawCallLookup.Clear();
		VisibleInstanceBufferCache.Clear();
		instanceCount = 0;
		Debug.Log("Rocket Light Renderer disabled", this);
	}

	public void ReleaseShadowInstance(ShadowInstance shadowInstance)
	{
		shadowInstance.commandBuffer?.Release();
		shadowInstance.planesBuffer?.Release();
		shadowInstance.renderArgsBuffer?.Release();
		shadowInstance.visibleInstancesBuffer?.Release();
	}

	public ShadowInstance CreateShadowInstance(InstancedIndirectDrawCall targetDrawCall, ShadowMapPass pass)
	{
		int shaderPass = targetDrawCall.MaterialPrefab.FindPass("ShadowCaster");
		CommandBuffer commandBuffer = new CommandBuffer();
		commandBuffer.name = "Cull and Render shadow - " + pass.ToString() + targetDrawCall.BaseMesh;
		if (!VisibleInstanceBufferCache.TryGetValue(targetDrawCall, out var value))
		{
			value = new ComputeBuffer(targetDrawCall.InstanceCount, InstancedIndirectDrawCall.MeshPerInstanceDatum.Size, ComputeBufferType.Append);
			VisibleInstanceBufferCache.Add(targetDrawCall, value);
		}
		value.name = "VisibleInstances " + targetDrawCall.BaseMesh;
		Material material = new Material(targetDrawCall.MaterialPrefab);
		material.SetBuffer("_InstanceData", value);
		ComputeBuffer computeBuffer = new ComputeBuffer(6, 16, ComputeBufferType.Structured);
		computeBuffer.name = "Planes " + pass.ToString() + targetDrawCall.BaseMesh;
		ComputeBuffer computeBuffer2 = new ComputeBuffer(1, 20, ComputeBufferType.DrawIndirect);
		computeBuffer2.name = "renderArgsBuffer " + pass.ToString() + targetDrawCall.BaseMesh;
		uint[] data = (uint[])targetDrawCall.Args.Clone();
		computeBuffer2.SetData(data);
		commandBuffer.BeginSample("Extract Frustum");
		commandBuffer.SetComputeBufferParam(ExtractFrustumComputeShader, extractFrustumKernelIndex, "FrustumPlanes", computeBuffer);
		commandBuffer.DispatchCompute(ExtractFrustumComputeShader, extractFrustumKernelIndex, 1, 1, 1);
		commandBuffer.EndSample("Extract Frustum");
		commandBuffer.BeginSample("Culling");
		commandBuffer.SetComputeBufferParam(CullingComputeShader, cullingKernelIndex, AllInstancesPropertyID, targetDrawCall.InstanceDataBuffer);
		commandBuffer.SetComputeBufferParam(CullingComputeShader, cullingKernelIndex, VisibleInstancesPropertyID, value);
		commandBuffer.SetComputeBufferParam(CullingComputeShader, cullingKernelIndex, CullingPlanesPropertyID, computeBuffer);
		commandBuffer.SetComputeIntParam(CullingComputeShader, "LastPlaneIndex", (TargetLight.type == LightType.Directional) ? 3 : 5);
		commandBuffer.SetComputeIntParam(CullingComputeShader, InputInstanceCountPropertyID, targetDrawCall.InstanceCount);
		commandBuffer.SetBufferCounterValue(value, 0u);
		commandBuffer.DispatchCompute(CullingComputeShader, cullingKernelIndex, targetDrawCall.InstanceCount / 1024 + 1, 1, 1);
		commandBuffer.CopyCounterValue(value, computeBuffer2, 4u);
		commandBuffer.EndSample("Culling");
		commandBuffer.BeginSample("Draw");
		commandBuffer.DrawMeshInstancedIndirect(targetDrawCall.BaseMesh, 0, material, shaderPass, computeBuffer2);
		commandBuffer.EndSample("Draw");
		return new ShadowInstance
		{
			commandBuffer = commandBuffer,
			renderArgsBuffer = computeBuffer2,
			visibleInstancesBuffer = value,
			materialInstance = material,
			planesBuffer = computeBuffer
		};
	}

	public static void DrawFrustum(Plane[] frustumPlanes)
	{
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		Plane plane = frustumPlanes[1];
		frustumPlanes[1] = frustumPlanes[2];
		frustumPlanes[2] = plane;
		for (int i = 0; i < 4; i++)
		{
			array[i] = Plane3Intersect(frustumPlanes[4], frustumPlanes[i], frustumPlanes[(i + 1) % 4]);
			array2[i] = Plane3Intersect(frustumPlanes[5], frustumPlanes[i], frustumPlanes[(i + 1) % 4]);
		}
		for (int j = 0; j < 4; j++)
		{
			Debug.DrawLine(array[j], array[(j + 1) % 4], Color.red, Time.deltaTime, depthTest: true);
			Debug.DrawLine(array2[j], array2[(j + 1) % 4], Color.blue, Time.deltaTime, depthTest: true);
			Debug.DrawLine(array[j], array2[j], Color.green, Time.deltaTime, depthTest: true);
		}
	}

	public static Vector3 Plane3Intersect(Plane p1, Plane p2, Plane p3)
	{
		return ((0f - p1.distance) * Vector3.Cross(p2.normal, p3.normal) + (0f - p2.distance) * Vector3.Cross(p3.normal, p1.normal) + (0f - p3.distance) * Vector3.Cross(p1.normal, p2.normal)) / Vector3.Dot(p1.normal, Vector3.Cross(p2.normal, p3.normal));
	}
}
