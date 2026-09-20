using System;
using System.Net;

namespace Assets.Scripts.OpenNat;

public class Mapping
{
	private DateTime _expiration;

	private int _lifetime;

	internal MappingLifetime LifetimeType { get; set; }

	public string Description { get; internal set; }

	public IPAddress PrivateIP { get; internal set; }

	public Protocol Protocol { get; internal set; }

	public int PrivatePort { get; internal set; }

	public IPAddress PublicIP { get; internal set; }

	public int PublicPort { get; internal set; }

	public int Lifetime
	{
		get
		{
			return _lifetime;
		}
		internal set
		{
			switch (value)
			{
			case int.MaxValue:
				LifetimeType = MappingLifetime.Session;
				_lifetime = 600;
				_expiration = DateTime.UtcNow.AddSeconds(_lifetime);
				break;
			case 0:
				LifetimeType = MappingLifetime.Permanent;
				_lifetime = 0;
				_expiration = DateTime.UtcNow;
				break;
			default:
				LifetimeType = MappingLifetime.Manual;
				_lifetime = value;
				_expiration = DateTime.UtcNow.AddSeconds(_lifetime);
				break;
			}
		}
	}

	public DateTime Expiration
	{
		get
		{
			return _expiration;
		}
		internal set
		{
			_expiration = value;
			_lifetime = (int)(_expiration - DateTime.UtcNow).TotalSeconds;
		}
	}

	internal Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort)
		: this(protocol, privateIP, privatePort, publicPort, 0, "Stationeers")
	{
	}

	public Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort, int lifetime, string description)
	{
		Guard.IsInRange(privatePort, 0, 65535, "privatePort");
		Guard.IsInRange(publicPort, 0, 65535, "publicPort");
		Guard.IsInRange(lifetime, 0, int.MaxValue, "lifetime");
		Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
		Guard.IsNotNull(privateIP, "privateIP");
		Protocol = protocol;
		PrivateIP = privateIP;
		PrivatePort = privatePort;
		PublicIP = IPAddress.None;
		PublicPort = publicPort;
		Lifetime = lifetime;
		Description = description;
	}

	public Mapping(Protocol protocol, int privatePort, int publicPort)
		: this(protocol, IPAddress.None, privatePort, publicPort, 0, "Stationeers")
	{
	}

	public Mapping(Protocol protocol, int privatePort, int publicPort, string description)
		: this(protocol, IPAddress.None, privatePort, publicPort, 0, description)
	{
	}

	public Mapping(Protocol protocol, int privatePort, int publicPort, int lifetime, string description)
		: this(protocol, IPAddress.None, privatePort, publicPort, lifetime, description)
	{
	}

	internal Mapping(Mapping mapping)
	{
		PrivateIP = mapping.PrivateIP;
		PrivatePort = mapping.PrivatePort;
		Protocol = mapping.Protocol;
		PublicIP = mapping.PublicIP;
		PublicPort = mapping.PublicPort;
		LifetimeType = mapping.LifetimeType;
		Description = mapping.Description;
		_lifetime = mapping._lifetime;
		_expiration = mapping._expiration;
	}

	public bool IsExpired()
	{
		if (LifetimeType != MappingLifetime.Permanent && LifetimeType != MappingLifetime.ForcedSession)
		{
			return Expiration < DateTime.UtcNow;
		}
		return false;
	}

	internal bool ShoundRenew()
	{
		if (LifetimeType == MappingLifetime.Session)
		{
			return IsExpired();
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (!(obj is Mapping mapping))
		{
			return false;
		}
		if (PublicPort == mapping.PublicPort)
		{
			return PrivatePort == mapping.PrivatePort;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((PublicPort * 397) ^ ((PrivateIP != null) ? PrivateIP.GetHashCode() : 0)) * 397) ^ PrivatePort;
	}

	public override string ToString()
	{
		return string.Format("{0} {1} --> {2}:{3} ({4})", (Protocol == Protocol.Tcp) ? "Tcp" : "Udp", PublicPort, PrivateIP, PrivatePort, Description);
	}
}
