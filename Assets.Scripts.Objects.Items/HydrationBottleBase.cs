using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class HydrationBottleBase : HydrationBase
{
	public GameObject WaterVisualizer;

	private float prevQuantity = float.MaxValue;

	public override void Start()
	{
		base.Start();
		UpdateVisualizer();
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (Math.Abs(prevQuantity - base.Quantity) > 0.01f)
		{
			prevQuantity = base.Quantity;
			UpdateVisualizer();
		}
	}

	public async UniTaskVoid WaitUpdateVisualizer()
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		UpdateVisualizer();
	}

	public void UpdateVisualizer()
	{
		if ((bool)WaterVisualizer)
		{
			WaterVisualizer.transform.localScale = new Vector3(WaterVisualizer.transform.localScale.x, Mathf.Min(base.Quantity, 1f), WaterVisualizer.transform.localScale.z);
		}
	}

	public override void AddLiquidToThing(float quantity)
	{
		base.AddLiquidToThing(quantity);
		base.Quantity += Mathf.Min(quantity, MaxQuantity);
		WaitUpdateVisualizer().Forget();
	}

	public override void OnStateChanged()
	{
		UpdateVisualizer();
	}
}
