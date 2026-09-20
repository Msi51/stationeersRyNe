using StormVolumes;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class StormPostEffect : MonoBehaviour
{
	[SerializeField]
	private Material _material;

	[SerializeField]
	private Material _meshMaterial;

	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private Camera _stormCardCamera;

	[SerializeField]
	private Texture3D[] _noiseTextures;

	private RenderTexture _stormCardDepthRT;

	private Vector2Int _screenResolution;

	private void Awake()
	{
		_screenResolution = new Vector2Int(Screen.width, Screen.height);
		SetupRenderTexture();
	}

	private void SetupRenderTexture()
	{
		int width = _screenResolution.x / 4;
		int height = _screenResolution.y / 4;
		if (_stormCardDepthRT != null)
		{
			_stormCardDepthRT.Release();
			Object.Destroy(_stormCardDepthRT);
		}
		_stormCardDepthRT = new RenderTexture(width, height, GraphicsFormat.R32G32_SFloat, GraphicsFormat.D16_UNorm);
		_stormCardDepthRT.width = width;
		_stormCardDepthRT.height = height;
		_stormCardDepthRT.Create();
		_stormCardCamera.targetTexture = _stormCardDepthRT;
	}

	private void Update()
	{
		if (!Mathf.Approximately(_camera.fieldOfView, _stormCardCamera.fieldOfView))
		{
			_stormCardCamera.fieldOfView = _camera.fieldOfView;
		}
		if (_screenResolution.x != Screen.width || _screenResolution.y != Screen.height)
		{
			_screenResolution.x = Screen.width;
			_screenResolution.y = Screen.height;
			SetupRenderTexture();
		}
		_material.SetTexture("_StormCardDepth", _stormCardDepthRT);
	}

	private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
	{
		Graphics.Blit(sourceTexture, destTexture, _material);
	}

	private void OnDestroy()
	{
		if (_stormCardDepthRT != null)
		{
			_stormCardDepthRT.Release();
			Object.Destroy(_stormCardDepthRT);
		}
	}

	public void ApplySetting(WeatherEvent currentWeatherEvent, Vector3 direction)
	{
		StormEffectMaterialController.ApplySetting(_material, currentWeatherEvent, direction);
		if (_meshMaterial != null)
		{
			StormEffectMaterialController.ApplySetting(_meshMaterial, currentWeatherEvent, direction);
		}
		if (currentWeatherEvent?.StormEffect != null && _noiseTextures != null && _noiseTextures.Length != 0)
		{
			int num = Mathf.Clamp(currentWeatherEvent.StormEffect.TextureIndex, 0, _noiseTextures.Length - 1);
			Texture3D noiseTexture = _noiseTextures[num];
			StormEffectMaterialController.ApplyNoiseTexture(_material, noiseTexture);
			if (_meshMaterial != null)
			{
				StormEffectMaterialController.ApplyNoiseTexture(_meshMaterial, noiseTexture);
			}
		}
	}

	public void SetDensityMult(float value)
	{
		_material.SetFloat(StormEffectMaterialController.DensityMult, value);
	}

	public void SetEmissiveMult(float value)
	{
		_material.SetFloat(StormEffectMaterialController.EmissiveMult, value);
	}
}
