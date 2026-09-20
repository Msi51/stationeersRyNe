using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class SmartRotate
{
	public struct RotationInformation(Permutation permutation, Quaternion rotation)
	{
		public readonly Permutation Permutation = permutation;

		public readonly Quaternion Rotation = rotation;
	}

	public class OrientationEntry
	{
		public readonly int[] EndLocations;

		public readonly RotationInformation OperationToGetToNext;

		public OrientationEntry(int[] endLocations, RotationInformation operationToGetToNext)
		{
			EndLocations = (int[])endLocations.Clone();
			OperationToGetToNext = operationToGetToNext;
		}
	}

	private class LevelOneArrayComparer : IEqualityComparer<int[]>
	{
		public bool Equals(int[] a1, int[] a2)
		{
			if (a1.Length != a2.Length)
			{
				return false;
			}
			for (int i = 0; i < a1.Length; i++)
			{
				if (a1[i] != a2[i])
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(int[] obj)
		{
			int num = 0;
			for (int i = 0; i < obj.Length; i++)
			{
				num ^= obj[i].GetHashCode();
			}
			return num;
		}
	}

	public enum ConnectionType
	{
		Elbow = 0,
		SideOutletElbow = 1,
		Straight = 2,
		Tee = 3,
		SideOutletTee = 4,
		Cross = 5,
		SideOutletCross = 6,
		SixWayCross = 7,
		Exhaustive = 8,
		CrossDistinguishedOppositeEnds = 9,
		ExhaustiveSymmetricEdge = 10,
		ExhaustiveSymmetricCorner = 11,
		FlatCorner = 1024,
		FlatStraight = 1025,
		FlatTee = 1026,
		FlatCross = 1027,
		FlatExhaustive = 1028,
		FaceAllAll = 2048,
		FaceAllY = 2050,
		FaceAllAll2 = 2052,
		FaceAllAll4 = 2053,
		FaceWallY = 2055,
		FaceWallZY = 2059,
		FaceFloorZY = 2065,
		StraightAsymmetric = 3000
	}

	public static Dictionary<ConnectionType, Dictionary<int[], int>> OrientationLookup;

	private static readonly RotationInformation _RotIdentity;

	public static readonly RotationInformation RotX;

	public static readonly RotationInformation RotY;

	public static readonly RotationInformation RotZ;

	private static readonly RotationInformation RotZZ;

	private static readonly RotationInformation _RotXInv;

	private static readonly RotationInformation _RotXInvY;

	private static readonly RotationInformation _RotZInv;

	private static readonly RotationInformation _RotZInvY;

	private static readonly RotationInformation _RotXY;

	private static readonly RotationInformation _RotYZ;

	private static readonly RotationInformation _RotYZZ;

	private static readonly RotationInformation _RotYYZInv;

	private static readonly RotationInformation _RotZY;

	public static readonly RotationInformation Rot2D;

	public static readonly Dictionary<ConnectionType, RotationInformation[]> RotationsList;

	public static readonly Dictionary<ConnectionType, int> NumberOfUniqueOrientationsOf;

	private static readonly int RotateBlueprintHash;

	private static LevelOneArrayComparer arrayComparer;

	private static void _ConstructRotationList(ConnectionType connectionType, int[] initialFacePermutation)
	{
		int num = NumberOfUniqueOrientationsOf[connectionType];
		int[] array = (int[])initialFacePermutation.Clone();
		OrientationLookup[connectionType].Add(array, 0);
		for (int i = 1; i < num; i++)
		{
			array = RotationsList[connectionType][i - 1].Permutation.Permute((int[])array.Clone());
			OrientationLookup[connectionType].Add(array, i);
		}
	}

	private static void _DoRotation(ISmartRotatable rotatable, int[] edgePermutation, RotationInformation[] rotationInformation, int index, bool isForward, Quaternion offset, Vector3 centerOfRotation)
	{
		float angle;
		Vector3 axis;
		if (isForward)
		{
			rotationInformation[index].Rotation.ToAngleAxis(out angle, out axis);
			rotatable.Rotate(axis, angle, offset, centerOfRotation);
			rotationInformation[index].Permutation.Permute(edgePermutation);
			return;
		}
		index--;
		if (index < 0)
		{
			index += rotationInformation.Length;
		}
		Quaternion.Inverse(rotationInformation[index].Rotation).ToAngleAxis(out angle, out axis);
		rotatable.Rotate(axis, angle, offset, centerOfRotation);
		rotationInformation[index].Permutation.InversePermute(edgePermutation);
	}

	private static void _CrementIndex(ref int index, int mod, bool isForward)
	{
		if (isForward)
		{
			index++;
			index %= mod;
			return;
		}
		index--;
		index %= mod;
		if (index < 0)
		{
			index += mod;
		}
	}

	public static void GetNext(ISmartRotatable rotatable)
	{
		if (rotatable != null)
		{
			GetNext(rotatable, Quaternion.identity, rotatable.Transform.position);
		}
	}

	public static void GetNext(ISmartRotatable rotatable, Quaternion offset)
	{
		if (rotatable != null)
		{
			GetNext(rotatable, offset, rotatable.Transform.position);
		}
	}

	public static void GetNext(ISmartRotatable rotatable, Quaternion offset, Vector3 centerOfRotation)
	{
		_GetNext(rotatable, isForward: true, offset, centerOfRotation);
	}

	public static void GetPrevious(ISmartRotatable rotatable)
	{
		GetPrevious(rotatable, Quaternion.identity);
	}

	public static void GetPrevious(ISmartRotatable rotatable, Quaternion offset)
	{
		if (rotatable != null)
		{
			GetPrevious(rotatable, offset, rotatable.Transform.position);
		}
	}

	public static void GetPrevious(ISmartRotatable rotatable, Quaternion offset, Vector3 centerOfRotation)
	{
		_GetNext(rotatable, isForward: false, offset, centerOfRotation);
	}

	public static void _BestMatchCount(ISmartRotatable rotatable, bool isForward, Quaternion offset, Vector3 centerOfRotation, out int numberOfMatches, out bool doesValidMatchExist, out int indexOfBest)
	{
		numberOfMatches = int.MinValue;
		doesValidMatchExist = false;
		ConnectionType connectionType = rotatable.GetConnectionType();
		int num = NumberOfUniqueOrientationsOf[connectionType];
		int[] openEndLocationPermutation = rotatable.GetOpenEndLocationPermutation(offset);
		Dictionary<int[], int> dictionary = OrientationLookup[connectionType];
		RotationInformation[] array = RotationsList[connectionType];
		int[] openEndLocationPermutation2 = rotatable.GetOpenEndLocationPermutation(offset);
		rotatable.ConnectedCount();
		int index = (indexOfBest = dictionary[openEndLocationPermutation]);
		while (num-- > 0)
		{
			_DoRotation(rotatable, openEndLocationPermutation2, array, index, isForward, offset, centerOfRotation);
			_CrementIndex(ref index, array.Length, isForward);
			if (rotatable.CanConstruct().CanConstruct)
			{
				doesValidMatchExist = true;
				int num2 = rotatable.ConnectedCount();
				if (num2 > numberOfMatches)
				{
					numberOfMatches = num2;
					indexOfBest = index;
				}
			}
		}
	}

	private static void _GetNext(ISmartRotatable rotatable, bool isForward, Quaternion offset, Vector3 centerOfRotation)
	{
		if (rotatable == null)
		{
			return;
		}
		ConnectionType connectionType = rotatable.GetConnectionType();
		int[] openEndLocationPermutation = rotatable.GetOpenEndLocationPermutation(offset);
		Dictionary<int[], int> dictionary = OrientationLookup[connectionType];
		RotationInformation[] array = RotationsList[connectionType];
		int index = dictionary[openEndLocationPermutation];
		int[] edgePermutation = openEndLocationPermutation;
		if (rotatable is IGridMergeable gridMergeable && gridMergeable.WillMergeWhenPlaced())
		{
			_DoRotation(rotatable, edgePermutation, array, index, isForward, offset, centerOfRotation);
			return;
		}
		_BestMatchCount(rotatable, isForward, offset, centerOfRotation, out var _, out var doesValidMatchExist, out var indexOfBest);
		if (indexOfBest == index)
		{
			if (!doesValidMatchExist)
			{
				_DoRotation(rotatable, edgePermutation, array, index, isForward, offset, centerOfRotation);
			}
		}
		else
		{
			while (index != indexOfBest)
			{
				_DoRotation(rotatable, edgePermutation, array, index, isForward, offset, centerOfRotation);
				_CrementIndex(ref index, array.Length, isForward);
			}
		}
		UIAudioManager.Play(RotateBlueprintHash);
	}

	static SmartRotate()
	{
		OrientationLookup = new Dictionary<ConnectionType, Dictionary<int[], int>>
		{
			{
				ConnectionType.Elbow,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.SideOutletElbow,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.Straight,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.Tee,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.CrossDistinguishedOppositeEnds,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.SideOutletTee,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.Cross,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.SideOutletCross,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.SixWayCross,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.Exhaustive,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.ExhaustiveSymmetricEdge,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.ExhaustiveSymmetricCorner,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FlatCorner,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FlatStraight,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FlatTee,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FlatCross,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FlatExhaustive,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceAllAll,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceAllY,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceAllAll2,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceAllAll4,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceWallY,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceWallZY,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.FaceFloorZY,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			},
			{
				ConnectionType.StraightAsymmetric,
				new Dictionary<int[], int>(new LevelOneArrayComparer())
			}
		};
		_RotIdentity = new RotationInformation(new Permutation(new int[0][]), Quaternion.identity);
		RotX = new RotationInformation(new Permutation(new int[1][] { new int[4] { 0, 1, 5, 3 } }), Quaternion.Euler(90f, 0f, 0f));
		RotY = new RotationInformation(new Permutation(new int[1][] { new int[4] { 1, 2, 3, 4 } }), Quaternion.Euler(0f, 90f, 0f));
		RotZ = new RotationInformation(new Permutation(new int[1][] { new int[4] { 0, 2, 5, 4 } }), Quaternion.Euler(0f, 0f, 90f));
		RotZZ = new RotationInformation(new Permutation(new int[2][]
		{
			new int[2] { 0, 5 },
			new int[2] { 2, 4 }
		}), Quaternion.Euler(0f, 0f, 180f));
		_RotXInv = new RotationInformation(new Permutation(new int[1][] { new int[4] { 0, 3, 5, 1 } }), Quaternion.Euler(-90f, 0f, 0f));
		_RotXInvY = new RotationInformation(new Permutation(new int[2][]
		{
			new int[3] { 0, 3, 4 },
			new int[3] { 1, 2, 5 }
		}), Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-90f, 0f, 0f));
		_RotZInv = new RotationInformation(new Permutation(new int[1][] { new int[4] { 0, 4, 5, 2 } }), Quaternion.Euler(0f, 0f, -90f));
		_RotZInvY = new RotationInformation(new Permutation(new int[2][]
		{
			new int[3] { 0, 4, 1 },
			new int[3] { 2, 3, 5 }
		}), Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, -90f));
		_RotXY = new RotationInformation(new Permutation(new int[2][]
		{
			new int[3] { 0, 1, 2 },
			new int[3] { 3, 4, 5 }
		}), Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(90f, 0f, 0f));
		_RotYZ = new RotationInformation(new Permutation(new int[2][]
		{
			new int[3] { 0, 3, 4 },
			new int[3] { 1, 2, 5 }
		}), Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, 90f, 0f));
		_RotYZZ = new RotationInformation(new Permutation(new int[3][]
		{
			new int[2] { 0, 5 },
			new int[2] { 1, 2 },
			new int[2] { 3, 4 }
		}), Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(0f, 90f, 0f));
		_RotYYZInv = new RotationInformation(new Permutation(new int[3][]
		{
			new int[2] { 0, 2 },
			new int[2] { 1, 3 },
			new int[2] { 4, 5 }
		}), Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, 180f, 0f));
		_RotZY = new RotationInformation(new Permutation(new int[2][]
		{
			new int[3] { 0, 2, 3 },
			new int[3] { 1, 5, 4 }
		}), Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, 90f));
		Rot2D = new RotationInformation(new Permutation(new int[1][] { new int[4] { 0, 1, 2, 3 } }), Quaternion.Euler(0f, 0f, 90f));
		RotationsList = new Dictionary<ConnectionType, RotationInformation[]>
		{
			{
				ConnectionType.Elbow,
				new RotationInformation[12]
				{
					RotY, RotY, RotY, _RotYZ, RotY, RotY, RotY, _RotYZ, RotY, RotY,
					RotY, _RotYZZ
				}
			},
			{
				ConnectionType.SideOutletElbow,
				new RotationInformation[8] { RotY, RotY, RotY, _RotYZZ, RotY, RotY, RotY, _RotYZZ }
			},
			{
				ConnectionType.Straight,
				new RotationInformation[3] { RotX, RotY, RotZ }
			},
			{
				ConnectionType.Tee,
				new RotationInformation[12]
				{
					RotY, RotY, RotY, RotX, RotZ, RotZ, RotZ, RotY, _RotXInv, _RotXInv,
					_RotXInv, RotZ
				}
			},
			{
				ConnectionType.SideOutletTee,
				new RotationInformation[12]
				{
					RotY, RotY, RotY, RotX, RotZ, RotZ, RotZ, RotY, _RotXInv, _RotXInv,
					_RotXInv, RotZ
				}
			},
			{
				ConnectionType.Cross,
				new RotationInformation[3] { RotX, RotZ, RotY }
			},
			{
				ConnectionType.SideOutletCross,
				new RotationInformation[6] { RotX, RotY, _RotZInv, RotX, RotY, _RotZInv }
			},
			{
				ConnectionType.SixWayCross,
				new RotationInformation[1] { _RotIdentity }
			},
			{
				ConnectionType.Exhaustive,
				new RotationInformation[24]
				{
					RotY, RotY, RotY, RotZ, RotY, RotY, RotY, RotZ, RotY, RotY,
					RotY, _RotZInv, RotY, RotY, RotY, _RotZInv, RotY, RotY, RotY, RotZ,
					RotY, RotY, RotY, _RotYYZInv
				}
			},
			{
				ConnectionType.CrossDistinguishedOppositeEnds,
				new RotationInformation[6] { RotY, RotX, RotZ, RotY, RotX, RotZ }
			},
			{
				ConnectionType.ExhaustiveSymmetricEdge,
				new RotationInformation[12]
				{
					RotY, RotY, RotY, _RotYZ, RotY, RotY, RotY, _RotYZ, RotY, RotY,
					RotY, _RotYZZ
				}
			},
			{
				ConnectionType.ExhaustiveSymmetricCorner,
				new RotationInformation[8] { RotY, RotY, RotY, _RotYZZ, RotY, RotY, RotY, _RotYZZ }
			},
			{
				ConnectionType.FlatCorner,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.FlatStraight,
				new RotationInformation[2] { Rot2D, Rot2D }
			},
			{
				ConnectionType.FlatTee,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.FlatCross,
				new RotationInformation[1] { _RotIdentity }
			},
			{
				ConnectionType.FlatExhaustive,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.FaceAllAll,
				new RotationInformation[24]
				{
					RotY, RotY, RotY, RotZ, RotY, RotY, RotY, RotZ, RotY, RotY,
					RotY, _RotZInv, RotY, RotY, RotY, _RotZInv, RotY, RotY, RotY, RotZ,
					RotY, RotY, RotY, _RotYYZInv
				}
			},
			{
				ConnectionType.FaceAllY,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.FaceAllAll2,
				new RotationInformation[12]
				{
					RotY, RotY, RotY, RotX, RotZ, RotZ, RotZ, RotY, _RotXInv, _RotXInv,
					_RotXInv, RotZ
				}
			},
			{
				ConnectionType.FaceAllAll4,
				new RotationInformation[6] { RotX, RotY, _RotZInv, RotX, RotY, _RotZInv }
			},
			{
				ConnectionType.FaceWallY,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.FaceWallZY,
				new RotationInformation[16]
				{
					RotZ, RotZ, RotZ, _RotZY, RotX, RotX, RotX, _RotXY, _RotZInv, _RotZInv,
					_RotZInv, _RotZInvY, _RotXInv, _RotXInv, _RotXInv, _RotXInvY
				}
			},
			{
				ConnectionType.FaceFloorZY,
				new RotationInformation[4] { Rot2D, Rot2D, Rot2D, Rot2D }
			},
			{
				ConnectionType.StraightAsymmetric,
				new RotationInformation[6] { RotY, RotY, RotY, RotZ, RotZZ, RotX }
			}
		};
		NumberOfUniqueOrientationsOf = new Dictionary<ConnectionType, int>
		{
			{
				ConnectionType.Elbow,
				RotationsList[ConnectionType.Elbow].Length
			},
			{
				ConnectionType.SideOutletElbow,
				RotationsList[ConnectionType.SideOutletElbow].Length
			},
			{
				ConnectionType.Straight,
				RotationsList[ConnectionType.Straight].Length
			},
			{
				ConnectionType.Tee,
				RotationsList[ConnectionType.Tee].Length
			},
			{
				ConnectionType.SideOutletTee,
				RotationsList[ConnectionType.SideOutletTee].Length
			},
			{
				ConnectionType.Cross,
				RotationsList[ConnectionType.Cross].Length
			},
			{
				ConnectionType.SideOutletCross,
				RotationsList[ConnectionType.SideOutletCross].Length
			},
			{
				ConnectionType.SixWayCross,
				RotationsList[ConnectionType.SixWayCross].Length
			},
			{
				ConnectionType.Exhaustive,
				RotationsList[ConnectionType.Exhaustive].Length
			},
			{
				ConnectionType.CrossDistinguishedOppositeEnds,
				RotationsList[ConnectionType.CrossDistinguishedOppositeEnds].Length
			},
			{
				ConnectionType.ExhaustiveSymmetricEdge,
				RotationsList[ConnectionType.ExhaustiveSymmetricEdge].Length
			},
			{
				ConnectionType.ExhaustiveSymmetricCorner,
				RotationsList[ConnectionType.ExhaustiveSymmetricCorner].Length
			},
			{
				ConnectionType.FlatCorner,
				RotationsList[ConnectionType.FlatCorner].Length
			},
			{
				ConnectionType.FlatStraight,
				RotationsList[ConnectionType.FlatStraight].Length
			},
			{
				ConnectionType.FlatTee,
				RotationsList[ConnectionType.FlatTee].Length
			},
			{
				ConnectionType.FlatCross,
				RotationsList[ConnectionType.FlatCross].Length
			},
			{
				ConnectionType.FlatExhaustive,
				RotationsList[ConnectionType.FlatExhaustive].Length
			},
			{
				ConnectionType.FaceAllAll,
				RotationsList[ConnectionType.FaceAllAll].Length
			},
			{
				ConnectionType.FaceAllY,
				RotationsList[ConnectionType.FaceAllY].Length
			},
			{
				ConnectionType.FaceAllAll2,
				RotationsList[ConnectionType.FaceAllAll2].Length
			},
			{
				ConnectionType.FaceAllAll4,
				RotationsList[ConnectionType.FaceAllAll4].Length
			},
			{
				ConnectionType.FaceWallY,
				RotationsList[ConnectionType.FaceWallY].Length
			},
			{
				ConnectionType.FaceWallZY,
				RotationsList[ConnectionType.FaceWallZY].Length
			},
			{
				ConnectionType.FaceFloorZY,
				RotationsList[ConnectionType.FaceFloorZY].Length
			},
			{
				ConnectionType.StraightAsymmetric,
				RotationsList[ConnectionType.StraightAsymmetric].Length
			}
		};
		RotateBlueprintHash = Animator.StringToHash("RotateBlueprint");
		arrayComparer = new LevelOneArrayComparer();
		_ConstructRotationList(ConnectionType.Elbow, new int[6] { 1, 1, 0, 0, 0, 0 });
		_ConstructRotationList(ConnectionType.SideOutletElbow, new int[6] { 1, 1, 1, 0, 0, 0 });
		_ConstructRotationList(ConnectionType.Straight, new int[6] { 1, 0, 0, 0, 0, 1 });
		_ConstructRotationList(ConnectionType.Tee, new int[6] { 1, 1, 0, 0, 0, 1 });
		_ConstructRotationList(ConnectionType.SideOutletTee, new int[6] { 1, 1, 1, 0, 0, 1 });
		_ConstructRotationList(ConnectionType.Cross, new int[6] { 1, 0, 1, 0, 1, 1 });
		_ConstructRotationList(ConnectionType.SideOutletCross, new int[6] { 0, 1, 1, 1, 1, 1 });
		_ConstructRotationList(ConnectionType.SixWayCross, new int[6] { 1, 1, 1, 1, 1, 1 });
		_ConstructRotationList(ConnectionType.Exhaustive, new int[6] { 0, 1, 2, 3, 4, 5 });
		_ConstructRotationList(ConnectionType.CrossDistinguishedOppositeEnds, new int[6] { 0, 1, 2, 1, 2, 0 });
		_ConstructRotationList(ConnectionType.ExhaustiveSymmetricEdge, new int[6] { 1, 1, 0, 0, 0, 0 });
		_ConstructRotationList(ConnectionType.ExhaustiveSymmetricCorner, new int[6] { 1, 1, 1, 0, 0, 0 });
		_ConstructRotationList(ConnectionType.FlatCorner, new int[4] { 1, 1, 0, 0 });
		_ConstructRotationList(ConnectionType.FlatStraight, new int[4] { 1, 0, 1, 0 });
		_ConstructRotationList(ConnectionType.FlatTee, new int[4] { 1, 1, 1, 0 });
		_ConstructRotationList(ConnectionType.FlatCross, new int[4] { 1, 1, 1, 1 });
		_ConstructRotationList(ConnectionType.FlatExhaustive, new int[4] { 0, 1, 2, 3 });
		_ConstructRotationList(ConnectionType.FaceAllAll, new int[6] { 0, 1, 2, 3, 4, 5 });
		_ConstructRotationList(ConnectionType.FaceAllY, new int[4] { 0, 1, 2, 3 });
		_ConstructRotationList(ConnectionType.FaceAllAll2, new int[6] { 1, 0, 0, 1, 0, 1 });
		_ConstructRotationList(ConnectionType.FaceAllAll4, new int[6] { 0, 0, 0, 0, 0, 1 });
		_ConstructRotationList(ConnectionType.FaceWallY, new int[4] { 0, 1, 2, 3 });
		_ConstructRotationList(ConnectionType.FaceWallZY, new int[6] { 2, 1, 0, 0, 0, 0 });
		_ConstructRotationList(ConnectionType.FaceFloorZY, new int[4] { 0, 1, 2, 3 });
		_ConstructRotationList(ConnectionType.StraightAsymmetric, new int[6] { 0, 1, 0, 2, 0, 0 });
	}

	public static void AutomaticSetup(ISmartRotatable rotatable)
	{
		int[] array = null;
		switch (rotatable.GetPlacementType())
		{
		case PlacementSnap.Grid:
			switch (rotatable.GetRotationAxis())
			{
			case RotationAxis.XY:
			case RotationAxis.ZX:
			case RotationAxis.ZY:
			case RotationAxis.All:
				array = new int[6];
				_AutomaticInitial3DSetup(rotatable, array);
				if (_SetPermutation(rotatable, array))
				{
					return;
				}
				rotatable.SetConnectionType(ConnectionType.Exhaustive);
				break;
			case RotationAxis.X:
			case RotationAxis.Y:
			case RotationAxis.Z:
				array = new int[4];
				_AutomaticInitial2DSetup(rotatable, array);
				if (_SetPermutation(rotatable, array))
				{
					return;
				}
				rotatable.SetConnectionType(ConnectionType.FlatExhaustive);
				break;
			}
			break;
		case PlacementSnap.FaceMount:
			array = new int[4];
			_AutomaticInitial2DSetup(rotatable, array);
			if (_SetPermutation(rotatable, array))
			{
				return;
			}
			rotatable.SetConnectionType(ConnectionType.FlatExhaustive);
			break;
		case PlacementSnap.Face:
			switch (rotatable.GetRotationAxis())
			{
			case RotationAxis.Y:
				switch (rotatable.GetAllowedRotations())
				{
				case AllowedRotations.All:
					array = new int[4] { 0, 1, 2, 3 };
					rotatable.SetConnectionType(ConnectionType.FaceWallY);
					if (_SetPermutation(rotatable, array))
					{
						return;
					}
					rotatable.SetConnectionType(ConnectionType.Exhaustive);
					break;
				case AllowedRotations.Wall:
					array = new int[4] { 0, 1, 2, 3 };
					rotatable.SetConnectionType(ConnectionType.FaceAllY);
					if (_SetPermutation(rotatable, array))
					{
						return;
					}
					rotatable.SetConnectionType(ConnectionType.Exhaustive);
					break;
				}
				break;
			case RotationAxis.All:
				if (rotatable.GetAllowedRotations() == AllowedRotations.All)
				{
					array = new int[6] { 0, 1, 2, 3, 4, 5 };
					rotatable.SetConnectionType(ConnectionType.FaceAllAll);
					if (_SetPermutation(rotatable, array))
					{
						return;
					}
					rotatable.SetConnectionType(ConnectionType.Exhaustive);
				}
				break;
			case RotationAxis.ZY:
				switch (rotatable.GetAllowedRotations())
				{
				case AllowedRotations.Wall:
					array = new int[6] { 2, 1, 0, 0, 0, 0 };
					rotatable.SetConnectionType(ConnectionType.FaceWallZY);
					if (_SetPermutation(rotatable, array))
					{
						return;
					}
					rotatable.SetConnectionType(ConnectionType.Exhaustive);
					break;
				case AllowedRotations.Floor:
					array = new int[4] { 0, 1, 2, 3 };
					rotatable.SetConnectionType(ConnectionType.FaceFloorZY);
					if (_SetPermutation(rotatable, array))
					{
						return;
					}
					rotatable.SetConnectionType(ConnectionType.Exhaustive);
					break;
				}
				break;
			}
			break;
		default:
			Debug.LogError("SmartRotate doesn't yet work with PlacementSnap type " + rotatable.GetPlacementType());
			return;
		}
		if (array == null)
		{
			Debug.LogError("SmartRotate doesn't yet work with PlacementSnap type " + rotatable.GetPlacementType());
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		rotatable.SetOpenEndsPermutation(array);
	}

	private static void _AutomaticInitial2DSetup(ISmartRotatable rotatable, int[] endLocations)
	{
		List<NetworkType> list = new List<NetworkType>(Enum.GetNames(typeof(NetworkType)).Length) { NetworkType.None };
		if (rotatable.GetOpenEnds() == null)
		{
			return;
		}
		foreach (Connection openEnd in rotatable.GetOpenEnds())
		{
			if (!list.Contains(openEnd.ConnectionType))
			{
				list.Add(openEnd.ConnectionType);
			}
			Dir faceDir = RocketGrid.GetFaceDir(openEnd.Transform.position, rotatable.Transform.position, rotatable.GetGridSize());
			int num = list.IndexOf(openEnd.ConnectionType);
			switch (faceDir)
			{
			case Dir.East:
				endLocations[0] = num;
				continue;
			case Dir.Up:
				endLocations[1] = num;
				continue;
			case Dir.West:
				endLocations[2] = num;
				continue;
			case Dir.Down:
				endLocations[3] = num;
				continue;
			}
			endLocations[0] = 0;
			endLocations[1] = 1;
			endLocations[2] = 2;
			endLocations[3] = 3;
			return;
		}
	}

	private static void _AutomaticInitial3DSetup(ISmartRotatable rotatable, int[] endLocations)
	{
		List<NetworkType> list = new List<NetworkType>(Enum.GetNames(typeof(NetworkType)).Length) { NetworkType.None };
		if (rotatable.GetOpenEnds() == null)
		{
			return;
		}
		foreach (Connection openEnd in rotatable.GetOpenEnds())
		{
			if (!list.Contains(openEnd.ConnectionType))
			{
				list.Add(openEnd.ConnectionType);
			}
			Dir faceDir = RocketGrid.GetFaceDir(openEnd.Transform.position, rotatable.Transform.position, rotatable.GetGridSize());
			int num = list.IndexOf(openEnd.ConnectionType);
			switch (faceDir)
			{
			case Dir.Up:
				endLocations[0] = num;
				break;
			case Dir.South:
				endLocations[1] = num;
				break;
			case Dir.East:
				endLocations[2] = num;
				break;
			case Dir.North:
				endLocations[3] = num;
				break;
			case Dir.West:
				endLocations[4] = num;
				break;
			case Dir.Down:
				endLocations[5] = num;
				break;
			}
		}
	}

	private static bool _SetPermutation(ISmartRotatable rotatable, int[] endLocations)
	{
		foreach (ConnectionType key in OrientationLookup.Keys)
		{
			foreach (int[] key2 in OrientationLookup[key].Keys)
			{
				if (arrayComparer.Equals(endLocations, key2))
				{
					rotatable.SetConnectionType(key);
					rotatable.SetOpenEndsPermutation(endLocations);
					return true;
				}
			}
		}
		return false;
	}
}
