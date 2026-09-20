using System;
using ch.sycoforge.Flares;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class ThingLight
{
	public Thing Thing;

	public Light Light;

	public VolumetricLight VolumetricLight;

	public EasyFlares Flare;

	public float Range;

	public LayerMask LayerMask;

	public static LayerMask BlankMask;

	private bool _showEffects;

	private bool _isTurnedOn;

	private bool IsRendered => !Thing.IsOccluded;

	private bool ShowEffects
	{
		get
		{
			if (IsRendered && _isTurnedOn && _showEffects)
			{
				return Light.enabled;
			}
			return false;
		}
	}

	public ThingLight(Light light, Thing thing)
	{
		Thing = thing;
		Light = light;
		VolumetricLight = light.GetComponent<VolumetricLight>();
		Flare = light.GetComponent<EasyFlares>();
		Range = light.range;
		LayerMask = light.cullingMask;
	}

	public void SetVisible(bool lightVisible, bool effectsVisible)
	{
		_isTurnedOn = lightVisible;
		_showEffects = effectsVisible;
		Refresh();
	}

	public void Refresh()
	{
		if (Light != null)
		{
			Light.cullingMask = (IsRendered ? LayerMask : BlankMask);
		}
		if (VolumetricLight != null)
		{
			VolumetricLight.enabled = ShowEffects;
		}
		if (Flare != null)
		{
			Flare.enabled = ShowEffects;
		}
	}
}
