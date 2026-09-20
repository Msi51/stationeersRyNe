using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionPool
{
	private static ProjectionPool system;

	private List<PoolItem> activePool;

	private List<PoolItem> inactivePool;

	private Transform parent;

	private static ProjectionPool System
	{
		get
		{
			if (system == null)
			{
				system = new ProjectionPool();
			}
			return system;
		}
	}

	internal static Transform Parent
	{
		get
		{
			if (System.parent == null)
			{
				System.parent = new GameObject("Projection Pool").transform;
			}
			return System.parent;
		}
	}

	internal static void Update(float deltaTime)
	{
		if (System.activePool != null && System.activePool.Count > 0)
		{
			for (int num = System.activePool.Count - 1; num >= 0; num--)
			{
				System.activePool[num].Update(deltaTime);
			}
		}
	}

	public static Projection Request(ProjectionType Type)
	{
		if (System.activePool == null)
		{
			System.activePool = new List<PoolItem>();
		}
		if (System.inactivePool != null && System.inactivePool.Count > 0)
		{
			PoolItem poolItem = System.inactivePool[0];
			System.inactivePool.RemoveAt(0);
			poolItem.GameObject.SetActive(value: true);
			System.activePool.Add(poolItem);
			poolItem.Reset(Type);
			return poolItem.Projection;
		}
		if (System.activePool.Count < DynamicDecals.Settings.poolLimit)
		{
			GameObject gameObject = new GameObject("Projection");
			PoolItem poolItem2 = new PoolItem(gameObject);
			gameObject.AddComponent<Decal>().PoolItem = poolItem2;
			gameObject.AddComponent<Eraser>().PoolItem = poolItem2;
			gameObject.AddComponent<Pulse>().PoolItem = poolItem2;
			poolItem2.Reset(Type);
			System.activePool.Add(poolItem2);
			return poolItem2.Projection;
		}
		PoolItem poolItem3 = System.activePool[0];
		System.activePool.RemoveAt(0);
		System.activePool.Add(poolItem3);
		poolItem3.Reset(Type);
		return poolItem3.Projection;
	}

	public static Projection RequestCopy(Projection Projection)
	{
		if (Projection == null)
		{
			return null;
		}
		if (Projection.GetType() == typeof(Decal))
		{
			Decal obj = (Decal)Request(ProjectionType.Decal);
			obj.CopyAllProperties((Decal)Projection);
			return obj;
		}
		if (Projection.GetType() == typeof(Eraser))
		{
			Eraser obj2 = (Eraser)Request(ProjectionType.Eraser);
			obj2.CopyAllProperties((Eraser)Projection);
			return obj2;
		}
		if (Projection.GetType() == typeof(Pulse))
		{
			Pulse obj3 = (Pulse)Request(ProjectionType.Pulse);
			obj3.CopyAllProperties((Pulse)Projection);
			return obj3;
		}
		throw new NotImplementedException("Projection Type not recognized, If your implementing your own projection types, you need to implement a copy method like the projection types above");
	}

	public static void Return(Projection Projection)
	{
		if (Projection.PoolItem != null)
		{
			Return(Projection.PoolItem);
		}
	}

	internal static void Return(PoolItem Item)
	{
		if (System.inactivePool == null)
		{
			System.inactivePool = new List<PoolItem>();
		}
		System.activePool.Remove(Item);
		if (Item.GameObject != null)
		{
			Item.GameObject.SetActive(value: false);
		}
		System.inactivePool.Add(Item);
	}
}
