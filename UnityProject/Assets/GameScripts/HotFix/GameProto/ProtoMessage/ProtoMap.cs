using System.Collections.Generic;
using System;

namespace GameProto {
    public static class ProtoMap {
        public static Dictionary<int, Type> protoMap = new Dictionary<int, Type> {
            { 100001, typeof(Protobuf.Social.V1.Login2S) },
            { 200001, typeof(Protobuf.Social.V1.Login2C) },
            { 100002, typeof(Protobuf.Social.V1.EnterRoom2S) },
            { 200002, typeof(Protobuf.Social.V1.EnterRoom2C) },
            { 300001, typeof(Protobuf.Social.V1.PlayerLite2B) },
            { 300002, typeof(Protobuf.Social.V1.RoomItem2B) },
            { 100003, typeof(Protobuf.Social.V1.RoomInit2S) },
            { 200003, typeof(Protobuf.Social.V1.RoomInit2C) },
            { 100004, typeof(Protobuf.Social.V1.PlayerMove2S) },
            { 200004, typeof(Protobuf.Social.V1.PlayerMove2C) },
            { 100005, typeof(Protobuf.Social.V1.ExitRoom2S) },
            { 200005, typeof(Protobuf.Social.V1.ExitRoom2C) },
            { 100006, typeof(Protobuf.Social.V1.PlayerAnim2S) },
            { 200006, typeof(Protobuf.Social.V1.PlayerAnim2C) },
            { 100007, typeof(Protobuf.Social.V1.PlayerEmoji2S) },
            { 200007, typeof(Protobuf.Social.V1.PlayerEmoji2C) },
            { 100008, typeof(Protobuf.Social.V1.UseItem2S) },
            { 200008, typeof(Protobuf.Social.V1.UseItem2C) },
            { 100009, typeof(Protobuf.Social.V1.RoomChat2S) },
            { 200009, typeof(Protobuf.Social.V1.RoomChat2C) },
            { 300009, typeof(Protobuf.Social.V1.RoomChat2B) },
            { 100010, typeof(Protobuf.Social.V1.PlayerJoystickMove2S) },
            { 200010, typeof(Protobuf.Social.V1.PlayerJoystickMove2C) },
        };

        public static Dictionary<Type, int> protoMapReverse = new Dictionary<Type, int> {
            { typeof(Protobuf.Social.V1.Login2S), 100001 },
            { typeof(Protobuf.Social.V1.Login2C), 200001 },
            { typeof(Protobuf.Social.V1.EnterRoom2S), 100002 },
            { typeof(Protobuf.Social.V1.EnterRoom2C), 200002 },
            { typeof(Protobuf.Social.V1.PlayerLite2B), 300001 },
            { typeof(Protobuf.Social.V1.RoomItem2B), 300002 },
            { typeof(Protobuf.Social.V1.RoomInit2S), 100003 },
            { typeof(Protobuf.Social.V1.RoomInit2C), 200003 },
            { typeof(Protobuf.Social.V1.PlayerMove2S), 100004 },
            { typeof(Protobuf.Social.V1.PlayerMove2C), 200004 },
            { typeof(Protobuf.Social.V1.ExitRoom2S), 100005 },
            { typeof(Protobuf.Social.V1.ExitRoom2C), 200005 },
            { typeof(Protobuf.Social.V1.PlayerAnim2S), 100006 },
            { typeof(Protobuf.Social.V1.PlayerAnim2C), 200006 },
            { typeof(Protobuf.Social.V1.PlayerEmoji2S), 100007 },
            { typeof(Protobuf.Social.V1.PlayerEmoji2C), 200007 },
            { typeof(Protobuf.Social.V1.UseItem2S), 100008 },
            { typeof(Protobuf.Social.V1.UseItem2C), 200008 },
            { typeof(Protobuf.Social.V1.RoomChat2S), 100009 },
            { typeof(Protobuf.Social.V1.RoomChat2C), 200009 },
            { typeof(Protobuf.Social.V1.RoomChat2B), 300009 },
            { typeof(Protobuf.Social.V1.PlayerJoystickMove2S), 100010 },
            { typeof(Protobuf.Social.V1.PlayerJoystickMove2C), 200010 },
        };

        public static Type GetType(int code) {
            if (protoMap.TryGetValue(code, out var type)) {
                return type;
            }
            return null;
        }

        public static int GetCode(Type type) {
            if (protoMapReverse.TryGetValue(type, out var code)) {
                return code;
            }
            return -1;
        }
    }
}