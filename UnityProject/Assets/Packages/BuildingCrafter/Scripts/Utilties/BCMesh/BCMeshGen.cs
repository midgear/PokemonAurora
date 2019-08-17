using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BuildingCrafter
{
	public static partial class BCMesh
	{
		/// <summary>
		/// Generates an empty gameObject
		/// </summary>
		public static GameObject GenerateEmptyGameObject (string undoMessage, bool procedural = false, bool allowUndo = false)
		{
			GameObject gameObject = new GameObject();

			if(allowUndo)
			{
				#if UNITY_EDITOR
				Undo.RegisterCreatedObjectUndo(gameObject, undoMessage);
				#endif
			}

			if(procedural && gameObject.GetComponent<ProceduralGameObject>() == null)
				gameObject.AddComponent<ProceduralGameObject>();

			return gameObject;
		}

		/// <summary>
		/// Can destroy in editor with undo step OR at runtime
		/// </summary>
		/// <param name="gameObject">Game object.</param>
		public static void DestroyGameObjectProperly (GameObject gameObject)
		{
			if(gameObject == null)
				return;

#if UNITY_EDITOR
			Undo.DestroyObjectImmediate(gameObject);
#else
			GameObject.Destroy(gameObject);
#endif
		}
	}
}
