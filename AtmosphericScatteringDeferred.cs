using System;
using Assets.Scripts;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
public class AtmosphericScatteringDeferred : PostEffectsBase
{
	[HideInInspector]
	public Shader deferredFogShader;

	private Material m_fogMaterial;

	private static readonly int FrustumCornersWs = Shader.PropertyToID("_FrustumCornersWS");

	private static readonly int CameraWs = Shader.PropertyToID("_CameraWS");

	private static readonly int MainTex = Shader.PropertyToID("_MainTex");

	public override bool CheckResources()
	{
		CheckSupport(needDepth: true);
		if (!deferredFogShader)
		{
			deferredFogShader = Shader.Find("Hidden/AtmosphericScattering_Deferred");
		}
		m_fogMaterial = CheckShaderAndCreateMaterial(deferredFogShader, m_fogMaterial);
		if (!isSupported)
		{
			ReportAutoDisable();
		}
		return isSupported;
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Camera currentCamera = CameraController.CurrentCamera;
		if (!isSupported || ((!currentCamera || currentCamera.actualRenderingPath != RenderingPath.DeferredShading) && (!AtmosphericScattering.instance || !AtmosphericScattering.instance.forcePostEffect)))
		{
			Graphics.Blit(source, destination);
			return;
		}
		Transform obj = currentCamera.transform;
		float nearClipPlane = currentCamera.nearClipPlane;
		float farClipPlane = currentCamera.farClipPlane;
		float fieldOfView = currentCamera.fieldOfView;
		float aspect = currentCamera.aspect;
		Matrix4x4 identity = Matrix4x4.identity;
		float num = fieldOfView * 0.5f;
		Vector3 vector = obj.right * nearClipPlane * Mathf.Tan(num * (MathF.PI / 180f)) * aspect;
		Vector3 vector2 = obj.up * nearClipPlane * Mathf.Tan(num * (MathF.PI / 180f));
		Vector3 vector3 = obj.forward * nearClipPlane - vector + vector2;
		float num2 = vector3.magnitude * farClipPlane / nearClipPlane;
		vector3.Normalize();
		vector3 *= num2;
		Vector3 vector4 = obj.forward * nearClipPlane + vector + vector2;
		vector4.Normalize();
		vector4 *= num2;
		Vector3 vector5 = obj.forward * nearClipPlane + vector - vector2;
		vector5.Normalize();
		vector5 *= num2;
		Vector3 vector6 = obj.forward * nearClipPlane - vector - vector2;
		vector6.Normalize();
		vector6 *= num2;
		identity.SetRow(0, vector3);
		identity.SetRow(1, vector4);
		identity.SetRow(2, vector5);
		identity.SetRow(3, vector6);
		Vector3 position = obj.position;
		m_fogMaterial.SetMatrix(FrustumCornersWs, identity);
		m_fogMaterial.SetVector(CameraWs, position);
		CustomGraphicsBlit(source, destination, m_fogMaterial, 0);
	}

	private static void CustomGraphicsBlit(RenderTexture src, RenderTexture dst, Material mat, int pass)
	{
		RenderTexture.active = dst;
		mat.SetTexture(MainTex, src);
		GL.PushMatrix();
		GL.LoadOrtho();
		mat.SetPass(pass);
		GL.Begin(7);
		GL.MultiTexCoord2(0, 0f, 0f);
		GL.Vertex3(0f, 0f, 3f);
		GL.MultiTexCoord2(0, 1f, 0f);
		GL.Vertex3(1f, 0f, 2f);
		GL.MultiTexCoord2(0, 1f, 1f);
		GL.Vertex3(1f, 1f, 1f);
		GL.MultiTexCoord2(0, 0f, 1f);
		GL.Vertex3(0f, 1f, 0f);
		GL.End();
		GL.PopMatrix();
	}
}
