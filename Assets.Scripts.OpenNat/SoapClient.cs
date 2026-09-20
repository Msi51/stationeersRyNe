using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Assets.Scripts.OpenNat;

public class SoapClient
{
	public delegate void DelegateEvent(bool Success, Mapping mapping, XmlDocument reponseData, MappingException me);

	private int TIMEOUT = 5;

	public bool isResponseCompleted;

	private readonly string _serviceType;

	private readonly Uri _url;

	private DelegateEvent OnComplete;

	public SoapClient(Uri url, string serviceType)
	{
		_url = url;
		_serviceType = serviceType;
	}

	public void TimeoutAfter(int timeout)
	{
		TIMEOUT = timeout;
	}

	public IEnumerator InvokeAsync(DelegateEvent onComplete, Mapping mapping, string operationName, IDictionary<string, object> args, UpnpNatDevice Device)
	{
		OnComplete = onComplete;
		byte[] array = BuildMessageBody(operationName, args);
		HttpWebRequest httpWebRequest = BuildHttpWebRequest(operationName, array);
		Stream requestStream = httpWebRequest.GetRequestStream();
		requestStream.Write(array, 0, array.Length);
		requestStream.Close();
		try
		{
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			if (httpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				OnComplete(Success: false, mapping, null, new MappingException((int)httpWebResponse.StatusCode, operationName + " failed"));
				yield break;
			}
			using Stream stream = httpWebResponse.GetResponseStream();
			using StreamReader streamReader = new StreamReader(stream);
			XmlDocument xmlDocument = GetXmlDocument(streamReader.ReadToEnd());
			OnComplete(Success: true, mapping, xmlDocument, null);
		}
		catch (Exception ex)
		{
			OnComplete(Success: false, mapping, null, new MappingException(0, operationName + " : " + ex.Message));
		}
	}

	private HttpWebRequest BuildHttpWebRequest(string operationName, byte[] messageBody)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_url);
		httpWebRequest.KeepAlive = false;
		httpWebRequest.Method = "POST";
		httpWebRequest.ContentType = "text/xml; charset=\"utf-8\"";
		httpWebRequest.Headers.Add("SOAPACTION", "\"" + _serviceType + "#" + operationName + "\"");
		httpWebRequest.ContentLength = messageBody.Length;
		httpWebRequest.Timeout = TIMEOUT * 1000;
		return httpWebRequest;
	}

	private byte[] BuildMessageBody(string operationName, IEnumerable<KeyValuePair<string, object>> args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<s:Envelope ");
		stringBuilder.AppendLine("   xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
		stringBuilder.AppendLine("   s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
		stringBuilder.AppendLine("   <s:Body>");
		stringBuilder.AppendLine("\t  <u:" + operationName + " xmlns:u=\"" + _serviceType + "\">");
		foreach (KeyValuePair<string, object> arg in args)
		{
			stringBuilder.AppendLine("\t\t <" + arg.Key + ">" + Convert.ToString(arg.Value, CultureInfo.InvariantCulture) + "</" + arg.Key + ">");
		}
		stringBuilder.AppendLine("\t  </u:" + operationName + ">");
		stringBuilder.AppendLine("   </s:Body>");
		stringBuilder.Append("</s:Envelope>\r\n\r\n");
		string s = stringBuilder.ToString();
		return Encoding.UTF8.GetBytes(s);
	}

	private XmlDocument GetXmlDocument(string response)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(response);
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
		xmlNamespaceManager.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
		XmlNode node;
		if ((node = xmlDocument.SelectSingleNode("//errorNs:UPnPError", xmlNamespaceManager)) != null)
		{
			int num = Convert.ToInt32(node.GetXmlElementText("errorCode"), CultureInfo.InvariantCulture);
			string xmlElementText = node.GetXmlElementText("errorDescription");
			Debug.LogWarning($"Server failed with error: {num} - {xmlElementText}");
			throw new MappingException(num, xmlElementText);
		}
		return xmlDocument;
	}
}
