using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Resource;
using Loxodon.Framework.Asynchronous;
using Loxodon.Framework.Execution;
using Loxodon.Framework.Views;
using UnityEngine;

namespace GameLogic {
    public class LoadAssetObject {
        public string Location;
        public object Asset;

        public float Progress;

        public bool IsDone {
            get { return this.isDone; }
        }

        private bool isDone;

        public LoadAssetObject(string location) {
            this.Location = location;
            isDone = false;
        }

        public void SetDone() {
            this.isDone = true;
        }

        public void SetResult(object asset) {
            this.Asset = asset;
        }

        public void SetProgress(float progress) {
            this.Progress = progress;
        }
    }
    public class UIViewLocator : UIViewLocatorBase {
        private GlobalWindowManagerBase globalWindowManager;
        private Dictionary<string, WeakReference> templates = new Dictionary<string, WeakReference>();

        protected string Normalize(string name) {
            int index = name.IndexOf('.');
            if (index < 0)
                return name;
            return name.Substring(0, index);
        }

        protected virtual IWindowManager GetDefaultWindowManager() {
            if (globalWindowManager != null)
                return globalWindowManager;

            globalWindowManager = GlobalWindowManagerBase.Root;
            if (globalWindowManager == null)
                throw new NotFoundException("GlobalWindowManager");
            return globalWindowManager;
        }

        public override T LoadView<T>(string name) {
            return DoLoadView<T>(name);
        }

        protected virtual T DoLoadView<T>(string name) {
            // name = Normalize(name);
            WeakReference weakRef;
            GameObject viewTemplateGo = null;
            try {
                if (this.templates.TryGetValue(name, out weakRef) && weakRef.IsAlive) {
                    viewTemplateGo = (GameObject)weakRef.Target;

                    //Check if the object is valid because it may have been destroyed.
                    //Unmanaged objects,the weak caches do not accurately track the validity of objects.
                    if (viewTemplateGo != null) {
                        string goName = viewTemplateGo.name;
                    }
                }
            } catch (Exception) {
                viewTemplateGo = null;
            }

            if (viewTemplateGo == null) {
                viewTemplateGo = GameModule.Resource.LoadAsset<GameObject>(name);
                if (viewTemplateGo != null) {
                    viewTemplateGo.SetActive(false);
                    this.templates[name] = new WeakReference(viewTemplateGo);
                }
            }

            if (viewTemplateGo == null || viewTemplateGo.GetComponent<T>() == null)
                return default(T);

            GameObject go = GameObject.Instantiate(viewTemplateGo);
            go.name = viewTemplateGo.name;
            T view = go.GetComponent<T>();
            if (view == null && go != null)
                GameObject.Destroy(go);
            return view;
        }

        public override IProgressResult<float, T> LoadViewAsync<T>(string name) {
            ProgressResult<float, T> result = new ProgressResult<float, T>();
            Executors.RunOnCoroutineNoReturn(DoLoad<T>(result, name));
            return result;
        }

        private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userData) {
            LoadAssetObject loadAssetObject = userData as LoadAssetObject;
            if (loadAssetObject == null)
                return;
            loadAssetObject.SetResult(asset);
            loadAssetObject.SetDone();
        }

        private void OnLoadAssetFailure(string assetName, LoadResourceStatus status, string errorMessage, object userData) {
            LoadAssetObject loadAssetObject = userData as LoadAssetObject;
            if (loadAssetObject == null)
                return;
            loadAssetObject.SetDone();
        }

        private void OnLoadAssetUpdate(string assetName, float progress, object userData) {
            LoadAssetObject loadAssetObject = userData as LoadAssetObject;
            if (loadAssetObject == null)
                return;
            loadAssetObject.SetProgress(progress);
        }

        protected virtual IEnumerator DoLoad<T>(IProgressPromise<float, T> promise, string name, IWindowManager windowManager = null) {
            // name = Normalize(name);
            WeakReference weakRef;
            GameObject viewTemplateGo = null;
            try {
                if (this.templates.TryGetValue(name, out weakRef) && weakRef.IsAlive) {
                    viewTemplateGo = (GameObject)weakRef.Target;

                    //Check if the object is valid because it may have been destroyed.
                    //Unmanaged objects,the weak caches do not accurately track the validity of objects.
                    if (viewTemplateGo != null) {
                        string goName = viewTemplateGo.name;
                    }
                }
            } catch (Exception) {
                viewTemplateGo = null;
            }

            if (viewTemplateGo == null) {
                var obj = new LoadAssetObject(name);
                GameModule.Resource.LoadAssetAsync(name, typeof(GameObject), 0,
                    new LoadAssetCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure, OnLoadAssetUpdate), obj);
                // ResourceRequest request = Resources.LoadAsync<GameObject>(name);
                while (!obj.IsDone) {
                    promise.UpdateProgress(obj.Progress);
                    yield return null;
                }

                viewTemplateGo = (GameObject)obj.Asset;
                if (viewTemplateGo != null) {
                    viewTemplateGo.SetActive(false);
                    this.templates[name] = new WeakReference(viewTemplateGo);
                }
            }

            if (viewTemplateGo == null || viewTemplateGo.GetComponent<T>() == null) {
                promise.UpdateProgress(1f);
                promise.SetException(new NotFoundException(name));
                yield break;
            }

            GameObject go = GameObject.Instantiate(viewTemplateGo);
            go.name = viewTemplateGo.name;
            T view = go.GetComponent<T>();
            if (view == null) {
                GameObject.Destroy(go);
                promise.SetException(new NotFoundException(name));
            } else {
                if (windowManager != null && view is IWindow)
                    (view as IWindow).WindowManager = windowManager;

                promise.UpdateProgress(1f);
                promise.SetResult(view);
            }
        }

        public override T LoadWindow<T>(string name) {
            return LoadWindow<T>(null, name);
        }

        public override T LoadWindow<T>(IWindowManager windowManager, string name) {
            if (windowManager == null)
                windowManager = this.GetDefaultWindowManager();

            T target = this.DoLoadView<T>(name);
            if (target != null)
                target.WindowManager = windowManager;

            return target;
        }

        public override IProgressResult<float, T> LoadWindowAsync<T>(string name) {
            return this.LoadWindowAsync<T>(null, name);
        }

        public override IProgressResult<float, T> LoadWindowAsync<T>(IWindowManager windowManager, string name) {
            if (windowManager == null)
                windowManager = this.GetDefaultWindowManager();

            ProgressResult<float, T> result = new ProgressResult<float, T>();
            Executors.RunOnCoroutineNoReturn(DoLoad<T>(result, name, windowManager));
            return result;
        }
    }
}