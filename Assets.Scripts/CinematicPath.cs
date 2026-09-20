using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts;

[XmlRoot("CinematicPath")]
public class CinematicPath
{
	public string Name = "Untitled";

	public bool ConstantSpeed;

	public float ConstantSpeedMps = 5f;

	[XmlArray("Keyframes")]
	[XmlArrayItem("Keyframe")]
	public List<CinematicKeyframe> Keyframes = new List<CinematicKeyframe>();

	private const int LengthSamples = 32;

	private const int ArcSamples = 32;

	private static readonly float[] _arcScratch = new float[33];

	public int Count => Keyframes?.Count ?? 0;

	public float TotalDuration => GetTotalDuration(loop: false);

	public float TotalDurationLooped => GetTotalDuration(loop: true);

	public float GetTotalDuration(bool loop)
	{
		if (Keyframes == null || Keyframes.Count == 0)
		{
			return 0f;
		}
		float num = Mathf.Max(0f, Keyframes[0].HoldDuration);
		for (int i = 1; i < Keyframes.Count; i++)
		{
			num += SegmentDuration(i, loop);
			num += Mathf.Max(0f, Keyframes[i].HoldDuration);
		}
		if (loop && Keyframes.Count >= 2)
		{
			num += SegmentDuration(Keyframes.Count, loop: true);
		}
		return num;
	}

	private float SegmentDuration(int i, bool loop)
	{
		float num = SegmentLength(i, loop);
		float num2;
		if (ConstantSpeed)
		{
			num2 = Mathf.Max(0.01f, ConstantSpeedMps);
		}
		else
		{
			int index = ((i != Keyframes.Count) ? i : 0);
			num2 = Mathf.Max(0.01f, Keyframes[index].Speed);
		}
		return num / num2;
	}

	private float SegmentLength(int i, bool loop)
	{
		GetCatmullPoints(i, loop, out var p, out var p2, out var p3, out var p4);
		float num = 0f;
		Vector3 a = p2;
		for (int j = 1; j <= 32; j++)
		{
			float t = (float)j / 32f;
			Vector3 vector = CatmullRom(p, p2, p3, p4, t);
			num += Vector3.Distance(a, vector);
			a = vector;
		}
		return num;
	}

	private void GetCatmullPoints(int i, bool loop, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
	{
		int count = Keyframes.Count;
		if (i == count)
		{
			p1 = Keyframes[count - 1].Position;
			p2 = Keyframes[0].Position;
			p0 = Keyframes[count - 2].Position;
			p3 = ((count >= 2) ? Keyframes[1].Position : (p2 + (p2 - p1)));
			return;
		}
		p1 = Keyframes[i - 1].Position;
		p2 = Keyframes[i].Position;
		if (i >= 2)
		{
			p0 = Keyframes[i - 2].Position;
		}
		else if (loop && count >= 2)
		{
			p0 = Keyframes[count - 1].Position;
		}
		else
		{
			p0 = p1 - (p2 - p1);
		}
		if (i + 1 < count)
		{
			p3 = Keyframes[i + 1].Position;
		}
		else if (loop && count >= 2)
		{
			p3 = Keyframes[0].Position;
		}
		else
		{
			p3 = p2 + (p2 - p1);
		}
	}

	public CinematicKeyframe Add(Vector3 pos, Quaternion rot, float fov, string label = null)
	{
		CinematicKeyframe cinematicKeyframe = new CinematicKeyframe
		{
			Label = (string.IsNullOrEmpty(label) ? $"Keyframe {Keyframes.Count}" : label),
			Position = pos,
			Rotation = rot,
			Fov = fov
		};
		Keyframes.Add(cinematicKeyframe);
		return cinematicKeyframe;
	}

	public bool Move(int from, int to)
	{
		if (from < 0 || from >= Keyframes.Count)
		{
			return false;
		}
		to = Mathf.Clamp(to, 0, Keyframes.Count - 1);
		if (from == to)
		{
			return false;
		}
		CinematicKeyframe item = Keyframes[from];
		Keyframes.RemoveAt(from);
		Keyframes.Insert(to, item);
		return true;
	}

	public bool RemoveAt(int index)
	{
		if (index < 0 || index >= Keyframes.Count)
		{
			return false;
		}
		Keyframes.RemoveAt(index);
		return true;
	}

	public bool Evaluate(float time, bool loop, out Vector3 position, out Quaternion rotation, out float fov)
	{
		position = default(Vector3);
		rotation = Quaternion.identity;
		fov = 70f;
		if (Keyframes == null || Keyframes.Count == 0)
		{
			return false;
		}
		if (Keyframes.Count == 1)
		{
			position = Keyframes[0].Position;
			rotation = Keyframes[0].Rotation;
			fov = Keyframes[0].Fov;
			return true;
		}
		bool flag = loop && Keyframes.Count >= 2;
		float num = Mathf.Max(0f, time);
		if (flag)
		{
			float totalDuration = GetTotalDuration(loop: true);
			if (totalDuration > 0f)
			{
				num = Mathf.Repeat(num, totalDuration);
			}
		}
		float num2 = Mathf.Max(0f, Keyframes[0].HoldDuration);
		if (num <= num2)
		{
			position = Keyframes[0].Position;
			rotation = Keyframes[0].Rotation;
			fov = Keyframes[0].Fov;
			return true;
		}
		for (int i = 1; i < Keyframes.Count; i++)
		{
			float num3 = SegmentDuration(i, loop);
			if (num <= num2 + num3)
			{
				float localT = ((num3 <= 0f) ? 1f : ((num - num2) / num3));
				SampleSegment(i, localT, loop, out position, out rotation, out fov);
				return true;
			}
			num2 += num3;
			float num4 = Mathf.Max(0f, Keyframes[i].HoldDuration);
			if (num <= num2 + num4)
			{
				position = Keyframes[i].Position;
				rotation = Keyframes[i].Rotation;
				fov = Keyframes[i].Fov;
				return true;
			}
			num2 += num4;
		}
		if (flag)
		{
			float num5 = SegmentDuration(Keyframes.Count, loop: true);
			if (num <= num2 + num5)
			{
				float localT2 = ((num5 <= 0f) ? 1f : ((num - num2) / num5));
				SampleSegment(Keyframes.Count, localT2, loop: true, out position, out rotation, out fov);
				return true;
			}
		}
		CinematicKeyframe cinematicKeyframe = Keyframes[Keyframes.Count - 1];
		position = cinematicKeyframe.Position;
		rotation = cinematicKeyframe.Rotation;
		fov = cinematicKeyframe.Fov;
		return true;
	}

	private void SampleSegment(int i, float localT, bool loop, out Vector3 position, out Quaternion rotation, out float fov)
	{
		int count = Keyframes.Count;
		CinematicKeyframe cinematicKeyframe;
		CinematicKeyframe cinematicKeyframe2;
		if (i == count)
		{
			cinematicKeyframe = Keyframes[count - 1];
			cinematicKeyframe2 = Keyframes[0];
		}
		else
		{
			cinematicKeyframe = Keyframes[i - 1];
			cinematicKeyframe2 = Keyframes[i];
		}
		GetCatmullPoints(i, loop, out var p, out var p2, out var p3, out var p4);
		float num = CinematicKeyframe.Apply(cinematicKeyframe2.Ease, localT);
		BuildArcTable(p, p2, p3, p4, out var totalLength);
		float t = ((totalLength > 1E-06f) ? ArcLengthToParam(num * totalLength) : num);
		position = CatmullRom(p, p2, p3, p4, t);
		rotation = SquadRotation(i, loop, t);
		fov = Mathf.Lerp(cinematicKeyframe.Fov, cinematicKeyframe2.Fov, t);
	}

	private Quaternion SquadRotation(int i, bool loop, float t)
	{
		GetSquadQuaternions(i, loop, out var q, out var q2, out var q3, out var q4);
		Quaternion a = QuatIntermediate(q, q2, q3);
		Quaternion b = QuatIntermediate(q2, q3, q4);
		return Squad(q2, q3, a, b, t);
	}

	private void GetSquadQuaternions(int i, bool loop, out Quaternion q0, out Quaternion q1, out Quaternion q2, out Quaternion q3)
	{
		int count = Keyframes.Count;
		if (i == count)
		{
			q1 = Keyframes[count - 1].Rotation;
			q2 = Keyframes[0].Rotation;
			q0 = Keyframes[count - 2].Rotation;
			q3 = ((count >= 2) ? Keyframes[1].Rotation : q2);
			return;
		}
		q1 = Keyframes[i - 1].Rotation;
		q2 = Keyframes[i].Rotation;
		if (i >= 2)
		{
			q0 = Keyframes[i - 2].Rotation;
		}
		else if (loop && count >= 2)
		{
			q0 = Keyframes[count - 1].Rotation;
		}
		else
		{
			q0 = q1;
		}
		if (i + 1 < count)
		{
			q3 = Keyframes[i + 1].Rotation;
		}
		else if (loop && count >= 2)
		{
			q3 = Keyframes[0].Rotation;
		}
		else
		{
			q3 = q2;
		}
	}

	private static Quaternion QuatIntermediate(Quaternion qPrev, Quaternion q, Quaternion qNext)
	{
		if (Quaternion.Dot(q, qPrev) < 0f)
		{
			qPrev = NegateQuat(qPrev);
		}
		if (Quaternion.Dot(q, qNext) < 0f)
		{
			qNext = NegateQuat(qNext);
		}
		Quaternion quaternion = Quaternion.Inverse(q);
		Quaternion quaternion2 = QuatLog(quaternion * qPrev);
		Quaternion quaternion3 = QuatLog(quaternion * qNext);
		Quaternion v = new Quaternion((0f - (quaternion2.x + quaternion3.x)) * 0.25f, (0f - (quaternion2.y + quaternion3.y)) * 0.25f, (0f - (quaternion2.z + quaternion3.z)) * 0.25f, 0f);
		return q * QuatExp(v);
	}

	public static Quaternion Squad(Quaternion q0, Quaternion q1, Quaternion a, Quaternion b, float t)
	{
		if (Quaternion.Dot(q0, q1) < 0f)
		{
			q1 = NegateQuat(q1);
		}
		if (Quaternion.Dot(a, b) < 0f)
		{
			b = NegateQuat(b);
		}
		Quaternion a2 = Quaternion.SlerpUnclamped(q0, q1, t);
		Quaternion quaternion = Quaternion.SlerpUnclamped(a, b, t);
		if (Quaternion.Dot(a2, quaternion) < 0f)
		{
			quaternion = NegateQuat(quaternion);
		}
		return Quaternion.SlerpUnclamped(a2, quaternion, 2f * t * (1f - t));
	}

	private static Quaternion QuatLog(Quaternion q)
	{
		float num = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z);
		if (num < 1E-06f)
		{
			return new Quaternion(0f, 0f, 0f, 0f);
		}
		float num2 = Mathf.Atan2(num, q.w) / num;
		return new Quaternion(q.x * num2, q.y * num2, q.z * num2, 0f);
	}

	private static Quaternion QuatExp(Quaternion v)
	{
		float num = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
		if (num < 1E-06f)
		{
			return new Quaternion(0f, 0f, 0f, 1f);
		}
		float num2 = Mathf.Sin(num) / num;
		return new Quaternion(v.x * num2, v.y * num2, v.z * num2, Mathf.Cos(num));
	}

	private static Quaternion NegateQuat(Quaternion q)
	{
		return new Quaternion(0f - q.x, 0f - q.y, 0f - q.z, 0f - q.w);
	}

	private static void BuildArcTable(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, out float totalLength)
	{
		Vector3 a = p1;
		float num = 0f;
		_arcScratch[0] = 0f;
		for (int i = 1; i <= 32; i++)
		{
			float t = (float)i / 32f;
			Vector3 vector = CatmullRom(p0, p1, p2, p3, t);
			num += Vector3.Distance(a, vector);
			_arcScratch[i] = num;
			a = vector;
		}
		totalLength = num;
	}

	private static float ArcLengthToParam(float arcLength)
	{
		float[] arcScratch = _arcScratch;
		if (arcLength <= 0f)
		{
			return 0f;
		}
		int num = arcScratch.Length - 1;
		if (arcLength >= arcScratch[num])
		{
			return 1f;
		}
		int num2 = 0;
		int num3 = num;
		while (num3 - num2 > 1)
		{
			int num4 = num2 + num3 >> 1;
			if (arcScratch[num4] < arcLength)
			{
				num2 = num4;
			}
			else
			{
				num3 = num4;
			}
		}
		float num5 = arcScratch[num3] - arcScratch[num2];
		float num6 = ((num5 > 1E-06f) ? ((arcLength - arcScratch[num2]) / num5) : 0f);
		return ((float)num2 + num6) / (float)num;
	}

	public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float num = t * t;
		float num2 = num * t;
		return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * num + (-p0 + 3f * p1 - 3f * p2 + p3) * num2);
	}

	public static string GetPathDirectory()
	{
		string text = Path.Combine(StationSaveUtils.GetSavePath(), "cinematic_paths");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static string GetPathFile(string name)
	{
		string text = Sanitize(name);
		return Path.Combine(GetPathDirectory(), text + ".xml");
	}

	public bool SaveToDisk()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = "Untitled";
			}
			string pathFile = GetPathFile(Name);
			return XmlSerialization.Serialize(this, pathFile);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return false;
		}
	}

	public static CinematicPath LoadFromDisk(string name)
	{
		return XmlSerialization.LoadOrNull<CinematicPath>(GetPathFile(name));
	}

	public static List<string> ListSavedNames()
	{
		List<string> list = new List<string>();
		try
		{
			foreach (string item in Directory.EnumerateFiles(GetPathDirectory(), "*.xml"))
			{
				list.Add(Path.GetFileNameWithoutExtension(item));
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		list.Sort();
		return list;
	}

	public static bool DeleteFromDisk(string name)
	{
		try
		{
			string pathFile = GetPathFile(name);
			if (File.Exists(pathFile))
			{
				File.Delete(pathFile);
				return true;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return false;
	}

	private static string Sanitize(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return "Untitled";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		char[] array = raw.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			char c = array[i];
			for (int j = 0; j < invalidFileNameChars.Length; j++)
			{
				if (c == invalidFileNameChars[j])
				{
					array[i] = '_';
					break;
				}
			}
		}
		return new string(array).Trim();
	}
}
