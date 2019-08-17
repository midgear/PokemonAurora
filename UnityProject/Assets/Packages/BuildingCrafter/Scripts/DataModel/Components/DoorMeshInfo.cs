using UnityEngine;
using System.Collections;
using BuildingCrafter;

/// <summary>
/// Information on how this mesh is laid out for the hinge above it
/// </summary>
public class DoorMeshInfo : MonoBehaviour 
{
	[Range(0, 0.25f)]
	public float HingeOffset = 0.035f;

	/// <summary>
	/// Door information from the original building crafter
	/// </summary>
	public DoorInfo DoorInfo;
}
