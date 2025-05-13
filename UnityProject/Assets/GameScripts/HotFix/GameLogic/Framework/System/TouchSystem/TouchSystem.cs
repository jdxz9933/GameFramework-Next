using System;
using System.Collections;
using System.Collections.Generic;
using GameBase;
using GameMain;
using Lean.Common;
using Lean.Touch;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameLogic {
    public class TouchSystem : BaseLogicSys<TouchSystem> {
        private const string InputGroupDefault = "Default";

        public bool IgnoreIfStartedOverGui {
            get => m_IgnoreIfStartedOverGui;
            set => m_IgnoreIfStartedOverGui = value;
        }

        [SerializeField] private bool m_IgnoreIfStartedOverGui;

        public bool IgnoreIfOverGui {
            get => m_IgnoreIfOverGui;
            set => m_IgnoreIfOverGui = value;
        }

        [SerializeField] private bool m_IgnoreIfOverGui;

        private LeanSelectByFinger select;

        private Action<LeanFinger> PointerDown;
        private Action<LeanFinger> PointerUp;
        private Action<LeanFinger> PointerUpdate;
        private Action<LeanFinger> PointerTap;
        private Action<LeanFinger> PointerSwipe;
        private Action<LeanFinger> PointerLongTap;
        private Action<List<LeanFinger>> ZoomIn;
        private Action<List<LeanFinger>> ZoomOut;

        private Dictionary<string, Action<LeanFinger>> m_PointerDownDic;
        private Dictionary<string, Action<LeanFinger>> m_PointerUpDic;
        private Dictionary<string, Action<LeanFinger>> m_PointerUpdateDic;
        private Dictionary<string, Action<LeanFinger>> m_PointerTapDic;
        private Dictionary<string, Action<LeanFinger>> m_PointerSwipeDic;
        private Dictionary<string, Action<LeanFinger>> m_PointerLongTapDic;
        private Dictionary<string, Action<float>> m_ZoomDic;
        private Dictionary<string, Action<float>> m_ZoomInDic;
        private Dictionary<string, Action<float>> m_ZoomOutDic;

        private HashSet<string> m_InputGroupSet = new HashSet<string>();

        private LeanTouchSimulator leanTouchSimulator;

        public override bool OnInit() {
            base.OnInit();

            m_IgnoreIfOverGui = false;
            m_IgnoreIfStartedOverGui = false;

            m_PointerDownDic = new Dictionary<string, Action<LeanFinger>>();
            m_PointerUpDic = new Dictionary<string, Action<LeanFinger>>();
            m_PointerUpdateDic = new Dictionary<string, Action<LeanFinger>>();
            m_PointerTapDic = new Dictionary<string, Action<LeanFinger>>();
            m_PointerSwipeDic = new Dictionary<string, Action<LeanFinger>>();
            m_PointerLongTapDic = new Dictionary<string, Action<LeanFinger>>();
            m_ZoomDic = new Dictionary<string, Action<float>>();
            m_ZoomInDic = new Dictionary<string, Action<float>>();
            m_ZoomOutDic = new Dictionary<string, Action<float>>();

            m_InputGroupSet = new HashSet<string>();
            m_InputGroupSet.Add(InputGroupDefault);

            //创建LeanTouch
            var newObj = new GameObject("LeanTouch");
            // newObj.transform.SetParent(transform);
            newObj.GetOrAddComponent<LeanTouch>();
            leanTouchSimulator = newObj.AddComponent<LeanTouchSimulator>();
            AddFightSelect(newObj.transform);
            

            LeanTouch.OnFingerDown += HandleFingerDown;
            LeanTouch.OnFingerUpdate += HandleFingerUpdate;
            LeanTouch.OnFingerUp += HandleFingerUp;
            LeanTouch.OnFingerTap += HandleFingerTap;
            LeanTouch.OnFingerOld += HandleFingerOld;
            LeanTouch.OnFingerSwipe += HandleFingerSwipe;
            LeanTouch.OnGesture += HandleGesture;

            return true;
        }

        private void AddFightSelect(Transform transform) {
            //创建LeanSelect
            var newObj = new GameObject("LeanSelect");
            newObj.transform.SetParent(transform);
            select = newObj.AddComponent<LeanSelectByFinger>();
            select.ScreenQuery.Layers = (1 << Constant.Layer.DecalsLayerId) | (1 << Constant.Layer.DecalsUILayerId);
            select.DeselectWithFingers = true;
            select.ScreenQuery.Search = LeanScreenQuery.SearchType.GetComponent;
            //var fingerTap = newObj.AddComponent<LeanFingerTap>();
            //fingerTap.OnFinger.AddListener(OnFinger);
            // var fingerDown = newObj.AddComponent<LeanFingerDown>();
            // fingerDown.OnFinger.AddListener(OnFinger);
        }

        private void OnFinger(LeanFinger finger) {
            select.SelectScreenPosition(finger, finger.ScreenPosition);
        }

        private void OnDestroy() {
            LeanTouch.OnFingerDown -= HandleFingerDown;
            LeanTouch.OnFingerUpdate -= HandleFingerUpdate;
            LeanTouch.OnFingerUp -= HandleFingerUp;
            LeanTouch.OnFingerTap -= HandleFingerTap;
            LeanTouch.OnFingerOld -= HandleFingerOld;
            LeanTouch.OnFingerSwipe -= HandleFingerSwipe;
            LeanTouch.OnGesture -= HandleGesture;
        }

        //缓存
        private List<KeyValuePair<string, Action<LeanFinger>>> m_PointerTapList_cache =
            new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<LeanFinger>>> m_PointerUpList_cache =
            new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<LeanFinger>>> m_PointerUpdateList_cache =
            new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<LeanFinger>>> m_PointerDownList_cache =
            new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<LeanFinger>>> m_PointerLongTapList_cache =
            new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<LeanFinger>>>
            m_PointerSwipeList_cache = new List<KeyValuePair<string, Action<LeanFinger>>>();

        private List<KeyValuePair<string, Action<float>>> m_ZoomList_cache = new();

        private List<KeyValuePair<string, Action<float>>> m_ZoomInList_cache = new();

        private List<KeyValuePair<string, Action<float>>> m_ZoomOutList_cache = new();

        private void HandleFingerOld(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            m_PointerLongTapList_cache.Clear();
            m_PointerLongTapList_cache.AddRange(m_PointerLongTapDic);
            foreach (var action in m_PointerLongTapList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }
        }

        private void HandleGesture(List<LeanFinger> obj) {
            var filteredFingers = LeanTouch.GetFingers(IgnoreIfStartedOverGui, IgnoreIfOverGui);
            if (filteredFingers.Count > 1) {
                var val = LeanGesture.GetPinchScale(filteredFingers);

                m_ZoomList_cache.Clear();
                m_ZoomList_cache.AddRange(m_ZoomDic);
                foreach (var action in m_ZoomList_cache) {
                    if (m_InputGroupSet.Contains(action.Key)) {
                        action.Value?.Invoke(val);
                    }
                }

                if (val > 1) {
                    // Log.Debug($"缩小:{val}");
                    m_ZoomOutList_cache.Clear();
                    m_ZoomOutList_cache.AddRange(m_ZoomOutDic);
                    foreach (var action in m_ZoomOutList_cache) {
                        if (m_InputGroupSet.Contains(action.Key)) {
                            action.Value?.Invoke(val);
                        }
                    }
                } else if (val < 1) {
                    // Log.Debug($"放大:{val}");
                    m_ZoomInList_cache.Clear();
                    m_ZoomInList_cache.AddRange(m_ZoomInDic);
                    foreach (var action in m_ZoomInDic) {
                        if (m_InputGroupSet.Contains(action.Key)) {
                            action.Value?.Invoke(val);
                        }
                    }
                }
            }
        }

        private void HandleFingerSwipe(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            m_PointerSwipeList_cache.Clear();
            m_PointerSwipeList_cache.AddRange(m_PointerSwipeDic);
            foreach (var action in m_PointerSwipeList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }
        }

        private void HandleFingerTap(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            m_PointerTapList_cache.Clear();
            m_PointerTapList_cache.AddRange(m_PointerTapDic);
            foreach (var action in m_PointerTapList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }
        }

        private void HandleFingerUp(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            m_PointerUpList_cache.Clear();
            m_PointerUpList_cache.AddRange(m_PointerUpDic);
            foreach (var action in m_PointerUpList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }
        }

        private void HandleFingerUpdate(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            // TODO
            // var filteredFingers = LeanTouch.GetFingers(IgnoreIfStartedOverGui, IgnoreIfOverGui);
            // if (filteredFingers.Count > 1) return;

            m_PointerUpdateList_cache.Clear();
            m_PointerUpdateList_cache.AddRange(m_PointerUpdateDic);
            foreach (var action in m_PointerUpdateList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }
        }

        private void HandleFingerDown(LeanFinger obj) {
            if (obj.StartedOverGui && IgnoreIfStartedOverGui) return;
            if (obj.IsOverGui && IgnoreIfOverGui) return;

            m_PointerDownList_cache.Clear();
            m_PointerDownList_cache.AddRange(m_PointerDownDic);
            foreach (var action in m_PointerDownList_cache) {
                if (m_InputGroupSet.Contains(action.Key)) {
                    action.Value?.Invoke(obj);
                }
            }

            OnFinger(obj);
        }

        public void AddPointerDownListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerDownDic.ContainsKey(inputGroup)) {
                m_PointerDownDic[inputGroup] += action;
            } else {
                m_PointerDownDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerDownListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerDownDic.ContainsKey(inputGroup)) {
                m_PointerDownDic[inputGroup] -= action;
            }
        }

        public void AddPointerUpListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerUpDic.ContainsKey(inputGroup)) {
                m_PointerUpDic[inputGroup] += action;
            } else {
                m_PointerUpDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerUpListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerUpDic.ContainsKey(inputGroup)) {
                m_PointerUpDic[inputGroup] -= action;
            }
        }

        public void AddPointerUpdateListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerUpdateDic.ContainsKey(inputGroup)) {
                m_PointerUpdateDic[inputGroup] += action;
            } else {
                m_PointerUpdateDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerUpdateListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerUpdateDic.ContainsKey(inputGroup)) {
                m_PointerUpdateDic[inputGroup] -= action;
            }
        }

        public void AddPointerTapListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerTapDic.ContainsKey(inputGroup)) {
                m_PointerTapDic[inputGroup] += action;
            } else {
                m_PointerTapDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerTapListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerTapDic.ContainsKey(inputGroup)) {
                m_PointerTapDic[inputGroup] -= action;
            }
        }

        public void AddPointerSwipeListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerSwipeDic.ContainsKey(inputGroup)) {
                m_PointerSwipeDic[inputGroup] += action;
            } else {
                m_PointerSwipeDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerSwipeListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerSwipeDic.ContainsKey(inputGroup)) {
                m_PointerSwipeDic[inputGroup] -= action;
            }
        }

        public void AddPointerLongTapListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerLongTapDic.ContainsKey(inputGroup)) {
                m_PointerLongTapDic[inputGroup] += action;
            } else {
                m_PointerLongTapDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemovePointerLongTapListener(Action<LeanFinger> action, string inputGroup = InputGroupDefault) {
            if (m_PointerLongTapDic.ContainsKey(inputGroup)) {
                m_PointerLongTapDic[inputGroup] -= action;
            }
        }

        public void AddZoomListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomDic.ContainsKey(inputGroup)) {
                m_ZoomDic[inputGroup] += action;
            } else {
                m_ZoomDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemoveZoomListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomDic.ContainsKey(inputGroup)) {
                m_ZoomDic[inputGroup] -= action;
            }
        }

        public void AddZoomInListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomOutDic.ContainsKey(inputGroup)) {
                m_ZoomOutDic[inputGroup] += action;
            } else {
                m_ZoomOutDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemoveZoomInListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomInDic.ContainsKey(inputGroup)) {
                m_ZoomInDic[inputGroup] -= action;
            }
        }

        public void AddZoomOutListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomInDic.ContainsKey(inputGroup)) {
                m_ZoomInDic[inputGroup] += action;
            } else {
                m_ZoomInDic.Add(inputGroup, action);
                m_InputGroupSet.Add(inputGroup);
            }
        }

        public void RemoveZoomOutListener(Action<float> action, string inputGroup = InputGroupDefault) {
            if (m_ZoomOutDic.ContainsKey(inputGroup)) {
                m_ZoomOutDic[inputGroup] -= action;
            }
        }

        //激活或禁用输入组
        public void SetInputActive(string inputGroup, bool active) {
            if (active) {
                m_InputGroupSet.Add(inputGroup);
            } else {
                m_InputGroupSet.Remove(inputGroup);
            }
        }

        public void SetTexture(Texture2D texture) {
            leanTouchSimulator.FingerTexture = texture;
        }
    }
}