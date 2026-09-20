using System;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class RocketMath
{
	public const float TwoPi = MathF.PI * 2f;

	public const float PiOverTwo = MathF.PI / 2f;

	public const float TERA = 1E+12f;

	public const float GIGA = 1E+09f;

	public const float MEGA = 1000000f;

	public const float KILO = 1000f;

	public const float CENTI = 0.01f;

	public const float MILLI = 0.001f;

	public const float MICRO = 1E-06f;

	public const float RootTwo = 1.4142135f;

	public const float RootThree = 1.7320508f;

	public const float Sixty = 60f;

	public const float Fp16MaxValue = 65504f;

	public const float Fp16MinValue = -65504f;

	public const float Minute = 60f;

	public const float Hour = 3600f;

	public const float ANIM_TOLERANCE = 0.0005f;

	private static System.Random _random = new System.Random();

	public static int GetDecimalPlaces(double n)
	{
		n = Math.Abs(n);
		n -= (double)(int)n;
		int num = 0;
		while (n > 0.0)
		{
			num++;
			n *= 10.0;
			n -= (double)(int)n;
		}
		return num;
	}

	public static float SquareDistance(float xDiff, float yDiff, float zDiff)
	{
		return xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
	}

	public static float HalfToFloat(ushort value)
	{
		float num = Mathf.HalfToFloat(value);
		if (!(num >= 65504f))
		{
			if (num <= -65504f)
			{
				return -65504f;
			}
			return num;
		}
		return 65504f;
	}

	public static float WeightedAverage(float valueA, float valueB, float weight)
	{
		weight = Mathf.Clamp01(weight);
		float num = valueA * weight;
		float num2 = valueB * (1f - weight);
		return num + num2;
	}

	public static PressurekPa WeightedAverage(PressurekPa valueA, PressurekPa valueB, float weight)
	{
		weight = Mathf.Clamp01(weight);
		PressurekPa pressurekPa = valueA * weight;
		PressurekPa pressurekPa2 = valueB * (1f - weight);
		return pressurekPa + pressurekPa2;
	}

	public static void RandomInBox(out Vector3 output, Vector3 firstCorner, Vector3 oppositeCorner)
	{
		output.x = UnityEngine.Random.Range(firstCorner.x, oppositeCorner.x);
		output.y = UnityEngine.Random.Range(firstCorner.y, oppositeCorner.y);
		output.z = UnityEngine.Random.Range(firstCorner.z, oppositeCorner.z);
	}

	public static int MaxAbs(int a, int b, int c)
	{
		return Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), Math.Abs(c));
	}

	public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
	{
		if (a == b)
		{
			return 0f;
		}
		Vector3 vector = b - a;
		return Mathf.Clamp01(Vector3.Dot(value - a, vector) / Vector3.Dot(vector, vector));
	}

	public static Vector3 RandomInBox(Vector3 firstCorner, Vector3 oppositeCorner)
	{
		RandomInBox(out var output, firstCorner, oppositeCorner);
		return output;
	}

	public static Vector3 TransformPoint(Vector3 localSpace, Vector3 translation, Quaternion rotation)
	{
		return rotation * localSpace + translation;
	}

	public static Vector3 InverseTransformPoint(Vector3 worldSpace, Vector3 translation, Quaternion rotation)
	{
		return Quaternion.Inverse(rotation) * (worldSpace - translation);
	}

	public static Vector3 InverseTransformDirecton(Vector3 worldSpace, Quaternion rotation)
	{
		return Quaternion.Inverse(rotation) * worldSpace;
	}

	public static float DistanceSquared(Vector3 vector1, Vector3 vector2)
	{
		return Vector3.SqrMagnitude(vector2 - vector1);
	}

	public static float DistanceSquared2D(Vector3 vector1, Vector3 vector2)
	{
		Vector3 vector3 = vector2 - vector1;
		vector3.y = 0f;
		return Vector3.SqrMagnitude(vector3);
	}

	public static bool Approximately(float float1, float float2, float tol = 1E-07f)
	{
		return Mathf.Abs(float1 - float2) < tol;
	}

	public static bool Approximately(double float1, double float2, double tol = 1E-07)
	{
		return Math.Abs(float1 - float2) < tol;
	}

	public static bool Approximately(MoleQuantity a, MoleQuantity b, DirtyMoleTolerance dirtyMoleTolerance)
	{
		return dirtyMoleTolerance switch
		{
			DirtyMoleTolerance.OnePercent => ApproximatelyByRatio(a.ToDouble(), b.ToDouble(), 0.009999999776482582), 
			DirtyMoleTolerance.Precision => Approximately(a, b, Chemistry.MINIMUM_QUANTITY_MOLES.ToDouble()), 
			_ => throw new ArgumentOutOfRangeException("dirtyMoleTolerance", dirtyMoleTolerance, null), 
		};
	}

	public static bool Approximately(MoleEnergy a, MoleEnergy b, DirtyMoleTolerance dirtyMoleTolerance)
	{
		return dirtyMoleTolerance switch
		{
			DirtyMoleTolerance.OnePercent => ApproximatelyByRatio(a.ToDouble(), b.ToDouble(), 0.009999999776482582), 
			DirtyMoleTolerance.Precision => Approximately(a, b, Chemistry.MINIMUM_QUANTITY_MOLES.ToDouble()), 
			_ => throw new ArgumentOutOfRangeException("dirtyMoleTolerance", dirtyMoleTolerance, null), 
		};
	}

	public static bool Approximately(MoleQuantity a, MoleQuantity b, double tol = 1E-07)
	{
		return Math.Abs((a - b).ToDouble()) < tol;
	}

	public static bool Approximately(MoleEnergy a, MoleEnergy b, double tol = 1E-07)
	{
		return Math.Abs((a - b).ToDouble()) < tol;
	}

	public static bool Approximately(TemperatureKelvin a, TemperatureKelvin b, double tol = 1E-07)
	{
		return Math.Abs((a - b).ToDouble()) < tol;
	}

	public static bool Approximately(PressurekPa a, PressurekPa b, double tol = 1E-07)
	{
		return Math.Abs((a - b).ToDouble()) < tol;
	}

	public static bool Approximately(VolumeLitres a, VolumeLitres b, double tol = 1E-07)
	{
		return Math.Abs((a - b).ToDouble()) < tol;
	}

	public static float KelvinToCelsius(TemperatureKelvin kelvin)
	{
		return (kelvin - Chemistry.Temperature.ZeroDegrees).ToFloat();
	}

	public static TemperatureKelvin CelsiusToKelvin(float celsius)
	{
		return new TemperatureKelvin(Chemistry.Temperature.ZeroDegrees.ToDouble() + (double)celsius);
	}

	public static bool Approximately(Vector3 a, Vector3 b)
	{
		return (a - b).sqrMagnitude < 9.999999E-09f;
	}

	public static bool Approximately(Vector2 a, Vector2 b)
	{
		return (a - b).sqrMagnitude < 9.999999E-09f;
	}

	public static bool Approximately(Vector3 a, Vector3 b, float tol)
	{
		return (a - b).sqrMagnitude < tol * tol;
	}

	public static bool Approximately(Quaternion a, Quaternion b, float tol)
	{
		return 1f - Quaternion.Dot(a.normalized, b.normalized) < tol;
	}

	public static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * Math.Clamp(t, 0.0, 1.0);
	}

	public static bool ApproximatelyByRatio(float a, float b, float ratioTol = 0.01f)
	{
		if (b == 0f && a == 0f)
		{
			return true;
		}
		if (b == 0f && a > 0f)
		{
			return false;
		}
		float num = Mathf.Abs(a / b);
		if (num > 1f - ratioTol)
		{
			return num < 1f + ratioTol;
		}
		return false;
	}

	public static bool ApproximatelyByRatio(double a, double b, double ratioTol = 0.01)
	{
		if (b == 0.0 && a == 0.0)
		{
			return true;
		}
		if (b == 0.0 && a > 0.0)
		{
			return false;
		}
		double num = Math.Abs(a / b);
		if (num > 1.0 - ratioTol)
		{
			return num < 1.0 + ratioTol;
		}
		return false;
	}

	public static float RoundToSignificantDigits(this float d, int digits)
	{
		if (d == 0f)
		{
			return 0f;
		}
		bool num = d < 0f;
		if (num)
		{
			d *= -1f;
		}
		double num2 = Math.Pow(10.0, Math.Floor(Math.Log10(Math.Abs(d))) + 1.0);
		float num3 = (float)(num2 * Math.Round((double)d / num2, digits));
		if (!num)
		{
			return num3;
		}
		return num3 * -1f;
	}

	public static int RoundUpToLeftmostPlace(float value)
	{
		if (value <= 0f)
		{
			return 0;
		}
		int num = Mathf.FloorToInt(Mathf.Log10(value));
		float num2 = Mathf.Pow(10f, num);
		return (int)((float)Mathf.RoundToInt(value / num2) * num2);
	}

	public static int ModuloCorrect(int value, int mod)
	{
		return (value % mod + mod) % mod;
	}

	public static float ModuloCorrect(float value, float mod)
	{
		return value - mod * Mathf.Floor(value / mod);
	}

	public static double ModuloCorrect(double value, double mod)
	{
		return value - mod * Math.Floor(value / mod);
	}

	public static bool CompareVectors(Vector3 a, Vector3 b, float angleError = float.Epsilon)
	{
		if (!Mathf.Approximately(a.magnitude, b.magnitude))
		{
			return false;
		}
		float num = Mathf.Cos(angleError * (MathF.PI / 180f));
		if (Vector3.Dot(a.normalized, b.normalized) >= num)
		{
			return true;
		}
		return false;
	}

	public static Vector3 Abs(Vector3 a)
	{
		return new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
	}

	public static MoleEnergy Abs(MoleEnergy a)
	{
		return new MoleEnergy(Math.Abs(a.ToDouble()));
	}

	public static PressurekPa Abs(PressurekPa a)
	{
		return new PressurekPa(Math.Abs(a.ToDouble()));
	}

	public static TemperatureKelvin Abs(TemperatureKelvin a)
	{
		return new TemperatureKelvin(Math.Abs(a.ToDouble()));
	}

	public static float SignedAngle(float a, float b)
	{
		float num = a % 360f;
		float num2 = b % 360f;
		float num3 = num - num2;
		if (num3 > 180f)
		{
			return num3 - 360f;
		}
		if (num3 < -180f)
		{
			return num3 + 360f;
		}
		return num3;
	}

	public static float SquareDistanceComparison(Vector3 vector1, Vector3 vector2, float value)
	{
		return DistanceSquared(vector1, vector2) - value * value;
	}

	public static float SquareDistanceComparison2D(Vector3 vector1, Vector3 vector2, float value)
	{
		return DistanceSquared2D(vector1, vector2) - value * value;
	}

	public static float MapToScale(float min, float max, float outMin, float outMax, float value)
	{
		float num = max - min;
		float num2 = outMax - outMin;
		return (value - min) * num2 / num + outMin;
	}

	public static double MapToScale(double min, double max, double outMin, double outMax, double value)
	{
		double num = max - min;
		double num2 = outMax - outMin;
		return (value - min) * num2 / num + outMin;
	}

	public static PressurekPa MapToScale(PressurekPa min, PressurekPa max, PressurekPa outMin, PressurekPa outMax, PressurekPa value)
	{
		PressurekPa pressurekPa = max - min;
		PressurekPa pressurekPa2 = outMax - outMin;
		return (value - min) * pressurekPa2 / pressurekPa + outMin;
	}

	public static VolumeLitres MapToScale(VolumeLitres min, VolumeLitres max, VolumeLitres outMin, VolumeLitres outMax, VolumeLitres value)
	{
		VolumeLitres volumeLitres = max - min;
		VolumeLitres volumeLitres2 = outMax - outMin;
		return (value - min) * volumeLitres2 / volumeLitres + outMin;
	}

	public static float MapToScaleClamp(float min, float max, float outMin, float outMax, float value)
	{
		return Mathf.Clamp(MapToScale(min, max, outMin, outMax, value), outMin, outMax);
	}

	public static float MaxAbsoluteComponent(Vector3 input)
	{
		float val = Math.Abs(input.x);
		val = Math.Max(Math.Abs(input.y), val);
		return Math.Max(Math.Abs(input.z), val);
	}

	public static bool IsSphereOutsideFrustum(Plane[] planes, Vector3 sphereCenter, float sphereRadius)
	{
		foreach (Plane plane in planes)
		{
			if (plane.GetDistanceToPoint(sphereCenter) + sphereRadius < 0f)
			{
				return true;
			}
		}
		return false;
	}

	public static void CartesianToSpherical(out float azimuth, out float elevation, out float radius, Vector3 cartesian)
	{
		radius = cartesian.magnitude;
		elevation = (float)Math.Acos(cartesian.z / radius);
		azimuth = (float)Math.Atan2(cartesian.y, cartesian.x);
	}

	public static void CartesianToSphericalFixed(out float azimuth, out float elevation, out float radius, Vector3 cartesian)
	{
		radius = cartesian.magnitude;
		elevation = (float)Math.Acos(cartesian.z / radius);
		azimuth = (float)Math.Atan2(0f - cartesian.y, cartesian.x);
	}

	public static Vector3 SphericalToCartesian(out Vector3 output, float azimuth, float elevation, float radius = 1f)
	{
		float num = (float)((double)radius * Math.Sin(elevation));
		output.x = (float)((double)num * Math.Cos(azimuth));
		output.y = (float)((double)num * Math.Sin(azimuth));
		output.z = (float)((double)radius * Math.Cos(elevation));
		return output;
	}

	public static Vector3 SphericalToCartesian(float azimuth, float elevation, float radius = 1f)
	{
		Vector3 output;
		return SphericalToCartesian(out output, azimuth, elevation, radius);
	}

	public static bool Chance(float probability)
	{
		return _random.NextDouble() < (double)probability;
	}

	public static float RandomGaussian(float minValue = 0f, float maxValue = 1f)
	{
		double num;
		double num3;
		do
		{
			num = 2.0 * _random.NextDouble() - 1.0;
			double num2 = 2.0 * _random.NextDouble() - 1.0;
			num3 = num * num + num2 * num2;
		}
		while (num3 >= 1.0);
		double num4 = num * Math.Sqrt(-2.0 * Math.Log(num3) / num3);
		float num5 = (minValue + maxValue) / 2f;
		float num6 = (maxValue - num5) / 3f;
		return Mathf.Clamp((float)num4 * num6 + num5, minValue, maxValue);
	}

	public static int Wrap(int value, int min, int max)
	{
		int num = max - min + 1;
		int num2 = value - min;
		return num2 - num * (int)Math.Floor((float)num2 / (float)num) + min;
	}

	public static MoleQuantity NumberOfMolesGas(PressurekPa pressure, VolumeLitres volume, TemperatureKelvin temperature)
	{
		return IdealGas.Quantity(pressure, volume, temperature);
	}

	public static MoleQuantity NumberOfMolesLiquid(VolumeLitres volume, Chemistry.GasType gasType)
	{
		if (Chemistry.MatterState(gasType) != AtmosphereHelper.MatterState.Liquid)
		{
			throw new Exception($"{gasType} is not a liquid!");
		}
		VolumeLitres volumeLitres = Chemistry.MolarVolumeLiquid(gasType);
		return new MoleQuantity(volume.ToDouble() / volumeLitres.ToDouble());
	}

	public static PressurekPa Min(PressurekPa val1, PressurekPa val2)
	{
		if (!(val1 < val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static PressurekPa Max(PressurekPa val1, PressurekPa val2)
	{
		if (!(val1 > val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static PressurekPa Clamp(PressurekPa value, PressurekPa min, PressurekPa max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static VolumeLitres Min(VolumeLitres val1, VolumeLitres val2)
	{
		if (!(val1 < val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static VolumeLitres Max(VolumeLitres val1, VolumeLitres val2)
	{
		if (!(val1 > val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static VolumeLitres Clamp(VolumeLitres value, VolumeLitres min, VolumeLitres max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static MoleQuantity Min(MoleQuantity val1, MoleQuantity val2)
	{
		if (!(val1 < val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static MoleQuantity Max(MoleQuantity val1, MoleQuantity val2)
	{
		if (!(val1 > val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static MoleQuantity Clamp(MoleQuantity value, MoleQuantity min, MoleQuantity max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static MoleQuantity Lerp(MoleQuantity a, MoleQuantity b, double t)
	{
		return a + (b - a) * new MoleQuantity(Math.Clamp(t, 0.0, 1.0));
	}

	public static MoleEnergy Min(MoleEnergy val1, MoleEnergy val2)
	{
		if (!(val1 < val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static MoleEnergy Max(MoleEnergy val1, MoleEnergy val2)
	{
		if (!(val1 > val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static MoleEnergy Clamp(MoleEnergy value, MoleEnergy min, MoleEnergy max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static MoleEnergy Lerp(MoleEnergy a, MoleEnergy b, double t)
	{
		return a + (b - a) * new MoleEnergy(Math.Clamp(t, 0.0, 1.0));
	}

	public static VolumeLitres Lerp(VolumeLitres a, VolumeLitres b, double t)
	{
		return a + (b - a) * new VolumeLitres(Math.Clamp(t, 0.0, 1.0));
	}

	public static TemperatureKelvin Min(TemperatureKelvin val1, TemperatureKelvin val2)
	{
		if (!(val1 < val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static TemperatureKelvin Max(TemperatureKelvin val1, TemperatureKelvin val2)
	{
		if (!(val1 > val2) && !val1.IsNaN())
		{
			return val2;
		}
		return val1;
	}

	public static TemperatureKelvin Clamp(TemperatureKelvin value, TemperatureKelvin min, TemperatureKelvin max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static TemperatureKelvin Lerp(TemperatureKelvin a, TemperatureKelvin b, double t)
	{
		return a + (b - a) * new TemperatureKelvin(Math.Clamp(t, 0.0, 1.0));
	}

	public static PressurekPa Lerp(PressurekPa a, PressurekPa b, double t)
	{
		return a + (b - a) * new PressurekPa(Math.Clamp(t, 0.0, 1.0));
	}

	public static bool IsPowerOfTwo(int value)
	{
		if (value == 0)
		{
			return false;
		}
		return (value & (value - 1)) == 0;
	}

	public static Vector3 LinePlaneIntersect(Vector3 origin, Vector3 direction, Vector3 planeNormal, Vector3 planePoint)
	{
		float num = Vector3.Dot(origin - planePoint, planeNormal);
		float num2 = Vector3.Dot(direction, planeNormal);
		float num3 = num / num2;
		return origin - direction * num3;
	}

	public static bool ClosestPointsOnTwoLines(Vector3 aOrigin, Vector3 aDirection, Vector3 bOrigin, Vector3 bDirection, out Vector3 aClosest, out Vector3 bClosest)
	{
		aClosest = Vector3.zero;
		bClosest = Vector3.zero;
		float num = Vector3.Dot(aDirection, aDirection);
		float num2 = Vector3.Dot(aDirection, bDirection);
		float num3 = Vector3.Dot(bDirection, bDirection);
		float num4 = num * num3 - num2 * num2;
		if (!Mathf.Approximately(num4, 0f))
		{
			Vector3 rhs = aOrigin - bOrigin;
			float num5 = Vector3.Dot(aDirection, rhs);
			float num6 = Vector3.Dot(bDirection, rhs);
			float num7 = (num2 * num6 - num5 * num3) / num4;
			float num8 = (num * num6 - num5 * num2) / num4;
			aClosest = aOrigin + aDirection * num7;
			bClosest = bOrigin + bDirection * num8;
			return true;
		}
		return false;
	}

	public static Vector2 BoxIntersection(Vector3 origin, Vector3 direction, Vector3 halfExtents)
	{
		CalculateEntryExit(direction.x, origin.x, halfExtents.x, out var entryDist, out var exitDist);
		CalculateEntryExit(direction.y, origin.y, halfExtents.y, out var entryDist2, out var exitDist2);
		CalculateEntryExit(direction.z, origin.z, halfExtents.z, out var entryDist3, out var exitDist3);
		float num = Mathf.Max(Mathf.Max(entryDist, entryDist2), entryDist3);
		float num2 = Mathf.Min(Mathf.Min(exitDist, exitDist2), exitDist3);
		if (num > num2 || num2 < 0f)
		{
			return new Vector2(-1f, -1f);
		}
		return new Vector2(num, num2);
		static void CalculateEntryExit(float num3, float num4, float num5, out float reference, out float reference2)
		{
			if (Approximately(num3, 0.0, 1E-05))
			{
				if (Mathf.Abs(num4) > num5)
				{
					reference = float.MaxValue;
					reference2 = float.MaxValue;
				}
				else
				{
					reference = float.MinValue;
					reference2 = float.MaxValue;
				}
			}
			else
			{
				float num6 = 1f / num3;
				float a = (0f - num5 - num4) * num6;
				float b = (num5 - num4) * num6;
				reference = Mathf.Min(a, b);
				reference2 = Mathf.Max(a, b);
			}
		}
	}
}
