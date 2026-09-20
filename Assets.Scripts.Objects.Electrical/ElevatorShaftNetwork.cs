using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects.Electrical;

public class ElevatorShaftNetwork
{
	public List<ElevatorShaft> Shafts = new List<ElevatorShaft>();

	public ElevatorCarrage Carrage;

	private float _speed;

	public const float DEFAULT_SPEED = 1.5f;

	private UniTask _refreshLevelNumbers;

	public float Speed
	{
		get
		{
			return _speed;
		}
		set
		{
			if (Carrage != null && NetworkManager.IsServer && !RocketMath.Approximately(_speed, value, 0.001f))
			{
				Carrage.NetworkUpdateFlags |= 256;
			}
			_speed = value;
		}
	}

	public int PoweredValue
	{
		get
		{
			foreach (ElevatorShaft shaft in Shafts)
			{
				if (shaft.PoweredValue >= 1 && (object)shaft.PowerCable != null)
				{
					return 1;
				}
			}
			return 0;
		}
	}

	public bool Powered
	{
		get
		{
			foreach (ElevatorShaft shaft in Shafts)
			{
				if (shaft.Powered && (object)shaft.PowerCable != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	public ElevatorShaftNetwork()
	{
		Speed = 1.5f;
	}

	public ElevatorShaftNetwork(ElevatorShaft elevatorShaft)
	{
		Register(elevatorShaft);
		Speed = 1.5f;
	}

	public ElevatorShaftNetwork(ElevatorLevel elevatorShaft, bool makeCarrage = true)
	{
		Register(elevatorShaft);
		Speed = 1.5f;
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running && makeCarrage)
		{
			ElevatorCarrage elevatorCarrage = OnServer.CreateOld(elevatorShaft.CarragePrefab, elevatorShaft.BottomPosition.position, elevatorShaft.ThingTransformLocalRotation, 0uL) as ElevatorCarrage;
			if (elevatorCarrage != null)
			{
				Register(elevatorCarrage);
				elevatorShaft.SetElevatorTarget(elevatorShaft.ShaftLevel);
			}
		}
	}

	public void RefreshLevelState()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		foreach (ElevatorShaft shaft in Shafts)
		{
			OnServer.Interact(shaft.InteractLock, (!Carrage || Carrage.ElevatorMode != ElevatorMode.Stationary) ? 1 : 0);
		}
		ShaftPowerUpdated();
	}

	public void Register(ElevatorCarrage elevatorCarrage)
	{
		if (!(Carrage == elevatorCarrage))
		{
			if ((bool)Carrage && GameManager.RunSimulation)
			{
				OnServer.Destroy(Carrage);
			}
			Carrage = elevatorCarrage;
			if ((bool)elevatorCarrage.CurrentShaft)
			{
				Carrage.LevelTarget = Shafts.IndexOf(elevatorCarrage.CurrentShaft);
			}
			elevatorCarrage.ShaftNetwork = this;
			RefreshLevelState();
		}
	}

	public void Register(ElevatorShaft elevatorShaft)
	{
		if (!Shafts.Contains(elevatorShaft))
		{
			Shafts.Add(elevatorShaft);
			elevatorShaft.ShaftNetwork = this;
			RefreshLevelNumbers();
		}
	}

	public void Deregister(ElevatorShaft elevatorShaft)
	{
		Shafts.Remove(elevatorShaft);
		if (Shafts.Count == 0)
		{
			Destroy();
		}
		ElevatorShaft elevatorShaft2 = elevatorShaft.ShaftAbove();
		ElevatorShaft elevatorShaft3 = elevatorShaft.ShaftBelow();
		if (GameManager.RunSimulation && (bool)Carrage && Carrage.CurrentShaft == elevatorShaft)
		{
			if ((bool)elevatorShaft2)
			{
				Carrage.TeleportTo(elevatorShaft2);
			}
			else if ((bool)elevatorShaft3)
			{
				Carrage.TeleportTo(elevatorShaft3);
			}
		}
		if ((bool)elevatorShaft2 && (bool)elevatorShaft3)
		{
			ElevatorShaftNetwork elevatorShaftNetwork = new ElevatorShaftNetwork();
			while ((bool)elevatorShaft3)
			{
				Deregister(elevatorShaft3);
				elevatorShaftNetwork.Register(elevatorShaft3);
				if ((bool)Carrage && Carrage.CurrentShaft == elevatorShaft3)
				{
					elevatorShaftNetwork.Register(Carrage);
					Carrage = null;
				}
				elevatorShaft3 = elevatorShaft3.ShaftBelow();
			}
		}
		RefreshLevelNumbers();
	}

	public void MergeInto(ElevatorShaftNetwork newNetwork)
	{
		int count = Shafts.Count;
		while (count-- > 0)
		{
			ElevatorShaft elevatorShaft = Shafts[count];
			Shafts.Remove(elevatorShaft);
			newNetwork.Register(elevatorShaft);
		}
		if ((bool)Carrage)
		{
			if (!newNetwork.Carrage)
			{
				newNetwork.Register(Carrage);
			}
			else if (GameManager.RunSimulation)
			{
				OnServer.Destroy(Carrage);
			}
			Carrage = null;
		}
		if (Shafts.Count > 0)
		{
			RefreshLevelNumbers();
		}
	}

	public void Destroy()
	{
		if ((bool)Carrage && GameManager.RunSimulation)
		{
			OnServer.Destroy(Carrage);
		}
	}

	private async UniTask RefreshLevels()
	{
		await UniTask.Delay(1000);
		Shafts.Sort((ElevatorShaft s1, ElevatorShaft s2) => s1.Position.y.CompareTo(s2.Position.y));
		int count = Shafts.Count;
		while (count-- > 0)
		{
			ElevatorShaft elevatorShaft = Shafts[count];
			if ((bool)elevatorShaft)
			{
				elevatorShaft.ShaftLevel = count;
			}
		}
		Carrage?.CurrentShaft.CheckCarrageState(Carrage);
		RefreshLevelState();
	}

	public void RefreshLevelNumbers()
	{
		if (_refreshLevelNumbers.Status != UniTaskStatus.Pending)
		{
			_refreshLevelNumbers = RefreshLevels();
		}
	}

	public float GetUsedPower(CableNetwork cableNetwork)
	{
		float num = 0f;
		foreach (ElevatorShaft shaft in Shafts)
		{
			num += shaft.GetShaftUsedPower(cableNetwork);
		}
		if (!(num <= 0f))
		{
			return num;
		}
		return -1f;
	}

	public bool AllowSetPower(CableNetwork cableNetwork)
	{
		bool flag = false;
		foreach (ElevatorShaft shaft in Shafts)
		{
			flag |= shaft.AllowSetPower(cableNetwork);
		}
		return flag;
	}

	public bool IsAnyOtherPowered(ElevatorLevel elevatorLevel)
	{
		foreach (ElevatorShaft shaft in Shafts)
		{
			if ((object)shaft.PowerCable != null && !(shaft == elevatorLevel) && shaft.Powered)
			{
				return true;
			}
		}
		return false;
	}

	public void ShaftPowerUpdated()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		bool powered = Powered;
		foreach (ElevatorShaft shaft in Shafts)
		{
			if (shaft.Powered != powered && !ShaftHasPowerConnection(shaft))
			{
				OnServer.Interact(shaft.InteractPowered, powered ? 1 : 0);
			}
		}
	}

	private bool ShaftHasPowerConnection(ElevatorShaft shaft)
	{
		foreach (Connection openEnd in shaft.OpenEnds)
		{
			if ((openEnd.ConnectionType & NetworkType.Power) != NetworkType.None)
			{
				return true;
			}
		}
		return false;
	}
}
