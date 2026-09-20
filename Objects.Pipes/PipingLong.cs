using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Pipes;

public class PipingLong : Piping
{
	[SerializeField]
	private int _length;

	private static List<Vector3> LocalGridRegistrationPoints3 = new List<Vector3>
	{
		new Vector3(-0.09999999f, -0.09999982f, -0.2640121f),
		new Vector3(-0.09999999f, 0.09999982f, -0.2640121f),
		new Vector3(0.09999999f, -0.09999982f, -0.2640121f),
		new Vector3(0.09999999f, 0.09999982f, -0.2640121f),
		new Vector3(-0.09999999f, -0.09999982f, 1.264012f),
		new Vector3(-0.09999999f, 0.09999982f, 1.264012f),
		new Vector3(0.09999999f, -0.09999982f, 1.264012f),
		new Vector3(0.09999999f, 0.09999982f, 1.264012f),
		new Vector3(-0.09999999f, -0.09999982f, 0.5f),
		new Vector3(-0.09999999f, 0.09999982f, 0.5f),
		new Vector3(0.09999999f, -0.09999982f, 0.5f),
		new Vector3(0.09999999f, 0.09999982f, 0.5f)
	};

	private static List<Vector3> LocalGridRegistrationPoints5 = new List<Vector3>
	{
		new Vector3(-0.09999999f, -0.09999982f, -0.2640121f),
		new Vector3(-0.09999999f, 0.09999982f, -0.2640121f),
		new Vector3(0.09999999f, -0.09999982f, -0.2640121f),
		new Vector3(0.09999999f, 0.09999982f, -0.2640121f),
		new Vector3(-0.09999999f, -0.09999982f, 2.264012f),
		new Vector3(-0.09999999f, 0.09999982f, 2.264012f),
		new Vector3(0.09999999f, -0.09999982f, 2.264012f),
		new Vector3(0.09999999f, 0.09999982f, 2.264012f),
		new Vector3(-0.09999999f, -0.09999982f, 1f),
		new Vector3(-0.09999999f, 0.09999982f, 1f),
		new Vector3(0.09999999f, -0.09999982f, 1f),
		new Vector3(0.09999999f, 0.09999982f, 1f)
	};

	private static List<Vector3> LocalGridRegistrationPoints10 = new List<Vector3>
	{
		new Vector3(-0.09999999f, -0.09999982f, -0.2640119f),
		new Vector3(-0.09999999f, 0.09999982f, -0.2640119f),
		new Vector3(0.09999993f, -0.09999982f, -0.2640119f),
		new Vector3(0.09999993f, 0.09999982f, -0.2640119f),
		new Vector3(-0.09999999f, -0.09999982f, 4.764012f),
		new Vector3(-0.09999999f, 0.09999982f, 4.764012f),
		new Vector3(0.09999993f, -0.09999982f, 4.764012f),
		new Vector3(0.09999993f, 0.09999982f, 4.764012f),
		new Vector3(-0.09999999f, -0.09999982f, 2.25f),
		new Vector3(-0.09999999f, 0.09999982f, 2.25f),
		new Vector3(0.09999993f, -0.09999982f, 2.25f),
		new Vector3(0.09999993f, 0.09999982f, 2.25f),
		new Vector3(-0.09999999f, -0.09999982f, 0.9929941f),
		new Vector3(-0.09999999f, 0.09999982f, 0.9929941f),
		new Vector3(0.09999993f, -0.09999982f, 0.9929941f),
		new Vector3(0.09999993f, 0.09999982f, 0.9929941f),
		new Vector3(-0.09999999f, -0.09999982f, 3.507006f),
		new Vector3(-0.09999999f, 0.09999982f, 3.507006f),
		new Vector3(0.09999993f, -0.09999982f, 3.507006f),
		new Vector3(0.09999993f, 0.09999982f, 3.507006f)
	};

	protected override void RegisterCurrentGrids()
	{
		lock (base.CurrentGrids)
		{
			base.CurrentGrids.Clear();
			switch (_length)
			{
			case 3:
			{
				foreach (Vector3 item in LocalGridRegistrationPoints3)
				{
					RegisterLocalPoint(item);
				}
				break;
			}
			case 5:
			{
				foreach (Vector3 item2 in LocalGridRegistrationPoints5)
				{
					RegisterLocalPoint(item2);
				}
				break;
			}
			case 10:
			{
				foreach (Vector3 item3 in LocalGridRegistrationPoints10)
				{
					RegisterLocalPoint(item3);
				}
				break;
			}
			}
		}
	}

	private void RegisterLocalPoint(Vector3 localPoint)
	{
		Vector3 vector = RocketMath.TransformPoint(localPoint, base.Position, Rotation);
		RegisterGrid(vector);
	}
}
