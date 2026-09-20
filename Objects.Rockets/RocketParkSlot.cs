using System;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

namespace Objects.Rockets;

public class RocketParkSlot
{
	public static readonly Dictionary<Vector2Int, RocketParkSlot> SlotsLookup = new Dictionary<Vector2Int, RocketParkSlot>(100);

	public static readonly List<RocketParkSlot> Slots = new List<RocketParkSlot>(100);

	private static Transform _parentTransform;

	private const int SPACING = 50;

	private const int OFFSET = 1;

	public static readonly Vector3 ParkParentPosition = new Vector3(3000f, 1001f, 0f);

	public static readonly Vector2 InvalidLocation = Vector2.zero;

	public readonly Vector2Int Location;

	public Rocket AssignedRocket;

	private static bool _initialised;

	public Vector3 GetWorldPosition()
	{
		return new Vector3(Location.x, 0f, Location.y) + ParkParentPosition;
	}

	public RocketParkSlot(Vector2Int location)
	{
		Location = location;
	}

	private void Assign(Rocket rocket)
	{
		if (rocket != null)
		{
			if (AssignedRocket != null)
			{
				throw new Exception("Can't assign rocket to slot as it already has one assigned");
			}
			if (rocket.RocketParkSlot != null && rocket.RocketParkSlot != this)
			{
				rocket.RocketParkSlot.UnAssign(rocket);
			}
			AssignedRocket = rocket;
			rocket.RocketParkSlot = this;
		}
	}

	private void UnAssign(Rocket rocket)
	{
		if (rocket == AssignedRocket)
		{
			AssignedRocket = null;
		}
		if (rocket.RocketParkSlot == this)
		{
			rocket.RocketParkSlot = null;
		}
	}

	public static void Initialise()
	{
		if (_initialised)
		{
			return;
		}
		_initialised = true;
		_parentTransform = new GameObject("~RocketPark").transform;
		_parentTransform.position = ParkParentPosition;
		SlotsLookup.Clear();
		Slots.Clear();
		int num = (int)Mathf.Sqrt(100f);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector2Int vector2Int = new Vector2Int(i * 50 + 1, j * 50 + 1);
				RocketParkSlot rocketParkSlot = new RocketParkSlot(vector2Int);
				SlotsLookup.Add(vector2Int, rocketParkSlot);
				Slots.Add(rocketParkSlot);
			}
		}
	}

	public static RocketParkSlot GetFreeSlot()
	{
		foreach (RocketParkSlot slot in Slots)
		{
			if (slot.AssignedRocket == null)
			{
				return slot;
			}
		}
		return null;
	}

	public static void ParkRocket(Rocket rocket)
	{
		(GetFreeSlot() ?? throw new NotImplementedException("No Free Slot to park rocket")).Assign(rocket);
	}

	public static void ParkRocket(Vector2Int location, Rocket rocket)
	{
		if (rocket == null)
		{
			return;
		}
		if (SlotsLookup.TryGetValue(location, out var value))
		{
			if (value.AssignedRocket == rocket)
			{
				return;
			}
			if (value.AssignedRocket == null)
			{
				value.Assign(rocket);
				return;
			}
		}
		RocketParkSlot freeSlot = GetFreeSlot();
		if (freeSlot != null)
		{
			freeSlot.Assign(rocket);
		}
		else
		{
			ConsoleWindow.PrintError("No free rocket park slot available for '" + rocket.DisplayName + "'.");
		}
	}

	public static void RemoveRocket(Rocket rocket)
	{
		rocket?.RocketParkSlot?.UnAssign(rocket);
	}

	public static void ClearAll()
	{
		_initialised = false;
		SlotsLookup.Clear();
		Slots.Clear();
		if (_parentTransform != null)
		{
			UnityEngine.Object.Destroy(_parentTransform.gameObject);
		}
	}
}
