using System.Xml.Serialization;
using Assets.Scripts.Util;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class AnimCurveKey
{
	[XmlAttribute("Time")]
	public float _time;

	[XmlAttribute("Value")]
	public float _value;

	[XmlAttribute("InTangent")]
	public float _inTangent;

	[XmlAttribute("OutTangent")]
	public float _outTangent;

	[XmlAttribute("InWeight")]
	public float _inWeight;

	[XmlAttribute("OutWeight")]
	public float _outWeight;

	[XmlAttribute("WeightedMode")]
	public WeightedMode _weightedMode;

	[XmlIgnore]
	public string Id;

	[XmlIgnore]
	private string _timeString;

	[XmlIgnore]
	private string _valueString;

	[XmlIgnore]
	private string _inTangentString;

	[XmlIgnore]
	private string _outTangentString;

	[XmlIgnore]
	private string _inWeightString;

	[XmlIgnore]
	private string _outWeightString;

	private const string KEY_TIME = "Time";

	private const string KEY_VALUE = "Value";

	private const string KEY_IN_TANGENT = "InTangent";

	private const string KEY_OUT_TANGENT = "OutTangent";

	private const string KEY_IN_WEIGHT = "InWeight";

	private const string KEY_OUT_WEIGHT = "OutWeight";

	private const string WEIGHTED_MODE = "WeightedMode";

	private const string REMOVE = "-";

	private static int _next;

	public static Keyframe Create(AnimCurveKey input)
	{
		return new Keyframe
		{
			time = input._time,
			value = input._value,
			inTangent = input._inTangent,
			outTangent = input._outTangent,
			inWeight = input._inWeight,
			outWeight = input._outWeight,
			weightedMode = input._weightedMode
		};
	}

	public void Draw(AnimationCurveData dataparent)
	{
		if (ImGui.CollapsingHeader(Id))
		{
			ImguiHelper.DrawInput("Time", ref _timeString);
			ImguiHelper.DrawInput("Value", ref _valueString);
			ImguiHelper.DrawInput("InTangent", ref _inTangentString);
			ImguiHelper.DrawInput("OutTangent", ref _outTangentString);
			ImguiHelper.DrawInput("InWeight", ref _inWeightString);
			ImguiHelper.DrawInput("OutWeight", ref _outWeightString);
			ImguiHelper.DrawCombo("WeightedMode", ref _weightedMode, EnumCollections.WeightedModes);
			if (ImGui.Button("-"))
			{
				dataparent.RemoveKey(this);
			}
		}
	}

	private static string NextId()
	{
		_next++;
		return "AnimCurveKey_" + StringManager.Get(_next);
	}

	public void InitStringValues()
	{
		_timeString = StringManager.Get(_time);
		_valueString = StringManager.Get(_value);
		_inTangentString = StringManager.Get(_inTangent);
		_outTangentString = StringManager.Get(_outTangent);
		_inWeightString = StringManager.Get(_inWeight);
		_outWeightString = StringManager.Get(_outWeight);
		Id = NextId();
	}

	public bool ApplyStringValues()
	{
		bool result = false;
		if (float.TryParse(_timeString, out var result2))
		{
			if (_time != result2)
			{
				result = true;
			}
			_time = result2;
		}
		if (float.TryParse(_valueString, out var result3))
		{
			if (result3 != _value)
			{
				result = true;
			}
			_value = result3;
		}
		if (float.TryParse(_inTangentString, out var result4))
		{
			if (result4 != _inTangent)
			{
				result = true;
			}
			_inTangent = result4;
		}
		if (float.TryParse(_outTangentString, out var result5))
		{
			if (result5 != _outTangent)
			{
				result = true;
			}
			_outTangent = result5;
		}
		if (float.TryParse(_inWeightString, out var result6))
		{
			if (result6 != _inWeight)
			{
				result = true;
			}
			_inWeight = result6;
		}
		if (float.TryParse(_outWeightString, out var result7))
		{
			if (result7 != _outWeight)
			{
				result = true;
			}
			_outWeight = result7;
		}
		return result;
	}
}
