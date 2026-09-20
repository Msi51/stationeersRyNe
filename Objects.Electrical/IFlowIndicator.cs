namespace Objects.Electrical;

public interface IFlowIndicator
{
	FlowIndicator FlowIndicator { get; set; }

	FlowIndicatorState FlowIndicatorStatus { get; }
}
