using Assets.Scripts.Inventory;
using TMPro;

namespace Assets.Scripts.Objects;

public class Book : Item
{
	public string[] BookContents;

	[ReadOnly]
	public int PageCount;

	private TextMeshProUGUI _titleText;

	private TextMeshProUGUI _pageCountText;

	public override void Awake()
	{
		base.Awake();
		if (!(InventoryManager.Instance?.ReadingPanel == null))
		{
			_titleText = InventoryManager.Instance.ReadingPanel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
			_pageCountText = InventoryManager.Instance.ReadingPanel.transform.Find("PageCount").GetComponent<TextMeshProUGUI>();
		}
	}

	private void UpdateBook()
	{
		if (HasAuthority)
		{
			_titleText.text = DisplayName;
			_pageCountText.text = PageCount + " / " + (BookContents.Length - 1);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Open:
		case InteractableType.OnOff:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			interactable.State = ((interactable.State != 1) ? 1 : 0);
			if (RootParent.HasAuthority)
			{
				InventoryManager.Instance.ReadingPanel.SetActive(interactable.State == 1);
			}
			UpdateBook();
			return delayedActionInstance.Succeed();
		case InteractableType.Button1:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (Interactables[0].State != 1)
			{
				return delayedActionInstance.Succeed();
			}
			PageCount = ((PageCount < BookContents.Length - 1) ? (PageCount + 1) : 0);
			UpdateBook();
			return delayedActionInstance.Succeed();
		case InteractableType.Button2:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (Interactables[0].State != 1)
			{
				return delayedActionInstance.Succeed();
			}
			PageCount = ((PageCount <= 0) ? (BookContents.Length - 1) : (PageCount - 1));
			UpdateBook();
			return delayedActionInstance.Succeed();
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		if (InventoryManager.Instance.ReadingPanel.activeSelf)
		{
			Interactables[0].State = 0;
			InventoryManager.Instance.ReadingPanel.SetActive(value: false);
		}
	}
}
