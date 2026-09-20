using Assets.Scripts.GridSystem;

namespace Assets.Scripts.Objects.Items;

public class AccessCard : Item
{
	public override int GetAccess => CustomColor.Bit;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractColor, CustomColor.Index);
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (GameManager.IsValidColor(index) && GameManager.RunSimulation && ColorState != index)
		{
			OnServer.Interact(base.InteractColor, index);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Color)
		{
			SetCustomColor(ColorState);
		}
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		SetCustomColor(ColorState);
	}
}
