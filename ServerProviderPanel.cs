using System.Collections.Generic;
using UnityEngine;

public class ServerProviderPanel : MonoBehaviour
{
	private static readonly List<ServerProvider> Data = new List<ServerProvider>();

	public ServerProviderButton buttonPrefab;

	private List<ServerProviderButton> buttons = new List<ServerProviderButton>();

	[SerializeField]
	private RectTransform buttonParent;

	private void Awake()
	{
		base.gameObject.SetActive(Data.Count > 0);
		Shuffle();
		foreach (ServerProvider datum in Data)
		{
			ServerProviderButton serverProviderButton = Object.Instantiate(buttonPrefab, buttonParent);
			serverProviderButton.Initialize(datum);
			buttons.Add(serverProviderButton);
		}
	}

	public static void Add(ServerProvider serverProvider)
	{
		Data.Add(serverProvider);
	}

	private static void Shuffle()
	{
		int num = Data.Count;
		while (num > 1)
		{
			num--;
			int num2 = Random.Range(num, Data.Count);
			List<ServerProvider> data = Data;
			int index = num2;
			List<ServerProvider> data2 = Data;
			int index2 = num;
			ServerProvider serverProvider = Data[num];
			ServerProvider serverProvider2 = Data[num2];
			ServerProvider serverProvider3 = (data[index] = serverProvider);
			serverProvider3 = (data2[index2] = serverProvider2);
		}
	}
}
