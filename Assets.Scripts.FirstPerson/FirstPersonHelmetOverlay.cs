using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;
using Weather;

namespace Assets.Scripts.FirstPerson;

[DefaultExecutionOrder(1000)]
public class FirstPersonHelmetOverlay : ManagerBase
{
	public static FirstPersonHelmetOverlay Instance;

	public static FirstPersonHelmet CurrentEquippedHelmet;

	private Transform _transform;

	public static int FirstPersonLayer = 26;

	private OpenSimplexNoise helmetShakeSimplexNoise = new OpenSimplexNoise();

	[Tooltip("How quickly the helmet shake changes direction when a storm is running")]
	public float helmetFlickerFrequency = 10f;

	[Tooltip("How much the helmet shakes by when a storm is running")]
	public float helmetFlickerIntensity = 0.01f;

	public static float CurrentFrostSetting { get; set; }

	public static bool IsEnabled
	{
		get
		{
			if (Settings.CurrentData.HelmetOverlay && CurrentEquippedHelmet != null)
			{
				return CurrentEquippedHelmet.Helmet != null;
			}
			return false;
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (!GameManager.IsBatchMode && !Instance)
		{
			Instance = this;
			_transform = GetComponent<Transform>();
		}
	}

	public void LateUpdate()
	{
		if (!GameManager.IsBatchMode && IsEnabled)
		{
			SetAlpha(CurrentFrostSetting);
			_ = Vector3.zero;
			if (WeatherManager.CurrentEventAffects(InventoryManager.ParentPosition.y) && CurrentEquippedHelmet != null && CurrentEquippedHelmet.HelmetSelf != null && CurrentEquippedHelmet.HelmetSelf.CanBeExposedToStorm())
			{
				float x = helmetShakeSimplexNoise.Evaluate(0f, Time.time * helmetFlickerFrequency) * helmetFlickerIntensity;
				float y = helmetShakeSimplexNoise.Evaluate(100f, Time.time * helmetFlickerFrequency) * helmetFlickerIntensity;
				float z = helmetShakeSimplexNoise.Evaluate(200f, Time.time * helmetFlickerFrequency) * helmetFlickerIntensity;
				new Vector3(x, y, z);
			}
		}
	}

	public void OnWorldExit()
	{
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
		CurrentEquippedHelmet = null;
	}

	public void OnHelmetSlotChange(GasMask helmet)
	{
		if (!helmet || (object)helmet.FirstPersonHelmet?.HelmetSelf != null)
		{
			if ((object)helmet == null)
			{
				RemoveHelmet();
			}
			else
			{
				EquippedHelmet(helmet);
			}
		}
	}

	public void RemoveHelmet()
	{
		if (CurrentEquippedHelmet != null && !(CurrentEquippedHelmet.Helmet == null) && !(CurrentEquippedHelmet.HelmetSelf == null) && !CurrentEquippedHelmet.HelmetSelf.BeingDestroyed)
		{
			CurrentEquippedHelmet.Helmet.transform.parent = CurrentEquippedHelmet.HelmetSelf.ThingTransform;
			CurrentEquippedHelmet.Helmet.SetActive(value: false);
			CurrentEquippedHelmet = null;
		}
	}

	public void DestroyCurrentHelmet()
	{
		if (CurrentEquippedHelmet != null)
		{
			if (CurrentEquippedHelmet.Helmet != null)
			{
				Object.Destroy(CurrentEquippedHelmet.Helmet);
			}
			CurrentEquippedHelmet = null;
		}
	}

	public void EquippedHelmet(GasMask helmet)
	{
		if (!(helmet?.FirstPersonHelmet?.Helmet == null) && !CameraController.Instance.ThirdController.enabled && !object.Equals(helmet.FirstPersonHelmet, CurrentEquippedHelmet))
		{
			CurrentEquippedHelmet = helmet.FirstPersonHelmet;
			if (!CurrentEquippedHelmet.HelmetSelf.BeingDestroyed)
			{
				CurrentEquippedHelmet.Transform.parent = _transform;
				CurrentEquippedHelmet.Transform.localPosition = CurrentEquippedHelmet.OffsetPosition;
				CurrentEquippedHelmet.Transform.localEulerAngles = CurrentEquippedHelmet.OffsetRotation;
				UpdateHelmetScale();
				SetHelmetLayers(FirstPersonLayer);
				CurrentEquippedHelmet.Helmet.SetActive(value: true);
			}
		}
	}

	public void HideCurrentHelmet()
	{
		if (CurrentEquippedHelmet != null && CurrentEquippedHelmet.Helmet != null)
		{
			CurrentEquippedHelmet.Helmet.SetActive(value: false);
		}
	}

	public void ShowCurrentHelmet()
	{
		if (CurrentEquippedHelmet != null && CurrentEquippedHelmet.Helmet != null)
		{
			CurrentEquippedHelmet.Helmet.SetActive(value: true);
		}
	}

	public void UpdateHelmetScale()
	{
		if (IsEnabled)
		{
			float num = Settings.GetSlider(SettingType.FieldOfView).maxValue - Settings.GetSlider(SettingType.FieldOfView).minValue;
			float num2 = CurrentEquippedHelmet.MaximumFieldOfViewScale - CurrentEquippedHelmet.MinimumFieldOfViewScale;
			float num3 = ((float)Settings.CurrentData.FieldOfView - Settings.GetSlider(SettingType.FieldOfView).minValue) * num2 / num + CurrentEquippedHelmet.MinimumFieldOfViewScale;
			CurrentEquippedHelmet.Transform.localScale = new Vector3(CurrentEquippedHelmet.Scale.x * num3, CurrentEquippedHelmet.Scale.y * num3, CurrentEquippedHelmet.Scale.z);
		}
	}

	public static void SetAllLayers(Transform transform, int layerId)
	{
		Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = layerId;
		}
	}

	public static void SetHelmetLayers(int layerID)
	{
		if (IsEnabled)
		{
			CurrentEquippedHelmet.Helmet.gameObject.layer = layerID;
			SetAllLayers(CurrentEquippedHelmet.Helmet.transform, layerID);
		}
	}

	private void SetAlpha(float alpha)
	{
		if (IsEnabled)
		{
			Material[] frostMaterials = CurrentEquippedHelmet.FrostMaterials;
			foreach (Material obj in frostMaterials)
			{
				Color color = obj.color;
				color.a = Mathf.Lerp(color.a, Mathf.Clamp(alpha, 0f, 1f), CurrentEquippedHelmet.FrostFadeSpeed);
				obj.color = color;
			}
		}
	}
}
