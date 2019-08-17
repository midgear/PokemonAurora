using UnityEngine;
using System.Collections;

public class PlayerDoorOpener : MonoBehaviour 
{
	Camera playerCamera;

	void Awake()
	{
		playerCamera = GetComponentInChildren<Camera>();
	}

	// Update is called once per frame
	void Update () 
	{
		if(Input.GetButtonDown("Fire1"))
		{
			this.TryAndOpenDoor();
		}
	}

	void TryAndOpenDoor ()
	{

		Ray rayFire = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		rayFire.origin += playerCamera.transform.forward * .25f;

		RaycastHit rayHit;

		Debug.DrawRay(rayFire.origin, rayFire.direction, Color.red, 1f);

		if(Physics.Raycast(rayFire, out rayHit, 4))
		{

			DoorMeshInfo doorMesh = rayHit.collider.GetComponent<DoorMeshInfo>();
			if(doorMesh != null)
			{
				Debug.Log("Fire Thing");
				DoorOpener doorOpener = rayHit.collider.GetComponentInParent<DoorOpener>();
				doorOpener.ChangeState();
			}
		}
	}
}
