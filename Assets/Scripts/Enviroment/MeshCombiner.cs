using UnityEngine;

/// <summary>
/// A component that marks a GameObject as a root for LOD mesh combination.
/// The actual combination logic is handled by the MeshCombinerLODEditor script.
/// </summary>
public class MeshCombiner : MonoBehaviour
{
    [Tooltip("The material to be used by the combined meshes. All source objects should ideally share this material.")]
    public Material combinedMaterial;

    [Tooltip("If true, the original child GameObjects will be destroyed after combining. This saves memory but is a permanent action at runtime. If false, only their renderers are disabled.")]
    public bool destroyOriginals = false;


    [Tooltip("A reference to the combined GameObject. This is managed by the editor script.")]
    [SerializeField] // Keep the reference serialized but hide it from the default inspector.
    private GameObject combinedLODObject;

    // The editor script will use these methods to safely manage the reference
    public GameObject GetCombinedObject() => combinedLODObject;
    public void SetCombinedObject(GameObject obj) => combinedLODObject = obj;
}




// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
//
// /// <summary>
// /// This component combines the meshes of all children with LOD Groups at runtime or via the editor.
// /// Attach this script to the parent GameObject containing all the child objects you wish to combine.
// /// </summary>
// public class MeshCombiner : MonoBehaviour
// {
//     [Tooltip("The material to be used by the combined meshes. All source objects should ideally share a material.")]
//     public Material combinedMaterial;
//
//     [Tooltip("If true, the meshes will be combined automatically when the scene starts.")]
//     public bool combineAtRuntime = true;
//
//     [Tooltip("If true, the original child GameObjects will be destroyed after combining. This saves memory but is a permanent action at runtime. If false, only their renderers are disabled.")]
//     public bool destroyOriginals = false;
//
//     private GameObject combinedLODObject;
//
//     void Start()
//     {
//         // If the flag is set, run the combination logic when the game starts.
//         if (combineAtRuntime)
//         {
//             CombineMeshes();
//         }
//     }
//
//     /// <summary>
//     /// The core logic for combining meshes. Can be called from Start() or from the editor script.
//     /// </summary>
//     public void CombineMeshes()
//     {
//         return;
//         // If we've already combined, destroy the old one first to allow for re-combining.
//         if (combinedLODObject != null)
//         {
//             Debug.LogWarning("Meshes have already been combined. Destroying previous instance to recombine.", this);
//             Destroy(combinedLODObject);
//         }
//
//         Transform parentTransform = transform;
//
//         // --- Step 1: Collect all LOD renderers from active children ---
//         var lodRenderers = new Dictionary<int, List<MeshFilter>>();
//         var originalObjectsToProcess = new List<GameObject>();
//
//         for (int i = 0; i < parentTransform.childCount; i++)
//         {
//             Transform child = parentTransform.GetChild(i);
//             if (!child.gameObject.activeInHierarchy) continue; // Skip inactive objects
//
//             LODGroup lodGroup = child.GetComponent<LODGroup>();
//             if (lodGroup != null)
//             {
//                 originalObjectsToProcess.Add(child.gameObject);
//                 LOD[] lods = lodGroup.GetLODs();
//                 for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
//                 {
//                     if (!lodRenderers.ContainsKey(lodIndex))
//                     {
//                         lodRenderers[lodIndex] = new List<MeshFilter>();
//                     }
//
//                     foreach (Renderer renderer in lods[lodIndex].renderers)
//                     {
//                         if (renderer != null)
//                         {
//                             MeshFilter mf = renderer.GetComponent<MeshFilter>();
//                             if (mf != null && mf.sharedMesh != null)
//                             {
//                                 lodRenderers[lodIndex].Add(mf);
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//         
//         if (originalObjectsToProcess.Count == 0)
//         {
//             Debug.LogWarning("No active children with LODGroup components found to combine.", gameObject);
//             return;
//         }
//
//         // --- Step 2: Create the new parent object for the combined meshes ---
//         combinedLODObject = new GameObject(parentTransform.name + "_LOD_Combined");
//         combinedLODObject.transform.position = parentTransform.position;
//         combinedLODObject.transform.rotation = parentTransform.rotation;
//         
//         LODGroup newLODGroup = combinedLODObject.AddComponent<LODGroup>();
//         var newLODs = new List<LOD>();
//
//         // --- Step 3: Combine meshes for each LOD level ---
//         foreach (var entry in lodRenderers.OrderBy(kvp => kvp.Key))
//         {
//             int lodIndex = entry.Key;
//             List<MeshFilter> meshFilters = entry.Value;
//             if (meshFilters.Count == 0)continue;
//
//             var combineInstances = new List<CombineInstance>();
//             foreach(MeshFilter mf in meshFilters)
//             {
//                 CombineInstance ci = new CombineInstance();
//                 ci.mesh = mf.sharedMesh;
//                 ci.transform = parentTransform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
//                 combineInstances.Add(ci);
//             }
//
//             GameObject lodMeshObject = new GameObject("LOD" + lodIndex);
//             lodMeshObject.transform.SetParent(combinedLODObject.transform, false);
//
//             Mesh combinedMesh = new Mesh();
//             combinedMesh.name = parentTransform.name + "_LOD" + lodIndex + "_Mesh";
//             combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
//             
//             lodMeshObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
//             MeshRenderer meshRenderer = lodMeshObject.AddComponent<MeshRenderer>();
//             meshRenderer.sharedMaterial = combinedMaterial;
//             
//             newLODs.Add(new LOD(GetScreenRelativeTransitionHeightForLOD(lodIndex), new Renderer[] { meshRenderer }));
//         }
//
//         // --- Step 4: Finalize the new LOD Group and process original objects ---
//         newLODGroup.SetLODs(newLODs.ToArray());
//         newLODGroup.RecalculateBounds();
//
//         foreach(GameObject originalObject in originalObjectsToProcess)
//         {
//             if (destroyOriginals)
//             {
//                 Destroy(originalObject);
//             }
//             else
//             {
//                 // Disable renderers to keep colliders and other scripts active
//                 var renderersToDisable = originalObject.GetComponentsInChildren<Renderer>();
//                 foreach(var rend in renderersToDisable)
//                 {
//                     rend.enabled = false;
//                 }
//                 
//                 var LODsToDisable = originalObject.GetComponentsInChildren<LODGroup>();
//                 foreach(var LOD in LODsToDisable)
//                 {
//                     LOD.enabled = false;
//                 }
//             }
//         }
//
//         Debug.Log($"Successfully combined meshes for {originalObjectsToProcess.Count} objects into '{combinedLODObject.name}'.", combinedLODObject);
//     }
//
//     // Helper to set some default LOD transition heights.
//     private float GetScreenRelativeTransitionHeightForLOD(int lodIndex)
//     {
//         // Increased values for more aggressive LOD transitions.
//         // This causes lower-detail LODs to be used sooner (when the object is larger on screen).
//         if (lodIndex == 0) return 0.8f; // High detail
//         if (lodIndex == 1) return 0.4f; // Medium detail
//         if (lodIndex == 2) return 0.2f; // Low detail
//         return 0.1f;                    // Culled / Last LOD
//     }
//
// }

