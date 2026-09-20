using UnityEngine;

public class AnimatedMaterialCelestialPrefab : CelestialPrefab
{
	public Material Material;

	[Range(0f, 10f)]
	public float Scale = 1f;

	private static readonly int Speed = Shader.PropertyToID("_Speed");

	public override void Start()
	{
		base.Start();
		Material?.SetFloat(Speed, (float)(OrbitalSimulation.GetTimeScale() * (double)Scale));
	}

	public override void OnTimeScaleChanged(double timeScale)
	{
		base.OnTimeScaleChanged(timeScale);
		Material?.SetFloat(Speed, (float)(timeScale * (double)Scale));
	}
}
