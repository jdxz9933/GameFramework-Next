using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Loxodon.Framework.Commands;
using Loxodon.Framework.Interactivity;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public class UIFormVMGenerateCodeHelper : AutoGenerateCodeHelper {
    public override string DirectoryPath => "Form";

    private Type GetClassType(ComponentAutoBindTool autoBindTool, string className) {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type targetType = assemblies
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Namespace == autoBindTool.Namespace && t.Name == className);

        if (targetType == null) {
            Debug.LogError($"找不到类型: {autoBindTool.Namespace}.{className}");
        }

        return targetType;
    }

    private List<FieldInfo> GetClassFields(ComponentAutoBindTool autoBindTool, string className) {
        List<FieldInfo> fileInfos = new List<FieldInfo>();
        Type targetType = GetClassType(autoBindTool, className);
        if (targetType == null) {
            Debug.LogError($"找不到类型: {autoBindTool.Namespace}.{className}");
            return fileInfos;
        }

        // FieldInfo[] fields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        // if (fields.Length > 0)
        // {
        //     for (int i = 0; i < fields.Length; i++)
        //     {
        //         var field = fields[i];
        //         //如果字段是有AutoBindAttribute特性的
        //         if (field.GetCustomAttribute<AutoBindAttribute>() != null)
        //         {
        //             fileInfos.Add(field);
        //         }
        //     }
        // }

        return fileInfos;
    }

    public override void CreateViewModelImpCode(ComponentAutoBindTool autoBindTool, string filePath, string className) {
        List<FieldInfo> fileInfos = GetClassFields(autoBindTool, className);
        if (fileInfos.Count > 0) {
            using (StreamWriter sw = new StreamWriter(filePath)) {
                usingSameStr.Clear();
                for (int i = 0; i < fileInfos.Count; i++) {
                    var field = fileInfos[i];
                    var name = field.FieldType.Namespace;
                    if (usingSameStr.Contains(name)) {
                        continue;
                    }

                    usingSameStr.Add(name);
                    sw.WriteLine($"using {name};");
                }

                if (!string.IsNullOrEmpty(autoBindTool.Namespace))
                    sw.WriteLine($"\nnamespace {autoBindTool.Namespace}" + "\n{");
                else
                    sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");

                sw.WriteLine("\tpublic partial class " + className + "\n\t{");
                for (int i = 0; i < fileInfos.Count; i++) {
                    var field = fileInfos[i];
                    CreateViewModelPropertyCode(sw, field);
                }

                sw.WriteLine("\n\t}\n}");
                sw.Close();
            }
        }

        // AutoViewModelCode(autoBindTool, filePath, claseStr);
    }

    public void CreateViewModelPropertyCode(StreamWriter sw, FieldInfo fieldInfo) {
        var fieldType = fieldInfo.FieldType;
        var fieldName = fieldInfo.Name;
        var fieldNameUpper = fieldInfo.GetNiceName();
        //如果是继承ICommand接口的
        if (typeof(ICommand).IsAssignableFrom(fieldType)) {
            sw.WriteLine($"\t\tpublic ICommand {fieldNameUpper} => {fieldName};");
        } else if (fieldType == typeof(IInteractionRequest)) { } else {
            // sw.WriteLine($"\t\tprivate {fieldType} m_{fieldNameUpper};");
            sw.WriteLine($"\t\tpublic {fieldType} {fieldNameUpper}{{");
            sw.WriteLine($"\t\t\tget => {fieldName};");
            sw.WriteLine($"\t\t\tset => Set(ref {fieldName}, value);");
            sw.WriteLine("\t\t}");
        }
    }

    public override void CreateBingCode(ComponentAutoBindTool autoBindTool, string filePath, string className) {
        using (StreamWriter sw = new StreamWriter(filePath)) {
            //sw.WriteLine("using System.Collections;");
            //sw.WriteLine("using System.Collections.Generic;");

            WriteUsing(sw, autoBindTool);
            sw.WriteLine("using UnityEngine;");
            //sw.WriteLine("using UnityEngine.UI;");

            if (!string.IsNullOrEmpty(autoBindTool.Namespace))
                sw.WriteLine($"\nnamespace {autoBindTool.Namespace}" + "\n{");
            else
                sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");

            //sw.WriteLine($"\t/*\n\t* Introduction：\n\t* Creator：xxxx\n\t* CreationTime：{DateTime.Now}\n\t*/");
            sw.WriteLine("\tpublic partial class " + className + "\n\t{");

            //组件字段
            foreach (ComponentAutoBindTool.BindData data in autoBindTool.BindDatas) {
                sw.WriteLine($"\t\tprivate {data.BindCom.GetType().Name} m_{data.Name};");
            }

            sw.WriteLine("\n\t\tprivate void GetBindComponents(GameObject go)\n\t\t{");

            //获取autoBindTool上的Component
            sw.WriteLine($"\t\t\tComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();\n");

            //根据索引获取
            for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
                ComponentAutoBindTool.BindData data = autoBindTool.BindDatas[i];
                string filedName = $"m_{data.Name}";
                sw.WriteLine($"\t\t\t{filedName} = autoBindTool.GetBindComponent<{data.BindCom.GetType().Name}>({i});");
            }

            sw.WriteLine("\t\t}\n\t}\n}");
            sw.Close();
        }
    }

    public override string GenAutoBindMountCode(ComponentAutoBindTool autoBindTool, string className, string codePath) {
        // var VMClass = className.Replace("Form", "ViewModel");
        var VMClass = autoBindTool.ViewModelName;
        var vmType = GetClassType(autoBindTool, VMClass);

        string btnStart =
            "/*--------------------Auto generate start button listener.Do not modify!--------------------*/";
        string btnEnd =
            "/*--------------------Auto generate end button listener.Do not modify!----------------------*/";
        string scriptEnd =
            "/*--------------------Auto generate footer.Do not add anything below the footer!------------*/";
        string scriptEndK = "\t}\n}";
        Dictionary<string, string> clickFuncDict = new Dictionary<string, string>();
        Dictionary<string, string> inputFuncDict = new Dictionary<string, string>();

        Dictionary<string, ComponentAutoBindTool.BindData> bindDataDict =
            new Dictionary<string, ComponentAutoBindTool.BindData>();

        for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
            if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper))
                clickFuncDict[$"m_{autoBindTool.BindDatas[i].Name}"] = $"{autoBindTool.BindDatas[i].Name}Event";
            else if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(InputField))
                inputFuncDict[$"m_{autoBindTool.BindDatas[i].Name}"] = $"{autoBindTool.BindDatas[i].Name}EndEditEvent";

            var bindData = autoBindTool.BindDatas[i];
            if (!string.IsNullOrEmpty(bindData.PropertyInfoName)) {
                bindDataDict.Add(bindData.Name, bindData);
            }

            //sw.WriteLine($"\t\t\t m_{m_Target.BindDatas[i].Name}.onClick.AddListener({m_Target.BindDatas[i].Name}Event);");
        }

        string filePath = $"{codePath}/{className}.cs";
        //string filePath = $"{codePath}/{className}.txt";
        if (!File.Exists(filePath)) {
            using (StreamWriter sw = new StreamWriter(filePath)) {
                sw.WriteLine(GetFileHead());

                sw.WriteLine("using Game.Builtin;");
                sw.WriteLine("using System.Collections;");
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using Loxodon.Framework.Binding;");
                sw.WriteLine("using UnityEngine;");
                sw.WriteLine("using UnityEngine.UI;");

                if (!string.IsNullOrEmpty(autoBindTool.Namespace))
                    sw.WriteLine($"\nnamespace {autoBindTool.Namespace}" + "\n{");
                else
                    sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");

                sw.WriteLine($"\t/// <summary>\n\t/// Please modify the description.\n\t/// </summary>");
                sw.WriteLine("\tpublic partial class " + className + " : Window\n\t{");

                sw.WriteLine($"        private {autoBindTool.ViewModelName} m_ViewModel;");
                #region OnCreate

                sw.WriteLine(
                    "\t\tprotected override void OnCreate(IBundle bundle) {\n\t\t\t GetBindComponents(gameObject);\n"); //   OnCreate
                
                sw.WriteLine($"            m_ViewModel = new {autoBindTool.ViewModelName}();");
                sw.WriteLine($"\t\t\tvar bindingSet = this.CreateBindingSet(m_ViewModel);");
                sw.WriteLine($"\t\t\tbindingSet.Bind().For(v => v.OnCloseRequest).To(vm => vm.CloseRequest);");
                sw.WriteLine(btnStart);
                if (vmType != null)
                    foreach (var bindData in bindDataDict) {
                        var code = ConvertBindData(bindData.Value, vmType);
                        if (!string.IsNullOrEmpty(code)) {
                            sw.WriteLine(code);
                        }
                    }

                sw.WriteLine(btnEnd);
                sw.WriteLine("\t\t\tbindingSet.Build();");
                sw.WriteLine("\t\t}\n");

                #endregion

                #region OnBindingSet

                // sw.WriteLine(
                //     "\t\tprotected override void OnBindingSet(object userData) {\n\t\t\t base.OnBindingSet(userData);\n\t\t\t "); //   OnBindingSet
                // var viewModelClass = className.Replace("Form", "ViewModel");
                //
                // var viewModelClassName = autoBindTool.ViewModelName;
                //
                // sw.WriteLine($"\t\t\tvar viewModel = userData as {viewModelClassName};");
                // sw.WriteLine($"\t\t\tif (viewModel == null) return;");
                // sw.WriteLine($"\t\t\tvar bindingSet = this.CreateBindingSet(viewModel);");
                // sw.WriteLine($"\t\t\tbindingSet.Bind().For(v => v.OnCloseRequest).To(vm => vm.CloseRequest);");
                // sw.WriteLine(btnStart);
                // if (vmType != null)
                //     foreach (var bindData in bindDataDict) {
                //         var code = ConvertBindData(bindData.Value, vmType);
                //         if (!string.IsNullOrEmpty(code)) {
                //             sw.WriteLine(code);
                //         }
                //     }
                //
                // sw.WriteLine(btnEnd);
                // sw.WriteLine("\t\t\tbindingSet.Build();");
                // sw.WriteLine("\t\t}\n");

                #endregion

                // #region ButtonEvent
                //
                // for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
                //     if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper)) {
                //         sw.WriteLine("\t\tprivate void " + autoBindTool.BindDatas[i].Name + "Event()" + "{}");
                //     }
                // }
                //
                // #endregion
                //
                // #region InputEvent
                //
                // for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
                //     if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(InputField)) {
                //         sw.WriteLine("\t\tprivate void " +
                //                      autoBindTool.BindDatas[i].Name +
                //                      "EndEditEvent(string arg0)" +
                //                      "{}");
                //     }
                // }
                //
                // #endregion
                //
                // sw.WriteLine(scriptEnd);
                sw.WriteLine(scriptEndK);
                sw.Close();
            }
        } else {
            bool stopWrite = false;
            bool writeNewOver = false;
            string[] strArr = File.ReadAllLines(filePath);
            List<string> strList = new List<string>();
            for (int i = 0; i < strArr.Length; i++) {
                string str = strArr[i];
                if (str.Trim().Equals(scriptEnd)) {
                    break;
                }

                if (str.Trim().Equals(btnStart)) {
                    strList.Add(btnStart);
                    stopWrite = true;
                }

                if (!stopWrite) {
                    strList.Add(str);
                } else {
                    if (!writeNewOver) {
                        // foreach (var clickFunc in clickFuncDict) {
                        //     strList.Add($"\t\t\t {clickFunc.Key}.onClick.AddListener({clickFunc.Value});");
                        // }
                        // foreach (var inputFunc in inputFuncDict) {
                        //     strList.Add($"\t\t\t{inputFunc.Key}.onEndEdit.AddListener({inputFunc.Value});");
                        // }
                        if (vmType != null) {
                            foreach (var bindData in bindDataDict) {
                                var code = ConvertBindData(bindData.Value, vmType);
                                if (!string.IsNullOrEmpty(code)) {
                                    strList.Add(code);
                                }
                            }
                        }

                        //writeNew
                        writeNewOver = true;
                    }
                }

                if (str.Trim().Equals(btnEnd)) {
                    strList.Add(btnEnd);
                    stopWrite = false;
                }
            }
            // foreach (KeyValuePair<string, string> pair in clickFuncDict) {
            //     bool contain = false;
            //     for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
            //         string str = strList[kIndex];
            //
            //         if (str.Contains($"{pair.Value}()")) {
            //             contain = true;
            //             break;
            //         }
            //     }
            //     if (!contain) {
            //         strList.Add($"\t\tprivate void {pair.Value}()" + "{}");
            //     }
            // }

            // foreach (KeyValuePair<string, string> pair in inputFuncDict) {
            //     bool contain = false;
            //     for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
            //         string str = strList[kIndex];
            //
            //         if (str.Contains($"{pair.Value}(string arg0)")) {
            //             contain = true;
            //             break;
            //         }
            //     }
            //     if (!contain) {
            //         strList.Add($"\t\tprivate void {pair.Value}(string arg0)" + "{}");
            //     }
            // }

            //strList.Add(scriptEnd);
            //strList.Add(scriptEndK);
            strList = ChangeFileHead(strList);
            File.WriteAllLines(filePath, strList.ToArray());
        }

        return filePath;
    }

    public string ConvertBindData(ComponentAutoBindTool.BindData bindData, Type viewModelType) {
        var fieldInfo = viewModelType.GetProperty(bindData.PropertyInfoName, BindingFlags.Public | BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogError("ViewModel中找不到字段 " + bindData.PropertyInfoName);
            return "";
        }

        switch (bindData.BindCom.GetType()) {
            case { } type when type == typeof(UIButtonSuper):
                if (typeof(ICommand).IsAssignableFrom(fieldInfo.PropertyType))
                    return
                        $"\t\t\tbindingSet.Bind(m_{bindData.Name}).For(v => v.onClick).To(vm => vm.{fieldInfo.Name}).OneWay();";
                if (fieldInfo.PropertyType == typeof(bool))
                    return
                        $"\t\t\tbindingSet.Bind(m_{bindData.Name}).For(v => v.isOn).To(vm => vm.{fieldInfo.Name}).OneWay();";
                break;
            // case { } type when type == typeof(AdvancedInputField):
            //     if (fieldInfo.FieldType == typeof(string)) {
            //         return $"\t\t\tbindingSet.Bind(m_{bindData.Name}).For(v => v.Text, v => v.OnValueChanged).To(vm => vm.{fieldInfo.GetNiceName()}).TwoWay();";
            //     }
            //     break;
            case { } type when type == typeof(Text):
                if (fieldInfo.PropertyType == typeof(string)) {
                    return
                        $"\t\t\tbindingSet.Bind(m_{bindData.Name}).For(v => v.text).To(vm => vm.{fieldInfo.Name}).TwoWay();";
                }

                break;
            // case { } type1 when type1 == typeof(UXText):
            //     if (fieldInfo.PropertyType == typeof(string)) {
            //         return
            //             $"\t\t\tbindingSet.Bind(m_{bindData.Name}).For(v => v.Text).To(vm => vm.{fieldInfo.Name}).TwoWay();";
            //     }
            //     break;
            case { } type when type == typeof(RectTransform):
                if (fieldInfo.PropertyType == typeof(bool)) {
                    return
                        $"\t\t\tbindingSet.Bind(m_{bindData.Name}.gameObject).For(v => v.activeSelf).To(vm => vm.{fieldInfo.Name}).OneWay();";
                }

                break;
        }

        Debug.LogError("请检查ViewModel中的字段类型是否正确 " + bindData.PropertyInfoName);
        return "";
    }

    private void BindingSet(ComponentAutoBindTool autoBindTool, FieldInfo fieldInfo, List<string> strList) {
        // var autoAttribute = fieldInfo.GetAttribute<AutoBindAttribute>();
    }

    public string ToUpperFormat(string input) {
        if (string.IsNullOrEmpty(input)) {
            return input;
        }

        //检查第一个字符是否为下划线
        if (input[0] == '_') {
            //将第一个字符转换为大写字母
            input = char.ToUpper(input[1]) + input.Substring(2);
        }

        // 检查第一个字符是否为小写字母
        if (char.IsLower(input[0])) {
            // 将第一个字符转换为大写字母
            input = char.ToUpper(input[0]) + input.Substring(1);
        }

        return input;
    }
}