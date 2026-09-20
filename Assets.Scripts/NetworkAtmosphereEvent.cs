using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Networks;

namespace Assets.Scripts;

public class NetworkAtmosphereEvent
{
	private Atmosphere Atmosphere;

	private GasMixture GasMixture;

	private readonly Dictionary<long, float> _refToNetworkVolumeLookup;

	private static Queue<NetworkAtmosphereEvent> NetworkAtmosphereEvents = new Queue<NetworkAtmosphereEvent>(64);

	public static void Clear()
	{
		lock (NetworkAtmosphereEvents)
		{
			NetworkAtmosphereEvents.Clear();
		}
	}

	private static void AddEvent(NetworkAtmosphereEvent newEvent)
	{
		lock (NetworkAtmosphereEvents)
		{
			NetworkAtmosphereEvents.Enqueue(newEvent);
		}
	}

	public static bool TryDequeue(out NetworkAtmosphereEvent next)
	{
		lock (NetworkAtmosphereEvents)
		{
			return NetworkAtmosphereEvents.TryDequeue(out next);
		}
	}

	private NetworkAtmosphereEvent(List<AtmosphericsNetwork> newAtmosphericsNetworks, GasMixture gasMixture)
	{
		GasMixture = gasMixture;
		_refToNetworkVolumeLookup = new Dictionary<long, float>();
		foreach (AtmosphericsNetwork newAtmosphericsNetwork in newAtmosphericsNetworks)
		{
			if (newAtmosphericsNetwork != null && newAtmosphericsNetwork.IsNetworkValid())
			{
				_refToNetworkVolumeLookup.Add(newAtmosphericsNetwork.Atmosphere.ReferenceId, newAtmosphericsNetwork.Atmosphere.Volume.ToFloat());
				newAtmosphericsNetwork.Atmosphere.IsAwaitingEvent = true;
				AtmosphericsController.World.RefreshPendingAtmosphericEvent.Enqueue(newAtmosphericsNetwork);
			}
		}
	}

	private NetworkAtmosphereEvent(List<AtmosphericsNetwork> newAtmosphericsNetworks, Atmosphere fromAtmosphere)
	{
		Atmosphere = fromAtmosphere;
		fromAtmosphere.IsAwaitingEvent = true;
		_refToNetworkVolumeLookup = new Dictionary<long, float>();
		foreach (AtmosphericsNetwork newAtmosphericsNetwork in newAtmosphericsNetworks)
		{
			if (newAtmosphericsNetwork != null && newAtmosphericsNetwork.IsNetworkValid())
			{
				_refToNetworkVolumeLookup.Add(newAtmosphericsNetwork.Atmosphere.ReferenceId, newAtmosphericsNetwork.Atmosphere.Volume.ToFloat());
				newAtmosphericsNetwork.Atmosphere.IsAwaitingEvent = true;
				AtmosphericsController.World.RefreshPendingAtmosphericEvent.Enqueue(newAtmosphericsNetwork);
			}
		}
		AtmosphericsController.World.RefreshPendingAtmosphericEvent.Enqueue(Atmosphere.AtmosphericsNetwork);
	}

	public static void DivideNetworkAtmosphere(List<AtmosphericsNetwork> newAtmosphericsNetworks, GasMixture gasMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new NetworkAtmosphereEvent(newAtmosphericsNetworks, gasMixture));
		}
	}

	public static void DivideNetworkAtmosphere(List<AtmosphericsNetwork> newAtmosphericsNetworks, Atmosphere fromAtmosphere)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new NetworkAtmosphereEvent(newAtmosphericsNetworks, fromAtmosphere));
		}
	}

	private void Apply()
	{
		float num = 0f;
		long key;
		float value;
		foreach (KeyValuePair<long, float> item in _refToNetworkVolumeLookup)
		{
			item.Deconstruct(out key, out value);
			long referenceId = key;
			float num2 = value;
			if (Referencable.Find(referenceId) != null)
			{
				num += num2;
			}
		}
		foreach (KeyValuePair<long, float> item2 in _refToNetworkVolumeLookup)
		{
			item2.Deconstruct(out key, out value);
			long referenceId2 = key;
			float num3 = value;
			GasMixture gasMixture = new GasMixture(GasMixture.IsValid ? GasMixture : Atmosphere.GasMixture);
			gasMixture.Scale(num3 / num);
			Atmosphere atmosphere = Referencable.Find<Atmosphere>(referenceId2);
			if (atmosphere != null)
			{
				atmosphere.GasMixture.Set(gasMixture);
				atmosphere.IsAwaitingEvent = false;
			}
		}
		if (Atmosphere != null)
		{
			Atmosphere.IsAwaitingEvent = false;
		}
	}

	public static void HandleNetworkChangedEvents()
	{
		NetworkAtmosphereEvent next;
		while (TryDequeue(out next))
		{
			next.Apply();
		}
	}
}
