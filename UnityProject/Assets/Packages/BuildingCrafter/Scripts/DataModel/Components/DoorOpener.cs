using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorOpener : MonoBehaviour
{
	public DoorMeshInfo doorMeshInfo;
	public DoorHinge doorHinge;

	private Transform doorTransform;

	private Quaternion closedRot = Quaternion.Euler(0,0,0);
	private Quaternion openRot;
	private bool changingState = false;
	[HideInInspector][SerializeField]
	private float doorNormalPos = 0;
	private float doorOpenSpeed = 1.5f;
	private bool isOpening = false;

	public bool IsOpen;

	[SerializeField]
	private bool swingIn;
	public bool SwingIn { get { return swingIn; } }

	void Start()
	{
		SetChildrenToStatic(this.gameObject, false);

		if(doorMeshInfo == null)
			doorMeshInfo = GetComponentInChildren<DoorMeshInfo>();

		if(doorMeshInfo != null)
		{
			UpdateSwingDirection(doorMeshInfo);
		}
	}

//	[SerializeField]
//	bool testOpen = false;
//	void Update()
//	{
//		if(testOpen)
//		{
//			testOpen = false;
//			ChangeState();
//		}
//	}

	public void SetDoorToStartingRotation()
	{
		if(doorMeshInfo == null)
			doorMeshInfo = GetComponentInChildren<DoorMeshInfo>();

		if(doorTransform == null)
		{
			doorHinge = this.GetComponentInChildren<DoorHinge>();
			doorTransform = doorHinge.transform;
		}
			
		if(doorMeshInfo == null || doorMeshInfo.DoorInfo.IsStartOpen == false || doorTransform == null)
			return;

		DoorMeshInfo doorInfo = this.doorMeshInfo;

		if(doorInfo.DoorInfo.Direction > 0)
			this.swingIn = true;
		else
			this.swingIn = false;

		float hingeOffset = doorInfo.HingeOffset;

		// Ensures the doormesh is offset correctly when loaded
		if(swingIn == true)
		{
			openRot = closedRot * Quaternion.Euler(0, doorInfo.DoorInfo.StartOpenAngle, 0);
			doorTransform.transform.localRotation = openRot;
			doorInfo.transform.localPosition = new Vector3(0, 0, hingeOffset * -1f);
		}
		else
		{
			openRot = closedRot * Quaternion.Euler(0, doorInfo.DoorInfo.StartOpenAngle * -1, 0);
			doorTransform.transform.localRotation = openRot;
			doorInfo.transform.localPosition = new Vector3(0, 0, hingeOffset);
		}

		this.doorNormalPos = doorInfo.DoorInfo.StartOpenAngle / 90;
		if(doorInfo.DoorInfo.StartOpenAngle > 1)
			this.IsOpen = false;

		if(doorNormalPos == 1)
		{
			this.IsOpen = true;
		}
	}

	void SetChildrenToStatic(GameObject parent, bool isStatic)
	{
		List<GameObject> childrenAndParent = BuildingCrafter.BCUtils.GetChildren(parent);
		
		for(int i = 0; i < childrenAndParent.Count; i++)
			childrenAndParent[i].isStatic = isStatic;
	}


	public void UpdateSwingDirection(DoorMeshInfo doorInfo)
	{
		if(doorInfo.DoorInfo.Direction > 0)
			this.swingIn = true;
		else
			this.swingIn = false;

		float hingeOffset = doorInfo.HingeOffset;

		// Ensures the doormesh is offset correctly when loaded
		if(swingIn == true)
		{
			openRot = closedRot * Quaternion.Euler(0, doorInfo.DoorInfo.MaxOpeningAngle, 0);
			doorInfo.transform.localPosition = new Vector3(0, 0, hingeOffset * -1f);
		}
		else
		{
			openRot = closedRot * Quaternion.Euler(0, doorInfo.DoorInfo.MaxOpeningAngle * -1, 0);
			doorInfo.transform.localPosition = new Vector3(0, 0, hingeOffset);
		}
	}

	public void ChangeState()
	{
		ChangeLocalState(this.IsOpen);
	}

	private void ChangeLocalState(bool isOpen)
	{
		if(isOpen == true)
			isOpening = false;
		else
			isOpening = true;
		
		// Starts a coroutine that swings the door
		StartCoroutine(this.SwingDoor());
	}

	private IEnumerator SwingDoor()
	{
		changingState = true;

		int breaker = 0;
		while(changingState == true && breaker < 1000)
		{
			breaker++;

			if(isOpening)
				doorNormalPos += Time.deltaTime * doorOpenSpeed;
			else
				doorNormalPos -= Time.deltaTime * doorOpenSpeed;

			// Sets the door swing transform to the door hinge;
			if(doorTransform == null)
			{
				doorHinge = this.GetComponentInChildren<DoorHinge>();
				doorTransform = doorHinge.transform;
			}

			doorTransform.localRotation = Quaternion.Slerp(closedRot, openRot, doorNormalPos);
			
			// Once the door is open, then set it to a static position
			if(isOpening && doorNormalPos > 1 || isOpening == false && doorNormalPos < 0)
			{
				if(isOpening == true)
				{
					doorTransform.localRotation = Quaternion.Slerp(closedRot, openRot, 1);
					IsOpen = true;
				}					
				else
				{
					doorTransform.localRotation = Quaternion.Slerp(closedRot, openRot, 0);
					IsOpen = false;
				}

				doorTransform.localRotation = Quaternion.Slerp(closedRot, openRot, doorNormalPos);
				
				changingState = false;
			}
			yield return new WaitForEndOfFrame();
		}
	}
}
