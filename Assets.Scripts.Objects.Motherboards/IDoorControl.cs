using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

public interface IDoorControl
{
	bool IsTriggered { get; }

	void SetMotherboards(bool isTriggered);

	void OnLinkWithBoard(Motherboard motherboard);

	void OnUnlinkWithBoard(Motherboard motherboard);
}
