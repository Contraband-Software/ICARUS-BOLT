using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Resources.System;
using UnityEditor;


// namespace Resources.Editor
// {
//     [CustomEditor(typeof(FirmwareUpgradeController))]
//     public class FirmwareUpgradeControllerEditor : UnityEditor.Editor
//     {
//         string[] firmwareNames;
//         string[] firmwareAssemblyQualifiedNames;
//         
//         int _choiceIndex;
//         
//         FirmwareUpgradeController instance;
//     
//         private static AssetBundle firmwareAssets;
//         
//         private void OnEnable()
//         {
//             firmwareAssemblyQualifiedNames = new string[] { };
//             firmwareNames = new string[] { };
//             
//             instance = target as FirmwareUpgradeController;
//             
//             firmwareAssets ??= AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "AssetBundles", "firmware"));
//             
//             if (firmwareAssets is null)
//                 throw new InvalidOperationException("Firmware asset bundle has not been initialized");
//                     
//             string [] x = firmwareAssets.GetAllAssetNames();
//                     
//             List<Type> firmwareUpgradePoolAssets = x
//                 .Select(a => firmwareAssets.LoadAsset<FirmwareUpgradeAsset>(a).GetType()).ToList();
//             
//             firmwareNames = firmwareUpgradePoolAssets.Select(a => a.Name).ToArray();
//             firmwareAssemblyQualifiedNames = firmwareUpgradePoolAssets.Select(f => f.AssemblyQualifiedName).ToArray();
//     
//             var choice = firmwareAssemblyQualifiedNames.ToList().IndexOf(instance.AssemblyQualifiedFirmware);
//             
//             _choiceIndex = 
//                 instance.AssemblyQualifiedFirmware == null ? 0 :
//                 choice != -1 && choice < firmwareAssemblyQualifiedNames.Length ? choice : 0;
//             
//             Debug.Log(_choiceIndex);
//             Debug.Log(firmwareAssemblyQualifiedNames.Length);
//         }
//         
//         public override void OnInspectorGUI ()
//         {
//             
//             // Draw the default inspector
//             DrawDefaultInspector();
//             
//             EditorGUILayout.BeginHorizontal();
//             
//             EditorGUILayout.LabelField("Firmware Asset Type");
//             _choiceIndex = EditorGUILayout.Popup(_choiceIndex, firmwareNames);
//             instance.AssemblyQualifiedFirmware = firmwareAssemblyQualifiedNames[_choiceIndex];
//             
//             // Save the changes back to the object
//             EditorUtility.SetDirty(instance);
//             
//             EditorGUILayout.EndHorizontal();
//         }
//     }
// }