using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

namespace BuildingCrafter
{
	[System.Serializable]
	public class BCAtlas
	{
		public enum Size 
		{
			Small2048,
			Medium4096,
			Large8192
		}

		public enum TextureMapFlag
		{
			None = 0,
			MainTexture = 1,
			GlossTexture = 2,
			NormalMap = 4,
			ParallaxMap = 8,
			OcclusionMap = 16,
			EmissionMap = 32
		}

		public BCAtlas(string newAtlasName, Size renderSize)
		{
			this.AtlasName = newAtlasName;
			this.RenderSize = renderSize;
			this.SubMaterials = new List<BCAtlasSubMaterial>();
		}

		public string AtlasName = "NewAtlas";

		[SerializeField] public Texture2D MainTexture;
		[SerializeField] public Texture2D GlossTexture;
		[SerializeField] public Texture2D NormalMap;
		[SerializeField] public Texture2D ParallaxMap;
		[SerializeField] public Texture2D OcclusionMap;
		[SerializeField] public Texture2D EmissionMap;

		// Used to space out the textures and ensure they properly wrap around
		public int TextureMargin = 128; // 128 seems the best size for mip mapping on 1024 textures, 64 should be a good buffer

		public Material Material;
		public Size RenderSize  = Size.Medium4096;
		public List<BCAtlasSubMaterial> SubMaterials = new List<BCAtlasSubMaterial>();

		public int GetTextureSize()
		{
			switch(RenderSize)
			{
			case Size.Small2048:
				return 2048;
			case Size.Medium4096:
				return 4096;
			case Size.Large8192:
				return 8192;
			}
			return 2048;
		}

		public override string ToString()
		{
			return "Main Texture (" + MainTexture + "), Gloss Texture (" + GlossTexture + "), NormalMap (" + NormalMap + ")";
		}

		#region Static Doers

		public static void AtlasGameObject(GameObject gameObject, BCAtlas[] bcAtlases)
		{
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.DisplayProgressBar("Atlasing", "Converting UVs", 0.95f);
			#endif
			MeshRenderer[] renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
			// 3. Convert all the mesh renderers UV mappings to the newly created atlas
			for(int i = 0; i < bcAtlases.Length; i++)
				ConvertMeshUVs(bcAtlases[i], renderers);

			#if UNITY_EDITOR
			UnityEditor.EditorUtility.ClearProgressBar();
			#endif
		}

		public static void ConvertMeshUVs(BCAtlas bcAtlas, MeshRenderer[] meshRenderer)
		{
			for(int i = 0; i < meshRenderer.Length;	i++)
				ConvertMeshUVs(bcAtlas, meshRenderer[i]);
		}

		public static void ConvertMeshUVs(BCAtlas bcAtlas, MeshRenderer meshRenderer)
		{
			MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
			Material material = meshRenderer.sharedMaterial;
			Mesh mesh = meshFilter.sharedMesh;

			BCAtlasSubMaterial subAtlas = null;

			if(bcAtlas.SubMaterials == null)
				return;

			for(int i = 0; i < bcAtlas.SubMaterials.Count; i++)
			{
				if(bcAtlas.SubMaterials[i].MaterialReference == material)
				{
					subAtlas = bcAtlas.SubMaterials[i];
					break;
				}
			}

			if(subAtlas == null)
				return;

			if(mesh == null)
				return;

			Vector2[] meshUv = mesh.uv;

			if(AreUVsOutsideOfBounds(meshUv))
			{
				Debug.Log("Mesh Uvs are outside bounds: (" + meshRenderer.name + ")");
				return;
			}

			Mesh newMesh = (Mesh)GameObject.Instantiate(mesh);
			newMesh.name += "_atlased";

			bool failedUVPoints;
			meshUv = ConvertMeshUvs(meshUv, subAtlas, out failedUVPoints);
			if(failedUVPoints)
				Debug.LogError("UVs in mesh " + mesh.name + " are outside of the normal range (0,0) to (1,1). Will not atlas correctly");

			newMesh.uv = meshUv;

			meshFilter.sharedMesh = newMesh;
			meshRenderer.sharedMaterial = bcAtlas.Material;
		}

		public static bool AreUVsOutsideOfBounds(Vector2[] oldUvs)
		{
			for(int i = 0; i < oldUvs.Length; i++)
			{
				float roundedU = (float)System.Math.Round(oldUvs[i].x, 5);
				float roundedV = (float)System.Math.Round(oldUvs[i].y, 5);

				if(roundedU < 0 || roundedU > 1 || roundedV < 0 || roundedV > 1)
				{
					Debug.Log(roundedU + ", " + roundedV);
					return true;
				}
			}

			return false;
		}

		public static Vector2[] ConvertMeshUvs(Vector2[] oldUvs, BCAtlasSubMaterial subMaterial, out bool failedUVPoints)
		{
			failedUVPoints = false;
			Vector2[] newUvs = new Vector2[oldUvs.Length];

			for(int i = 0; i < oldUvs.Length; i++)
			{
				float roundedU = (float)System.Math.Round(oldUvs[i].x, 5);
				float roundedV = (float)System.Math.Round(oldUvs[i].y, 5);

				if(roundedU < 0 || roundedU > 1 || roundedV < 0 || roundedV > 1)
					failedUVPoints = true;

				float atlasUSizeNormal = (subMaterial.EndUV.x - subMaterial.StartUV.x);
				float atlasVSizeNormal = (subMaterial.EndUV.y - subMaterial.StartUV.y);

				float newU = subMaterial.StartUV.x + oldUvs[i].x * atlasUSizeNormal;
				float newV = subMaterial.StartUV.y + oldUvs[i].y * atlasVSizeNormal;

				newUvs[i] = new Vector2(newU, newV);
			}

			return newUvs;
		}

		#endregion
	}

	public struct IntVector2
	{
		public IntVector2(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public int X;
		public int Y;

		public static IntVector2 operator + (IntVector2 a, IntVector2 b)
		{
			return new IntVector2(a.X + b.X, a.Y + b.Y);
		}

		public static IntVector2 operator - (IntVector2 a, IntVector2 b)
		{
			return new IntVector2(a.X - b.X, a.Y - b.Y);
		}

		public static bool operator == (IntVector2 lhs, IntVector2 rhs)
		{
			return lhs.X == rhs.X && lhs.Y == rhs.Y;
		}

		public static bool operator != (IntVector2 lhs, IntVector2 rhs)
		{
			return !(lhs == rhs);
		}

		public override bool Equals (object obj)
		{
			return this == (IntVector2)obj; 
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString ()
		{
			return string.Format ("({0}, {1})", X, Y);
		}
	}

	[System.Serializable]
	public class BCAtlasSubMaterial
	{
		public BCAtlasSubMaterial(IntVector2 topLeftPos, int size, int margin, int maxTextureSize, Material materialRef)
		{
			topLeftPos.X += margin;
			topLeftPos.Y += margin;

			float UStart, UEnd, VStart, VEnd;

			UStart = (float)topLeftPos.X / maxTextureSize;
			UEnd = (float)(topLeftPos.X + size) / maxTextureSize;
			VStart = (float)topLeftPos.Y / maxTextureSize;
			VEnd = (float)(topLeftPos.Y + size) / maxTextureSize;

			this.StartPos = topLeftPos;
			this.Size = size;
			this.StartUV = new Vector2(UStart, VStart);
			this.EndUV = new Vector2(UEnd, VEnd);
			this.MaterialReference = materialRef;
		}

		/// <summary>
		/// The start position WITHOUT any margin added
		/// </summary>
		public IntVector2 StartPos = new IntVector2();
		public Vector2 StartUV;
		public Vector2 EndUV;
		//  how big the sub material is for UV mapping
		public int Size = 1024;
		// The material that this section relates too
		public Material MaterialReference;
	}
}