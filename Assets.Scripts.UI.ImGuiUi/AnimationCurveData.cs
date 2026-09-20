using System.Collections.Generic;
using System.Xml.Serialization;
using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class AnimationCurveData
{
	[XmlElement("Key")]
	public List<AnimCurveKey> Keys = new List<AnimCurveKey>(8);

	[XmlIgnore]
	public string Id;

	private const string ADD = "+";

	[XmlIgnore]
	public AnimationCurve Curve { get; private set; }

	public static implicit operator AnimationCurve(AnimationCurveData reference)
	{
		return reference.Curve;
	}

	public virtual void Init()
	{
		Curve = ToAnimationCurve();
		foreach (AnimCurveKey key in Keys)
		{
			key.InitStringValues();
		}
	}

	public AnimationCurve ToAnimationCurve()
	{
		return Create(this);
	}

	public static AnimationCurve Create(AnimationCurveData data)
	{
		if (data == null)
		{
			return null;
		}
		AnimationCurve animationCurve = new AnimationCurve();
		foreach (AnimCurveKey key in data.Keys)
		{
			animationCurve.AddKey(AnimCurveKey.Create(key));
		}
		return animationCurve;
	}

	public void Draw()
	{
		if (ImGui.Button("+"))
		{
			AddKey();
		}
		for (int i = 0; i < Keys.Count; i++)
		{
			Keys[i].Draw(this);
		}
	}

	public void RemoveKey(AnimCurveKey key)
	{
		Keys.Remove(key);
		Curve = ToAnimationCurve();
	}

	private void AddKey()
	{
		AnimCurveKey animCurveKey = new AnimCurveKey
		{
			_time = 100f,
			_value = 0f
		};
		animCurveKey.InitStringValues();
		Keys.Add(animCurveKey);
		Curve = ToAnimationCurve();
	}

	public bool Apply()
	{
		bool flag = false;
		foreach (AnimCurveKey key in Keys)
		{
			if (key.ApplyStringValues())
			{
				flag = true;
			}
		}
		if (flag)
		{
			Curve = ToAnimationCurve();
		}
		return flag;
	}
}
