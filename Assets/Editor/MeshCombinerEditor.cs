using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(MeshCombiner))]
public class MeshCombinerLODEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (just the material slot now)
        base.OnInspectorGUI();

        MeshCombiner combiner = (MeshCombiner)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Controls are disabled in Play Mode.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

        GameObject existingCombinedObject = combiner.GetCombinedObject();

        if (existingCombinedObject != null)
        {
            // If the combined object exists, show the restore button
            if (GUILayout.Button("Restore Original Meshes"))
            {
                RestoreOriginals(combiner);
            }
        }
        else
        {
            // If no combined object exists, show the combine button
            if (GUILayout.Button("Bake Combined LOD Meshes"))
            {
                if (combiner.combinedMaterial == null)
                {
                    Debug.LogError("Please assign a Combined Material before baking.", combiner.gameObject);
                    return;
                }
                BakeCombinedMeshes(combiner);
            }
        }
    }

    private void BakeCombinedMeshes(MeshCombiner combiner)
    {
        Transform parentTransform = combiner.transform;

        var lodRenderers = new Dictionary<int, List<CombineInstance>>();
        var originalObjectsToProcess = new List<GameObject>();

        // --- Step 1: Collect mesh data from all valid children ---
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform child = parentTransform.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;

            LODGroup lodGroup = child.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                originalObjectsToProcess.Add(child.gameObject);
                LOD[] lods = lodGroup.GetLODs();
                for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
                {
                    if (!lodRenderers.ContainsKey(lodIndex))
                    {
                        lodRenderers[lodIndex] = new List<CombineInstance>();
                    }

                    foreach (Renderer renderer in lods[lodIndex].renderers)
                    {
                        if (renderer == null) continue;

                        Mesh meshToCombine = GetMeshFromRenderer(renderer);
                        if (meshToCombine != null)
                        {
                            CombineInstance ci = new CombineInstance
                            {
                                mesh = meshToCombine,
                                transform = parentTransform.worldToLocalMatrix * renderer.transform.localToWorldMatrix
                            };
                            lodRenderers[lodIndex].Add(ci);
                        }
                    }
                }
            }
        }
        
        if (originalObjectsToProcess.Count == 0)
        {
            Debug.LogWarning("No active children with LODGroup components found to combine.", combiner.gameObject);
            return;
        }

        // --- Step 2: Create the new parent object and components ---
        GameObject combinedLODObject = new GameObject(parentTransform.name + "_LOD_Combined");
        Undo.RegisterCreatedObjectUndo(combinedLODObject, "Create Combined LOD Object");
        combinedLODObject.transform.SetParent(parentTransform, false); 
        combinedLODObject.transform.position = parentTransform.position;
        combinedLODObject.transform.rotation = parentTransform.rotation;
        combinedLODObject.isStatic = true; // Mark as static for lighting, occlusion, etc.
        
        LODGroup newLODGroup = Undo.AddComponent<LODGroup>(combinedLODObject);
        var newLODs = new List<LOD>();

        // --- Step 3: Combine meshes and generate Lightmap UVs for each LOD level ---
        foreach (var entry in lodRenderers.OrderBy(kvp => kvp.Key))
        {
            int lodIndex = entry.Key;
            List<CombineInstance> combineInstances = entry.Value;
            if (combineInstances.Count == 0) continue;

            GameObject lodMeshObject = new GameObject("LOD" + lodIndex) { isStatic = true };
            Undo.RegisterCreatedObjectUndo(lodMeshObject, "Create LOD Child");
            lodMeshObject.transform.SetParent(combinedLODObject.transform, false);

            Mesh combinedMesh = new Mesh
            {
                name = parentTransform.name + "_LOD" + lodIndex + "_Mesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
            
            // --- CRUCIAL STEP: Generate Lightmap UVs ---
            Unwrapping.GenerateSecondaryUVSet(combinedMesh);

            var newMf = Undo.AddComponent<MeshFilter>(lodMeshObject);
            newMf.sharedMesh = combinedMesh;
            var newMr = Undo.AddComponent<MeshRenderer>(lodMeshObject);
            newMr.sharedMaterial = combiner.combinedMaterial;
            
            newLODs.Add(new LOD(GetScreenRelativeTransitionHeightForLOD(lodIndex), new Renderer[] { newMr }));
        }

        newLODGroup.SetLODs(newLODs.ToArray());
        newLODGroup.RecalculateBounds();

        // --- Step 4: Disable renderers on original objects ---
        foreach (GameObject originalObject in originalObjectsToProcess)
        {
            ToggleOriginalObject(originalObject, false);
        }

        // --- Step 5: Store reference and finalize ---
        Undo.RecordObject(combiner, "Set Combined Object Reference");
        combiner.SetCombinedObject(combinedLODObject);

        Debug.Log($"Successfully baked meshes for {originalObjectsToProcess.Count} objects into '{combinedLODObject.name}'.", combinedLODObject);
    }

    private void RestoreOriginals(MeshCombiner combiner)
    {
        GameObject combinedObject = combiner.GetCombinedObject();
        if (combinedObject == null)
        {
            Debug.LogWarning("No combined object reference found to restore.", combiner.gameObject);
            Undo.RecordObject(combiner, "Clear Broken Reference");
            combiner.SetCombinedObject(null); 
            return;
        }

        // --- Step 1: Re-enable all original renderers and LOD Groups ---
        for (int i = 0; i < combiner.transform.childCount; i++)
        {
            GameObject originalObject = combiner.transform.GetChild(i).gameObject;
            if(originalObject.GetComponent<LODGroup>() != null)
            {
                ToggleOriginalObject(originalObject, true);
            }
        }
        
        // --- Step 2: Destroy the combined object ---
        Undo.DestroyObjectImmediate(combinedObject);

        // --- Step 3: Clear the reference ---
        Undo.RecordObject(combiner, "Clear Combined Object Reference");
        combiner.SetCombinedObject(null);
        
        Debug.Log("Successfully restored original objects and deleted the combined mesh.");
    }

    /// <summary>
    /// Helper to enable/disable renderers and LODGroups on an original object.
    /// </summary>
    private void ToggleOriginalObject(GameObject originalObject, bool enable)
    {
        var lodGroup = originalObject.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            Undo.RecordObject(lodGroup, (enable ? "Enable" : "Disable") + " LOD Group");
            lodGroup.enabled = enable;
        }

        var renderersToToggle = originalObject.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderersToToggle)
        {
            Undo.RecordObject(rend, (enable ? "Enable" : "Disable") + " Renderer");
            rend.enabled = enable;
        }
    }
    
    /// <summary>
    /// Helper to get a mesh from a renderer, supporting both MeshFilter and SkinnedMeshRenderer.
    /// </summary>
    private Mesh GetMeshFromRenderer(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer smr)
        {
            return smr.sharedMesh;
        }
        if (renderer.TryGetComponent<MeshFilter>(out var mf))
        {
            return mf.sharedMesh;
        }
        return null;
    }

    private float GetScreenRelativeTransitionHeightForLOD(int lodIndex)
    {
        if (lodIndex == 0) return 0.8f;
        if (lodIndex == 1) return 0.6f;
        if (lodIndex == 2) return 0.3f;
        return 0.04f;
    }
}

