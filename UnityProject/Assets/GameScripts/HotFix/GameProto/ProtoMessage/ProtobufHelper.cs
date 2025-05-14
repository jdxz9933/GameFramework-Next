using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Google.Protobuf;
using UnityEngine;

namespace GameProto {
    public static class ProtobufHelper {
        public static void ToStream<T>(T message, System.IO.Stream stream) where T : IMessage {
            message.WriteTo(stream);
        }

        public static byte[] ToBytes<T>(T message) where T : IMessage {
            return message.ToByteArray();
        }

        public static object FromBytes(byte[] bytes, System.Type type) {
            var message = (Google.Protobuf.IMessage)System.Activator.CreateInstance(type);
            message.MergeFrom(bytes);
            return message;
        }

        public static T FromBytes<T>(byte[] bytes) where T : Google.Protobuf.IMessage, new() {
            var message = new T();
            message.MergeFrom(bytes);
            return message;
        }

        public static T FromStream<T>(System.IO.Stream stream) where T : Google.Protobuf.IMessage, new() {
            var message = new T();
            message.MergeFrom(stream);
            return message;
        }
    }
}