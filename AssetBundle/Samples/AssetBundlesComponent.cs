/****************************************************
文件：AssetBundlesComponent.cs
作者：haitao.li
日期：2023/03/31 09:15:51
功能：此文件为实例模板，可以copy文件到项目中定制
*****************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BFNetwork;
using BFNetwork.Configuration;
using CustomJson;
using UnityEngine;
using UnityEngine.Networking;

namespace ResourceTools
{
    /// <summary>
    /// ResourceTools资源组件
    /// </summary>
    public class AssetBundlesComponent : MonoBehaviour
    {
         [HideInInspector]
        public RunMode RunMode = RunMode.Updatable;
        public int MaxTaskExcuteCount = 10;
        public float UnloadDelayTime = 5;
        public bool IsEditorMode = true;
        public float EditorModeMaxDelay = 1;
    
        private LoadState _state = LoadState.None;
        
        private readonly string TAG = "========WoodAssetBundleComponent ";
    
    
        private void Awake()
        {
            AssetBundlesManager.RunMode = RunMode;
          
            AssetBundlesManager.MaxTaskExcuteCount = MaxTaskExcuteCount;
            AssetBundlesManager.UnloadDelayTime = UnloadDelayTime;
            // 定制事件上报方法
            // AssetBundlesManager.SendEventAction = CustomFunction
            
#if UNITY_ANDROID
            AssetBundlesManager.RemoteUrl = "Android Bundle  URL";
#elif UNITY_IOS
            AssetBundlesManager.RemoteUrl = "IOS Bundle  URL";
#endif
    
    #if UNITY_EDITOR
            AssetBundlesManager.IsEditorMode = IsEditorMode;
            AssetBundlesManager.EditorModeMaxDelay = EditorModeMaxDelay;
    #endif
            
            DontDestroyOnLoad(this);
        }
    
        private void Update()
        {
            AssetBundlesManager.Update();
    
            checkLoadState();
        }
    
        private void Start()
        {
            _state = LoadState.CreateEssentialFile;
        }
    
    
        private void checkLoadState()
        {
            /// 更改完状态后，进入对应的状态，将_state = None 防止异步方法重复多次执行
            switch (_state)
            {
                case LoadState.CreateEssentialFile:
                    StartCoroutine(createEssentialFile());
                    break;
                case LoadState.LoadVersionFile:
                    checkVersionFile();
                    break;
                case LoadState.DownloadManifest:
                    downloadManifest();
                    break;
                case LoadState.CheckoutBundles:
                    _state = LoadState.None;
                    AssetBundlesManager.SendDebugEvent("CheckoutBundles");
                    AssetBundlesManager.CheckoutBundles(checkoutBundles);
                    break;
                case LoadState.LoadBundleData:
                    loadBundles();
                    break;
            }
        }
    
        IEnumerator createEssentialFile()
        {
            _state = LoadState.None;
            AssetBundlesManager.SendDebugEvent("Check_Essential_File");
            try
            {
                if (!Directory.Exists(Util.GetPersistentDataPath()))
                {
                    AssetBundlesManager.SendDebugEvent("Create_Persistent_Path");
                    Directory.CreateDirectory(Util.GetPersistentDataPath());
                }
            }
            catch (Exception e)
            {
                string ErrInfo = e.Message;
                _state = LoadState.LoadBundleData;
                AssetBundlesManager.SendDebugEvent("Create_Persistent_Path_Error");
                yield break;
            }
    
            if (!File.Exists(Util.GetReadWritePath(AssetBundlesConfig.VersionFileName)))
            {
                UnityWebRequest request = UnityWebRequest.Get(Util.GetReadOnlyPathRequest(AssetBundlesConfig.VersionFileName));
                yield return request.SendWebRequest();
                if (request.isHttpError || request.isNetworkError || String.IsNullOrEmpty( request.downloadHandler.text))
                {
                    _state = LoadState.LoadBundleData;
                    AssetBundlesManager.SendDebugEvent("CopyVersionFile_Error : " + request.error);
                    yield break;
                }
                
                FileUtil.CreateFile(Util.GetReadWritePath(AssetBundlesConfig.VersionFileName), request.downloadHandler.text);
                
                AssetBundlesManager.SendDebugEvent("Copy_Version_File");
                
            }
            
            
            try
            {
                AssetBundlesManager.SendDebugEvent("Check_Cache_File");
    
                string cacheFilePath = Util.GetReadWritePath(AssetBundlesConfig.CacheFileName);
                if (!File.Exists(cacheFilePath))
                {
                    AssetBundlesManager.SendDebugEvent("Create_CacheBundle_File");
                    FileUtil.CreateFile(cacheFilePath, String.Empty);
                    
                }
                
                
            }
            catch (Exception e)
            {
                _state = LoadState.LoadBundleData;
                
                AssetBundlesManager.SendDebugEvent("Create_CacheBundle_File_Error");
    
                yield break;
            }
    
            _state = LoadState.LoadVersionFile;
    
        }
        
    
        async Task checkVersionFile()
        {
            _state = LoadState.None;
            AssetBundlesManager.SendDebugEvent("Check_Persistent_Version_File");
    
    
            string persistentVersionPath = Util.GetReadWritePath(AssetBundlesConfig.VersionFileName); 
            if (!File.Exists(persistentVersionPath))
            {
                
    
                AssetBundlesManager.SendDebugEvent("Persistent_Version_File_Not_Fount");
                _state = LoadState.LoadBundleData;
            }
    
            string varesionData = string.Empty;
            using (StreamReader sr = new StreamReader(persistentVersionPath))
            {
                 varesionData = await sr.ReadToEndAsync();
            }
            
            if (string.IsNullOrEmpty(varesionData))
            {
                File.Delete(persistentVersionPath);
                /// manifest是对比Version 更新的，删除Version文件的时候同时删除下Manifest文件
                string manifestPath = Util.GetReadWritePath(AssetBundlesConfig.ManifestFileName);
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }
                AssetBundlesManager.SendDebugEvent("Persistent_Version_Is_Null");
                _state = LoadState.CreateEssentialFile;
                return;
            }
            
    
            try
            {
                AssetBundlesVersion localVersionData = JsonParser.ParseJson<AssetBundlesVersion>(varesionData);
                
    
                AssetBundlesManager.SendDebugEvent("Request_Remote_Version_File");
    
                ///TODO  服务器没有文件就没有回调
                var respone = BFHttpManager.GetInstance().SendMessageAsync(BFNetworkEvent.RequestType.REQUEST_TEXT,
                    Path.Combine(Util.GetRemoteUrl(), AssetBundlesConfig.VersionFileName), String.Empty, String.Empty);
                await respone;
                if (!respone.Result.IsSuccess)
                {
                    AssetBundlesManager.SendDebugEvent("RequestRemoteVersionFile Fail");
    
                    _state = LoadState.LoadBundleData;
                    return;
                }
                AssetBundlesManager.SendDebugEvent("RequestRemoteVersionFile Success");
    
                AssetBundlesVersion remoteVersionData = CustomJson.JsonParser.ParseJson<AssetBundlesVersion>((string)respone.Result.Data);
                AssetBundlesManager.UpdateUriPrefix = remoteVersionData.GetRemoteBundleUrl();
            
                if (!localVersionData.Equals(remoteVersionData))
                {
                    try
                    {
                        FileUtil.CreateFile(persistentVersionPath, remoteVersionData);
                        _state = LoadState.DownloadManifest;
    
                    }
                    catch (Exception e)
                    {
                        _state = LoadState.LoadBundleData;
                        return;
                    }
                
                }
                else if (!File.Exists(Util.GetReadWritePath(AssetBundlesConfig.ManifestFileName)))
                {
                    _state = LoadState.DownloadManifest;
                }
                else
                {
                    _state = LoadState.CheckoutBundles;
                }
            }
            catch (Exception e)
            {
                Debug.Log(TAG + " Exception = " +  e.Message);
            }
    
           
            
        }
    
        
        async Task downloadManifest()
        {
            _state = LoadState.None;
    
    
            AssetBundlesManager.SendDebugEvent("DownloadManifestFile");
            string manifestUrl = Path.Combine(AssetBundlesManager.UpdateUriPrefix, AssetBundlesConfig.ManifestFileName);
    
            var  respone = BFHttpManager.GetInstance().SendMessageAsync(BFNetworkEvent.RequestType.REQUEST_FILE, manifestUrl,
                Path.Combine(Util.GetPersistentDataPath(), AssetBundlesConfig.ManifestFileName), String.Empty);
            await respone;
            if (!respone.Result.IsSuccess)
            {
    
                AssetBundlesManager.SendDebugEvent("DownloadManifestFile Fail");
                _state = LoadState.LoadBundleData;
                return;
            }
    
            AssetBundlesManager.SendDebugEvent("DownloadManifestFile Success");
    
            _state = LoadState.CheckoutBundles;
    
        }
        
        /// <summary>
       /// 
       /// </summary>
       /// <param name="count">需要更新的总资源数</param>
       /// <param name="length">总大小</param>
        private void checkoutBundles(int count, long length)
        {
            
            if (count == 0)
            {
    
                AssetBundlesManager.SendDebugEvent("UpdateBundleCunt 0");
                _state = LoadState.LoadBundleData;
                return;
            }
    
            AssetBundlesManager.SendDebugEvent("UpdateBundleCunt " + count);
    
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("总的需要更新资源数：" + count);
            sb.AppendLine("总大小：" + length);
            
            List<Updater> updaters =  AssetBundlesManager.GetAllUpdater();
            foreach (Updater updater in updaters)
            {
                sb.AppendLine("需要更新的资源组：" + updater.UpdateGroup);
                sb.AppendLine("更新文件数：" + updater.TotalCount);
                sb.AppendLine("更新大小：" + updater.TotalLength);
                Debug.Log(sb.ToString());
                sb.Clear();
            }
           
            foreach (Updater updater in updaters)
            {
                AssetBundlesManager.UpdateAssets(OnFileDownloaded, updater.UpdateGroup);
            }
    
            _state = LoadState.LoadBundleData;
        }
    
       private void OnFileDownloaded(bool success, int updatedCount, long updatedLength, int totalCount, long totalLength, string fileName, string group)
       {
           if (!success)
           {
               return;
           }
    
           StringBuilder sb = new StringBuilder();
           sb.AppendLine("已更新数量：" + updatedCount);
           sb.AppendLine("已更新大小：" + updatedLength);
           sb.AppendLine("总数量：" + totalCount);
           sb.AppendLine("总大小：" + totalLength);
           sb.AppendLine("资源名：" + fileName);
           sb.AppendLine("资源组：" + group);
    
           Debug.Log(sb.ToString());
           sb.Clear();
           
           AssetBundlesManager.SendDebugEvent(String.Format("Downloaded_Bundle_Name={0}_Group={1}", fileName, group));
    
    
           if (updatedCount >= totalCount)
           {
               Debug.Log(group + "组的所有资源下载完毕");
    
               Debug.Log($"请打开 {Application.persistentDataPath}  查看");
               
               AssetBundlesManager.SendDebugEvent("Downloaded_Bundle_Finish");
    
           }
            
            
       }
    
       private void loadBundles()
       {
           _state = LoadState.None;
           
           AssetBundlesManager.SendDebugEvent("LoadBundles");
    
           AssetBundlesManager.CheckPackageManifest((success) =>
           {
    
               // /// load Bundle信息文件结束
               // EventCenter.Broadcast(EventId.LoadManifestEnd);
               
               if (!success)
               {
                   Debug.Log("检查资源失败");
                   return;
               }
               
           });
    
       }
                
        
    }
    
}

