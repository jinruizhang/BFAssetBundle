/****************************************************
文件：BundleItem
作者：haitao.li
日期：2023/07/04 10:17:14
功能：Editor 下一条Bundle的信息
*****************************************************/

using UnityEngine;

namespace ResourceTools.Editor
{
    [System.Serializable]
    public class BundleItem
    {
        public string BundleName;
        public string VersionName;
        public Hash128 Hash;
        public int VersionCode;

        public BundleItem(string bundleName, string versionName, Hash128 hash, int versionCode)
        {
            BundleName = bundleName;
            VersionName = versionName;
            Hash = hash;
            VersionCode = versionCode;
        }

        public BundleItem(BundleManifestInfo info)
        {
            BundleName = info.BundleName;
            VersionName = info.VersionName;
            Hash = info.Hash;
            VersionCode = info.VersionCode;
        }
    }
}