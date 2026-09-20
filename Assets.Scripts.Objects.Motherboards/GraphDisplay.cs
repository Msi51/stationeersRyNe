using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class GraphDisplay : Circuitboard
{
	public LineRenderer LineRenderer;

	public Text MaxValue;

	public Text MinValue;

	public RectTransform ZeroAxis;

	public GameObject ZeroAxisLabel;

	public int ZeroAxisMin = -300;

	public int ZeroAxisMax = 300;

	[ReadOnly]
	public List<ISetable> SetableDevices = new List<ISetable>();

	private static float _segment = 1f / 60f;

	private const int _valuesCount = 60;

	private Coroutine _setValues;

	private CircularArray<double> _values = new CircularArray<double>(60);

	private WaitForSeconds _waitForTick = new WaitForSeconds(0.5f);

	private bool _rendered;

	private static string _nanString = "NaN";

	public override bool CanDeviceLink(Device device)
	{
		return device is ISetable;
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		SetableDevices.Clear();
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (IsDeviceConnected(linkedDevice) && linkedDevice is ISetable item)
			{
				SetableDevices.Add(item);
			}
		}
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		if (!GameManager.IsBatchMode)
		{
			base.OnInsertedToComputer(computer);
			if (_setValues != null)
			{
				StopCoroutine(_setValues);
			}
			_setValues = StartCoroutine(SetValues());
			LineRenderer.gameObject.SetActive(value: true);
			ParentColorChange(ParentComputer.AsThing().CustomColor);
			ParentComputer.AsThing().OnColorChange += ParentColorChange;
		}
	}

	public override void OnRemovedFromComputer(IComputer computer)
	{
		if (!GameManager.IsBatchMode)
		{
			base.OnRemovedFromComputer(computer);
			if (_setValues != null)
			{
				StopCoroutine(_setValues);
				LineRenderer.gameObject.SetActive(value: false);
			}
			computer.AsThing().OnColorChange -= ParentColorChange;
		}
	}

	private void ParentColorChange(ColorSwatch colorSwatch)
	{
		if (colorSwatch != null)
		{
			LineRenderer.material = ParentComputer.AsThing().CustomColor.Emissive;
		}
	}

	private double GetAverageValue()
	{
		double num = 0.0;
		foreach (ISetable setableDevice in SetableDevices)
		{
			num += setableDevice.Setting;
		}
		return num / (double)SetableDevices.Count;
	}

	private Vector3[] GetPositions(bool isZero, float min, float scale)
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < _values.Count; i++)
		{
			float y = 0f;
			if (!isZero)
			{
				y = ((float)_values[i] - min) / scale;
			}
			list.Add(new Vector3((float)i * _segment - 0.5f, y, 0f));
		}
		return list.ToArray();
	}

	private IEnumerator SetValues()
	{
		while ((bool)this)
		{
			_values.Clear();
			while (ParentComputer.AsThing().OnOff && ParentComputer.AsThing().Powered)
			{
				if (!_rendered)
				{
					_rendered = true;
					LineRenderer.gameObject.SetActive(value: true);
				}
				double averageValue = GetAverageValue();
				_values.AddToEnd(double.IsNaN(averageValue) ? 0.0 : averageValue);
				float num = Mathf.Max((float)_values.Max(), 0f);
				float num2 = Mathf.Min((float)_values.Min(), 0f);
				if (num == 0f && num2 == 0f)
				{
					num = 1f;
					num2 = -1f;
				}
				float num3 = num - num2;
				bool flag = Mathf.Abs(num3) <= 1.1E-44f;
				LineRenderer.SetPositions(GetPositions(flag, num2, num3));
				Vector2 anchoredPosition = ZeroAxis.anchoredPosition;
				anchoredPosition.y = (flag ? 0f : ((0f - num2) / num3 * (float)(ZeroAxisMax - ZeroAxisMin) + (float)ZeroAxisMin));
				ZeroAxis.anchoredPosition = anchoredPosition;
				if (Mathf.Approximately(num2, 0f) || Mathf.Approximately(num, 0f))
				{
					if (ZeroAxisLabel.activeSelf)
					{
						ZeroAxisLabel.SetActive(value: false);
					}
				}
				else if (!ZeroAxisLabel.activeSelf)
				{
					ZeroAxisLabel.SetActive(value: true);
				}
				MaxValue.text = (flag ? _nanString : num.ToStringRounded());
				MinValue.text = (flag ? _nanString : num2.ToStringRounded());
				yield return _waitForTick;
			}
			if (_rendered)
			{
				_rendered = false;
				LineRenderer.gameObject.SetActive(value: false);
			}
			yield return _waitForTick;
		}
	}

	private bool ShouldDraw()
	{
		if (ParentComputer != null && !ParentComputer.AsThing().IsOccluded && ParentComputer.AsThing().OnOff)
		{
			return ParentComputer.AsThing().Powered;
		}
		return false;
	}
}
