using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using UnityEngine;
using Weather;

namespace StormVolumes;

public class StormCardMeshController : MonoBehaviour
{
	private readonly struct LightInfo(Vector3 position, Vector3 direction, Color color, float range, float squareDistance)
	{
		public readonly Vector3 Position = position;

		public readonly Vector3 Direction = direction;

		public readonly Color Color = color;

		public readonly float Range = range;

		public readonly float SquareDistance = squareDistance;
	}

	[SerializeField]
	private Material _material;

	[SerializeField]
	private MeshFilter meshFilter;

	[SerializeField]
	private MeshFilter _depthOnlyMeshFilter;

	[SerializeField]
	private MeshRenderer stormCardRenderer;

	public const int MAX_LIGHTS = 8;

	public const int MAX_WEARABLE_LIGHTS = 4;

	public const int MAX_FLARE_LIGHTS = 1;

	public const int LIGHT_DISTANCE = 20;

	public const int LIGHT_SQUARE_DISTANCE = 400;

	public static Vector4[] _lightPositions = new Vector4[8];

	public static Vector4[] _lightcolors = new Vector4[8];

	public static Vector4[] _lightparams = new Vector4[8];

	private static List<LightInfo> LightsInfo = new List<LightInfo>(128);

	private void Update()
	{
		if (WeatherManager.IsWeatherEventRunning && WeatherManager.CurrentWeatherEvent.StormEffect != null)
		{
			UpdateLights();
		}
	}

	public static void UpdateLights()
	{
		LightsInfo.Clear();
		if (!(WorldManager.Instance.WorldSun?.TargetLight))
		{
			return;
		}
		if ((bool)InventoryManager.Parent)
		{
			if (InventoryManager.Parent.AsHuman.HelmetSlot.Contains<IWearableLight>(out var occupant))
			{
				AddLights(occupant.GetAsThing);
			}
			if (InventoryManager.Parent.AsHuman.LeftHandSlot.Contains<IWearableLight>(out var occupant2))
			{
				AddLights(occupant2.GetAsThing);
			}
			if (InventoryManager.Parent.AsHuman.RightHandSlot.Contains<IWearableLight>(out var occupant3))
			{
				AddLights(occupant3.GetAsThing);
			}
		}
		foreach (IWearableLight allIWearableLight in Thing.AllIWearableLights)
		{
			AddLights(allIWearableLight.GetAsThing);
		}
		foreach (ILight allILight in Thing.AllILights)
		{
			AddLights(allILight.GetAsThing);
		}
		Color value = WorldManager.Instance.WorldSun.TargetLight.color * WorldManager.Instance.WorldSun.TargetLight.intensity;
		Shader.SetGlobalColor(StormEffectMaterialController.MainLightColor, value);
		Shader.SetGlobalVector(StormEffectMaterialController.MainLightDir, OrbitalSimulation.WorldSunVector);
		int num = Mathf.Min(8, LightsInfo.Count);
		Shader.SetGlobalInt(StormEffectMaterialController.LightCount, num);
		for (int i = 0; i < num; i++)
		{
			_lightPositions[i] = LightsInfo[i].Position;
			_lightcolors[i] = LightsInfo[i].Color;
			_lightparams[i] = new Vector4(LightsInfo[i].Direction.x, LightsInfo[i].Direction.y, LightsInfo[i].Direction.z, LightsInfo[i].Range);
		}
		Shader.SetGlobalVectorArray(StormEffectMaterialController.LightPositions, _lightPositions);
		Shader.SetGlobalVectorArray(StormEffectMaterialController.LightColors, _lightcolors);
		Shader.SetGlobalVectorArray(StormEffectMaterialController.LightParams, _lightparams);
	}

	private static void AddLights(Thing thing)
	{
		if (!thing.OnOff || !thing.Powered)
		{
			return;
		}
		float num = Vector3.SqrMagnitude(thing.Transform.position - CameraController.CameraPosition);
		if (num > 400f)
		{
			return;
		}
		foreach (ThingLight light in thing.Lights)
		{
			Vector3 position = light.Light.gameObject.transform.position;
			Vector3 direction = ((light.Light.type == LightType.Spot) ? light.Light.gameObject.transform.forward : Vector3.zero);
			Color color = light.Light.color * light.Light.intensity;
			float range = light.Light.range;
			LightInfo item = new LightInfo(position, direction, color, range, num);
			bool flag = false;
			for (int i = 0; i < LightsInfo.Count; i++)
			{
				if (LightsInfo[i].SquareDistance > item.SquareDistance)
				{
					flag = true;
					LightsInfo.Insert(i, item);
					break;
				}
			}
			if (!flag)
			{
				LightsInfo.Add(item);
			}
		}
	}

	public void SetMesh(Mesh mesh)
	{
		meshFilter.sharedMesh = mesh;
		_depthOnlyMeshFilter.sharedMesh = mesh;
	}

	public void ClearMesh()
	{
		Mesh mesh = meshFilter?.sharedMesh;
		if (mesh != null)
		{
			Object.Destroy(mesh);
		}
		SetMesh(null);
	}

	public void ApplySetting(WeatherEvent currentWeatherEvent, Vector3 direction)
	{
		StormEffectMaterialController.ApplySetting(_material, currentWeatherEvent, direction);
	}

	public void SetDensityMult(float value)
	{
		_material.SetFloat(StormEffectMaterialController.DensityMult, value);
	}

	public void SetEmissiveMult(float value)
	{
		_material.SetFloat(StormEffectMaterialController.EmissiveMult, value);
	}

	public void EnableEffects(bool isEnabled)
	{
		stormCardRenderer.enabled = isEnabled;
	}
}
