using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game.HotUpdate {
    public class TextureObject : ObjectBase {
        private TextureLoad m_TextureLoad;
        private ResourceComponent m_ResourceComponent;

        // public static TextureObject Create(string collectionPath, UnityEngine.Texture target, TextureLoad textureLoad,
        //     ResourceComponent resourceComponent = null) {
        //     TextureObject item = ReferencePool.Acquire<TextureObject>();
        //     item.Initialize(collectionPath, target);
        //     item.m_TextureLoad = textureLoad;
        //     item.m_ResourceComponent = resourceComponent;
        //     return item;
        // }

        public void Replace(Texture2D texture2D) {
            Texture2DBuffer texture2DBuffer = Target as Texture2DBuffer;
            GameObject.Destroy(texture2DBuffer.Texture2D);
            texture2DBuffer.Texture2D = null;
            texture2DBuffer.Texture2D = texture2D;
        }

        public static TextureObject Create(string collectionPath, Texture2DBuffer target, TextureLoad textureLoad) {
            TextureObject item = ReferencePool.Acquire<TextureObject>();
            item.Initialize(collectionPath, target);
            item.m_TextureLoad = textureLoad;
            return item;
        }

        protected override void Release(bool isShutdown) {
            Texture2DBuffer buffer = (Texture2DBuffer)Target;
            if (buffer == null) {
                return;
            }
            switch (m_TextureLoad) {
                case TextureLoad.FromResource:
                    m_ResourceComponent.UnloadAsset(buffer.Texture2D);
                    m_ResourceComponent = null;
                    buffer.Dispose();
                    break;
                case TextureLoad.FromNet:
                case TextureLoad.FromFileSystem:
                   
                    buffer.Dispose();
                    break;
            }
        }
    }
}