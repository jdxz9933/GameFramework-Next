using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using unity.libwebp;
using unity.libwebp.Interop;
using UnityEngine;
using WebP;

namespace GameLogic {
    public static class TextureExt {
        public enum ImageType {
            defult,
            jpg,
            png,
            webp
        }

        public static ImageType GetImageType(string file) {
            var lower = file.ToLower();

            if (lower.Contains("?")) {
                var strs = lower.Split("?");

                lower = strs[0];
            }

            //如果后缀名是png，就是透明的
            if (lower.EndsWith(".jpg")) {
                return ImageType.jpg;
            } else if (lower.EndsWith(".webp")) {
                return ImageType.webp;
            } else if (lower.EndsWith(".png")) {
                return ImageType.png;
            }
            return ImageType.defult;
        }

        public static unsafe void LoadRGBAFromWebPNoGC(byte[] lData, ref int lWidth, ref int lHeight, BytesBuffer buffer, bool lMipmaps, out Error lError,
            Texture2DExt.ScalingFunction scalingFunction = null) {
            lError = 0;

            byte[] lRawData = null;
            int lLength = lData.Length;

            fixed (byte* lDataPtr = lData) {
                // If we've been supplied a function to alter the width and height, use that now.
                scalingFunction?.Invoke(ref lWidth, ref lHeight);

                // If mipmaps are requested we need to create 1/3 more memory for the mipmaps to be generated in.
                int numBytesRequired = lWidth * lHeight * 4;
                if (lMipmaps) {
                    numBytesRequired = Mathf.CeilToInt((numBytesRequired * 4.0f) / 3.0f);
                }
                lRawData = buffer.CreateBytes(numBytesRequired);
                // lRawData = new byte[numBytesRequired];
                fixed (byte* lRawDataPtr = lRawData) {
                    int lStride = 4 * lWidth;

                    // As we have to reverse the y order of the data, we pass through a negative stride and 
                    // pass through a pointer to the last line of the data.
                    byte* lTmpDataPtr = lRawDataPtr + (lHeight - 1) * lStride;

                    WebPDecoderConfig config = new WebPDecoderConfig();

                    if (NativeLibwebp.WebPInitDecoderConfig(&config) == 0) {
                        throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
                    }

                    // Set up decode options
                    config.options.use_threads = 1;
                    if (scalingFunction != null) {
                        config.options.use_scaling = 1;
                    }
                    config.options.scaled_width = lWidth;
                    config.options.scaled_height = lHeight;

                    // read the .webp input file information
                    VP8StatusCode result = NativeLibwebp.WebPGetFeatures(lDataPtr, (UIntPtr)lLength, &config.input);
                    if (result != VP8StatusCode.VP8_STATUS_OK) {
                        throw new Exception(string.Format("Failed WebPGetFeatures with error {0}.", result.ToString()));
                    }

                    // specify the output format
                    config.output.colorspace = WEBP_CSP_MODE.MODE_RGBA;
                    config.output.u.RGBA.rgba = lTmpDataPtr;
                    config.output.u.RGBA.stride = -lStride;
                    config.output.u.RGBA.size = (UIntPtr)(lHeight * lStride);
                    config.output.height = lHeight;
                    config.output.width = lWidth;
                    config.output.is_external_memory = 1;

                    // Decode
                    result = NativeLibwebp.WebPDecode(lDataPtr, (UIntPtr)lLength, &config);
                    if (result != VP8StatusCode.VP8_STATUS_OK) {
                        throw new Exception(string.Format("Failed WebPDecode with error {0}.", result.ToString()));
                    }
                }
                lError = Error.Success;
            }
        }

        public static async UniTask<Texture2DBuffer> CreateImageAsync(byte[] bytes, TextureFormat textureFormat, ImageType imageType,
            bool makeNoLongerReadable = true) {
            Texture2D tex = null;

            Texture2DBuffer texture2DBuffer = null;
            switch (imageType) {
                case ImageType.webp:
                    await UniTask.SwitchToTaskPool();
                    int lWidth;
                    int lHeight;
                    Texture2DExt.GetWebPDimensions(bytes, out lWidth, out lHeight);
                    using (BytesBuffer buffer = BytesBuffer.Create()) {
                        LoadRGBAFromWebPNoGC(bytes, ref lWidth, ref lHeight, buffer, false, out Error lError);
                        // byte[] lRawData = Texture2DExt.LoadRGBAFromWebP(bytes, ref lWidth, ref lHeight, false, out Error lError);
                        await UniTask.SwitchToMainThread();
                        if (lError != Error.Success) {
                            Debug.LogError($"Failed to fill Texture2d with webp: {lError}");
                            return null;
                        } else {
                            texture2DBuffer = Texture2DBuffer.Create();
                            tex = texture2DBuffer.CreateTexture2D(lWidth, lHeight, textureFormat);
                            // tex = Texture2DExt.CreateWebpTexture2D(lWidth, lHeight, false, false);
                            tex.LoadRawTextureData(buffer.Data);
                            tex.Apply(false, false);
                        }
                    }
                    break;
                default:
                    texture2DBuffer = Texture2DBuffer.Create();
                    tex = texture2DBuffer.CreateTexture2D(0, 0, textureFormat);
                    // tex = new Texture2D(0, 0, textureFormat, false);
                    tex.LoadImage(bytes);
                    tex.Apply(false, false);
                    break;
            }

            return texture2DBuffer;
        }

        public static Texture2D CreateImage(byte[] bytes, TextureFormat textureFormat, ImageType imageType,
            bool makeNoLongerReadable = true) {
            Texture2D tex = null;
            switch (imageType) {
                case ImageType.webp:
                    tex = Texture2DExt.CreateTexture2DFromWebP(bytes, false, false, out var error,
                        makeNoLongerReadable: makeNoLongerReadable);
                    if (error != Error.Success) {
                        Debug.LogError($"Failed to fill Texture2d with webp: {error}");
                        return null;
                    }
                    break;
                default:
                    tex = new Texture2D(0, 0, textureFormat, false);
                    tex.LoadImage(bytes);
                    tex.Apply(false, makeNoLongerReadable);
                    break;
            }
            return tex;
        }

        public static bool LoadImage(this Texture2D tex, byte[] bytes, ImageType imageType) {
            switch (imageType) {
                case ImageType.webp:
                    tex.LoadWebP(bytes, out var error);
                    if (error != Error.Success) {
                        Debug.LogError($"Failed to fill Texture2d with webp: {error}");
                        return false;
                    }
                    return true;
                default:
                    return tex.LoadImage(bytes);
            }
        }

        public static byte[] GetBytes(this Texture2D texture, ImageType imageType = ImageType.defult) {
            byte[] bytes;
            switch (imageType) {
                case ImageType.jpg:
                    bytes = texture.EncodeToJPG();
                    break;
                case ImageType.png:
                    bytes = texture.EncodeToPNG();
                    break;
                case ImageType.webp: {
                    // Flip updown.
                    // ref: https://github.com/netpyoung/unity.webp/issues/25
                    Color[] pixels = texture.GetPixels();
                    Color[] pixelsFlipped = new Color[pixels.Length];
                    int w = texture.width;
                    int h = texture.height;
                    // for (int y = 0; y < h; y++) {
                    //     Array.Copy(pixels, y * h, pixelsFlipped, (h - y - 1) * w, w);
                    // }
                    //像素上下翻转
                    for (int y = 0; y < h; y++) {
                        Array.Copy(pixels, y * w, pixelsFlipped, (h - y - 1) * w, w);
                    }

                    texture.SetPixels(pixelsFlipped);
                }
                    bytes = texture.EncodeToWebP(80, out var error);
                    if (error != Error.Success) {
                        Debug.LogError("Webp EncodeToWebP Error : " + error.ToString());
                    }
                    break;
                default:
                    bytes = texture.EncodeToPNG();
                    break;
            }

            return bytes;
        }

        public static Sprite ToSprite(Texture2D texture, float pixelDensity = 100f) {
            texture.mipMapBias = -1f;
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                pixelDensity);
        }

        public static Sprite ToSprite(Texture2D texture, Vector2 pivot, float pixelDensity = 100f) {
            texture.mipMapBias = -1f;
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), pivot, pixelDensity);
        }
    }
}