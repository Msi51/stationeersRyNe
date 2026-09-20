using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI.HelperHints;

namespace Trading;

public class TraderContactCondition : ConditionData
{
	[XmlAttribute("IsResolved")]
	public bool IsResolved;

	[XmlAttribute("IsContacted")]
	public bool IsContacted;

	[XmlAttribute("IsLanded")]
	public bool IsLanded;

	public override string DebugName
	{
		get
		{
			string text = (IsResolved ? "Resolved" : "");
			string text2 = (IsContacted ? "Contacted" : "");
			string text3 = (IsLanded ? "Landed" : "");
			return "Contact " + text + " " + text2 + " " + text3;
		}
	}

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (IsResolved ? 1 : 0)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is TraderContact traderContact)
		{
			if (IsContacted)
			{
				flag = traderContact.Contacted;
			}
			if (IsLanded)
			{
				flag = traderContact.ConnectedPad != null;
			}
		}
		else if (t is SatelliteDish satelliteDish)
		{
			foreach (ScannedContactData scannedContactDatum in satelliteDish.DishScannedContacts.ScannedContactData)
			{
				if (IsResolved)
				{
					flag = scannedContactDatum.CurrentTimeTillResolve <= 0f;
				}
				if (IsContacted)
				{
					flag = scannedContactDatum.Contact.Contacted;
				}
				if (IsLanded)
				{
					flag = scannedContactDatum.Contact.ConnectedPad != null;
				}
				if (flag)
				{
					break;
				}
			}
		}
		else if (t is CommsMotherboard commsMotherboard && commsMotherboard.SelectedDish != null)
		{
			foreach (ScannedContactData scannedContactDatum2 in commsMotherboard.SelectedDish.DishScannedContacts.ScannedContactData)
			{
				if (IsResolved)
				{
					flag = scannedContactDatum2.CurrentTimeTillResolve <= 0f;
				}
				if (IsContacted)
				{
					flag = scannedContactDatum2.Contact.Contacted;
				}
				if (IsLanded)
				{
					flag = scannedContactDatum2.Contact.ConnectedPad != null;
				}
				if (flag)
				{
					break;
				}
			}
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (IsResolved)
		{
			GameStrings.TraderContactCondition.AppendFormat(stringBuilder, GameStrings.ResolveAction);
		}
		if (IsContacted)
		{
			GameStrings.TraderContactCondition.AppendFormat(stringBuilder, GameStrings.InterrogateAction);
		}
		if (IsLanded)
		{
			GameStrings.TraderContactCondition.AppendFormat(stringBuilder, GameStrings.LandAction);
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
