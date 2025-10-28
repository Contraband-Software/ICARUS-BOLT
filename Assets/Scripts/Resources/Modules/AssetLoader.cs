using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Resources.Modules
{
    public static class AssetLoader
    {
        private static List<ModuleUpgradeAsset> cache;
        
        public static List<ModuleUpgradeAsset> GetAll()
        {
            if (cache is not null)
                return cache;
            
            var myLoadedAssetBundle = AssetBundle.LoadFromFile(
                Path.Combine(Application.streamingAssetsPath, "AssetBundles", "modules"));
            if (myLoadedAssetBundle == null)
                throw new InvalidOperationException("Module asset bundle has not been initialized");
            
            string [] x = myLoadedAssetBundle.GetAllAssetNames();
            
            cache = x
                .Select(a => myLoadedAssetBundle.LoadAsset<ModuleUpgradeAsset>(a)).ToList();
            return cache;
        }
    }
}