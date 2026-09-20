using Assets.Scripts.Objects;
using Assets.Scripts.UI;

public class HandInfoContainer : GameBase
{
	public UiComponentRenderer HandSwapKeyPanel;

	public UiComponentRenderer HandHotKeyPanel;

	public HotkeyGrid SlotHints;

	public UiComponentRenderer SlotHintInteractable;

	public UiComponentRenderer SlotHintHasStorage;

	public HandInsert HandToggleOn;

	public HandInsert HandSecondaryAction;

	public HandInsert HandPrecisionPlace;

	public HandInsert HandThrow;

	public DynamicThing ThingInHand;
}
