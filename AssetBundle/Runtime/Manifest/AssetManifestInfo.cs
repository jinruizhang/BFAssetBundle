using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceTools
{
    /// <summary>
    /// Asset清单信息
    /// </summary>
    public class AssetManifestInfo
    {
        /// <summary>
        /// Asset名
        /// </summary>
        public string AssetName;

        /// <summary>
        /// 资源类型
        /// </summary>
        public string Type;

        /// <summary>
        /// 依赖的所有Asset
        /// </summary>
        public string[] Dependencies;
    }

}
