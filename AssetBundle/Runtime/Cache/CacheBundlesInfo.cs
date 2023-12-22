/****************************************************
文件：CacheBundlesInfo
作者：haitao.li
日期：2023/02/05 11:33:46
功能：记录所有的 Bundle 信息
*****************************************************/

using System.Collections.Generic;

namespace ResourceTools.Cache
{
    public class CacheBundlesInfo
    {
        public List<CacheBundle> BundlesInfo;


        public bool ContainsKey(string key)
        {
            if (BundlesInfo == null)
            {
                return false;
            }

            foreach (var item in BundlesInfo)
            {
                if (item.BundleName == key)
                {
                    return true;
                }
            }

            return false;
        }
    }
}