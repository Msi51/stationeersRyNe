using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArm : GameBase
{
	[SerializeField]
	private Transform _pivot1;

	[SerializeField]
	private Transform _pivot2;

	[SerializeField]
	private Transform _pivot3;

	[SerializeField]
	private Transform _pivot4;

	private Vector3 _pivot1Up = new Vector3(90f, 0f, 0f);

	private Vector3 _pivot2Up = new Vector3(270f, 0f, 0f);

	private Vector3 _pivot3Up = new Vector3(270f, 0f, 0f);

	private Vector3 _pivot4Up = new Vector3(90f, 0f, 0f);

	private Vector3 _pivot1Down = new Vector3(30f, 0f, 0f);

	private Vector3 _pivot2Down = new Vector3(330f, 0f, 0f);

	private Vector3 _pivot3Down = new Vector3(330f, 0f, 0f);

	private Vector3 _pivot4Down = new Vector3(30f, 0f, 0f);

	public async UniTask AnimateDown(CancellationToken cancellationToken)
	{
		await AnimateAsync(inverse: false, cancellationToken);
	}

	public async UniTask AnimateUp(CancellationToken cancellationToken)
	{
		await AnimateAsync(inverse: true, cancellationToken);
	}

	private void Awake()
	{
		Animate(0f);
	}

	public void Animate(float value)
	{
		_pivot1.localRotation = Quaternion.Euler(Vector3.Lerp(_pivot1Up, _pivot1Down, value));
		_pivot2.localRotation = Quaternion.Euler(Vector3.Lerp(_pivot2Up, _pivot2Down, value));
		_pivot3.localRotation = Quaternion.Euler(Vector3.Lerp(_pivot3Up, _pivot3Down, value));
		_pivot4.localRotation = Quaternion.Euler(Vector3.Lerp(_pivot4Up, _pivot4Down, value));
	}

	private async UniTask AnimateAsync(bool inverse, CancellationToken cancellationToken)
	{
		float current = 0f;
		float target = 1f;
		while (current < target && !cancellationToken.IsCancellationRequested)
		{
			Animate(inverse ? (1f - current) : current);
			current += Time.deltaTime;
			await UniTask.Yield(cancellationToken);
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			current = target;
			Animate(inverse ? (1f - current) : current);
		}
	}

	public void SetZeroRotation()
	{
		_pivot1.localRotation = Quaternion.identity;
		_pivot2.localRotation = Quaternion.identity;
		_pivot3.localRotation = Quaternion.identity;
		_pivot4.localRotation = Quaternion.identity;
	}
}
