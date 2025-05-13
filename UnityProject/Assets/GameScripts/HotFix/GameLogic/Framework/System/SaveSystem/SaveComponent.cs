using System;
using System.Collections.Generic;
using GameBase;
using UnityGameFramework.Runtime;
using UnityEngine;

namespace GameLogic {
    public class SaveComponent : BaseLogicSys<SaveComponent> {
        private static readonly string SavePATH = "SaveDatas/save_data.es3";

        private Dictionary<Type, SaveDataAgent> _saveDataAgents;

        private bool IsDirty;

        private ES3Settings _settings;

        public override bool OnInit() {
            base.OnInit();
            _saveDataAgents = new Dictionary<Type, SaveDataAgent>();
            IsDirty = false;
            ES3Settings.defaultSettings.directory = ES3.Directory.PersistentDataPath;
            ES3Settings.defaultSettings.path = SavePATH;
            _settings = new ES3Settings();
            _settings.directory = ES3.Directory.PersistentDataPath;
            _settings.path = SavePATH;
            _settings.compressionType = ES3.CompressionType.Gzip;
            return true;
        }

        protected string GetSaveFilePath(string fileName) {
            return $"{SaveHelper.SaveRootDir}/{fileName}";
        }

        public T GetData<T>(out bool isHas) where T : SaveDataBase, new() {
            var type = typeof(T);
            if (_saveDataAgents.ContainsKey(type)) {
                isHas = true;
                return (T)_saveDataAgents[type].Data;
            }
            var data = new T();
            SaveDataAgent agent = new SaveDataAgent(data);
            isHas = false;
            _saveDataAgents.Add(type, agent);

            var filePath = GetSaveFilePath(data.GetSavePath());
            if (SaveHelper.FileExists(filePath, _settings)) {
                isHas = true;
                SaveHelper.LoadInto(data.GetSaveKey(), data, filePath, _settings);
            }
            return data;
        }

        public void SaveData<T>(T data) where T : SaveDataBase {
            var type = typeof(T);
            if (_saveDataAgents.ContainsKey(type)) {
                var agent = _saveDataAgents[type];
                agent.Data = data;
                agent.IsDirty = true;
            } else {
                Log.Error("SaveDataAgent not exist {0}", type);
                SaveDataAgent agent = new SaveDataAgent(data);
                agent.IsDirty = true;
                _saveDataAgents.Add(type, agent);
            }
            IsDirty = true;
        }

        public void ClearData<T>() where T : SaveDataBase {
            var type = typeof(T);
            if (_saveDataAgents.ContainsKey(type)) {
                var angent = _saveDataAgents[type];
                var data = angent.Data as SaveDataBase;
                if (data != null)
                    SaveHelper.DeleteFile(GetSaveFilePath(data.GetSavePath()), _settings);
                _saveDataAgents.Remove(type);
            } else {
                Log.Error("SaveDataAgent not exist {0}", type);
            }
        }

        public void ClearAllData() {
            foreach (var saveDataAgent in _saveDataAgents) {
                var data = saveDataAgent.Value.Data as SaveDataBase;
                if (data != null)
                    SaveHelper.DeleteFile(GetSaveFilePath(data.GetSavePath()), _settings);
            }
            _saveDataAgents.Clear();
        }

        public override void OnUpdate() {
            if (IsDirty) {
                SaveAllESData();
                IsDirty = false;
            }
        }

        private void SaveAllESData() {
            foreach (var saveDataAgent in _saveDataAgents) {
                if (saveDataAgent.Value.IsDirty) {
                    var data = saveDataAgent.Value.Data as SaveDataBase;
                    if (data != null)
                        SaveHelper.Save(data.GetSaveKey(), saveDataAgent.Value.Data,
                            GetSaveFilePath(data.GetSavePath()), _settings);
                    else {
                        Log.Error("SaveDataAgent data is null {0}", saveDataAgent.Key);
                    }
                    saveDataAgent.Value.IsDirty = false;
                }
            }
        }

        public byte[] Serialize<T>(T value) {
            return SaveHelper.Serialize(value, _settings);
        }

        public string SerializeToBase64<T>(T value) {
            byte[] bytes = Serialize(value);

            Log.Error(bytes.Length.ToString());
            return Convert.ToBase64String(bytes);
        }

        public T Deserialize<T>(byte[] bytes) {
            return SaveHelper.Deserialize<T>(bytes, _settings);
        }

        public T DeserializeFromBase64<T>(string base64) {
            byte[] bytes = Convert.FromBase64String(base64);
            return Deserialize<T>(bytes);
        }

        public void DeserializeInto<T>(byte[] bytes, T obj) where T : class {
            SaveHelper.DeserializeInto(bytes, obj, _settings);
        }

        public T DeserializeIntoFromBase64<T>(string base64) where T : class {
            byte[] bytes = Convert.FromBase64String(base64);
            return Deserialize<T>(bytes);
        }
    }
}