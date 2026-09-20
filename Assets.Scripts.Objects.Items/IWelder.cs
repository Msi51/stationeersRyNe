using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public interface IWelder : IConstructor, IUsed
{
	void SendStartWelding();

	void SendStopWelding();

	void SetSparks(Vector3 position);

	void ResetSparks();
}
