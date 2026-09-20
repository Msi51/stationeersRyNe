using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Vehicles;

public readonly struct RoverUpdate
{
	private readonly Vector3[] _wheelRotations;

	public Vector3 GetWheelLocalRotation(int index)
	{
		if (index >= _wheelRotations.Length)
		{
			return Vector3.zero;
		}
		return _wheelRotations[index];
	}

	public RoverUpdate(WheeledBase rover)
	{
		_wheelRotations = new Vector3[rover.Wheels.Count];
		for (int i = 0; i < rover.Wheels.Count; i++)
		{
			Vector3 vector = Vector3.zero;
			if (rover.Wheels[i] != null && (bool)rover.Wheels[i].WheelTransform)
			{
				vector = rover.Wheels[i].WheelTransform.localEulerAngles;
			}
			_wheelRotations[i] = vector;
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		Vector3[] wheelRotations = _wheelRotations;
		foreach (Vector3 value in wheelRotations)
		{
			writer.WriteVector3Half(value);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public void Read(RocketBinaryReader reader)
	{
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			_wheelRotations[i] = reader.ReadVector3Half();
		}
	}
}
