using UnityEngine;

namespace Assets.Scripts.OpenNat;

internal sealed class Finalizer
{
	~Finalizer()
	{
		Debug.Log("Closing ports opened in this session");
		NatDiscoverer.ReleaseSessionMappings();
	}
}
