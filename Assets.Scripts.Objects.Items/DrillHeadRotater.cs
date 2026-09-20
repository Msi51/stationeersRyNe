using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class DrillHeadRotater : GameBase
{
	public Tool ParentTool;

	private bool _initialized;

	private Task _rotateHeads;

	public void Awake()
	{
		Initialize();
	}

	public void Start()
	{
		Initialize();
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			ParentTool.OnInteractable += CheckState;
		}
	}

	private async UniTask RotateHeads()
	{
		while (ParentTool != null && ParentTool.Powered)
		{
			Transform.Rotate(Vector3.right, Time.deltaTime * 180f, Space.Self);
			await UniTask.NextFrame();
		}
		_rotateHeads = null;
	}

	private void CheckState()
	{
		if (ParentTool.Powered && (_rotateHeads == null || _rotateHeads.IsCompleted))
		{
			_rotateHeads = RotateHeads().AsTask();
		}
	}
}
