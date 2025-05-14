using System;
using System.Collections;
using System.Collections.Generic;
using BestHTTP;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameLogic {
    public partial class TextureSystem {
        private Dictionary<WebWaitImage, HTTPRequest> m_Requests = new Dictionary<WebWaitImage, HTTPRequest>();

        public async UniTask SetTextureByNetworkAsync(WebWaitImage image, string file) {
            if (m_Requests.ContainsKey(image)) {
                m_Requests[image].Abort();
                m_Requests.Remove(image);
            }

            if (m_TexturePool.CanSpawn(file)) {
                var obj = m_TexturePool.Spawn(file);
                image.SetTextureObject(obj);
                // Texture2D texture = (Texture2D)m_TexturePool.Spawn(file).Target;
                // image.SetTexture(TextureExt.ToSprite(texture));
                image.OnLoadSucceed(file);
                image.OnLoadComplete();
                return;
            }

            var request = new HTTPRequest(new Uri(file), ImageDownloaded);
            request.Uri = new Uri(file);
            request.Tag = image;
            m_Requests.Add(image, request);
            request.Send();
        }

        private void ImageDownloaded(HTTPRequest originalrequest, HTTPResponse response) {
            UnityEngine.Profiling.Profiler.BeginSample("TextureComponent.ImageDownloaded");

            var waitImage = originalrequest.Tag as WebWaitImage;
            if (waitImage == null) {
                return;
            }

            if (m_Requests.ContainsKey(waitImage)) {
                m_Requests.Remove(waitImage);
            }
            switch (originalrequest.State) {
                case HTTPRequestStates.Finished:
                    if (response.IsSuccess) {
                        var bytes = response.Data;

                        var imgType = TextureExt.GetImageType(originalrequest.Uri.AbsoluteUri);
                        LoadTextureSucceed(waitImage, originalrequest.Uri.AbsoluteUri, bytes, imgType, waitImage.IsReadAble).Forget();
                        UnityEngine.Profiling.Profiler.EndSample();
                        return;

                        // var tex = TextureExt.CreateImage(bytes, TextureFormat.RGBA32, imgType, waitImage.IsReadAble);
                        // TextureObject texObj = TextureObject.Create(originalrequest.Uri.AbsoluteUri, tex, TextureLoad.FromNet);
                        // m_TexturePool.Register(texObj, true);
                        // Profiller.LogSample("ImageDownloaded", $"Register url:{originalrequest.Uri.AbsoluteUri} {tex.width}x{tex.height}");
                        // waitImage.SetTextureObject(texObj);
                        // waitImage.OnLoadSucceed(originalrequest.Uri.AbsoluteUri);
                    } else {
                        waitImage.OnLoadFailed(originalrequest.Uri.AbsoluteUri);
                        Log.Warning(string.Format(
                            "{0}\nRequest finished Successfully, but the server sent an error. Status Code: {1}-{2} Message: {3}",
                            originalrequest.Uri.AbsoluteUri,
                            response.StatusCode,
                            response.Message,
                            response.DataAsText));
                    }
                    break;
                case HTTPRequestStates.Error:
                    waitImage.OnLoadFailed(originalrequest.Uri.AbsoluteUri);
                    Log.Error("Request Finished with Error! " +
                              (originalrequest.Exception != null
                                  ? (originalrequest.Exception.Message +
                                     "\n" +
                                     originalrequest.Exception.StackTrace)
                                  : "No Exception"));
                    break;
                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    // waitImage.OnLoadFailed(originalrequest.Uri.AbsoluteUri);
                    Log.Warning("Request Aborted!");
                    break;
                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    waitImage.OnLoadFailed(originalrequest.Uri.AbsoluteUri);
                    Log.Error("Connection Timed Out!");
                    break;
                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    waitImage.OnLoadFailed(originalrequest.Uri.AbsoluteUri);
                    Log.Error("Processing the request Timed Out!");
                    break;
            }
            waitImage.OnLoadComplete();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private async UniTask LoadTextureSucceed(WebWaitImage waitImage, string url, byte[] data, TextureExt.ImageType imageType, bool IsReadAble) {
            var texture = await TextureExt.CreateImageAsync(data, TextureFormat.RGBA32, imageType, IsReadAble);
            TextureObject texObj = TextureObject.Create(url, texture, TextureLoad.FromNet);
            m_TexturePool.Register(texObj, true);
            waitImage.SetTextureObject(texObj);
            waitImage.OnLoadSucceed(url);
            waitImage.OnLoadComplete();
        }
    }
}