using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

public class AtmosphericScatteringBlend
{
	public readonly List<GradientColorKey> RayleighColorRampColorKey;

	public readonly List<GradientAlphaKey> RayleighColorRampAlphaKey = new List<GradientAlphaKey>
	{
		new GradientAlphaKey(1f, 0f),
		new GradientAlphaKey(1f, 1f)
	};

	public readonly List<GradientColorKey> MieColorRampColorKey;

	public readonly List<GradientAlphaKey> MieColorRampAlphaKey = new List<GradientAlphaKey>
	{
		new GradientAlphaKey(1f, 0f),
		new GradientAlphaKey(1f, 1f)
	};

	public readonly Color HeightRayleighColor;

	public readonly float WorldRayleighDensity;

	public readonly float WorldMieDensity;

	public readonly float HeightRayleighDensity;

	public readonly float HeightMieDensity;

	public readonly float AtmosphereThickness;

	public AtmosphericScatteringBlend(WorldSettingData defaultData, Atmosphere globalAtmosphere)
	{
		AtmosphericScatteringData atmosphericScatteringData = defaultData.AtmosphericScatteringData;
		if (atmosphericScatteringData == null)
		{
			atmosphericScatteringData = new AtmosphericScatteringData();
		}
		MoleQuantity moleQuantity = ((defaultData.GlobalAtmosphereData.TotalMolesGasses() > MoleQuantity.Zero) ? defaultData.GlobalAtmosphereData.TotalMolesGasses() : new MoleQuantity(40.0));
		if (globalAtmosphere.TotalMolesGases < moleQuantity)
		{
			WorldRayleighDensity = RocketMath.MapToScale(0f, moleQuantity.ToFloat(), 0f, atmosphericScatteringData.WorldRayleighDensity, globalAtmosphere.TotalMolesGases.ToFloat());
			WorldMieDensity = RocketMath.MapToScale(0f, moleQuantity.ToFloat(), 0f, atmosphericScatteringData.WorldMieDensity, globalAtmosphere.TotalMolesGases.ToFloat());
			HeightRayleighDensity = RocketMath.MapToScale(0f, moleQuantity.ToFloat(), 0f, atmosphericScatteringData.HeightRayleighDensity, globalAtmosphere.TotalMolesGases.ToFloat());
			HeightMieDensity = RocketMath.MapToScale(0f, moleQuantity.ToFloat(), 0f, atmosphericScatteringData.HeightMieDensity, globalAtmosphere.TotalMolesGases.ToFloat());
			RayleighColorRampAlphaKey = new List<GradientAlphaKey>
			{
				new GradientAlphaKey(Mathf.Clamp01((globalAtmosphere.TotalMolesGases / moleQuantity).ToFloat()), 0f),
				new GradientAlphaKey(Mathf.Clamp01((globalAtmosphere.TotalMolesGases / moleQuantity).ToFloat()), 1f)
			};
			MieColorRampAlphaKey = RayleighColorRampAlphaKey;
		}
		else if (globalAtmosphere.TotalMolesGases > moleQuantity)
		{
			WorldRayleighDensity = RocketMath.MapToScale(moleQuantity.ToFloat(), moleQuantity.ToFloat() * 10f, atmosphericScatteringData.WorldRayleighDensity, atmosphericScatteringData.WorldRayleighDensity * 10f, globalAtmosphere.TotalMolesGases.ToFloat());
			WorldMieDensity = RocketMath.MapToScale(moleQuantity.ToFloat(), moleQuantity.ToFloat() * 10f, atmosphericScatteringData.WorldMieDensity, atmosphericScatteringData.WorldMieDensity * 10f, globalAtmosphere.TotalMolesGases.ToFloat());
			HeightRayleighDensity = RocketMath.MapToScale(moleQuantity.ToFloat(), moleQuantity.ToFloat() * 10f, atmosphericScatteringData.HeightRayleighDensity, atmosphericScatteringData.HeightRayleighDensity * 10f, globalAtmosphere.TotalMolesGases.ToFloat());
			HeightMieDensity = RocketMath.MapToScale(moleQuantity.ToFloat(), moleQuantity.ToFloat() * 10f, atmosphericScatteringData.HeightMieDensity, atmosphericScatteringData.HeightMieDensity * 10f, globalAtmosphere.TotalMolesGases.ToFloat());
		}
		else
		{
			WorldRayleighDensity = atmosphericScatteringData.WorldRayleighDensity;
			WorldMieDensity = atmosphericScatteringData.WorldMieDensity;
			HeightRayleighDensity = atmosphericScatteringData.HeightRayleighDensity;
			HeightMieDensity = atmosphericScatteringData.HeightMieDensity;
		}
		AtmosphereThickness = RocketMath.MapToScale(0f, moleQuantity.ToFloat(), 0f, AtmosphericScattering.DefaultAtmosphereThickness, globalAtmosphere.TotalMolesGases.ToFloat());
		List<Tuple<AtmosphericScatteringBlendData, float>> list = new List<Tuple<AtmosphericScatteringBlendData, float>>();
		foreach (AtmosphericScatteringBlendData allBlend in AtmosphericScatteringBlendData.AllBlends)
		{
			list.Add(new Tuple<AtmosphericScatteringBlendData, float>(allBlend, allBlend.GetWeight(globalAtmosphere)));
		}
		List<Tuple<Color, float>> list2 = new List<Tuple<Color, float>>();
		List<Tuple<Color, float>> list3 = new List<Tuple<Color, float>>();
		AtmosphericScatteringBlendData item;
		float item2;
		foreach (Tuple<AtmosphericScatteringBlendData, float> item6 in list)
		{
			item6.Deconstruct(out item, out item2);
			AtmosphericScatteringBlendData atmosphericScatteringBlendData = item;
			float item3 = item2;
			list2.Add(new Tuple<Color, float>(atmosphericScatteringBlendData.RayleighColorRampColorKey[0].color, item3));
			list3.Add(new Tuple<Color, float>(atmosphericScatteringBlendData.RayleighColorRampColorKey[1].color, item3));
		}
		RayleighColorRampColorKey = new List<GradientColorKey>
		{
			new GradientColorKey(BlendColors(list2), 0f),
			new GradientColorKey(BlendColors(list3), 1f)
		};
		list2.Clear();
		list3.Clear();
		foreach (Tuple<AtmosphericScatteringBlendData, float> item7 in list)
		{
			item7.Deconstruct(out item, out item2);
			AtmosphericScatteringBlendData atmosphericScatteringBlendData2 = item;
			float item4 = item2;
			list2.Add(new Tuple<Color, float>(atmosphericScatteringBlendData2.MieColorRampColorKey[0].color, item4));
			list3.Add(new Tuple<Color, float>(atmosphericScatteringBlendData2.MieColorRampColorKey[1].color, item4));
		}
		MieColorRampColorKey = new List<GradientColorKey>
		{
			new GradientColorKey(BlendColors(list2), 0f),
			new GradientColorKey(BlendColors(list3), 1f)
		};
		list2.Clear();
		foreach (Tuple<AtmosphericScatteringBlendData, float> item8 in list)
		{
			item8.Deconstruct(out item, out item2);
			AtmosphericScatteringBlendData atmosphericScatteringBlendData3 = item;
			float item5 = item2;
			list2.Add(new Tuple<Color, float>(atmosphericScatteringBlendData3.HeightRayleighColor, item5));
		}
		HeightRayleighColor = BlendColors(list2);
	}

	public static Color BlendColors(List<Tuple<Color, float>> weightedColors)
	{
		float num = 0f;
		foreach (Tuple<Color, float> weightedColor in weightedColors)
		{
			num += weightedColor.Item2;
		}
		if (num <= 0f)
		{
			return Color.white;
		}
		for (int i = 0; i < weightedColors.Count; i++)
		{
			weightedColors[i] = new Tuple<Color, float>(weightedColors[i].Item1, weightedColors[i].Item2 / num);
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		foreach (Tuple<Color, float> weightedColor2 in weightedColors)
		{
			num2 += weightedColor2.Item1.r * weightedColor2.Item2;
			num3 += weightedColor2.Item1.g * weightedColor2.Item2;
			num4 += weightedColor2.Item1.b * weightedColor2.Item2;
		}
		return new Color(num2, num3, num4);
	}
}
