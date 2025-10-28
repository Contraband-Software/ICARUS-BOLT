using Resources.Firmware;
using Resources.Modules;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Resources;
using Resources.System;
using System;

namespace ProgressionV2
{
    public static class InventoryData
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "inventory.json");

        public static ItemStore Inventory { get; private set; } = new();

        public static void Initialize()
        {
            ItemStore.Initialize();
            Inventory.ClearItems();
        }

        #region SERIALIZATION
        public static void Save()
        {
            if (Inventory == null || Inventory.Items == null) return;
            ItemStoreSaveData saveData = new ItemStoreSaveData()
            {
                firmwares = Inventory.Items.Values.OfType<FirmwareData>().ToList(),
                modules = Inventory.Items.Values.OfType<ModuleData>().ToList(),
            };
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(SavePath, json);
            Debug.Log("Inventory Saved to " + SavePath);
        }

        public static void Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("No Inventory file found, starting fresh");
                Inventory.ClearItems();
                return;
            }

            string json = File.ReadAllText(SavePath);
            var loaded = JsonConvert.DeserializeObject<ItemStoreSaveData>(json);

            Inventory.ClearItems();

            // Combine both firmware and module lists into one sequence of ItemData
            IEnumerable<ItemData> allItems =
                (loaded.firmwares ?? Enumerable.Empty<FirmwareData>())
                .Cast<ItemData>()
                .Concat((loaded.modules ?? Enumerable.Empty<ModuleData>())
                .Cast<ItemData>());

            // assign all items into dictionary
            foreach (var item in allItems)
            {
                Inventory.AddItem(item);
            }

            // Collect any items that dont have a valid firmware/module id
            var invalidIds = new List<int>();

            foreach (ItemData item in Inventory.Items.Values)
            {
                if (item is FirmwareData firmwareData && !ItemStore.IsValidFirmware(firmwareData.firmwareId))
                {
                    invalidIds.Add(item.itemId);
                }
                else if (item is ModuleData moduleData && !ItemStore.IsValidModule(moduleData.moduleId))
                {
                    invalidIds.Add(item.itemId);
                }
            }
            // remove invalid firmware/module id items
            foreach (int id in invalidIds)
            {
                Debug.Log($"Item of ID: " + id + " had invalid firmware/module id. removing.");
                Inventory.DestroyItem(id);
            }

            Debug.Log("Inventory loaded from " + SavePath);
        }
        #endregion

        #region MUTATION
        #endregion
    }
}
