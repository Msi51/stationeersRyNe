using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ServerProviderButton : MonoBehaviour
{
	public ServerProvider serverProvider;

	[SerializeField]
	public Image logo;

	public void Initialize(ServerProvider data)
	{
		serverProvider = data;
		byte[] data2 = File.ReadAllBytes(serverProvider.Logo.Path);
		Texture2D texture2D = new Texture2D(2, 2);
		texture2D.LoadImage(data2);
		logo.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0f, 0f));
	}

	public void OpenURL()
	{
		if (!string.IsNullOrEmpty(serverProvider.Url))
		{
			Application.OpenURL(serverProvider.Url);
		}
	}
}
