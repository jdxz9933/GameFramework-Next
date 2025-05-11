using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAutoGenerateCodeHelper {
    /// <summary>
    /// 自动绑定代码生成目录
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// 生成组件代码
    /// </summary>
    /// <param name="autoBindTool"></param>
    /// <param name="filePath"></param>
    /// <param name="className"></param>
    public void CreateBingCode(ComponentAutoBindTool autoBindTool, string filePath, string className);

    public void CreateViewModelImpCode(ComponentAutoBindTool autoBindTool, string filePath, string className);

    /// <summary>
    /// 生成逻辑代码
    /// </summary>
    /// <param name="autoBindTool"></param>
    /// <param name="className"></param>
    /// <param name="codePath"></param>
    /// <returns></returns>
    public abstract string GenAutoBindMountCode(ComponentAutoBindTool autoBindTool, string className, string codePath);
}