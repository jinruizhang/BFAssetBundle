using CustomJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BFNetwork;
using UnityEngine;
using UnityEngine.Networking;

namespace ResourceTools
{
    /// <summary>
    /// 资源检查器
    /// </summary>
    public class Checker
    {
        /// <summary>
        /// 资源检查信息的字典
        /// </summary>
        private Dictionary<string, CheckInfo> checkInfoDict = new Dictionary<string, CheckInfo>();

        /// <summary>
        /// cache资源检查信息的字典
        /// </summary>
        private ResourceToolsManifest cacheManifest = null;
        List<BundleManifestInfo> cacheBundleManifestInfos = new List<BundleManifestInfo>();


        /// <summary>
        /// 版本信息检查完毕回调
        /// </summary>
        private Action<int, long> onVersionChecked;
        


        //三方资源清单的检查完毕标记
        private bool readOnlyChecked;
        private bool readWriteCheked;
        private bool cacheBundleChecked;

        /// <summary>
        /// 检查资源版本
        /// </summary>
        public void CheckVersion(Action<int, long> onVersionChecked)
        {

            this.onVersionChecked = onVersionChecked;
            
            //进行只读区 读写区 远端三方的资源清单检查 
            string readOnlyManifestPath = Util.GetReadOnlyPathRequest(AssetBundlesConfig.ManifestFileName);
            
            string cacheBundlesPath = Util.GetReadWritePathRequest(AssetBundlesConfig.CacheFileName); 
            
            string remoteManifestUri = Util.GetReadWritePathRequest(AssetBundlesConfig.ManifestFileName);


            WebRequestTask readOnlyTask = new WebRequestTask(AssetBundlesManager.taskExcutor, readOnlyManifestPath,
                readOnlyManifestPath, CheckReadOnlyManifest);

            WebRequestTask cacheFileTask = new WebRequestTask(AssetBundlesManager.taskExcutor, cacheBundlesPath,
                cacheBundlesPath, CheckCacheManifest);

            WebRequestTask downloaderManifestTask = new WebRequestTask(AssetBundlesManager.taskExcutor,
                remoteManifestUri, remoteManifestUri, CheckReadWriteManifest);
            
            
            AssetBundlesManager.taskExcutor.AddTask(readOnlyTask);
            AssetBundlesManager.taskExcutor.AddTask(cacheFileTask);
            AssetBundlesManager.taskExcutor.AddTask(downloaderManifestTask);

        }
        
        
        
        

        /// <summary>
        /// 检查只读区资源清单
        /// </summary>
        private void CheckReadOnlyManifest(bool success, string error, UnityWebRequest data)
        {

            if (!success)
            {
                Debug.LogError("Check ReadOnlyManifest Error! 路径不对 检测是否需要加file://");
                AssetBundlesManager.SendDebugEvent("Check ReadOnlyManifest Fail");
                readOnlyChecked = true;
                onVersionChecked?.Invoke(0, 0);
                return;
            }

            AssetBundlesManager.SendDebugEvent("Check_ReadOnlyManifest_Success");

            ResourceToolsManifest manifest = JsonParser.ParseJson<ResourceToolsManifest>(data.downloadHandler.text);
            foreach (BundleManifestInfo item in manifest.Bundles)
            {
                CheckInfo checkInfo = GetCheckInfo(item.BundleName);
                checkInfo.ReadOnlyInfo = item;
            }
            
            readOnlyChecked = true;
            refreshCheckInfos();
            // loadReadWriteManifest();
        }


        /// <summary>
        /// 检查下载的远端资源清单
        /// </summary>
        private void CheckReadWriteManifest(bool success, string error, UnityWebRequest data)
        {
            if (!success)
            {
                AssetBundlesManager.SendDebugEvent("Check_ReadWriteManifest_Fail");
                Debug.LogError("Check ReadWriteManifest Error! 路径不对 检测是否需要加file://");
                onVersionChecked?.Invoke(0, 0);
                return;
            }

            AssetBundlesManager.SendDebugEvent("Check_ReadWriteManifest_Success");

            
            ResourceToolsManifest manifest = JsonParser.ParseJson<ResourceToolsManifest>(data.downloadHandler.text);
            
            AssetBundlesManager.CurrentVersion = manifest.GameVersion;
            AssetBundlesManager.ManifestVersion = manifest.ManifestVersion;
            
            foreach (BundleManifestInfo item in manifest.Bundles)
            {
                CheckInfo checkInfo = GetCheckInfo(item.BundleName);
                checkInfo.RemoteInfo = item;
            }
            
            
            readWriteCheked = true;
            refreshCheckInfos();
        }
        
        /// <summary>
        /// 检查远端资源清单
        /// </summary>
        private void CheckCacheManifest(bool success, string error, UnityWebRequest data)
        {
            if (!success)
            {
                AssetBundlesManager.SendDebugEvent("Check_CacheManifest_Fail");
                Debug.LogError("CheckReadWriteManifest Error! 路径不对 检测是否需要加file://");
                onVersionChecked?.Invoke(0, 0);
                return;
            }

            AssetBundlesManager.SendDebugEvent("Check_CacheManifest_Success");

            string cacheData = data.downloadHandler.text;
            if (!String.IsNullOrEmpty(cacheData))
            {
                cacheManifest = JsonParser.ParseJson<ResourceToolsManifest>(cacheData);
                cacheBundleManifestInfos = cacheManifest.Bundles.ToList();

                foreach (BundleManifestInfo item in cacheManifest.Bundles)
                {
                    CheckInfo checkInfo = GetCheckInfo(item.BundleName);
                    checkInfo.ReadWriteInfo = item;
                    ResourceToolsUpdater.readWriteManifestInfoDict[item.BundleName] = item;
                }
            }
           

            cacheBundleChecked = true;
            refreshCheckInfos();
            // loadReadWriteManifest();
        }


        // /// <summary>
        // /// 检查读写区资源清单
        // /// </summary>
        // private void CheckCacheInfos()
        // {
        //     if (!readWriteCheked || !readOnlyChecked)
        //     {
        //         return;
        //     }
        //
        //     string cachePaht = Util.GetReadWritePath(AssetBundlesConfig.CacheFileName);
        //     if (!File.Exists(cachePaht))
        //     {
        //         readWriteCheked = true;
        //         RefershCheckInfos();
        //         return;
        //     }
        //
        //
        //     var items = File.ReadAllText(cachePaht)
        //         .Split(AssetBundlesConfig.CacheLineBreak.ToCharArray());
        //
        //     foreach (var item in items)
        //     {
        //         if (string.IsNullOrEmpty(item))
        //         {
        //             continue;
        //         }
        //         var bundleData = item.Split(AssetBundlesConfig.CacheSplicing.ToCharArray());
        //         cacheBundles.Add(bundleData[0], bundleData[1]);
        //     }
        //     // File.Delete(Util.GetReadWritePath(AssetBundlesConfig.CacheFileName));
        //     
        //     
        //     foreach (var item in readOnlyManifest.Bundles)
        //     {
        //         if (!cacheBundles.ContainsKey(item.BundleName))
        //         {
        //             cacheBundles.Add(item.BundleName, item.Hash.ToString());
        //         }
        //         else
        //         {
        //             cacheBundles[item.BundleName] = item.Hash.ToString();
        //         }
        //         
        //     }
        //
        //     string cacheData = String.Empty;
        //     foreach (var key in cacheBundles.Keys)
        //     {
        //         if (!String.IsNullOrEmpty(cacheData))
        //         {
        //             cacheData += AssetBundlesConfig.CacheLineBreak;
        //         }
        //
        //         cacheData += key + AssetBundlesConfig.CacheSplicing + cacheBundles[key];
        //     }
        //     
        //     // using (StreamWriter sw = new StreamWriter(Util.GetReadWritePath(AssetBundlesConfig.CacheFileName)))
        //     // {
        //     //     sw.Write(cacheData);
        //     // }
        //     
        //     
        //
        //     // CacheBundlesInfo bundlesInfo = JsonParser.ParseJson<CacheBundlesInfo>(uwr.downloadHandler.text);
        //     //
        //     // foreach (CacheBundle item in bundlesInfo.BundlesInfo)
        //     // {
        //     //     CheckInfo checkInfo = GetCheckInfo(item.BundleName);
        //     //     checkInfo.ReadWriteInfo = item;
        //     //     ResourceToolsUpdater.readWriteManifestInfoDict[item.BundleName] = item;
        //     // }
        //
        //     // cacheBundleChecked = true;
        //     // RefershCheckInfos();
        // }
        
        
        /// <summary>
        /// 获取资源检查信息
        /// </summary>
        private CheckInfo GetCheckInfo(string name)
        {
            if (!checkInfoDict.TryGetValue(name, out CheckInfo checkInfo))
            {
                checkInfo = new CheckInfo(name);
                checkInfoDict.Add(name, checkInfo);
                return checkInfo;
            }

            return checkInfo;
        }



        /// <summary>
        /// 刷新资源检查信息
        /// </summary>
        private void refreshCheckInfos()
        {
            if (!readOnlyChecked || !readWriteCheked || !cacheBundleChecked)
            {
                return;
            }

            AssetBundlesManager.SendDebugEvent("RefreshCheckInfos");

            //清理旧的资源组与更新器信息
            AssetBundlesManager.groupInfoDict.Clear();
            ResourceToolsUpdater.groupUpdaterDict.Clear();

            //需要更新的所有资源的数量与长度
            int totalCount = 0;
            long totalLength = 0;
            

            //是否需要生成读写区资源清单
            bool needGenerateManifest = false;

            foreach (KeyValuePair<string, CheckInfo> item in checkInfoDict)
            {
                CheckInfo checkInfo = item.Value;
                checkInfo.RefreshState();

                switch (checkInfo.State)
                {
                    case CheckStatus.NeedUpdate:
                        //需要更新

                        Updater updater = ResourceToolsUpdater.GetOrCreateGroupUpdater(checkInfo.RemoteInfo.Group);
                        updater.UpdateBundles.Add(checkInfo.RemoteInfo);
                        updater.TotalCount++;
                        updater.TotalLength += checkInfo.RemoteInfo.Length;

                        totalCount++;
                        totalLength += checkInfo.RemoteInfo.Length;
                        break;

                    // /// 如果加载逻辑时先加载bundle在执行远端Bundle检测，下面的这两个步骤是可以省略？
                    // case CheckStatus.InReadWrite:
                    //     //最新版本已存放在读写区
                    //     AssetBundlesManager.InitRuntimeInfo(checkInfo.ReadWriteInfo, true);
                    //     break;
                    //
                    // case CheckStatus.InReadOnly:
                    //     //最新版本已存放在只读区
                    //     AssetBundlesManager.InitRuntimeInfo(checkInfo.ReadOnlyInfo, false);
                    //     break;
                }

                /// 2023.09.15 远端更新的资源，如果读写区存在，先不删除，等更新完成后再删除
                if (checkInfo.NeedRemove && checkInfo.State != CheckStatus.NeedUpdate)
                {
                    //需要删除
                    Debug.Log("删除读写区资源：" + checkInfo.Name);
                    string path = Util.GetReadWritePath(checkInfo.Name);
                    File.Delete(path);

                    //从读写区资源信息字典中删除
                    ResourceToolsUpdater.readWriteManifestInfoDict.Remove(checkInfo.Name);

                    // 从cache文件中删除
                    for (int i = cacheBundleManifestInfos.Count - 1; i >= 0; i--)
                    {
                        if (cacheBundleManifestInfos[i].BundleName.Equals(checkInfo.Name))
                        {
                            cacheBundleManifestInfos.RemoveAt(i);
                        }
                    }

                    needGenerateManifest = true;
                }
            }

            if (needGenerateManifest)
            {
                //删除过读写区资源 需要重新生成读写区资源清单
                ResourceToolsUpdater.GenerateReadWriteManifest();
                // 重新生成cacheFile
                cacheManifest.Bundles = cacheBundleManifestInfos.ToArray();
                FileUtil.CreateFile(Util.GetReadWritePath(AssetBundlesConfig.CacheFileName), cacheManifest);

            }

            //调用版本信息检查完毕回调
            onVersionChecked?.Invoke(totalCount, totalLength);

        }

    }
}

