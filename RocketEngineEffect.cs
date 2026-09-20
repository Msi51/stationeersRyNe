using Assets.Scripts.Util;
using UnityEngine;

public class RocketEngineEffect : GameBase
{
	private const int MAX_QUANTITY = 40;

	public const float COLD_TEMPERATURE = 1273.15f;

	public const float HOT_TEMPERATURE = 3273.15f;

	public const float LOW_FORCE = 0f;

	public const float HIGH_FORCE = 30000f;

	[Header("Animated Variables")]
	[Range(0f, 1f)]
	public float Flicker;

	[Range(1f, 40f)]
	public int Quantity = 40;

	[Range(0f, 40f)]
	public float Distance = 20f;

	[Range(0f, 1f)]
	public float Power;

	[Range(0f, 10f)]
	public float OpacityFalloff = 1f;

	[Range(0f, 1f)]
	public float Efficiency;

	[Header("Resources")]
	public Mesh Mesh;

	public Material Material;

	public Animator Animator;

	public Light Light;

	public LensFlare Flare;

	private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

	private static readonly int PowerProperty = Shader.PropertyToID("_Power");

	private static readonly int RimPowerProperty = Shader.PropertyToID("_RimPower");

	[Header("Settings")]
	public Gradient Color = new Gradient();

	public Gradient RimColor = new Gradient();

	public AnimationCurve DistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public AnimationCurve SizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public AnimationCurve EfficiencyRimCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	private Matrix4x4[] _matrices;

	private float[] _opacities;

	private MaterialPropertyBlock _propertyBlock;

	private static readonly int ColorProperty = Shader.PropertyToID("_Color");

	private static readonly int RimColorProperty = Shader.PropertyToID("_RimColor");

	private Material _material;

	private static readonly int ThrustProperty = Animator.StringToHash("Thrust");

	private static readonly int EfficiencyProperty = Animator.StringToHash("Efficiency");

	private float _temperature;

	private float _thrust;

	public void Start()
	{
		_propertyBlock = new MaterialPropertyBlock();
		_propertyBlock.SetFloatArray(OpacityProperty, new float[40]);
		_material = new Material(Material);
	}

	private void CacheValues()
	{
		float num = Transform.lossyScale.magnitude / 50f;
		_matrices = new Matrix4x4[Quantity];
		_opacities = new float[Quantity];
		float num2 = 1f + Flicker * Random.Range(0f, 1f);
		for (int i = 0; i < Quantity; i++)
		{
			float num3 = (float)i / (float)Quantity;
			Vector3 pos = Transform.position + Transform.forward * (num2 * (DistanceCurve.Evaluate(num3) * Distance * num));
			_matrices[i] = Matrix4x4.TRS(pos, Transform.rotation, Transform.lossyScale * SizeCurve.Evaluate(num3));
			_opacities[i] = Mathf.Pow(1f - num3, OpacityFalloff);
		}
		Color color = RimColor.Evaluate(Efficiency);
		float num4 = RocketMath.MapToScale(0f, 1f, 0.5f, 1f, num2);
		_propertyBlock.SetFloatArray(OpacityProperty, _opacities);
		_material.SetFloat(PowerProperty, Power * num4);
		_material.SetFloat(RimPowerProperty, EfficiencyRimCurve.Evaluate(Efficiency));
		_material.SetColor(ColorProperty, Color.Evaluate(Efficiency));
		_material.SetColor(RimColorProperty, color);
		Light.color = color;
		Flare.color = color;
		Light.range = 1f * Power * num4;
		Light.intensity = 4f * Power * num4;
	}

	public void UpdateEachFrame()
	{
		if (_propertyBlock != null && (object)_material != null)
		{
			CacheValues();
			Graphics.DrawMeshInstanced(Mesh, 0, _material, _matrices, Quantity, _propertyBlock);
		}
	}

	public void SetRunning(bool isRunning)
	{
		base.enabled = isRunning;
		Light.enabled = isRunning;
		Animator.enabled = isRunning;
		Flare.enabled = isRunning;
	}

	public void SetTemperature(float temperature)
	{
		_temperature = Mathf.Lerp(_temperature, temperature, Time.deltaTime);
		Animator.SetFloat(EfficiencyProperty, RocketMath.MapToScaleClamp(0f, 30000f, 0f, 1f, _temperature));
	}

	public void SetThrust(float currentThrust)
	{
		_thrust = Mathf.Lerp(_thrust, currentThrust, Time.deltaTime);
		Animator.SetFloat(ThrustProperty, _thrust);
	}
}
