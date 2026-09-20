using UnityEngine;
using UnityEngine.Animations;

namespace Assets.Scripts.Objects;

public class Thruster : GameBase
{
	[Header("Thruster")]
	[Range(0f, 20f)]
	public float VisualScale = 1f;

	public Axis Axis = Axis.Y;

	public Light Light;

	[Range(0f, 20f)]
	public float LightIntensity = 1f;

	public void UpdateScale(Vector3 throttleDir, float throttleMagnitude)
	{
		Transform transform = Transform;
		float b = Vector3.Dot(Axis switch
		{
			Axis.X => Transform.right, 
			Axis.Y => Transform.up, 
			Axis.Z => Transform.forward, 
			_ => Transform.up, 
		}, throttleDir);
		float value = Mathf.Max(0f, b) * throttleMagnitude;
		value = Mathf.Clamp(value, 0.01f, 0.99f);
		value += Random.Range(-0.01f, 0.01f);
		Vector3 localScale = transform.localScale;
		switch (Axis)
		{
		case Axis.X:
			localScale.x = value * VisualScale;
			break;
		case Axis.Y:
			localScale.y = value * VisualScale;
			break;
		case Axis.Z:
			localScale.z = value * VisualScale;
			break;
		}
		transform.localScale = localScale;
		Light.intensity = Mathf.Lerp(0f, LightIntensity, value);
	}

	public void OnEditorValidate()
	{
		Light = GetComponent<Light>();
	}
}
