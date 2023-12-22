using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ResourceTools
{
    /// <summary>
    /// 加载场景的任务
    /// </summary>
    public class LoadSceneTask : BaseTask
    {
        private const string TAG = "LoadSceneTask----";

        /// <summary>
        /// Asset加载状态
        /// </summary>
        private enum LoadSceneStatus
        {
            None,

            /// <summary>
            /// Bundle加载中
            /// </summary>
            BundleLoading,

            /// <summary>
            /// Bundle加载结束
            /// </summary>
            BundleLoaded,
            
            /// <summary>
            /// 依赖Asset加载中
            /// </summary>
            DependciesLoading,

            /// <summary>
            /// 依赖Asset加载结束
            /// </summary>
            DependciesLoaded,


            /// <summary>
            /// 场景加载中
            /// </summary>
            ScenesLoading,

            /// <summary>
            /// 场景加载结束加载结束
            /// </summary>
            ScenesLoaded,
        }
        
        private enum LoadType
        {
            None,
            Done,
        }


        protected Action<bool, AsyncOperation> onFinished;
        
        protected Action<bool> doneFinished;

        protected BundleRuntimeInfo bundleInfo;

        protected AssetRuntimeInfo assetInfo;

        protected AsyncOperation asyncOp;

        protected LoadSceneMode sceneMode;

        /// <summary>
        /// Asset加载状态
        /// </summary>
        private LoadSceneStatus loadAssetState;

        /// <summary>
        /// 总的依赖Asset数量
        /// </summary>
        private int totalDependencyCount;
        
        /// <summary>
        /// 已加载的依赖Asset数量
        /// </summary>
        private int loadedDependencyCount;
        
        private Action<bool, Object> onDependencyLoaded;

        private LoadType loadType;

        public override float Progress
        {
            get
            {
                if (asyncOp == null)
                {
                    return 0;
                }

                return asyncOp.progress;
            }
        }

        public LoadSceneTask(TaskExcutor owner, string name, Action<bool, AsyncOperation> onFinished, LoadSceneMode sceneMode = LoadSceneMode.Single) : base(owner, name)
        {
            assetInfo = AssetBundlesManager.assetInfoDict[Name];
            bundleInfo = AssetBundlesManager.bundleInfoDict[assetInfo.BundleName];
            this.onFinished = onFinished;
            this.sceneMode = sceneMode;
            onDependencyLoaded = OnDependencyLoaded;
            loadType = LoadType.None;
        }
        
        public LoadSceneTask(TaskExcutor owner, string name, Action<bool> doneFinished, LoadSceneMode sceneMode = LoadSceneMode.Single) : base(owner, name)
        {
            assetInfo = AssetBundlesManager.assetInfoDict[Name];
            bundleInfo = AssetBundlesManager.bundleInfoDict[assetInfo.BundleName];
            this.sceneMode = sceneMode;
            onDependencyLoaded = OnDependencyLoaded;
            this.doneFinished = doneFinished;
            loadType = LoadType.Done;
        }

        protected void LoadAsync()
        {
            asyncOp = SceneManager.LoadSceneAsync(Name, this.sceneMode);
            if (loadType == LoadType.None)
            {
                asyncOp.allowSceneActivation = false;
            }
            else
            {
                asyncOp.allowSceneActivation = true;
            }
        }


        /// <summary>
        /// 执行任务
        /// </summary>
        public override void Execute()
        {
            if (bundleInfo.Bundle == null)
            {
                //Bundle未加载到内存中 加载Bundle

                loadAssetState = LoadSceneStatus.BundleLoading;
                LoadBundleTask task = new LoadBundleTask(owner, assetInfo.BundleName, OnBundleLoaded);
                owner.AddTask(task);
            }
            else
            {
                //Bundle已加载到内存中 直接转移到BundleLoaded状态
                loadAssetState = LoadSceneStatus.BundleLoaded;
            }
        }

        /// <summary>
        /// 依赖资源加载完毕的回调
        /// </summary>
        private void OnDependencyLoaded(bool success, Object asset)
        {
            loadedDependencyCount++;

            if (success)
            {
                
                AssetRuntimeInfo dependencyAssetInfo = AssetBundlesManager.assetToAssetInfoDict[asset];
                BundleRuntimeInfo dependencyBundleInfo = AssetBundlesManager.bundleInfoDict[dependencyAssetInfo.BundleName];

                if (dependencyAssetInfo.BundleName!= bundleInfo.ManifestInfo.BundleName && !bundleInfo.DependencyBundles.Contains(dependencyAssetInfo.BundleName))
                {
                    //记录依赖到的其他Bundle 增加其引用计数
                    bundleInfo.DependencyBundles.Add(dependencyAssetInfo.BundleName);
                    dependencyBundleInfo.DependencyCount++;
                }
            }

          
        }
        

        /// <summary>
        /// Bundle加载结束的回调
        /// </summary>
        private void OnBundleLoaded(bool success)
        {
            if (!success)
            {
                //Bundle加载失败了 直接转移到AssetLoaded状态
                loadAssetState = LoadSceneStatus.ScenesLoaded;
                return;
            }

            loadAssetState = LoadSceneStatus.BundleLoaded;
        }

        /// <summary>
        /// 轮询任务
        /// </summary>
        public override void Update()
        {
            //1.加载Bundle
            if (loadAssetState == LoadSceneStatus.BundleLoading)
            {
                TaskState = TaskStatus.Waiting;
                return;
            }

            if (loadAssetState == LoadSceneStatus.BundleLoaded)
            {
                TaskState = TaskStatus.Executing;
                //添加引用计数
                assetInfo.RefCount++;
                bundleInfo.UsedAssets.Add(Name);

                
                //加载依赖
                loadAssetState = LoadSceneStatus.DependciesLoading;

                totalDependencyCount = assetInfo.ManifestInfo.Dependencies.Length;

                foreach (string dependency in assetInfo.ManifestInfo.Dependencies)
                {
                    AssetBundlesManager.LoadAssetAsync(dependency, onDependencyLoaded);
                }
                return;
            }
            
            
            //2.加载依赖Asset
            if (loadAssetState == LoadSceneStatus.DependciesLoading)
            {
                TaskState = TaskStatus.Waiting;

                //依赖加载中
                if (loadedDependencyCount != totalDependencyCount)
                {
                    return;
                }

                //依赖加载结束
                loadAssetState = LoadSceneStatus.DependciesLoaded;
            }

            if (loadAssetState == LoadSceneStatus.DependciesLoaded)
            {
                //依赖加载结束
                if (assetInfo.Asset == null)
                {
                    loadAssetState = LoadSceneStatus.ScenesLoading;
                    LoadAsync();
                }
                else
                {
                    loadAssetState = LoadSceneStatus.ScenesLoaded;
                }

                return;
            }
            


            //3.加载Asset
            if (loadAssetState == LoadSceneStatus.ScenesLoading)
            {
                TaskState = TaskStatus.Executing;

                if (loadType == LoadType.None)
                {
                    //allowSceneActivation == false 时，load 场景结束后场景进度为0.9f
                    if (asyncOp.progress < 0.89f)
                    {
                        return;
                    }

                    loadAssetState = LoadSceneStatus.ScenesLoaded;
                }
                else if (loadType == LoadType.Done)
                {
                    if (!asyncOp.isDone)
                    {
                        return;
                    }
                    
                    loadAssetState = LoadSceneStatus.ScenesLoaded;                
                }
               
            }

            //4.Asset加载结束
            if (loadAssetState == LoadSceneStatus.ScenesLoaded)
            {
                TaskState = TaskStatus.Finished;
                if (bundleInfo.Bundle == null)
                {
                    //Bundle加载失败 或者 Asset加载失败 
                    Debug.LogError("Asset加载失败：" + Name);
                    if (bundleInfo.Bundle)
                    {
                        //Bundle加载成功 但是Asset加载失败
                        //清空Asset的引用计数
                        assetInfo.RefCount = 0;
                        bundleInfo.UsedAssets.Remove(Name);
                        AssetBundlesManager.CheckBundleLifeCycle(bundleInfo);
                    }

                    if (loadType == LoadType.None)
                    {
                        onFinished?.Invoke(false, null);
                    }
                    else
                    {
                        doneFinished?.Invoke(false);
                    }
                }
                else
                {
                    if (loadType == LoadType.None)
                    {
                        onFinished?.Invoke(true, asyncOp);
                    }
                    else
                    {
                        doneFinished?.Invoke(true);
                    }
                }
            }
        }


        // /// <summary>
        // /// 加载结束
        // /// </summary>
        // protected virtual void LoadDone()
        // {
        //     AssetBundleRequest abAsyncOp = (AssetBundleRequest)asyncOp;
        //     assetInfo.Asset = abAsyncOp.asset;
        //
        //     if (assetInfo.Asset)
        //     {
        //         //添加关联
        //         AssetBundlesManager.assetToAssetInfoDict[assetInfo.Asset] = assetInfo;
        //     }
        // }
    }
}