/****************************************************
文件：AssetBundlesVersion
作者：haitao.li
日期：2023/02/01 16:33:36
功能：记录Version 数据类
*****************************************************/

using System.IO;

namespace ResourceTools
{
    public class AssetBundlesVersion
    {
        public string AppVersion;
        public int ManifestVersion;
        // public string RemoteLoadPath;
        public string Platform;

        public bool Equals(AssetBundlesVersion versionData)
        {
            if (!AppVersion.Equals(versionData.AppVersion))
            {
                return false;
            }

            if (ManifestVersion != versionData.ManifestVersion)
            {
                return false;
            }

            return true;
        }
        

        public string GetRemoteBundleUrl()
        {
            return Path.Combine(Util.GetRemoteUrl(), AppVersion + AssetBundlesConfig.Splicing + ManifestVersion);
        }
        
    }
}