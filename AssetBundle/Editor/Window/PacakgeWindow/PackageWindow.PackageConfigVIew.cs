using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ResourceTools.Editor
{
    public partial class PackageWindow
    {
        /// <summary>
        /// 是否初始化过打包配置界面
        /// </summary>
        private bool isInitPackageConfigView;
        
        
        /// <summary>
        /// 可选打包平台
        /// </summary>
        private List<PkgUtil.CustomPlatforms> targetPlatforms = new List<PkgUtil.CustomPlatforms>()
        {
            PkgUtil.CustomPlatforms.Android,
            PkgUtil.CustomPlatforms.iOS,
        };
        
        // private Dictionary<PkgUtil.CustomPlatforms, string> bundleUrl = new Dictionary<PkgUtil.CustomPlatforms, string>()
        // {
        //     { PkgUtil.CustomPlatforms.Android,"" },
        //     { PkgUtil.CustomPlatforms.Android_D, "" },
        //     { PkgUtil.CustomPlatforms.iOS,"" },
        //     { PkgUtil.CustomPlatforms.iOS_D,"" },
        // };

        // /// <summary>
        // /// 各平台选择状态
        // /// </summary>
        // private Dictionary<BuildTarget, bool> selectedPlatforms = new Dictionary<BuildTarget, bool>()
        // {
        //     // {BuildTarget.StandaloneWindows,false },
        //     // {BuildTarget.StandaloneWindows64,false },
        //     {BuildTarget.Android,false},
        //     {BuildTarget.iOS,false},
        // };
        private PkgUtil.CustomPlatforms selectedPlatform = PkgUtil.CustomPlatforms.Android;

        
        
        /// <summary>
        /// 可选打包设置
        /// </summary>
        private string[] options = Enum.GetNames(typeof(BuildAssetBundleOptions));

        /// <summary>
        /// 打包设置选择状态
        /// </summary>
        private Dictionary<string, bool> selectedOptions = new Dictionary<string, bool>();



        /// <summary>
        /// 初始化打包配置界面
        /// </summary>
        private void InitPackgeConfigView()
        {
            // foreach (BuildTarget item in PkgUtil.PkgCfg.TargetPlatforms)
            // {
            //     selectedPlatforms[item] = true;
            // }

            for (int i = targetPlatforms.Count - PkgUtil.PkgCfg.ServerUrl.Count; i > 0; i--)
            {
                PkgUtil.PkgCfg.ServerUrl.Add("");
            }


            selectedPlatform = PkgUtil.PkgCfg.TargetPlatform;
            
            
            for (int i = 1; i < options.Length; i++)
            {
                BuildAssetBundleOptions option = (BuildAssetBundleOptions)Enum.Parse(typeof(BuildAssetBundleOptions), options[i]);
            
                if ((PkgUtil.PkgCfg.Options & option) > 0)
                {
                    selectedOptions[options[i]] = true;
                }
                else
                {
                    selectedOptions[options[i]] = false;
                }
            }
        }

        /// <summary>
        /// 保存打包配置
        /// </summary>
        private void SavePackageConfig()
        {

            // PkgUtil.PkgCfg.TargetPlatform.Clear();
            // foreach (KeyValuePair<BuildTarget, bool> item in selectedPlatforms)
            // {
            //     if (item.Value == true)
            //     {
            //         PkgUtil.PkgCfg.TargetPlatforms.Add(item.Key);
            //     }
            // }
            
            PkgUtil.PkgCfg.TargetPlatform = selectedPlatform;

            PkgUtil.PkgCfg.Options = BuildAssetBundleOptions.None;
            
            foreach (KeyValuePair<string, bool> item in selectedOptions)
            {
                if (item.Value == true)
                {
                    BuildAssetBundleOptions option = (BuildAssetBundleOptions)Enum.Parse(typeof(BuildAssetBundleOptions), item.Key);
                    PkgUtil.PkgCfg.Options |= option;
                }
            }

            EditorUtility.SetDirty(PkgUtil.PkgCfg);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 绘制打包配置界面
        /// </summary>
        private void DrawPackageConfigView()
        {
            if (!isInitPackageConfigView)
            {
                InitPackgeConfigView();
                isInitPackageConfigView = true;
            }

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("游戏版本号：" + Application.version,GUILayout.Width(200));

                EditorGUILayout.LabelField("资源清单版本号：", GUILayout.Width(100));
                PkgUtil.PkgCfg.ManifestVersions[(BuildTarget)selectedPlatform] = EditorGUILayout.IntField(PkgUtil.PkgCfg.ManifestVersion((BuildTarget)selectedPlatform), GUILayout.Width(50));
                
            }

            EditorGUILayout.Space();
            
            selectedPlatform = (PkgUtil.CustomPlatforms)EditorGUILayout.EnumPopup("请选择打包平台： ", selectedPlatform);
            
            // EditorGUILayout.Space();
            //
            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     GUILayout.Label("Bundle服务器地址：", GUILayout.Width(150));
            //     PkgUtil.PkgCfg.ServerUrl[(int)selectedPlatform] = GUILayout.TextField(PkgUtil.PkgCfg.ServerUrl[(int)selectedPlatform]);
            // }
            
            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     for (int i = 0; i < targetPlatforms.Count; i++)
            //     {
            //         using (EditorGUILayout.ToggleGroupScope toggle = new EditorGUILayout.ToggleGroupScope(targetPlatforms[i].ToString(), selectedPlatforms[targetPlatforms[i]]))
            //         {
            //             selectedPlatforms[targetPlatforms[i]] = toggle.enabled;
            //         }
            //
            //     }
            // }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("选择打包设置：");

            if (selectedOptions.Count == 0)
            {
                InitPackgeConfigView();
            }

            for (int i = 1; i < options.Length; i += 3)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = 0; j < 3 && i + j < options.Length; j++)
                    {
                        using (EditorGUILayout.ToggleGroupScope toggle = new EditorGUILayout.ToggleGroupScope(options[i + j], selectedOptions[options[i + j]]))
                        {
                            selectedOptions[options[i + j]] = toggle.enabled;
                        }
                    }

                }
            }

            EditorGUILayout.Space();



            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("打包输出根目录：", GUILayout.Width(100));
                PkgUtil.PkgCfg.OutputPath = GUILayout.TextField(PkgUtil.PkgCfg.OutputPath);
                if (GUILayout.Button("选择目录", GUILayout.Width(100)))
                {
                    string folder = EditorUtility.OpenFolderPanel("选择打包输出根目录", PkgUtil.PkgCfg.OutputPath, Directory.GetCurrentDirectory());
                    if (folder != string.Empty)
                    {
                        PkgUtil.PkgCfg.OutputPath = folder.Replace(Directory.GetCurrentDirectory() + "/","");
                    }
                }
            }
            
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("导出项目目录：", GUILayout.Width(100));
                PkgUtil.PkgCfg.ExportProjectName[(BuildTarget)selectedPlatform] = GUILayout.TextField(PkgUtil.PkgCfg.ExportProjectLabelName((BuildTarget)selectedPlatform));
                if (GUILayout.Button("选择目录", GUILayout.Width(100)))
                {
                    string folder = EditorUtility.OpenFolderPanel("选择导出项目目录", PkgUtil.PkgCfg.ExportProjectLabelName((BuildTarget)selectedPlatform), Directory.GetCurrentDirectory());
                    if (folder != string.Empty)
                    {
                        PkgUtil.PkgCfg.ExportProjectName[(BuildTarget)selectedPlatform] = folder.Replace(Directory.GetCurrentDirectory() + "/","");
                    }
                }
            }

            

            EditorGUILayout.Space();

            using (EditorGUILayout.ToggleGroupScope toggle = new EditorGUILayout.ToggleGroupScope("冗余分析", PkgUtil.PkgCfg.IsAnalyzeRedundancy))
            {
                PkgUtil.PkgCfg.IsAnalyzeRedundancy = toggle.enabled;
            }


            EditorGUILayout.Space();

            
            // using (EditorGUILayout.ToggleGroupScope toggle = new EditorGUILayout.ToggleGroupScope("打包平台只选中了1个时，打包后复制资源到StreamingAssets下", PkgUtil.PkgCfg.IsCopyToStreamingAssets))
            // {
            //     PkgUtil.PkgCfg.IsCopyToStreamingAssets = toggle.enabled;
            // }
            
            // if (PkgUtil.PkgCfg.IsCopyToStreamingAssets)
            // {
            //     EditorGUILayout.LabelField("要复制的资源组（以分号分隔，为空则全部复制）：");
            //     PkgUtil.PkgCfg.CopyGroup = EditorGUILayout.TextField(PkgUtil.PkgCfg.CopyGroup);
            // }

            EditorGUILayout.Space();
            

            using (new EditorGUILayout.HorizontalScope())
            {

                if (GUILayout.Button("打包AssetBundle", GUILayout.Width(200)))
                {

                    /// 检查是否选中至少一个平台
                    if (!targetPlatforms.Contains(PkgUtil.PkgCfg.TargetPlatform))
                    {
                        EditorUtility.DisplayDialog("提示", "请设置正常的打包渠道", "确认");
                        return;
                    }

                    if (!PkgUtil.PkgCfg.ExportProjectName.TryGetValue((BuildTarget)PkgUtil.PkgCfg.TargetPlatform, out string path) || String.IsNullOrEmpty(path))
                    {
                        EditorUtility.DisplayDialog("提示", "请先设置导出项目名，用于导出时删除指定平台下，不是该平台使用的Bundle文件", "确认");
                    }


                    if (PkgUtil.PkgCfg.TargetPlatform == PkgUtil.CustomPlatforms.Android)
                    {
#if !UNITY_ANDROID
                        EditorUtility.DisplayDialog("提示", "切换平台在打包", "确认");
                        return;
#endif
                    }
                    else if (PkgUtil.PkgCfg.TargetPlatform == PkgUtil.CustomPlatforms.iOS)
                    {
#if !UNITY_IOS
                        EditorUtility.DisplayDialog("提示", "切换平台在打包", "确认");
                        return;
#endif
                    }
                    
                    
                    


                    var platform = (BuildTarget) PkgUtil.PkgCfg.TargetPlatform;

                    Packager.ExecutePackagePipeline( PkgUtil.PkgCfg.Options, platform, PkgUtil.PkgCfg.IsAnalyzeRedundancy, PkgUtil.PkgCfg.CopyGroup);
                        


                    EditorUtility.DisplayDialog("提示", "打包AssetBundle结束", "确认");

                    SavePackageConfig();

                    return;
                }
            }


            if (EditorGUI.EndChangeCheck())
            {
                SavePackageConfig();
            }
        }
    }
}

