using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System;

namespace ResourceTools
{
    public static class Util
    {

        public static string GetRemoteUrl()
        {
            if (String.IsNullOrEmpty(AssetBundlesManager.RemoteUrl))
            {
                Debug.LogError("The remote Version URL is empty!");
            }
            
            return Path.Combine(AssetBundlesManager.RemoteUrl, Application.version);
        }
        
        /// <summary>
        /// 下载Bundle时使用的URL
        /// </summary>
        /// <returns></returns>
        public static string GetDownloadUrl()
        {
            /// 如果RemoteUrl 为空，就报错
            if (String.IsNullOrEmpty(AssetBundlesManager.RemoteUrl))
            {
                Debug.LogError("The remote Version URL is empty!");
            }
            
            return  AssetBundlesManager.RemoteUrl;
        }
        

        /// <summary>
        /// 获取在只读区下的完整路径
        /// File 接口使用
        /// 在Android 平台不能通过File来访问StreamingAsset 文件夹
        /// </summary>
        public static string GetReadOnlyPath(string path)
        {
            string readOnlyPath = GetStreamingAssetsPath();
            string result = Path.Combine(readOnlyPath, path);
            Debug.Log("GetReadOnlyPath = " + result);
            return result;
        }

        /// <summary>
        /// 使用UnityWebRequest 加载的路劲，后边要换种方式，保存俩路径太蠢
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetReadOnlyPathRequest(string path )
        {
            string readOnlyPath = GetStreamingAssetsPath();
            
#if UNITY_IOS
            readOnlyPath = $"file://{readOnlyPath}";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
            readOnlyPath = "file://" + readOnlyPath + "/";
#endif
            
            string result = Path.Combine(readOnlyPath, path);
            // Debug.Log("GetReadOnlyPath = " + result);
            return result;
        }
        


        /// <summary>
        /// 获取在读写区下的完整路径
        /// 使用UnityWebRequest加载资源需要拼接file:///
        /// </summary>
        public static string GetReadWritePath(string path)
        {
            string result = Path.Combine(GetPersistentDataPath(), path);
            
            // Debug.Log("GetReadWritePath = " + result);

            return result;
        }

        public static string GetReadWritePathRequest(string path)
        {
            string result = GetReadWritePath(path);

            result = "file://" + result;

            return result;

        }

        /// <summary>
        /// 沙盒路径下存放Bundle的目录
        /// </summary>
        /// <returns></returns>
        public static string GetPersistentDataPath()
        {
            string path = Path.Combine(Application.persistentDataPath, AssetBundlesConfig.AssetBundlesFolderName);

            return path;
        }


        /// <summary>
        /// streamingAsset 目录下的Bundle路径
        /// </summary>
        /// <returns></returns>
        public static string GetStreamingAssetsPath()
        {
            
            string target = "Android";
#if UNITY_IOS
            target = "iOS";
#elif !UNITY_ANDROID
            Debug.LogError("不是Android 和 iOS 平台");
#endif
            
            string path = Path.Combine(Application.streamingAssetsPath, AssetBundlesConfig.AssetBundlesFolderName, target);

            return path;
        }

        
        
    }

    

}
