using System.IO;
using UnityEngine;

namespace Editor
{
    using UnityEditor;

    public class CreateAssetBundles
    {
        [MenuItem ("Assets/Build AssetBundles")]
        static void BuildAllAssetBundles ()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "AssetBundles");
            
            BuildPipeline.BuildAssetBundles (filePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            
            //Refresh the Project folder
            AssetDatabase.Refresh();
        }
    }
}