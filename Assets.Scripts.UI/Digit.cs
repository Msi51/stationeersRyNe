using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Audio;
using UnityEngine;

namespace Assets.Scripts.UI;

public class Digit : GameBase, IGamePoolable<Digit>, IPoolable<Digit>
{
	private static GameObjectPool<Digit> _prefabPool;

	private static Digit _prefabDigit;

	public MeshRenderer MeshRenderer;

	public MeshFilter MeshFilter;

	public LogicDisplay Parent;

	protected static GameObjectPool<Digit> PrefabPool
	{
		get
		{
			return _prefabPool;
		}
		set
		{
			_prefabPool = value;
		}
	}

	public int PoolId { get; set; }

	public string DebugName { get; set; }

	public bool IsActive { get; set; }

	public ObjectPool<Digit> Pool { get; set; }

	public GameObjectPool<Digit> GamePool
	{
		get
		{
			return PrefabPool;
		}
		set
		{
			PrefabPool = value;
		}
	}

	public static void Initialize(Digit digit)
	{
		_prefabPool = new GameObjectPool<Digit>("Digit");
		_prefabPool.Initialize(20);
		_prefabPool.PopulateAll(digit);
		_prefabDigit = digit;
	}

	public static Digit AssignFromPool(LogicDisplay display)
	{
		if (_prefabPool.Available == 0)
		{
			_prefabPool.PopulateOnce(_prefabDigit);
		}
		Digit digit = _prefabPool.Get();
		try
		{
			digit.Parent = display;
			return digit;
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
		return null;
	}

	internal static void ReturnToPrefabPool(Digit rockyBody)
	{
		_prefabPool.Return(rockyBody);
	}

	public static void ReturnAllPooled()
	{
		foreach (Digit item in new List<Digit>(_prefabPool._active))
		{
			ReturnToPrefabPool(item);
		}
	}

	public void DrawInList(ref int index)
	{
		throw new NotImplementedException();
	}

	public void ReturnToPool()
	{
		_prefabPool.Return(this);
	}
}
