using System.Threading;
using Cysharp.Threading.Tasks;
// using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
// using UnityGameFramework.Extension;

namespace GameLogic {
    public class WebWaitImage : MonoBehaviour {
        [SerializeField] protected GameObject m_WaitObj;
        [SerializeField] private Image m_RawImage;

        [SerializeField] private bool isReadAble = false;

        public bool IsReadAble => isReadAble;

        public Image RawImage => m_RawImage;

        private AspectRatioFitter aspectRatioFitter;

        private string m_lastImgPath;

        //原始图片
        private Sprite originalSprite;

        //是否加载完成
        private bool isLoadComplete = false;

        public bool IsLoadComplete => isLoadComplete;

        public RectTransform RectTrans => _mRectTransform;

        private RectTransform _mRectTransform;

        public bool SetNativeSize { get; set; }

        public bool IsUISprite { get; set; } = true;

        public UnityEvent onLoadComplete = new UnityEvent();

        private TextureObject m_TextureObject;

        private CancellationTokenSource _cancellationTokenSource;

        private void Awake() {
            ShowWaitObj(false);

            if (m_RawImage != null) {
                aspectRatioFitter = m_RawImage.GetComponent<AspectRatioFitter>();
            }
            originalSprite = m_RawImage.sprite;
            isLoadComplete = true;

            _mRectTransform = GetComponent<RectTransform>();
            m_TextureObject = null;
        }

        private void OnDestroy() {
            if (m_TextureObject != null) {
                if (m_TextureObject.Target != null)
                    GFX.Texture.Unspawn(m_TextureObject);
                m_TextureObject = null;
            }
        }

        private void ShowWaitObj(bool isShow) {
            if (m_WaitObj != null) {
                m_WaitObj.SetActive(isShow);
            }
        }

        private void SetSprite(Sprite sprite) {
            if (sprite == null) {
                m_RawImage.sprite = originalSprite;
                m_RawImage.color = Color.clear;
            } else {
                m_RawImage.sprite = sprite;
                m_RawImage.color = Color.white;
            }
            if (aspectRatioFitter != null && m_RawImage.sprite != null) {
                aspectRatioFitter.aspectRatio =
                    m_RawImage.sprite.texture.width / (float)m_RawImage.sprite.texture.height;
            }
            if (SetNativeSize) {
                m_RawImage.SetNativeSize();
            }
            m_RawImage.enabled = true;
        }

        public void SetTextureObject(TextureObject textureObject) {
            if (m_TextureObject != null) {
                TextureComponent.Instance.Unspawn(m_TextureObject);
                // GFX.Texture.Unspawn(m_TextureObject);
                m_TextureObject = null;
            }
            m_TextureObject = textureObject;
            if (textureObject == null) {
                SetSprite(null);
                return;
            }

            var texture = (Texture2DBuffer)m_TextureObject.Target;
            SetSprite(TextureExt.ToSprite(texture.Texture2D));
        }

        private string imgPath;

        public string ImgPath {
            get { return imgPath; }
            set {
                if (imgPath == value) return;
                imgPath = value;
                SetImgPath(value);
            }
        }

        private Texture2D texture2D;

        public Texture2D Texture2D {
            get { return texture2D; }
            set {
                texture2D = value;
                if (texture2D != null) {
                    m_RawImage.sprite = TextureExt.ToSprite(texture2D);
                }
            }
        }

        protected void SetImgPath(string imgPath) {
            ShowWaitObj(true);
            m_RawImage.enabled = false;
            isLoadComplete = false;
            if (string.IsNullOrEmpty(imgPath)) {
                m_lastImgPath = null;
                SetTextureObject(null);
                OnLoadComplete();
                return;
            }

            bool isLocal = this.imgPath.Contains(Constant.User.LocalImgPath);
            bool isWeb = this.imgPath.Contains(Constant.User.WebImgPath);
            if (isLocal) {
                if (m_lastImgPath == imgPath) {
                    OnLoadComplete();
                    return;
                }
                // m_lastImgPath = imgPath;
                // var textureObject = GFX.Texture.LoadTextureObject(imgPath);
                // SetTextureObject(textureObject);
                // OnLoadComplete();
                
                
                TextureComponent.Instance.SetTextureByFileAsync(this, imgPath).Forget();
                // GFX.Texture.SetTextureByFileAsync(this, imgPath).Forget();
                // m_RawImage.sprite = TextureExt.ToSprite(GFX.Texture.LoadTexture(imgPath));
            } else if (isWeb) {
                if (imgPath == m_lastImgPath) {
                    OnLoadComplete();
                    return;
                }
                TextureComponent.Instance.SetTextureByNetworkAsync(this, imgPath).Forget();
            } else {
                if (imgPath == m_lastImgPath) {
                    OnLoadComplete();
                    return;
                }
                LoadSprite().Forget();
            }
        }

        public void OnLoadSucceed(string imgPath) {
            m_lastImgPath = imgPath;
        }

        public void OnLoadFailed(string imgPath) {
            SetTextureObject(null);
            m_lastImgPath = null;
        }

        public void OnLoadComplete() {
            isLoadComplete = true;
            ShowWaitObj(false);
            onLoadComplete?.Invoke();
        }

        protected async UniTask LoadSprite() {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new();
            string spriteIconPath;
            // if (IsUISprite) {
            //     spriteIconPath = AssetUtility.GetUISpriteAsset(imgPath);
            // } else {
            //     spriteIconPath = AssetUtility.GetSpriteAsset(imgPath);
            // }
            var asset = await GameModule.Resource.LoadAssetAsync<Sprite>(imgPath);
            if (_cancellationTokenSource.Token.IsCancellationRequested) {
                return;
            }
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            if (asset != null) {
                OnLoadSucceed(imgPath);
                SetSprite(asset);
            } else {
                m_lastImgPath = null;
                SetSprite(null);
            }
            OnLoadComplete();
        }

        private void OnEnable() {
            m_lastImgPath = null;
        }
    }
}