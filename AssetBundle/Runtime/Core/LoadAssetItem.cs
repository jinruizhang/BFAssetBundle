using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceTools
{
    public  class LoadAssetItem
    {
        
        /// <summary>
        /// 总的依赖Asset数量
        /// </summary>
        private int totalDependencyCount;

        /// <summary>
        /// 已加载的依赖Asset数量
        /// </summary>
        private int loadedDependencyCount;
        
        protected BundleRuntimeInfo bundleInfo;
        protected AssetRuntimeInfo assetInfo;
        
        
        private Action<bool, Object> onDependencyLoaded;
        
        protected Action<bool, Object> onFinished;
        
        /// <summary>
        /// 资源名称
        /// </summary>
        public string Name;


        
        public LoadAssetItem(string name, Action<bool, Object> onFinished)
        {
            Name = name;
            if (AssetBundlesManager.assetInfoDict.TryGetValue(name, out assetInfo))
            {
                bundleInfo = AssetBundlesManager.bundleInfoDict[assetInfo.BundleName];
                onDependencyLoaded = OnDependencyLoaded;
                this.onFinished = onFinished;
                this.loadAsset();
            }
            else
            {
                Debug.LogError( name + " 资源不存在");
            }
        }
        
        
        private void loadAsset()
        {
            if (bundleInfo.Bundle == null)
            {
                this.onBundleLoading();
            }
            else
            {
                //Bundle已加载到内存中 直接转移到BundleLoaded状态
                onBundleLoaded();
            }
        }


        private void onBundleLoading()
        {
            //Bundle未加载到内存中 加载Bundle
            var bundleInfo = AssetBundlesManager.bundleInfoDict[assetInfo.BundleName];
            bundleInfo.Bundle = AssetBundle.LoadFromFile(bundleInfo.LoadPath);
    
            if (bundleInfo.Bundle == null)
            {
                Debug.LogError("Bundle加载失败：" + bundleInfo.Bundle.name);
                this.onAssetLoaded();
            }
            else
            {
                Debug.Log("Bundle加载成功：" + bundleInfo.Bundle.name);
                this.onBundleLoaded();
            }
        }
        

        private void onAssetLoading()
        {
            assetInfo.Asset = bundleInfo.Bundle.LoadAsset(Name);

            if (assetInfo.Asset)
            {
                //添加关联
                AssetBundlesManager.assetToAssetInfoDict[assetInfo.Asset] = assetInfo;
            }
            
            this.onAssetLoaded();
        }
        


        private void onAssetLoaded()
        {
            if (bundleInfo.Bundle == null || (!bundleInfo.ManifestInfo.IsScene && assetInfo.Asset == null))
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

                    //加载过依赖 卸载依赖
                    UnloadDependencies();
                }

                onFinished?.Invoke(false, null);
            }
            else
            {
                Debug.Log("Asset加载成功：" + Name);
                onFinished?.Invoke(true, assetInfo.Asset);
            }
        }
        
        


        private void onBundleLoaded()
        {
            //添加引用计数
            assetInfo.RefCount++;
            bundleInfo.UsedAssets.Add(Name);
            totalDependencyCount = assetInfo.ManifestInfo.Dependencies.Length;
            foreach (string dependency in assetInfo.ManifestInfo.Dependencies)
            {
                // AssetBundlesManager.LoadAsset(dependency, onDependencyLoaded);
            }

            dependencyLoadCB();

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
        /// 所有依赖的资源加载完毕
        /// </summary>
        private void dependencyLoadCB()
        {
            if (loadedDependencyCount != totalDependencyCount)
            {
                return;
            }
            
            if (assetInfo.Asset == null)
            {
                onAssetLoading();
            }
            else
            {
                onAssetLoaded();
            }
        }
        
        
        /// <summary>
        /// 卸载依赖的Asset
        /// </summary>
        protected void UnloadDependencies()
        {
            for (int i = 0; i < assetInfo.ManifestInfo.Dependencies.Length; i++)
            {
                string dependencyName = assetInfo.ManifestInfo.Dependencies[i];

                if (AssetBundlesManager.assetInfoDict.TryGetValue(dependencyName,out AssetRuntimeInfo dependencyInfo))
                {
                    if (dependencyInfo.Asset != null)
                    {
                        //将已加载好的依赖都卸载了
                        AssetBundlesManager.UnloadAsset(dependencyInfo.Asset);
                    }
                }
            }
        }

    }
}