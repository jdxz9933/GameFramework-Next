using System;
using System.Collections;
using System.Collections.Generic;
using BestHTTP;
using UnityEngine;
using UnityGameFramework.Runtime;
using System.IO;
using GameBase;
using GameFramework.ObjectPool;

namespace GameLogic {
    public partial class TextureSystem : BaseLogicSys<TextureSystem> {
        private const int maxCachedDay = 30;

        private const ulong maxCachedSize = ulong.MaxValue;

        private const int MaxConnectionPerServer = 10;

        /// <summary>
        /// 对象池自动释放时间间隔
        /// </summary>
        [SerializeField] private float m_AutoReleaseInterval = 60f;

        /// <summary>
        /// 散图集合对象池
        /// </summary>
        private IObjectPool<TextureObject> m_TexturePool;

        private void Start() {
            HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.Error;
            HTTPManager.MaxConnectionPerServer = MaxConnectionPerServer;
#if !BESTHTTP_DISABLE_CACHING
            // Remove too old cache entries.
            BestHTTP.Caching.HTTPCacheService.BeginMaintainence(
                new BestHTTP.Caching.HTTPCacheMaintananceParams(TimeSpan.FromDays(maxCachedDay), maxCachedSize));
#endif
            m_TexturePool = GameModule.ObjectPool.CreateMultiSpawnObjectPool<TextureObject>("TextureItemObject", m_AutoReleaseInterval, 16, 60, 0);
            var rootPath = Path.Combine(Application.persistentDataPath, TextureFileSystemName);
            if (!Directory.Exists(rootPath)) {
                Directory.CreateDirectory(rootPath);
            }
        }

        public bool TrySpawn(string file, out TextureObject textureObject) {
            if (m_TexturePool.CanSpawn(file)) {
                textureObject = m_TexturePool.Spawn(file);
                return true;
            }
            textureObject = null;
            return false;
        }

        public void Unspawn(TextureObject textureObject) {
            m_TexturePool.Unspawn(textureObject);
        }
    }
}