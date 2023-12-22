using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using BFNetwork;
using BFNetwork.Internal;
using UnityEngine.Networking;
namespace ResourceTools
{
    /// <summary>
    /// 下载文件任务
    /// </summary>
    public class DownloadFileTask : BaseTask
    {
        // private UnityWebRequestAsyncOperation op;

        /// <summary>
        /// AssetBundle清单信息
        /// </summary>
        private BundleManifestInfo bundleInfo;

        /// <summary>
        /// 发起此下载任务的更新器
        /// </summary>
        private Updater updater;

        /// <summary>
        /// 下载地址
        /// </summary>
        private string downloadUri;

        /// <summary>
        /// 本地文件路径
        /// </summary>
        private string localFilePath;
        

        private Action<bool,BundleManifestInfo, BFHttpResponse> onFinished;

        internal override Delegate FinishedCallback
        {
            get
            {
                return onFinished;
            }

            set
            {
                onFinished = (Action<bool, BundleManifestInfo, BFHttpResponse>)value;
            }
        }

        /// <summary>
        /// downloaded 已下载
        /// downloadLength 全部大小
        /// </summary>
        public long downloaded, downloadLength;

        public override float Progress
        {
            get
            {
                return (downloaded * 1.0f / downloadLength);
            }
        }
        
        public DownloadFileTask(TaskExcutor owner, string name,BundleManifestInfo bundleInfo,Updater updater, string localFilePath, string downloadUri, Action<bool,BundleManifestInfo, BFHttpResponse> onFinished) : base(owner, name)
        {
            this.bundleInfo = bundleInfo;
            this.updater = updater;
            this.localFilePath = localFilePath;
            this.downloadUri = downloadUri;
            this.onFinished = onFinished;
        }

        public override async void Execute()
        {
            TaskState = TaskStatus.Free;
            if (updater.state == UpdaterStatus.Paused)
            {
                //处理下载暂停 暂停只对还未开始执行的下载任务有效
                return;
            }
            TaskState = TaskStatus.Executing;
            var temp = BFHttpManager.GetInstance().SendMessageAsync(BFNetworkEvent.RequestType.REQUEST_FILE, downloadUri, localFilePath, bundleInfo.BundleName, null, OnDownloadProgress);

            await temp;

            OnResponseFinish(temp.Result);
        }
        
        private void  OnResponseFinish(BFHttpResponse response)
        {

            TaskState = TaskStatus.Finished;

            if (response.Code != 200)
            {
                //下载失败
                Debug.LogError($"下载失败：{Name},错误信息：{response.Message}");
                onFinished?.Invoke(false , bundleInfo, response);
                return;
            }
            onFinished?.Invoke(true,bundleInfo,response);
        }

        public void OnDownloadProgress(long downloaded, long downloadLength)
        {
            this.downloaded = downloaded;
            this.downloadLength = downloadLength;

        }

        public override void Update()
        {
        }
    }

}
