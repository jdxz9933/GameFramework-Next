using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using BindData = ComponentAutoBindTool.BindData;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;
using System.Text;
using GameFramework;
using UnityEngine.UI;

[CustomEditor(typeof(ComponentAutoBindTool))]
public class ComponentAutoBindToolInspector : Editor
{
    private ComponentAutoBindTool m_Target;

    private SerializedProperty m_BindDatas;
    private SerializedProperty m_BindComs;
    private List<BindData> m_TempList = new List<BindData>();
    private List<string> m_TempFiledNames = new List<string>();
    private List<string> m_TempComponentTypeNames = new List<string>();

    private string[] s_AssemblyNames = { "Assembly-CSharp-Editor", "GameLogic" };
    private string[] m_HelperTypeNames;
    private string m_HelperTypeName;
    private int m_HelperTypeNameIndex;

    private string[] m_GenHelperTypeNames;
    private string m_GenHelperTypeName;
    private int m_GenHelperTypeNameIndex;

    private AutoBindGlobalSetting m_Setting;

    private SerializedProperty m_Namespace;
    private SerializedProperty m_ClassName;
    private SerializedProperty m_ViewModelName;
    private SerializedProperty m_ComCodePath;
    private SerializedProperty m_MountCodePath;

    private void OnEnable()
    {
        m_Target = (ComponentAutoBindTool)target;
        m_BindDatas = serializedObject.FindProperty("BindDatas");
        m_BindComs = serializedObject.FindProperty("m_BindComs");

        m_HelperTypeNames = GetTypeNames(typeof(IAutoBindRuleHelper), s_AssemblyNames);
        m_GenHelperTypeNames = GetTypeNames(typeof(AutoGenerateCodeHelper), s_AssemblyNames);
        m_Setting = AutoBindGlobalSetting.GetAutoBindGlobalSetting();
        m_Namespace = serializedObject.FindProperty("m_Namespace");
        m_ClassName = serializedObject.FindProperty("m_ClassName");
        m_ViewModelName = serializedObject.FindProperty("m_ViewModelName");
        m_ComCodePath = serializedObject.FindProperty("m_ComCodePath");
        m_MountCodePath = serializedObject.FindProperty("m_MountCodePath");

        m_Namespace.stringValue = string.IsNullOrEmpty(m_Namespace.stringValue)
            ? m_Setting.Namespace
            : m_Namespace.stringValue;
        m_ClassName.stringValue = string.IsNullOrEmpty(m_ClassName.stringValue)
            ? m_Target.gameObject.name
            : m_ClassName.stringValue;
        m_ComCodePath.stringValue = string.IsNullOrEmpty(m_ComCodePath.stringValue)
            ? m_Setting.ComCodePath
            : m_ComCodePath.stringValue;
        m_MountCodePath.stringValue = string.IsNullOrEmpty(m_MountCodePath.stringValue)
            ? m_Setting.MountCodePath
            : m_MountCodePath.stringValue;

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTopButton();

        DrawHelperSelect();
        DrawGenHelperSelect();
        DrawSetting();

        DrawKvData();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 绘制顶部按钮
    /// </summary>
    private void DrawTopButton()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("排序"))
        {
            Sort();
        }

        if (GUILayout.Button("全部删除"))
        {
            RemoveAll();
        }

        if (GUILayout.Button("删除空引用"))
        {
            RemoveNull();
        }

        if (GUILayout.Button("自动绑定组件"))
        {
            AutoBindComponent();
        }

        if (GUILayout.Button("生成绑定代码"))
        {
            if (m_ClassName.stringValue.Equals("UIForm") || string.IsNullOrEmpty(m_ClassName.stringValue))
            {
                EditorUtility.DisplayDialog("提示", "请先同步物体名字到类名，点击【物体名】按钮", "确定");
                return;
            }

            GenAutoBindCode();
        }

        if (GUILayout.Button("挂载逻辑代码"))
        {
            string className = !string.IsNullOrEmpty(m_Target.ClassName)
                ? m_Target.ClassName
                : m_Target.gameObject.name;
            if (!m_Target.gameObject.GetComponent(className))
            {
                Type _type = GetTypeWithName(className);
                if (_type != null)
                {
                    m_Target.gameObject.AddComponent(_type);
                }
            }

            EditorUtility.SetDirty(m_Target.gameObject);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 排序
    /// </summary>
    private void Sort()
    {
        m_TempList.Clear();
        foreach (BindData data in m_Target.BindDatas)
        {
            var newdata = new BindData(data.Name, data.BindCom);
            newdata.PropertyInfoName = data.PropertyInfoName;
            m_TempList.Add(newdata);
        }

        m_TempList.Sort((x, y) => { return string.Compare(x.Name, y.Name, StringComparison.Ordinal); });

        m_BindDatas.ClearArray();
        foreach (BindData data in m_TempList)
        {
            AddBindData(data.Name, data.BindCom, data.PropertyInfoName);
        }

        SyncBindComs();
    }

    /// <summary>
    /// 全部删除
    /// </summary>
    private void RemoveAll()
    {
        m_BindDatas.ClearArray();

        SyncBindComs();
    }

    /// <summary>
    /// 删除空引用
    /// </summary>
    private void RemoveNull()
    {
        for (int i = m_BindDatas.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
            if (element.objectReferenceValue == null)
            {
                m_BindDatas.DeleteArrayElementAtIndex(i);
            }
        }

        SyncBindComs();
    }

    /// <summary>
    /// 自动绑定组件
    /// </summary>
    private void AutoBindComponent()
    {
        m_TempList.Clear();
        foreach (BindData data in m_Target.BindDatas)
        {
            var newData = new BindData(data.Name, data.BindCom);
            newData.PropertyInfoName = data.PropertyInfoName;
            m_TempList.Add(newData);
        }

        m_BindDatas.ClearArray();

        Transform[] childs = m_Target.gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in childs)
        {
            m_TempFiledNames.Clear();
            m_TempComponentTypeNames.Clear();
            if (child == m_Target.transform)
            {
                continue;
            }

            ComponentAutoBindTool componentAuto1 = child.gameObject.GetComponent<ComponentAutoBindTool>();
            ComponentAutoBindTool componentAuto = child.gameObject.GetComponentInParent<ComponentAutoBindTool>(true);
            if (componentAuto1 == null)
            {
                if (componentAuto != null && componentAuto != m_Target)
                {
                    continue;
                }
            }

            if (AutoBindGlobalSetting.IsValidBind(child, m_TempFiledNames, m_TempComponentTypeNames))
            {
                for (int i = 0; i < m_TempFiledNames.Count; i++)
                {
                    Component com = child.GetComponent(m_TempComponentTypeNames[i]);
                    if (com == null)
                    {
                        Debug.LogError($"{child.name}上不存在{m_TempComponentTypeNames[i]}的组件");
                    }
                    else
                    {
                        string newFiledName = m_TempFiledNames[i].Replace("#", "");
                        string fieldInfoName = string.Empty;
                        for (int j = 0; j < m_TempList.Count; j++)
                        {
                            if (m_TempList[j].Name == newFiledName)
                            {
                                fieldInfoName = m_TempList[j].PropertyInfoName;
                                break;
                            }
                        }

                        AddBindData(newFiledName, child.GetComponent(m_TempComponentTypeNames[i]), fieldInfoName);
                    }
                }
            }
        }

        SyncBindComs();
    }

    private void DrawGenHelperSelect()
    {
        m_GenHelperTypeName = m_GenHelperTypeNames[0];
        // if (m_Target.GenerateCodeHelper != null) {
        //     m_GenHelperTypeName = m_Target.GenerateCodeHelper.GetType().Name;
        //     for (int i = 0; i < m_GenHelperTypeNames.Length; i++) {
        //         if (m_GenHelperTypeName == m_GenHelperTypeNames[i]) {
        //             m_GenHelperTypeNameIndex = i;
        //         }
        //     }
        // } else {
        //     IAutoGenerateCodeHelper helper =
        //         (IAutoGenerateCodeHelper)CreateHelperInstance(m_GenHelperTypeName, s_AssemblyNames);
        //     m_Target.GenerateCodeHelper = helper;
        // }

        if (!string.IsNullOrEmpty(m_Target.generateCodeHelperTypeName))
        {
            m_GenHelperTypeName = m_Target.generateCodeHelperTypeName;
            for (int i = 0; i < m_GenHelperTypeNames.Length; i++)
            {
                if (m_GenHelperTypeName == m_GenHelperTypeNames[i])
                {
                    m_GenHelperTypeNameIndex = i;
                }
            }
        }

        IAutoGenerateCodeHelper helper =
            (IAutoGenerateCodeHelper)CreateHelperInstance(m_GenHelperTypeName, s_AssemblyNames);
        m_Target.GenerateCodeHelper = helper;
        m_Target.generateCodeHelperTypeName = m_GenHelperTypeName;

        // foreach (GameObject go in Selection.gameObjects) {
        //     ComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();
        //     if (autoBindTool.RuleHelper == null) {
        //         // IAutoGenerateCodeHelper helper =
        //         //     (IAutoGenerateCodeHelper)CreateHelperInstance(m_GenHelperTypeName, s_AssemblyNames);
        //         autoBindTool.GenerateCodeHelper = helper;
        //     }
        // }
        int selectedIndex =
            EditorGUILayout.Popup("AutoBindGenCodeHelper", m_GenHelperTypeNameIndex, m_GenHelperTypeNames);
        if (selectedIndex != m_GenHelperTypeNameIndex)
        {
            m_GenHelperTypeNameIndex = selectedIndex;
            m_GenHelperTypeName = m_GenHelperTypeNames[selectedIndex];
            AutoGenerateCodeHelper helper1 =
                (AutoGenerateCodeHelper)CreateHelperInstance(m_GenHelperTypeName, s_AssemblyNames);
            m_Target.GenerateCodeHelper = helper1;
            m_Target.generateCodeHelperTypeName = m_GenHelperTypeName;
            EditorUtility.SetDirty(m_Target.gameObject);
        }
    }

    /// <summary>
    /// 绘制辅助器选择框
    /// </summary>
    private void DrawHelperSelect()
    {
        m_HelperTypeName = m_HelperTypeNames[0];

        if (m_Target.RuleHelper != null)
        {
            m_HelperTypeName = m_Target.RuleHelper.GetType().Name;

            for (int i = 0; i < m_HelperTypeNames.Length; i++)
            {
                if (m_HelperTypeName == m_HelperTypeNames[i])
                {
                    m_HelperTypeNameIndex = i;
                }
            }
        }
        else
        {
            IAutoBindRuleHelper helper = (IAutoBindRuleHelper)CreateHelperInstance(m_HelperTypeName, s_AssemblyNames);
            m_Target.RuleHelper = helper;
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            ComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();
            if (autoBindTool != null && autoBindTool.RuleHelper == null)
            {
                IAutoBindRuleHelper helper =
                    (IAutoBindRuleHelper)CreateHelperInstance(m_HelperTypeName, s_AssemblyNames);
                autoBindTool.RuleHelper = helper;
            }
        }

        int selectedIndex = EditorGUILayout.Popup("AutoBindRuleHelper", m_HelperTypeNameIndex, m_HelperTypeNames);
        if (selectedIndex != m_HelperTypeNameIndex)
        {
            m_HelperTypeNameIndex = selectedIndex;
            m_HelperTypeName = m_HelperTypeNames[selectedIndex];
            IAutoBindRuleHelper helper = (IAutoBindRuleHelper)CreateHelperInstance(m_HelperTypeName, s_AssemblyNames);
            m_Target.RuleHelper = helper;
            EditorUtility.SetDirty(m_Target.gameObject);
        }
    }

    /// <summary>
    /// 绘制设置项
    /// </summary>
    private void DrawSetting()
    {
        EditorGUILayout.BeginHorizontal();
        m_Namespace.stringValue = EditorGUILayout.TextField(new GUIContent("命名空间："), m_Namespace.stringValue);
        if (GUILayout.Button("默认设置"))
        {
            m_Namespace.stringValue = m_Setting.Namespace;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_ClassName.stringValue = EditorGUILayout.TextField(new GUIContent("类名："), m_ClassName.stringValue);
        if (GUILayout.Button("物体名"))
        {
            m_ClassName.stringValue = m_Target.gameObject.name;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_ViewModelName.stringValue =
            EditorGUILayout.TextField(new GUIContent("ViewModel类名："), m_ViewModelName.stringValue);
        if (GUILayout.Button("默认设置"))
        {
            m_ViewModelName.stringValue = m_ClassName.stringValue + "ViewModel";
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("组件代码保存路径：");
        EditorGUILayout.LabelField(m_ComCodePath.stringValue);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择组件代码路径"))
        {
            string folder = Path.Combine(Application.dataPath, m_ComCodePath.stringValue);
            if (!Directory.Exists(folder))
            {
                folder = Application.dataPath;
            }

            string path = EditorUtility.OpenFolderPanel("选择组件代码保存路径", folder, "");
            if (!string.IsNullOrEmpty(path))
            {
                m_ComCodePath.stringValue = path.Replace(Application.dataPath + "/", "");
            }
        }

        if (GUILayout.Button("默认设置"))
        {
            m_ComCodePath.stringValue = m_Setting.ComCodePath;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("挂载代码保存路径：");
        EditorGUILayout.LabelField(m_MountCodePath.stringValue);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择挂载代码路径"))
        {
            string folder = Path.Combine(Application.dataPath, m_MountCodePath.stringValue);
            if (!Directory.Exists(folder))
            {
                folder = Application.dataPath;
            }

            string path = EditorUtility.OpenFolderPanel("选择挂载代码保存路径", folder, "");
            if (!string.IsNullOrEmpty(path))
            {
                m_MountCodePath.stringValue = path.Replace(Application.dataPath + "/", "");
            }
        }

        if (GUILayout.Button("默认设置"))
        {
            m_MountCodePath.stringValue = m_Setting.MountCodePath;
        }

        EditorGUILayout.EndHorizontal();
    }

    private List<PropertyInfo> GetClassFields(ComponentAutoBindTool autoBindTool, string className)
    {
        List<PropertyInfo> fileInfos = new List<PropertyInfo>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type targetType = assemblies
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Namespace == autoBindTool.Namespace && t.Name == className);

        if (targetType == null)
        {
            Debug.LogError($"找不到类型: {autoBindTool.Namespace}.{className}");
            return fileInfos;
        }

        PropertyInfo[] fields = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (fields.Length > 0)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                //如果字段是有AutoBindAttribute特性的
                // if (field.GetCustomAttribute<AutoBindAttribute>() != null)
                // {
                fileInfos.Add(field);
                // }
            }
        }

        return fileInfos;
    }

    /// <summary>
    /// 绘制键值对数据
    /// </summary>
    private void DrawKvData()
    {
        //绘制key value数据

        int needDeleteIndex = -1;

        EditorGUILayout.BeginVertical();
        SerializedProperty property;

        List<string> filedNames = new List<string>();
        List<PropertyInfo> fields = new List<PropertyInfo>();
        if (m_Target.GenerateCodeHelper is UIFormVMGenerateCodeHelper)
        {
            fields = GetClassFields(m_Target, m_ViewModelName.stringValue);
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                filedNames.Add(field.Name);
            }

            filedNames.Add("Empty");
        }

        for (int i = 0; i < m_BindDatas.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(25));
            property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            property.stringValue = EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
            property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
            property.objectReferenceValue =
                EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(Component), true);

            if (m_Target.GenerateCodeHelper is UIFormVMGenerateCodeHelper)
            {
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("PropertyInfoName");
                int chooseIndex = -1;
                for (int j = 0; j < fields.Count; j++)
                {
                    var field = fields[j];
                    if (field.Name == property.stringValue)
                    {
                        chooseIndex = j;
                    }
                }

                chooseIndex = EditorGUILayout.Popup(chooseIndex, filedNames.ToArray());
                if (chooseIndex >= 0 && chooseIndex < fields.Count)
                {
                    //用下拉框显示字段名 
                    property.stringValue = fields[chooseIndex].Name;
                }
                else
                {
                    property.stringValue = string.Empty;
                }
            }

            // property.stringValue = EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
            if (GUILayout.Button("X"))
            {
                //将元素下标添加进删除list
                needDeleteIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        //删除data
        if (needDeleteIndex != -1)
        {
            m_BindDatas.DeleteArrayElementAtIndex(needDeleteIndex);
            SyncBindComs();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 添加绑定数据
    /// </summary>
    private void AddBindData(string name, Component bindCom, string fieldInfoName)
    {
        for (int i = 0; i < m_BindDatas.arraySize; i++)
        {
            SerializedProperty elementData = m_BindDatas.GetArrayElementAtIndex(i);
            if (elementData.FindPropertyRelative("Name").stringValue == name)
            {
                Debug.LogError($"有重复名字！请检查后重新生成！Name:{name}");
                return;
            }
        }

        int index = m_BindDatas.arraySize;
        m_BindDatas.InsertArrayElementAtIndex(index);
        SerializedProperty element = m_BindDatas.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("Name").stringValue = name;
        element.FindPropertyRelative("BindCom").objectReferenceValue = bindCom;
        element.FindPropertyRelative("PropertyInfoName").stringValue = fieldInfoName;
    }

    /// <summary>
    /// 同步绑定数据
    /// </summary>
    private void SyncBindComs()
    {
        m_BindComs.ClearArray();

        for (int i = 0; i < m_BindDatas.arraySize; i++)
        {
            SerializedProperty property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
            m_BindComs.InsertArrayElementAtIndex(i);
            m_BindComs.GetArrayElementAtIndex(i).objectReferenceValue = property.objectReferenceValue;
        }
    }

    /// <summary>
    /// 获取指定基类在指定程序集中的所有子类名称
    /// </summary>
    private string[] GetTypeNames(Type typeBase, string[] assemblyNames)
    {
        List<string> typeNames = new List<string>();
        foreach (string assemblyName in assemblyNames)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch
            {
                continue;
            }

            if (assembly == null)
            {
                continue;
            }

            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsClass && !type.IsAbstract && typeBase.IsAssignableFrom(type))
                {
                    typeNames.Add(type.FullName);
                }
            }
        }

        typeNames.Sort();
        return typeNames.ToArray();
    }

    /// <summary>
    /// 创建辅助器实例
    /// </summary>
    private object CreateHelperInstance(string helperTypeName, string[] assemblyNames)
    {
        foreach (string assemblyName in assemblyNames)
        {
            Assembly assembly = Assembly.Load(assemblyName);

            object instance = assembly.CreateInstance(helperTypeName);
            if (instance != null)
            {
                return instance;
            }
        }

        return null;
    }

    /// <summary>
    /// 写入第三方引用
    /// </summary>
    /// <param name="streamWriter">写入流</param>
    private void WriteUsing(StreamWriter streamWriter)
    {
        usingSameStr.Clear();
        //根据索引获取
        for (int i = 0; i < m_Target.BindDatas.Count; i++)
        {
            BindData data = m_Target.BindDatas[i];
            if (!string.IsNullOrEmpty(data.BindCom.GetType().Namespace))
            {
                if (usingSameStr.Contains(data.BindCom.GetType().Namespace))
                {
                    continue;
                }

                usingSameStr.Add(data.BindCom.GetType().Namespace);
                streamWriter.WriteLine($"using {data.BindCom.GetType().Namespace};");
            }
        }
    }

    private List<string> usingSameStr = new List<string>();

    /// <summary>
    /// 生成自动绑定代码
    /// </summary>
    private void GenAutoBindCode()
    {
        GameObject go = m_Target.gameObject;

        string className = !string.IsNullOrEmpty(m_Target.ClassName) ? m_Target.ClassName : go.name;
        string codePath = !string.IsNullOrEmpty(m_Target.ComCodePath) ? m_Target.ComCodePath : m_Setting.ComCodePath;
        codePath = Path.Combine(Application.dataPath, codePath);
        codePath = $"{codePath}/{m_Target.GenerateCodeHelper.DirectoryPath}";
        if (!Directory.Exists(codePath))
        {
            Debug.LogError($"{go.name}的代码保存路径{codePath}无效");
            Directory.CreateDirectory(codePath);
        }

        string filePath = $"{codePath}/{className}.BindComponents.cs";
        m_Target.GenerateCodeHelper.CreateBingCode(m_Target, filePath, className);
        ModifyFileFormat(filePath);

        // var classStr = className.Replace("Form", "ViewModel");
        //
        // codePath = $"{codePath}/VM";
        // if (!Directory.Exists(codePath)) {
        //     Debug.LogError($"{go.name}的代码保存路径{codePath}无效");
        //     Directory.CreateDirectory(codePath);
        // }

        // filePath = $"{codePath}/{classStr}.Imp.cs";
        // m_Target.GenerateCodeHelper.CreateViewModelImpCode(m_Target, filePath, classStr);
        // ModifyFileFormat(filePath);
        // using (StreamWriter sw = new StreamWriter(filePath)) {
        //     //sw.WriteLine("using System.Collections;");
        //     //sw.WriteLine("using System.Collections.Generic;");
        //
        //     WriteUsing(sw);
        //     sw.WriteLine("using UnityEngine;");
        //     //sw.WriteLine("using UnityEngine.UI;");
        //
        //     if (!string.IsNullOrEmpty(m_Target.Namespace))
        //         sw.WriteLine($"\nnamespace {m_Target.Namespace}" + "\n{");
        //     else
        //         sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");
        //
        //     //sw.WriteLine($"\t/*\n\t* Introduction：\n\t* Creator：xxxx\n\t* CreationTime：{DateTime.Now}\n\t*/");
        //     sw.WriteLine("\tpublic partial class " + className + "\n\t{");
        //
        //     //组件字段
        //     foreach (BindData data in m_Target.BindDatas) {
        //         sw.WriteLine($"\t\tprivate {data.BindCom.GetType().Name} m_{data.Name};");
        //     }
        //
        //     sw.WriteLine("\n\t\tprivate void GetBindComponents(GameObject go)\n\t\t{");
        //
        //     //获取autoBindTool上的Component
        //     sw.WriteLine($"\t\t\tComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();\n");
        //
        //     //根据索引获取
        //     for (int i = 0; i < m_Target.BindDatas.Count; i++) {
        //         BindData data = m_Target.BindDatas[i];
        //         string filedName = $"m_{data.Name}";
        //         sw.WriteLine($"\t\t\t{filedName} = autoBindTool.GetBindComponent<{data.BindCom.GetType().Name}>({i});");
        //     }
        //
        //     sw.WriteLine("\t\t}\n\t}\n}");
        //     sw.Close();
        // }
        GenAutoBindMountCode(go, className);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"ClassName:{className}代码生成完毕");
        //EditorUtility.DisplayDialog("提示", "代码生成完毕,正在挂载", "OK");
    }

    private void ModifyFileFormat(string filePath)
    {
        string text = "";
        using (StreamReader read = new StreamReader(filePath))
        {
            string oldtext = read.ReadToEnd();
            text = oldtext;
            text = text.Replace("\n", "\r\n");
            text = text.Replace("\r\r\n", "\r\n"); // 防止替换了正常的换行符      
            if (oldtext.Length == text.Length)
            {
                // 如果没有变化就退出
            }
        }

        File.WriteAllText(filePath, text, Encoding.UTF8); //utf-8格式保存，防止乱码
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

    private string GetFileHead()
    {
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

    string strChangeAuthor = "//修改作者:";
    string strChangeTime = "//修改时间:";

    private List<string> ChangeFileHead(List<string> strList)
    {
        for (int i = 0; i < strList.Count; i++)
        {
            if (strList[i].Contains(strChangeAuthor))
            {
                strList[i] = $"{strChangeAuthor}{SystemInfo.deviceName}";
            }

            if (strList[i].Contains(strChangeTime))
            {
                strList[i] = $"{strChangeTime}{System.DateTime.Now:yyyy-MM-dd HH-mm-ss}";
            }
        }

        return strList;
    }

    /// <summary>
    /// 生成自动绑定的挂载代码
    /// </summary>
    private void GenAutoBindMountCode(GameObject go, string className)
    {
        string codePath = !string.IsNullOrEmpty(m_Target.MountCodePath)
            ? m_Target.MountCodePath
            : m_Setting.MountCodePath;
        codePath = Path.Combine(Application.dataPath, codePath);
        if (!Directory.Exists(codePath))
        {
            Debug.LogError($"挂载{go.name}的代码保存路径{codePath}无效");
            return;
        }

        //string folderName = GetClassFolderName(className);
        //codePath = $"{codePath}/{folderName}";
        if (!Directory.Exists(codePath))
        {
            Directory.CreateDirectory(codePath);
        }

        var filePath = m_Target.GenerateCodeHelper.GenAutoBindMountCode(m_Target, className, codePath);

        // string btnStart =
        //     "/*--------------------Auto generate start button listener.Do not modify!--------------------*/";
        // string btnEnd =
        //     "/*--------------------Auto generate end button listener.Do not modify!----------------------*/";
        // string scriptEnd =
        //     "/*--------------------Auto generate footer.Do not add anything below the footer!------------*/";
        // string scriptEndK = "\t}\n}";
        // Dictionary<string, string> clickFuncDict = new Dictionary<string, string>();
        // Dictionary<string, String> inputFuncDict = new Dictionary<string, string>();
        // for (int i = 0; i < m_Target.BindDatas.Count; i++) {
        //     if (m_Target.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper))
        //         clickFuncDict[$"m_{m_Target.BindDatas[i].Name}"] = $"{m_Target.BindDatas[i].Name}Event";
        //     else if (m_target.BindDatas[i].BindCom.GetType() == typeof(InputField))
        //         inputFuncDict[$"m_{m_target.BindDatas[i].Name}"] = $"{m_target.BindDatas[i].Name}EndEditEvent";
        //     //sw.WriteLine($"\t\t\t m_{m_Target.BindDatas[i].Name}.onClick.AddListener({m_Target.BindDatas[i].Name}Event);");
        // }
        // string filePath = $"{codePath}/{className}.cs";
        // //string filePath = $"{codePath}/{className}.txt";
        // if (!File.Exists(filePath)) {
        //     using (StreamWriter sw = new StreamWriter(filePath)) {
        //         sw.WriteLine(GetFileHead());
        //
        //         sw.WriteLine("using Game.Builtin;");
        //         sw.WriteLine("using System.Collections;");
        //         sw.WriteLine("using System.Collections.Generic;");
        //         sw.WriteLine("using UnityEngine;");
        //         sw.WriteLine("using UnityEngine.UI;");
        //
        //         if (!string.IsNullOrEmpty(m_Target.Namespace))
        //             sw.WriteLine($"\nnamespace {m_Target.Namespace}" + "\n{");
        //         else
        //             sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");
        //
        //         sw.WriteLine($"\t/// <summary>\n\t/// Please modify the description.\n\t/// </summary>");
        //         sw.WriteLine("\tpublic partial class " + className + " : UGuiForm\n\t{");
        //
        //         #region OnInit
        //
        //         sw.WriteLine(
        //             "\t\tprotected override void OnInit(object userData) {\n\t\t\t base.OnInit(userData);\n\t\t\t GetBindComponents(gameObject);\n"); //   OnInit
        //         sw.WriteLine(btnStart);
        //         foreach (var clickFunc in clickFuncDict) {
        //             sw.WriteLine($"\t\t\t{clickFunc.Key}.onClick.AddListener({clickFunc.Value});");
        //         }
        //         foreach (var inputFunc in inputFuncDict) {
        //             sw.WriteLine($"\t\t\t{inputFunc.Key}.onEndEdit.AddListener({inputFunc.Value});");
        //         }
        //         sw.WriteLine(btnEnd);
        //         sw.WriteLine("\t\t}\n");
        //
        //         #endregion
        //
        //         #region ButtonEvent
        //
        //         for (int i = 0; i < m_Target.BindDatas.Count; i++) {
        //             if (m_Target.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper)) {
        //                 sw.WriteLine("\t\tprivate void " + m_Target.BindDatas[i].Name + "Event()" + "{}");
        //             }
        //         }
        //
        //         #endregion
        //
        //         #region InputEvent
        //
        //         for (int i = 0; i < m_Target.BindDatas.Count; i++) {
        //             if (m_Target.BindDatas[i].BindCom.GetType() == typeof(InputField)) {
        //                 sw.WriteLine("\t\tprivate void " +
        //                              m_Target.BindDatas[i].Name +
        //                              "EndEditEvent(string arg0)" +
        //                              "{}");
        //             }
        //         }
        //
        //         #endregion
        //
        //         sw.WriteLine(scriptEnd);
        //         sw.WriteLine(scriptEndK);
        //         sw.Close();
        //     }
        // } else {
        //     bool stopWrite = false;
        //     bool writeNewOver = false;
        //     string[] strArr = File.ReadAllLines(filePath);
        //     List<string> strList = new List<string>();
        //     for (int i = 0; i < strArr.Length; i++) {
        //         string str = strArr[i];
        //         if (str.Trim().Equals(scriptEnd)) {
        //             break;
        //         }
        //         if (str.Trim().Equals(btnStart)) {
        //             strList.Add(btnStart);
        //             stopWrite = true;
        //         }
        //
        //         if (!stopWrite) {
        //             strList.Add(str);
        //         } else {
        //             if (!writeNewOver) {
        //                 foreach (var clickFunc in clickFuncDict) {
        //                     strList.Add($"\t\t\t {clickFunc.Key}.onClick.AddListener({clickFunc.Value});");
        //                 }
        //                 foreach (var inputFunc in inputFuncDict) {
        //                     strList.Add($"\t\t\t{inputFunc.Key}.onEndEdit.AddListener({inputFunc.Value});");
        //                 }
        //                 //writeNew
        //                 writeNewOver = true;
        //             }
        //         }
        //         if (str.Trim().Equals(btnEnd)) {
        //             strList.Add(btnEnd);
        //             stopWrite = false;
        //         }
        //     }
        //     foreach (KeyValuePair<string, string> pair in clickFuncDict) {
        //         bool contain = false;
        //         for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
        //             string str = strList[kIndex];
        //
        //             if (str.Contains($"{pair.Value}()")) {
        //                 contain = true;
        //                 break;
        //             }
        //         }
        //         if (!contain) {
        //             strList.Add($"\t\tprivate void {pair.Value}()" + "{}");
        //         }
        //     }
        //
        //     foreach (KeyValuePair<string, string> pair in inputFuncDict) {
        //         bool contain = false;
        //         for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
        //             string str = strList[kIndex];
        //
        //             if (str.Contains($"{pair.Value}(string arg0)")) {
        //                 contain = true;
        //                 break;
        //             }
        //         }
        //         if (!contain) {
        //             strList.Add($"\t\tprivate void {pair.Value}(string arg0)" + "{}");
        //         }
        //     }
        //
        //     strList.Add(scriptEnd);
        //     strList.Add(scriptEndK);
        //     strList = ChangeFileHead(strList);
        //     File.WriteAllLines(filePath, strList.ToArray());
        // }

        ModifyFileFormat(filePath);
    }

    private string GetClassFolderName(string className)
    {
        string folderName = className.Replace("Form", "");
        if (className.Contains("_"))
        {
            string[] args = className.Split('_');
            if (args.Length > 1)
            {
                folderName = args[0];
            }
        }

        return folderName;
    }

    /// <summary>
    /// Get a type with name.
    /// 根据名字获取一个类型
    /// </summary>
    public static Type GetTypeWithName(string typeName)
    {
        Assembly[] assmblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = assmblies.Length - 1; i >= 0; i--)
        {
            if (assmblies[i].GetName().Name != "Game.HotUpdate" &&
                assmblies[i].GetName().Name != "GameLogic") continue;

            Type[] __types = assmblies[i].GetTypes();

            for (int j = __types.Length - 1; j >= 0; j--)
            {
                if (__types[j].Name != typeName) continue;
                return __types[j];
            }
        }

        return null;
    }
}