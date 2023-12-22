using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BestHTTP.Timings;
using BFNetwork.Internal;
using UnityEngine;

namespace ResourceTools
{
    /// <summary>
    /// 资源更新器
    /// </summary>
    public class Updater
    {
        /// <summary>
        /// 重新生成一次读写区资源清单所需的下载字节数
        /// </summary>
        private static long generateManifestLength = 1024 * 1024 * 10; //10M

        /// <summary>
        /// 从上一次重新生成读写区资源清单到现在下载的字节数
        /// </summary>
        private long deltaUpatedLength;

        /// <summary>
        /// Bundle更新回调
        /// </summary>
        private Action<bool, int, long, int, long, string, string> onUpdated;

        /// <summary>
        /// 文件下载回调
        /// </summary>
        private Action<bool, BundleManifestInfo, BFHttpResponse> onDownloadFinished;


        /// <summary>
        /// 更新器状态
        /// </summary>
        public UpdaterStatus state;

        /// <summary>
        /// 需要更新的资源
        /// </summary>
        public List<BundleManifestInfo> UpdateBundles = new List<BundleManifestInfo>();

        /// <summary>
        /// 已经下载的Bundle
        /// </summary>
        private List<BundleManifestInfo> downloadBundles = new List<BundleManifestInfo>();

        /// <summary>
        /// 需要更新的资源组
        /// </summary>
        public string UpdateGroup;

        /// <summary>
        /// 需要更新的资源总数
        /// </summary>
        public int TotalCount;

        /// <summary>
        /// 需要更新的资源长度
        /// </summary>
        public long TotalLength;

        /// <summary>
        /// 已更新资源文件数量
        /// </summary>
        public int UpdatedCount;

        /// <summary>
        /// 已更新资源文件长度
        /// </summary>
        public long UpdatedLength;
        


        public Updater()
        {
            onDownloadFinished = OnDownloadFinished;
        }

        /// <summary>
        /// 移除更新完毕回调，主要给边玩边下模式调用
        /// </summary>
        // internal void RemoveBundleUpdatedCallback(Action<bool, int, long, int, long, string, string> onUpdated)
        // {
        //     this.onUpdated -= onUpdated;
        // }


        private List<DownloadFileTask> _tasks = new List<DownloadFileTask>();
        

        /// <summary>
        /// 更新所有需要更新的资源文件
        /// </summary>
        internal void UpdateAssets(Action<bool, int, long, int, long, string, string> onUpdated)
        {
            state = UpdaterStatus.Runing;

            foreach (BundleManifestInfo updateBundleInfo in UpdateBundles)
            {
                if (downloadBundles.Contains(updateBundleInfo) || string.IsNullOrEmpty(updateBundleInfo.VersionName))
                {
                    continue;
                }
                
                //创建下载文件的任务
                string localFilePath = Util.GetReadWritePath(updateBundleInfo.BundleName);

                string downloadUri = Path.Combine(Util.GetDownloadUrl(), updateBundleInfo.VersionName, 
                    updateBundleInfo.VersionName + AssetBundlesConfig.Splicing + updateBundleInfo.VersionCode,
                    updateBundleInfo.BundleName);
                // string downloadUri = Path.Combine(ResourceToolsUpdater.UpdateUriPrefix, updateBundleInfo.BundleName);
                DownloadFileTask task = new DownloadFileTask(AssetBundlesManager.taskExcutor, downloadUri,
                    updateBundleInfo, this, localFilePath, downloadUri, onDownloadFinished);
                AssetBundlesManager.taskExcutor.AddTask(task);
                _tasks.Add(task);

                Hashtable eventData = new Hashtable()
                {
                    {"BundleGroup", UpdateGroup},
                    {"BundleName", updateBundleInfo.BundleName},
                    {"BundleSize", updateBundleInfo.Length}

                };

                AssetBundlesManager.SendEvent("Bundle_Download_Start", eventData);
                
            }

            this.onUpdated = onUpdated;
        }
        

        /// <summary>
        /// 资源文件下载完毕的回调
        /// </summary>
        private void OnDownloadFinished(bool success, BundleManifestInfo bundleInfo, BFHttpResponse response)
        {
            bool timeTag = false;
            string requestTime = string.Empty;
            if (response != null && response.Timing != null)
            {
                timeTag = response.Timing.TryGetValue(TimingEventNames.Finished, out requestTime);
                Debug.Log("======= OnDownloadFinished BundleName" + bundleInfo.BundleName + " Download Time = " + requestTime);
            }

           
            if (!success)
            {
                int failCount = PlayerPrefs.GetInt(bundleInfo.Hash.ToString(), 0);
                PlayerPrefs.SetInt(bundleInfo.Hash.ToString(), failCount + 1);
                if (timeTag)
                {
                    TimeSpan timeSpan = TimeSpan.Parse(requestTime);
                    Hashtable eventData = new Hashtable()
                    {
                        {"BundleGroup", UpdateGroup},
                        {"DownloadTime", (int)timeSpan.TotalSeconds},
                        {"BundleName", bundleInfo.BundleName},
                        {"BundleSize", bundleInfo.Length},
                        {"Reason", response.Message},
                        {"SumFailedTimes", failCount + 1}
                    };
                
                    AssetBundlesManager.SendEvent("Bundle_Download_Fail", eventData);
                }
               
                

                Debug.LogError($"更新{bundleInfo.BundleName}失败");
                state = UpdaterStatus.Free;
                onUpdated?.Invoke(false, UpdatedCount, UpdatedLength, TotalCount, TotalLength, bundleInfo.BundleName,
                    UpdateGroup);
                
                return;
            }

            if (timeTag)
            {
                int failCount = PlayerPrefs.GetInt(bundleInfo.Hash.ToString(), 0);

                TimeSpan timeSpan = TimeSpan.Parse(requestTime);
                Hashtable data = new Hashtable() 
                {
                    {"BundleGroup", UpdateGroup},
                    {"DownloadTime", (int)timeSpan.TotalSeconds},
                    {"BundleName", bundleInfo.BundleName},
                    {"BundleSize", bundleInfo.Length},
                    {"SumFailedTimes", failCount}
                };
                AssetBundlesManager.SendEvent("Bundle_Download_Success", data);
            }




            //刷新已下载资源信息
            UpdatedCount++;
            UpdatedLength += bundleInfo.Length;
            deltaUpatedLength += bundleInfo.Length;
            downloadBundles.Add(bundleInfo);


            //将下载好的bundle信息添加到RuntimeInfo中
            AssetBundlesManager.InitRuntimeInfo(bundleInfo, true);

            //刷新读写区资源信息列表
            ResourceToolsUpdater.readWriteManifestInfoDict[bundleInfo.BundleName] = bundleInfo;

            //刷新资源组本地资源信息
            GroupInfo groupInfo = AssetBundlesManager.GetOrCreateGroupInfo(bundleInfo.Group);
            groupInfo.localBundles.Add(bundleInfo.BundleName);
            groupInfo.localCount++;
            groupInfo.localLength += bundleInfo.Length;

            bool allDownloaded = UpdatedCount >= TotalCount;

            if (allDownloaded || deltaUpatedLength >= generateManifestLength)
            {
                //资源下载完毕 或者已下载字节数达到要求 就重新生成一次读写区资源清单
                deltaUpatedLength = 0;
                ResourceToolsUpdater.GenerateReadWriteManifest();
            }

            if (allDownloaded)
            {
                //该组资源都更新完毕，可以删掉updater了
                ResourceToolsUpdater.groupUpdaterDict.Remove(UpdateGroup);
            }

            PlayerPrefs.Save();

            onUpdated?.Invoke(true, UpdatedCount, UpdatedLength, TotalCount, TotalLength, bundleInfo.BundleName,
                UpdateGroup);
        }
        
        
        /// <summary>
        /// 一组资源的下载进度
        /// </summary>
        /// <returns></returns>
        public float Progress()
        {
            (long downloadedLength, long totalLength) downloadData = GetDownloadedAndTotalLength();
            if (downloadData.totalLength == 0)
            {
                return 0;
            }
            
            return (downloadData.downloadedLength * 1.0f / downloadData.totalLength);
        }

        /// <summary>
        /// 返回当前组资源下载的总长度和已下载长度
        /// </summary>
        /// <returns></returns>
        public (long downloadedLength, long totalLength) GetDownloadedAndTotalLength()
        {
            long totalLength = 0;
            long downloadedLength = 0;
            foreach (var item in _tasks)
            {
                downloadedLength += item.downloaded;
                totalLength += item.downloadLength;
            }

            return (downloadedLength, totalLength);
        }
        
    }
}