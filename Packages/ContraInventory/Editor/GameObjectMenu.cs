using UnityEngine;
using UnityEditor;

namespace Software.Contraband.Inventory.Editor
{
    public class GameObjectMenu : UnityEditor.Editor
    {
        private static string GetPackagePath()
        {
            return "Packages/software.contraband.inventories";
        }

        private static void _SpawnPrefabInEditor(GameObject prefab, string name)
        {
            prefab.name = name;

            PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            if (Selection.activeTransform != null)
            {
                prefab.transform.SetParent(Selection.activeTransform, false);
            }

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localEulerAngles = Vector3.zero;
            prefab.transform.localScale = Vector3.one;
        }


        private static string ManagerEditorName = "InventoryManager";

        [MenuItem("GameObject/UI/ContraInventory/Manager/Empty Manager", false, 1)]
        private static void CreateNewEmptyManager()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventorySystemBase01.prefab"));
            _SpawnPrefabInEditor(prefab, ManagerEditorName);
        }

        [MenuItem("GameObject/UI/ContraInventory/Manager/Basic Manager", false, 2)]
        private static void CreateNewBasicManager()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventorySystemBase02.prefab"));
            _SpawnPrefabInEditor(prefab, ManagerEditorName);
        }


        private static string ContainerEditorName = "Container";

        [MenuItem("GameObject/UI/ContraInventory/Container", false, 3)]
        private static void CreateNewEmptyContainer()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventoryContainerBase01.prefab"));
            _SpawnPrefabInEditor(prefab, ContainerEditorName);
        }

        private static string SlotEditorName = "Slot";

        [MenuItem("GameObject/UI/ContraInventory/Slot/Empty Slot", false, 4)]
        private static void CreateNewEmptySlot()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventorySlotBase01.prefab"));
            _SpawnPrefabInEditor(prefab, SlotEditorName);
        }

        [MenuItem("GameObject/UI/ContraInventory/Slot/Basic Slot", false, 5)]
        private static void CreateNewBasicSlot()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventorySlotBase02.prefab"));
            _SpawnPrefabInEditor(prefab, SlotEditorName);
        }

        private static string ItemEditorName = "Item";

        [MenuItem("GameObject/UI/ContraInventory/Item/Empty Item", false, 6)]
        private static void CreateNewEmptyItemt()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventoryItemBase01.prefab"));
            _SpawnPrefabInEditor(prefab, ItemEditorName);
        }

        [MenuItem("GameObject/UI/ContraInventory/Item/Basic Item", false, 7)]
        private static void CreateNewBasicItem()
        {
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetPackagePath() +
                                                                  "\\Runtime\\Prefabs\\InventoryItemBase02.prefab"));
            _SpawnPrefabInEditor(prefab, ItemEditorName);
        }
    }
}