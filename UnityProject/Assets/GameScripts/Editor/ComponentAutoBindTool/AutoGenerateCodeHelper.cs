using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class AutoGenerateCodeHelper : IAutoGenerateCodeHelper {
    public abstract string DirectoryPath { get; }

    /// <summary>
    /// 生成组件代码
    /// </summary>
    /// <param name="autoBindTool"></param>
    /// <param name="filePath"></param>
    /// <param name="className"></param>
    public abstract void CreateBingCode(ComponentAutoBindTool autoBindTool, string filePath, string className);


    public virtual void CreateViewModelImpCode(ComponentAutoBindTool autoBindTool, string filePath, string className) {
        
    }
    
    /// <summary>
    /// 生成逻辑代码
    /// </summary>
    /// <param name="autoBindTool"></param>
    /// <param name="className"></param>
    /// <param name="codePath"></param>
    /// <returns></returns>
    public abstract string GenAutoBindMountCode(ComponentAutoBindTool autoBindTool, string className, string codePath);

    protected List<string> usingSameStr = new List<string>();

    /// <summary>
    /// 写入第三方引用
    /// </summary>
    /// <param name="streamWriter">写入流</param>
    protected void WriteUsing(StreamWriter streamWriter, ComponentAutoBindTool m_Target) {
        usingSameStr.Clear();
        //根据索引获取
        for (int i = 0; i < m_Target.BindDatas.Count; i++) {
            ComponentAutoBindTool.BindData data = m_Target.BindDatas[i];
            if (!string.IsNullOrEmpty(data.BindCom.GetType().Namespace)) {
                if (usingSameStr.Contains(data.BindCom.GetType().Namespace)) {
                    continue;
                }
                usingSameStr.Add(data.BindCom.GetType().Namespace);
                streamWriter.WriteLine($"using {data.BindCom.GetType().Namespace};");
            }
        }
    }

    string strChangeAuthor = "//修改作者:";
    string strChangeTime = "//修改时间:";

    protected List<string> ChangeFileHead(List<string> strList) {
        for (int i = 0; i < strList.Count; i++) {
            if (strList[i].Contains(strChangeAuthor)) {
                strList[i] = $"{strChangeAuthor}{SystemInfo.deviceName}";
            }
            if (strList[i].Contains(strChangeTime)) {
                strList[i] = $"{strChangeTime}{System.DateTime.Now:yyyy-MM-dd HH-mm-ss}";
            }
        }
        return strList;
    }

    private static string annotationCSStr =
        "// ================================================\r\n" +
        "//描 述:\r\n" +
        "//作 者:#Author#\r\n" +
        "//创建时间:#CreatTime#\r\n" +
        "//修改作者:#ChangeAuthor#\r\n" +
        "//修改时间:#ChangeTime#\r\n" +
        "//版 本:#Version# \r\n" +
        "// ===============================================\r\n";

    protected string GetFileHead() {
        string annotationStr = annotationCSStr;
        //annotationStr = annotationStr.Replace("#Class#",
        //    fileNameWithoutExtension);
        //把#CreateTime#替换成具体创建的时间
        annotationStr = annotationStr.Replace("#CreatTime#",
            System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        annotationStr = annotationStr.Replace("#ChangeTime#",
            System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        //把#Author# 替换
        annotationStr = annotationStr.Replace("#Author#",
            SystemInfo.deviceName);
        //把#ChangeAuthor# 替换
        annotationStr = annotationStr.Replace("#ChangeAuthor#",
            SystemInfo.deviceName);
        //把#Version# 替换
        //annotationStr = annotationStr.Replace("#Version#",
        //    DeerSettingsUtils.FrameworkGlobalSettings.ScriptVersion);
        return annotationStr;
    }
}