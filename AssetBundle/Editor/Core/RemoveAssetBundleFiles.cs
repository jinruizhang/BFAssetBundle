/****************************************************
文件：RemoveAssetBundleFiles
作者：haitao.li
日期：2023/07/12 12:18:31
功能：打包结束后移除不同平台的文件夹
*****************************************************/


using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace  ResourceTools.Editor
{
    public class RemoveAssetBundleFiles : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return  Int32.MaxValue;} }
        public void OnPostprocessBuild(BuildReport report)
        {
            string exceptDirectory = Application.platform.ToString();
            string platformPaht = "";
#if UNITY_ANDROID
            if (!PkgUtil.PkgCfg.ExportProjectName.TryGetValue(BuildTarget.Android, out string projectName))
            {
                Debug.LogError("Android平台导出时，删除其他平台Bundle时路径设置错误");
                return;
            }
            exceptDirectory = "Android";
            platformPaht = "unityLibrary/src/main/assets/AssetBundles";
#elif UNITY_IOS
            if (!PkgUtil.PkgCfg.ExportProjectName.TryGetValue(BuildTarget.iOS, out string projectName))
            {
                Debug.LogError("iOS平台导出时，删除其他平台Bundle时路径设置错误");
                return;
            }
            exceptDirectory = "iOS";
            platformPaht =  "Data/Raw/AssetBundles";
#else
            if (!PkgUtil.PkgCfg.ExportProjectName.TryGetValue(Application.platform, out string projectName))
            {
                Debug.LogError(Application.platform.ToString() + "平台导出时，删除其他平台Bundle时路径设置错误");
                return;
            }
#endif
            
            // 获取Assets文件夹的路径
            string assetsPath = Application.dataPath;
        
            // 获取Assets文件夹的父目录，也就是项目的根目录
            string projectRootPath = Directory.GetParent(assetsPath).ToString();
            
            string targetPath = Path.Combine(projectRootPath, projectName,platformPaht);

            DeleteDirectoriesExcept(targetPath, exceptDirectory);
            
        }
        
        void DeleteDirectoriesExcept(string parentDirectory, string exceptDirectory)
        {
            // 拼接得到要删除目录的路径
            if (!Directory.Exists(parentDirectory))
            {
                return;
            }
            
            // 获取所有子目录
            string[] directories = Directory.GetDirectories(parentDirectory);

            foreach (string directory in directories)
            {
                // 如果是我们想要保留的目录，则跳过
                if (directory.EndsWith(exceptDirectory))
                    continue;

                // 删除目录
                Directory.Delete(directory, true);
            }

            // 刷新AssetDatabase以确保Unity编辑器显示的是最新的文件系统状态
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        
        
        
    }
}