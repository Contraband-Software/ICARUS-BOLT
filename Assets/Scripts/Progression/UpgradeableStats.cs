using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Resources;
using Resources.System;
using Resources.Firmware;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProgressionV2
{
    /// <summary>
    /// Provides an interface to the Firmware side of your inventory
    /// </summary>
    public class UpgradeableStats : MonoBehaviour
    {
        private static readonly string assetNotFound =
            "That firmware type has not been built into the asset bundle";
        
        // Public config
        [SerializeField] private Stats baseStats;
       
        #region PUBLIC_API
        
        /// <summary>
        /// Returns all the info about a stat, including base value, icon, name, description, etc.
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        public StatValue GetBaseStatInfo(Stat stat)
        {
            var s = baseStats.GetStat(stat);
#if UNITY_EDITOR
            if (s is null)
                throw new InvalidOperationException("This stat has no base value recorded in the stats asset!");
#endif
            return s;
        }
        
        /// <summary>
        /// Returns a stat value, with the additive upgrades applied first, then the multiplicative ones.
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        public float GetStat(Stat stat)
        {
            var baseStat = baseStats.GetStat(stat).value;
            List<FirmwareData> activeFirmware = InventoryData.Inventory.GetActiveFirmwares();
            List<ModuleData> activeModules = InventoryData.Inventory.GetActiveModules();

            // Additive upgrades
            activeFirmware.ForEach(f =>
            {
                var asset = ItemStore.GetFirmwareAsset(f.firmwareId);
                if (asset != null)
                {
                    baseStat += PollChange(asset, stat, f.tier);
                }
            });
            activeModules.ForEach(m =>
            {
                var asset = ItemStore.GetModuleAsset(m.moduleId);
                if (asset != null)
                {
                    baseStat += PollChange(asset, stat, 0);
                }
            });


            // multiplicative upgrades
            activeFirmware.ForEach(f =>
            {
                var asset = ItemStore.GetFirmwareAsset(f.firmwareId);
                if (asset != null)
                {
                    baseStat *= PollMultiplier(asset, stat, f.tier);
                }
            });
            activeModules.ForEach(m =>
            {
                var asset = ItemStore.GetModuleAsset(m.moduleId);
                if (asset != null)
                {
                    baseStat *= PollMultiplier(asset, stat, 0);
                }
            });

            return baseStat;
        }
        #endregion

        #region GET_UPGRADED_VALUE
        private static float PollChange(IStatChange statChange, Stat stat, int onTier)
        {
            try
            {
                float query = statChange.GetStatChange(stat, onTier);
                if (float.IsNaN(query)) return 0;
                return query;
            }
            catch (NoMoreStatsException)
            {
                return 0;
            }
        }

        private static float PollMultiplier(IStatChange statChange, Stat stat, int onTier)
        {
            try
            {
                float query = statChange.GetStatMultiplier(stat, onTier);
                if (float.IsNaN(query)) return 1;
                return query;
            }
            catch (NoMoreStatsException)
            {
                return 1;
            }
        }
        #endregion
    }
}