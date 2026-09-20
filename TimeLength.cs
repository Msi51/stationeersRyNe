using System;
using System.Text;
using Assets.Scripts.Localization2;

public readonly struct TimeLength : IEquatable<TimeLength>
{
	private readonly ulong days;

	private readonly ulong hours;

	private readonly ulong minutes;

	private readonly ulong seconds;

	private readonly ulong milliSeconds;

	public ulong Days => days;

	public ulong Hours => hours;

	public ulong Minutes => minutes;

	public ulong Seconds => seconds;

	public ulong MilliSeconds => milliSeconds;

	public TimeLength(double inputSeconds)
	{
		days = (ulong)inputSeconds / 86400;
		inputSeconds -= (double)(days * 3600 * 24);
		hours = (ulong)inputSeconds / 3600;
		inputSeconds -= (double)(hours * 3600);
		minutes = (ulong)inputSeconds / 60;
		inputSeconds -= (double)(minutes * 60);
		seconds = (ulong)Math.Floor(inputSeconds);
		inputSeconds -= (double)seconds;
		milliSeconds = (ulong)(inputSeconds * 1000.0);
	}

	public static TimeLength FromDays(ulong d)
	{
		return new TimeLength(d * 86400);
	}

	public static TimeLength FromDays(double d)
	{
		return new TimeLength(d * 86400.0);
	}

	public static TimeLength FromDaysHoursMinutesSeconds(ulong d, ulong h, ulong m, ulong s)
	{
		return new TimeLength(d * 3600 * 24 + h * 3600 + m * 60 + s);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (days != 0)
		{
			stringBuilder.Append(days).Append(GameStrings.TimeLengthDays);
		}
		if (hours != 0 || days != 0)
		{
			stringBuilder.Append(hours).Append(GameStrings.TimeLengthHours);
		}
		if (minutes != 0 || hours != 0 || days != 0)
		{
			stringBuilder.Append(minutes).Append(GameStrings.TimeLengthMinutes);
		}
		stringBuilder.Append(seconds).Append(GameStrings.TimeLengthSeconds);
		return stringBuilder.ToString();
	}

	public override bool Equals(object obj)
	{
		if (obj is TimeLength other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(TimeLength other)
	{
		if (hours == other.hours && minutes == other.minutes && seconds == other.seconds)
		{
			return milliSeconds == other.milliSeconds;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(hours, minutes, seconds, milliSeconds);
	}

	public static bool operator ==(TimeLength left, TimeLength right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TimeLength left, TimeLength right)
	{
		return !(left == right);
	}

	public static TimeLength operator +(TimeLength a, TimeLength b)
	{
		return new TimeLength(a.ToTotalSeconds() + b.ToTotalSeconds());
	}

	public double ToTotalSeconds()
	{
		return (double)days * 3600.0 * 24.0 + (double)hours * 3600.0 + (double)minutes * 60.0 + (double)seconds + (double)milliSeconds / 1000.0;
	}

	public static TimeLength FromSeconds(double totalSeconds)
	{
		return new TimeLength(totalSeconds);
	}
}
