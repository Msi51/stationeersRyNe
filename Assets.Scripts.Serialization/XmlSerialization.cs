using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Serialization;

public static class XmlSerialization
{
	public static T LoadOrCreateNew<T>(string path, Func<T> onNewCreated) where T : class
	{
		T val;
		if (!File.Exists(path))
		{
			val = onNewCreated();
			val.SaveXml(path);
		}
		else
		{
			val = Deserialize<T>(path);
			if (val == null)
			{
				val = onNewCreated();
				val.SaveXml(path);
			}
		}
		return val;
	}

	public static T LoadOrNull<T>(string path)
	{
		if (!File.Exists(path))
		{
			return default(T);
		}
		return Deserialize<T>(path);
	}

	public static bool SaveXml<T>(this T savable, string path) where T : class
	{
		return Serialize(savable, path);
	}

	public static bool Serialize<T>(T obj, string path)
	{
		try
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			using StreamWriter textWriter = new StreamWriter(path);
			xmlSerializer.Serialize(textWriter, obj);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return false;
		}
	}

	public static bool Serialization(XmlSerializer xmlSerializer, object obj, string path)
	{
		if (xmlSerializer == null || obj == null)
		{
			return false;
		}
		try
		{
			using StreamWriter streamWriter = new StreamWriter(path);
			return Serialization(xmlSerializer, streamWriter, obj, path);
		}
		catch (Exception ex)
		{
			string text = ((ex.InnerException != null) ? ("\nFurther info: " + ex.InnerException.Message) : string.Empty);
			Debug.LogError("An error occured serializing data!\n" + ex.Message + text);
			return false;
		}
	}

	public static bool Serialization(XmlSerializer xmlSerializer, StreamWriter streamWriter, object obj, string path = "")
	{
		if (streamWriter == null || obj == null)
		{
			streamWriter?.Close();
			return false;
		}
		try
		{
			xmlSerializer.Serialize(streamWriter, obj);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("An error occured serializing data!: " + path + " - " + ex.Message);
			return false;
		}
		finally
		{
			streamWriter.Close();
		}
	}

	public static object Deserialize(XmlSerializer xmlSerializer, string path)
	{
		if (xmlSerializer == null || !File.Exists(path))
		{
			return null;
		}
		try
		{
			using StreamReader streamReader = new StreamReader(path, Encoding.GetEncoding("UTF-8"));
			return Deserialize(xmlSerializer, streamReader, path);
		}
		catch (Exception ex)
		{
			string text = ((ex.InnerException != null) ? ("\nFurther info: " + ex.InnerException.Message) : string.Empty);
			ConsoleWindow.PrintError("An error occured deserializing data!\n" + ex.Message + text);
			return false;
		}
	}

	public static T Deserialize<T>(string path, string root = "")
	{
		try
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("File not found at path: " + path);
			}
			XmlSerializer xmlSerializer = (string.IsNullOrEmpty(root) ? new XmlSerializer(typeof(T)) : new XmlSerializer(typeof(T), new XmlRootAttribute(root)));
			using StreamReader textReader = new StreamReader(path);
			return (T)xmlSerializer.Deserialize(textReader);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return default(T);
		}
	}

	public static object Deserialize(XmlSerializer xmlSerializer, StreamReader streamReader, string path = "")
	{
		if (xmlSerializer == null || streamReader == null)
		{
			streamReader?.Close();
			return null;
		}
		try
		{
			using XmlReader xmlReader = XmlReader.Create(streamReader, XmlSaveLoad.XmlReaderSettings);
			return xmlSerializer.Deserialize(xmlReader);
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			Debug.LogError("An error occurred while deserializing a file!: " + path + " - " + ex.Message + ((ex.InnerException != null) ? (" : " + ex.InnerException.Message) : ""));
			return null;
		}
		finally
		{
			streamReader.Close();
		}
	}
}
