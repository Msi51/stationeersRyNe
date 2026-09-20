using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public interface IUmbilical : IRocketComponent, IReferencable, IEvaluable
{
	Thing AsThing { get; }

	int PartnerDistance { get; set; }

	Vector3 FirstPartnerSearchPosition { get; }

	UmbilicalType UmbilicalType { get; }

	bool IsOpen { get; }

	bool IsCompatibleWith(IUmbilical other);

	void SetPartner(IUmbilical partner);

	void PartnerRemoved();

	IndestructableDamageState GetDamageState();

	void RetractUmbilical();

	void ExtendUmbilical();
}
