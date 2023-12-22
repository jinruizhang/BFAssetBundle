using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomJson;
using System.IO;
using System;

namespace ResourceTools
{
    /// <summary>
    /// ResourceTools资源更新器
    /// </summary>
    public static class ResourceToolsUpdater
    {
        
        /// <summary>
        /// 读写区资源信息，用于生成读写区资源清单
        /// </summary>
        internal static Dictionary<string, BundleManifestInfo> readWriteManifestInfoDict = new Dictionary<string, BundleManifestInfo>();

        
        /// <summary>
        /// 资源更新Uri前缀，下载资源文件时会以 UpdateUriPrefix/AssetBundleName 为下载地址
        /// </summary>
        internal static string UpdateUriPrefix;

        /// <summary>
        /// 资源更新器字典 key为资源组，下载结束后会删除对应的更新器
        /// </summary>
        internal static Dictionary<string, Updater> groupUpdaterDict = new Dictionary<string, Updater>();

        /// <summary>
        /// 生成读写区资源清单
        /// </summary>
        internal static void GenerateReadWriteManifest()
        {
            ResourceToolsManifest manifest = new ResourceToolsManifest();
            manifest.GameVersion = AssetBundlesManager.CurrentVersion;
            manifest.ManifestVersion = AssetBundlesManager.ManifestVersion;
            
            BundleManifestInfo[] bundleInfos = new BundleManifestInfo[readWriteManifestInfoDict.Count];
            int index = 0;
            foreach (KeyValuePair<string, BundleManifestInfo> item in readWriteManifestInfoDict)
            {
                bundleInfos[index] = item.Value;
                index++;
            }
            

            List<BundleManifestInfo> allBundleInfos = new List<BundleManifestInfo>();
            Dictionary<string, BundleManifestInfo> localreadWriteManifestInfoDict = new Dictionary<string, BundleManifestInfo>();

            if (File.Exists((Util.GetReadWritePath(AssetBundlesConfig.CacheFileName))))
            {
                string str = File.ReadAllText(Util.GetReadWritePath(AssetBundlesConfig.CacheFileName));
                if (!string.IsNullOrEmpty(str))
                {
                    ResourceToolsManifest localManifest = JsonParser.ParseJson<ResourceToolsManifest>(str);
                    foreach (var item in localManifest.Bundles)
                    {
                    
                        if (!localreadWriteManifestInfoDict.ContainsKey(item.BundleName))
                        {
                            localreadWriteManifestInfoDict.Add(item.BundleName, item);
                        }
                    }
                }
               
            }
            
            /// 远端下载的新数据替换本地的可读可写中的老的数据
            foreach (KeyValuePair<string, BundleManifestInfo> item in localreadWriteManifestInfoDict)
            {
                if (readWriteManifestInfoDict.ContainsKey(item.Key))
                {
                    continue;
                }
                
                allBundleInfos.Add(item.Value);
            }  
            
            /// 将新配置添加到老配置数据最后
            allBundleInfos.AddRange(bundleInfos);
            
            /// 将全部数据写入配置数据
            manifest.Bundles = allBundleInfos.ToArray();

            FileUtil.CreateFile(Util.GetReadWritePath(AssetBundlesConfig.CacheFileName), manifest);
        }


        /// <summary>
        /// 检查资源版本
        /// </summary>
        internal static void CheckoutBundles(Action<int, long> onVersionChecked)
        {
            Checker checker = new Checker();
            checker.CheckVersion(onVersionChecked);
        }

        /// <summary>
        /// 获取指定资源组的更新器，若不存在则创建
        /// </summary>
        internal static Updater GetOrCreateGroupUpdater(string group)
        {
            if (!groupUpdaterDict.TryGetValue(group,out Updater updater))
            {
                updater = new Updater();
                updater.UpdateGroup = group;
                groupUpdaterDict.Add(group, updater);
            }
            return updater;
        }

        /// <summary>
        /// 更新资源
        /// </summary>
        internal static void UpdateAssets(Action<bool,int, long, int, long, string, string> onUpdated,string updateGroup)
        {
            if (!groupUpdaterDict.TryGetValue(updateGroup,out Updater groupUpdater))
            {
                Debug.LogError("更新失败，没有找到该资源组的资源更新器：" + updateGroup);
                return;
            }

            //更新指定资源组
            if (groupUpdater.state != UpdaterStatus.Free)
            {
                Debug.LogError("此资源组已在更新中：" + updateGroup);
            }
            else
            {
                groupUpdater.UpdateAssets(onUpdated);
            }
        }

        /// <summary>
        /// 暂停资源更新器
        /// </summary>
        internal static void PauseUpdater(bool isPause ,string group)
        {
            if (!groupUpdaterDict.TryGetValue(group, out Updater groupUpdater))
            {
                Debug.LogError("暂停失败，没有找到该资源组的资源更新器：" + group);
                return;
            }

            //暂停指定资源组的更新器
            if (isPause)
            {
                groupUpdater.state = UpdaterStatus.Paused;
            }
            else
            {
                groupUpdater.state = UpdaterStatus.Runing;
            }

          
        }

    }
}

