using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ResourceTools;

namespace ResourceTools.Editor
{
    /// <summary>
    /// 打包规则
    /// </summary>
    [Serializable]
    public class PackageRule:IComparable<PackageRule>
    {
        public string Directory;
        public PackageMode Mode;
        public string Group = AssetBundlesConfig.DefaultGroup;

        public bool InPackage = true;
        
        public int CompareTo(PackageRule other)
        {
            return Directory.CompareTo(other.Directory);
        }
    }
}

