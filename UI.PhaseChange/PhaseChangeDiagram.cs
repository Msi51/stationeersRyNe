using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.PhaseChange;

public class PhaseChangeDiagram : MonoBehaviour
{
	private class GasGraph
	{
		public readonly Mole mole;

		public readonly Chemistry.GasType gasType;

		private readonly List<Vector2> regularCurve;

		private readonly List<Vector2> logCurve;

		private readonly (Vector2, Vector2) regularFreeze;

		private readonly (Vector2, Vector2) logFreeze;

		private readonly Vector2 regularMaxLiquidTemp;

		private readonly Vector2 logMaxLiquidTemp;

		private readonly int maxPressure;

		private readonly int maxPressureLog;

		public List<Vector2> Curve(bool log)
		{
			if (!log)
			{
				return regularCurve;
			}
			return logCurve;
		}

		public (Vector2, Vector2) Freeze(bool log)
		{
			if (!log)
			{
				return regularFreeze;
			}
			return logFreeze;
		}

		public Vector2 MaxLiquidTemp(bool log)
		{
			if (!log)
			{
				return regularMaxLiquidTemp;
			}
			return logMaxLiquidTemp;
		}

		public int MaxPressure(bool log)
		{
			if (!log)
			{
				return maxPressure;
			}
			return maxPressureLog;
		}

		public GasGraph(Mole mole, TemperatureKelvin maxTemp, int resolution, Func<Vector2, Vector2> pointScaler)
		{
			this.mole = mole;
			gasType = mole.Type;
			maxPressure = Mathf.CeilToInt(mole.MinimumLiquidPressureAtMaxTemperature().ToFloat() + 1000f);
			regularCurve = RenderGraphCurve(mole, maxTemp, useLog: false, resolution, pointScaler);
			regularFreeze = RenderFreezeLine(mole, maxTemp, useLog: false, pointScaler);
			regularMaxLiquidTemp = RenderMaxLiquidTempPoint(mole, maxTemp, useLog: false, pointScaler);
			maxPressureLog = Mathf.CeilToInt(Mathf.Pow(10f, (float)Math.Log10(60000.0)));
			logCurve = RenderGraphCurve(mole, maxTemp, useLog: true, resolution, pointScaler);
			logFreeze = RenderFreezeLine(mole, maxTemp, useLog: true, pointScaler);
			logMaxLiquidTemp = RenderMaxLiquidTempPoint(mole, maxTemp, useLog: true, pointScaler);
		}

		private static List<Vector2> RenderGraphCurve(Mole mole, TemperatureKelvin maxTemperature, bool useLog, int resolution, Func<Vector2, Vector2> scalePoint)
		{
			List<Vector2> list = new List<Vector2>(resolution);
			int num = 0;
			float num2 = 0f;
			for (int i = 0; i <= resolution; i++)
			{
				float num3 = (float)i / (float)(resolution - 1);
				double num4 = CalculateY(num3, mole, maxTemperature, useLog);
				if (num4 >= 1.0)
				{
					i++;
					num3 = num2;
					num++;
				}
				if (num > 1)
				{
					break;
				}
				num2 = num3;
				num4 = Mathf.Clamp01((float)num4);
				list.Add(scalePoint(new Vector2(num3, (float)num4)));
			}
			return list;
		}

		private static (Vector2, Vector2) RenderFreezeLine(Mole mole, TemperatureKelvin maxTemp, bool useLog, Func<Vector2, Vector2> pointScaler)
		{
			TemperatureKelvin temperatureKelvin = mole.FreezingTemperature() / maxTemp;
			double num = CalculateY(temperatureKelvin.ToFloat(), mole, maxTemp, useLog);
			return (pointScaler(new Vector2(temperatureKelvin.ToFloat(), (float)num)), pointScaler(new Vector2(temperatureKelvin.ToFloat(), 1f)));
		}

		private static Vector2 RenderMaxLiquidTempPoint(Mole mole, TemperatureKelvin maxTemp, bool useLog, Func<Vector2, Vector2> pointScaler)
		{
			TemperatureKelvin temperatureKelvin = mole.MaxLiquidTemperature() / maxTemp;
			double num = CalculateY(temperatureKelvin.ToFloat(), mole, maxTemp, useLog);
			return pointScaler(new Vector2(temperatureKelvin.ToFloat(), (float)num));
		}
	}

	[SerializeField]
	private TMP_Text temperature;

	[SerializeField]
	private TMP_Text pressure;

	[SerializeField]
	private RectTransform dotRectTransform;

	[SerializeField]
	private UILineRenderer uiLineRenderer;

	[FormerlySerializedAs("PressureMaxText")]
	[SerializeField]
	private TMP_Text pressureMaxText;

	[FormerlySerializedAs("PressureMinText")]
	[SerializeField]
	private TMP_Text pressureMinText;

	[FormerlySerializedAs("TempMaxText")]
	[SerializeField]
	private TMP_Text tempMaxText;

	[FormerlySerializedAs("TempFreezeText")]
	[SerializeField]
	private TMP_Text tempFreezeText;

	[FormerlySerializedAs("TempMinText")]
	[SerializeField]
	private TMP_Text tempMinText;

	[SerializeField]
	private TMP_Text solidText;

	[SerializeField]
	private TMP_Text liquidText;

	[SerializeField]
	private TMP_Text gasText;

	[FormerlySerializedAs("MaxGraphTemperature")]
	[SerializeField]
	private int maxGraphTemperature = 700;

	[SerializeField]
	private float xMargin = 0.1f;

	[SerializeField]
	private float yMargin = 0.15f;

	[FormerlySerializedAs("GasType")]
	[SerializeField]
	private Chemistry.GasType gasType;

	[SerializeField]
	private Chemistry.GasType comparisonGasType;

	[FormerlySerializedAs("UseLog")]
	public bool useLog = true;

	[FormerlySerializedAs("UseCelsius")]
	public bool useCelsius;

	[FormerlySerializedAs("_diagramRectTransform")]
	[SerializeField]
	private RectTransform diagramRectTransform;

	[SerializeField]
	private TMP_Dropdown comparisonDropdown;

	private const int resolution = 100;

	private static Dictionary<Chemistry.GasType, GasGraph> graphs;

	private static readonly Color GraphColor = Color.black;

	private static readonly Color GuideColor = Color.gray;

	private static readonly Color CrosshairColor = Color.gray;

	private static readonly Color FreezeColor = Color.blue;

	private const float GraphThickness = 3f;

	private const float GuideThickness = 2f;

	private const float CrosshairThickness = 2f;

	private RectTransform crosshairHorizontalTransform;

	private RectTransform crosshairVerticalTransform;

	private Vector2 GraphArea => new Vector2(DiagramSize.x - ((1f - Margins.x * 2f) * 2f + DiagramSize.x * Margins.x * 2f), DiagramSize.y - ((1f - Margins.y * 2f) * 2f + DiagramSize.y * Margins.y * 2f));

	private Vector2 Margins => new Vector2(xMargin, yMargin);

	private Vector2 DiagramSize => diagramRectTransform.rect.size;

	private void GenerateData()
	{
		if (graphs != null)
		{
			return;
		}
		comparisonDropdown.options.Clear();
		comparisonDropdown.options.Add(new TMP_Dropdown.OptionData("None"));
		graphs = new Dictionary<Chemistry.GasType, GasGraph>(Chemistry.Gases.Count);
		Func<Vector2, Vector2> pointScaler = PlotPointOnGraph;
		foreach (Mole gase in Chemistry.Gases)
		{
			comparisonDropdown.options.Add(new TMP_Dropdown.OptionData(gase.DisplayName));
			graphs[gase.Type] = new GasGraph(gase, new TemperatureKelvin(maxGraphTemperature), 100, pointScaler);
		}
	}

	public void SetVisible(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void ToggleLog(bool value)
	{
		useLog = value;
		Initialize(gasType, comparisonGasType);
	}

	public void ToggleCelsius(bool value)
	{
		useCelsius = value;
		Initialize(gasType, comparisonGasType);
	}

	public void SelectComparisonInt(int value)
	{
		comparisonGasType = ((value > 0) ? Chemistry.Gases[value - 1].Type : Chemistry.GasType.Undefined);
		Initialize(gasType, comparisonGasType);
	}

	private void OnEnable()
	{
		if (!Stationpedia.Instance.isExpanded)
		{
			base.gameObject.SetActive(value: false);
		}
		comparisonDropdown.SetValueWithoutNotify(0);
	}

	private void Update()
	{
		if (Input.mousePresent && gasType != Chemistry.GasType.Undefined)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(diagramRectTransform, Input.mousePosition, null, out var localPoint);
			Rect rect = diagramRectTransform.rect;
			if (IsInside(localPoint, rect))
			{
				float num = rect.width * (1f - 2f * xMargin);
				float num2 = rect.height * (1f - 2f * yMargin);
				float num3 = Mathf.Clamp01((localPoint.x - rect.xMin - rect.width * xMargin) / num);
				Mole mole = graphs[gasType].mole;
				float num4 = (float)CalculateY(num3, mole, new TemperatureKelvin(maxGraphTemperature), useLog);
				float x = num * num3 - num / 2f;
				float y = num2 * num4 - num2 / 2f;
				float num5 = num3 * (float)maxGraphTemperature;
				string text = (useCelsius ? StringGenerator.GetString(Mathf.CeilToInt(RocketMath.KelvinToCelsius(new TemperatureKelvin(num5))), Unit.DegreesCelcius) : StringGenerator.GetString(Mathf.CeilToInt(num5), Unit.DegreesKelvin));
				temperature.text = text;
				num4 = ((!useLog) ? (num4 * (PlotSpaceYNormalizationValue(mole, useLog: false) - 1000f)) : Mathf.Pow(10f, num4 * PlotSpaceYNormalizationValue(mole, useLog: true)));
				pressure.text = ((num4 > 60000f) ? $"{GameStrings.Infinity}K Pa" : $"{num4:0.0}K Pa");
				dotRectTransform.anchoredPosition = new Vector2(x, y);
				crosshairHorizontalTransform.anchoredPosition = new Vector2(x, crosshairHorizontalTransform.anchoredPosition.y);
				crosshairVerticalTransform.anchoredPosition = new Vector2(crosshairVerticalTransform.anchoredPosition.x, y);
			}
		}
		static bool IsInside(Vector2 p, Rect r)
		{
			if (p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin)
			{
				return p.y <= r.yMax;
			}
			return false;
		}
	}

	public void Initialize(Chemistry.GasType type, Chemistry.GasType comparisonType = Chemistry.GasType.Undefined)
	{
		gasType = type;
		comparisonGasType = comparisonType;
		bool flag = gasType != Chemistry.GasType.Undefined && gasType != Chemistry.GasType.Fuel && gasType != Chemistry.GasType.Air && gasType != Chemistry.GasType.Helium;
		base.gameObject.SetActive(flag);
		if (flag)
		{
			GenerateData();
			SetDisplayValues(gasType);
			RenderGraphs(gasType, comparisonType);
		}
	}

	private void SetDisplayValues(Chemistry.GasType gasType)
	{
		if (gasType != Chemistry.GasType.Undefined)
		{
			GasGraph gasGraph = graphs[gasType];
			pressureMaxText.text = StringGenerator.GetString(gasGraph.MaxPressure(useLog), Unit.kPa);
			pressureMinText.text = StringGenerator.GetString(0, Unit.kPa);
			if (useCelsius)
			{
				tempMaxText.text = StringGenerator.GetString(Mathf.CeilToInt((float)maxGraphTemperature - 273.15f), Unit.DegreesCelcius);
				tempMinText.text = StringGenerator.GetString(Mathf.CeilToInt((float)Math.Clamp(-272.15, -273.15, 80000.0)), Unit.DegreesCelcius);
				tempFreezeText.text = StringGenerator.GetString(Mathf.CeilToInt(Mole.FreezingTemperature(gasType).ToFloat() - Chemistry.Temperature.ZeroDegrees.ToFloat()), Unit.DegreesCelcius);
			}
			else
			{
				tempMaxText.text = StringGenerator.GetString(Mathf.CeilToInt(maxGraphTemperature), Unit.DegreesKelvin);
				tempMinText.text = StringGenerator.GetString(0, Unit.DegreesKelvin);
				tempFreezeText.text = StringGenerator.GetString(Mathf.CeilToInt(Mole.FreezingTemperature(gasType).ToFloat()), Unit.DegreesKelvin);
			}
		}
	}

	private void RenderGraphs(Chemistry.GasType gasType, Chemistry.GasType compareType)
	{
		uiLineRenderer.ClearLines();
		RenderCrosshair();
		bool isComparing = compareType != gasType && compareType != Chemistry.GasType.Undefined;
		if (gasType != Chemistry.GasType.Undefined)
		{
			RenderGraph(graphs[gasType], useLog, isComparing);
		}
		if (compareType != Chemistry.GasType.Undefined)
		{
			RenderGraph(graphs[compareType], useLog, isComparing);
		}
		RenderGuides();
	}

	private void RenderGraph(GasGraph graph, bool useLog, bool isComparing)
	{
		List<Vector2> points = graph.Curve(useLog);
		(Vector2, Vector2) tuple = graph.Freeze(useLog);
		Vector2 maxLiquidTempPoint = graph.MaxLiquidTemp(useLog);
		uiLineRenderer.DrawLines(points, GraphColor, 3f);
		uiLineRenderer.DrawLine(tuple.Item1, tuple.Item2, FreezeColor, 2f);
		PositionMatterStateLabels(tuple.Item1, maxLiquidTempPoint, isComparing);
	}

	private void PositionMatterStateLabels(Vector2 freezePoint, Vector2 maxLiquidTempPoint, bool isComparing)
	{
		TMP_Text tMP_Text = solidText;
		TMP_Text tMP_Text2 = liquidText;
		bool flag = (gasText.enabled = !isComparing);
		bool flag3 = (tMP_Text2.enabled = flag);
		tMP_Text.enabled = flag3;
		if (!isComparing)
		{
			Vector2 vector = GraphArea * 0.5f;
			Vector2 vector2 = new Vector2(0f - vector.x, vector.y);
			Vector2 vector3 = new Vector2(vector.x, 0f - vector.y);
			Vector2 anchoredPosition = new Vector2(vector2.x + (freezePoint.x - vector2.x) * 0.5f, freezePoint.y + (vector2.y - freezePoint.y) * 0.5f);
			Vector2 anchoredPosition2 = new Vector2(freezePoint.x + (maxLiquidTempPoint.x - freezePoint.x) * 0.5f, freezePoint.y + (vector2.y - freezePoint.y) * 0.75f);
			Vector2 anchoredPosition3 = new Vector2(maxLiquidTempPoint.x, vector3.y + (freezePoint.y - vector3.y) * 0.5f);
			solidText.rectTransform.anchoredPosition = anchoredPosition;
			liquidText.rectTransform.anchoredPosition = anchoredPosition2;
			gasText.rectTransform.anchoredPosition = anchoredPosition3;
			tempFreezeText.rectTransform.anchoredPosition = new Vector2(freezePoint.x, tempFreezeText.rectTransform.anchoredPosition.y);
		}
	}

	private void RenderGuides()
	{
		DrawLine(Vector2.zero, new Vector2(1f, 0f), GuideColor, 2f);
		DrawLine(Vector2.zero, new Vector2(0f, 1f), GuideColor, 2f);
	}

	private void RenderCrosshair()
	{
		crosshairHorizontalTransform = DrawLine(Vector2.zero, new Vector2(0f, 1f), CrosshairColor, 2f);
		crosshairVerticalTransform = DrawLine(Vector2.zero, new Vector2(1f, 0f), CrosshairColor, 2f);
	}

	private RectTransform DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
	{
		return uiLineRenderer.DrawLine(PlotPointOnGraph(from), PlotPointOnGraph(to), color, thickness);
	}

	private Vector2 PlotPointOnGraph(Vector2 point)
	{
		return AdjustForMargins(point * DiagramSize);
	}

	private Vector2 AdjustForMargins(Vector2 value)
	{
		return AdjustForMargins(value, Margins, DiagramSize);
	}

	private static Vector2 AdjustForMargins(Vector2 value, Vector2 margins, Vector2 size)
	{
		return new Vector2(value.x * (1f - margins.x * 2f) + size.x * margins.x - size.x / 2f, value.y * (1f - margins.y * 2f) + size.y * margins.y - size.y / 2f);
	}

	private static float PlotSpaceYNormalizationValue(Mole mole, bool useLog)
	{
		if (!useLog)
		{
			return mole.MinimumLiquidPressureAtMaxTemperature().ToFloat() + 1000f;
		}
		return (float)Math.Log10(60000.0);
	}

	private static double CalculateY(float x, Mole mole, TemperatureKelvin maxTemperature, bool useLog)
	{
		TemperatureKelvin temperatureKelvin = maxTemperature * x;
		TemperatureKelvin temperatureKelvin2 = RocketMath.Clamp(temperatureKelvin, mole.FreezingTemperature(), maxTemperature);
		float num = RocketMath.Clamp(MoleHelper.EvaporationPressure(mole.Type, temperatureKelvin2), mole.MinLiquidPressure(), mole.MinimumLiquidPressureAtMaxTemperature()).ToFloat();
		num = ((temperatureKelvin < mole.FreezingTemperature()) ? Chemistry.ArmstrongLimit.ToFloat() : ((temperatureKelvin > mole.MaxLiquidTemperature()) ? TemperatureKelvin.MaxValue.ToFloat() : num));
		num = (useLog ? (Mathf.Log10(num) / PlotSpaceYNormalizationValue(mole, useLog: true)) : (num / mole.MinimumLiquidPressureAtMaxTemperature().ToFloat()));
		return num;
	}
}
