using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Resources.System;
using UnityEngine;

namespace Resources.Modules
{
    public class AttachedModules : MonoBehaviour
    {
        private static readonly string assetNotFound =
            "That module type has not been built into the asset bundle";
        
        // State
        public List<ModuleUpgradeAsset> ModuleUpgradePool { get; private set; } = new();
        public List<ModuleUpgradeAsset> ActiveModules { get; private set; } = new();

        #region UNITY
        private void Awake()
        {
            ModuleUpgradePool = AssetLoader.GetAll();
        }
        #endregion

        #region PUBLIC_API
        /// <summary>
        /// Overwrite the active firmwares.
        /// </summary>
        /// <param name="types"></param>
        public void SetModules(IEnumerable<Type> types)
        {
            ActiveModules.Clear();
            foreach (Type type in types)
                AddModuleUpgrade(type);
        }
        
        /// <summary>
        /// Checks if a Module upgrade is present.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool ModulePresent<T>() where T : ModuleUpgradeAsset
        {
            return ActiveModules.Any(f => f.GetType() == typeof(T));
        }
        public bool ModulePresent(Type t)
        {
            return ActiveModules.Any(f => f.GetType() == t);
        }
        
        /// <summary>
        /// Adds a Module upgrade, if the current Module is already added, it will do nothing and return false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool AddModuleUpgrade<T>() where T : ModuleUpgradeAsset
        {
            if (ModulePresent<T>())
                return false;
            
            ModuleUpgradeAsset module = GetModule<T>(ModuleUpgradePool);
            
            ActiveModules.Add(module);
            return true;
        }
        public bool AddModuleUpgrade(Type t)
        {
            if (ModulePresent(t))
                return false;
            
            ModuleUpgradeAsset module = GetModule(t, ModuleUpgradePool);
            
            ActiveModules.Add(module);
            return true;
        }

        /// <summary>
        /// Removes a Module upgrade, if it was never present, it will return false.
        /// If the Module had any tiers, they will be reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool RemoveModuleUpgrade<T>() where T : ModuleUpgradeAsset
        {
            var query = ActiveModules.Where(f => f.GetType() == typeof(T));

            List<ModuleUpgradeAsset> moduleUpgrades = query.ToList();
            if (!moduleUpgrades.Any())
                return false;
            
            ActiveModules = ActiveModules.Except(moduleUpgrades).ToList();
            return true;
        }
        public bool RemoveModuleUpgrade(Type t)
        {
            var query = ActiveModules.Where(f => f.GetType() == t);

            List<ModuleUpgradeAsset> moduleUpgrades = query.ToList();
            if (!moduleUpgrades.Any())
                return false;
            
            ActiveModules = ActiveModules.Except(moduleUpgrades).ToList();
            return true;
        }
        #endregion

        private ModuleUpgradeAsset GetModule<T>(List<ModuleUpgradeAsset> pool) where T : ModuleUpgradeAsset
        {
            ModuleUpgradeAsset module = pool.FirstOrDefault(f => f.GetType() == typeof(T));
#if UNITY_EDITOR
            if (module is null)
                throw new ArgumentException(assetNotFound);
#endif
            return module;
        }
        private ModuleUpgradeAsset GetModule(Type t, List<ModuleUpgradeAsset> pool)
        {
            ModuleUpgradeAsset firmware = pool.FirstOrDefault(f => f.GetType() == t);
#if UNITY_EDITOR
            if (firmware is null)
                throw new ArgumentException(assetNotFound);
#endif
            return firmware;
        }
    }
}