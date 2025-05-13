using System;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game.HotUpdate {
    public class Texture2DBuffer : IReference, IDisposable {
        public Texture2D Texture2D;

        public Texture2D CreateTexture2D(int width, int height, TextureFormat format) {
            if (Texture2D != null) {
                Log.Error("Texture2D is not null");
                Texture2D.Reinitialize(width, height, format, false);
                Texture2D.wrapMode = TextureWrapMode.Clamp;
                return Texture2D;
            }
            Texture2D = new Texture2D(width, height, format, false, false);
            Texture2D.wrapMode = TextureWrapMode.Clamp;
            return Texture2D;
        }

        public void Clear() {
        }

        public void Dispose() {
            // UnityEngine.Object.Destroy(Texture2D);
            // Texture2D = null;
            ReferencePool.Release(this);
        }

        public static Texture2DBuffer Create() {
            return ReferencePool.Acquire<Texture2DBuffer>();
        }
    }
}