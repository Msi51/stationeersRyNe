namespace Assets.Scripts.Objects.Items;

public interface IUsed
{
	bool IsEmpty { get; }

	bool OnUseItem(float quantity, Thing onUseThing);

	string ToTooltip();
}
