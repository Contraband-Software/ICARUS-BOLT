using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Object2Terrain : EditorWindow {
 
	[MenuItem("Terrain/Object to Terrain", false, 2000)] static void OpenWindow () {
 
		EditorWindow.GetWindow<Object2Terrain>(true);
	}
 
	private int resolution = 512;
	private Vector3 addTerrain;
	int bottomTopRadioSelected = 0;
	static string[] bottomTopRadio = new string[] { "Bottom Up", "Top Down"};
	private float shiftHeight = 0f;
    private int detailResolution = 32;
 
	void OnGUI () {
 
		resolution = EditorGUILayout.IntField("Resolution", resolution);
		addTerrain = EditorGUILayout.Vector3Field("Add terrain", addTerrain);
		shiftHeight = EditorGUILayout.Slider("Shift height", shiftHeight, -1f, 1f);
		bottomTopRadioSelected = GUILayout.SelectionGrid(bottomTopRadioSelected, bottomTopRadio, bottomTopRadio.Length, EditorStyles.radioButton);
 
		if(GUILayout.Button("Create Terrain")){
 
			if(Selection.activeGameObject == null){
 
				EditorUtility.DisplayDialog("No object selected", "Please select an object.", "Ok");
				return;
			}
 
			else{
 
				CreateTerrain();
			}
		}
	}
 
	delegate void CleanUp();

    void CreateTerrain()
    {
        // Fire up the progress bar
        ShowProgressBar(1, 100);

        // Create a new TerrainData object
        TerrainData terrain = new TerrainData();
        terrain.heightmapResolution = resolution;
        terrain.SetDetailResolution(detailResolution, detailResolution);
        terrain.alphamapResolution = resolution - 1;

        // Save the TerrainData as an asset in the project
        string path = "Assets/TerrainData"; // Adjust the path as needed
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "TerrainData");
        }

        // Find the next available number for the new asset
        string[] existingAssets = AssetDatabase.FindAssets("t:TerrainData", new[] { path });
        int highestNumber = 0;
        foreach (string asset in existingAssets)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(asset);
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (int.TryParse(assetName.Substring("TerrainData_".Length), out int number))
            {
                if (number > highestNumber)
                {
                    highestNumber = number;
                }
            }
        }

        // Generate a unique asset path with the next available number
        string terrainDataName = $"TerrainData_{highestNumber + 1}";
        string terrainDataPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{terrainDataName}.asset");
        AssetDatabase.CreateAsset(terrain, terrainDataPath);
        AssetDatabase.SaveAssets();

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrain);

        // Register the creation of the terrain object for undo
        Undo.RegisterCreatedObjectUndo(terrainObject, "Object to Terrain");

        // Ensure at least one terrain layer is added
        if (terrain.terrainLayers.Length == 0)
        {
            TerrainLayer defaultLayer = new TerrainLayer();
            terrain.terrainLayers = new TerrainLayer[] { defaultLayer };
        }

        // Get the MeshCollider from the selected object
        MeshCollider collider = Selection.activeGameObject.GetComponent<MeshCollider>();
        CleanUp cleanUp = null;

        // Add a collider to our source object if it does not exist.
        // Otherwise, raycasting won't work.
        if (!collider)
        {
            collider = Selection.activeGameObject.AddComponent<MeshCollider>();
            cleanUp = () => DestroyImmediate(collider);
        }

        // Get the bounds of the collider
        Bounds bounds = collider.bounds;
        float sizeFactor = collider.bounds.size.y / (collider.bounds.size.y + addTerrain.y);
        terrain.size = collider.bounds.size + addTerrain;
        bounds.size = new Vector3(terrain.size.x, collider.bounds.size.y, terrain.size.z);

        // Set the position of the terrain object to the position of the selected object
        Vector3 objectPosition = Selection.activeGameObject.transform.position;
        terrainObject.transform.position = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);

        // Initialize height and hole maps
        float[,] heights = new float[terrain.heightmapResolution, terrain.heightmapResolution];
        bool[,] holes = new bool[terrain.holesResolution, terrain.holesResolution];

        // Do raycasting samples over the object to see what terrain heights should be
        Ray ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
        RaycastHit hit = new RaycastHit();
        float meshHeightInverse = 1 / bounds.size.y;
        Vector3 rayOrigin = ray.origin;

        int maxHeight = heights.GetLength(0);
        int maxLength = heights.GetLength(1);

        Vector2 stepXZ = new Vector2(bounds.size.x / maxLength, bounds.size.z / maxHeight);

        for (int zCount = 0; zCount < maxHeight; zCount++)
        {
            ShowProgressBar(zCount, maxHeight);

            for (int xCount = 0; xCount < maxLength; xCount++)
            {
                float height = 0.0f;
                bool hitTerrain = false;

                if (collider.Raycast(ray, out hit, bounds.size.y * 3))
                {
                    height = (hit.point.y - bounds.min.y) * meshHeightInverse;
                    height += shiftHeight;

                    // Bottom up
                    if (bottomTopRadioSelected == 0)
                    {
                        height *= sizeFactor;
                    }

                    // Clamp the height
                    if (height < 0)
                    {
                        height = 0;
                    }

                    hitTerrain = true;
                }

                heights[zCount, xCount] = height;

                // Convert coordinates to hole resolution
                int holeX = Mathf.FloorToInt((float)xCount / maxLength * terrain.holesResolution);
                int holeZ = Mathf.FloorToInt((float)zCount / maxHeight * terrain.holesResolution);

                // Set holes (true means no hole, false means hole)
                if (holeX < terrain.holesResolution && holeZ < terrain.holesResolution)
                {
                    holes[holeZ, holeX] = hitTerrain;
                }

                rayOrigin.x += stepXZ[0];
                ray.origin = rayOrigin;
            }

            rayOrigin.z += stepXZ[1];
            rayOrigin.x = bounds.min.x;
            ray.origin = rayOrigin;
        }

        // Set the heights of the terrain
        terrain.SetHeights(0, 0, heights);

        // Apply holes to the terrain
        terrain.SetHoles(0, 0, holes);

        // set as sibling of selected mesh
        terrainObject.transform.SetParent(Selection.activeGameObject.transform.parent, true);

        // Clear the progress bar
        EditorUtility.ClearProgressBar();

        // Clean up if necessary
        if (cleanUp != null)
        {
            cleanUp();
        }

        // Refresh the Asset Database to ensure the saved data is correctly recognized
        AssetDatabase.Refresh();

        // Clean up unused TerrainData assets
        CleanUpUnusedTerrainDataAssets();
    }

    void CleanUpUnusedTerrainDataAssets()
    {
        // Get all TerrainData assets
        string[] assetGuids = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/TerrainData" });
        HashSet<string> usedAssets = new HashSet<string>();

        // Find all Terrain objects in the scene
        Terrain[] terrains = GameObject.FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(terrain.terrainData);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    usedAssets.Add(assetPath);
                }
            }
        }

        // Delete TerrainData assets that are not used
        foreach (string guid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!usedAssets.Contains(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        // Refresh the Asset Database to update the project view
        AssetDatabase.Refresh();
    }


    void ShowProgressBar(float progress, float maxProgress){
 
		float p = progress / maxProgress;
		EditorUtility.DisplayProgressBar("Creating Terrain...", Mathf.RoundToInt(p * 100f)+ " %", p);
	}
}