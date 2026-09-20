using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using DG.Tweening;
using UnityEngine;

public class PowerTransmitterVisualiser : MonoBehaviour
{
	public enum Direction
	{
		In,
		Out
	}

	[SerializeField]
	private LineRenderer _lineRenderer;

	private static readonly int COLOR = Shader.PropertyToID("_Color");

	private static readonly int WAVESPEED = Shader.PropertyToID("_WaveSpeed");

	[SerializeField]
	private Color InnerColor = Color.white;

	[ColorUsage(false, true)]
	[SerializeField]
	private Color EmissionColor = Color.red * 10f;

	public void Activate()
	{
		InnerColor.a = 1f;
		EmissionColor.a = 1f;
		_lineRenderer.material.DOColor(InnerColor, COLOR, 0f);
		_lineRenderer.material.DOColor(EmissionColor, Thing.EMISSION_COLOR, 0f);
	}

	public void Deactivate()
	{
		InnerColor.a = 0f;
		EmissionColor.a = 0f;
		_lineRenderer.material.DOColor(InnerColor, COLOR, 0f);
		_lineRenderer.material.DOColor(EmissionColor, Thing.EMISSION_COLOR, 0f);
	}

	public void SetDirection(Direction direction)
	{
		int num = ((direction != Direction.In) ? 1 : (-1));
		_lineRenderer.material.SetFloat(WAVESPEED, num);
	}

	public void SetIntensity(float intensity)
	{
		if (ThreadedManager.IsThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(delegate
			{
				SetMaterialPropertiesForIntensity(intensity);
			});
		}
		else
		{
			SetMaterialPropertiesForIntensity(intensity);
		}
	}

	private void SetMaterialPropertiesForIntensity(float intensity)
	{
		intensity = Mathf.Clamp01(intensity);
		InnerColor.a = intensity;
		EmissionColor.a = intensity;
		_lineRenderer.material.DOColor(InnerColor, COLOR, 1f);
		_lineRenderer.material.DOColor(EmissionColor, Thing.EMISSION_COLOR, 1f);
	}
}
