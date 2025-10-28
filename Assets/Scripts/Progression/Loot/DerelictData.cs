using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace ProgressionV2
{
    public static class DerelictData
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "derelicts.json");

        public static Dictionary<string, ItemStore> LootForDerelict = new();
        public static string activeDerelict;

        public static void Initialize()
        {
            ItemStore.Initialize();
            foreach(var itemStore in LootForDerelict.Values)
            {
                itemStore.ClearItems();
            }
            LootForDerelict.Clear();
        }

        #region SERIALIZATION
        public static void Save()
        {
            var saveData = new DerelictSaveData();

            if (LootForDerelict == null) return;
            foreach (var kvp in LootForDerelict)
            {
                var store = kvp.Value;

                saveData.derelicts[kvp.Key] = new ItemStoreSaveData
                {
                    firmwares = store.Items.Values.OfType<FirmwareData>().ToList(),
                    modules = store.Items.Values.OfType<ModuleData>().ToList()
                };
            }

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(SavePath, json);
            Debug.Log("Derelict loot saved to " + SavePath);
        }

        public static void Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("No Derelict loot file found, starting fresh");
                LootForDerelict.Clear();
                return;
            }

            string json = File.ReadAllText(SavePath);
            var loaded = JsonConvert.DeserializeObject<DerelictSaveData>(json);

            LootForDerelict.Clear();

            foreach (var kvp in loaded.derelicts)
            {
                var store = new ItemStore();

                IEnumerable<ItemData> allItems =
                    (kvp.Value.firmwares ?? Enumerable.Empty<FirmwareData>())
                    .Cast<ItemData>()
                    .Concat((kvp.Value.modules ?? Enumerable.Empty<ModuleData>())
                    .Cast<ItemData>());

                foreach (var item in allItems)
                    store.AddItem(item);

                LootForDerelict[kvp.Key] = store;
            }

            Debug.Log("Derelict loot loaded from " + SavePath);
        }
        #endregion

        public static void GenerateNewLoot(
            string derelictId,
            int maxLoot,
            int maxPointBudget,
            float biasMeanPoint = 1.0f,
            float biasSigma = 1.0f,
            float biasStrength = 1.0f
            )
        {
            List<ItemData> items = LootGen.GenerateLoot(
                maxLoot,
                maxPointBudget,
                biasMeanPoint,
                biasSigma,
                biasStrength);
            if (!LootForDerelict.ContainsKey(derelictId))
            {
                LootForDerelict[derelictId] = new ItemStore();
            }
            LootForDerelict[derelictId].ClearItems();
            foreach(ItemData item in items)
            {
                LootForDerelict[derelictId].AddItem(item);
            }
        }

        public static bool LootForDerelictGenerated(string derelictId)
        {
            return LootForDerelict.ContainsKey(derelictId);
        }

        public static void ResetLootSlots(string derelictId)
        {
            if (!LootForDerelictGenerated(derelictId)) return;
            foreach(ItemData item in LootForDerelict[derelictId].Items.Values)
            {
                LootForDerelict[derelictId].RegisterItemToSlot(item.itemId, -1);
            }
        }

        public static ItemStore GetDerelictLoot(string derelictId)
        {
            if (!LootForDerelictGenerated(derelictId)) return null;
            return LootForDerelict[derelictId];
        }
    }
}
