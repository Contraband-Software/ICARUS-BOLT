using Resources.Firmware;
using Resources.Modules;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace ProgressionV2
{
    public class ItemStore
    {
        public static bool initialized = false;

        public Dictionary<int, ItemData> Items { get; private set; } = new();

        // FIRMWARE
        public static List<FirmwareUpgradeAsset> AllFirmwareAssets { get; private set; } = new(); // All firmware in game
        private static Dictionary<int, FirmwareUpgradeAsset> FirmwareLookup = new();

        // MODULES
        public static List<ModuleUpgradeAsset> AllModuleAssets { get; private set; } = new(); // All modules in game
        private static Dictionary<int, ModuleUpgradeAsset> ModuleLookup = new();

        public event Action<ModuleUpgradeAsset> OnModuleActivated;
        public event Action<ModuleUpgradeAsset> OnModuleDeactivated;

        public void ClearItems()
        {
            Items.Clear();
        }

        #region STATIC_ACTIONS
        public static void Initialize()
        {
            if(initialized) return;

            AllFirmwareAssets.Clear();
            FirmwareLookup.Clear();
            AllModuleAssets.Clear();
            ModuleLookup.Clear();

            PopulateFirmwarePool();
            PopulateModulePool();

            initialized = true;
        }

        private static void PopulateFirmwarePool()
        {
            AllFirmwareAssets = Resources.Firmware.AssetLoader.GetAll();
            FirmwareLookup.Clear();
            foreach (FirmwareUpgradeAsset fAsset in AllFirmwareAssets)
            {
                FirmwareLookup[fAsset.Id] = fAsset;
            }
        }

        private static void PopulateModulePool()
        {
            AllModuleAssets = Resources.Modules.AssetLoader.GetAll();
            ModuleLookup.Clear();
            foreach (ModuleUpgradeAsset mAsset in AllModuleAssets)
            {
                ModuleLookup[mAsset.Id] = mAsset;
            }
        }
        #endregion

        #region QUERIES
        public ItemData GetItemById(int itemId)
        {
            if (Items.TryGetValue(itemId, out var item))
                return item;

            return null; // not found
        }
        /// <summary>
        /// Gets all firmware which are in the store and active
        /// </summary>
        /// <returns></returns>
        public List<FirmwareData> GetActiveFirmwares()
        {
            return Items.Values.OfType<FirmwareData>()
                .Where(f => f.isActive)
                .ToList();
        }

        /// <summary>
        /// Gets all modules that are in the store and active
        /// </summary>
        /// <returns></returns>
        public List<ModuleData> GetActiveModules()
        {
            return Items.Values.OfType<ModuleData>()
                .Where(f => f.isActive)
                .ToList();
        }

        /// <summary>
        /// Check if firmware of given type is present in the store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasFirmware<T>() where T : FirmwareUpgradeAsset
        {
            return Items.Values.OfType<FirmwareData>().Any(f =>
            {
                var asset = GetFirmwareAsset(f.firmwareId);
                return asset is T;
            });
        }
        public FirmwareUpgradeAsset GetFirmwareAssetByItemId(int itemId)
        {
            FirmwareData firmwareData = GetFirmwareDataByItemId(itemId);
            if (firmwareData == null) return null;
            return GetFirmwareAsset(firmwareData.firmwareId);
        }
        public FirmwareData GetFirmwareDataByItemId(int itemId)
        {
            ItemData item = GetItemById(itemId);
            if (item == null) return null;
            if (item.GetType() != typeof(FirmwareData)) return null;
            return (FirmwareData)item;
        }

        /// <summary>
        /// Check if firmware of given type is present in the store and is active
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasActiveFirmware<T>(int excludeItemId = -1) where T : FirmwareUpgradeAsset
        {
            return Items.Values.OfType<FirmwareData>().Any(f =>
            {
                if (!f.isActive) return false;
                if (f.itemId == excludeItemId) return false; // ignore this one
                var asset = GetFirmwareAsset(f.firmwareId);
                return asset != null && asset.GetType() == typeof(T); // strict match only
            });
        }
        public bool HasActiveFirmware(Type firmwareType, int excludeItemId = -1)
        {
            if (firmwareType == null || !typeof(FirmwareUpgradeAsset).IsAssignableFrom(firmwareType))
            {
                Debug.LogWarning($"HasActiveFirmware: Invalid type {firmwareType}");
                return false;
            }

            return Items.Values.OfType<FirmwareData>().Any(f =>
            {
                if (!f.isActive) return false;
                if (f.itemId == excludeItemId) return false; // ignore this one
                var asset = GetFirmwareAsset(f.firmwareId);
                return asset != null && asset.GetType() == firmwareType; // strict match only
            });
        }

        /// <summary>
        /// Check if module of given type is present in the store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasModule<T>() where T : ModuleUpgradeAsset
        {
            return Items.Values.OfType<ModuleData>().Any(m =>
            {
                var asset = GetModuleAsset(m.moduleId);
                return asset is T;
            });
        }

        /// <summary>
        /// Check if module of given type is present in the store and is active
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasActiveModule<T>(int excludeItemId = -1) where T : ModuleUpgradeAsset
        {
            return Items.Values.OfType<ModuleData>().Any(m =>
            {
                if (!m.isActive) return false;
                if (m.itemId == excludeItemId) return false;
                var asset = GetModuleAsset(m.moduleId);
                return asset != null && asset.GetType() == typeof(T); // strict match only
            });
        }
        public bool HasActiveModule(Type moduleType, int excludeItemId = -1)
        {
            if (moduleType == null || !typeof(ModuleUpgradeAsset).IsAssignableFrom(moduleType))
            {
                Debug.LogWarning($"HasActiveModule: Invalid type {moduleType}");
                return false;
            }

            return Items.Values.OfType<ModuleData>().Any(m =>
            {
                if (!m.isActive) return false;
                if (m.itemId == excludeItemId) return false;
                var asset = GetModuleAsset(m.moduleId);
                return asset != null && asset.GetType() == moduleType; // strict match only
            });
        }

        private AbstractUpgradeAsset GetAssetByItemId(int itemId)
        {
            ItemData itemData = GetItemById(itemId);
            if (itemData == null) return null;

            switch (itemData)
            {
                case FirmwareData firmwareData:
                    return GetFirmwareAsset(firmwareData.firmwareId);
                case ModuleData moduleData:
                    return GetModuleAsset(moduleData.moduleId);
                default: return null;
            }
        }

        public Sprite GetItemIcon(int itemId)
        {
            AbstractUpgradeAsset asset = GetAssetByItemId(itemId);
            if (asset == null) return null;
            return asset.Icon;
        }

        public ItemData GetItemFromSlot(int slotId)
        {
            return Items.Values.FirstOrDefault(item => item.slotId == slotId);
        }

        public int GetFirmwareItemTier(int itemId)
        {
            FirmwareData firmwareData = GetFirmwareDataByItemId(itemId);
            if(firmwareData == null) return -1;
            return firmwareData.tier;
        }

        public bool CanUpgadeItem(int itemId)
        {
            ItemData item = GetItemById(itemId);
            if (item == null) return false;

            if (item.GetType() != typeof(FirmwareData)) return false;
            FirmwareData firmwareData = (FirmwareData)item;

            FirmwareUpgradeAsset firmwareAsset = GetFirmwareAsset(firmwareData.firmwareId);
            if (firmwareAsset == null) return false;

            if (firmwareData.tier >= firmwareAsset.MaxTier) return false;

            Debug.Log("Can Upgrade Item");

            return true;
        }

        /// <summary>
        /// Checks if an incoming item is a valid candidate for upgrading an item
        /// </summary>
        /// <returns></returns>
        public bool ValidUpgradeCandidate(int itemId, ItemData candidate_item)
        {
            // cant be the same item
            if (candidate_item.itemId == itemId) return false;

            FirmwareData firmwardData = GetFirmwareDataByItemId(itemId);
            if (firmwardData == null) return false;

            if(candidate_item.GetType() != typeof(FirmwareData)) return false;
            FirmwareData candidateFirmwareData = (FirmwareData)candidate_item;
            if (candidateFirmwareData == null) return false;

            if (!CanUpgadeItem(itemId)) return false;

            // both items must be same firmware type
            if (firmwardData.firmwareId != candidateFirmwareData.firmwareId) return false;

            // both items must be same tier
            if (firmwardData.tier != candidateFirmwareData.tier) return false;

            Debug.Log("Upgrade Candidate Valid");

            return true;
        }

        #endregion


        #region STATIC_QUERIES
        /// <summary>
        /// Get the scriptableObject firmware asset referred to by a firmwareId
        /// </summary>
        /// <param name="firmwareId"></param>
        /// <returns></returns>
        public static FirmwareUpgradeAsset GetFirmwareAsset(int firmwareId)
        {
            if (FirmwareLookup.TryGetValue(firmwareId, out var asset))
            {
                return asset;
            }

            Debug.LogWarning($"Firmware with ID {firmwareId} not found in lookup!");
            return null;
        }

        /// <summary>
        /// Get the scriptableObject module asset referred to by a moduleId
        /// </summary>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static ModuleUpgradeAsset GetModuleAsset(int moduleId)
        {
            if (ModuleLookup.TryGetValue(moduleId, out var asset))
            {
                return asset;
            }
            Debug.LogWarning($"Module with ID {moduleId} not found in lookup!");
            return null;
        }
        public static bool IsValidFirmware(int firmwareId)
        {
            return FirmwareLookup.TryGetValue(firmwareId, out FirmwareUpgradeAsset _);
        }
        public static bool IsValidModule(int moduleId)
        {
            return ModuleLookup.TryGetValue(moduleId, out ModuleUpgradeAsset _);
        }

        public static AbstractUpgradeAsset GetAsset(ItemData item)
        {
            if(item.GetType() == typeof(FirmwareData))
            {
                return GetFirmwareAsset(((FirmwareData)item).firmwareId);
            }
            else if(item.GetType() == typeof(ModuleData))
            {
                return GetModuleAsset(((ModuleData)item).moduleId);
            }
            return null;
        }
        #endregion

        #region MUTATIONS
        public bool AddItem(ItemData item)
        {
            if (Items.ContainsKey(item.itemId))
            {
                Debug.LogError("item of id: " + item.itemId + " already exists in store");
                return false;
            }
            else
            {
                Items[item.itemId] = item;
                return true;
            }
        }

        /// <summary>
        /// Call when you intend to remove the item from the store and 
        /// remove it entirely from existence
        /// </summary>
        /// <param name="itemId"></param>
        public void DestroyItem(int itemId)
        {
            SetModuleActive(itemId, false);
            RemoveItem(itemId);
        }

        /// <summary>
        /// Remove an item by itemId from this store
        /// </summary>
        /// <param name="itemId"></param>
        public void RemoveItem(int itemId)
        {
            if (Items.Remove(itemId))
                Debug.Log($"Deleted item with ID {itemId}");
            else
                Debug.LogWarning($"No item found with ID {itemId}");
        }

        public void RegisterItemToSlot(int itemId, int slotId)
        {

            Debug.Log("Attempting to register item of id: " + itemId + " to slot: " + slotId);
            ItemData item = GetItemById(itemId);
            if (item == null) return;
            item.slotId = slotId;

            Debug.Log("item of id: " + itemId + " set to slotId: " + slotId);
        }

        public void SetFirmwareActive(int itemId, bool active)
        {
            ItemData item = GetItemById(itemId);
            if (item == null) return;
            if (item.GetType() == typeof(FirmwareData))
            {
                FirmwareData firmwareData = (FirmwareData)item;
                firmwareData.isActive = active;
            }
        }

        public void SetModuleActive(int itemId, bool active)
        {
            ItemData item = GetItemById(itemId);
            if (item == null) return;
            if (item.GetType() == typeof(ModuleData))
            {

                ModuleData moduleData = (ModuleData)item;
                ModuleUpgradeAsset moduleAsset = GetModuleAsset(moduleData.moduleId);
                if (moduleAsset == null) return;
                moduleData.isActive = active;

                if (active)
                {
                    Debug.Log("Module Activated");
                    OnModuleActivated?.Invoke(moduleAsset);
                }
                else
                {
                    Debug.Log("Module Deactivated");
                    OnModuleDeactivated?.Invoke(moduleAsset);
                }
            }
        }

        public void UpgradeFirmware(int itemId, int increaseBy)
        {
            ItemData item = GetItemById(itemId);
            if (item == null) return;
            if (item.GetType() != typeof(FirmwareData)) return;

            FirmwareData firmwareItem = (FirmwareData)item;
            firmwareItem.tier += increaseBy;
        }
        #endregion
    }
}
