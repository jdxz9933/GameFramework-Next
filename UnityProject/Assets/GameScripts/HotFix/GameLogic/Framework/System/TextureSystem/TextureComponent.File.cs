using System;
using System.Collections;
using System.Collections.Generic;
using BestHTTP;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;
using System.IO;

namespace GameLogic {
    public partial class TextureSystem {
        private const string TextureFileSystemName = "TextureFileSystem";

        private string GetFilePath(string file) {
            var filePath = Path.Combine(Application.persistentDataPath, TextureFileSystemName,
                Path.GetFileNameWithoutExtension(file));
            return filePath;
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="file">保存路径</param>
        /// <param name="texture">图片</param>
        /// <returns></returns>
        public void SaveTexture(string file, Texture2D texture) {
            var imgType = TextureExt.GetImageType(file);
            byte[] bytes = texture.GetBytes(imgType);
            SaveTexture(file, bytes);
            if (m_TexturePool.CanSpawn(file)) {
                var obj = m_TexturePool.Spawn(file);
                obj.Replace(texture);
            }
        }

        /// <summary>
        /// 删除图片(注意！！！！）删除了由于缓存图片还在，所以如果引用需要修改就需要改名
        /// </summary>
        /// <param name="file"></param>
        public void RemoveTexture(string file) {
            var filePath = GetFilePath(file);
            SaveHelper.DeleteFile(filePath);
        }

        /// <summary>检查是否存在指定文件。</summary>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        public bool HasFile(string file) {
            if (string.IsNullOrEmpty(file)) {
                Log.Debug("File is Null or Empty");
                return false;
            }
            return SaveHelper.FileExists(GetFilePath(file));
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="file">保存路径</param>
        /// <param name="texture">图片byte数组</param>
        /// <returns></returns>
        private void SaveTexture(string file, byte[] texture) {
            var filePath = GetFilePath(file);
            if (HasFile(file))
                RemoveTexture(file);
            SaveHelper.SaveBytes(texture, filePath);
        }

        // public TextureObject LoadTextureObject(string file) {
        //     if (TrySpawn(file, out var textureObject)) {
        //         return textureObject;
        //     }
        //     var texture = LoadTexture(file);
        //     if (texture == null) {
        //         return null;
        //     }
        //     textureObject = TextureObject.Create(file, texture, TextureLoad.FromFileSystem);
        //     m_TexturePool.Register(textureObject, true);
        //     return textureObject;
        // }

        private async UniTask<TextureObject> LoadTextureObjectAsync(string file) {
            var bytes = LoadTextureBytes(file);
            if (bytes == null) {
                return null;
            }
            var imgType = TextureExt.GetImageType(file);
            var texture = await TextureExt.CreateImageAsync(bytes, TextureFormat.RGBA32, imgType);
            var textureObject = TextureObject.Create(file, texture, TextureLoad.FromFileSystem);
            m_TexturePool.Register(textureObject, true);
            return textureObject;
        }

        public Texture2D LoadTexture(string file, bool makeNoLongerReadable = true) {
            var bytes = LoadTextureBytes(file);
            if (bytes == null) {
                return null;
            }
            var imgType = TextureExt.GetImageType(file);
            var texture = TextureExt.CreateImage(bytes, TextureFormat.RGBA32, imgType, makeNoLongerReadable);
            return texture;
        }

        public byte[] LoadTextureBytes(string file) {
            var filePath = GetFilePath(file);
            if (!HasFile(file)) {
                Log.Debug("File is not Exist");
                return null;
            }
            return SaveHelper.LoadRawBytes(filePath);
        }

        public async UniTask SetTextureByFileAsync(WebWaitImage image, string file) {
            if (m_TexturePool.CanSpawn(file)) {
                var obj = m_TexturePool.Spawn(file);
                image.SetTextureObject(obj);
                image.OnLoadSucceed(file);
                image.OnLoadComplete();
                return;
            }
            var textureObject = await LoadTextureObjectAsync(file);
            if (textureObject == null) {
                image.OnLoadFailed(file);
                image.OnLoadComplete();
                return;
            }
            image.SetTextureObject(textureObject);
            image.OnLoadSucceed(file);
            image.OnLoadComplete();
        }
    }
}