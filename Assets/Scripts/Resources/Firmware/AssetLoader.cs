using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Resources.Firmware
{
    public static class AssetLoader
    {
        private static List<FirmwareUpgradeAsset> cache;
        
        public static List<FirmwareUpgradeAsset> GetAll()
        {
            if (cache is not null)
                return cache;
            
            var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "AssetBundles", "firmware"));
            if (myLoadedAssetBundle == null)
                throw new InvalidOperationException("Firmware asset bundle has not been initialized");
            
            string [] x = myLoadedAssetBundle.GetAllAssetNames();
            
            cache = x
                .Select(a => myLoadedAssetBundle.LoadAsset<FirmwareUpgradeAsset>(a)).ToList();
            
            return cache;
        }
    }
}