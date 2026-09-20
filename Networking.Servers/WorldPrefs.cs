using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Networking.Servers;

public class WorldPrefs
{
	public class UI
	{
		public class Window
		{
			[XmlAttribute]
			public int Slot;

			[XmlAttribute]
			public int Hash;

			[XmlAttribute]
			public bool Open;

			[XmlAttribute]
			public bool Docked;

			[XmlAttribute]
			public float X;

			[XmlAttribute]
			public float Y;

			public static explicit operator Window(WindowSaveData from)
			{
				return new Window
				{
					Slot = from.SlotId,
					Hash = from.StringHash,
					Open = from.IsOpen,
					Docked = !from.IsUndocked,
					X = from.Position.x,
					Y = from.Position.y
				};
			}

			public static explicit operator WindowSaveData(Window self)
			{
				return new WindowSaveData
				{
					SlotId = self.Slot,
					StringHash = self.Hash,
					IsOpen = self.Open,
					IsUndocked = !self.Docked,
					Position = new Vector2(self.X, self.Y)
				};
			}
		}

		[XmlAttribute]
		public int SelectedButton;

		[XmlAttribute]
		public int ActiveHandSlot;

		[XmlElement(ElementName = "Window")]
		public List<Window> Windows = new List<Window>();

		public static explicit operator UI(UserInterfaceSaveData from)
		{
			UI uI = new UI();
			uI.SelectedButton = from?.SelectedButton ?? (-1);
			uI.ActiveHandSlot = from?.ActiveHandSlot ?? (-1);
			if (from?.OpenSlots != null)
			{
				uI.Windows = new List<Window>(from.OpenSlots.Count);
				foreach (WindowSaveData openSlot in from.OpenSlots)
				{
					uI.Windows.Add((Window)openSlot);
				}
			}
			else
			{
				uI.Windows = new List<Window>();
			}
			return uI;
		}

		public static explicit operator UserInterfaceSaveData(UI self)
		{
			List<WindowSaveData> list = new List<WindowSaveData>(self.Windows.Count);
			foreach (Window window in self.Windows)
			{
				list.Add((WindowSaveData)window);
			}
			return new UserInterfaceSaveData
			{
				OpenSlots = list,
				SelectedButton = self.SelectedButton,
				ActiveHandSlot = self.ActiveHandSlot
			};
		}
	}

	[XmlAttribute]
	public string Id;

	[XmlElement]
	public UI Interface;

	[XmlElement("DiscoveredPoi")]
	public List<int> DiscoveredPois;
}
