using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public class BCTestWindow : MonoBehaviour 
	{
		public static void DestroyAllTestWindows(bool undo = true)
		{
			BCTestWindow[] windows = GameObject.FindObjectsOfType<BCTestWindow>();
			for(int i = 0; i < windows.Length; i++)
			{
				MeshFilter[] meshFilters = windows[i].GetComponentsInChildren<MeshFilter>();

				for(int n = 0; n < meshFilters.Length; n++)
				{
					if(meshFilters[n] != null)
						BCGenerator.DestroyAllProceduralMeshes(meshFilters[n].gameObject, false);
				}

#if UNITY_EDITOR
				if(undo)
					Undo.DestroyObjectImmediate(windows[i].gameObject);
				else
					GameObject.DestroyImmediate(windows[i].gameObject, false);
#else
				GameObject.Destroy(windows[i].gameObject);
#endif
			}
		}
	}
}
