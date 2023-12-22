using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ResourceTools.Editor
{
    /// <summary>
    /// 打包配置
    /// </summary>
    public class PackageConfig : ScriptableObject
    {
        // /// <summary>
        // /// 资源清单版本号
        // /// </summary>
        // public int ManifestVersion;

        public PkgUtil.CustomPlatforms TargetPlatform;


        /// <summary>
        /// 保存Bundle 地址，使用Dir数据回丢失
        /// </summary>
        public List<string> ServerUrl;


        /// <summary>
        /// 打包设置
        /// </summary>
        public BuildAssetBundleOptions Options;

        /// <summary>
        /// 打包输出目录
        /// </summary>
        public string OutputPath;
        
        
        /// <summary>
        /// 是否进行冗余分析
        /// </summary>
        public bool IsAnalyzeRedundancy;

        /// <summary>
        /// 打包后是否将资源复制到StreamingAssets目录下
        /// </summary>
        public bool IsCopyToStreamingAssets;

        /// <summary>
        /// 要复制到StreamingAssets目录下的资源组，以分号分隔，默认只有一个Base组
        /// </summary>
        public string CopyGroup = AssetBundlesConfig.DefaultGroup;

        /// <summary>
        /// 如果版本号发生改变，就清空StreamingAssets目录重新生成
        /// </summary>
        public SerializableDictionary<BuildTarget, string> Versions = new SerializableDictionary<BuildTarget, string>();

        /// <summary>
        /// 不同平台的ManifestVersion
        /// </summary>
        public SerializableDictionary<BuildTarget, int> ManifestVersions = new SerializableDictionary<BuildTarget, int>();

        /// <summary>
        /// 导出项目名，StreamingAssets中放不同平台的文件夹，导出项目时根据这个目录结构删除指定的文件夹
        /// </summary>
        public SerializableDictionary<BuildTarget, string> ExportProjectName =
            new SerializableDictionary<BuildTarget, string>();

        /// <summary>
        /// 所有的Bundle数据信息
        /// </summary>
        public SerializableDictionary<BuildTarget, SerializableDictionary<string, BundleItem>> BundleDatas = new SerializableDictionary<BuildTarget, SerializableDictionary<string, BundleItem>>() ;
        
        [MenuItem("ResourceTools/创建打包配置文件")]
        private static void CreateConfig()
        {
            PackageConfig cfg = PkgUtil.CreateConfigAsset<PackageConfig>();

            if (cfg != null)
            {
                // cfg.TargetPlatforms = new List<BuildTarget>();
                cfg.TargetPlatform = PkgUtil.CustomPlatforms.Android;

                cfg.ServerUrl = new List<string>();

                cfg.IsAnalyzeRedundancy = true;

                cfg.Options = BuildAssetBundleOptions.ChunkBasedCompression
                    | BuildAssetBundleOptions.DisableLoadAssetByFileName
                    | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

                cfg.OutputPath = "";
                
                cfg.ManifestVersions = new SerializableDictionary<BuildTarget, int>();

                cfg.IsCopyToStreamingAssets = true;

                cfg.CopyGroup = AssetBundlesConfig.DefaultGroup;

                cfg.BundleDatas = new SerializableDictionary<BuildTarget, SerializableDictionary<string, BundleItem>>();

                cfg.Versions = new SerializableDictionary<BuildTarget, string>();

                EditorUtility.SetDirty(cfg);
            }
        }
        
        public int ManifestVersion(BuildTarget target)
        {
            if (ManifestVersions.TryGetValue(target, out int manifestVersion))
            {
                return manifestVersion;
            }
            else
            {
                ManifestVersions.Add(target, 1);
                return 1;
            }
        }
        
        public string ExportProjectLabelName(BuildTarget target)
        {
            if (ExportProjectName.TryGetValue(target, out string manifestVersion))
            {
                return manifestVersion;
            }
            else
            {
                ExportProjectName.Add(target, String.Empty);
                return "";
            }
        }
        
        
    }
}

