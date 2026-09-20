using System.Threading;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Structures;

public class TutorialSpawnPoint : SmallGrid, ISmartRotatable
{
	[SerializeField]
	private Transform _spawnPosition;

	private CancellationTokenWrapper _cancellation = new CancellationTokenWrapper();

	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			Human.OnHumanCreated += PositionPlayer;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsCursor)
		{
			Human.OnHumanCreated -= PositionPlayer;
			_cancellation.Cancel();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		PositionAllPlayers();
	}

	private void PositionPlayer(Entity entity)
	{
		if (entity is Human human)
		{
			_cancellation.CancelAndInitialize();
			WaitThenPositionPlayer(human, _cancellation.Token).Forget();
		}
	}

	private void PositionAllPlayers()
	{
		_cancellation.CancelAndInitialize();
		WaitThenPositionAllPlayers(_cancellation.Token).Forget();
	}

	private async UniTaskVoid WaitThenPositionPlayer(Human human, CancellationToken cancellationToken)
	{
		while ((object)InventoryManager.ParentHuman == null)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			await UniTask.NextFrame(cancellationToken);
		}
		await UniTask.NextFrame(cancellationToken);
		PositionHuman(human);
	}

	private async UniTaskVoid WaitThenPositionAllPlayers(CancellationToken cancellationToken)
	{
		while ((object)InventoryManager.ParentHuman == null)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			await UniTask.NextFrame(cancellationToken);
		}
		await UniTask.NextFrame(cancellationToken);
		foreach (Human allHuman in Human.AllHumans)
		{
			PositionHuman(allHuman);
		}
	}

	private void PositionHuman(Human human)
	{
		if (human.HasAuthority && !(InventoryManager.ParentHuman != human))
		{
			Vector3 vector = _spawnPosition.position;
			float y = _spawnPosition.eulerAngles.y;
			human.ForceSetPosition(vector);
			CameraController.Instance.RotationY = y;
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
