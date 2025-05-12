using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIFormGenerateCodeHelper : AutoGenerateCodeHelper {
    public override string DirectoryPath => "Form";

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
        string btnStart =
            "/*--------------------Auto generate start button listener.Do not modify!--------------------*/";
        string btnEnd =
            "/*--------------------Auto generate end button listener.Do not modify!----------------------*/";
        string scriptEnd =
            "/*--------------------Auto generate footer.Do not add anything below the footer!------------*/";
        string scriptEndK = "\t}\n}";
        Dictionary<string, string> clickFuncDict = new Dictionary<string, string>();
        Dictionary<string, string> inputFuncDict = new Dictionary<string, string>();
        for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
            if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper))
                clickFuncDict[$"m_{autoBindTool.BindDatas[i].Name}"] = $"{autoBindTool.BindDatas[i].Name}Event";
            else if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(InputField))
                inputFuncDict[$"m_{autoBindTool.BindDatas[i].Name}"] = $"{autoBindTool.BindDatas[i].Name}EndEditEvent";
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
                sw.WriteLine("using UnityEngine;");
                sw.WriteLine("using UnityEngine.UI;");

                if (!string.IsNullOrEmpty(autoBindTool.Namespace))
                    sw.WriteLine($"\nnamespace {autoBindTool.Namespace}" + "\n{");
                else
                    sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");

                sw.WriteLine($"\t/// <summary>\n\t/// Please modify the description.\n\t/// </summary>");
                sw.WriteLine("\tpublic partial class " + className + " : UGuiPanel\n\t{");

                #region OnInit

                sw.WriteLine(
                    "\t\tprotected override void OnInit(object userData) {\n\t\t\t base.OnInit(userData);\n\t\t\t GetBindComponents(gameObject);\n"); //   OnInit
                sw.WriteLine(btnStart);
                foreach (var clickFunc in clickFuncDict) {
                    sw.WriteLine($"\t\t\t{clickFunc.Key}.onClick.AddListener({clickFunc.Value});");
                }
                foreach (var inputFunc in inputFuncDict) {
                    sw.WriteLine($"\t\t\t{inputFunc.Key}.onEndEdit.AddListener({inputFunc.Value});");
                }
                sw.WriteLine(btnEnd);
                sw.WriteLine("\t\t}\n");

                #endregion

                #region ButtonEvent

                for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
                    if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(UIButtonSuper)) {
                        sw.WriteLine("\t\tprivate void " + autoBindTool.BindDatas[i].Name + "Event()" + "{}");
                    }
                }

                #endregion

                #region InputEvent

                for (int i = 0; i < autoBindTool.BindDatas.Count; i++) {
                    if (autoBindTool.BindDatas[i].BindCom.GetType() == typeof(InputField)) {
                        sw.WriteLine("\t\tprivate void " +
                                     autoBindTool.BindDatas[i].Name +
                                     "EndEditEvent(string arg0)" +
                                     "{}");
                    }
                }

                #endregion

                sw.WriteLine(scriptEnd);
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
                        foreach (var clickFunc in clickFuncDict) {
                            strList.Add($"\t\t\t {clickFunc.Key}.onClick.AddListener({clickFunc.Value});");
                        }
                        foreach (var inputFunc in inputFuncDict) {
                            strList.Add($"\t\t\t{inputFunc.Key}.onEndEdit.AddListener({inputFunc.Value});");
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
            foreach (KeyValuePair<string, string> pair in clickFuncDict) {
                bool contain = false;
                for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
                    string str = strList[kIndex];

                    if (str.Contains($"{pair.Value}()")) {
                        contain = true;
                        break;
                    }
                }
                if (!contain) {
                    strList.Add($"\t\tprivate void {pair.Value}()" + "{}");
                }
            }

            foreach (KeyValuePair<string, string> pair in inputFuncDict) {
                bool contain = false;
                for (int kIndex = 0; kIndex < strList.Count; kIndex++) {
                    string str = strList[kIndex];

                    if (str.Contains($"{pair.Value}(string arg0)")) {
                        contain = true;
                        break;
                    }
                }
                if (!contain) {
                    strList.Add($"\t\tprivate void {pair.Value}(string arg0)" + "{}");
                }
            }

            strList.Add(scriptEnd);
            strList.Add(scriptEndK);
            strList = ChangeFileHead(strList);
            File.WriteAllLines(filePath, strList.ToArray());
        }

        return filePath;
    }
}