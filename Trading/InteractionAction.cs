using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;

namespace Trading;

public class InteractionAction : DelayedAction
{
	private const string TYPE_ATTRIBUTE = "Type";

	[XmlAttribute("Type")]
	public InteractableType Type;

	private const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public int Value;

	[XmlIgnore]
	private TimeLength _timeLength;

	private const string X_ELEMENT_NAME = "Interaction";

	public override string XElementName => "Interaction";

	public InteractionAction()
	{
	}

	public InteractionAction(Interactable interactable)
	{
		Type = interactable.Action;
		Value = interactable.State;
	}

	public override void Initialize()
	{
		base.Initialize();
		_timeLength = TimeLength.FromSeconds(Delay?.GetTotalSeconds() ?? 0.0);
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Thing { IsBeingDestroyed: false } thing)
		{
			Interactable interactable = thing.GetInteractable(Type);
			bool skipAnimation = GameManager.GameState != GameState.Running;
			interactable?.Interact(Value, skipAnimation);
		}
		return false;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		return false;
	}

	public override int GetChecksum()
	{
		return (int)(((((uint)Type ^ (uint)(Value * 1000)) * 41) ^ (uint)(Delay.GetHashCode() * 10000)) * 41);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XElement parentElement2 = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
		XDocumentHelper.SetAttribute(parentElement2, "Type", Type.ToString());
		XDocumentHelper.SetAttribute(parentElement2, "Value", Value.ToString());
		Delay?.Add(ref parentElement2, "TimeSpan");
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (Delay == null)
		{
			stringBuilder.AppendLine(EnumCollections.InteractableTypes.GetName(Type) + " is " + StringManager.Get(Value));
			return;
		}
		stringBuilder.AppendLine(EnumCollections.InteractableTypes.GetName(Type) + " is " + StringManager.Get(Value) + " after " + _timeLength);
	}
}
