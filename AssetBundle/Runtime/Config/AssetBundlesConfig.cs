/****************************************************
文件：AssetBundlesConfig
作者：haitao.li
日期：2023/02/02 17:39:34
功能：配置
*****************************************************/

using UnityEngine;

namespace ResourceTools
{
    public class AssetBundlesConfig
    {
        public const string DefaultGroup = "Base";
        
        public const string ManifestFileName = "AssetManifest.json";

        public static readonly string CacheFileName = "CacheBundles.json";

        
        public static readonly string AssetBundlesFolderName = "AssetBundles";
        
        public static readonly string VersionFileName = "Version.txt";


        public static readonly string BundleExtString = ".bundle";

        public static readonly string Splicing = "_";
        


    }
}