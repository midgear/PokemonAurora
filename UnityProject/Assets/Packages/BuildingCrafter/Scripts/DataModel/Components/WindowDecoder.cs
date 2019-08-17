using UnityEngine;
using System.Collections;

public class WindowDecoder : MonoBehaviour
{

	/// <summary>
	/// Gets the window sizes.
	/// </summary>
	/// <returns>The Vec3 of window sizes, which are X=width, y=lowerHeight, z=topHeight</returns>
	public static Vector3 GetWindowSizes(GameObject windowHolder)
	{
		Quaternion oldRotation = windowHolder.transform.localRotation; // Rotate to zero so we get a proper reading
		
		MeshFilter[] meshFilters = windowHolder.GetComponentsInChildren<MeshFilter>();

		if(meshFilters.Length < 1)
			return new Vector3();

		Bounds bounds = new Bounds(meshFilters[0].sharedMesh.bounds.center, meshFilters[0].sharedMesh.bounds.size);

		for(int i = 0; i < meshFilters.Length; i++)
			bounds.Encapsulate(meshFilters[i].sharedMesh.bounds);

		Vector3 newInfo = new Vector3(bounds.extents.x * 2, 
		                             	bounds.center.y - bounds.extents.y,
		                             	bounds.center.y + bounds.extents.y);

		windowHolder.transform.localRotation = oldRotation; // Rotate back

		return newInfo;
	}

}
