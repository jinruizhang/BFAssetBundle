using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

namespace ResourceTools.Editor
{
    /// <summary>
    /// 打包器
    /// </summary>
    public static class Packager
    {

        public static BuildTarget currentTargetPlatform = BuildTarget.Android;
        /// <summary>
        /// 执行打包管线
        /// </summary>
        public static void ExecutePackagePipeline(BuildAssetBundleOptions options, BuildTarget targetPlatform, bool isAnalyzeRedundancy,string copyGroup)
        {

            currentTargetPlatform = targetPlatform;
            
            /// 获取最终打包输出目录
            string finalOutputPath = GetFinalOutputPath(currentTargetPlatform);
            int manifestVersion = PkgUtil.PkgCfg.ManifestVersion(targetPlatform);


            /// 创建打包输出目录
            CreateOutputPath(finalOutputPath);

            /// 获取AssetBundleBuildList，然后打包AssetBundle
            List<AssetBundleBuild> abBuildList = PkgUtil.PkgRuleCfg.GetAssetBundleBuildList(isAnalyzeRedundancy);
            AssetBundleManifest unityManifest = PackageAssetBundles(finalOutputPath, abBuildList,options,targetPlatform);

            /// 生成资源清单文件
            ResourceToolsManifest manifest = GenerateManifestFile(finalOutputPath, abBuildList,unityManifest,manifestVersion);

            /// 生成StreamingAssets 下资源
            CreateStreamingAssets(targetPlatform,finalOutputPath, manifest);
            
            /// 记录没有变化的Bundle
            ChangeBundleVersion(manifest,targetPlatform);
            
            /// 生成Manifest文件
            FileUtil.CreateFile(Path.Combine(finalOutputPath,AssetBundlesConfig.ManifestFileName), manifest);
            
            /// 创建Version相关的版本
            CreateVersionFile(targetPlatform);
            
            /// 资源清单版本号自增
            ChangeManifestVersion();

        }
        
        public static void CreateStreamingAssets(BuildTarget targetPlatform, string outputPath,ResourceToolsManifest manifest)
        {
            bool copyTag = false;
            if (!PkgUtil.PkgCfg.Versions.TryGetValue(targetPlatform,out string cfgVersion))
            {
                PkgUtil.PkgCfg.Versions.Add(targetPlatform, Application.version);
                copyTag = true;
            }
            else if (!Application.version.Equals(cfgVersion))
            {
                copyTag = true;
            }
            else if (PkgUtil.PkgCfg.ManifestVersions.TryGetValue(targetPlatform, out int cfgManifestVersion))
            {
                if (cfgManifestVersion == 1)
                {
                    copyTag = true;
                }
            }

            /// 将特定的Bundle复制到StreamingAssets下
            if (copyTag)
            {
                CopyToStreamingAssets(outputPath,manifest);
                PkgUtil.PkgCfg.Versions[targetPlatform] = Application.version;
            }
        }

        public static void ChangeBundleVersion(ResourceToolsManifest manifest,BuildTarget targetPlatform)
        {
            string finalOutputPath = GetFinalOutputPath(targetPlatform);

            
            if (!PkgUtil.PkgCfg.BundleDatas.TryGetValue(targetPlatform, out SerializableDictionary<string, BundleItem> bundleDatas))
            {
                bundleDatas = new SerializableDictionary<string, BundleItem> ();
                PkgUtil.PkgCfg.BundleDatas.Add(targetPlatform, bundleDatas);
            }
            
            /// 移除已经删除的bundle
            List<string> keysToRemove = new List<string>();
            foreach (var key in bundleDatas.Keys)
            {
                if (!manifest.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            
            
            foreach (var key in keysToRemove)
            {
                bundleDatas.Remove(key);
            }
            
            foreach (BundleManifestInfo bundleManifestInfo in manifest.Bundles)
            {
                if (!bundleDatas.TryGetValue(bundleManifestInfo.BundleName, out BundleItem bundleItem))
                {
                    bundleItem = new BundleItem(bundleManifestInfo);
                    bundleDatas.Add(bundleManifestInfo.BundleName, bundleItem);
                }
                else if (bundleItem.Hash != bundleManifestInfo.Hash)
                {
                   bundleItem.Hash = bundleManifestInfo.Hash;
                   bundleItem.VersionName = bundleManifestInfo.VersionName;
                   bundleItem.VersionCode = bundleManifestInfo.VersionCode;
                }
                else if (bundleItem.VersionName != bundleManifestInfo.VersionName || bundleItem.VersionCode != bundleManifestInfo.VersionCode)
                {
                    
                    string bundlePath = Path.Combine(finalOutputPath, bundleManifestInfo.BundleName);

                    string recordBundlePath = Path.Combine(GetOutPutBundlePath(targetPlatform), bundleItem.VersionName,
                        bundleItem.VersionName + AssetBundlesConfig.Splicing + bundleItem.VersionCode,
                        bundleManifestInfo.BundleName);
                    if (File.Exists(bundlePath) &&  File.Exists(recordBundlePath))
                    {
                        bundleManifestInfo.VersionName = bundleItem.VersionName;
                        bundleManifestInfo.VersionCode = bundleItem.VersionCode;
                        File.Delete(bundlePath);
                    }
                    else
                    {
                        bundleItem.Hash = bundleManifestInfo.Hash;
                        bundleItem.VersionName = bundleManifestInfo.VersionName;
                        bundleItem.VersionCode = bundleManifestInfo.VersionCode;
                    }
                    
                }
            }

        }
       

        
        /// <summary>
        /// 创建打包输出目录
        /// </summary>
        private static void CreateOutputPath(string outputPath)
        {
            //打包目录已存在就清空
            DirectoryInfo dirInfo;
            if (Directory.Exists(outputPath))
            {

                dirInfo = new DirectoryInfo(outputPath);
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(outputPath);
            }
        }
    
        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        private static AssetBundleManifest PackageAssetBundles(string outputPath,List<AssetBundleBuild> abBuildList, BuildAssetBundleOptions options, BuildTarget targetPlatform)
        {
            AssetBundleManifest unityManifest =  BuildPipeline.BuildAssetBundles(outputPath, abBuildList.ToArray(), options, targetPlatform);

            DirectoryInfo dirInfo = new DirectoryInfo(outputPath);
            string directoryName = outputPath.Substring(outputPath.LastIndexOf("\\") + 1);
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                if (file.Name == directoryName || file.Extension == ".manifest")
                {
                    //删除manifest文件
                    file.Delete();
                }
            }

            return unityManifest;
        }
  
        /// <summary>
        /// 生成资源清单文件
        /// </summary>
        private static ResourceToolsManifest GenerateManifestFile(string outputPath, List<AssetBundleBuild> abBuildList, AssetBundleManifest unityManifest,int manifestVersion)
        {

            ResourceToolsManifest manifest = new ResourceToolsManifest();
            manifest.GameVersion = Application.version;
            manifest.ManifestVersion = manifestVersion;

            manifest.Bundles = new BundleManifestInfo[abBuildList.Count];
            for (int i = 0; i < abBuildList.Count; i++)
            {
                AssetBundleBuild abBulid = abBuildList[i];

                BundleManifestInfo abInfo = new BundleManifestInfo();
                manifest.Bundles[i] = abInfo;

                abInfo.BundleName = abBulid.assetBundleName;

                string fullPath = Path.Combine(outputPath, abInfo.BundleName);
                FileInfo fi = new FileInfo(fullPath);

                abInfo.Length = fi.Length;
                abInfo.Hash = unityManifest.GetAssetBundleHash(abInfo.BundleName);

                abInfo.IsScene = abBulid.assetNames[0].EndsWith(".unity");  //判断是否为场景ab

                abInfo.Group = AssetCollector.GetAssetBundleGroup(abInfo.BundleName);  //标记资源组

                abInfo.Assets = new AssetManifestInfo[abBulid.assetNames.Length];

                abInfo.VersionName = Application.version;
                
                abInfo.VersionCode =  PkgUtil.PkgCfg.ManifestVersion(currentTargetPlatform);
                
                for (int j = 0; j < abBulid.assetNames.Length; j++)
                {
                    AssetManifestInfo assetInfo = new AssetManifestInfo();
                    abInfo.Assets[j] = assetInfo;

                    assetInfo.AssetName = abBulid.assetNames[j];
                    assetInfo.Type =  AssetDatabase.GetMainAssetTypeAtPath(assetInfo.AssetName).Name;
                    assetInfo.Dependencies = PkgUtil.GetDependencies(assetInfo.AssetName,false);  //依赖列表不进行递归记录 因为加载的时候会对依赖进行递归加载
                }

               
            }
            return manifest;
            
        }
    
        /// <summary>
        /// 修改资源清单版本号
        /// </summary>
        private static void ChangeManifestVersion()
        {
            //自增
            PkgUtil.PkgCfg.ManifestVersions[currentTargetPlatform]++;
        }


        
        /// <summary>
        /// 返回Version 路径
        /// </summary>
        /// <param name="targetPlatform"></param>
        /// <returns></returns>
        private static void CreateVersionFile(BuildTarget targetPlatform)
        {
            string versionPaht = Path.Combine(GetFinalOutputPath(targetPlatform), AssetBundlesConfig.VersionFileName);
            if (File.Exists(versionPaht))
            {
                File.Delete(versionPaht);
            }
            
            AssetBundlesVersion bundlesVersion = new AssetBundlesVersion();
            bundlesVersion.ManifestVersion = PkgUtil.PkgCfg.ManifestVersion(targetPlatform);
            bundlesVersion.AppVersion = Application.version;
            // bundlesVersion.RemoteLoadPath = PkgUtil.PkgCfg.ServerUrl[(int)PkgUtil.PkgCfg.TargetPlatform];
            bundlesVersion.Platform = targetPlatform.ToString();
            
            FileUtil.CreateFile(versionPaht, bundlesVersion);
            FileUtil.CreateFile(Path.Combine(Util.GetStreamingAssetsPath(), AssetBundlesConfig.VersionFileName), bundlesVersion);
            
        }

        /// <summary>
        /// 创建资源更新Manifest
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="ignoreGopyGroup"></param>
        /// <param name="manifest"></param>
        private static void CreateUpdateManifest(string outputPath, string ignoreGopyGroup,ResourceToolsManifest manifest)
        {
            ResourceToolsManifest updateMainfest = new ResourceToolsManifest(manifest);
            
            HashSet<string> copyGroupSet = null;
            if (!string.IsNullOrEmpty(ignoreGopyGroup))
            {
                copyGroupSet = new HashSet<string>(ignoreGopyGroup.Split(';'));
            }

            string manifestFileName = AssetBundlesConfig.ManifestFileName;

            if (copyGroupSet != null)
            {
                //根据要复制的资源组修改资源清单
                List<BundleManifestInfo> abInfoList = new List<BundleManifestInfo>();
                foreach (BundleManifestInfo abInfo in updateMainfest.Bundles)
                {
                    if (!copyGroupSet.Contains(abInfo.Group))
                    {
                        abInfoList.Add(abInfo);
                    }
                }
                updateMainfest.Bundles = abInfoList.ToArray();
            }

            FileUtil.CreateFile(Path.Combine(outputPath,manifestFileName), updateMainfest);

            Debug.Log("写入远端更新manifest结束");


        }
        
        /// <summary>
        /// 将资源移动到StreamingAssets下
        /// </summary>
        private static void CopyToStreamingAssets(string outputPath,ResourceToolsManifest manifest)
        {
            ResourceToolsManifest localManifest = new ResourceToolsManifest(manifest);
            

            string readOnlyPath = Util.GetStreamingAssetsPath();
            //StreamingAssets目录已存在就清空
            if (Directory.Exists(readOnlyPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(readOnlyPath);
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(readOnlyPath);
            }

            
            DirectoryInfo outputDirInfo = new DirectoryInfo(outputPath);

            string manifestFileName = AssetBundlesConfig.ManifestFileName;

            foreach (FileInfo item in outputDirInfo.GetFiles())
            {
                string strExt = System.IO.Path.GetExtension(item.Name);
                if (!strExt.Equals(AssetBundlesConfig.BundleExtString))
                {
                    //跳过资源清单文件
                    continue;
                }

                // string groupName = AssetCollector.GetAssetBundleGroup(item.Name);
                
                if (!AssetCollector.IsLocalAssetBundle(item.Name))
                {
                    //跳过并非指定要复制的资源组的资源文件
                    continue;
                }
                

                item.CopyTo(Util.GetReadOnlyPath(item.Name));
            }

           

           
            //根据要复制的资源组修改资源清单
            List<BundleManifestInfo> abInfoList = new List<BundleManifestInfo>();
            foreach (BundleManifestInfo abInfo in localManifest.Bundles)
            {
                if (AssetCollector.IsLocalAssetBundle(abInfo.BundleName))
                {
                    abInfoList.Add(abInfo);
                }
            }
            localManifest.Bundles = abInfoList.ToArray();
            
            
            //生成仅包含被复制的资源组的资源清单文件到StreamingAssets下
            FileUtil.CreateFile(Util.GetReadOnlyPath(manifestFileName), localManifest);
            

            AssetDatabase.Refresh();
            Debug.Log("已将资源复制到StreamingAssets目录下");
        }
        
        
        /// <summary>
        /// Version 文件夹目录
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="targetPlatform"></param>
        /// <returns></returns>
        private static string GetVersionOutputPath(BuildTarget targetPlatform)
        {
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(),PkgUtil.PkgCfg.OutputPath);
            return Path.Combine(outputPath, targetPlatform.ToString(), Application.version);
        }
        
        /// <summary>
        /// 打包输出对应版本根目录
        /// </summary>
        /// <param name="targetPlatform"></param>
        /// <returns></returns>
        private static string GetOutPutBundlePath(BuildTarget targetPlatform)
        {
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(),PkgUtil.PkgCfg.OutputPath);
            return Path.Combine(outputPath, targetPlatform.ToString());
        }


        /// <summary>
        /// 获取最终打包输出目录
        /// </summary>
        private static string GetFinalOutputPath(BuildTarget targetPlatform)
        {
            int manifestVersion = PkgUtil.PkgCfg.ManifestVersion(targetPlatform);
            string dir =  Application.version + AssetBundlesConfig.Splicing + manifestVersion;
            string result = Path.Combine(GetVersionOutputPath(targetPlatform) ,dir);
            return result;
        }
        
    }
    

}
