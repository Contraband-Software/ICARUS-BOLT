//#define GAME_CONTENT_EDITOR_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Resources.System;
using UnityEngine;

using ModuleAssetLoader = Resources.Modules.AssetLoader;
using FirmwareAssetLoader = Resources.Firmware.AssetLoader;

namespace Progression
{
#if UNITY_EDITOR && GAME_CONTENT_EDITOR_ENABLED
    public class TestItemInitializer : MonoBehaviour
    {
        [Serializable]
        public struct ModuleAssignment
        {
            public ModuleItem module;
            public string type;
        }
        [SerializeField] private List<ModuleAssignment> modules;
        
        [Serializable]
        public struct FirmwareAssignment
        {
            public FirmwareItem firmware;
            public string type;
        }
        [SerializeField] private List<FirmwareAssignment> firmwares;
        
        private void Start()
        {
            {
                var moduleAssets = ModuleAssetLoader.GetAll();
                var firmwareAssets = FirmwareAssetLoader.GetAll();
                
                modules.ForEach(m => m.module.ModuleType = moduleAssets
                    .First(a => a.FriendlyName == m.type).GetType());
                
                firmwares.ForEach(m => m.firmware.FirmwareType = firmwareAssets
                    .First(a => a.FriendlyName == m.type).GetType());
            }
        }
    }
#endif
}