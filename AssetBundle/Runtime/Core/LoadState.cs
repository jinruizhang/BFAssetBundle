/****************************************************
文件：LoadState
作者：haitao.li
日期：2023/02/03 10:47:40
功能：loading各种状态
*****************************************************/

namespace ResourceTools
{
    public enum LoadState
    {
        None,
        CreateEssentialFile,
        LoadVersionFile,
        DownloadManifest,
        
        CheckoutBundles,
        
        LoadBundleData,
    }
}