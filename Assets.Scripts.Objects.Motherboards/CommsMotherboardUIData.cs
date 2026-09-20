using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Objects.Motherboards;

public struct CommsMotherboardUIData
{
	public SatelliteDish SelectedDish;

	public ITraderDestination SelectedLandingPad;

	public ScannedContactUIData[] ScannedContactsDatas;

	public static CommsMotherboardUIData Invalid;

	public bool IsValid
	{
		get
		{
			if (SelectedDish != null)
			{
				return ScannedContactsDatas != null;
			}
			return false;
		}
	}

	public bool Equals(CommsMotherboardUIData other)
	{
		if (!IsValid && !other.IsValid)
		{
			return true;
		}
		if (!IsValid || !other.IsValid)
		{
			return false;
		}
		if (SelectedDish?.ReferenceId == other.SelectedDish?.ReferenceId && SelectedLandingPad?.ReferenceId == other.SelectedLandingPad?.ReferenceId)
		{
			return ScannedContactsDataEquals(other);
		}
		return false;
	}

	private bool ScannedContactsDataEquals(CommsMotherboardUIData other)
	{
		if (ScannedContactsDatas.Length != other.ScannedContactsDatas.Length)
		{
			return false;
		}
		for (int i = 0; i < ScannedContactsDatas.Length; i++)
		{
			if (!ScannedContactsDatas[i].Equals(other.ScannedContactsDatas[i]))
			{
				return false;
			}
		}
		return true;
	}

	public CommsMotherboardUIData(SatelliteDish selectedDish, ITraderDestination selectedLandingPad)
	{
		if (selectedDish == null || selectedDish.DishScannedContacts == null)
		{
			SelectedDish = null;
			SelectedLandingPad = null;
			ScannedContactsDatas = null;
			return;
		}
		SelectedDish = selectedDish;
		SelectedLandingPad = selectedLandingPad;
		ScannedContactsDatas = new ScannedContactUIData[selectedDish.DishScannedContacts.ScannedContactData.Count];
		for (int i = 0; i < SelectedDish.DishScannedContacts.ScannedContactData.Count; i++)
		{
			TraderContact contact = SelectedDish.DishScannedContacts.ScannedContactData[i].Contact;
			ScannedContactsDatas[i] = new ScannedContactUIData(SelectedDish, contact);
		}
	}
}
