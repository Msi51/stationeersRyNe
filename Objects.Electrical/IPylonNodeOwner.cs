using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Objects.Electrical;

public interface IPylonNodeOwner
{
	List<PylonNode> Nodes { get; set; }

	long GetRefId();

	Structure AsStructure();
}
