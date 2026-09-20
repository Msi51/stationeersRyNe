using UnityEngine;

namespace Assets.Scripts.Objects;

public struct FlameParticleData(Color color, float size, Vector3 scale, int numberOfEmitters)
{
	public Color Color = color;

	public float Size = size;

	public Vector3 Scale = scale;

	public int NumberOfEmitters = numberOfEmitters;
}
