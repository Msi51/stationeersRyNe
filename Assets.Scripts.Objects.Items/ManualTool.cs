using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ManualTool : Tool
{
	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		string text = InteractableType.Activate.ToString();
		foreach (InteractableState state in savedData.States)
		{
			if (Animator.StringToHash(state.StateName) == Animator.StringToHash(text))
			{
				state.State = 0;
			}
		}
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		Activate = 0;
	}

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		Thing.Interact(base.InteractActivate, 1);
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		Thing.Interact(base.InteractActivate, 0);
	}
}
