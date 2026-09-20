using TraderUI;
using UnityEngine;

namespace Assets.Scripts.Objects.Motherboards.Comms;

public class CommsUIPanel : MonoBehaviour
{
	[SerializeField]
	private TabWell _tabWell;

	[SerializeField]
	private ContactsTab _contactsTab;

	[SerializeField]
	private LogTab _logTab;

	public ContactsTab ContactsTab => _contactsTab;

	public void Initialise(CommsMotherboard motherboard)
	{
		_contactsTab.Initialise(motherboard);
		ShowContactsPanel();
	}

	public void MotherboardDestroyed()
	{
		_contactsTab.Clear();
	}

	public void MotherboardInserted()
	{
		_tabWell.Select(0);
		_contactsTab.InitialiseToggleGroups();
	}

	private void TabSelected(int index)
	{
		switch (index)
		{
		case 0:
			ShowContactsPanel();
			break;
		case 1:
			ShowLogPanel();
			break;
		}
	}

	private void ShowContactsPanel()
	{
		_contactsTab.Show();
		_logTab.Hide();
	}

	private void ShowLogPanel()
	{
		_logTab.Show();
		_contactsTab.Hide();
	}

	private void OnEnable()
	{
		_tabWell.TabSelected.AddListener(TabSelected);
	}

	private void OnDisable()
	{
		_tabWell.TabSelected.RemoveListener(TabSelected);
	}
}
