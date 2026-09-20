using Assets.Scripts.Util;

namespace Assets.Scripts.GridSystem;

public class GridManager : Singleton<GridManager>
{
	public static GridPathfinder PathFinder;

	public override void ManagerStart()
	{
		base.ManagerStart();
		PathFinder = new GridPathfinder();
		Singleton<MonoBehaviourListener>.Instance.SetType(MonoBehaviourType.GridManager);
	}

	private new void OnApplicationQuit()
	{
		GameManager.GameState = GameState.None;
	}
}
