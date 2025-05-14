using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using BestHTTP;
using BestHTTP.WebSocket;
using Cysharp.Threading.Tasks;
using GameFramework;
using Google.Protobuf;
using Loxodon.Framework.Messaging;
using Protobuf.Social.V1;
using GameBase;
using Log = UnityGameFramework.Runtime.Log;
using GameProto;
using Protobuf.Common.V1;

namespace GameLogic {
    public class NetSystem : BaseLogicSys<NetSystem> {
        private WebSocket webSocket;

        private Uri url;

        private const int MaxRetryCount = 5;

        private int retryCount = 0;

        private ConcurrentQueue<IMessage> messageQueue;

        private ConcurrentDictionary<Type, TaskResult<IMessage>> taskResultMap;

        private Messenger messenger;

        private long uid;

        public long Uid => uid;

        public override bool OnInit() {
            messageQueue = new ConcurrentQueue<IMessage>();
            taskResultMap = new ConcurrentDictionary<Type, TaskResult<IMessage>>();
            return base.OnInit();
        }

        public void Init(string url) {
            messenger = Messenger.Default;
            this.url = new Uri(url);
            SpeedTest().Forget();
        }

        private async UniTask SpeedTest() {
            PingHelper[] pingHelpers = new[] {
                new PingHelper("yoyatime-server.yoyaworld.com"),
                new PingHelper("www.google.com"),
                new PingHelper("www.baidu.com")
            };

            UniTask<PingHelper>[] tasks = new UniTask<PingHelper>[pingHelpers.Length];
            for (int i = 0; i < pingHelpers.Length; i++) {
                tasks[i] = pingHelpers[i].RunRepeat(MaxRetryCount);
            }
            var task = await UniTask.WhenAny(tasks);
            if (task.result != null) {
                Log.Warning("PingHelper.RunRepeat 成功 host:" + task.result.Host);
                for (int i = 0; i < pingHelpers.Length; i++) {
                    var helper = pingHelpers[i];
                    helper.Cancel();
                }
            }

            // var pingHelper = new PingHelper("yoyatime-server.yoyaworld.com");
            // var helper = await pingHelper.RunRepeat(MaxRetryCount);
            // if (helper != null) {
            //     Log.Warning("PingHelper.RunRepeat 成功 host:" + helper.Host);
            // }
        }

        private bool isConnecting = false;

        //测速
        private float lastPingTime = 0;
        private float pingInterval = 5f;
        private float pingTimeout = 3f;

        public void Connect() {
            if (isConnecting) {
                return;
            }
            isConnecting = true;

            webSocket = new WebSocket(url);
            // this.webSocket.StartPingThread = true;
// #if !UNITY_WEBGL || UNITY_EDITOR
//             this.webSocket.StartPingThread = true;
// #endif
            this.webSocket.OnOpen += OnOpen;
            this.webSocket.OnError += OnError;
            this.webSocket.OnClosed += OnClosed;
            this.webSocket.OnMessage += OnMessage;
            this.webSocket.OnBinary += OnBinary;
            this.webSocket.Open();
        }

        public override void OnUpdate() {
            if (Input.GetKeyDown(KeyCode.A)) {
                Log.Warning("Send: {0}", webSocket.Latency);
            }

            if (messageQueue.Count <= 0) return;
            while (messageQueue.TryDequeue(out var message)) {
                var type = message.GetType();
                if (taskResultMap.TryRemove(type, out var taskResult)) {
                    taskResult.SetResult(message);
                    ReferencePool.Release(taskResult);
                } else {
                    messenger?.Publish(message);
                }
            }
        }

        public void Close() {
            if (webSocket != null) {
                webSocket.Close();
                webSocket = null;
            }
        }

        public void Send(byte[] data) {
            if (webSocket != null && webSocket.State == WebSocketStates.Open) {
                webSocket.Send(data);
            }
        }

        public void Send(string message) {
            if (webSocket != null && webSocket.State == WebSocketStates.Open) {
                webSocket.Send(message);
            }
        }

        public void Send(IMessage message) {
            if (webSocket != null && webSocket.State == WebSocketStates.Open) {
                var data = ProtobufHelper.ToBytes(message);
                var type = message.GetType();
                var code = ProtoMap.GetCode(type);
                if (code == -1) {
                    Log.Error("ProtoMap.GetCode(type) == -1");
                    return;
                }

                Log.Warning("Send: {0} {1}", message.GetType(), message.ToString());

                MessagePack messagePack = new MessagePack {
                    Cmd = code,
                    Body = ByteString.CopyFrom(data)
                };
                var sendData = ProtobufHelper.ToBytes(messagePack);
                Send(sendData);
            }
        }

        public async UniTask<K> SendWaitResponse<T, K>(T message, float timeout = 5f) where T : IMessage where K : class, IMessage {
            if (webSocket != null && webSocket.State == WebSocketStates.Open) {
                //设置等待进程
                var taskResult = TaskResult<IMessage>.Create(this);
                if (taskResultMap.TryAdd(typeof(K), taskResult)) {
                    Send(message);
                    var resMessage = await taskResult.Task;
                    return resMessage as K;
                }
            }
            return null;
        }

        private void OnMessage(WebSocket websocket, string message) {
            Log.Warning("WebSocket message: " + message);
            //message 转成 bytes
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            MessagePack messagePack = ProtobufHelper.FromBytes<MessagePack>(data);
            var cmd = messagePack.Cmd;
            var body = messagePack.Body.ToByteArray();
            var type = ProtoMap.GetType(cmd);
            if (type == null) {
                Log.Error("ProtoMap.GetType(cmd) == null");
                return;
            }
            var obj = ProtobufHelper.FromBytes(body, type);
            messageQueue.Enqueue((IMessage)obj);
        }

        private void OnBinary(WebSocket websocket, byte[] data) {
            string hex = BitConverter.ToString(data);
            // Log.Warning("WebSocket binary: " + hex);
            MessagePack messagePack = ProtobufHelper.FromBytes<MessagePack>(data);
            var cmd = messagePack.Cmd;
            var body = messagePack.Body.ToByteArray();
            var type = ProtoMap.GetType(cmd);
            if (type == null) {
                Log.Error("ProtoMap.GetType(code) == null");
                return;
            }
            var message = ProtobufHelper.FromBytes(body, type);

            Log.Warning("receive:{0}  {1}", message.GetType(), message.ToString());
            messageQueue.Enqueue((IMessage)message);
        }

        private void OnClosed(WebSocket websocket, ushort code, string message) {
            Log.Warning("WebSocket closed: " + message);
        }

        private void OnError(WebSocket websocket, string reason) {
            Log.Error(reason);
            isConnecting = false;
            Close();
            ReRetryConnect();
        }

        private bool isReRetryConnect = false;

        private void ReRetryConnect() {
            retryCount++;
            if (retryCount > MaxRetryCount) {
                Log.Error("WebSocket retry count exceeded");
                return;
            }
            Log.Warning("WebSocket retrying connection: " + retryCount);
            var delay = 2 + Mathf.Pow(2, retryCount);
            isReRetryConnect = true;
            // Utility.InvokeAsync(Connect, delay).Forget();
        }

        private void OnOpen(WebSocket websocket) {
            Log.Warning("WebSocket connected");

            isConnecting = false;
            DoConnect().Forget();
            retryCount = 0;
        }

        private async UniTask DoConnect() {
            await Login();
            if (isReRetryConnect) {
                messenger?.Publish(new ReConnectMessage(this));
            }
            isReRetryConnect = false;
        }

        private async UniTask Login() {
            Login2S infoPbResV2 = new Login2S();
            // infoPbResV2.Guid = GFX.Server.Guid;
            var respones = await SendWaitResponse<Login2S, Login2C>(infoPbResV2);
            if (respones != null) {
                Log.Warning("WebSocket message: " + respones.Uid);
                uid = respones.Uid;
            }
        }
    }
}