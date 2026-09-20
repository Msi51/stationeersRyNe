using Assets.Scripts.GridSystem;

namespace Assets.Scripts.Objects.Structures;

public class Diode : WallLight
{
	protected override bool NeverCastShadows => true;

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
			SetCustomColor(ColorState, OnOff && Powered);
		}
		if (GameManager.GameState == GameState.Running && interactable.Action == InteractableType.Powered)
		{
			SetCustomColor(OnOff && Powered);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractColor, CustomColor.Index);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
	}
}
