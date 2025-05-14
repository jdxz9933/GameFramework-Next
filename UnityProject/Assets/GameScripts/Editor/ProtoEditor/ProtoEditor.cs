using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameEditor {
    public class ProtoEditor {
        private const string protoWorkSpaceDir = "protobuf";

        private const string proteDir = "protocol";

        private const string protoOuputDir = "Assets/GameMain/Scripts/HotUpdate/Net/ProtoMessage";

        private const string protoMapFile = "ProtoMap";

        private static Dictionary<int, string> protoMap = new Dictionary<int, string>();

        [MenuItem("Tools/Proto2CS")]
        public static void AllProto2CS() {
            string rootDir = Environment.CurrentDirectory;
            string protoDir = Path.Combine(rootDir, protoWorkSpaceDir);

            string protoc;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                protoc = Path.Combine(rootDir, protoWorkSpaceDir, "protoc.exe");
            } else {
                protoc = Path.Combine(rootDir, protoWorkSpaceDir, "protoc");
            }
            string hotfixMessageCodePath = Path.Combine(rootDir, protoOuputDir);

            var protoDirPath = Path.Combine(protoDir, proteDir);
            DirectoryInfo dir = new DirectoryInfo(protoDirPath);

            ProcessProtoFiles(protoDirPath, hotfixMessageCodePath, protoc);
            Debug.Log("proto2cs succeed!");

            AssetDatabase.Refresh();
        }

        public static void ProcessProtoFiles(string protoDir, string outputDir, string protocPath) {
            if (!Directory.Exists(outputDir)) {
                Directory.CreateDirectory(outputDir);
            } else {
                var files = Directory.GetFiles(outputDir);
                foreach (var file in files) {
                    File.Delete(file);
                }
                var dirs = Directory.GetDirectories(outputDir);
                foreach (var dirPath in dirs) {
                    Directory.Delete(dirPath, true);
                }
            }

            DirectoryInfo dir = new DirectoryInfo(protoDir);
            FileInfo[] protoFiles = dir.GetFiles("*.proto", SearchOption.AllDirectories);
            protoMap.Clear();
            foreach (FileInfo protoFile in protoFiles) {
                string protoFilePath = protoFile.FullName;

                if (protoFilePath.Contains("google"))
                    continue;

                string filePath = protoFile.FullName;
                var fileDirectoryPath = Path.GetDirectoryName(filePath);
                //剔除掉protoDirPath
                fileDirectoryPath = fileDirectoryPath.Replace(protoDir, "");
                var csDir = outputDir + fileDirectoryPath;
                //格式化路径
                csDir = csDir.Replace("\\", "/");
                if (!Directory.Exists(csDir)) {
                    Directory.CreateDirectory(csDir);
                }

                AnalyzeProtoMap(protoFilePath);
                // Debug.Log($"protoFilePath: {protoFilePath}, csDir: {csDir}");
                string arguments = $"--csharp_out=\"{csDir}\" --proto_path=\"{protoDir}\" \"{protoFilePath}\"";

                Run(protocPath, arguments, waitExit: true);
            }
            GenerateProtoMapFile();
        }

        public static Process Run(string exe, string arguments, string workingDirectory = ".", bool waitExit = false) {
            try {
                bool redirectStandardOutput = true;
                bool redirectStandardError = true;
                bool useShellExecute = false;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    redirectStandardOutput = false;
                    redirectStandardError = false;
                    useShellExecute = true;
                }

                if (waitExit) {
                    redirectStandardOutput = true;
                    redirectStandardError = true;
                    useShellExecute = false;
                }

                ProcessStartInfo info = new ProcessStartInfo {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = useShellExecute,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                };

                Process process = Process.Start(info);

                if (waitExit) {
                    process.WaitForExit();
                    if (process.ExitCode != 0) {
                        throw new Exception($"{process.StandardOutput.ReadToEnd()} {process.StandardError.ReadToEnd()}");
                    }
                }

                return process;
            } catch (Exception e) {
                throw new Exception($"dir: {Path.GetFullPath(workingDirectory)}, command: {exe} {arguments}", e);
            }
        }

        private static void AnalyzeProtoMap(string filePath) {
// syntax = "proto3";
// package common;
//
// option go_package = "game_server/api/common/v1";
// option csharp_namespace = "Protobuf.Common.V1";
//
// //[10000000] 消息体
// message MessagePack{
//  int32 code = 1;  //编码
//  bytes body = 2;  //消息体
// }
//
// //[30000000] 测试广播
// message GuangBo2b{
//   string content = 2;  //消息体
//   string ip = 1;  //ip
// }

            var lines = File.ReadAllText(filePath);
            string namespaceName = string.Empty;
            //匹配 option csharp_namespace = "Protobuf.Common.V1";
            var namespaceRegex = new System.Text.RegularExpressions.Regex("option csharp_namespace = \"(.*)\";");
            var match = namespaceRegex.Match(lines);
            if (match.Success) {
                namespaceName = match.Groups[1].Value;
                Debug.Log("Namespace: " + namespaceName);
            } else {
                //匹配 package common;
                var packageRegex = new System.Text.RegularExpressions.Regex("package (.*);");
                match = packageRegex.Match(lines);
                if (match.Success) {
                    namespaceName = match.Groups[1].Value;
                    Debug.Log("Package: " + namespaceName);
                }
            }
            if (string.IsNullOrEmpty(namespaceName))
                return;

            ////[10000000] 消息体
            // message MessagePack{
            //     int32 code = 1;  //编码
            //     bytes body = 2;  //消息体
            // }
            var messageRegex = new System.Text.RegularExpressions.Regex(@"(?:\/\/\s*)?\[(\d+)\].*?\r?\n(?:\/\/\s*)?message\s+(\w+)\s*\{",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            var matches = messageRegex.Matches(lines);
            foreach (System.Text.RegularExpressions.Match match1 in matches) {
                var code = int.Parse(match1.Groups[1].Value);
                var messageName = match1.Groups[2].Value;
                var fullName = namespaceName + "." + messageName;
                Debug.Log("Code: " + code + ", Message: " + fullName);
                if (!protoMap.TryAdd(code, fullName)) {
                    Debug.LogError("Code: " + code + ", Message: " + fullName + " already exists");
                    continue;
                }
            }
        }

        public static void GenerateProtoMapFile() {
            var protoMapFile = Path.Combine(protoOuputDir, "ProtoMap.cs");

            if (!Directory.Exists(Path.GetDirectoryName(protoMapFile))) {
                Directory.CreateDirectory(Path.GetDirectoryName(protoMapFile));
            }

            using (var sw = new StreamWriter(protoMapFile)) {
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using System;");
                sw.WriteLine("namespace GameProto {");
                sw.WriteLine("\n");
                sw.WriteLine("    public static class ProtoMap {");
                sw.WriteLine("        public static Dictionary<int, Type> protoMap = new Dictionary<int, Type> {");

                foreach (var kv in protoMap) {
                    sw.WriteLine("            {" + kv.Key + ", typeof(" + kv.Value + ")},");
                }

                sw.WriteLine("        };");
                sw.WriteLine("        public static Dictionary<Type, int> protoMapReverse = new Dictionary<Type, int> {");

                foreach (var kv in protoMap) {
                    sw.WriteLine("            {typeof(" + kv.Value + "), " + kv.Key + "},");
                }
                sw.WriteLine("        };");
                sw.WriteLine("        public static Type GetType(int code) {");
                sw.WriteLine("            if (protoMap.TryGetValue(code, out var type)) {");
                sw.WriteLine("                return type;");
                sw.WriteLine("            }");
                sw.WriteLine("            return null;");
                sw.WriteLine("        }");
                sw.WriteLine("        public static int GetCode(Type type) {");
                sw.WriteLine("            if (protoMapReverse.TryGetValue(type, out var code)) {");
                sw.WriteLine("                return code;");
                sw.WriteLine("            }");
                sw.WriteLine("            return -1;");
                sw.WriteLine("        }");
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            AssetDatabase.Refresh();
        }
    }
}