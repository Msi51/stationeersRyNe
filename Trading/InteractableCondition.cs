using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class InteractableCondition : ConditionData
{
	[XmlAttribute("Action")]
	public InteractableType InteractableType;

	[XmlAttribute("State")]
	public int InteractableState;

	public override string DebugName => $"Action {InteractableType} {InteractableState}";

	public override int GetChecksum()
	{
		return (int)(((((uint)base.GetChecksum() ^ (uint)InteractableType) * 41) ^ (uint)InteractableState) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Thing thing)
		{
			Interactable interactable = thing.GetInteractable(InteractableType);
			if (interactable != null && interactable.State == InteractableState)
			{
				flag = true;
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
		stringBuilder.AppendLine(GameStrings.InteractableActionToState.AsString(Localization.GetName(InteractableType), StringManager.Get(InteractableState)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
