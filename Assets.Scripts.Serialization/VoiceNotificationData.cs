using System.Xml.Serialization;

namespace Assets.Scripts.Serialization;

[XmlRoot]
public class VoiceNotificationData
{
	public string Notification;

	public bool IsEnabled;
}
