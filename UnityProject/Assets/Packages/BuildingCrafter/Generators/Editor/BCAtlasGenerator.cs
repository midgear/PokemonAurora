using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace BuildingCrafter
{
	public static class BCAtlasGenerator 
	{
		#region Create Atlas from Building Style

		public static BCAtlas[] AtlasBuildingStyle(BuildingStyle style)
		{	
			string pathToStyle = AssetDatabase.GetAssetPath(style);

			pathToStyle = FindFolderFromAssetName(pathToStyle);

			if(Directory.Exists(pathToStyle))
				Directory.Delete(pathToStyle, true);

			if(Directory.Exists(pathToStyle) == false)
				Directory.CreateDirectory(pathToStyle);

			AssetDatabase.Refresh();

			// 1. Create all the atlases that are needed for this Atlas Group
			BCAtlas[] atlasGroup = CreateAtlasGroup(style, pathToStyle, style.AtlasSize);

			// 2. Set all the textures to readable within them
			List<BCAtlasSubMaterial> allSubMaterials = GetAllSubMaterials(atlasGroup);
			if(atlasGroup.Length > 0)
			{
				EditorUtility.DisplayProgressBar("Textures", "Making sure the textures can be read", .25f);
				SetAllMapsToReadable(atlasGroup[0].GetTextureSize(), allSubMaterials);
			}	

			// 3. Create all the atlases with their textures
			for(int i = 0; i < atlasGroup.Length; i++)
			{
				BuildingCrafter.BCAtlas.TextureMapFlag flags = FindTextureFlag(atlasGroup[i]);
				CreateAtlasFromSubMaterials(atlasGroup[i], flags, atlasGroup[i].AtlasName, pathToStyle); // the path should be under the building style path, have to find that
			}

			// 3p1. Import the new atlas textures at their respective sizes
//			ImportNewTexturesAtProperSize(BCAtlas[] atlases);

			// 4. Convert the normals on the separate textures back to regular normals
			if(atlasGroup.Length > 0)
				SetNormalsBackToNormalMapping(atlasGroup[0].GetTextureSize(), allSubMaterials);

			// 5. Now load the assets onto the BCAtlas materials
			// Now we need to refresh the database and load up the newly created textures to the BC Atlas Materials
			LoadAndAssignTexturesToAtlases(atlasGroup, pathToStyle);

			EditorUtility.ClearProgressBar();

			return atlasGroup;
		}

		private static BCAtlas[] CreateAtlasGroup(BuildingStyle style, string fullDirectoryPath, BCAtlas.Size atlasSize)
		{
			// 1. get all the materials
			Material[] subMaterials = style.GetAllMaterials();

			// 2. Add all the materials to a new atlas and keep going till we have accounted for all the materials
			List<BCAtlas> atlases = new List<BCAtlas>();

			// Create the first atlas
			BCAtlas firstAtlas = new BCAtlas(style.name + "_atlas_1", atlasSize);
			atlases.Add(firstAtlas);

			for(int index = 0; index < subMaterials.Length; index++)
			{
				// Validate the sub material to make sure the main texture isn't too big
				if(IsTextureTooBigForAtlas(subMaterials[index], firstAtlas))
				{
					Debug.LogError("The texture for the submaterial won't fit in the atlas size of " + firstAtlas.GetTextureSize());
					continue;
				}

				for(int i = 0; i < atlases.Count; i++)
				{
					bool textureAtMaxSize = false;
					bool successfulAdd = AddMaterialToAtlas(atlases[i], subMaterials[index], out textureAtMaxSize);
					if(successfulAdd)
						break;

					// Ensures larger than the atlas margins are just ignored
					if(textureAtMaxSize)
						break;

					if(i == atlases.Count - 1)
					{	
						// Add a new atlas to the system and then add to this new atlas
						BCAtlas newAtlas = new BCAtlas(style.name + "_atlas_" + (atlases.Count + 1), firstAtlas.RenderSize);

						AddMaterialToAtlas(newAtlas, subMaterials[index], out textureAtMaxSize);
						if(textureAtMaxSize) // This should never get tiggered
							Debug.LogError("You really broke it, this should never appear");

						atlases.Add(newAtlas);
						break;
					}
				}
			}

			return atlases.ToArray<BCAtlas>();
		}

		public static bool IsTextureTooBigForAtlas(Material subMaterial, BCAtlas atlas)
		{
			bool textureFailed = false;

			int mainTextureU = 0;
			int mainTextureV = 0;

			{
				Texture texture = subMaterial.GetTexture("_MainTex");
				if(texture != null)
				{
					mainTextureU = texture.width;
					mainTextureV = texture.height;

					if(mainTextureU >= atlas.GetTextureSize() + atlas.TextureMargin)
						textureFailed = true;

					if(mainTextureU >= atlas.GetTextureSize() + atlas.TextureMargin)
						textureFailed = true;
				}
			}

			if(subMaterial.HasProperty("_MetallicGlossMap"))
			{
				if(CheckForTextureProblems(subMaterial, "_MetallicGlossMap", mainTextureU, mainTextureV, atlas))
					textureFailed = true;
			}

			if(subMaterial.HasProperty("_SpecGlossMap"))
			{
				if(CheckForTextureProblems(subMaterial, "_SpecGlossMap", mainTextureU, mainTextureV, atlas))
					textureFailed = true;
			}

			if(CheckForTextureProblems(subMaterial, "_BumpMap", mainTextureU, mainTextureV, atlas))
				textureFailed = true;

			if(CheckForTextureProblems(subMaterial, "_ParallaxMap", mainTextureU, mainTextureV, atlas))
				textureFailed = true;

			if(CheckForTextureProblems(subMaterial, "_OcclusionMap", mainTextureU, mainTextureV, atlas))
				textureFailed = true;

			if(CheckForTextureProblems(subMaterial, "_EmissionMap", mainTextureU, mainTextureV, atlas))
				textureFailed = true;

			return textureFailed;
		}

		public static bool CheckForTextureProblems(Material subMaterial, string refName, int mainTextureU, int mainTextureV, BCAtlas atlas)
		{
			Texture texture = subMaterial.GetTexture(refName);
			if(texture != null)
			{
				if(texture.width != mainTextureU || texture.height != mainTextureV)
					Debug.LogError("texture " + texture.name + " which is a " + refName + " has a different size than the main albedo texture, this will cause problems with atlasing, please fix.");

				if(texture.width >= atlas.GetTextureSize() + atlas.TextureMargin)
					return true;

				if(texture.height >= atlas.GetTextureSize() + atlas.TextureMargin)
					return true;
			}
			return false;
		}

		#endregion

		#region Set Textures to Readable

		private static List<BCAtlasSubMaterial> GetAllSubMaterials(BCAtlas[] atlasGroup)
		{
			List<BCAtlasSubMaterial> subMaterials = new List<BCAtlasSubMaterial>();

			for(int i = 0; i < atlasGroup.Length; i++)
			{
				subMaterials.AddRange(atlasGroup[i].SubMaterials);
			}

			return subMaterials;
		}

		private static void SetAllMapsToReadable(int atlasMaxSize, List<BCAtlasSubMaterial> subMaterials)
		{
			for(int subIndex = 0; subIndex < subMaterials.Count; subIndex++)
			{
				Material material = subMaterials[subIndex].MaterialReference;

				SetTextureToReadable(subMaterials[subIndex], "_MainTex");
				if(material.shader.name == "Standard")
					SetTextureToReadable(subMaterials[subIndex], "_MetallicGlossMap");
				if(material.shader.name == "Standard (Specular setup)")
					SetTextureToReadable(subMaterials[subIndex], "_SpecGlossMap");

				//				SetTextureToReadable(subMaterials[subIndex], "_BumpMap");
				{
					// Always check and set the normal map to a normal map
					Texture2D texture2D = material.GetTexture("_BumpMap") as Texture2D;
					SetTextureImporterFormat(texture2D, atlasMaxSize, false);
				}

				SetTextureToReadable(subMaterials[subIndex], "_ParallaxMap");
				SetTextureToReadable(subMaterials[subIndex], "_OcclusionMap");
				SetTextureToReadable(subMaterials[subIndex], "_EmissionMap");
			}
		}

		private static void SetTextureToReadable(BCAtlasSubMaterial sub, string textureName)
		{
			Texture2D texture2D = sub.MaterialReference.GetTexture(textureName) as Texture2D;
			if(texture2D == null)
			{
				//				Debug.Log(sub.MaterialReference.ToString() + "'s texture " + textureName + " was not found, returning");
				return;
			}

			// Texture is set to readable and ALWAYS not a normal map
			IsTextureReadable(8192, texture2D, false);
		}

		private static void SetNormalsBackToNormalMapping(int atlasSize, List<BCAtlasSubMaterial> subMaterials)
		{
			for(int subIndex = 0; subIndex < subMaterials.Count; subIndex++)
			{
				Texture2D texture2D = subMaterials[subIndex].MaterialReference.GetTexture("_BumpMap") as Texture2D;
				if(texture2D == null)
				{
					continue;
				}	

				SetTextureImporterFormat(texture2D, atlasSize, true);
			}
		}

		#endregion


		#region Import all Textures

		public static void LoadAndAssignTexturesToAtlases(BCAtlas[] atlasGroup, string path)
		{
			AssetDatabase.Refresh();

			// Reimport the normal atlas group maps
			for(int i = 0; i < atlasGroup.Length; i++)
			{
				BCAtlas bcAtlas = atlasGroup[i];
				Texture2D normalMap = ImportTextureAtPath(bcAtlas.AtlasName + "_normalTexture" + "_atlas.png", path);

				SetTextureImporterFormat(normalMap, bcAtlas.GetTextureSize(), true);
			}

			AssetDatabase.Refresh();

			for(int i = 0; i < atlasGroup.Length; i++)
			{
				BCAtlas bcAtlas = atlasGroup[i];

				atlasGroup[i].MainTexture = ImportTextureAtPath(bcAtlas.AtlasName + "_mainTexture" + "_atlas.png", path);
				SetTextureImporterFormat(atlasGroup[i].MainTexture, bcAtlas.GetTextureSize(), false);
				bcAtlas.Material.SetTexture("_MainTex", bcAtlas.MainTexture);

				atlasGroup[i].GlossTexture = ImportTextureAtPath(bcAtlas.AtlasName + "_glossTexture" + "_atlas.png", path);
				SetTextureImporterFormat(atlasGroup[i].GlossTexture, bcAtlas.GetTextureSize(), false);
				bcAtlas.Material.SetTexture("_MetallicGlossMap", bcAtlas.GlossTexture);
				bcAtlas.Material.SetFloat("_GlossMapScale", 0.5f); // NOTE this may need to change depending on how smooth things are

				atlasGroup[i].NormalMap = ImportTextureAtPath(bcAtlas.AtlasName + "_normalTexture" + "_atlas.png", path);
//				SetTextureImporterFormat(atlasGroup[i].GlossTexture, bcAtlas.GetTextureSize(), false); // Skip the normal map as it already has been done above
				bcAtlas.Material.SetTexture("_BumpMap", bcAtlas.NormalMap);

				atlasGroup[i].ParallaxMap = ImportTextureAtPath(bcAtlas.AtlasName + "_parallaxTexture" + "_atlas.png", path);
				SetTextureImporterFormat(atlasGroup[i].ParallaxMap, bcAtlas.GetTextureSize(), false);
				bcAtlas.Material.SetTexture("_ParallaxMap", bcAtlas.ParallaxMap);
				bcAtlas.Material.SetFloat("_Parallax", 0.005f);

				atlasGroup[i].OcclusionMap = ImportTextureAtPath(bcAtlas.AtlasName + "_occlusionTexture" + "_atlas.png", path);
				SetTextureImporterFormat(atlasGroup[i].OcclusionMap, bcAtlas.GetTextureSize(), false);
				bcAtlas.Material.SetTexture("_OcclusionMap", bcAtlas.OcclusionMap);

				atlasGroup[i].EmissionMap = ImportTextureAtPath(bcAtlas.AtlasName + "_emissionTexture" + "_atlas.png", path);
				SetTextureImporterFormat(atlasGroup[i].EmissionMap, bcAtlas.GetTextureSize(), false);
				bcAtlas.Material.SetTexture("_EmissionMap", bcAtlas.EmissionMap);
			}
		}

		private static Texture2D ImportTextureAtPath(string textureName, string path)
		{
			if(path.Length > 0 && path[path.Length - 1] != '/')
				path += "/";

			string loadPath = path + textureName;

			return (Texture2D)AssetDatabase.LoadAssetAtPath(loadPath, typeof(Texture2D));
		}

		#endregion

		public static BuildingCrafter.BCAtlas.TextureMapFlag FindTextureFlag(BCAtlas bcAtlas)
		{
			BCAtlas.TextureMapFlag flag = new BCAtlas.TextureMapFlag();

			for(int i = 0; i < bcAtlas.SubMaterials.Count; i++)
			{
				if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_MainTex") != null)
					flag |= BCAtlas.TextureMapFlag.MainTexture;

				if(bcAtlas.SubMaterials[i].MaterialReference.HasProperty("_MetallicGlossMap"))
				{
					if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_MetallicGlossMap") != null)
						flag |= BCAtlas.TextureMapFlag.GlossTexture;
				}

				if(bcAtlas.SubMaterials[i].MaterialReference.HasProperty("_SpecGlossMap"))
				{
					if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_SpecGlossMap") != null)
						flag |= BCAtlas.TextureMapFlag.GlossTexture;
				}

				if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_BumpMap") != null)
					flag |= BCAtlas.TextureMapFlag.NormalMap;

				if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_ParallaxMap") != null)
					flag |= BCAtlas.TextureMapFlag.ParallaxMap;

				if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_OcclusionMap") != null)
					flag |= BCAtlas.TextureMapFlag.OcclusionMap;

				if(bcAtlas.SubMaterials[i].MaterialReference.GetTexture("_EmissionMap") != null)
					flag |= BCAtlas.TextureMapFlag.EmissionMap;
			}

			return flag;
		}

		public static bool AddMaterialToAtlas(BCAtlas bcAtlas, Material subMaterial, out bool textureAtMaxSize)
		{
			textureAtMaxSize = false;
			if(subMaterial == null)
				return false;

			// TODO: Make sure we can still atlas things without a main texture
			if(subMaterial.mainTexture == null)
				return false;

			for(int i = 0; i < bcAtlas.SubMaterials.Count; i++)
			{
				// Test to see if the material has already been added
				if(bcAtlas.SubMaterials[i].MaterialReference == subMaterial)
					return false;
			}

			int newTextureSize = Mathf.Max(subMaterial.mainTexture.width, subMaterial.mainTexture.height);
			if(newTextureSize >= bcAtlas.GetTextureSize() - bcAtlas.TextureMargin)
			{
				textureAtMaxSize = true;
				return false;
			}

			IntVector2 firstFreeSpot;
			if(FindSmallestUVSpot(bcAtlas, newTextureSize, out firstFreeSpot))
			{
				if(subMaterial.mainTextureScale != Vector2.one)
					Debug.LogError("The submaterial " + subMaterial + " which is trying to be atlased has a scale that isn't 1,1. This will not be Atlased correctly and look strange. " +
						"If you want this feature please email building crafter at 8bitgoose.com. Otherwise scale your textures in photoshop or another program");

				if(subMaterial.mainTextureOffset != Vector2.zero)
					Debug.LogError("The submaterial " + subMaterial + " which is trying to be atlased has an offset that isn't 1, 1. This will not be Atlased correctly and look strange. " +
						"If you want this feature please email building crafter at 8bitgoose.com. Otherwise offset your textures in photoshop or another program");

				bcAtlas.SubMaterials.Add(new BCAtlasSubMaterial(firstFreeSpot, newTextureSize, bcAtlas.TextureMargin, bcAtlas.GetTextureSize(), subMaterial));
				return true;
			}

			//			Debug.LogError("Failed to add subMaterial, does not fit " + subMaterial); // Should probably break everything or do something new
			return false;
		}

		public static void CreateAtlasFromSubMaterials(BCAtlas bcAtlas, BCAtlas.TextureMapFlag flags, string atlasName, string path)
		{
			// If the material on this item is null, then we need to create a new atlas material
			Material atlasMaterial = new Material(Shader.Find("Standard"));

			SaveItemToFile(path, atlasName, atlasMaterial);
			bcAtlas.Material = atlasMaterial;

			// Set temp references for the blank items
			if((flags & BCAtlas.TextureMapFlag.MainTexture) == BCAtlas.TextureMapFlag.MainTexture)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Main Texture", 0.15f);
				bcAtlas.MainTexture = CreateTextureForAtlas(bcAtlas, Color.gray);
			}	

			if((flags & BCAtlas.TextureMapFlag.GlossTexture) == BCAtlas.TextureMapFlag.GlossTexture)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Gloss Texture", 0.3f);
				bcAtlas.GlossTexture = CreateTextureForAtlas(bcAtlas, Color.black);
			}

			if((flags & BCAtlas.TextureMapFlag.NormalMap) == BCAtlas.TextureMapFlag.NormalMap)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Normal Texture", 0.45f);
				bcAtlas.NormalMap = CreateTextureForAtlas(bcAtlas, new Color(130f/255f, 122f/255f, 1));
			}

			if((flags & BCAtlas.TextureMapFlag.ParallaxMap) == BCAtlas.TextureMapFlag.ParallaxMap)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Parallax Texture", 0.6f);
				bcAtlas.ParallaxMap = CreateTextureForAtlas(bcAtlas, Color.black);
			}

			if((flags & BCAtlas.TextureMapFlag.OcclusionMap) == BCAtlas.TextureMapFlag.OcclusionMap)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Occlusion Texture", 0.75f);
				bcAtlas.OcclusionMap = CreateTextureForAtlas(bcAtlas, Color.white);
			}

			if((flags & BCAtlas.TextureMapFlag.EmissionMap) == BCAtlas.TextureMapFlag.EmissionMap)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Creating New Emission Texture", 0.9f);
				bcAtlas.EmissionMap = CreateTextureForAtlas(bcAtlas, Color.white);
			}

			for(int subIndex = 0; subIndex < bcAtlas.SubMaterials.Count; subIndex++)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Writing Sub Material " + bcAtlas.SubMaterials[subIndex].MaterialReference.name + " to Atlas Textures", ((float)subIndex / (float)bcAtlas.SubMaterials.Count));
				WriteTexturesToAtlas(bcAtlas, bcAtlas.SubMaterials[subIndex]);
			}

			string basePath = GetBaseAppPath(Application.dataPath);

			// ============= MAIN TEXTURE =============
			// Now save the main textures to the harddrive for quickness
			if(bcAtlas.MainTexture != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Main Atlas Texture", .15f);
				SaveTexture(bcAtlas.MainTexture, atlasName + "_mainTexture" + "_atlas.png", path, basePath);
			}

			// ============= GLOSS TEXTURE =============
			if(bcAtlas.GlossTexture != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Gloss Texture", 0.3f);
				SaveTexture(bcAtlas.GlossTexture, atlasName + "_glossTexture" + "_atlas.png", path, basePath);
			}

			// ============= NORMAL TEXTURE =============
			if(bcAtlas.NormalMap != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Normal Texture", 0.45f);
				SaveTexture(bcAtlas.NormalMap, atlasName + "_normalTexture" + "_atlas.png", path, basePath);
			}

			// ============= PARALLAX TEXTURE =============
			if(bcAtlas.ParallaxMap != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Heightmap Texture", 0.6f);
				SaveTexture(bcAtlas.ParallaxMap, atlasName + "_parallaxTexture" + "_atlas.png", path, basePath);
			}
			// ============= OCCLUSION TEXTURE =============
			if(bcAtlas.OcclusionMap != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Occlusion Texture", 0.75f);
				SaveTexture(bcAtlas.OcclusionMap, atlasName + "_occlusionTexture" + "_atlas.png", path, basePath);
			}
			// ============= EMISSION TEXTURE =============
			if(bcAtlas.EmissionMap != null)
			{
				EditorUtility.DisplayProgressBar("Atlasing", "Saving Emission Texture", .9f);
				SaveTexture(bcAtlas.EmissionMap, atlasName + "_emissionTexture" + "_atlas.png", path, basePath);
			}
		}

		public static Texture2D CreateTextureForAtlas(BCAtlas bcAtlas, Color fillColor, bool isNormal = false)
		{
			Texture2D atlasTexture = new Texture2D(bcAtlas.GetTextureSize(), bcAtlas.GetTextureSize(), TextureFormat.RGBA32, false);
			SetTextureImporterFormat(atlasTexture, bcAtlas.GetTextureSize(), isNormal);
			Color[] allColors = new Color[bcAtlas.GetTextureSize() * bcAtlas.GetTextureSize()];
			for(int i = 0; i < allColors.Length; i++)
				allColors[i] = fillColor;

			atlasTexture.SetPixels(allColors);

			return atlasTexture;
		}

		public static void WriteTexturesToAtlas(BCAtlas bcAtlas, BCAtlasSubMaterial subMaterial)
		{
			Material material = subMaterial.MaterialReference;

			bcAtlas.MainTexture = WriteTextureToAtlas(bcAtlas, bcAtlas.MainTexture, "_MainTex", subMaterial);

			if(material.shader.name == "Standard")
				bcAtlas.GlossTexture = WriteTextureToAtlas(bcAtlas, bcAtlas.GlossTexture, "_MetallicGlossMap", subMaterial);
			if(material.shader.name == "Standard (Specular setup)")
				bcAtlas.GlossTexture = WriteTextureToAtlas(bcAtlas, bcAtlas.GlossTexture, "_SpecGlossMap", subMaterial);

			// TODO: modify the strength of the normal map based on the values fed in from the submaterial
			bcAtlas.NormalMap = WriteTextureToAtlas(bcAtlas, bcAtlas.NormalMap, "_BumpMap", subMaterial);

			// HACK - larger heightmap can cause weird parallax, so we need to divide the heightmap amount by a certain amount
			float amount = 1;
			switch(bcAtlas.RenderSize)
			{
			case BCAtlas.Size.Small2048:
				amount = 1.25f;
				break;

			case BCAtlas.Size.Medium4096:
				amount = 2f;
				break;

			case BCAtlas.Size.Large8192:
				amount = 4f;
				break;
			}

			bcAtlas.ParallaxMap = WriteTextureToAtlas(bcAtlas, bcAtlas.ParallaxMap, "_ParallaxMap", subMaterial, amount);
			bcAtlas.OcclusionMap = WriteTextureToAtlas(bcAtlas, bcAtlas.OcclusionMap, "_OcclusionMap", subMaterial);
			bcAtlas.EmissionMap = WriteTextureToAtlas(bcAtlas, bcAtlas.EmissionMap, "_EmissionMap", subMaterial);

			// MAPS TO EVENTUALLY INTEGRATE
			// "_DetailMask"
			// "_DetailAlbedoMap"
			// "_DetailNormalMap"
		}

		public static Texture2D WriteTextureToAtlas(BCAtlas bcAtlas, Texture2D atlasTexture, string textureName, BCAtlasSubMaterial sub, float darkenAmount = 1)
		{
			Texture2D texture2D = sub.MaterialReference.GetTexture(textureName) as Texture2D;
			if(texture2D == null)
			{
				return atlasTexture;
			}	

			if(atlasTexture == null)
			{
				Debug.Log("Atlas texture is null this time on " + bcAtlas.AtlasName + "[" + bcAtlas.ToString() + "]");
				atlasTexture = new Texture2D(bcAtlas.GetTextureSize(), bcAtlas.GetTextureSize(), TextureFormat.RGBA32, false);
			}

			// Create a temp pixel texture that we can modify
			Texture2D writingTexture = new Texture2D(texture2D.width, texture2D.height);
			Color[] pixels = texture2D.GetPixels();

			if(darkenAmount != 1)
			{
				for(int i = 0; i < pixels.Length; i++)
				{
					Color localColour = pixels[i];
					localColour.r /= darkenAmount;
					localColour.g /= darkenAmount;
					localColour.b /= darkenAmount;
					pixels[i] = localColour;
				}
			}

			writingTexture.SetPixels(pixels);
				
			atlasTexture.SetPixels(sub.StartPos.X, sub.StartPos.Y, sub.Size, sub.Size, writingTexture.GetPixels());
			WriteRepeatingTextures(atlasTexture, sub.StartPos, sub.Size, bcAtlas.TextureMargin, writingTexture);

			return atlasTexture;
		}

		public static void IsTextureReadable(int maxTextureSize, Texture2D texture, bool isNormal = false)
		{
			try
			{
				texture.GetPixel(0, 0);
			}
			catch(UnityException e)
			{
				if(e.Message.StartsWith("Texture '" + texture.name + "' is not readable"))
				{
					int size = Mathf.Min(maxTextureSize, 8192);
					SetTextureImporterFormat(texture, size, isNormal);
				}
			}
		}

		#region Importing Textures

		public static void ImportNewTexturesAtProperSize(BCAtlas[] atlasGroup)
		{
			
		}

		#endregion

		#region Writing Textures

		public static void WriteRepeatingTextures(Texture2D writeToTexture, IntVector2 startPos, int size, int margin, Texture2D textureToReadFrom)
		{
			// Write the top left side
			// write the middle left side
			Color[] cornerColours = new Color[margin * margin];
			Color[] sideColours = new Color[size * margin];

			for(int i = 0; i < cornerColours.Length; i++)
				cornerColours[i] = Color.magenta;

			for(int i = 0; i < sideColours.Length; i++)
				sideColours[i] = Color.magenta;
			
			// leftBottom
			cornerColours = textureToReadFrom.GetPixels(size - margin, size - margin, margin, margin);
			writeToTexture.SetPixels(startPos.X - margin, startPos.Y - margin, margin, margin, cornerColours);

			// leftTop
			cornerColours = textureToReadFrom.GetPixels(size - margin, 0, margin, margin);
			writeToTexture.SetPixels(startPos.X - margin, startPos.Y + size, margin, margin, cornerColours);

			// rightBottom
			cornerColours = textureToReadFrom.GetPixels(0, size - margin, margin, margin);
			writeToTexture.SetPixels(startPos.X + size, startPos.Y - margin, margin, margin, cornerColours);

			// rightTop
			cornerColours = textureToReadFrom.GetPixels(0, 0, margin, margin);
			writeToTexture.SetPixels(startPos.X + size, startPos.Y + size, margin, margin, cornerColours);

			// left
			// to get the repeating, we need to grab the end of the block by the margin amount
			sideColours = textureToReadFrom.GetPixels(size - margin, 0, margin, size);
			writeToTexture.SetPixels(startPos.X - margin, startPos.Y, margin, size, sideColours);

			// right
			sideColours = textureToReadFrom.GetPixels(0, 0, margin, size);
			writeToTexture.SetPixels(startPos.X + size, startPos.Y, margin, size, sideColours);

			// bottom
			sideColours = textureToReadFrom.GetPixels(0, size - margin, size, margin);
			writeToTexture.SetPixels(startPos.X, startPos.Y - margin, size, margin, sideColours);

			// top
			sideColours = textureToReadFrom.GetPixels(0, 0, size, margin);
			writeToTexture.SetPixels(startPos.X, startPos.Y + size, size, margin, sideColours);
		}


		public static bool FindSmallestUVSpot(BCAtlas atlas, int newSize, out IntVector2 newSpot)
		{
			int margin = atlas.TextureMargin;
			newSpot = new IntVector2(0, 0);
			// 1. Find all the same U's that are small. Test to see if above will fit in
			// 2. If this fails increase to the smallest U of the found points
			// 3. Test to see if it will fit in at the end of that (or at 0, newV)
			List<int> possibleIntersections = new List<int>();
			int startU = 0;

			if(atlas.SubMaterials.Count == 0)
				return true;

			int breaker = 0;
			while(breaker < 32)
			{
				breaker++;
				for(int i = 0; i < atlas.SubMaterials.Count; i++)
				{
					// Find out if the start U falls on the sub atlas. If it does, add it to the list of things that can collide
					if(CollidesInU(atlas, atlas.SubMaterials[i], startU))
					{
						possibleIntersections.Add(i);
					}
				}

				// Just try to add the spot to a new spot
				for(int index = 0; index < possibleIntersections.Count; index++)
				{
					BCAtlasSubMaterial subMaterial = atlas.SubMaterials[possibleIntersections[index]];
					int topV = subMaterial.StartPos.Y + subMaterial.Size + atlas.TextureMargin;
					IntVector2 testStart = new IntVector2(startU, topV);

					if(CanTextureFitAtPoint(atlas, testStart, newSize))
					{
						newSpot = new IntVector2(startU, topV);
						//						Debug.Log(newSpot);
						return true;
					}
				}

				// It doesn't fit at the current spot, so we find the smallest V and try and fit it there
				int newStartValue = int.MaxValue;

				int smallestSize = atlas.GetTextureSize();
				for(int index = 0; index < possibleIntersections.Count; index++)
				{
					BCAtlasSubMaterial subMaterial = atlas.SubMaterials[possibleIntersections[index]];
					int newU = subMaterial.StartPos.X + subMaterial.Size + margin;
					if(newStartValue > newU)
						newStartValue = newU;

					// Find the smallest submaterials
					if(subMaterial.Size < smallestSize)
						smallestSize = subMaterial.Size;
				}

				// Find all the sub materials which are the smallest size

				for(int index = 0; index < possibleIntersections.Count; index++)
				{
					BCAtlasSubMaterial subMaterial = atlas.SubMaterials[possibleIntersections[index]];

					// Find the smallest submaterials
					if(subMaterial.Size == smallestSize)
					{
						int newU = subMaterial.StartPos.X + subMaterial.Size + margin;
						int newV = subMaterial.StartPos.Y - margin;

						if(newStartValue < newU)
							newStartValue = newU;

						if(CanTextureFitAtPoint(atlas, new IntVector2(newU, newV), newSize))
						{
							newSpot = new IntVector2(newU, newV);
							return true;
						}
					}
				}

				startU = newStartValue;
			}

			return false;

		}

		public static bool CollidesInU(BCAtlas atlas, BCAtlasSubMaterial sub, int uInQuestion)
		{
			int lowerBound = sub.StartPos.X - atlas.TextureMargin;
			int upperBound = sub.StartPos.X + sub.Size + atlas.TextureMargin;

			if(uInQuestion >= lowerBound && uInQuestion < upperBound)
				return true;

			return false;
		}

		public static bool CanTextureFitAtPoint(BCAtlas atlas, IntVector2 startPointWithMargin, int newSize)
		{
			IntVector2 upperEnd = startPointWithMargin + new IntVector2(atlas.TextureMargin + newSize + atlas.TextureMargin, atlas.TextureMargin + newSize + atlas.TextureMargin);

			if(upperEnd.Y > atlas.GetTextureSize() || upperEnd.X > atlas.GetTextureSize())
				return false;

			for(int i = 0; i < atlas.SubMaterials.Count; i++)
			{
				BCAtlasSubMaterial subMaterial = atlas.SubMaterials[i];

				IntVector2 lowerStartCorner = startPointWithMargin + new IntVector2(0, 1);
				IntVector2 upperStartCorner = startPointWithMargin + new IntVector2(0, newSize + atlas.TextureMargin) + new IntVector2(0, 1);
				IntVector2 lowerEndCorner = startPointWithMargin + new IntVector2(newSize + atlas.TextureMargin, 0) + new IntVector2(0, 1);
				IntVector2 upperEndCorner = startPointWithMargin + new IntVector2(newSize + atlas.TextureMargin, newSize + atlas.TextureMargin) + new IntVector2(0, 1);

				if(IsPointWithinSubAtlas(subMaterial, lowerStartCorner, atlas.TextureMargin))
					return false;

				if(IsPointWithinSubAtlas(subMaterial, upperStartCorner, atlas.TextureMargin))
					return false;

				if(IsPointWithinSubAtlas(subMaterial, lowerEndCorner, atlas.TextureMargin))
					return false;

				if(IsPointWithinSubAtlas(subMaterial, upperEndCorner, atlas.TextureMargin))
					return false;

			}

			return true;
		}

		public static bool IsPointWithinSubAtlas(BCAtlasSubMaterial sub, IntVector2 point, int margin)
		{
			IntVector2 lowerBound = sub.StartPos - new IntVector2(margin, margin);
			IntVector2 upperBound = sub.StartPos + new IntVector2(sub.Size + margin, sub.Size + margin);

			bool pointFallsWithinXBounds = point.X >= lowerBound.X && point.X < upperBound.X;
			bool pointFallsWithinYBounds = point.Y >= lowerBound.Y && point.Y < upperBound.Y;

			if(pointFallsWithinXBounds && pointFallsWithinYBounds)
				return true;

			return false;
		}

		// MUST REFRESH DATABASE AFTER THIS
		public static void SetTextureImporterFormat(Texture2D texture, int maxSize, bool isNormal = false)
		{
			if (null == texture) 
				return;

			string assetPath = AssetDatabase.GetAssetPath(texture);
			var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if(tImporter != null)
			{
				if(isNormal == false)
					tImporter.textureType = TextureImporterType.Default;
				else
					tImporter.textureType = TextureImporterType.NormalMap;
				tImporter.isReadable = true;
				tImporter.textureCompression = TextureImporterCompression.Uncompressed;
				tImporter.maxTextureSize = maxSize;
				AssetDatabase.ImportAsset(assetPath);
			}
		}

		public static void SaveItemToFile(string assetFolderPath, string fileName, Object obj)
		{
			string newFilePath = assetFolderPath;

			if(newFilePath.Length > 0 && newFilePath[0] == '/')
				newFilePath = newFilePath.Remove(0, 1);

			if(newFilePath.StartsWith("Assets") == false)
				newFilePath = newFilePath.Insert(0, "Assets/");

			// Adds a slash to the end so we have a proper file path
			if(newFilePath.Length > 0 && newFilePath[newFilePath.Length - 1] != '/')
				newFilePath += "/";

			newFilePath += fileName + ".mat";

			AssetDatabase.CreateAsset(obj, newFilePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public static void SaveTexture(Texture2D texture, string textureName, string texturePathFolder, string basePath)
		{
			string fullPath = basePath + "/" + texturePathFolder + "/" + textureName;

			if(texture == null)
			{
				Debug.Log("Texture is null (" + textureName + ")");
				return;
			}	

			byte[] bytes = texture.EncodeToPNG();
			File.WriteAllBytes(fullPath, bytes);
		}

		/// <summary>
		/// Must feed in Application.dataPath;
		/// </summary>
		/// <returns>The bass app path.</returns>
		/// <param name="applicateBase">Applicate base.</param>
		public static string GetBaseAppPath(string applicationDOTdataPath)
		{
			int count = applicationDOTdataPath.Length;
			return applicationDOTdataPath.Remove(count - 7, 7);
		}

		public static string FindFolderFromAssetName(string fullAssetPath)
		{
			int lastSlash = fullAssetPath.LastIndexOf("/");
			int lastPeriod = fullAssetPath.LastIndexOf(".");
			if(lastPeriod > lastSlash)
			{
				return fullAssetPath.Remove(lastPeriod, fullAssetPath.Length - lastPeriod);
			}

			return fullAssetPath;
		}

		#endregion
	}
}