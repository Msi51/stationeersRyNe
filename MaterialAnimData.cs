using System;
using Assets.Scripts.Util;
using UnityEngine;

[Serializable]
public class MaterialAnimData
{
	public MeshRenderer Renderer;

	public Vector2 mainTextureScale = new Vector2(1f, 1f);

	public Vector2 emissionMapScale = new Vector2(1f, 1f);

	private static readonly int _MainTex = Shader.PropertyToID("_MainTex");

	private static readonly int _EmissionMap = Shader.PropertyToID("_EmissionMap");

	public void Apply()
	{
		Renderer.material.SetTextureScale(_MainTex, mainTextureScale);
		Renderer.material.SetTextureScale(_EmissionMap, emissionMapScale);
	}

	public bool MoveTowards(float speed)
	{
		Vector2 textureScale = Renderer.material.GetTextureScale(_MainTex);
		Vector2 textureScale2 = Renderer.material.GetTextureScale(_EmissionMap);
		Vector2 vector = Vector2.MoveTowards(textureScale, mainTextureScale, Time.deltaTime * speed);
		Vector2 vector2 = Vector2.MoveTowards(textureScale2, emissionMapScale, Time.deltaTime * speed);
		if (RocketMath.Approximately(textureScale, vector, 0.0005f) && RocketMath.Approximately(textureScale2, vector2, 0.0005f))
		{
			Apply();
			return true;
		}
		Renderer.material.SetTextureScale(_MainTex, vector);
		Renderer.material.SetTextureScale(_EmissionMap, vector2);
		return false;
	}

	public void Lerp(MaterialAnimData previous, float t)
	{
		t = Mathf.Clamp01(t);
		Renderer.material.SetTextureScale(_MainTex, Vector2.Lerp(previous.mainTextureScale, mainTextureScale, t));
		Renderer.material.SetTextureScale(_EmissionMap, Vector2.Lerp(previous.emissionMapScale, emissionMapScale, t));
	}
}
