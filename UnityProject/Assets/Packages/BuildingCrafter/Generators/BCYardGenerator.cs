using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibTessDotNet;
using UnityMesh = UnityEngine.Mesh;

namespace BuildingCrafter
{

	public static partial class BCGenerator
	{
		/// <summary>
		/// Generates all the layouts for a yard
		/// </summary>
		public static bool GenerateYardLayouts (BuildingBlueprint buildingBp)
		{
			// 1. Find all the yards on the first floor
			if(buildingBp.Floors == null || buildingBp.Floors.Count < 0 || buildingBp.Floors[0].YardLayouts == null || buildingBp.Floors[0].YardLayouts.Count < 1)
				return false;
			
			List<YardLayout> allYards = buildingBp.Floors[0].YardLayouts;
			
			// Find all the floors of a certain style, group them and combine them.
			List<YardLayout> grassYards = new List<YardLayout>();
			List<YardLayout> concrete = new List<YardLayout>();
			List<YardLayout> dirtPath = new List<YardLayout>();
			
			for(int i = 0; i < allYards.Count; i++)
			{
				switch(allYards[i].YardType)
				{
				case YardTypeEmum.GrassYard:
					grassYards.Add(allYards[i]);
					break;
					
				case YardTypeEmum.Concrete:
					concrete.Add(allYards[i]);
					break;
					
				case YardTypeEmum.DirtPath:
					dirtPath.Add(allYards[i]);
					break;
					
				default:
					grassYards.Add(allYards[i]);
					break;
				}
			}
			
			if(grassYards.Count > 0)
				GenerateYardGameObject(grassYards, buildingBp.BuildingStyle.Grass, buildingBp);
			
			if(concrete.Count > 0)
				GenerateYardGameObject(concrete, buildingBp.BuildingStyle.Concrete, buildingBp);
			
			if(dirtPath.Count > 0)
				GenerateYardGameObject(dirtPath, buildingBp.BuildingStyle.DirtPath, buildingBp);

			return true;
		}
		
		/// <summary>
		/// Generates a combined mesh of the yards fed into it
		/// </summary>
		private static void GenerateYardGameObject(List<YardLayout> yardLayouts, Material material, BuildingBlueprint buildingBp, string yardType = "Yard")
		{
			MeshInfo meshInfos = new MeshInfo();
			
			for(int i = 0; i < yardLayouts.Count; i++)
			{
				meshInfos = BCMesh.CombineMeshInfos(meshInfos, GenerateYardMeshes(yardLayouts[i]));
			}
			
			UnityMesh mesh = BCMesh.GetMeshFromMeshInfo(meshInfos, buildingBp.BlueprintXZCenter);
			mesh.name = "Procedural " + yardType;
			
			BCMesh.CalculateMeshTangents(mesh);
			
			GameObject yard = BCMesh.GenerateEmptyGameObject("Create Yard", true);
			yard.name = yardType;
			
			MeshFilter meshFilter = yard.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = yard.AddComponent<MeshRenderer>();
			meshRenderer.material = material;
			
			meshFilter.mesh = mesh;
			
			yard.transform.SetParent(buildingBp.transform);
			yard.transform.localPosition = Vector3.zero;
			yard.transform.localRotation = Quaternion.identity;
		}

		/// <summary>
		/// Generates mesh for the yard
		/// </summary>
		private static MeshInfo GenerateYardMeshes (YardLayout yardLayout, FloorBlueprint floorBelow = null)
		{
			MeshInfo meshInfo = new MeshInfo();
			
			if(yardLayout != null && yardLayout.PerimeterWalls.Count > 2)
			{
				Vector3[] yardPoints = yardLayout.PerimeterWalls.ToArray<Vector3>();
				meshInfo = BCMesh.GenerateGenericMeshInfo(yardPoints, 1);
			}
			return meshInfo;
		}
	}
}