using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.Util;
using Trading;

namespace Assets.Scripts.UI.HelperHints;

public class HelperHintBuilder
{
	private readonly StringBuilder sb = new StringBuilder(4096);

	private readonly Stack<LogicOperator> branchStack = new Stack<LogicOperator>(8);

	private int indent;

	private const float SubtextSizePercent = 80f;

	private const float SmallSizePercent = 80f;

	private const int bulletHighlightDepth = -1;

	private const string HexWhite = "#ebebeb";

	private const string HexLightGrey = "#bcbcbc";

	private const string HexMedGrey = "#969696";

	private const string HexDarkGrey = "#808080";

	private const string HexYellow = "#ffeb00";

	private const string HexOrange = "#ff8000";

	private const string HexGreen = "#00ff00";

	private const string HexRed = "#ff0000";

	private const string HexTurquoise = "#14cdde";

	private const string HexPink = "#f603c9";

	private const string HexPurple = "#8d65e5";

	private bool sectionIsCompleted;

	private string HeaderColor
	{
		get
		{
			if (!sectionIsCompleted)
			{
				return "#ebebeb";
			}
			return "#808080";
		}
	}

	private string TextColor
	{
		get
		{
			if (!sectionIsCompleted)
			{
				return "#bcbcbc";
			}
			return "#808080";
		}
	}

	private string InfoColor => "#969696";

	private string BranchColor
	{
		get
		{
			if (!sectionIsCompleted)
			{
				return "#ebebeb";
			}
			return "#808080";
		}
	}

	public bool ShowDismissed { get; set; }

	public void Clear()
	{
		sb.Clear();
		indent = 0;
		branchStack.Clear();
	}

	public string Build()
	{
		return sb.ToString();
	}

	private bool HasMultipleVisibleConditions(ConditionDataCollection conditionDataCollection)
	{
		int num = 0;
		foreach (ConditionData condition in conditionDataCollection.Conditions)
		{
			if (!condition.Hidden)
			{
				num++;
			}
			if (num > 1)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasMultipleVisibleConditions(ObjectiveConditionCollection objectiveConditionCollection)
	{
		int num = 0;
		foreach (ConditionData condition in objectiveConditionCollection.Conditions)
		{
			if (!condition.Hidden)
			{
				num++;
			}
			if (num > 1)
			{
				return true;
			}
		}
		return false;
	}

	private void BeginIndent()
	{
		indent++;
		sb.BeginIndent(indent);
	}

	private void EndIndent()
	{
		sb.EndIndent();
		indent--;
	}

	private void BeginBranch(LogicOperator logicOperator)
	{
		sb.Newline();
		Bullet(forBranch: true);
		StringBuilder stringBuilder = sb;
		string branchColor = BranchColor;
		stringBuilder.AppendBranchColorText(branchColor, logicOperator switch
		{
			LogicOperator.Any => GameStrings.TradeOperatorAny.DisplayString, 
			LogicOperator.All => GameStrings.TradeOperatorAll.DisplayString, 
			LogicOperator.None => GameStrings.TradeOperatorNone.DisplayString, 
			_ => string.Empty, 
		});
		BeginIndent();
		branchStack.Push(logicOperator);
	}

	private void EndBranch()
	{
		branchStack.Pop();
		EndIndent();
	}

	private void Bullet(bool forBranch = false)
	{
		if (forBranch && branchStack.Count > 0)
		{
			switch (branchStack.Peek())
			{
			case LogicOperator.All:
				sb.AppendColorText(BranchColor, "• ");
				return;
			case LogicOperator.Any:
				sb.AppendColorText(BranchColor, "• ");
				return;
			case LogicOperator.None:
				sb.AppendColorText(BranchColor, "• ");
				return;
			}
		}
		if (indent <= -1)
		{
			sb.AppendColorText("#ffeb00", "• ");
		}
		else
		{
			sb.Append("• ");
		}
	}

	private void BeginObjective(HelperHintViewModel viewModel)
	{
		indent = 0;
		sectionIsCompleted = viewModel.Completed;
		sb.AppendGroupHeader(viewModel.Expanded, viewModel.Completed || viewModel.Dismissed, HeaderColor, viewModel.Title, viewModel.ExpandId, viewModel.DismissId);
		if (viewModel.Completed && Localization.CurrentLanguage != LanguageCode.CN)
		{
			sb.Append("<s>");
		}
		if (viewModel.Expanded)
		{
			if (!string.IsNullOrWhiteSpace(viewModel.Info))
			{
				AppendInfo(viewModel.Info);
			}
			sb.HalfNewline();
		}
		sb.BeginColor(TextColor);
	}

	private void EndObjective(HelperHintViewModel viewModel)
	{
		sb.EndColor();
		if (viewModel.Completed && Localization.CurrentLanguage != LanguageCode.CN)
		{
			sb.Append("</s>");
		}
		if (viewModel.Expanded)
		{
			sb.DoubleNewline();
		}
		else
		{
			sb.OneAndHalfNewline();
		}
		indent = 0;
	}

	private void AppendInfo(string text)
	{
		BeginIndent();
		sb.OneAndHalfNewline();
		sb.AppendColorText(InfoColor, text, italic: true);
		EndIndent();
	}

	private void AppendBranch(ObjectiveConditionCollection conditionCollection)
	{
		bool flag = HasMultipleVisibleConditions(conditionCollection);
		if (flag)
		{
			BeginBranch(conditionCollection.LogicOperator);
		}
		foreach (ObjectiveConditionCollection conditionCollection2 in conditionCollection.ConditionCollections)
		{
			if (!conditionCollection2.Hidden)
			{
				AppendBranch(conditionCollection2);
			}
		}
		foreach (ConditionData condition in conditionCollection.Conditions)
		{
			if (!condition.Hidden)
			{
				sb.Newline();
				Bullet(flag);
				condition.AppendHelperHint(this);
			}
		}
		if (flag)
		{
			EndBranch();
		}
	}

	private void AppendBranch(ConditionDataCollection conditionDataCollection)
	{
		bool flag = HasMultipleVisibleConditions(conditionDataCollection);
		if (flag)
		{
			BeginBranch(conditionDataCollection.LogicOperator);
		}
		foreach (ConditionData condition in conditionDataCollection.Conditions)
		{
			if (!condition.Hidden)
			{
				sb.Newline();
				Bullet(flag);
				condition.AppendHelperHint(this);
			}
		}
		AppendList(conditionDataCollection.ConditionCollections);
		if (flag)
		{
			EndBranch();
		}
	}

	private void AppendList(List<ObjectiveConditionCollection> objectiveConditionCollections)
	{
		if (objectiveConditionCollections == null || objectiveConditionCollections.Count <= 0)
		{
			return;
		}
		BeginIndent();
		foreach (ObjectiveConditionCollection objectiveConditionCollection in objectiveConditionCollections)
		{
			if (!objectiveConditionCollection.Hidden)
			{
				AppendBranch(objectiveConditionCollection);
			}
		}
		EndIndent();
	}

	private void AppendList(List<ConditionDataCollection> conditionCollections)
	{
		if (conditionCollections == null || conditionCollections.Count <= 0)
		{
			return;
		}
		BeginIndent();
		if (indent > 1)
		{
			sb.BeginSize(80f);
		}
		foreach (ConditionDataCollection conditionCollection in conditionCollections)
		{
			if (!conditionCollection.Hidden)
			{
				AppendBranch(conditionCollection);
			}
		}
		if (indent > 1)
		{
			sb.EndSize();
		}
		EndIndent();
	}

	private void AppendList(List<ConditionData> conditions)
	{
		if (conditions == null || conditions.Count <= 0)
		{
			return;
		}
		BeginIndent();
		if (indent > 1)
		{
			sb.BeginSize(80f);
		}
		foreach (ConditionData condition in conditions)
		{
			if (!condition.Hidden)
			{
				sb.Newline();
				Bullet();
				condition.AppendHelperHint(this);
			}
		}
		if (indent > 1)
		{
			sb.EndSize();
		}
		EndIndent();
	}

	private void AppendList(List<LocalizedStringReference> stringReferences)
	{
		if (stringReferences == null || stringReferences.Count <= 0)
		{
			return;
		}
		BeginIndent();
		foreach (LocalizedStringReference stringReference in stringReferences)
		{
			sb.Newline();
			Bullet();
			sb.Append(stringReference);
		}
		EndIndent();
	}

	public void Append(HelperHintViewModel viewModel)
	{
		if (viewModel.Active && (!viewModel.Dismissed || ShowDismissed))
		{
			BeginObjective(viewModel);
			if (viewModel.Expanded)
			{
				AppendList(viewModel.Notices);
				AppendList(viewModel.ConditionData);
				AppendList(viewModel.ObjectiveConditionCollections);
			}
			EndObjective(viewModel);
		}
	}

	public void Append(ConditionData condition)
	{
	}

	public void Append(SurvivalPropertyCondition survivalPropertyCondition)
	{
		float value = 0f;
		if (!float.IsNaN(survivalPropertyCondition.PercentValue))
		{
			value = survivalPropertyCondition.PercentValue;
		}
		if (!float.IsNaN(survivalPropertyCondition.RatioValue))
		{
			value = survivalPropertyCondition.RatioValue * 100f;
		}
		sb.AppendFormat(GameStrings.SurvivalPropertyCondition, EnumCollections.EntitySurvivalProperty.GetName(survivalPropertyCondition.SurvivalProperty), EnumCollections.CompareOperator.GetName(survivalPropertyCondition.CompareOperator), value.ToStringPercent());
	}

	public void Append(CustomNameCondition condition)
	{
		if (!string.IsNullOrEmpty(condition.Value))
		{
			GameStrings.CustomNameCondition.AppendFormat(sb, condition.Value);
		}
	}

	public void Append(TraderContactCondition condition)
	{
		if (condition.IsResolved)
		{
			sb.AppendFormat(GameStrings.TraderContactCondition, GameStrings.ResolveAction);
		}
		if (condition.IsContacted)
		{
			sb.AppendFormat(GameStrings.TraderContactCondition, GameStrings.InterrogateAction);
		}
		if (condition.IsLanded)
		{
			if (condition.IsContacted)
			{
				sb.Newline();
				Bullet();
			}
			sb.AppendFormat(GameStrings.TraderContactCondition, GameStrings.LandAction);
		}
	}

	public void Append(NetworkCondition condition)
	{
		sb.AppendFormat(GameStrings.BuildNetworkCondition, condition.NetworkType.GetName());
		AppendList(condition.Conditions);
		AppendList(condition.ConditionCollections);
	}

	public void Append(SizeCondition condition)
	{
		sb.AppendFormat(GameStrings.SizeTwoDCondition, condition.CompareOperator.DisplayString(), StringManager.Get(condition.ValueX), StringManager.Get(condition.ValueY));
	}

	public void Append(SpeciesCondition condition)
	{
		sb.Append(GameStrings.HelperHintSpeciesState);
		sb.AppendColorText("yellow", condition.Id.GetName());
	}

	public void Append(ThingPrefabCondition condition)
	{
		if (!Prefab.TryFind(condition.PrefabNameHash, out var thing))
		{
			ConsoleWindow.PrintError("Cannot find prefab " + condition.PrefabName + " for child condition.");
			return;
		}
		sb.AppendFormat(GameStrings.HelperHintCreateThingCondition, thing.PrefabName, Localization.GetName(thing));
		AppendList(condition.Conditions);
		AppendList(condition.ConditionCollections);
	}

	public void Append(ChildItemPrefabCondition condition)
	{
		if (!Prefab.TryFind(condition.PrefabNameHash, out var thing))
		{
			ConsoleWindow.PrintError("Cannot find prefab " + condition.PrefabName + " for child condition.");
			return;
		}
		string name = Localization.GetName(thing);
		if (condition.SlotIndex > 0)
		{
			sb.AppendFormat(GameStrings.HelperHintContainsItemInSlot, thing.PrefabName, name, StringManager.Get(condition.SlotIndex));
		}
		else if (!string.IsNullOrWhiteSpace(condition.SlotId))
		{
			sb.AppendFormat(GameStrings.HelperHintContainsItemInSlot, thing.PrefabName, name, Localization.GetSlotName(condition.SlotId));
		}
		else
		{
			sb.AppendFormat(GameStrings.HelperHintContainsItem, thing.PrefabName, name);
		}
	}

	public void Append(InCellCondition condition)
	{
		sb.AppendFormat(GameStrings.HelperHintInCellCondition, StringManager.Get(condition.Grid));
		AppendList(condition.Conditions);
		AppendList(condition.ConditionCollections);
	}

	public void Append(EntityStateCondition condition)
	{
		sb.Append(GameStrings.HelperHintHumanCondition);
		AppendList(condition.Conditions);
		AppendList(condition.ConditionCollections);
	}

	public void Append(MoleCondition condition)
	{
		sb.AppendFormat(GameStrings.HelperHintContainsComparison, condition.CompareOperator.DisplayString());
		condition.Value.AppendPrefix(sb, "mol", "yellow");
	}

	public void Append(PreSpawnedCondition condition)
	{
		sb.AppendFormat(GameStrings.PreSpawnedCondition, StringManager.Get(condition.WasPreSpawned));
	}

	public void Append(BuildStateCondition condition)
	{
		if (condition.IsCompleted)
		{
			sb.Append(GameStrings.StructureIsCompleted);
		}
		if (condition.CanManufacture)
		{
			if (condition.IsCompleted)
			{
				sb.Append(", ");
			}
			sb.Append(GameStrings.StructureCanManufacture);
		}
		if (condition.MachineTier != MachineTier.Undefined)
		{
			sb.AppendLine(condition.MachineTier.GetName());
		}
	}

	public void Append(DecayCondition condition)
	{
		sb.AppendFormat(GameStrings.HelperHintDecayComparison, condition.CompareOperator.DisplayString());
		condition.Value.AppendPercent(sb, "yellow");
	}

	public void Append(GasCondition condition)
	{
		if (condition.Percent != -1 || condition.Moles != -1 || condition.PartialPressure != -1f)
		{
			string name = Localization.GetName(condition.GasType);
			string arg = condition.CompareOperator.DisplayString();
			if (condition.Percent != -1)
			{
				sb.AppendGasText(name);
				sb.AppendFormat(GameStrings.HelperHintRatioMustBeComparison, arg);
				condition.Percent.AppendPercent(sb, "yellow");
			}
			if (condition.Moles != -1)
			{
				sb.AppendGasText(name);
				sb.AppendFormat(GameStrings.HelperHintRatioMustBeComparison, arg);
				condition.Moles.AppendPrefix(sb, "mol", "yellow");
			}
			if (condition.PartialPressure > 0f)
			{
				sb.Append(GameStrings.HelperHintPartialPressure);
				sb.AppendGasText(name);
				sb.AppendFormat(GameStrings.HelperHintMustBeComparison, arg);
				condition.PartialPressure.AppendPrefix(sb, "kPa", "yellow");
			}
		}
	}

	public void Append(InteractableCondition condition)
	{
		if (condition.InteractableState == 0)
		{
			switch (condition.InteractableType)
			{
			case InteractableType.OnOff:
				GameStrings.InteractableMustBeOff.AppendFormat(sb);
				break;
			case InteractableType.Powered:
				GameStrings.InteractableMustBeUnpowered.AppendFormat(sb);
				break;
			case InteractableType.Open:
				GameStrings.InteractableMustBeClosed.AppendFormat(sb);
				break;
			}
		}
		else if (condition.InteractableState == 1)
		{
			switch (condition.InteractableType)
			{
			case InteractableType.OnOff:
				GameStrings.InteractableMustBeOn.AppendFormat(sb);
				break;
			case InteractableType.Powered:
				GameStrings.InteractableMustBePowered.AppendFormat(sb);
				break;
			case InteractableType.Open:
				GameStrings.InteractableMustBeOpen.AppendFormat(sb);
				break;
			}
		}
		else
		{
			sb.AppendFormat(GameStrings.InteractableActionToState, Localization.GetName(condition.InteractableType), StringManager.Get(condition.InteractableState));
		}
	}

	public void Append(LogicCondition condition)
	{
		sb.Append(condition.LogicType.GetName());
		sb.Append(" ");
		sb.Append(StringManager.Get(condition.Value));
	}

	public void Append(ObjectiveCompleteCondition condition)
	{
		if (condition.ObjectiveId != null)
		{
			sb.AppendFormat(GameStrings.ObjectiveIsCompletedCondition, condition.ObjectiveId);
		}
	}

	public void Append(PercentCondition condition)
	{
		sb.AppendFormat(GameStrings.HelperHintQuantityComparison, condition.CompareOperator.DisplayString());
		condition.Percent.AppendPercent(sb, "yellow");
	}

	public void Append(PressureCondition condition)
	{
		if (!(condition.PressurekPa < 0f))
		{
			sb.AppendFormat(GameStrings.HelperHintPressureCondition, condition.CompareOperator.DisplayString());
			(condition.PressurekPa * 1000f).AppendPrefix(sb, "Pa", "yellow");
		}
	}

	public void Append(QuantityCondition condition)
	{
		sb.AppendFormat(GameStrings.HelperHintQuantityComparison, condition.CompareOperator.DisplayString());
		sb.Append(StringManager.Get(condition.Quantity));
	}

	public void Append(ReagentCondition condition)
	{
		reagentLine("Flour", condition.Flour);
		reagentLine("Milk", condition.Milk);
		reagentLine("Egg", condition.Egg);
		reagentLine("Iron", condition.Iron);
		reagentLine("Gold", condition.Gold);
		reagentLine("Carbon", condition.Carbon);
		reagentLine("Uranium", condition.Uranium);
		reagentLine("Copper", condition.Copper);
		reagentLine("Steel", condition.Steel);
		reagentLine("Hydrocarbon", condition.Hydrocarbon);
		reagentLine("Silver", condition.Silver);
		reagentLine("Nickel", condition.Nickel);
		reagentLine("Lead", condition.Lead);
		reagentLine("Electrum", condition.Electrum);
		reagentLine("Invar", condition.Invar);
		reagentLine("Constantan", condition.Constantan);
		reagentLine("Solder", condition.Solder);
		reagentLine("Plastic", condition.Plastic);
		reagentLine("Silicon", condition.Silicon);
		reagentLine("SalicylicAcid", condition.SalicylicAcid);
		reagentLine("Alcohol", condition.Alcohol);
		reagentLine("Oil", condition.Oil);
		reagentLine("Potato", condition.Potato);
		reagentLine("Tomato", condition.Tomato);
		reagentLine("Fenoxitone", condition.Fenoxitone);
		reagentLine("ColorRed", condition.ColorRed);
		reagentLine("ColorGreen", condition.ColorGreen);
		reagentLine("ColorBlue", condition.ColorBlue);
		reagentLine("ColorYellow", condition.ColorYellow);
		reagentLine("ColorOrange", condition.ColorOrange);
		reagentLine("Pumpkin", condition.Pumpkin);
		reagentLine("Rice", condition.Rice);
		reagentLine("Waspaloy", condition.Waspaloy);
		reagentLine("Stellite", condition.Stellite);
		reagentLine("Inconel", condition.Inconel);
		reagentLine("Hastelloy", condition.Hastelloy);
		reagentLine("Astroloy", condition.Astroloy);
		reagentLine("Cobalt", condition.Cobalt);
		reagentLine("Corn", condition.Corn);
		reagentLine("Wheat", condition.Wheat);
		reagentLine("Biomass", condition.Biomass);
		reagentLine("Soy", condition.Soy);
		reagentLine("Mushroom", condition.Mushroom, lastLine: true);
		void reagentLine(string name, double value, bool lastLine = false)
		{
			if (!(value <= 0.0))
			{
				sb.Append(name);
				sb.Append(" ");
				sb.Append(StringManager.Get(value));
				if (!lastLine)
				{
					sb.Newline();
				}
			}
		}
	}

	public void Append(RoomCondition condition)
	{
		if (condition.RoomType == RoomType.Undefined)
		{
			sb.Append(GameStrings.HelperHintAnyRoomState);
		}
		else
		{
			sb.AppendFormat(GameStrings.HelperHintRoomState, condition.RoomType.GetName());
		}
		if (condition.MinSize > 0 || condition.MaxSize != -1)
		{
			BeginIndent();
			sb.BeginSize(80f);
			sb.BeginColor(InfoColor);
			if (condition.MinSize > 0)
			{
				sb.Newline();
				sb.AppendFormat(GameStrings.RoomMinSizeCondition, StringManager.Get(condition.MinSize));
			}
			if (condition.MaxSize > 0)
			{
				sb.Newline();
				sb.AppendFormat(GameStrings.RoomMaxSizeCondition, StringManager.Get(condition.MaxSize));
			}
			sb.EndColor();
			sb.EndSize();
			EndIndent();
		}
		AppendList(condition.Conditions);
		AppendList(condition.ConditionCollections);
	}
}
