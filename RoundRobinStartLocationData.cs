using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

public class RoundRobinStartLocationData : StartLocationData
{
	[XmlElement("StartLocation")]
	public List<StartLocationData> Locations = new List<StartLocationData>();

	private List<int> _shuffleIndex;

	public override void Initialize(ModAbout mod)
	{
		if (IsValid())
		{
			DataCollection.Register(this, mod);
			Shuffle();
		}
	}

	private void Shuffle()
	{
		_shuffleIndex = new List<int>(Locations.Count);
		for (int i = 0; i < Locations.Count; i++)
		{
			_shuffleIndex.Add(i);
		}
		System.Random random = new System.Random();
		int num = _shuffleIndex.Count;
		while (num > 1)
		{
			num--;
			int num2 = random.Next(num + 1);
			List<int> shuffleIndex = _shuffleIndex;
			int index = num2;
			List<int> shuffleIndex2 = _shuffleIndex;
			int index2 = num;
			int num3 = _shuffleIndex[num];
			int num4 = _shuffleIndex[num2];
			int num5 = (shuffleIndex[index] = num3);
			num5 = (shuffleIndex2[index2] = num4);
		}
	}

	public override Vector3 WorldPosition()
	{
		return Get().WorldPosition();
	}

	public override bool IsValid()
	{
		return Locations.Count > 0;
	}

	public override StartLocationData Get()
	{
		Shuffle();
		int num = 0;
		StartLocationData startLocationData = null;
		int num2 = 0;
		while (startLocationData == null)
		{
			StartLocationData startLocationData2 = Locations[_shuffleIndex[num2]];
			if (ClientsAtLocation(startLocationData2) == num)
			{
				startLocationData = DataCollection.Get<StartLocationData>(startLocationData2.Id);
			}
			num2++;
			if (num2 == Locations.Count)
			{
				num2 = 0;
				num++;
			}
		}
		return startLocationData;
	}

	private int ClientsAtLocation(StartLocationData locationData)
	{
		int num = 0;
		foreach (SerializedClientInfo value in GameManager.ClientInfo.Values)
		{
			if (value.StartLocationHash == locationData.IdHash)
			{
				num++;
			}
		}
		return num;
	}
}
