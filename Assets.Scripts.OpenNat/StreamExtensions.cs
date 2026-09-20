using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace Assets.Scripts.OpenNat;

internal static class StreamExtensions
{
	internal static string ReadAsMany(this StreamReader stream, int bytesToRead)
	{
		char[] array = new char[bytesToRead];
		stream.ReadBlock(array, 0, bytesToRead);
		return new string(array);
	}

	internal static string GetXmlElementText(this XmlNode node, string elementName)
	{
		XmlElement xmlElement = node[elementName];
		if (xmlElement == null)
		{
			return string.Empty;
		}
		return xmlElement.InnerText;
	}

	internal static bool ContainsIgnoreCase(this string s, string pattern)
	{
		return s.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static void LogInfo(this TraceSource source, string format, params object[] args)
	{
		try
		{
		}
		catch (ObjectDisposedException)
		{
			source.Switch.Level = SourceLevels.Off;
		}
	}

	internal static void LogWarn(this TraceSource source, string format, params object[] args)
	{
		try
		{
		}
		catch (ObjectDisposedException)
		{
			source.Switch.Level = SourceLevels.Off;
		}
	}

	internal static void LogError(this TraceSource source, string format, params object[] args)
	{
		try
		{
		}
		catch (ObjectDisposedException)
		{
			source.Switch.Level = SourceLevels.Off;
		}
	}

	internal static string ToPrintableXml(this XmlDocument document)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode);
		try
		{
			xmlTextWriter.Formatting = Formatting.Indented;
			document.WriteContentTo(xmlTextWriter);
			xmlTextWriter.Flush();
			memoryStream.Flush();
			memoryStream.Position = 0L;
			return new StreamReader(memoryStream).ReadToEnd();
		}
		catch (Exception)
		{
			return document.ToString();
		}
	}
}
