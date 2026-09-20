using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;

[XmlRoot("TimeSpan")]
public class TimeSpanReference
{
	private const string DAYS = "Days";

	[XmlAttribute("Days")]
	public double Days = double.NaN;

	private const string HOURS = "Hours";

	[XmlAttribute("Hours")]
	public double Hours = double.NaN;

	private const string MINUTES = "Minutes";

	[XmlAttribute("Minutes")]
	public double Minutes = double.NaN;

	private const string SECONDS = "Seconds";

	[XmlAttribute("Seconds")]
	public double Seconds = double.NaN;

	public double GetTotalSeconds()
	{
		double num = 0.0;
		if (!double.IsNaN(Days))
		{
			num += Days * 86400.0;
		}
		if (!double.IsNaN(Hours))
		{
			num += Hours * 3600.0;
		}
		if (!double.IsNaN(Minutes))
		{
			num += Minutes * 60.0;
		}
		if (!double.IsNaN(Seconds))
		{
			num += Seconds;
		}
		return num;
	}

	public static implicit operator TimeSpan(TimeSpanReference reference)
	{
		return TimeSpan.FromSeconds(reference.GetTotalSeconds());
	}

	public void Add(ref XElement parentElement, string elementName)
	{
		XElement element = XDocumentHelper.MakeElement(elementName, ref parentElement);
		if (!double.IsNaN(Days))
		{
			XDocumentHelper.SetAttribute(element, "Days", Days.ToString("F4"));
		}
		if (!double.IsNaN(Hours))
		{
			XDocumentHelper.SetAttribute(element, "Hours", Hours.ToString("F4"));
		}
		if (!double.IsNaN(Minutes))
		{
			XDocumentHelper.SetAttribute(element, "Minutes", Minutes.ToString("F4"));
		}
		if (!double.IsNaN(Seconds))
		{
			XDocumentHelper.SetAttribute(element, "Seconds", Seconds.ToString("F4"));
		}
	}
}
