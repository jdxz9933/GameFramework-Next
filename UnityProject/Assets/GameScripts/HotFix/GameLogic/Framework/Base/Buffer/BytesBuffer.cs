using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameLogic {
    public class BytesBuffer : IReference, IDisposable {
        public byte[] Data;

        public byte[] CreateBytes(int length) {
            if (Data != null) {
                Log.Error("Data is not null");
                return Data;
            }
            Data = ArrayPool<byte>.Shared.Rent(length);
            return Data;
        }

        public void Clear() {
            ArrayPool<byte>.Shared.Return(Data);
            Data = null;
        }

        public static BytesBuffer Create() {
            return ReferencePool.Acquire<BytesBuffer>();
        }

        public void Dispose() {
            ReferencePool.Release(this);
        }
    }
}