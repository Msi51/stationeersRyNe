using System.Xml.Serialization;
using UnityEngine;

namespace TerrainSystem;

public abstract class SeekData
{
	public const int DEFAULT_ATTEMPTS = 24;

	public const int MAX_ATTEMPTS = 32;

	private const string ATTEMPTS_NAME = "Attempts";

	[XmlAttribute("Attempts")]
	public int Attempts = 24;

	public int GetMaxAttempts()
	{
		return Mathf.Min(Attempts, 32);
	}
}
