using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BFNetwork;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace ResourceTools
{
    /// <summary>
    /// ResourceTools资源管理器
    /// </summary>
    public class AssetBundlesManager
    {
        /// <summary>
        /// Bundle运行时信息字典（只有在这个字典里的才是在本地可加载的）
        /// </summary>
        internal static Dictionary<string, BundleRuntimeInfo> bundleInfoDict =
            new Dictionary<string, BundleRuntimeInfo>();

        public static Dictionary<string, BundleRuntimeInfo> BundleInfoDict
        {
            get
            {
                return bundleInfoDict;
            }
        }

        /// <summary>
        /// Asset运行时信息字典（只有在这个字典里的才是在本地可加载的）
        /// </summary>
        internal static Dictionary<string, AssetRuntimeInfo> assetInfoDict = new Dictionary<string, AssetRuntimeInfo>();

        /// <summary>
        /// Asset和Asset运行时信息的映射字典(不包括场景)
        /// </summary>
        internal static Dictionary<Object, AssetRuntimeInfo> assetToAssetInfoDict =
            new Dictionary<Object, AssetRuntimeInfo>();

        /// <summary>
        /// 资源组信息字典
        /// </summary>
        internal static Dictionary<string, GroupInfo> groupInfoDict = new Dictionary<string, GroupInfo>();

        /// <summary>
        /// 任务执行器
        /// </summary>
        internal static TaskExcutor taskExcutor = new TaskExcutor();

        /// <summary>
        /// 编辑器资源模式下的最大加载延时
        /// </summary>
        public static float EditorModeMaxDelay;

        /// <summary>
        /// 资源卸载延迟时间
        /// </summary>
        public static float UnloadDelayTime;

        /// <summary>
        /// 单帧最大任务执行数量
        /// </summary>
        public static int MaxTaskExcuteCount
        {
            set { taskExcutor.MaxExcuteCount = value; }
        }

        public static readonly string ESTag = "AssetFramework_Event";

        /// <summary>
        /// 运行模式
        /// </summary>
        public static RunMode RunMode { get;  set; }

        /// <summary>
        /// 是否开启编辑器资源模式
        /// </summary>
        public static bool IsEditorMode { get;  set; }

        /// <summary>
        /// 自定义事件上报系统
        /// </summary>
        public static Action<string, Hashtable> SendEventAction;

        public static Action<string, Hashtable> DebugEventAction;
        
        /// <summary>
        /// 项目的远端URL
        /// </summary>
        public static string RemoteUrl = string.Empty;
        
        
        /// <summary>
        /// 资源更新Uri前缀，下载资源文件时会以 UpdateUriPrefix/BundleName 为下载地址
        /// </summary>
        public static string UpdateUriPrefix
        {
            get { return ResourceToolsUpdater.UpdateUriPrefix; }

            set { ResourceToolsUpdater.UpdateUriPrefix = value; }
        }

        
        
        /// <summary>
        /// bundle 是否下载完成且加载到内存中
        /// </summary>
        /// <param name="bundleGroupName"></param>
        /// <returns></returns>
        public static bool IsBundleNeedUpdate(string bundleGroupName)
        {
            List<Updater> updaters = GetAllUpdater();
            Updater curUpdater = null;
            foreach (var item in updaters)
            {
                if (bundleGroupName == item.UpdateGroup)
                {
                    curUpdater = item;
                    break;
                }
            }

            return curUpdater != null;
        }

        /// <summary>
        /// 是否在本地
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public static bool IsBundleInLocality(string bundleName)
        {
            if (!bundleInfoDict.TryGetValue(bundleName, out BundleRuntimeInfo info))
            {
                return false;
            }
            
            // StreamingAssets 中的文件直接返回true,Android 平台下 StreamingAssets 中的文件不能通过File来获取
            if (!info.InReadWrite)
            {
                return true;
            }

            return File.Exists(info.LoadPath);
        }

        /// <summary>
        /// 远端下载的资源版本
        /// </summary>
        public static string CurrentVersion = "1.0.0";

        public static int ManifestVersion = 1;

        /// <summary>
        /// 轮询ResourceTools管理器
        /// </summary>
        public static void Update()
        {
            taskExcutor.Update();
        }

        #region 资源清单检查

        /// <summary>
        /// 检查安装包内资源清单，将资源保存为一个dir，加载前检测这个dir中是否有该资源
        /// </summary>
        public static void CheckPackageManifest(Action<bool> callback)
        {
            bool tag = true;
            string path = Util.GetReadWritePath(AssetBundlesConfig.ManifestFileName);
            Debug.Log("ManifestFile path = " + path);

            SendDebugEvent("Load Bundles CheckManifest");


            /// 先去检索可读可写路径下资源是否存在，如果可读可写路径下文件存在，先读取可读可写路径下资源，然后在读取只读路径下资源
            if (!File.Exists(path))
            {
                Debug.Log("可读可写 ManifestFile 文件不存在，获取只读路径下文件");
                checkStreamingAssetsManifest(callback);
            }
            else
            {
                checkReadWriteManifest(path, callback);
            }
        }


        /// <summary>
        /// 检测可读可写路径下资源
        /// TODO 逻辑整合，每次都先加载Streaming下的文件
        /// </summary>
        private static async Task checkReadWriteManifest(string path, Action<bool> callback)
        {
            Debug.Log("ManifestFile，获取可读可写路径下文件");

            if (!File.Exists(path))
            {
                SendDebugEvent("Load Bundles ReadWriteManifest Fail");
                Debug.LogError("资源清单检查失败");
                callback?.Invoke(false);
                return;
            }

            SendDebugEvent("Load Bundles ReadWriteManifest Success");

            string readOnlyPath = Util.GetReadOnlyPathRequest(AssetBundlesConfig.ManifestFileName);

            WebRequestTask task = new WebRequestTask(taskExcutor, readOnlyPath, readOnlyPath, (success, erro, uwr) =>
            {
                if (success)
                {
                    SendDebugEvent("Load Bundles StreamingManifest Success");

                    ResourceToolsManifest localManifest =
                        CustomJson.JsonParser.ParseJson<ResourceToolsManifest>(uwr.downloadHandler.text);

                    string allText = File.ReadAllText(path);

                    /// 判断资源在是否包含在Cache 文件中
                    ResourceToolsManifest manifest = CustomJson.JsonParser.ParseJson<ResourceToolsManifest>(allText);

                    string cachePath = Util.GetReadWritePath(AssetBundlesConfig.CacheFileName);
                    ResourceToolsManifest cache = null;
                    if (File.Exists(cachePath))
                    {
                        string cacheAllText = File.ReadAllText(cachePath);
                        if (!String.IsNullOrEmpty(cacheAllText))
                        {
                            cache = CustomJson.JsonParser.ParseJson<ResourceToolsManifest>(cacheAllText);
                        }
                    }

                    /// 加载所有的资源
                    foreach (BundleManifestInfo abInfo in manifest.Bundles)
                    {
                        bool isReadWrite = cache == null ? false : cache.Contains(abInfo);

                        if (isReadWrite)
                        {
                            InitRuntimeInfo(abInfo, true);
                        }
                        else if (localManifest.Contains(abInfo))
                        {
                            InitRuntimeInfo(abInfo, false);
                        }
                    }

                    /// 如果本地的版本号和沙盒中版本号不一致，将StreamingAssets中的Bundle资源替换沙盒中的
                    /// 比如更新了APP 远端Manifest 资源还没加载到
                    if (!localManifest.GameVersion.Equals(manifest.GameVersion))
                    {
                        foreach (BundleManifestInfo abInfo in localManifest.Bundles)
                        {
                            InitRuntimeInfo(abInfo, false);
                        }
                    }

                    

                    callback?.Invoke(true);
                    Debug.Log("ManifestFile，读取可读可写路径下信息结束");
                }
                else
                {
                    SendDebugEvent("Load Bundles StreamingManifest Fail");
                    Debug.LogError("资源清单检查失败");
                    callback?.Invoke(false);
                }
            });

            taskExcutor.AddTask(task);
        }

        /// <summary>
        /// 本地的StreamingAssets 资源
        /// </summary>
        public static void checkStreamingAssetsManifest(Action<bool> callback)
        {
            Debug.Log("ManifestFile，获取只读路径下文件");
            string path = Util.GetReadOnlyPathRequest(AssetBundlesConfig.ManifestFileName);
            WebRequestTask task = new WebRequestTask(taskExcutor, path, path, (success, erro, uwr) =>
            {
                if (success)
                {
                    SendDebugEvent("Load Bundles StreamingManifest Success");

                    ResourceToolsManifest manifest =
                        CustomJson.JsonParser.ParseJson<ResourceToolsManifest>(uwr.downloadHandler.text);
                    foreach (BundleManifestInfo abInfo in manifest.Bundles)
                    {
                        bool isReadWrite = false;
                        InitRuntimeInfo(abInfo, isReadWrite);
                    }

                    callback?.Invoke(true);
                }
                else
                {
                    SendDebugEvent("Load Bundles StreamingManifest Fail");
                    Debug.LogError("资源清单检查失败");
                    callback?.Invoke(false);
                }
            });

            taskExcutor.AddTask(task);
        }


        /// <summary>
        /// 检查资源版本
        /// </summary>
        public static void CheckoutBundles(Action<int, long> onVersionChecked)
        {
            if (RunMode == RunMode.PackageOnly)
            {
                Debug.LogError("PackageOnly模式下不能调用CheckVersion");
                return;
            }

            ResourceToolsUpdater.CheckoutBundles(onVersionChecked);
        }


        /// <summary>
        /// 根据资源清单信息初始化运行时信息
        /// </summary>
        internal static void InitRuntimeInfo(BundleManifestInfo bundleManifestInfo, bool inReadWrite)
        {
            string bundleName = bundleManifestInfo.BundleName;

            
            BundleRuntimeInfo bundleRuntimeInfo = new BundleRuntimeInfo();

            if (bundleInfoDict.TryGetValue(bundleName, out BundleRuntimeInfo oldBundleRuntimeInfo))
            {
                oldBundleRuntimeInfo.Bundle?.Unload(false);
                bundleRuntimeInfo.UsedAssets = oldBundleRuntimeInfo.UsedAssets;
                bundleRuntimeInfo.DependencyBundles = oldBundleRuntimeInfo.DependencyBundles;
                
                bundleInfoDict.Remove(bundleName);
            }

            bundleInfoDict.Add(bundleName, bundleRuntimeInfo);
            bundleRuntimeInfo.ManifestInfo = bundleManifestInfo;
            bundleRuntimeInfo.InReadWrite = inReadWrite;

            
            foreach (AssetManifestInfo assetManifestInfo in bundleManifestInfo.Assets)
            {
                AssetRuntimeInfo assetRuntimeInfo = new AssetRuntimeInfo();
                if (assetInfoDict.ContainsKey(assetManifestInfo.AssetName))
                {
                    assetInfoDict.Remove(assetManifestInfo.AssetName);
                }

                assetInfoDict.Add(assetManifestInfo.AssetName, assetRuntimeInfo);
                assetRuntimeInfo.ManifestInfo = assetManifestInfo;
                assetRuntimeInfo.BundleName = bundleManifestInfo.BundleName;
            }
        }

        /// <summary>
        /// 获取资源组信息，若不存在则添加
        /// </summary>
        internal static GroupInfo GetOrCreateGroupInfo(string group)
        {
            if (!groupInfoDict.TryGetValue(group, out GroupInfo groupInfo))
            {
                groupInfo = new GroupInfo();
                groupInfo.GroupName = group;
                groupInfoDict.Add(group, groupInfo);
            }

            return groupInfo;
        }

        /// <summary>
        /// 获取指定资源组
        /// </summary>
        public static GroupInfo GetGroupInfo(string group)
        {
            if (!groupInfoDict.TryGetValue(group, out GroupInfo groupInfo))
            {
                Debug.LogError("不存在此资源组：" + group);
            }

            return groupInfo;
        }

        /// <summary>
        /// 获取所有资源组信息
        /// </summary>
        public static List<GroupInfo> GetAllGroup()
        {
            List<GroupInfo> result = new List<GroupInfo>();
            foreach (KeyValuePair<string, GroupInfo> item in groupInfoDict)
            {
                result.Add(item.Value);
            }

            return result;
        }

        #endregion

        #region 资源更新

        /// <summary>
        /// 更新资源
        /// </summary>
        public static void UpdateAssets(Action<bool, int, long, int, long, string, string> onUpdated,
            string updateGroup)
        {
            if (RunMode == RunMode.PackageOnly)
            {
                Debug.LogError("PackageOnly模式下不能调用UpdateAsset");
                return;
            }

            ResourceToolsUpdater.UpdateAssets(onUpdated, updateGroup);
        }

        /// <summary>
        /// 暂停更新资源
        /// </summary>
        public static void PauseUpdateAsset(string group)
        {
            ResourceToolsUpdater.PauseUpdater(true, group);
        }

        /// <summary>
        /// 恢复更新资源
        /// </summary>
        public static void ResumeUpdateAsset(string group)
        {
            ResourceToolsUpdater.PauseUpdater(false, group);
        }

        /// <summary>
        /// 获取指定组的更新器
        /// </summary>
        public static Updater GetUpdater(string group)
        {
            ResourceToolsUpdater.groupUpdaterDict.TryGetValue(group, out Updater result);
            return result;
        }

        /// <summary>
        /// 获取所有更新器
        /// </summary>
        /// <returns></returns>
        public static List<Updater> GetAllUpdater()
        {
            List<Updater> result = new List<Updater>(ResourceToolsUpdater.groupUpdaterDict.Values);
            return result;
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 同步加载AssetBundle
        /// 单独加载Bundle，资源引用管理需要自己处理不推荐使用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundle(string name)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetBundle>(name);
                if (asset)
                {
                    return asset;
                }
                else
                {
                    Debug.LogError($"Bundle加载失败:{name}");
                    return null;
                }
            }
#endif

            if (bundleInfoDict.TryGetValue(name, out BundleRuntimeInfo bundleInfo))
            {
                if (bundleInfo.Bundle == null)
                {
                    bundleInfo.Bundle = AssetBundle.LoadFromFile(bundleInfo.LoadPath);

                    if (bundleInfo.Bundle == null)
                    {
                        SendDebugEvent("Load_Bundle_Fail_Name_" + bundleInfo.LoadPath + "_AssetName_" + name);
                        Debug.LogError("Bundle加载失败：" + name);
                        return null;
                    }
                    else
                    {
                        Debug.Log("Bundle加载成功：" + bundleInfo.Bundle.name);
                        return bundleInfo.Bundle;
                    }
                }
            }
            else
            {
                SendDebugEvent("Load_Bundle_Fail" + name + "_NotFind");
                Debug.LogError(name + " 资源不存在");
                return null;
            }

            return null;
        }


        /// <summary>
        /// 同步加载资源，没有记录引用计数，和解决bundle 重复依赖问题 需要重新写
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="loadedCallback"></param>
        public static T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(name);
                if (asset)
                {
                    return asset;
                }
                else
                {
                    Debug.LogError($"Asset加载失败:{name}");
                    return null;
                }
            }
#endif
            if (assetInfoDict.TryGetValue(name, out AssetRuntimeInfo assetInfo))
            {
                var bundleInfo = bundleInfoDict[assetInfo.BundleName];
                if (bundleInfo.Bundle == null)
                {
                    /// 加载资源
                    var bundle = LoadAssetBundle(assetInfo.BundleName);
                    if (bundle == null)
                    {
                        return null;
                    }
                }

                /// TODO: 如果不用 SubAssets 可以把 key 改成 path 减少 GC。
                var key = $"{name}[{typeof(T).Name}]";
                
                
                /// 添加引用计数
                assetInfo.RefCount++;
                bundleInfo.UsedAssets.Add(name);

                /// 加载资源所有的依赖资源
                foreach (string dependency in assetInfo.ManifestInfo.Dependencies)
                {
                    if (!assetInfoDict.ContainsKey(dependency))
                    {
                        continue;;
                    }
                    
                    LoadAsset<Object>(dependency);
                }

                if (assetInfo.Asset == null || !(assetInfo.Asset is T))
                {
                    assetInfo.Asset = bundleInfo.Bundle.LoadAsset<T>(name);
                    if (assetInfo.Asset)
                    {
                        assetToAssetInfoDict[assetInfo.Asset] = assetInfo;
                    }
                }

                if (bundleInfo.Bundle == null || (!bundleInfo.ManifestInfo.IsScene && assetInfo.Asset == null))
                {
                    /// Bundle加载失败 或者 Asset加载失败 
                    Debug.LogError("Asset加载失败：" + name);

                    if (bundleInfo.Bundle)
                    {
                        /// Bundle加载成功 但是Asset加载失败
                        /// 清空Asset的引用计数
                        assetInfo.RefCount = 0;
                        bundleInfo.UsedAssets.Remove(name);
                        /// 卸载Bundle异步
                        CheckBundleLifeCycle(bundleInfo);

                        /// 加载过依赖 卸载依赖
                        for (int i = 0; i < assetInfo.ManifestInfo.Dependencies.Length; i++)
                        {
                            string dependencyName = assetInfo.ManifestInfo.Dependencies[i];

                            if (assetInfoDict.TryGetValue(dependencyName, out AssetRuntimeInfo dependencyInfo))
                            {
                                if (dependencyInfo.Asset != null)
                                {
                                    //将已加载好的依赖都卸载了
                                    UnloadAsset(dependencyInfo.Asset);
                                }
                            }
                        }
                    }

                    return null;
                }


                return (T) assetInfo.Asset;
            }
            else
            {
                SendDebugEvent("Load_Bundle_Fail_AssetName_" + name + "_NotFind");
                Debug.LogError(name + " 资源不存在");
                return null;
            }
        }

//         /// <summary>
//         /// 加载Asset
//         /// </summary>
//         public static void LoadAssetAsync<T>(string assetName, Action<bool, object> loadedCallback)
//         {
// #if UNITY_EDITOR 
//             if (IsEditorMode)
//             {
//                 EditorLoadAssetTask<T> editorModeTask = new EditorLoadAssetTask<T>(taskExcutor, assetName, loadedCallback);
//                 taskExcutor.AddTask(editorModeTask);
//                 return;
//             }
// #endif
//             //检查Asset是否已在本地准备好
//             if (!CheckAssetReady(assetName))
//             {
//                 return;
//             }
//
//             //创建加载Asset的任务
//             LoadAssetTask task = new LoadAssetTask(taskExcutor, assetName, loadedCallback);
//             taskExcutor.AddTask(task);
//         }
//         

        /// <summary>
        /// 加载Asset
        /// </summary>
        public static void LoadAssetAsync(string assetName, Action<bool, Object> loadedCallback)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                EditorLoadAssetTask<Object> editorModeTask =
                    new EditorLoadAssetTask<Object>(taskExcutor, assetName, loadedCallback);
                taskExcutor.AddTask(editorModeTask);
                return;
            }
#endif
            //检查Asset是否已在本地准备好
            if (!CheckAssetReady(assetName))
            {
                loadedCallback(false, null);
                return;
            }

            //创建加载Asset的任务
            LoadAssetTask task = new LoadAssetTask(taskExcutor, assetName, loadedCallback);
            taskExcutor.AddTask(task);
        }


        /// <summary>
        /// 加载场景
        /// </summary>
        public static void LoadScene(string sceneName, Action<bool, AsyncOperation> loadedCallback)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).completed += (op) =>
                {
                    loadedCallback?.Invoke(true, null);
                };
                return;
            }
#endif
            if (!CheckAssetReady(sceneName))
            {
                return;
            }

            //创建加载场景的任务
            LoadSceneTask task = new LoadSceneTask(taskExcutor, sceneName, loadedCallback);
            taskExcutor.AddTask(task);
        }
        
        /// <summary>
        /// 加载场景Done之后回调
        /// </summary>
        public static void LoadScene(string sceneName, Action<bool> loadedCallback)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).completed += (op) =>
                {
                    loadedCallback?.Invoke(true);
                };
                return;
            }
#endif
            if (!CheckAssetReady(sceneName))
            {
                return;
            }

            //创建加载场景的任务
            LoadSceneTask task = new LoadSceneTask(taskExcutor, sceneName, loadedCallback);
            taskExcutor.AddTask(task);
        }

        /// <summary>
        /// 批量加载Asset
        /// </summary>
        public static void LoadAssetsAsync(List<string> assetNames, Action<List<Object>> loadedCallback)
        {
            if (assetNames == null || assetNames.Count == 0)
            {
                Debug.LogError("批量加载Asset失败，assetNames为空或数量为0");
                return;
            }

#if UNITY_EDITOR
            if (IsEditorMode)
            {
                EditorLoadAssetsTask editorModeTask = new EditorLoadAssetsTask(taskExcutor,
                    nameof(EditorLoadAssetsTask), assetNames, loadedCallback);
                taskExcutor.AddTask(editorModeTask);
                return;
            }
#endif
            //创建批量加载Asset的任务
            LoadAssetsTask task = new LoadAssetsTask(taskExcutor,
                nameof(LoadAssetsTask) + Time.frameCount + UnityEngine.Random.Range(0f, 100f), assetNames,
                loadedCallback);
            taskExcutor.AddTask(task);
        }

        /// <summary>
        /// 检查Asset是否已准备好
        /// </summary>
        public static bool CheckAssetReady(string assetName)
        {
            if (!assetInfoDict.ContainsKey(assetName))
            {
                // Debug.LogError("Asset加载失败，不在资源清单中：" + assetName);
                return false;
            }

            return true;
        }

        #endregion

        #region 资源卸载

        /// <summary>
        /// 卸载Asset
        /// </summary>
        public static void UnloadAsset(Object asset)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                return;
            }
#endif
            if (asset == null)
            {
                return;
            }

            if (!assetToAssetInfoDict.TryGetValue(asset, out AssetRuntimeInfo assetInfo))
            {
                Debug.LogError("要卸载的Asset未加载过：" + asset.name);
                return;
            }

            InternalUnloadAsset(assetInfo);
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public static void UnloadScene(string sceneName, bool unloadScene = true)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                SceneManager.UnloadSceneAsync(sceneName);
                return;
            }
#endif

            if (!assetInfoDict.TryGetValue(sceneName, out AssetRuntimeInfo assetInfo))
            {
                Debug.LogError("要卸载的Scene不在资源清单中 ：" + sceneName);
                return;
            }

            InternalUnloadAsset(assetInfo, unloadScene);
        }


        /// <summary>
        /// 卸载Asset
        /// </summary>
        private static void InternalUnloadAsset(AssetRuntimeInfo assetInfo, bool unloadScene = true)
        {
            if (assetInfo.RefCount == 0)
            {
                return;
            }

            //卸载依赖资源
            foreach (string dependency in assetInfo.ManifestInfo.Dependencies)
            {
                if (assetInfoDict.TryGetValue(dependency, out AssetRuntimeInfo dependencyInfo) &&
                    dependencyInfo.Asset != null)
                {
                    UnloadAsset(dependencyInfo.Asset);
                }
            }

            BundleRuntimeInfo bundleInfo = bundleInfoDict[assetInfo.BundleName];
            if (bundleInfo.ManifestInfo.IsScene && unloadScene)
            {
                //卸载场景
                SceneManager.UnloadSceneAsync(assetInfo.ManifestInfo.AssetName);
            }

            //减少引用计数
            assetInfo.RefCount--;

            if (assetInfo.RefCount == 0)
            {
                //已经没人在使用这个Asset了
                //从Bundle的 UsedAsset 中移除
                bundleInfo.UsedAssets.Remove(assetInfo.ManifestInfo.AssetName);
                CheckBundleLifeCycle(bundleInfo);
            }
        }

        /// <summary>
        /// 检查Bundle是否可以卸载，若可以则卸载
        /// </summary>
        internal static void CheckBundleLifeCycle(BundleRuntimeInfo bundleInfo)
        {
            if (bundleInfo.UsedAssets.Count == 0 && bundleInfo.DependencyCount == 0)
            {
                UnloadBundleTask task = new UnloadBundleTask(taskExcutor, bundleInfo.ManifestInfo.BundleName);
                taskExcutor.AddTask(task);
            }
        }

        #endregion


        public static bool CheckIsDownloadGroup(string groupName)
        {
            var list = new List<string>(ResourceToolsUpdater.groupUpdaterDict.Keys);

            if (list == null)
            {
                return false;
            }

            return list.Contains(groupName);
        }


        public static void SendDebugEvent(string eventData)
        {
            DebugEventAction?.Invoke(ESTag, new Hashtable()
            {
                {
                    "Name",
                    eventData
                }
            });
        }
        

        public static void SendEvent(string eventData)
        {
            SendEventAction?.Invoke(ESTag, new Hashtable()
            {
                {
                    "Name",
                    eventData
                }
            });
        }
        
        public static void SendEvent(string eventName, Hashtable hashtable)
        {
            SendEventAction?.Invoke(eventName, hashtable);
        }
        
    }
}